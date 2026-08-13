using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using LoopFlow.Models;

namespace LoopFlow.Services
{
    public class SweepEngine
    {
        private readonly ApplicationDbContext _db;
        private readonly ILoopApiService _loopApi;

        public SweepEngine(ApplicationDbContext db, ILoopApiService loopApi)
        {
            _db = db;
            _loopApi = loopApi;
        }

        public async Task<SweepHistory> ProcessIncomingSalesCollectionAsync(int buyerId, decimal saleAmount)
        {
            var buyer = await _db.Buyers
                .Include(b => b.CreditLimit)
                .Include(b => b.SweepConfiguration)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == buyerId);

            if (buyer == null || buyer.CreditLimit == null || buyer.CreditLimit.UsedCredit <= 0)
                return null;

            var sweepPct = buyer.SweepConfiguration != null ? buyer.SweepConfiguration.FixedPercentage ?? buyer.CreditLimit.SweepPercentage : buyer.CreditLimit.SweepPercentage;
            var sweepAmount = Math.Min(saleAmount * (sweepPct / 100.00m), buyer.CreditLimit.UsedCredit);

            if (sweepAmount <= 0) return null;

            var loanBalBefore = buyer.CreditLimit.UsedCredit;
            buyer.CreditLimit.UsedCredit -= sweepAmount;
            buyer.CreditLimit.AvailableCredit += sweepAmount;
            var loanBalAfter = buyer.CreditLimit.UsedCredit;

            var loopRef = await _loopApi.SendMoneyDisbursementAsync("LOOP-BANK-REPAYMENT", sweepAmount, "SWEEP-" + buyer.BuyerCode);

            // Add Loan Transaction
            var loanTxn = new LoanTransaction
            {
                BuyerId = buyer.Id,
                TransactionType = "Sweep",
                Amount = sweepAmount,
                PrincipalAmount = sweepAmount * 0.95m,
                InterestAmount = sweepAmount * 0.05m,
                BalanceBefore = loanBalBefore,
                BalanceAfter = loanBalAfter,
                Status = "Completed",
                TransactionReference = loopRef,
                Notes = "Automated " + sweepPct + "% Daily Repayment Sweep from incoming IPN sale of KES " + saleAmount.ToString("N2")
            };
            _db.LoanTransactions.Add(loanTxn);

            // Add Sweep History
            var sweepHist = new SweepHistory
            {
                BuyerId = buyer.Id,
                SweepAmount = sweepAmount,
                SweepPercentage = sweepPct,
                BalanceBefore = saleAmount,
                BalanceAfter = saleAmount - sweepAmount,
                LoanBalanceBefore = loanBalBefore,
                LoanBalanceAfter = loanBalAfter,
                Status = "Completed",
                TransactionReference = loopRef,
                SweepDate = DateTime.UtcNow
            };
            _db.SweepHistories.Add(sweepHist);

            await _db.SaveChangesAsync();
            return sweepHist;
        }
    }
}
