using System.Security.Claims;
using ClosedXML.Excel;
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

    public ImportResultDto? Result       { get; set; }
    public string?          ActiveSource { get; set; }

    /// <summary>Destination selector: empty = Global Bank, otherwise a Test Id.</summary>
    [BindProperty] public Guid? TestId { get; set; }

    /// <summary>All available tests for the "Map to Test" dropdown.</summary>
    public List<(Guid Id, string Title)> AvailableTests { get; set; } = [];

    public async Task OnGetAsync()
    {
        AvailableTests = await _import.GetTestsForDropdownAsync();
    }

    /// <summary>Generate and stream an Excel (.xlsx) template file.</summary>
    public IActionResult OnGetExcelTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Questions");

        // ── Headers ──────────────────────────────────────────────────────────
        var headers = new[]
        {
            "SubjectName","TopicName","Subtopic","DifficultyLevel","ComplexityLevel",
            "ExamType","Marks","NegativeMarks","QuestionType","QuestionText",
            "OptionA","OptionB","OptionC","OptionD","CorrectOptions",
            "NumericalAnswer","Tags","Solution"
        };
        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3c5e");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // ── Sample rows ──────────────────────────────────────────────────────
        var rows = new object[][]
        {
            ["Physics","Mechanics","Kinematics","Medium","Medium","JEE Main","4 Marks","-1 Mark","MCQ",
             "A ball is thrown vertically upward with 20 m/s. Maximum height? (g=10)","10 m","20 m","40 m","80 m","B","","Kinematics","v²=u²-2gh; h=20m"],
            ["Mathematics","Calculus","Differentiation","Hard","High","JEE Advanced","4 Marks","-2 Marks","MSQ",
             "Which functions are differentiable at x=0?","f(x)=|x|","f(x)=x|x|","f(x)=x²","f(x)=sin(x)","B,C,D","","Calculus",""],
            ["Physics","Electrostatics","Coulomb's Law","Easy","Low","JEE Main","4 Marks","-1 Mark","NAT",
             "Electric force between 1μC charges at 1m is ___ ×10⁻³ N","","","","","","9","Electrostatics","F=kq²/r²"],
            ["Biology","Cell Biology","Cell Division","Easy","Low","NEET","4 Marks","-1 Mark","TrueFalse",
             "DNA replication occurs during S phase of the cell cycle.","True","False","","","A","","Cell Division",""],
            ["Chemistry","Organic Chemistry","Hydrocarbons","Medium","Medium","JEE Main","2 Marks","No Negative","FillInBlanks",
             "The IUPAC name of CH₃-CH₂-OH is ___.","Ethanol","Methanol","Propanol","Butanol","A","","Organic Chemistry",""],
            ["Mathematics","Algebra","Matrices","Hard","High","JEE Advanced","4 Marks","-2 Marks","MSQ",
             "Which are always symmetric if A and B are symmetric?","A+B","AB","AB+BA","A-B","A,C,D","","Matrices",""],
        };

        for (int r = 0; r < rows.Length; r++)
        {
            var rowArr = (object[])rows[r];
            for (int c = 0; c < rowArr.Length; c++)
                ws.Cell(r + 2, c + 1).Value = rowArr[c]?.ToString() ?? "";
            if (r % 2 == 1)
                ws.Row(r + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8f9fa");
        }

        // ── Reference sheet ───────────────────────────────────────────────────
        var ref_ws = wb.Worksheets.Add("Reference - Valid Values");
        ref_ws.Cell("A1").Value = "QuestionType Values";     ref_ws.Cell("A1").Style.Font.Bold = true;
        ref_ws.Cell("A2").Value = "MCQ";         ref_ws.Cell("B2").Value = "Single Correct (one answer from A-D)";
        ref_ws.Cell("A3").Value = "MSQ";         ref_ws.Cell("B3").Value = "Multiple Select (one or more correct)";
        ref_ws.Cell("A4").Value = "NAT";         ref_ws.Cell("B4").Value = "Numerical Answer Type (leave options blank, fill NumericalAnswer)";
        ref_ws.Cell("A5").Value = "TrueFalse";   ref_ws.Cell("B5").Value = "True/False (OptionA=True, OptionB=False, CorrectOptions=A or B)";
        ref_ws.Cell("A6").Value = "FillInBlanks";ref_ws.Cell("B6").Value = "Fill in the blank (options are hints, CorrectOptions=letter)";
        ref_ws.Cell("A7").Value = "AssertionReason"; ref_ws.Cell("B7").Value = "Assertion and Reason MCQ style";
        ref_ws.Cell("A8").Value = "PassageBased";ref_ws.Cell("B8").Value = "Passage / comprehension based";
        ref_ws.Cell("A9").Value = "MatrixMatch"; ref_ws.Cell("B9").Value = "Matrix match question";

        ref_ws.Cell("A11").Value = "DifficultyLevel"; ref_ws.Cell("A11").Style.Font.Bold = true;
        ref_ws.Cell("A12").Value = "Easy";
        ref_ws.Cell("A13").Value = "Medium";
        ref_ws.Cell("A14").Value = "Hard";

        ref_ws.Cell("A16").Value = "ComplexityLevel"; ref_ws.Cell("A16").Style.Font.Bold = true;
        ref_ws.Cell("A17").Value = "Low";
        ref_ws.Cell("A18").Value = "Medium";
        ref_ws.Cell("A19").Value = "High";

        ref_ws.Cell("A21").Value = "ExamType"; ref_ws.Cell("A21").Style.Font.Bold = true;
        ref_ws.Cell("A22").Value = "JEE Main";
        ref_ws.Cell("A23").Value = "JEE Advanced";
        ref_ws.Cell("A24").Value = "NEET";
        ref_ws.Cell("A25").Value = "Board";

        ref_ws.Cell("A27").Value = "Marks"; ref_ws.Cell("A27").Style.Font.Bold = true;
        ref_ws.Cell("A28").Value = "1 Mark";
        ref_ws.Cell("A29").Value = "2 Marks";
        ref_ws.Cell("A30").Value = "3 Marks";
        ref_ws.Cell("A31").Value = "4 Marks";

        ref_ws.Cell("A33").Value = "NegativeMarks"; ref_ws.Cell("A33").Style.Font.Bold = true;
        ref_ws.Cell("A34").Value = "No Negative";
        ref_ws.Cell("A35").Value = "-0.25 Marks";
        ref_ws.Cell("A36").Value = "-1 Mark";
        ref_ws.Cell("A37").Value = "-2 Marks";

        ref_ws.Columns().AdjustToContents();
        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "questions-template.xlsx");
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (Guid?)null;

    public async Task<IActionResult> OnPostCsvAsync(IFormFile? file)
        => await HandleImport(file, "csv",
            stream => _import.ImportCsvAsync(stream, CurrentUserId, TestId));

    public async Task<IActionResult> OnPostExcelAsync(IFormFile? file)
        => await HandleImport(file, "excel",
            stream => _import.ImportExcelAsync(stream, CurrentUserId, TestId));

    public async Task<IActionResult> OnPostPdfAsync(IFormFile? file)
        => await HandleImport(file, "pdf",
            stream => _import.ImportPdfAsync(stream, CurrentUserId, TestId));

    public async Task<IActionResult> OnPostOcrAsync(IFormFile? file)
        => await HandleImport(file, "ocr",
            stream => _import.ImportPdfOcrAsync(stream, CurrentUserId, TestId),
            maxMb: 20);

    public async Task<IActionResult> OnPostRrbAlpAsync(IFormFile? file)
        => await HandleImport(file, "rrbalp",
            stream => _import.ImportRrbAlpAsync(stream, CurrentUserId),
            maxMb: 25);

    private async Task<IActionResult> HandleImport(
        IFormFile? file, string source,
        Func<Stream, Task<ImportResultDto>> importFn,
        int maxMb = 10)
    {
        ActiveSource   = source;
        AvailableTests = await _import.GetTestsForDropdownAsync();

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

        // Validate: if TestId was intended but not provided, block the upload
        // (the UI enforces this with JS, but defend server-side too)
        // TestId == null means "Global Bank" — always valid.

        try
        {
            await using var stream = file.OpenReadStream();
            Result = await importFn(stream);

            if (Result.Imported > 0)
            {
                TempData["Success"] = Result.MappedTestName is not null
                    ? $"{Result.Imported} question(s) successfully added to \"{Result.MappedTestName}\"."
                    : $"{Result.Imported} question(s) imported to the global question bank.";
            }
            else
            {
                TempData["Error"] = "No questions were imported. Check the errors below.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Import failed: {ex.Message}";
        }

        return Page();
    }
}
