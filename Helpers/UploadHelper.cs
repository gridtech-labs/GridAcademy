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
    /// Elements that can execute or fetch, stripped from uploaded SVG diagrams.
    /// </summary>
    private static readonly string[] UnsafeSvgElements =
    [
        "script", "foreignobject", "iframe", "embed", "object",
        "handler", "set", "animate", "animatetransform", "animatemotion",
    ];

    /// <summary>
    /// Removes anything active from an uploaded SVG (scripts, event handlers, external
    /// references) and rewrites the file in place. SVG is XML, so a diagram exported from a
    /// drawing tool survives untouched while an SVG carrying script does not.
    /// Throws when the file is not valid XML — the caller should reject the upload.
    /// </summary>
    /// <param name="url">The URL returned by <see cref="SaveAsync"/>, e.g. /uploads/questions/x.svg</param>
    public static void SanitizeSvg(IWebHostEnvironment env, string url)
    {
        var relative = url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
            ? url["/uploads/".Length..]
            : url.TrimStart('/');
        var path = Path.Combine(Root(env), relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) return;

        System.Xml.Linq.XDocument doc;
        try
        {
            // DTDs disabled: an uploaded file must not be able to pull in external entities.
            var settings = new System.Xml.XmlReaderSettings
            {
                DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                XmlResolver   = null,
            };
            using var reader = System.Xml.XmlReader.Create(path, settings);
            doc = System.Xml.Linq.XDocument.Load(reader);
        }
        catch
        {
            File.Delete(path);
            throw new InvalidOperationException("the file is not valid SVG.");
        }

        foreach (var el in doc.Descendants()
                     .Where(e => UnsafeSvgElements.Contains(e.Name.LocalName.ToLowerInvariant()))
                     .ToList())
            el.Remove();

        foreach (var el in doc.Descendants())
        {
            var unsafeAttrs = el.Attributes().Where(a =>
                    // onclick, onload, …
                    a.Name.LocalName.StartsWith("on", StringComparison.OrdinalIgnoreCase)
                    // links out of the document: javascript:, http(s):, data:
                    || (a.Name.LocalName.Equals("href", StringComparison.OrdinalIgnoreCase)
                        && !a.Value.TrimStart().StartsWith('#')))
                .ToList();

            foreach (var a in unsafeAttrs) a.Remove();
        }

        doc.Save(path);
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
