using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using DataAccess.Interceptors;
using DataAccess.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Presentation.Interceptors
{
    /// <summary>
    /// EF Core singleton interceptor. Fires on every SaveChanges against UnicContext,
    /// captures old/new scalar values for audited entities, then enqueues a
    /// PendingAuditEntry to AuditChannel for async persistence.
    /// </summary>
    public sealed class AuditInterceptor : SaveChangesInterceptor
    {
        // Entity types (by EF short name) that should be audited.
        private static readonly HashSet<string> AuditedNames = new(StringComparer.Ordinal)
        {
             "Club", "Department",
            "UserClubRole", "ClubRole",
            "RecruitmentCampaign",
            "UserClubRoleAssignment"
        };

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AuditChannel _auditChannel;

        // Per-save state keyed by the DbContext instance (weak reference → no leak).
        private readonly ConditionalWeakTable<DbContext, List<PreSaveSnapshot>> _pending = new();

        public AuditInterceptor(IHttpContextAccessor httpContextAccessor, AuditChannel auditChannel)
        {
            _httpContextAccessor = httpContextAccessor;
            _auditChannel = auditChannel;
        }

        // ── Before save: capture state while OriginalValues is still pristine ──

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Capture(eventData.Context);
            return new(result);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            Capture(eventData.Context);
            return result;
        }

        private void Capture(DbContext? context)
        {
            if (context is null) return;

            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;
            var snapshots = new List<PreSaveSnapshot>();

            foreach (var entry in context.ChangeTracker.Entries())
            {
                // Prevent infinite loop when the processor saves RecordOfChange rows.
                if (entry.Entity is RecordOfChange) continue;

                var entityName = entry.Metadata.ShortName();
                if (!AuditedNames.Contains(entityName)) continue;
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

                var snap = new PreSaveSnapshot
                {
                    State = entry.State,
                    EntityName = entityName,
                    ChangedBy = userId,
                    ChangedAt = now,
                    EntryRef = entry
                };

                if (entry.State == EntityState.Modified)
                {
                    // Changed properties only — compare OriginalValues vs CurrentValues.
                    var changedProps = entry.Properties
                        .Where(p => !p.Metadata.IsPrimaryKey()
                                    && !Equals(p.OriginalValue, p.CurrentValue))
                        .Select(p => p.Metadata.Name)
                        .ToList();

                    if (changedProps.Count == 0) continue;

                    // Detect soft-delete: IsDeleted flipped false → true
                    var isDeletedProp = entry.Properties
                        .FirstOrDefault(p => p.Metadata.Name == "IsDeleted");
                    if (isDeletedProp != null
                        && isDeletedProp.OriginalValue is false
                        && isDeletedProp.CurrentValue is true)
                    {
                        snap.IsSoftDelete = true;
                    }

                    snap.ChangedPropertyNames = changedProps;
                    snap.EntityId = GetEntityId(entry);
                    snap.ClubId = TryGetClubId(entry);
                    snap.OldValueJson = SerializePropertyValues(entry, changedProps, useOriginal: true);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    snap.EntityId = GetEntityId(entry);
                    snap.ClubId = TryGetClubId(entry);
                    snap.OldValueJson = SerializeAllScalars(entry.OriginalValues);
                }
                // Added: EntityId is 0 until after save (DB-generated PK); we fill it in SavedChanges.

                snapshots.Add(snap);
            }

            if (snapshots.Count > 0)
            {
                // Remove any stale entry for this context before adding.
                _pending.Remove(context);
                _pending.Add(context, snapshots);
            }
        }

        // ── After save: PK is now populated for Added entities ──

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            Enqueue(eventData.Context);
            return new(result);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            Enqueue(eventData.Context);
            return result;
        }

        private void Enqueue(DbContext? context)
        {
            if (context is null) return;
            if (!_pending.TryGetValue(context, out var snapshots)) return;
            _pending.Remove(context);

            foreach (var snap in snapshots)
            {
                PendingAuditEntry entry;

                switch (snap.State)
                {
                    case EntityState.Added:
                        entry = new PendingAuditEntry(
                            snap.EntityName,
                            EntityId: GetEntityId(snap.EntryRef!),
                            ClubId: TryGetClubId(snap.EntryRef!),
                            OldValue: null,
                            NewValue: SerializeAllScalars(snap.EntryRef!.CurrentValues),
                            snap.ChangedAt,
                            snap.ChangedBy,
                            ChangeType: "CREATE");
                        break;

                    case EntityState.Modified:
                        entry = new PendingAuditEntry(
                            snap.EntityName,
                            snap.EntityId,
                            snap.ClubId,
                            OldValue: snap.OldValueJson,
                            NewValue: snap.IsSoftDelete ? null : SerializePropertyValues(snap.EntryRef!, snap.ChangedPropertyNames!, useOriginal: false),
                            snap.ChangedAt,
                            snap.ChangedBy,
                            ChangeType: snap.IsSoftDelete ? "SOFT DELETE" : "UPDATE");
                        break;

                    case EntityState.Deleted:
                        entry = new PendingAuditEntry(
                            snap.EntityName,
                            snap.EntityId,
                            snap.ClubId,
                            OldValue: snap.OldValueJson,
                            NewValue: null,
                            snap.ChangedAt,
                            snap.ChangedBy,
                            ChangeType: "DELETE");
                        break;

                    default:
                        continue;
                }

                _auditChannel.Writer.TryWrite(entry);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var claim = _httpContextAccessor.HttpContext?.User
                .FindFirst("UserId")
                ?? _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier);

            return claim != null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
        }

        private static int GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            var pk = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
            if (pk == null) return 0;
            return entry.CurrentValues[pk] is int i ? i : 0;
        }

        private static int? TryGetClubId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
        {
            var prop = entry.Metadata.GetProperties().FirstOrDefault(p => p.Name == "ClubId");
            if (prop == null) return null;
            return entry.CurrentValues[prop] as int?;
        }

        private static string SerializePropertyValues(
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
            List<string> propertyNames,
            bool useOriginal)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var name in propertyNames)
            {
                var prop = entry.Property(name);
                dict[name] = useOriginal ? prop.OriginalValue : prop.CurrentValue;
            }
            return JsonSerializer.Serialize(dict);
        }

        private static string SerializeAllScalars(Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues pv)
        {
            var dict = pv.Properties
                .Where(p => !p.IsShadowProperty())
                .ToDictionary(p => p.Name, p => pv[p]);
            return JsonSerializer.Serialize(dict);
        }

        // ── Inner state ──────────────────────────────────────────────

        private sealed class PreSaveSnapshot
        {
            public EntityState State { get; set; }
            public string EntityName { get; set; } = string.Empty;
            public int EntityId { get; set; }
            public int? ClubId { get; set; }
            public string? OldValueJson { get; set; }
            public List<string>? ChangedPropertyNames { get; set; }
            public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry? EntryRef { get; set; }
            public Guid ChangedBy { get; set; }
            public DateTime ChangedAt { get; set; }
            public bool IsSoftDelete { get; set; }
        }
    }
}
