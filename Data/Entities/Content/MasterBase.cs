namespace GridAcademy.Data.Entities.Content;

public abstract class MasterBase
{
    public int    Id        { get; set; }
    public string Name      { get; set; } = "";
    public bool   IsActive  { get; set; } = true;
    public int    SortOrder { get; set; } = 0;
}
