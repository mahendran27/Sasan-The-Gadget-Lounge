using System.Runtime.Versioning;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Core.Enums;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing;

/// <summary>
/// Universal Barcode Label Printing Engine.
/// Generates vector PDF documents and native TSPL thermal printer streams
/// across all 7 Barcode Sticker Styles (Choices 1 to 7).
/// </summary>
[SupportedOSPlatform("windows")]
public class BarcodeLabelEngine
{
    private readonly BarcodeGeneratorService _barcodeService;

    public BarcodeLabelEngine(BarcodeGeneratorService barcodeService)
    {
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Creates the QuestPDF IDocument for the chosen barcode sticker design
    /// </summary>
    public IDocument CreateDocument(BarcodeLabelStyle style, BarcodeStickerData data)
    {
        return style switch
        {
            BarcodeLabelStyle.Flagship_50x30mm => new FlagshipStickerDocument(data, _barcodeService),
            BarcodeLabelStyle.CompactAccessory_50x25mm => new CompactAccessoryDocument(data, _barcodeService),
            BarcodeLabelStyle.DumbbellJewelryTag => new DumbbellTagDocument(data, _barcodeService),
            BarcodeLabelStyle.TamperWarrantyVoidSeal_25mm => new TamperVoidSealDocument(data, _barcodeService),
            BarcodeLabelStyle.DualQrTechExplorer => new DualQrTechExplorerDocument(data, _barcodeService),
            BarcodeLabelStyle.MultiStickerA4Sheet_24Up => new MultiStickerA4SheetDocument(data, _barcodeService),
            BarcodeLabelStyle.MetallicSilverWeatherproof => new MetallicWeatherproofDocument(data, _barcodeService),
            _ => new FlagshipStickerDocument(data, _barcodeService)
        };
    }

    /// <summary>
    /// Generates high-DPI PDF bytes for the chosen sticker format
    /// </summary>
    public byte[] GeneratePdfBytes(BarcodeLabelStyle style, BarcodeStickerData data)
    {
        var doc = CreateDocument(style, data);
        return doc.GeneratePdf();
    }

    /// <summary>
    /// Generates native TSPL / ESC-POS command stream for direct USB/LAN thermal label printers
    /// </summary>
    public byte[] GenerateTsplStream(BarcodeLabelStyle style, BarcodeStickerData data)
    {
        string cmd = style switch
        {
            BarcodeLabelStyle.CompactAccessory_50x25mm => _barcodeService.GenerateTsplLabelCommand(data.ModelName, data.Identifier, data.Price, data.Category ?? "ACC"),
            _ => _barcodeService.GenerateTsplDetailedLabelCommand(
                data.ModelName, 
                data.Specs ?? string.Empty, 
                data.Identifier, 
                data.Price, 
                data.ConditionGrade ?? "Brand New", 
                data.BatteryHealth, 
                data.Warranty ?? "Shop Warranty")
        };
        return Encoding.ASCII.GetBytes(cmd);
    }
}

// =========================================================================
// 1. CHOICE 1: FLAGSHIP 50x30mm ALL-IN-ONE THERMAL STICKER
// =========================================================================
[SupportedOSPlatform("windows")]
internal class FlagshipStickerDocument : IDocument
{
    private readonly BarcodeStickerData _data;
    private readonly BarcodeGeneratorService _barcodeService;

    public FlagshipStickerDocument(BarcodeStickerData data, BarcodeGeneratorService barcodeService)
    {
        _data = data;
        _barcodeService = barcodeService;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.ContinuousSize(50, Unit.Millimetre);
            page.Margin(1.5f, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(6.5f).FontFamily("Segoe UI").FontColor("#000000"));

            page.Content().Column(col =>
            {
                // Shop Header
                col.Item().AlignCenter().Text(_data.ShopName.ToUpper()).FontSize(6.8f).Bold();

                // Model Name
                col.Item().PaddingTop(0.5f).Text(_data.ModelName).FontSize(7.2f).Bold();

                // Specs
                if (!string.IsNullOrWhiteSpace(_data.Specs))
                    col.Item().Text(_data.Specs).FontSize(5.8f).FontColor("#333333");

                // Badges
                var badges = new List<string>();
                if (_data.BatteryHealth.HasValue) badges.Add($"Bat: {_data.BatteryHealth}%");
                if (!string.IsNullOrWhiteSpace(_data.ConditionGrade)) badges.Add(_data.ConditionGrade);
                if (!string.IsNullOrWhiteSpace(_data.Warranty)) badges.Add(_data.Warranty);
                if (badges.Any())
                    col.Item().PaddingTop(0.5f).Text(string.Join(" • ", badges)).FontSize(5.5f).Italic();

                // Code-128 Barcode
                byte[] barcodeBytes = _barcodeService.GenerateCode128Barcode(_data.Identifier, 260, 48);
                if (barcodeBytes.Length > 0)
                    col.Item().PaddingTop(1f).AlignCenter().Height(16, Unit.Point).Image(barcodeBytes);

                // Identifier & Price
                col.Item().PaddingTop(0.5f).Row(row =>
                {
                    row.RelativeItem(7).Text($"IMEI: {_data.Identifier}").FontSize(6.2f).Bold();
                    row.RelativeItem(5).AlignRight().Text($"₹{_data.Price:N0}").FontSize(8f).Bold();
                });
            });
        });
    }
}

// =========================================================================
// 2. CHOICE 2: COMPACT ACCESSORY 50x25mm STICKER
// =========================================================================
[SupportedOSPlatform("windows")]
internal class CompactAccessoryDocument : IDocument
{
    private readonly BarcodeStickerData _data;
    private readonly BarcodeGeneratorService _barcodeService;

    public CompactAccessoryDocument(BarcodeStickerData data, BarcodeGeneratorService barcodeService)
    {
        _data = data;
        _barcodeService = barcodeService;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.ContinuousSize(50, Unit.Millimetre);
            page.Margin(1.5f, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(6f).FontFamily("Segoe UI").FontColor("#000000"));

            page.Content().Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text(_data.ShopName.ToUpper()).FontSize(6.2f).Bold();
                    r.AutoItem().Text(_data.Category ?? "ACC").FontSize(5.5f).Bold();
                });

                col.Item().PaddingTop(0.5f).Text(_data.ModelName).FontSize(7f).Bold();

                // Code-128
                byte[] barcodeBytes = _barcodeService.GenerateCode128Barcode(_data.Identifier, 260, 42);
                if (barcodeBytes.Length > 0)
                    col.Item().PaddingTop(1f).AlignCenter().Height(14, Unit.Point).Image(barcodeBytes);

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text($"SKU: {_data.Identifier}").FontSize(5.8f).Bold();
                    if (_data.MRP.HasValue && _data.MRP > _data.Price)
                    {
                        r.AutoItem().Text($"MRP ₹{_data.MRP:N0}").FontSize(5.5f).Strikethrough();
                        r.AutoItem().PaddingLeft(3).Text($"₹{_data.Price:N0}").FontSize(7.5f).Bold();
                    }
                    else
                    {
                        r.AutoItem().Text($"₹{_data.Price:N0}").FontSize(7.5f).Bold();
                    }
                });
            });
        });
    }
}

// =========================================================================
// 3. CHOICE 3: DUMBBELL / JEWELRY FOLDABLE WRAP TAG (70x15mm)
// =========================================================================
[SupportedOSPlatform("windows")]
internal class DumbbellTagDocument : IDocument
{
    private readonly BarcodeStickerData _data;
    private readonly BarcodeGeneratorService _barcodeService;

    public DumbbellTagDocument(BarcodeStickerData data, BarcodeGeneratorService barcodeService)
    {
        _data = data;
        _barcodeService = barcodeService;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.ContinuousSize(70, Unit.Millimetre);
            page.Margin(1.5f, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(5.5f).FontFamily("Segoe UI").FontColor("#000000"));

            page.Content().Row(row =>
            {
                // Left Wing (Shop & Micro-QR)
                row.RelativeItem(5).Column(left =>
                {
                    left.Item().Text("SASAN").FontSize(6.2f).Bold();
                    left.Item().Text("GADGET LOUNGE").FontSize(5f);

                    byte[] qrBytes = _barcodeService.GenerateQrCode(_data.Identifier, 70);
                    if (qrBytes.Length > 0)
                        left.Item().PaddingTop(1).Height(18, Unit.Point).Image(qrBytes);

                    left.Item().Text(_data.Identifier.Length > 10 ? _data.Identifier[..10] + ".." : _data.Identifier).FontSize(5f);
                });

                // Center Bridge (Fold zone)
                row.ConstantItem(12, Unit.Millimetre).AlignCenter().Column(bridge =>
                {
                    bridge.Item().PaddingTop(8).Text("=== FOLD ===").FontSize(4f).FontColor("#888888");
                });

                // Right Wing (Specs & Price)
                row.RelativeItem(5).Column(right =>
                {
                    right.Item().Text(_data.ModelName).FontSize(6.5f).Bold();
                    if (!string.IsNullOrWhiteSpace(_data.Specs))
                        right.Item().Text(_data.Specs).FontSize(5f).FontColor("#444444");

                    right.Item().PaddingTop(2).Text($"₹{_data.Price:N0}").FontSize(8f).Bold();
                    right.Item().Text("100% GENUINE").FontSize(4.8f).Italic().FontColor("#222222");
                });
            });
        });
    }
}

// =========================================================================
// 4. CHOICE 4: TAMPER WARRANTY VOID SEAL (25x25mm Round/Square)
// =========================================================================
[SupportedOSPlatform("windows")]
internal class TamperVoidSealDocument : IDocument
{
    private readonly BarcodeStickerData _data;
    private readonly BarcodeGeneratorService _barcodeService;

    public TamperVoidSealDocument(BarcodeStickerData data, BarcodeGeneratorService barcodeService)
    {
        _data = data;
        _barcodeService = barcodeService;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.ContinuousSize(25, Unit.Millimetre);
            page.Margin(1, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(4.8f).FontFamily("Segoe UI").FontColor("#000000"));

            page.Content().Border(0.5f).BorderColor("#000000").Padding(1).Column(col =>
            {
                col.Item().AlignCenter().Text("★ SECURITY SEAL ★").FontSize(4.8f).Bold();
                col.Item().AlignCenter().Text("WARRANTY VOID IF BROKEN").FontSize(4.5f).Bold();

                byte[] qrBytes = _barcodeService.GenerateQrCode($"AUTH:{_data.Identifier}|SASAN", 60);
                if (qrBytes.Length > 0)
                    col.Item().PaddingTop(1).AlignCenter().Height(14, Unit.Point).Image(qrBytes);

                col.Item().AlignCenter().Text($"ID: {_data.Identifier}").FontSize(4.2f).Bold();
                col.Item().AlignCenter().Text("SASAN THE GADGET LOUNGE").FontSize(4.2f);
                col.Item().AlignCenter().Text("HELPLINE: 9080803070").FontSize(4f).Bold();
            });
        });
    }
}

// =========================================================================
// 5. CHOICE 5: DUAL-QR TECH EXPLORER STICKER (60x32mm)
// =========================================================================
[SupportedOSPlatform("windows")]
internal class DualQrTechExplorerDocument : IDocument
{
    private readonly BarcodeStickerData _data;
    private readonly BarcodeGeneratorService _barcodeService;

    public DualQrTechExplorerDocument(BarcodeStickerData data, BarcodeGeneratorService barcodeService)
    {
        _data = data;
        _barcodeService = barcodeService;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.ContinuousSize(60, Unit.Millimetre);
            page.Margin(1.5f, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(6f).FontFamily("Segoe UI").FontColor("#000000"));

            page.Content().Column(col =>
            {
                // Header
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("SASAN THE GADGET LOUNGE").FontSize(6.8f).Bold();
                    r.AutoItem().Text($"₹{_data.Price:N0}").FontSize(8.5f).Bold();
                });

                col.Item().PaddingTop(0.5f).Text(_data.ModelName).FontSize(7.2f).Bold();

                // Dual Column: Left POS Barcode | Right Customer Phone QR
                col.Item().PaddingTop(1f).Row(row =>
                {
                    // Left: Cashier Barcode
                    row.RelativeItem(7).Column(lc =>
                    {
                        byte[] bc = _barcodeService.GenerateCode128Barcode(_data.Identifier, 220, 38);
                        if (bc.Length > 0)
                            lc.Item().Height(14, Unit.Point).Image(bc);

                        lc.Item().Text($"IMEI: {_data.Identifier}").FontSize(5.5f).Bold();
                        if (_data.BatteryHealth.HasValue)
                            lc.Item().Text($"Health: {_data.BatteryHealth}% | {_data.ConditionGrade}").FontSize(5.2f);
                    });

                    // Right: Customer Tech Explorer QR
                    row.RelativeItem(5).AlignRight().Column(rc =>
                    {
                        byte[] qr = _barcodeService.GenerateQrCode($"https://sasanthegadgetlounge.in/verify?imei={_data.Identifier}", 80);
                        if (qr.Length > 0)
                            rc.Item().AlignCenter().Height(20, Unit.Point).Image(qr);

                        rc.Item().AlignCenter().Text("SCAN TO EXPLORE").FontSize(4.8f).Bold();
                    });
                });
            });
        });
    }
}

// =========================================================================
// 6. CHOICE 6: MULTI-STICKER A4 SHEET 24-UP (3 Cols x 8 Rows = 24 Labels)
// =========================================================================
[SupportedOSPlatform("windows")]
internal class MultiStickerA4SheetDocument : IDocument
{
    private readonly BarcodeStickerData _data;
    private readonly BarcodeGeneratorService _barcodeService;

    public MultiStickerA4SheetDocument(BarcodeStickerData data, BarcodeGeneratorService barcodeService)
    {
        _data = data;
        _barcodeService = barcodeService;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(15, Unit.Point);
            page.DefaultTextStyle(x => x.FontSize(6f).FontFamily("Segoe UI").FontColor("#000000"));

            page.Content().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                    cols.RelativeColumn(1);
                });

                // Generate 24 stickers (8 rows x 3 columns)
                byte[] barcodeBytes = _barcodeService.GenerateCode128Barcode(_data.Identifier, 200, 32);

                for (int i = 0; i < 24; i++)
                {
                    table.Cell().Border(0.5f).BorderColor("#CCCCCC").Padding(4).Column(cell =>
                    {
                        cell.Item().AlignCenter().Text("SASAN GADGET LOUNGE").FontSize(6.2f).Bold();
                        cell.Item().Text(_data.ModelName).FontSize(6.5f).Bold();
                        if (!string.IsNullOrWhiteSpace(_data.Specs))
                            cell.Item().Text(_data.Specs).FontSize(5.2f).FontColor("#444444");

                        if (barcodeBytes.Length > 0)
                            cell.Item().PaddingTop(1).AlignCenter().Height(13, Unit.Point).Image(barcodeBytes);

                        cell.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"IMEI: {_data.Identifier}").FontSize(5.2f);
                            r.AutoItem().Text($"₹{_data.Price:N0}").FontSize(7f).Bold();
                        });
                    });
                }
            });
        });
    }
}

// =========================================================================
// 7. CHOICE 7: METALLIC SILVER WEATHERPROOF INDUSTRIAL STICKER (50x30mm)
// =========================================================================
[SupportedOSPlatform("windows")]
internal class MetallicWeatherproofDocument : IDocument
{
    private readonly BarcodeStickerData _data;
    private readonly BarcodeGeneratorService _barcodeService;

    public MetallicWeatherproofDocument(BarcodeStickerData data, BarcodeGeneratorService barcodeService)
    {
        _data = data;
        _barcodeService = barcodeService;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.ContinuousSize(50, Unit.Millimetre);
            page.Margin(1.5f, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(6f).FontFamily("Consolas").FontColor("#000000"));

            // Metallic dark high-contrast border
            page.Content().Border(1).BorderColor("#000000").Padding(2).Column(col =>
            {
                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("SASAN LAB CERTIFIED").FontSize(6.5f).Bold();
                    r.AutoItem().Text("CE / BIS").FontSize(5.5f).Bold();
                });

                col.Item().Text(_data.ModelName).FontSize(7f).Bold();
                if (!string.IsNullOrWhiteSpace(_data.Specs))
                    col.Item().Text(_data.Specs).FontSize(5.5f);

                // Code 128
                byte[] bc = _barcodeService.GenerateCode128Barcode(_data.Identifier, 260, 40);
                if (bc.Length > 0)
                    col.Item().PaddingTop(1).AlignCenter().Height(15, Unit.Point).Image(bc);

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text($"SN/IMEI: {_data.Identifier}").FontSize(6f).Bold();
                    r.AutoItem().Text($"INR {_data.Price:N0}").FontSize(7.5f).Bold();
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Text("QC: PASS • GENUINE HARDWARE").FontSize(5f);
                    r.AutoItem().Text("HOTLINE: 9080803070").FontSize(5f).Bold();
                });
            });
        });
    }
}
