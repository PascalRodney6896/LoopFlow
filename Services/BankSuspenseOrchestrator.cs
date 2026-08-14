using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using LoopFlow.Models;

namespace LoopFlow.Services
{
    public class BankSuspenseOrchestrator
    {
        private readonly ApplicationDbContext _db;

        public BankSuspenseOrchestrator(ApplicationDbContext db)
        {
            _db = db;
        }

        // Get or Create Master Suspense Account
        public async Task<BankSuspenseAccount> GetOrCreateSuspenseAccountAsync()
        {
            var account = await _db.BankSuspenseAccounts.FirstOrDefaultAsync();
            if (account == null)
            {
                account = new BankSuspenseAccount
                {
                    AccountNumber = "NCBA-SUSPENSE-001",
                    AccountName = "NCBA Bank Trade Financing Suspense Ledger",
                    TotalBalance = 0m,
                    SupplierDisbursementBalance = 0m,
                    TotalDisbursedToSuppliers = 0m,
                    MerchantRepaymentCollectionBalance = 0m,
                    TotalMerchantRepaymentsCollected = 0m,
                    MerchantFundsReceived = 0m,
                    FundsHeld = 0m,
                    PendingDisbursement = 0m,
                    TotalDisbursed = 0m,
                    ReversedFailedAmount = 0m,
                    UnreconciledItemsCount = 0,
                    LastReconciledAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.BankSuspenseAccounts.Add(account);
                await _db.SaveChangesAsync();
            }
            return account;
        }

        // 1. Record Supplier Disbursement Escrow Hold (When Loan Approved)
        public async Task<BankSuspenseLedgerEntry> RecordMerchantObligationHoldAsync(int orderId, string actor = "NCBA Bank System")
        {
            var order = await _db.PurchaseOrders
                .Include(p => p.Buyer)
                .Include(p => p.Buyer.User)
                .Include(p => p.SupplierSplits)
                .Include(p => p.Invoices)
                .Include(p => p.FinancingRequest)
                .FirstOrDefaultAsync(p => p.Id == orderId);

            if (order == null) return null;

            var existingHold = await _db.BankSuspenseLedgerEntries
                .FirstOrDefaultAsync(e => e.OrderId == orderId && e.LedgerState == "FundsHeld" && e.BucketType == "DISBURSEMENT_HOLDING");
            if (existingHold != null) return existingHold;

            var suspenseAccount = await GetOrCreateSuspenseAccountAsync();
            decimal openingBalance = suspenseAccount.SupplierDisbursementBalance;
            decimal amount = order.TotalAmount;
            decimal closingBalance = openingBalance + amount;

            var supplierSplit = order.SupplierSplits.FirstOrDefault();
            var invoice = order.Invoices.FirstOrDefault();

            var entry = new BankSuspenseLedgerEntry
            {
                TransactionReference = "SUSP-HOLD-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + new Random().Next(10000, 99999),
                BankSuspenseAccountId = suspenseAccount.Id,
                BuyerId = order.BuyerId,
                SupplierId = supplierSplit?.SupplierId,
                OrderId = order.Id,
                InvoiceId = invoice?.Id,
                FinancingRequestId = order.FinancingRequest?.Id,
                BucketType = "DISBURSEMENT_HOLDING",
                EntryType = "Credit",
                LedgerState = "FundsHeld",
                Amount = amount,
                SourceAccount = "MERCHANT_CREDIT_FACILITY (" + (order.Buyer?.User?.FullName ?? "Merchant") + ")",
                DestinationAccount = "NCBA_DISBURSEMENT_ESCROW_HOLDING",
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                ActorRole = actor,
                ReconciliationStatus = "Reconciled",
                Notes = "Disbursement Escrow Hold: Facility approval funds placed in suspense holding for PO #" + order.OrderNumber,
                CreatedAt = DateTime.UtcNow
            };

            _db.BankSuspenseLedgerEntries.Add(entry);

            // Update Master Suspense Account Sub-Ledgers
            suspenseAccount.SupplierDisbursementBalance = closingBalance;
            suspenseAccount.FundsHeld += amount;
            suspenseAccount.PendingDisbursement += amount;
            suspenseAccount.TotalBalance = suspenseAccount.SupplierDisbursementBalance + suspenseAccount.MerchantRepaymentCollectionBalance;
            suspenseAccount.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var trustChain = new TrustChainService(_db);
            await trustChain.RecordEventAsync(
                order.Id,
                "InvoicePostedToBank",
                "{\"Order\":\"" + order.OrderNumber + "\",\"Amount\":" + amount + ",\"Ref\":\"" + entry.TransactionReference + "\",\"Bucket\":\"DISBURSEMENT_HOLDING\"}"
            );

            return entry;
        }

        // 2. Release Supplier Disbursement from Escrow (When Conditions Met)
        public async Task<BankSuspenseLedgerEntry> ReleaseSupplierDisbursementAsync(int orderId, string actor = "NCBA Bank System")
        {
            var order = await _db.PurchaseOrders
                .Include(p => p.Buyer)
                .Include(p => p.Buyer.User)
                .Include(p => p.SupplierSplits)
                .Include(p => p.Invoices)
                .Include(p => p.FinancingRequest)
                .FirstOrDefaultAsync(p => p.Id == orderId);

            if (order == null) return null;

            var existingDisbursement = await _db.BankSuspenseLedgerEntries
                .FirstOrDefaultAsync(e => e.OrderId == orderId && e.LedgerState == "DisbursedToSupplier" && e.BucketType == "DISBURSEMENT_HOLDING");
            if (existingDisbursement != null) return existingDisbursement;

            var suspenseAccount = await GetOrCreateSuspenseAccountAsync();
            decimal openingBalance = suspenseAccount.SupplierDisbursementBalance;
            decimal amount = order.TotalAmount;

            if (openingBalance < amount)
            {
                await RecordMerchantObligationHoldAsync(orderId, actor);
                suspenseAccount = await GetOrCreateSuspenseAccountAsync();
                openingBalance = suspenseAccount.SupplierDisbursementBalance;
            }

            decimal closingBalance = Math.Max(0m, openingBalance - amount);
            var supplierSplit = order.SupplierSplits.FirstOrDefault();
            var supplier = supplierSplit != null ? await _db.Suppliers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == supplierSplit.SupplierId) : null;
            var invoice = order.Invoices.FirstOrDefault();

            var entry = new BankSuspenseLedgerEntry
            {
                TransactionReference = "SUSP-DISB-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + new Random().Next(10000, 99999),
                BankSuspenseAccountId = suspenseAccount.Id,
                BuyerId = order.BuyerId,
                SupplierId = supplierSplit?.SupplierId,
                OrderId = order.Id,
                InvoiceId = invoice?.Id,
                FinancingRequestId = order.FinancingRequest?.Id,
                BucketType = "DISBURSEMENT_HOLDING",
                EntryType = "Debit",
                LedgerState = "DisbursedToSupplier",
                Amount = amount,
                SourceAccount = "NCBA_DISBURSEMENT_ESCROW_HOLDING",
                DestinationAccount = "SUPPLIER_SETTLEMENT_ACCOUNT (" + (supplier?.User?.FullName ?? supplierSplit?.SupplierName ?? "Supplier") + ")",
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                ActorRole = actor,
                ReconciliationStatus = "Reconciled",
                Notes = "Disbursement released from Escrow to Supplier for PO #" + order.OrderNumber,
                CreatedAt = DateTime.UtcNow
            };

            _db.BankSuspenseLedgerEntries.Add(entry);

            // Update Master Suspense Account Sub-Ledgers
            suspenseAccount.SupplierDisbursementBalance = closingBalance;
            suspenseAccount.TotalDisbursedToSuppliers += amount;
            suspenseAccount.FundsHeld = Math.Max(0m, suspenseAccount.FundsHeld - amount);
            suspenseAccount.PendingDisbursement = Math.Max(0m, suspenseAccount.PendingDisbursement - amount);
            suspenseAccount.TotalDisbursed += amount;
            suspenseAccount.TotalBalance = suspenseAccount.SupplierDisbursementBalance + suspenseAccount.MerchantRepaymentCollectionBalance;
            suspenseAccount.LastReconciledAt = DateTime.UtcNow;
            suspenseAccount.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var trustChain = new TrustChainService(_db);
            await trustChain.RecordEventAsync(
                order.Id,
                "Settlement",
                "{\"Order\":\"" + order.OrderNumber + "\",\"Amount\":" + amount + ",\"Ref\":\"" + entry.TransactionReference + "\",\"Bucket\":\"DISBURSEMENT_HOLDING\"}"
            );

            return entry;
        }

        // 3. Record Merchant Loan Repayment & Sweeps Deposit (Separate Bucket)
        public async Task<BankSuspenseLedgerEntry> RecordSweepRepaymentDepositAsync(int buyerId, decimal sweepAmount, string sourceNote = "30% Automated Sales Sweep")
        {
            var buyer = await _db.Buyers.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == buyerId);
            var suspenseAccount = await GetOrCreateSuspenseAccountAsync();

            decimal openingBalance = suspenseAccount.MerchantRepaymentCollectionBalance;
            decimal closingBalance = openingBalance + sweepAmount;

            var entry = new BankSuspenseLedgerEntry
            {
                TransactionReference = "SUSP-REPAY-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + new Random().Next(10000, 99999),
                BankSuspenseAccountId = suspenseAccount.Id,
                BuyerId = buyerId,
                BucketType = "MERCHANT_REPAYMENT_COLLECTION",
                EntryType = "Credit",
                LedgerState = "FundsReceived",
                Amount = sweepAmount,
                SourceAccount = "MERCHANT_DAILY_COLLECTION (" + (buyer?.User?.FullName ?? "Merchant") + ")",
                DestinationAccount = "NCBA_MERCHANT_REPAYMENT_COLLECTION_LEDGER",
                OpeningBalance = openingBalance,
                ClosingBalance = closingBalance,
                ActorRole = "LOOP Automated Sweep Engine",
                ReconciliationStatus = "Reconciled",
                Notes = sourceNote + " deposited to Merchant Repayment Collection Sub-Ledger",
                CreatedAt = DateTime.UtcNow
            };

            _db.BankSuspenseLedgerEntries.Add(entry);

            suspenseAccount.MerchantRepaymentCollectionBalance = closingBalance;
            suspenseAccount.TotalMerchantRepaymentsCollected += sweepAmount;
            suspenseAccount.MerchantFundsReceived += sweepAmount;
            suspenseAccount.TotalBalance = suspenseAccount.SupplierDisbursementBalance + suspenseAccount.MerchantRepaymentCollectionBalance;
            suspenseAccount.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return entry;
        }

        // 4. Double-Entry Reconciliation Engine Check
        public async Task<bool> ReconcileSuspenseAccountAsync()
        {
            var suspenseAccount = await GetOrCreateSuspenseAccountAsync();
            var entries = await _db.BankSuspenseLedgerEntries.ToListAsync();

            decimal totalCredits = entries.Where(e => e.EntryType == "Credit").Sum(e => e.Amount);
            decimal totalDebits = entries.Where(e => e.EntryType == "Debit").Sum(e => e.Amount);
            decimal calculatedBalance = totalCredits - totalDebits;

            suspenseAccount.ReconciledBalance = calculatedBalance;
            suspenseAccount.UnreconciledItemsCount = entries.Count(e => e.ReconciliationStatus == "Unreconciled");
            suspenseAccount.LastReconciledAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return calculatedBalance == suspenseAccount.TotalBalance;
        }
    }
}
