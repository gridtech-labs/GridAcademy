namespace GridAcademy.Helpers;

/// <summary>
/// Centralises the uploads root path so all pages/services write to the same location.
/// In production (Railway), set the UPLOADS_PATH env var to a mounted volume path
/// e.g. /app/uploads — then add a Railway Volume at that mount point.
/// In local dev, files go to wwwroot/uploads as before.
/// </summary>
public static class UploadHelper
{
    private static string? _root;

    /// <summary>Absolute path to the uploads root directory.</summary>
    public static string Root(IWebHostEnvironment env)
    {
        if (_root != null) return _root;

        var fromEnv = Environment.GetEnvironmentVariable("UPLOADS_PATH");
        _root = !string.IsNullOrWhiteSpace(fromEnv)
            ? fromEnv
            : Path.Combine(env.WebRootPath, "uploads");

        Directory.CreateDirectory(_root);
        return _root;
    }

    /// <summary>
    /// Saves a file under <c>uploadsRoot/subfolder/</c> and returns the public URL
    /// path starting with <c>/uploads/</c>.
    /// </summary>
    public static async Task<string> SaveAsync(
        IFormFile file,
        IWebHostEnvironment env,
        string subfolder,
        string[]? allowedExtensions = null)
    {
        allowedExtensions ??= [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"];

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed.");

        var dir = Path.Combine(Root(env), subfolder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(dir, fileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        // Always return a URL relative to /uploads/ — the static-file middleware
        // maps /uploads/* → uploads root regardless of physical location.
        return $"/uploads/{subfolder}/{fileName}";
    }
}
