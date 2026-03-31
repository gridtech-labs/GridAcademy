using GridAcademy.DTOs.Content.Import;

namespace GridAcademy.Services;

public interface IImportService
{
    Task<ImportResultDto> ImportCsvAsync(Stream stream, Guid? importedBy = null, Guid? testId = null);
    Task<ImportResultDto> ImportExcelAsync(Stream stream, Guid? importedBy = null, Guid? testId = null);
    Task<ImportResultDto> ImportPdfAsync(Stream stream, Guid? importedBy = null, Guid? testId = null);

    /// <summary>
    /// Renders the PDF via the Mathpix API (requires configured credentials),
    /// extracts questions with LaTeX math already in place, and saves them as Draft.
    /// </summary>
    Task<ImportResultDto> ImportPdfOcrAsync(Stream stream, Guid? importedBy = null, Guid? testId = null);

    /// <summary>
    /// Imports questions and creates test papers from the GridAcademy RRB ALP
    /// Excel format (columns: QID, Question, Option A-D, Correct Answer, Subject,
    /// Topic, Difficulty, Marks, Negative Marks, Language).
    /// Processes Master_Question_Bank sheet for questions and CBT1_Paper,
    /// CBT2_Paper, Mock_Test_1..10 sheets to auto-create Test papers.
    /// </summary>
    Task<ImportResultDto> ImportRrbAlpAsync(Stream stream, Guid? importedBy = null);

    /// <summary>
    /// Returns a list of all available tests (Id + Title) for the destination selector dropdown.
    /// </summary>
    Task<List<(Guid Id, string Title)>> GetTestsForDropdownAsync();
}
