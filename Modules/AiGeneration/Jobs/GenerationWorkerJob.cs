using GridAcademy.Modules.AiGeneration.Services;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace GridAcademy.Modules.AiGeneration.Jobs;

/// <summary>
/// Hangfire background job that runs a GenerationJob end-to-end.
/// Enqueued by AiAdminController; picked up by Hangfire worker on the "default" queue.
/// </summary>
public sealed class GenerationWorkerJob
{
    private readonly GenerationService _service;
    private readonly ILogger<GenerationWorkerJob> _log;

    public GenerationWorkerJob(GenerationService service, ILogger<GenerationWorkerJob> log)
    {
        _service = service;
        _log     = log;
    }

    /// <summary>Entry point called by Hangfire. jobId = generation_jobs.id.</summary>
    // AutomaticRetry(0) — retries at the LLM level (GeminiLlmProvider) are sufficient.
    // Hangfire retries would re-run from scratch and duplicate already-saved drafts.
    [AutomaticRetry(Attempts = 0)]
    [Queue("default")]
    public async Task ExecuteAsync(int jobId, CancellationToken ct)
    {
        _log.LogInformation("GenerationWorkerJob started for job {JobId}.", jobId);
        await _service.RunJobAsync(jobId, ct);
        _log.LogInformation("GenerationWorkerJob finished for job {JobId}.", jobId);
    }
}
