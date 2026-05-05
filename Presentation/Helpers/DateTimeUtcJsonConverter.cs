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
    /// Previously, this converter stamped them with 'Z' (UTC), causing browsers
    /// in UTC+7 to add another +7 hours (double offset).
    /// 
    /// Now it stamps with '+07:00' so the browser knows the value is already
    /// in Vietnam time and displays it correctly.
    /// </summary>
    public class DateTimeUtcJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (DateTime.TryParse(str, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt;
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
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            var str = reader.GetString();
            if (DateTime.TryParse(str, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
                return dt;
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value == null) { writer.WriteNullValue(); return; }
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fff+07:00"));
        }
    }
}
