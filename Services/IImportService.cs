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
    /// Downloads the content at <paramref name="url"/>, auto-detects PDF vs HTML,
    /// and imports questions using the appropriate parser.
    /// Questions are saved to the global bank; if <paramref name="testId"/> is supplied
    /// they are also mapped to that test (same flow as file-based imports).
    /// </summary>
    Task<ImportResultDto> ImportFromUrlAsync(string url, Guid? importedBy = null, Guid? testId = null, bool useOcr = false);

    /// <summary>
    /// Downloads the content at <paramref name="url"/>, extracts questions,
    /// and returns an Excel file (byte[]) in the GridAcademy import template format.
    /// No questions are saved to the database — this is a preview/export only.
    /// </summary>
    Task<(byte[] Bytes, string FileName, int QuestionCount)> ExportUrlToExcelAsync(string url, bool useOcr = false);

    /// <summary>
    /// Returns a list of all available tests (Id + Title) for the destination selector dropdown.
    /// </summary>
    Task<List<(Guid Id, string Title)>> GetTestsForDropdownAsync();
}
