using System.IO;

namespace SasanERP.Services.Printing;

/// <summary>
/// Provides cached, high-resolution logo image bytes for QuestPDF Invoices
/// Supports both vibrant Full Color and high-contrast Black & White laser printing.
/// </summary>
public static class LogoProvider
{
    private static byte[]? _cachedColorLogo;
    private static byte[]? _cachedBwLogo;

    public static byte[]? GetLogoBytes(bool isMonochrome = false, string? customPath = null)
    {
        // 1. If custom user path provided and exists
        if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
        {
            try
            {
                return File.ReadAllBytes(customPath);
            }
            catch
            {
                // Fall back to default bundled logo
            }
        }

        // 2. Return cached bytes if already loaded
        if (isMonochrome && _cachedBwLogo != null) return _cachedBwLogo;
        if (!isMonochrome && _cachedColorLogo != null) return _cachedColorLogo;

        // 3. Search common application paths for the bundled Sasan Gadget Lounge logo
        string fileName = isMonochrome ? "logo_bw.png" : "logo.png";
        string[] searchPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", fileName),
            Path.Combine(AppContext.BaseDirectory, fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "SasanERP.Services", "Assets", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "SasanERP.UI", "Assets", fileName),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "SasanERP.Services", "Assets", fileName)
        };

        foreach (var p in searchPaths)
        {
            if (File.Exists(p))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(p);
                    if (isMonochrome) _cachedBwLogo = bytes;
                    else _cachedColorLogo = bytes;
                    return bytes;
                }
                catch
                {
                    // try next path
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Clears memory cache (e.g. after user updates the logo in store settings)
    /// </summary>
    public static void ResetCache()
    {
        _cachedColorLogo = null;
        _cachedBwLogo = null;
    }
}
