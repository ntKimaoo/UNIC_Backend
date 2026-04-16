using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models;

public class FundType
{
    [Key]
    public int FundTypeId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}

