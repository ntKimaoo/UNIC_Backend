using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UNIC.Presentation.Helpers
{
    /// <summary>
    /// Serializes DateTime values with Vietnam timezone offset (+07:00).
    /// 
    /// The system stores all dates in Vietnam local time (UTC+7) in the database.
    /// SQL Server returns these as DateTimeKind.Unspecified.
    /// 
    /// Write: stamps with '+07:00' so browsers display correctly.
    /// Read:  converts incoming UTC ('Z') dates to Vietnam local time,
    ///        so service-layer comparisons with VnTimeHelper.Now are consistent.
    /// </summary>
    public class DateTimeUtcJsonConverter : JsonConverter<DateTime>
    {
        private static readonly TimeZoneInfo VnTz =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (DateTime.TryParse(str, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            {
                // Frontend sends .toISOString() → UTC ('Z').
                // Convert to Vietnam local time so comparisons with VnTimeHelper.Now work.
                if (dt.Kind == DateTimeKind.Utc)
                    return TimeZoneInfo.ConvertTimeFromUtc(dt, VnTz);
                // If it already has an offset (+07:00), parse strips it → Local kind.
                // The numeric value is already correct (Vietnam time).
                return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
            }
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // DB stores Vietnam local time (UTC+7) as Unspecified kind.
            // Stamp with +07:00 offset so browsers display the correct time.
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fff+07:00"));
        }
    }

    /// <summary>
    /// Same as DateTimeUtcJsonConverter but for nullable DateTime.
    /// </summary>
    public class NullableDateTimeUtcJsonConverter : JsonConverter<DateTime?>
    {
        private static readonly TimeZoneInfo VnTz =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            var str = reader.GetString();
            if (DateTime.TryParse(str, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            {
                if (dt.Kind == DateTimeKind.Utc)
                    return TimeZoneInfo.ConvertTimeFromUtc(dt, VnTz);
                return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
            }
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value == null) { writer.WriteNullValue(); return; }
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff+07:00"));
        }
    }
}
