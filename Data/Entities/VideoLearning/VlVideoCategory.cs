using System.ComponentModel.DataAnnotations;

namespace GridAcademy.Data.Entities.VideoLearning;

public class VlVideoCategory
{
    public int Id { get; set; }
    public int DomainId { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;

    public VlDomain Domain { get; set; } = null!;
    public ICollection<VlVideo> Videos { get; set; } = [];
}
