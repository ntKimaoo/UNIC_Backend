using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UNIC.Presentation.Helpers
{
    /// <summary>
    /// Ensures DateTime values are always serialized as UTC ISO-8601 with 'Z' suffix.
    /// Without this, .NET serializes "Unspecified" DateTimes without any timezone indicator,
    /// causing browsers in UTC+7 to display them 7 hours off (e.g. 01:00 instead of 08:00).
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
            // Data is stored as Vietnam time (UTC+7). Emit +07:00 so browsers display the correct local time.
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
