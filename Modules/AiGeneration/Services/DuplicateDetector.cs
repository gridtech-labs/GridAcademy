using GridAcademy.Data;
using GridAcademy.Modules.AiGeneration.Infrastructure.Embeddings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GridAcademy.Modules.AiGeneration.Services;

/// <summary>
/// Detects near-duplicate questions using pgvector cosine similarity.
/// Uses the `<=>` operator (cosine distance) on the question_embeddings table.
/// Degraded gracefully when pgvector is not installed or key is missing.
/// </summary>
public sealed class DuplicateDetector
{
    private readonly AppDbContext          _db;
    private readonly IEmbeddingsProvider   _embed;
    private readonly double                _threshold;
    private readonly ILogger<DuplicateDetector> _log;

    public DuplicateDetector(
        AppDbContext        db,
        IEmbeddingsProvider embed,
        IConfiguration      cfg,
        ILogger<DuplicateDetector> log)
    {
        _db        = db;
        _embed     = embed;
        _log       = log;
        _threshold = cfg.GetValue<double>("Ai:Generation:DuplicateCosineThreshold", 0.92);
    }

    /// <summary>
    /// Embeds the question text, then queries question_embeddings for the nearest
    /// neighbour. Returns (matched_id, score) when a duplicate is found above threshold,
    /// or null when nothing suspicious is found (or when embeddings are unavailable).
    /// </summary>
    public async Task<(Guid MatchedId, double Score)?> FindDuplicateAsync(
        string questionText,
        CancellationToken ct = default)
    {
        // 1. Get embedding
        float[]? vector;
        try
        {
            vector = await _embed.EmbedAsync(questionText, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Embeddings call failed — duplicate check skipped.");
            return null;
        }

        if (vector is null) return null;

        // 2. Format vector as Postgres literal, e.g. '[0.1,0.2,...]'
        var literal = "[" + string.Join(",", vector.Select(v => v.ToString("G6"))) + "]";

        // 3. Raw SQL cosine distance query (<=> = cosine distance; 1 - distance = similarity)
        // We look for rows where similarity ≥ threshold, i.e. distance ≤ (1 - threshold).
        var distanceMax = 1.0 - _threshold;

        try
        {
            var conn = _db.Database.GetDbConnection();
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                SELECT source_id,
                       1 - (embedding <=> '{literal}'::vector) AS similarity
                FROM   question_embeddings
                WHERE  embedding <=> '{literal}'::vector <= {distanceMax.ToString("G6")}
                ORDER  BY embedding <=> '{literal}'::vector
                LIMIT  1
                """;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                var matchedId = reader.GetGuid(0);
                var score     = reader.GetDouble(1);
                return (matchedId, score);
            }
        }
        catch (Exception ex)
        {
            // pgvector not installed, table doesn't exist, etc — degrade gracefully
            _log.LogDebug(ex, "DuplicateDetector DB query failed — duplicate check skipped.");
        }

        return null;
    }

    /// <summary>
    /// Stores the embedding for a newly-approved question or pending draft so future
    /// duplicate checks see it immediately.
    /// </summary>
    public async Task UpsertEmbeddingAsync(
        Guid   sourceId,
        int    sourceType,   // 1 = live question, 2 = draft
        string questionText,
        CancellationToken ct = default)
    {
        float[]? vector;
        try
        {
            vector = await _embed.EmbedAsync(questionText, ct);
        }
        catch { return; }

        if (vector is null) return;

        var literal = "[" + string.Join(",", vector.Select(v => v.ToString("G6"))) + "]";

        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                INSERT INTO question_embeddings (source_id, source, embedding, created_at)
                VALUES ('{sourceId}', {sourceType}, '{literal}'::vector, now())
                ON CONFLICT DO NOTHING
                """;
            await cmd.ExecuteNonQueryAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Failed to upsert question embedding for {Id}.", sourceId);
        }
    }
}
