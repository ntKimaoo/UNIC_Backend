namespace UNIC.BusinessLogic.Constants
{
    /// <summary>
    /// Trạng thái đơn ứng tuyển.
    /// PENDING → duyệt → APPROVED / REJECTED / SUCCESS.
    /// SUCCESS = đạt, được mời phỏng vấn.
    /// </summary>
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

        /// <summary>
        /// Trạng thái cho phép chuyển sang phỏng vấn (tạo InterviewSchedule).
        /// </summary>
        public static bool CanProceedToInterview(string? status)
        {
            return string.Equals(status, Success, StringComparison.OrdinalIgnoreCase);
        }
    }
}
