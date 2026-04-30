namespace BusinessLogic.DTOs
{
    public class RecordOfChangeResponseDto
    {
        public int Id { get; set; }
        public string EntityName { get; set; } = null!;
        public int EntityId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime ChangedAt { get; set; }
        public Guid ChangedBy { get; set; }
        public string ChangedByName { get; set; } = null!;
        public string ChangeType { get; set; } = null!;
        public string Notification { get; set; } = null!;
        public int? ClubId { get; set; }
        public string? ClubName { get; set; }
    }

    public class RecordOfChangeFilterDto
    {
        public string? Search { get; set; }
        public string? EntityName { get; set; }
        public string? ChangeType { get; set; }
        public int? ClubId { get; set; }
        public Guid? ChangedBy { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? OldValueSearch { get; set; }
        public string? NewValueSearch { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
