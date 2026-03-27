namespace GridAcademy.DTOs.Content.Import;

public class ImportRowError
{
    public int    Row     { get; set; }
    public string Field   { get; set; } = "";
    public string Message { get; set; } = "";
}

public class ImportResultDto
{
    public int                  TotalRows    { get; set; }
    public int                  Imported     { get; set; }
    public int                  Skipped      { get; set; }
    public List<ImportRowError> Errors       { get; set; } = [];
    public string               Source       { get; set; } = ""; // "CSV" | "Excel" | "PDF"
}
