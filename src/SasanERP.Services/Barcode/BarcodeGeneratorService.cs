using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace SasanERP.Services.Barcode;

/// <summary>
/// High-DPI Barcode and QR Code generation service using ZXing.Net
/// </summary>
[SupportedOSPlatform("windows")]
public class BarcodeGeneratorService
{
    /// <summary>
    /// Generates Code-128 1D barcode image bytes (PNG) for IMEI or Serial Numbers
    /// </summary>
    public byte[] GenerateCode128Barcode(string content, int width = 300, int height = 80)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<byte>();

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Height = height,
                Width = width,
                Margin = 2,
                PureBarcode = false
            }
        };

        var pixelData = writer.Write(content);
        using var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, pixelData.Width, pixelData.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppRgb);

        try
        {
            System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>
    /// Generates high-resolution 2D QR Code image bytes (PNG) for UPI Payment or IMEI Quick-lookup
    /// </summary>
    public byte[] GenerateQrCode(string content, int size = 200)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<byte>();

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = size,
                Width = size,
                Margin = 1,
                ErrorCorrection = ZXing.QrCode.Internal.ErrorCorrectionLevel.M
            }
        };

        var pixelData = writer.Write(content);
        using var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, pixelData.Width, pixelData.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppRgb);

        try
        {
            System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    /// <summary>
    /// Formats standard NPCI UPI Intent URI for Dynamic QR Code Billing
    /// Example: upi://pay?pa=9080803070@sbi&pn=Sasan%20The%20Gadget%20Lounge&am=82999.00&cu=INR
    /// </summary>
    public static string BuildUpiPaymentUri(string upiId, string payeeName, decimal amount, string invoiceNumber)
    {
        string encodedName = Uri.EscapeDataString(payeeName);
        string formattedAmount = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        return $"upi://pay?pa={upiId}&pn={encodedName}&am={formattedAmount}&cu=INR&tn=Invoice_{invoiceNumber}";
    }

    /// <summary>
    /// Generates detailed raw TSPL command for 50mm x 38mm or 50mm x 25mm thermal stickers
    /// with complete device specifications (Battery, Condition, Warranty, Storage, Price, IMEI).
    /// </summary>
    public string GenerateTsplDetailedLabelCommand(
        string modelName,
        string specs,
        string imei,
        decimal price,
        string condition,
        int? batteryHealth,
        string warranty)
    {
        string batteryText = batteryHealth.HasValue ? $"Bat: {batteryHealth}%" : string.Empty;
        string conditionWarranty = $"{condition} | {warranty}";

        return $@"
SIZE 50 mm, 38 mm
GAP 2 mm, 0 mm
DIRECTION 1
CLS
TEXT 20,15,""2"",0,1,1,""SASAN THE GADGET LOUNGE""
TEXT 20,42,""1"",0,1,1,""{Truncate(modelName, 26)}""
TEXT 20,64,""1"",0,1,1,""{Truncate(specs, 28)}""
BARCODE 20,88,""128"",50,1,0,2,2,""{imei}""
TEXT 20,148,""1"",0,1,1,""IMEI: {imei}""
TEXT 20,170,""1"",0,1,1,""{Truncate(conditionWarranty, 30)} {batteryText}""
TEXT 20,195,""3"",0,1,1,""INR {price:N0}""
PRINT 1,1
";
    }

    /// <summary>
    /// Builds structured payload for customer QR code scan (e.g. opens verification or warranty details)
    /// </summary>
    public static string BuildDeviceQrPayload(string model, string imei, decimal price, int? battery, string condition, string warranty)
    {
        return $"SASAN GADGET LOUNGE | {model} | IMEI: {imei} | Price: INR {price:N0} | Batt: {battery}% | Grade: {condition} | Warranty: {warranty} | Support: 9080803070";
    }

    /// <summary>
    /// Generates raw TSPL (Thermal Printer Command Language) for standard 50mm x 25mm barcode stickers.
    /// Used by TSC, TVS, Zebra, and Xprinter thermal barcode label printers.
    /// </summary>
    public string GenerateTsplLabelCommand(string modelName, string imei, decimal price, string condition)
    {
        return $@"
SIZE 50 mm, 25 mm
GAP 2 mm, 0 mm
DIRECTION 1
CLS
TEXT 20,15,""2"",0,1,1,""SASAN THE GADGET LOUNGE""
TEXT 20,40,""1"",0,1,1,""{Truncate(modelName, 26)}""
BARCODE 20,65,""128"",45,1,0,2,2,""{imei}""
TEXT 20,120,""1"",0,1,1,""IMEI: {imei}""
TEXT 20,145,""2"",0,1,1,""INR {price:N0}  [{condition}]""
PRINT 1,1
";
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength - 2) + "..";
    }
}
