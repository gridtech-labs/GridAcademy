using System.Security.Claims;
using GridAcademy.Common;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers;

/// <summary>Bulk question import via CSV, Excel, or PDF.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
public class ImportController : ControllerBase
{
    private readonly IImportService      _svc;
    private readonly IAiPdfImportService _aiPdf;

    public ImportController(IImportService svc, IAiPdfImportService aiPdf)
    {
        _svc   = svc;
        _aiPdf = aiPdf;
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    /// <summary>Import questions from a CSV file.</summary>
    [HttpPost("csv")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportCsv(IFormFile file, [FromForm] Guid? testId = null)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResponse.Fail("No file provided."));
        if (file.Length > MaxFileSizeBytes)   return BadRequest(ApiResponse.Fail("File exceeds 10 MB limit."));

        using var stream = file.OpenReadStream();
        var result = await _svc.ImportCsvAsync(stream, CurrentUserId, testId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>Import questions from an Excel (.xlsx) file.</summary>
    [HttpPost("excel")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportExcel(IFormFile file, [FromForm] Guid? testId = null)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResponse.Fail("No file provided."));
        if (file.Length > MaxFileSizeBytes)   return BadRequest(ApiResponse.Fail("File exceeds 10 MB limit."));

        using var stream = file.OpenReadStream();
        var result = await _svc.ImportExcelAsync(stream, CurrentUserId, testId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>
    /// Import questions from a chapter-wise question-bank PDF, read by the LLM itself
    /// (maths and figures included, answer key applied). Saved as Draft unless
    /// <paramref name="publishImmediately"/> is set, so nothing reaches students unreviewed.
    /// Scriptable for bulk chapter imports.
    /// </summary>
    [HttpPost("ai-pdf")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportAiPdf(
        IFormFile file,
        [FromForm] int subjectId,
        [FromForm] int topicId,
        [FromForm] Guid? testId = null,
        [FromForm] bool publishImmediately = false,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResponse.Fail("No file provided."));
        if (file.Length > 20 * 1024 * 1024)   return BadRequest(ApiResponse.Fail("File exceeds 20 MB limit."));

        using var stream = file.OpenReadStream();
        var result = await _aiPdf.ImportAsync(stream, file.FileName,
            new AiPdfImportOptions(subjectId, topicId, testId, CurrentUserId, publishImmediately), ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>
    /// Dry run for a PYQ book: reports the chapters found, their page ranges and how many answers
    /// were read from the answer key — without importing anything or calling the model. Run this
    /// before a full import to confirm the book was understood.
    /// </summary>
    [HttpPost("pyq-preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(300 * 1024 * 1024)]
    public async Task<IActionResult> PyqPreview(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResponse.Fail("No file provided."));

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var info = Services.PyqBook.PyqBookStructure.Read(ms.ToArray());

        var summary = info.Chapters.Select(c => new
        {
            c.Number,
            c.Title,
            Answers    = c.Answers.Count,
            HighestQNo = c.Answers.Count > 0 ? c.Answers.Keys.Max() : 0,
            // Missing numbers below the highest: a sign the key was misread for that chapter.
            Gaps       = c.Answers.Count > 0
                ? Enumerable.Range(1, c.Answers.Keys.Max()).Count(q => !c.Answers.ContainsKey(q))
                : 0,
        }).ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            info.PageCount,
            info.PagesWithText,
            info.AnswerKeyStartPage,
            QuestionPages = info.AnswerKeyStartPage - 1,
            Chapters      = summary.Count,
            TotalAnswers  = summary.Sum(s => s.Answers),
            TotalGaps     = summary.Sum(s => s.Gaps),
            Detail        = summary,
        }));
    }

    /// <summary>Import questions by parsing a JEE/NEET-pattern PDF.</summary>
    [HttpPost("pdf")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportPdf(IFormFile file, [FromForm] Guid? testId = null)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResponse.Fail("No file provided."));
        if (file.Length > MaxFileSizeBytes)   return BadRequest(ApiResponse.Fail("File exceeds 10 MB limit."));

        using var stream = file.OpenReadStream();
        var result = await _svc.ImportPdfAsync(stream, CurrentUserId, testId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>
    /// Import questions from a PDF using Mathpix OCR.
    /// Best for math-heavy papers (JEE/NEET) where standard PDF text extraction
    /// produces garbled symbols. Requires Mathpix:AppId and Mathpix:AppKey in appsettings.json.
    /// </summary>
    [HttpPost("pdf-ocr")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportPdfOcr(IFormFile file, [FromForm] Guid? testId = null)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResponse.Fail("No file provided."));
        if (file.Length > MaxFileSizeBytes)   return BadRequest(ApiResponse.Fail("File exceeds 10 MB limit."));

        using var stream = file.OpenReadStream();
        var result = await _svc.ImportPdfOcrAsync(stream, CurrentUserId, testId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>Download the CSV template file.</summary>
    [HttpGet("template/csv")]
    public IActionResult DownloadCsvTemplate()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "samples", "questions-template.csv");
        if (!System.IO.File.Exists(path)) return NotFound(ApiResponse.Fail("Template file not found."));
        return PhysicalFile(path, "text/csv", "questions-template.csv");
    }
}
