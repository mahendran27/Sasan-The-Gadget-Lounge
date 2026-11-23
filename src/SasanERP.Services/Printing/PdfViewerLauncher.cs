using System.Diagnostics;

namespace SasanERP.Services.Printing;

/// <summary>
/// Resilient cross-version PDF launcher for Windows POS & Store Workstations.
/// Ensures PDF invoices and barcode stickers ALWAYS open, even if Adobe Reader
/// is not installed, by cascading through Microsoft Edge, Chrome, and Windows Explorer.
/// </summary>
public static class PdfViewerLauncher
{
    public static bool Launch(string pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
            return false;

        // Tier 1: System default handler (Acrobat, Foxit, Nitro, etc.)
        try
        {
            Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            return true;
        }
        catch { /* Fall through to browser */ }

        // Tier 2: Microsoft Edge (Built into Windows 10 & 11)
        try
        {
            Process.Start(new ProcessStartInfo("msedge.exe", $"\"{pdfPath}\"") { UseShellExecute = true });
            return true;
        }
        catch { /* Fall through to Chrome */ }

        // Tier 3: Google Chrome
        try
        {
            Process.Start(new ProcessStartInfo("chrome.exe", $"\"{pdfPath}\"") { UseShellExecute = true });
            return true;
        }
        catch { /* Fall through to Explorer */ }

        // Tier 4: Reveal file in Windows Explorer
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{pdfPath}\"") { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
