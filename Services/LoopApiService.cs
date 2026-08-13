using System;
using System.Threading.Tasks;

namespace LoopFlow.Services
{
    public interface ILoopApiService
    {
        Task<string> RequestStkPushAsync(string phoneNumber, decimal amount, string reference);
        Task<string> SendMoneyDisbursementAsync(string recipientLoopAccount, decimal amount, string reference);
        Task<decimal> GetWalletBalanceAsync(string loopWalletId);
    }

    public class LoopApiService : ILoopApiService
    {
        public Task<string> RequestStkPushAsync(string phoneNumber, decimal amount, string reference)
        {
            var referenceNo = "LOOP-STK-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            return Task.FromResult(referenceNo);
        }

        public Task<string> SendMoneyDisbursementAsync(string recipientLoopAccount, decimal amount, string reference)
        {
            var referenceNo = "LOOP-TXN-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
            return Task.FromResult(referenceNo);
        }

        public Task<decimal> GetWalletBalanceAsync(string loopWalletId)
        {
            return Task.FromResult(45000.00m);
        }
    }
}
