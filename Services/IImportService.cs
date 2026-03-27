using GridAcademy.DTOs.Content.Import;

namespace GridAcademy.Services;

public interface IImportService
{
    Task<ImportResultDto> ImportCsvAsync(Stream stream, Guid? importedBy = null);
    Task<ImportResultDto> ImportExcelAsync(Stream stream, Guid? importedBy = null);
    Task<ImportResultDto> ImportPdfAsync(Stream stream, Guid? importedBy = null);

    /// <summary>
    /// Renders the PDF via the Mathpix API (requires configured credentials),
    /// extracts questions with LaTeX math already in place, and saves them as Draft.
    /// </summary>
    Task<ImportResultDto> ImportPdfOcrAsync(Stream stream, Guid? importedBy = null);
}
