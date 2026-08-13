using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using LoopFlow.Models;

namespace LoopFlow.Services
{
    public class MerchantBrainService
    {
        private readonly ApplicationDbContext _db;

        public MerchantBrainService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<CashFlowForecast> GetOrGenerateForecastAsync(int buyerId)
        {
            return await _db.CashFlowForecasts.FirstOrDefaultAsync(c => c.BuyerId == buyerId);
        }
    }
}
