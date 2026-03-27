using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.Marketplace;

/// <summary>Admin-managed homepage hero banners.</summary>
public class MpCmsBanner
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [MaxLength(500)]
    public string? LinkUrl { get; set; }

    [MaxLength(500)]
    public string? SubTitle { get; set; }

    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
