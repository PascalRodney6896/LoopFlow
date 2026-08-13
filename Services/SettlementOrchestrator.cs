using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using LoopFlow.Models;

namespace LoopFlow.Services
{
    public class SettlementOrchestrator
    {
        private readonly ApplicationDbContext _db;
        private readonly ILoopApiService _loopApi;
        private readonly TrustChainService _trustChain;

        public SettlementOrchestrator(ApplicationDbContext db, ILoopApiService loopApi, TrustChainService trustChain)
        {
            _db = db;
            _loopApi = loopApi;
            _trustChain = trustChain;
        }

        public async Task<bool> ProcessFinancingRequestApprovalAsync(int requestId)
        {
            var req = await _db.FinancingRequests
                .Include(r => r.Order)
                .Include(r => r.Order.SupplierSplits)
                .Include(r => r.Buyer)
                .Include(r => r.Buyer.CreditLimit)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (req == null || req.Order == null || req.Buyer == null) return false;

            req.Status = "Approved";
            req.ApprovedDate = DateTime.UtcNow;
            req.ApprovedAmount = req.TotalAmount;
            req.Order.Status = "Funded";

            // Update Buyer Credit Limit
            if (req.Buyer.CreditLimit != null)
            {
                req.Buyer.CreditLimit.UsedCredit += req.TotalAmount;
                req.Buyer.CreditLimit.AvailableCredit -= req.TotalAmount;
            }

            // Record Loan Transaction Disbursement
            var loanTxn = new LoanTransaction
            {
                OrderId = req.OrderId,
                BuyerId = req.BuyerId,
                TransactionType = "Disbursement",
                Amount = req.TotalAmount,
                PrincipalAmount = req.TotalAmount,
                FeeAmount = req.TotalAmount * 0.005m,
                BalanceBefore = req.Buyer.CreditLimit != null ? req.Buyer.CreditLimit.UsedCredit - req.TotalAmount : 0,
                BalanceAfter = req.Buyer.CreditLimit != null ? req.Buyer.CreditLimit.UsedCredit : req.TotalAmount,
                Status = "Completed",
                TransactionReference = "TXN-LOOP-LOAN-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper(),
                Notes = "Disbursed via LOOP Send Money API for multi-supplier bundle."
            };
            _db.LoanTransactions.Add(loanTxn);

            // Execute Multi-Party Supplier Split Payouts via LOOP Send Money API
            foreach (var split in req.Order.SupplierSplits)
            {
                var loopRef = await _loopApi.SendMoneyDisbursementAsync(split.SupplierCode, split.Amount, req.Order.OrderNumber);
                split.IsPaid = true;
                split.PaymentDate = DateTime.UtcNow;
                split.TransactionReference = loopRef;

                var supplier = await _db.Suppliers.FindAsync(split.SupplierId);
                if (supplier != null)
                {
                    supplier.TotalSales += split.Amount;
                }
            }

            // Create Batch Record
            var batch = new SettlementBatch
            {
                BatchNumber = "BATCH-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper(),
                OrderId = req.OrderId,
                TotalAmount = req.TotalAmount,
                SupplierCount = req.Order.SupplierSplits.Count,
                Status = "Completed",
                CompletedAt = DateTime.UtcNow
            };
            _db.SettlementBatches.Add(batch);

            await _db.SaveChangesAsync();

            // Record Cryptographic TrustChain Audit Record
            await _trustChain.RecordEventAsync(req.OrderId, "Settlement", "{\"Order\":\"" + req.Order.OrderNumber + "\",\"Amount\":" + req.TotalAmount + ",\"Status\":\"Disbursed\"}");

            return true;
        }
    }
}
