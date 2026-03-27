namespace GridAcademy.Services;

/// <summary>
/// Sends a PDF to the Mathpix API and returns the full text as Mathpix Markdown (MMD),
/// with all mathematical expressions already formatted as inline LaTeX $…$ / display $$…$$.
/// </summary>
public interface IMathpixService
{
    /// <summary>True when AppId and AppKey are both configured in appsettings.json.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Uploads <paramref name="pdfBytes"/> to Mathpix, polls until processing is complete,
    /// then returns the full MMD text.  Throws on API error or timeout.
    /// </summary>
    Task<string> OcrPdfAsync(byte[] pdfBytes, CancellationToken ct = default);
}
