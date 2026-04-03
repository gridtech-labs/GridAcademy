using GridAcademy.Data;
using GridAcademy.Data.Entities.Exam;
using GridAcademy.DTOs.ExamContent;
using GridAcademy.Services.ExamContent.AI;
using GridAcademy.Services.ExamContent.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GridAcademy.Controllers;

[ApiController]
[Route("api/ai")]
public class AIRewriteController(
    AppDbContext db,
    IAIRewriteService rewriteService,
    IOptions<AiRewriteOptions> options,
    ILogger<AIRewriteController> logger) : ControllerBase
{
    private readonly AiRewriteOptions _options = options.Value;

    [HttpPost("rewrite")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Instructor")]
    public async Task<IActionResult> RewriteDrafts([FromBody] AiRewriteRequest? request, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE exam_notifications
                ADD COLUMN IF NOT EXISTS is_ai_processed boolean NOT NULL DEFAULT FALSE,
                ADD COLUMN IF NOT EXISTS ai_processed_at timestamptz;
            """, ct);

        var requestedBatch = request?.BatchSize ?? _options.DefaultBatchSize;
        var batchSize = Math.Clamp(requestedBatch, 1, _options.MaxBatchSize);

        logger.LogInformation("AI rewrite batch started. RequestedBatch={RequestedBatch}, EffectiveBatch={BatchSize}", requestedBatch, batchSize);

        var drafts = await db.ExamNotifications
            .Where(x => x.Status == PublicationStatus.Draft && !x.IsAiProcessed)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        var processed = 0;
        var failed = 0;

        foreach (var notification in drafts)
        {
            var success = false;

            for (var attempt = 1; attempt <= _options.MaxRetries + 1 && !success; attempt++)
            {
                try
                {
                    logger.LogInformation("AI rewrite attempt {Attempt} for notification {Title}", attempt, notification.Title);
                    var rewritten = await rewriteService.RewriteAsync(notification.ContentHtml, notification.Title, ct);

                    notification.ContentHtml = rewritten.ContentHtml;
                    notification.MetaTitle = rewritten.MetaTitle;
                    notification.MetaDescription = rewritten.MetaDescription;
                    notification.IsAiProcessed = true;
                    notification.AiProcessedAt = DateTime.UtcNow;
                    notification.UpdatedAt = DateTime.UtcNow;

                    await db.SaveChangesAsync(ct);

                    processed++;
                    success = true;
                    logger.LogInformation("AI rewrite completed for {Title}", notification.Title);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "AI rewrite failed for {Title} on attempt {Attempt}", notification.Title, attempt);

                    if (attempt > _options.MaxRetries)
                    {
                        failed++;
                    }
                }
            }
        }

        return Ok(new AiRewriteBatchResponse(processed, failed));
    }
}
