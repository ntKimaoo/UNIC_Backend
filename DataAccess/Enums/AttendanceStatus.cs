namespace DataAccess.Enums
{
    public enum AttendanceStatus
    {
        REGISTERED,
        PENDING,
        WAITLIST,
        CANCELLED,
        PRESENT,
        CHECKED_IN,   // alias for PRESENT — legacy DB value
        ABSENT,
        REJECTED
    }
}
