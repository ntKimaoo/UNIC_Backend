namespace BusinessLogic.DTOs;

public sealed class FundTypeDto
{
    public int FundTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

