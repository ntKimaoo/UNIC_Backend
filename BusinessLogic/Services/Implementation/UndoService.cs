using BusinessLogic.Services.Interface;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BusinessLogic.Services.Implementation
{
    public class UndoService : IUndoService
    {
        private readonly UnicContext _context;

        public UndoService(UnicContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message)> UndoAsync(int recordOfChangeId)
        {
            var record = await _context.RecordsOfChange
                .FirstOrDefaultAsync(r => r.Id == recordOfChangeId);

            if (record == null)
                return (false, "Record of change not found.");

            if (record.IsUndo)
                return (false, "This record has already been undone.");

            var result = record.ChangeType switch
            {
                "UPDATE" => await UndoUpdateAsync(record),
                "DELETE" => (false, "Undo DELETE is not supported."),
                "CREATE" => (false, "Undo CREATE is not supported."),
                "SOFT DELETE" => await UndoSoftDeleteAsync(record),
                _ => (false, $"Unsupported change type: {record.ChangeType}")
            };

            if (result.Item1)
            {
                record.IsUndo = true;
                await _context.SaveChangesAsync();
            }

            return result;
        }

        private async Task<(bool, string)> UndoUpdateAsync(RecordOfChange record)
        {
            if (string.IsNullOrEmpty(record.OldValue))
                return (false, "No old value stored for this record.");

            Dictionary<string, JsonElement>? oldValues;
            try
            {
                oldValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(record.OldValue);
            }
            catch
            {
                return (false, "Failed to parse old values from record.");
            }

            if (oldValues == null || oldValues.Count == 0)
                return (false, "Old values are empty.");

            var entity = await FindEntityAsync(record.EntityName, record.EntityId);
            if (entity == null)
                return (false, $"Entity '{record.EntityName}' with ID {record.EntityId} not found.");

            var entry = _context.Entry(entity);
            var skipped = new List<string>();

            foreach (var (propName, jsonValue) in oldValues)
            {
                var prop = entry.Metadata.FindProperty(propName);
                if (prop == null) { skipped.Add(propName); continue; }

                try
                {
                    entry.Property(propName).CurrentValue = ConvertJsonElement(jsonValue, prop.ClrType);
                }
                catch
                {
                    skipped.Add(propName);
                }
            }

            await _context.SaveChangesAsync();

            var msg = $"Reverted UPDATE on {record.EntityName} #{record.EntityId}.";
            if (skipped.Count > 0)
                msg += $" Skipped properties: {string.Join(", ", skipped)}.";

            return (true, msg);
        }

        private static readonly HashSet<string> SoftDeleteEntities = ["Department", "ClubRole"];

        private async Task<(bool, string)> UndoSoftDeleteAsync(RecordOfChange record)
        {
            if (!SoftDeleteEntities.Contains(record.EntityName))
                return (false, $"Cannot undo hard-deleted '{record.EntityName}'. Only soft-deleted entities (Department, ClubRole) can be restored.");

            var entity = await FindEntityAsync(record.EntityName, record.EntityId);
            if (entity == null)
                return (false, $"{record.EntityName} #{record.EntityId} not found.");

            var entry = _context.Entry(entity);
            var prop = entry.Metadata.FindProperty("IsDeleted");
            if (prop == null)
                return (false, "Entity does not support soft delete.");

            entry.Property("IsDeleted").CurrentValue = false;
            await _context.SaveChangesAsync();
            return (true, $"Restored {record.EntityName} #{record.EntityId}.");
        }

        // ── Entity lookup by name ─────────────────────────────────────────────

        private async Task<object?> FindEntityAsync(string entityName, int entityId)
        {
            return entityName switch
            {
                "Club"                   => await _context.Clubs.FindAsync(entityId),
                "ClubRole"               => await _context.ClubRoles.FindAsync(entityId),
                "Department"             => await _context.Departments.FindAsync(entityId),
                "Event"                  => await _context.Events.FindAsync(entityId),
                "ClubFund"               => await _context.ClubFunds.FindAsync(entityId),
                "FundTransaction"        => await _context.FundTransactions.FindAsync(entityId),
                "UserClubRole"           => await _context.UserClubRoles.FindAsync(entityId),
                "RecruitmentCampaign"    => await _context.RecruitmentCampaigns.FindAsync(entityId),
                _ => null
            };
        }

        // ── JsonElement → CLR type conversion ────────────────────────────────

        private static object? ConvertJsonElement(JsonElement element, Type targetType)
        {
            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (element.ValueKind == JsonValueKind.Null)
                return null;

            if (underlying == typeof(string))        return element.GetString();
            if (underlying == typeof(int))           return element.GetInt32();
            if (underlying == typeof(long))          return element.GetInt64();
            if (underlying == typeof(short))         return element.GetInt16();
            if (underlying == typeof(bool))          return element.GetBoolean();
            if (underlying == typeof(decimal))       return element.GetDecimal();
            if (underlying == typeof(double))        return element.GetDouble();
            if (underlying == typeof(float))         return (float)element.GetDouble();
            if (underlying == typeof(Guid))          return element.GetGuid();
            if (underlying == typeof(DateTime))      return element.GetDateTime();
            if (underlying == typeof(DateTimeOffset)) return element.GetDateTimeOffset();

            // Fallback: full deserialization
            return JsonSerializer.Deserialize(element.GetRawText(), targetType);
        }
    }
}
