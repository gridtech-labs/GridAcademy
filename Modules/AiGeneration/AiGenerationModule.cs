using GridAcademy.Modules.AiGeneration.Infrastructure.Embeddings;
using GridAcademy.Modules.AiGeneration.Infrastructure.Llm;
using GridAcademy.Modules.AiGeneration.Jobs;
using GridAcademy.Modules.AiGeneration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GridAcademy.Modules.AiGeneration;

/// <summary>
/// Registers all AI Generation services into the DI container.
/// Call services.AddAiGenerationModule(cfg) from Program.cs.
/// </summary>
public static class AiGenerationModule
{
    public static IServiceCollection AddAiGenerationModule(
        this IServiceCollection services,
        IConfiguration          cfg)
    {
        // ── HTTP clients ─────────────────────────────────────────────────────
        // Separate named HttpClient per provider so timeouts/retry policies differ.
        // Generation can take a few minutes for larger batches (a 32k-token response
        // streams slowly), so give a generous timeout. Configurable via
        // Ai:Gemini:TimeoutSeconds (Railway: Ai__Gemini__TimeoutSeconds).
        var geminiTimeout = TimeSpan.FromSeconds(
            int.TryParse(cfg["Ai:Gemini:TimeoutSeconds"], out var gts) && gts > 0 ? gts : 300);

        services.AddHttpClient<GeminiLlmProvider>(c => c.Timeout = geminiTimeout);

        services.AddHttpClient<AnthropicLlmProvider>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(300);
        });

        services.AddHttpClient<GeminiEmbeddingsProvider>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(30);
        });

        // ── LLM provider (swap by config) ────────────────────────────────────
        // NOTE: resolve the concrete provider through the container so it gets its
        // typed HttpClient (with the configured timeout). A plain
        // AddTransient<ILLMProvider, GeminiLlmProvider> would activate a SECOND
        // instance with a default 100s HttpClient, ignoring the timeout above.
        var llmProviderName = cfg["Ai:LlmProvider"] ?? "gemini";
        if (llmProviderName.Equals("anthropic", StringComparison.OrdinalIgnoreCase))
        {
            services.AddTransient<ILLMProvider>(sp => sp.GetRequiredService<AnthropicLlmProvider>());
        }
        else
        {
            // Default: Gemini
            services.AddTransient<ILLMProvider>(sp => sp.GetRequiredService<GeminiLlmProvider>());
        }

        // ── Embeddings provider ───────────────────────────────────────────────
        services.AddTransient<IEmbeddingsProvider, GeminiEmbeddingsProvider>();

        // ── Core services ────────────────────────────────────────────────────
        services.AddTransient<LlmUsageTracker>();
        services.AddTransient<PromptBuilder>();
        services.AddTransient<SelfVerifier>();
        services.AddTransient<MathRecomputer>();
        services.AddTransient<DuplicateDetector>();
        services.AddTransient<DraftConverter>();
        services.AddTransient<GenerationService>();
        services.AddTransient<ReviewService>();

        // ── Hangfire jobs ────────────────────────────────────────────────────
        services.AddTransient<GenerationWorkerJob>();
        services.AddTransient<OrphanedJobCleanerJob>();

        return services;
    }
}
