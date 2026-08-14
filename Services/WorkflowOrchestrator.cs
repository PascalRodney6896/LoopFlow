using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using LoopFlow.Models;

namespace LoopFlow.Services
{
    public class WorkflowOrchestrator
    {
        private readonly ApplicationDbContext _db;
        private readonly TrustChainService _trustChain;

        public WorkflowOrchestrator(ApplicationDbContext db)
        {
            _db = db;
            _trustChain = new TrustChainService(db);
        }

        public async Task LogActivityAsync(
            string actorName,
            string actorRole,
            string actionType,
            string entityType,
            int? entityId,
            string referenceNumber,
            string oldValue,
            string newValue,
            string notes,
            bool success = true,
            string errorMessage = null)
        {
            var log = new AuditLog
            {
                ActorName = actorName,
                ActorRole = actorRole,
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                ReferenceNumber = referenceNumber,
                OldValue = oldValue,
                NewValue = newValue,
                Notes = notes,
                Success = success,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.UtcNow
            };
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task SendNotificationAsync(int userId, string title, string message, string type = "INFO")
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                SentAt = DateTime.UtcNow
            };
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        public async Task RecordTrustChainEventAsync(int orderId, string eventType, string eventData)
        {
            await _trustChain.RecordEventAsync(orderId, eventType, eventData);
        }
    }
}
