using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LoopFlow.Models;

namespace LoopFlow.Services
{
    public class TrustChainService
    {
        private readonly ApplicationDbContext _db;

        public TrustChainService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<TrustChainRecord> RecordEventAsync(int orderId, string eventType, string eventData)
        {
            return await RecordEventAsync(orderId, eventType, eventData, null);
        }

        public async Task<TrustChainRecord> RecordEventAsync(int orderId, string eventType, string eventData, int? verifiedUserId)
        {
            var lastRecord = await _db.TrustChainRecords
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            var prevHash = lastRecord != null ? lastRecord.Hash : "0000000000000000000000000000000000000000000000000000000000000000";
            var rawData = orderId + ":" + eventType + ":" + eventData + ":" + prevHash + ":" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                var hashString = sb.ToString();

                var record = new TrustChainRecord
                {
                    OrderId = orderId,
                    EventType = eventType,
                    EventData = eventData,
                    Hash = hashString,
                    PreviousHash = prevHash,
                    VerifiedBy = verifiedUserId,
                    VerificationStatus = "Verified",
                    VerificationDate = DateTime.UtcNow,
                    IsTampered = false
                };

                _db.TrustChainRecords.Add(record);
                await _db.SaveChangesAsync();
                return record;
            }
        }
    }
}
