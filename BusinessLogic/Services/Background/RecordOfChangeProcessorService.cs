using System;
using System.Threading;
using System.Threading.Tasks;
using DataAccess.Interceptors;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services.Background
{
    /// <summary>
    /// Consumes PendingAuditEntry items from AuditChannel, resolves the actor's
    /// display name from the database, builds the notification message, and
    /// persists a RecordOfChange row — all without blocking the original request.
    /// </summary>
    public sealed class RecordOfChangeProcessorService : BackgroundService
    {
        private readonly ILogger<RecordOfChangeProcessorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly AuditChannel _auditChannel;

        public RecordOfChangeProcessorService(
            ILogger<RecordOfChangeProcessorService> logger,
            IServiceProvider serviceProvider,
            AuditChannel auditChannel)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _auditChannel = auditChannel;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RecordOfChangeProcessorService started.");

            await foreach (var pending in _auditChannel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await PersistAsync(pending, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to persist audit record for {EntityName} #{EntityId} ({ChangeType})",
                        pending.EntityName, pending.EntityId, pending.ChangeType);
                }
            }

            _logger.LogInformation("RecordOfChangeProcessorService stopped.");
        }

        private async Task PersistAsync(PendingAuditEntry pending, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<UnicContext>();

            var actorName = await ResolveActorNameAsync(context, pending.ChangedBy, ct);
            var notification = BuildNotification(actorName, pending);

            var record = new RecordOfChange
            {
                EntityName = pending.EntityName,
                EntityId = pending.EntityId,
                OldValue = pending.OldValue,
                NewValue = pending.NewValue,
                ChangedAt = pending.ChangedAt,
                ChangedBy = pending.ChangedBy,
                ChangeType = pending.ChangeType,
                Notification = notification,
                ClubId = pending.ClubId
            };

            context.RecordsOfChange.Add(record);
            await context.SaveChangesAsync(ct);
        }

        private static async Task<string> ResolveActorNameAsync(
            UnicContext context, Guid userId, CancellationToken ct)
        {
            if (userId == Guid.Empty) return "Người dùng ẩn danh";

            var name = await context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);

            return string.IsNullOrWhiteSpace(name) ? $"Người dùng #{userId}" : name;
        }

        private static string BuildNotification(string actorName, PendingAuditEntry p)
        {
            var entityLabel = EntityLabel(p.EntityName);
            if(p.EntityName=="UserClubRoleAssignment")
            {
                return p.ChangeType switch
                {
                    "CREATE" => $"{actorName} đã phân thêm vai trò cho thành viên #{p.EntityId}",
                    "DELETE" => $"{actorName} đã xóa vai trò của thành viên #{p.EntityId}",
                    _ => $"{actorName} đã thay đổi thành viên #{p.EntityId} trong câu lạc bộ #{p.ClubId}"
                };
            }
            return p.ChangeType switch
            {
                "CREATE" => $"{actorName} đã tạo {entityLabel} #{p.EntityId}",
                "UPDATE" => $"{actorName} đã cập nhật {entityLabel} #{p.EntityId}",
                "DELETE" => $"{actorName} đã xóa {entityLabel} #{p.EntityId}",
                _ => $"{actorName} đã thay đổi {entityLabel} #{p.EntityId}"
            };
        }

        private static string EntityLabel(string entityName) => entityName switch
        {
            "Event" => "sự kiện",
            "Club" => "câu lạc bộ",
            "Department" => "phòng ban",
            "ClubFund" => "quỹ câu lạc bộ",
            "FundTransaction" => "giao dịch quỹ",
            "UserClubRole" => "thành viên",
            "ClubRole" => "vai trò",
            "RecruitmentCampaign" => "chiến dịch tuyển dụng",
            "UserClubRoleAssignment" => "phân quyền thành viên",
            _ => entityName
        };
    }
}
