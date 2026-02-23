namespace UNIC.BusinessLogic.Constants
{
    public static class ApplicationStatus
    {
        public const string Pending = "PENDING";
        public const string Approved = "APPROVED";
        public const string Rejected = "REJECTED";
        public const string Success = "SUCCESS";

        public static readonly IReadOnlySet<string> ValidStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Pending,
            Approved,
            Rejected,
            Success
        };

        public static bool IsValid(string? status)
        {
            return !string.IsNullOrWhiteSpace(status) && ValidStatuses.Contains(status);
        }

        public static bool CanProceedToInterview(string? status)
        {
            return string.Equals(status, Success, StringComparison.OrdinalIgnoreCase);
        }
    }
}
