using IPTS.Data;
using IPTS.Models.Entites;
using IPTS.Models.Enums;
using IPTS.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IPTS.Services
{
    public class AuditService(ApplicationDbContext context)
    {
        private readonly ApplicationDbContext _context = context;

        public async Task WriteAsync(
            EnAuditAction action,
            string details,
            string? actorUserId = null,
            string? actorUserName = null,
            string? targetUserId = null,
            string? entityName = null,
            string? entityId = null,
            string? ipAddress = null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Action = (int)action,
                Details = details,
                ActorUserId = actorUserId,
                ActorUserName = actorUserName,
                TargetUserId = targetUserId,
                EntityName = entityName,
                EntityId = entityId,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLogViewModel>> GetAsync(
            int? action = null,
            string? actor = null,
            DateTime? from = null,
            DateTime? to = null,
            int take = 200)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (action.HasValue)
                query = query.Where(a => a.Action == action.Value);

            if (!string.IsNullOrWhiteSpace(actor))
            {
                var term = actor.Trim();
                query = query.Where(a =>
                    (a.ActorUserName != null && a.ActorUserName.Contains(term)) ||
                    (a.ActorUserId != null && a.ActorUserId.Contains(term)));
            }

            if (from.HasValue)
                query = query.Where(a => a.CreatedAt >= from.Value.ToUniversalTime());

            if (to.HasValue)
            {
                var end = to.Value.Date.AddDays(1).ToUniversalTime();
                query = query.Where(a => a.CreatedAt < end);
            }

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .Select(a => new AuditLogViewModel
                {
                    Id = a.Id,
                    Action = a.Action,
                    ActionName = ((EnAuditAction)a.Action).ToString(),
                    ActorUserId = a.ActorUserId,
                    ActorUserName = a.ActorUserName,
                    TargetUserId = a.TargetUserId,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    Details = a.Details,
                    IpAddress = a.IpAddress,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();
        }
    }
}
