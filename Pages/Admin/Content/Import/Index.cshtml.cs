using System.Security.Claims;
using GridAcademy.DTOs.Content.Import;
using GridAcademy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridAcademy.Pages.Admin.Content.Import;

[Authorize(Roles = "Admin,Instructor")]
public class IndexModel : PageModel
{
    private readonly IImportService  _import;
    private readonly IMathpixService _mathpix;

    public IndexModel(IImportService import, IMathpixService mathpix)
    {
        _import  = import;
        _mathpix = mathpix;
    }

    /// <summary>True when Mathpix credentials are present in appsettings.json.</summary>
    public bool MathpixConfigured => _mathpix.IsConfigured;

    public ImportResultDto? Result      { get; set; }
    public string?          ActiveSource { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostCsvAsync(IFormFile? file)
        => await HandleImport(file, "csv", stream =>
        {
            var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (Guid?)null;
            return _import.ImportCsvAsync(stream, userId);
        });

    public async Task<IActionResult> OnPostExcelAsync(IFormFile? file)
        => await HandleImport(file, "excel", stream =>
        {
            var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (Guid?)null;
            return _import.ImportExcelAsync(stream, userId);
        });

    public async Task<IActionResult> OnPostPdfAsync(IFormFile? file)
        => await HandleImport(file, "pdf", stream =>
        {
            var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (Guid?)null;
            return _import.ImportPdfAsync(stream, userId);
        });

    public async Task<IActionResult> OnPostOcrAsync(IFormFile? file)
        => await HandleImport(file, "ocr", stream =>
        {
            var userId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (Guid?)null;
            return _import.ImportPdfOcrAsync(stream, userId);
        }, maxMb: 20);  // larger limit — PDF OCR uploads go to Mathpix directly

    private async Task<IActionResult> HandleImport(
        IFormFile? file, string source,
        Func<Stream, Task<ImportResultDto>> importFn,
        int maxMb = 10)
    {
        ActiveSource = source;

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload.";
            return Page();
        }

        if (file.Length > maxMb * 1024 * 1024)
        {
            TempData["Error"] = $"File size must be under {maxMb} MB.";
            return Page();
        }

        try
        {
            await using var stream = file.OpenReadStream();
            Result = await importFn(stream);

            if (Result.Imported > 0)
                TempData["Success"] = $"{Result.Imported} question(s) imported successfully.";
            else
                TempData["Error"] = "No questions were imported. Check the errors below.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Import failed: {ex.Message}";
        }

        return Page();
    }
}
