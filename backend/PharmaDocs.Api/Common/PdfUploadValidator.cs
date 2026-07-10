using Microsoft.AspNetCore.Http;
using PharmaDocs.Api.Common.Exceptions;

namespace PharmaDocs.Api.Common;

/// <summary>
/// Gedeelde validatie voor PDF-uploads — facturen (<see cref="Services.DocumentService"/>)
/// én kennisbank-procedures (<see cref="Services.KnowledgeService"/>). Controleert
/// aanwezigheid, grootte, content-type/extensie én de <b>%PDF-magic-byte-check</b> (L5).
/// Eén bron van waarheid, zodat beide uploadpaden even streng zijn; voordien miste de
/// kennisupload de magic-byte-check.
/// </summary>
public static class PdfUploadValidator
{
    /// <summary>Bovengrens per upload — gelijkgehouden met de AI-service (10 MB).</summary>
    public const long MaxBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Gooit een <see cref="AppException"/> (400/413/415) als de upload geen geldig,
    /// aanvaardbaar PDF-bestand is. Verandert de leespositie van de stream niet: de
    /// magic-byte-check gebeurt op een aparte <see cref="IFormFile.OpenReadStream"/>.
    /// </summary>
    public static void Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("Geen bestand ontvangen.");
        if (file.Length > MaxBytes)
            throw new PayloadTooLargeException("Bestand te groot (max. 10 MB).");

        var isPdf = file.ContentType is "application/pdf" or "application/octet-stream"
            || file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
            throw new UnsupportedMediaTypeException("Enkel PDF-bestanden worden ondersteund.");

        // Magic bytes (L5): een echt PDF begint met "%PDF". Weert een niet-PDF met een
        // misleidende .pdf-naam of octet-stream-content-type.
        Span<byte> header = stackalloc byte[4];
        using var probe = file.OpenReadStream();
        var read = probe.ReadAtLeast(header, 4, throwOnEndOfStream: false);
        if (read < 4 || header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46)
            throw new UnsupportedMediaTypeException("Het bestand is geen geldig PDF (ontbrekende %PDF-header).");
    }
}
