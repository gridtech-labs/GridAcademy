using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.VideoLearning;

public class VlSalesChannel
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? BaseUrl { get; set; }

    [Required, MaxLength(64)]
    public string ApiKeyHash { get; set; } = "";  // SHA-256 hex, never plain text

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<VlChannelProgramPrice> ChannelProgramPrices { get; set; } = [];
    public ICollection<VlEnrollment>          Enrollments          { get; set; } = [];
}
