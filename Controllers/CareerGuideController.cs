using GridAcademy.Common;
using GridAcademy.Services.CareerGuide;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GridAcademy.Controllers;

[ApiController]
[Route("api/career-guide")]
[AllowAnonymous]
public class CareerGuideController(ICareerGuideService svc) : ControllerBase
{
    /// <summary>Returns active quiz questions with options. Used by the frontend quiz.</summary>
    [HttpGet("quiz")]
    public async Task<IActionResult> GetQuiz(CancellationToken ct)
    {
        var questions = await svc.GetActiveQuestionsAsync(ct);
        return Ok(ApiResponse<object>.Ok(questions));
    }
}
