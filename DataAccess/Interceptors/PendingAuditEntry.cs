using System;

namespace DataAccess.Interceptors
{
    /// <summary>
    /// Lightweight snapshot of one DB change, queued by AuditInterceptor
    /// and consumed by RecordOfChangeProcessorService.
    /// </summary>
    public sealed record PendingAuditEntry(
        string EntityName,
        int EntityId,
        int? ClubId,
        string? OldValue,
        string? NewValue,
        DateTime ChangedAt,
        Guid ChangedBy,
        string ChangeType   // "CREATE" | "UPDATE" | "DELETE"
    );
}
