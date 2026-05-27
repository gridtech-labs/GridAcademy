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
        services.AddHttpClient<GeminiLlmProvider>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<AnthropicLlmProvider>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(60);
        });

        services.AddHttpClient<GeminiEmbeddingsProvider>(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(30);
        });

        // ── LLM provider (swap by config) ────────────────────────────────────
        var llmProviderName = cfg["Ai:LlmProvider"] ?? "gemini";
        if (llmProviderName.Equals("anthropic", StringComparison.OrdinalIgnoreCase))
        {
            services.AddTransient<ILLMProvider, AnthropicLlmProvider>();
        }
        else
        {
            // Default: Gemini
            services.AddTransient<ILLMProvider, GeminiLlmProvider>();
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
