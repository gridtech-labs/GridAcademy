namespace GridAcademy.Helpers;

/// <summary>
/// Converts relative upload paths (e.g. /uploads/exams/banner.jpg) to absolute URLs
/// so the frontend (Vercel) can load images directly from Railway without a proxy.
///
/// Set API_BASE_URL env var on Railway → https://gridacademy-production.up.railway.app
/// Leave empty in appsettings for local dev (relative paths work fine on localhost).
/// </summary>
public static class MediaUrlHelper
{
    private static string? _base;

    public static void Configure(IConfiguration config)
    {
        // Prefer env var, fall back to appsettings "ApiBaseUrl" key
        _base = Environment.GetEnvironmentVariable("API_BASE_URL")
             ?? config["ApiBaseUrl"]
             ?? string.Empty;

        // Strip trailing slash
        _base = _base.TrimEnd('/');
        Console.WriteLine($"[MediaUrl] Base URL: '{_base}' (empty = relative paths used)");
    }

    /// <summary>
    /// Returns an absolute URL if <paramref name="relativePath"/> starts with /uploads/,
    /// otherwise returns it unchanged (already absolute, or null/empty).
    /// </summary>
    public static string? Abs(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return relativePath;
        if (relativePath.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return relativePath;
        if (string.IsNullOrEmpty(_base)) return relativePath;   // local dev
        return $"{_base}{relativePath}";
    }
}
