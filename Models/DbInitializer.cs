using System;
using System.Data.Entity;
using System.Linq;

namespace LoopFlow.Models
{
    public class DbInitializer : DropCreateDatabaseIfModelChanges<ApplicationDbContext>
    {
        protected override void Seed(ApplicationDbContext context)
        {
            base.Seed(context);

            if (context.DomainUsers.Any())
            {
                return;
            }

            // 1. Seed Demo Users
            var buyerUser = new User
            {
                FullName = "Wanjiku Farmer Cooperative",
                Email = "wanjiku@farmcoop.co.ke",
                PhoneNumber = "+254712345678",
                BusinessName = "Rift Valley Agri-Hub",
                Role = "Buyer",
                PasswordHash = "DemoHash123",
                Salt = "Salt123",
                IsActive = true,
                IsVerified = true
            };

            var supplierUser1 = new User
            {
                FullName = "Kenya Seed & Fertilizer Ltd",
                Email = "sales@kenyaseed.co.ke",
                PhoneNumber = "+254722998877",
                BusinessName = "Kenya Seed Corp",
                Role = "Supplier",
                PasswordHash = "DemoHash123",
                Salt = "Salt123",
                IsActive = true,
                IsVerified = true
            };

            var supplierUser2 = new User
            {
                FullName = "Athi River Chemicals Ltd",
                Email = "orders@athichem.co.ke",
                PhoneNumber = "+254733445566",
                BusinessName = "Athi River Chemicals",
                Role = "Supplier",
                PasswordHash = "DemoHash123",
                Salt = "Salt123",
                IsActive = true,
                IsVerified = true
            };

            var bankUser = new User
            {
                FullName = "LOOP SME Credit Underwriter",
                Email = "underwriter@loop.co.ke",
                PhoneNumber = "+254700000000",
                BusinessName = "LOOP Bank Kenya",
                Role = "Financier",
                PasswordHash = "DemoHash123",
                Salt = "Salt123",
                IsActive = true,
                IsVerified = true
            };

            context.DomainUsers.Add(buyerUser);
            context.DomainUsers.Add(supplierUser1);
            context.DomainUsers.Add(supplierUser2);
            context.DomainUsers.Add(bankUser);
            context.SaveChanges();

            // 2. Seed Loop Accounts
            var buyerWallet = new LoopAccount
            {
                UserId = buyerUser.Id,
                LoopAccountId = "ACC-LOOP-BUY-001",
                LoopWalletId = "WLT-LOOP-BUY-001",
                WalletNumber = "2020019988",
                AccountNumber = "0100987654321",
                LoopCustomerCode = "CUST-BUYER-88",
                WalletBalance = 45000.00m,
                AccountBalance = 120000.00m
            };

            var sup1Wallet = new LoopAccount
            {
                UserId = supplierUser1.Id,
                LoopAccountId = "ACC-LOOP-SUP-001",
                LoopWalletId = "WLT-LOOP-SUP-001",
                WalletNumber = "2020021122",
                AccountNumber = "0100987654322",
                LoopCustomerCode = "CUST-SUP1-99",
                WalletBalance = 850000.00m,
                AccountBalance = 2400000.00m
            };

            var sup2Wallet = new LoopAccount
            {
                UserId = supplierUser2.Id,
                LoopAccountId = "ACC-LOOP-SUP-002",
                LoopWalletId = "WLT-LOOP-SUP-002",
                WalletNumber = "2020033344",
                AccountNumber = "0100987654323",
                LoopCustomerCode = "CUST-SUP2-100",
                WalletBalance = 420000.00m,
                AccountBalance = 1100000.00m
            };

            context.LoopAccounts.Add(buyerWallet);
            context.LoopAccounts.Add(sup1Wallet);
            context.LoopAccounts.Add(sup2Wallet);
            context.SaveChanges();

            // 3. Seed Buyer & Supplier Profiles
            var buyerProfile = new Buyer
            {
                UserId = buyerUser.Id,
                BuyerCode = "BUY-001",
                BusinessCategory = "Agriculture & Produce",
                YearsInBusiness = 6,
                AverageMonthlySpend = 650000.00m,
                CreditScore = 88,
                IsCreditApproved = true,
                TotalPurchases = 3200000.00m
            };

            var supplier1Profile = new Supplier
            {
                UserId = supplierUser1.Id,
                SupplierCode = "SUP-001",
                BusinessRegistration = "CPR/2018/889900",
                KRA_PIN = "P051234567A",
                BusinessCategory = "Agri Inputs & Seeds",
                ContactPhone = "+254722998877",
                ContactEmail = "sales@kenyaseed.co.ke",
                BusinessAddress = "Kenya Seed Complex, Nakuru Road, Kitale",
                SettlementBank = "NCBA Bank Kenya",
                SettlementAccount = "0100987654322",
                SettlementAccountName = "Kenya Seed & Fertilizer Ltd",
                PaymentDetails = "Paybill: 888222 | Account: SUP-001-SETTLE",
                KYCStatus = "Verified",
                AverageOrderValue = 300000.00m,
                IsVerifiedSupplier = true,
                Rating = 4.9m,
                TotalSales = 12500000.00m
            };

            var supplier2Profile = new Supplier
            {
                UserId = supplierUser2.Id,
                SupplierCode = "SUP-002",
                BusinessRegistration = "CPR/2019/776655",
                KRA_PIN = "P059876543B",
                BusinessCategory = "Agri Chemicals & Fertilizer",
                ContactPhone = "+254733445566",
                ContactEmail = "orders@athichem.co.ke",
                BusinessAddress = "Athi River Industrial Zone, Plot 42, Mavoko",
                SettlementBank = "NCBA Bank Kenya",
                SettlementAccount = "0100987654323",
                SettlementAccountName = "Athi River Chemicals Ltd",
                PaymentDetails = "Paybill: 888223 | Account: SUP-002-SETTLE",
                KYCStatus = "Verified",
                AverageOrderValue = 200000.00m,
                IsVerifiedSupplier = true,
                Rating = 4.7m,
                TotalSales = 8400000.00m
            };

            context.Buyers.Add(buyerProfile);
            context.Suppliers.Add(supplier1Profile);
            context.Suppliers.Add(supplier2Profile);
            context.SaveChanges();

            // 4. Seed Credit Limit & Sweep Configurations
            var creditLimit = new CreditLimit
            {
                BuyerId = buyerProfile.Id,
                TotalCreditLimit = 500000.00m,
                UsedCredit = 150000.00m,
                AvailableCredit = 350000.00m,
                InterestRate = 17.00m,
                FacilityFeeRate = 0.50m,
                InsuranceFeeRate = 0.11m,
                SweepPercentage = 30.00m,
                MaxExposureLimit = 35000000.00m,
                IsActive = true
            };

            var sweepConfig = new SweepConfiguration
            {
                BuyerId = buyerProfile.Id,
                SweepType = "Fixed",
                FixedPercentage = 30.00m,
                MinimumBalance = 1000.00m,
                SweepFrequency = "Daily",
                IsActive = true
            };

            context.CreditLimits.Add(creditLimit);
            context.SweepConfigurations.Add(sweepConfig);
            context.SaveChanges();

            // 5. Seed Purchase Orders & Supplier Splits
            var po1 = new PurchaseOrder
            {
                OrderNumber = "ORD-2026-8801",
                BuyerId = buyerProfile.Id,
                TotalAmount = 500000.00m,
                PaymentMethod = "LOOP_BNPL",
                Status = "Completed",
                SupplierVerificationStatus = "VERIFIED",
                InventoryAvailabilityConfirmed = true,
                FulfilmentStatus = "Delivered",
                RequiredDeliveryDate = DateTime.UtcNow.AddDays(5),
                OrderDate = DateTime.UtcNow.AddDays(-5),
                DispatchedAt = DateTime.UtcNow.AddDays(-3),
                DeliveredAt = DateTime.UtcNow.AddDays(-1)
            };

            var po2 = new PurchaseOrder
            {
                OrderNumber = "ORD-2026-9042",
                BuyerId = buyerProfile.Id,
                TotalAmount = 350000.00m,
                PaymentMethod = "LOOP_BNPL",
                Status = "PendingSupplierApproval",
                SupplierVerificationStatus = "PENDING_VERIFICATION",
                InventoryAvailabilityConfirmed = false,
                FulfilmentStatus = "Order Received",
                RequiredDeliveryDate = DateTime.UtcNow.AddDays(7),
                OrderDate = DateTime.UtcNow.AddDays(-1)
            };

            context.PurchaseOrders.Add(po1);
            context.PurchaseOrders.Add(po2);
            context.SaveChanges();

            var split1 = new SupplierSplit
            {
                OrderId = po1.Id,
                SupplierId = supplier1Profile.Id,
                SupplierName = "Kenya Seed & Fertilizer Ltd",
                SupplierCode = "SUP-001",
                Amount = 300000.00m,
                ItemDescription = "Hybrid Maize Seed H614 - 1,000 Bags",
                Quantity = 1000,
                UnitPrice = 300.00m,
                IsPaid = true,
                PaymentStatus = "COMPLETED",
                InvoiceNumber = "INV-KS-2026-001",
                VerificationStatus = "VERIFIED",
                PaymentDate = DateTime.UtcNow.AddDays(-2),
                TransactionReference = "TXN-LOOP-DISB-9901"
            };

            var split2 = new SupplierSplit
            {
                OrderId = po1.Id,
                SupplierId = supplier2Profile.Id,
                SupplierName = "Athi River Chemicals Ltd",
                SupplierCode = "SUP-002",
                Amount = 200000.00m,
                ItemDescription = "CAN Topdressing Fertilizer - 500 Bags",
                Quantity = 500,
                UnitPrice = 400.00m,
                IsPaid = true,
                PaymentStatus = "COMPLETED",
                InvoiceNumber = "INV-ARC-2026-008",
                VerificationStatus = "VERIFIED",
                PaymentDate = DateTime.UtcNow.AddDays(-2),
                TransactionReference = "TXN-LOOP-DISB-9902"
            };

            var split3 = new SupplierSplit
            {
                OrderId = po2.Id,
                SupplierId = supplier1Profile.Id,
                SupplierName = "Kenya Seed & Fertilizer Ltd",
                SupplierCode = "SUP-001",
                Amount = 350000.00m,
                ItemDescription = "Certified Wheat Seeds - 700 Bags",
                Quantity = 700,
                UnitPrice = 500.00m,
                IsPaid = false,
                PaymentStatus = "PENDING",
                InvoiceNumber = "INV-KS-2026-002",
                VerificationStatus = "PENDING_VERIFICATION"
            };

            context.SupplierSplits.Add(split1);
            context.SupplierSplits.Add(split2);
            context.SupplierSplits.Add(split3);

            // Seed Supplier Invoices
            var inv1 = new SupplierInvoice
            {
                InvoiceNumber = "INV-KS-2026-001",
                OrderId = po1.Id,
                SupplierId = supplier1Profile.Id,
                Amount = 300000.00m,
                Currency = "KES",
                InvoiceDate = DateTime.UtcNow.AddDays(-4),
                DueDate = DateTime.UtcNow.AddDays(10),
                Status = "PAID",
                VerificationStatus = "VERIFIED",
                DuplicateCheckHash = "SUP-001_INV-KS-2026-001_300000.00",
                Notes = "Standard commercial invoice for Order #ORD-2026-8801"
            };

            var inv2 = new SupplierInvoice
            {
                InvoiceNumber = "INV-KS-2026-002",
                OrderId = po2.Id,
                SupplierId = supplier1Profile.Id,
                Amount = 350000.00m,
                Currency = "KES",
                InvoiceDate = DateTime.UtcNow.AddDays(-1),
                DueDate = DateTime.UtcNow.AddDays(14),
                Status = "PENDING_VERIFICATION",
                VerificationStatus = "PENDING_VERIFICATION",
                DuplicateCheckHash = "SUP-001_INV-KS-2026-002_350000.00",
                Notes = "Pending supplier confirmation for Wheat Seed order"
            };

            context.SupplierInvoices.Add(inv1);
            context.SupplierInvoices.Add(inv2);

            // 6. Seed Financing Request
            var finRequest = new FinancingRequest
            {
                OrderId = po1.Id,
                BuyerId = buyerProfile.Id,
                TotalAmount = 500000.00m,
                CreditLimitAtRequest = 500000.00m,
                Status = "Approved",
                ApprovedDate = DateTime.UtcNow.AddDays(-2),
                ApprovedAmount = 500000.00m,
                Notes = "Approved by Kenya Seed & Athi River Chemicals for immediate planting season fulfillment."
            };

            context.FinancingRequests.Add(finRequest);
            context.SaveChanges();

            // 7. Seed Loan & Repayment Sweep History
            var loanDisbursement = new LoanTransaction
            {
                OrderId = po1.Id,
                BuyerId = buyerProfile.Id,
                TransactionType = "Disbursement",
                Amount = 500000.00m,
                PrincipalAmount = 500000.00m,
                InterestAmount = 0.00m,
                FeeAmount = 2500.00m,
                BalanceBefore = 0.00m,
                BalanceAfter = 500000.00m,
                Status = "Completed",
                TransactionReference = "TXN-LOOP-LOAN-001",
                Notes = "Initial Loan Disbursement via LOOP Send Money API"
            };

            var sweepRepayment = new LoanTransaction
            {
                OrderId = po1.Id,
                BuyerId = buyerProfile.Id,
                TransactionType = "Sweep",
                Amount = 150000.00m,
                PrincipalAmount = 145000.00m,
                InterestAmount = 5000.00m,
                FeeAmount = 0.00m,
                BalanceBefore = 500000.00m,
                BalanceAfter = 350000.00m,
                Status = "Completed",
                TransactionReference = "TXN-LOOP-SWEEP-1002",
                Notes = "Automated 30% daily collections sweep via LOOP IPN"
            };

            context.LoanTransactions.Add(loanDisbursement);
            context.LoanTransactions.Add(sweepRepayment);

            var sweepHist = new SweepHistory
            {
                BuyerId = buyerProfile.Id,
                SweepAmount = 150000.00m,
                SweepPercentage = 30.00m,
                BalanceBefore = 500000.00m,
                BalanceAfter = 350000.00m,
                LoanBalanceBefore = 500000.00m,
                LoanBalanceAfter = 350000.00m,
                Status = "Completed",
                TransactionReference = "TXN-LOOP-SWEEP-1002",
                SweepDate = DateTime.UtcNow.AddDays(-1)
            };

            context.SweepHistories.Add(sweepHist);

            // 8. Seed TrustChain Record & AI Cash Flow Forecasts
            var trustChain = new TrustChainRecord
            {
                OrderId = po1.Id,
                EventType = "Settlement",
                EventData = "{\"Order\":\"ORD-2026-8801\",\"SupplierA\":300000,\"SupplierB\":200000,\"LoopRef\":\"TXN-LOOP-DISB-9901\"}",
                Hash = "8f9b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b",
                PreviousHash = "0000000000000000000000000000000000000000000000000000000000000000",
                VerificationStatus = "Verified",
                VerificationDate = DateTime.UtcNow,
                IsTampered = false
            };

            var cashForecast = new CashFlowForecast
            {
                BuyerId = buyerProfile.Id,
                ForecastDate = DateTime.UtcNow.AddDays(30),
                ProjectedInflow = 1200000.00m,
                ProjectedOutflow = 650000.00m,
                NetCashFlow = 550000.00m,
                ProjectedBalance = 900000.00m,
                IsDeficit = false,
                SurplusAmount = 550000.00m,
                ConfidenceLevel = 0.96m,
                PeriodType = "30Day"
            };

            var mmfAdvice = new InvestmentRecommendation
            {
                BuyerId = buyerProfile.Id,
                ProductType = "MMF",
                ProductName = "NCBA Loop Money Market Fund (Surplus Sweep)",
                Amount = 250000.00m,
                Rate = 11.85m,
                Tenor = "30 days",
                Liquidity = "High",
                RiskLevel = "Low",
                Explanation = "Your 30-day projected surplus of KES 550,000 allows sweeping KES 250,000 into NCBA MMF to earn ~11.85% p.a. while keeping daily liquidity intact.",
                Status = "Pending",
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };

            context.TrustChainRecords.Add(trustChain);
            context.CashFlowForecasts.Add(cashForecast);
            context.InvestmentRecommendations.Add(mmfAdvice);
            context.SaveChanges();
        }
    }
}
