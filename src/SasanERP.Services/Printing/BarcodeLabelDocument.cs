using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SasanERP.Services.Barcode;

namespace SasanERP.Services.Printing;

/// <summary>
/// Data contract for printing a rich gadget barcode sticker
/// </summary>
public record BarcodeStickerData(
    string ShopName,
    string ModelName,
    string? Specs,               // e.g. "256GB | Natural Titanium"
    string Identifier,          // IMEI 1, Serial Number, or SKU
    decimal Price,
    decimal? MRP,
    string? ConditionGrade,     // "Grade A+ (Flawless)"
    int? BatteryHealth,         // 98
    string? Warranty,           // "90 Days Shop Warranty"
    string? Category            // "Smartphone", "Laptop", "Drone"
);

/// <summary>
/// High-Resolution Vector Barcode Label Generator for 50x25mm and 50x38mm sticker rolls
/// Prints directly to thermal label printers or saves as high-DPI label PDF.
/// </summary>
public class BarcodeLabelDocument : IDocument
{
    private readonly BarcodeStickerData _data;
    private readonly BarcodeGeneratorService _barcodeService;
    private readonly float _widthMm;
    private readonly float _heightMm;

    public BarcodeLabelDocument(
        BarcodeStickerData data, 
        BarcodeGeneratorService barcodeService,
        float widthMm = 50f, 
        float heightMm = 30f)
    {
        _data = data;
        _barcodeService = barcodeService;
        _widthMm = widthMm;
        _heightMm = heightMm;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Label_{_data.Identifier}",
        Author = _data.ShopName
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.ContinuousSize(_widthMm, Unit.Millimetre);
            page.Margin(1.5f, Unit.Millimetre);
            page.DefaultTextStyle(x => x.FontSize(6.5f).FontFamily("Segoe UI").FontColor("#000000"));

            page.Content().Column(col =>
            {
                // 1. Shop Header
                col.Item().AlignCenter().Text(_data.ShopName.ToUpper())
                    .FontSize(6.5f).Bold().FontColor("#000000");

                // 2. Model Name
                col.Item().PaddingTop(0.5f).Text(_data.ModelName)
                    .FontSize(7.2f).Bold();

                // 3. Storage & Color
                if (!string.IsNullOrWhiteSpace(_data.Specs))
                {
                    col.Item().Text(_data.Specs).FontSize(5.8f).FontColor("#333333");
                }

                // 4. Battery Health & Condition Grade Badges
                var badgeParts = new List<string>();
                if (_data.BatteryHealth.HasValue) badgeParts.Add($"Bat: {_data.BatteryHealth}%");
                if (!string.IsNullOrWhiteSpace(_data.ConditionGrade)) badgeParts.Add(_data.ConditionGrade);
                if (!string.IsNullOrWhiteSpace(_data.Warranty)) badgeParts.Add(_data.Warranty);

                if (badgeParts.Any())
                {
                    col.Item().PaddingTop(0.5f).Text(string.Join(" • ", badgeParts))
                        .FontSize(5.5f).FontColor("#222222").Italic();
                }

                // 5. Code-128 Barcode Image
                byte[] barcodeBytes = _barcodeService.GenerateCode128Barcode(_data.Identifier, 260, 48);
                if (barcodeBytes.Length > 0)
                {
                    col.Item().PaddingTop(1f).AlignCenter().Height(16, Unit.Point).Image(barcodeBytes);
                }

                // 6. Identifier (IMEI) & Price Row
                col.Item().PaddingTop(0.5f).Row(row =>
                {
                    row.RelativeItem(7).Column(c =>
                    {
                        c.Item().Text($"IMEI: {_data.Identifier}").FontSize(6.2f).Bold();
                    });

                    row.RelativeItem(5).AlignRight().Column(c =>
                    {
                        c.Item().Text($"₹{_data.Price:N0}").FontSize(8f).Bold();
                    });
                });
            });
        });
    }
}
