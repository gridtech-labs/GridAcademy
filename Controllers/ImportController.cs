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
    private readonly IImportService _svc;
    public ImportController(IImportService svc) => _svc = svc;

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    /// <summary>Import questions from a CSV file.</summary>
    [HttpPost("csv")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportCsv(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResponse.Fail("No file provided."));
        if (file.Length > MaxFileSizeBytes)   return BadRequest(ApiResponse.Fail("File exceeds 10 MB limit."));

        using var stream = file.OpenReadStream();
        var result = await _svc.ImportCsvAsync(stream, CurrentUserId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>Import questions from an Excel (.xlsx) file.</summary>
    [HttpPost("excel")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResponse.Fail("No file provided."));
        if (file.Length > MaxFileSizeBytes)   return BadRequest(ApiResponse.Fail("File exceeds 10 MB limit."));

        using var stream = file.OpenReadStream();
        var result = await _svc.ImportExcelAsync(stream, CurrentUserId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>Import questions by parsing a JEE/NEET-pattern PDF.</summary>
    [HttpPost("pdf")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportPdf(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResponse.Fail("No file provided."));
        if (file.Length > MaxFileSizeBytes)   return BadRequest(ApiResponse.Fail("File exceeds 10 MB limit."));

        using var stream = file.OpenReadStream();
        var result = await _svc.ImportPdfAsync(stream, CurrentUserId);
        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>
    /// Import questions from a PDF using Mathpix OCR.
    /// Best for math-heavy papers (JEE/NEET) where standard PDF text extraction
    /// produces garbled symbols. Requires Mathpix:AppId and Mathpix:AppKey in appsettings.json.
    /// </summary>
    [HttpPost("pdf-ocr")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportPdfOcr(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(ApiResponse.Fail("No file provided."));
        if (file.Length > MaxFileSizeBytes)   return BadRequest(ApiResponse.Fail("File exceeds 10 MB limit."));

        using var stream = file.OpenReadStream();
        var result = await _svc.ImportPdfOcrAsync(stream, CurrentUserId);
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
