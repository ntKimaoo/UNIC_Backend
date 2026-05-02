using System;

namespace BusinessLogic.Helpers
{
    /// <summary>
    /// Helper to get Vietnam local time (UTC+7).
    /// All event dates in the system are stored in Vietnam local time,
    /// so comparisons and timestamps should use VnNow instead of DateTime.UtcNow.
    /// </summary>
    public static class VnTimeHelper
    {
        private static readonly TimeZoneInfo VnTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        /// <summary>
        /// Returns the current time in Vietnam (UTC+7).
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VnTimeZone);
    }
}
