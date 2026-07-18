namespace BlazorAtoms.Barcodes;

/// <summary>
/// Single source of truth for the URL-resolution + anchor emission rules shared by
/// <see cref="AtomQrCode"/>, <see cref="AtomBarcode"/>, and <see cref="AtomQrCodeImage"/>.
///
/// Rules:
///   1. If <c>explicitHref</c> is non-empty → use it verbatim. <c>autoLink</c> is ignored.
///   2. Else if <c>autoLink</c> is true → try to parse <c>candidate</c> as an absolute URI and
///      accept only http/https/mailto/tel schemes. javascript:/data:/file: are rejected —
///      QR payloads are a hostile input vector.
///   3. Else → no URL.
/// </summary>
internal static class BarcodeLink
{
    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https", "mailto", "tel",
    };

    public static bool TryResolveUrl(string? explicitHref, string? candidate, bool autoLink, out string url)
    {
        if (!string.IsNullOrWhiteSpace(explicitHref))
        {
            url = explicitHref;
            return true;
        }

        if (autoLink && !string.IsNullOrWhiteSpace(candidate) &&
            Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
            AllowedSchemes.Contains(uri.Scheme))
        {
            url = uri.AbsoluteUri;
            return true;
        }

        url = string.Empty;
        return false;
    }
}
