using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;
using SasanERP.Services.Barcode;
using SasanERP.Services.Printing;

namespace SasanERP.UI.ViewModels;

/// <summary>
/// Product Intake and Barcode Sticker Studio ViewModel for Sasan The Gadget Lounge
/// Allows recording comprehensive device specifications and printing instant barcode stickers.
/// </summary>
public partial class InventoryIntakeViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly BarcodeGeneratorService _barcodeService;

    public ObservableCollection<DeviceType> Categories { get; } = new(Enum.GetValues<DeviceType>());
    public ObservableCollection<ConditionGrade> ConditionGrades { get; } = new(Enum.GetValues<ConditionGrade>());
    public ObservableCollection<WarrantyStatus> WarrantyOptions { get; } = new(Enum.GetValues<WarrantyStatus>());
    public ObservableCollection<PurchaseSource> PurchaseSources { get; } = new(Enum.GetValues<PurchaseSource>());
    public ObservableCollection<Supplier> Suppliers { get; } = new();

    [ObservableProperty]
    private DeviceType _selectedCategory = DeviceType.Smartphone;

    [ObservableProperty]
    private string _brand = "Apple";

    [ObservableProperty]
    private string _modelName = "iPhone 15 Pro";

    [ObservableProperty]
    private string _storageCapacity = "128GB";

    [ObservableProperty]
    private string _color = "Natural Titanium";

    [ObservableProperty]
    private bool _isSerialized = true;

    [ObservableProperty]
    private string _imei1 = string.Empty;

    [ObservableProperty]
    private string? _imei2;

    [ObservableProperty]
    private string _serialNumber = string.Empty;

    [ObservableProperty]
    private int? _batteryHealthPercentage = 98;

    [ObservableProperty]
    private ConditionGrade _selectedCondition = ConditionGrade.GradeAPlus_Flawless;

    [ObservableProperty]
    private WarrantyStatus _selectedWarranty = WarrantyStatus.ShopWarranty_90Days;

    [ObservableProperty]
    private PurchaseSource _selectedSource = PurchaseSource.DirectImport;

    [ObservableProperty]
    private decimal _purchaseCost = 72000.00m;

    [ObservableProperty]
    private decimal _sellingPrice = 82999.00m;

    [ObservableProperty]
    private decimal _mrp = 134900.00m;

    [ObservableProperty]
    private int _quantity = 1; // For bulk accessories

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private byte[]? _previewBarcodeBytes;

    [ObservableProperty]
    private string _statusMessage = "Ready. Enter device specs and click Save & Print Sticker.";

    [ObservableProperty]
    private bool _isBusy;

    public InventoryIntakeViewModel(AppDbContext context, BarcodeGeneratorService barcodeService)
    {
        _context = context;
        _barcodeService = barcodeService;
        GenerateRandomSampleImei();
        UpdatePreviewBarcode();
    }

    public async Task LoadSuppliersAsync()
    {
        var list = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
        Suppliers.Clear();
        foreach (var s in list)
        {
            Suppliers.Add(s);
        }
        SelectedSupplier = Suppliers.FirstOrDefault();
    }

    [RelayCommand]
    public void GenerateRandomSampleImei()
    {
        var random = new Random();
        Imei1 = "35" + random.Next(100000000, 999999999).ToString() + random.Next(1000, 9999).ToString();
        Imei2 = "35" + random.Next(100000000, 999999999).ToString() + random.Next(1000, 9999).ToString();
        SerialNumber = "F2L" + random.Next(100000, 999999).ToString() + "M";
        UpdatePreviewBarcode();
    }

    [RelayCommand]
    public void UpdatePreviewBarcode()
    {
        string code = !string.IsNullOrWhiteSpace(Imei1) 
            ? Imei1 
            : (!string.IsNullOrWhiteSpace(SerialNumber) ? SerialNumber : "SASAN001");

        try
        {
            PreviewBarcodeBytes = _barcodeService.GenerateCode128Barcode(code, 300, 70);
        }
        catch
        {
            PreviewBarcodeBytes = null;
        }
    }

    /// <summary>
    /// Saves the item/device into inventory and prints a high-resolution barcode sticker
    /// </summary>
    [RelayCommand]
    public async Task SaveAndPrintStickerAsync()
    {
        if (string.IsNullOrWhiteSpace(ModelName))
        {
            StatusMessage = "[ERROR] Model Name is required.";
            return;
        }

        if (IsSerialized && string.IsNullOrWhiteSpace(Imei1) && string.IsNullOrWhiteSpace(SerialNumber))
        {
            StatusMessage = "[ERROR] Serialized devices must have an IMEI 1 or Serial Number.";
            return;
        }

        IsBusy = true;
        try
        {
            string sku = GenerateSku();

            // Find or create parent product
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Name == ModelName && p.Brand == Brand);

            if (product == null)
            {
                product = new Product
                {
                    Name = ModelName,
                    Brand = Brand,
                    Category = SelectedCategory,
                    SKU = sku,
                    Barcode = !string.IsNullOrWhiteSpace(Imei1) ? Imei1 : sku,
                    HSNCode = GetHsnForCategory(SelectedCategory),
                    IsSerialized = IsSerialized,
                    DefaultCostPrice = PurchaseCost,
                    SellingPrice = SellingPrice,
                    MRP = Mrp,
                    GSTPercentage = 18.0m,
                    CurrentStock = 0,
                    ReorderLevel = 1
                };
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }

            if (IsSerialized)
            {
                // Verify unique IMEI
                bool imeiExists = await _context.SerializedUnits
                    .AnyAsync(u => u.IMEI1 == Imei1 || (u.SerialNumber == SerialNumber && !string.IsNullOrEmpty(SerialNumber)));

                if (imeiExists)
                {
                    StatusMessage = $"[ERROR] Device with IMEI {Imei1} or Serial {SerialNumber} already exists in database!";
                    return;
                }

                var unit = new SerializedUnit
                {
                    ProductId = product.Id,
                    IMEI1 = Imei1,
                    IMEI2 = Imei2,
                    SerialNumber = SerialNumber,
                    Color = Color,
                    StorageCapacity = StorageCapacity,
                    BatteryHealthPercentage = BatteryHealthPercentage,
                    ConditionGrade = SelectedCondition,
                    WarrantyStatus = SelectedWarranty,
                    PurchaseSource = SelectedSource,
                    PurchaseCost = PurchaseCost,
                    TaggedPrice = SellingPrice,
                    Status = UnitStatus.InStock,
                    SupplierId = SelectedSupplier?.Id,
                    PurchaseDate = DateTime.UtcNow
                };

                _context.SerializedUnits.Add(unit);
                product.CurrentStock++;
                await _context.SaveChangesAsync();

                StatusMessage = $"[SUCCESS] Device saved: {product.Name} (IMEI: {Imei1}). Printing sticker...";
            }
            else
            {
                // Bulk accessory item
                product.CurrentStock += Quantity;
                product.DefaultCostPrice = PurchaseCost;
                product.SellingPrice = SellingPrice;
                await _context.SaveChangesAsync();

                StatusMessage = $"[SUCCESS] Bulk stock added: {product.Name} (Qty: +{Quantity}). Total Stock: {product.CurrentStock}.";
            }

            // Print the sticker
            PrintBarcodeSticker();

            // Prepare for next entry
            GenerateRandomSampleImei();
        }
        catch (Exception ex)
        {
            StatusMessage = $"[ERROR] Failed to save device: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Generates sticker document and sends to printer / saves PDF
    /// </summary>
    [RelayCommand]
    public void PrintBarcodeSticker()
    {
        string identifier = !string.IsNullOrWhiteSpace(Imei1) 
            ? Imei1 
            : (!string.IsNullOrWhiteSpace(SerialNumber) ? SerialNumber : GenerateSku());

        string specsText = $"{StorageCapacity} | {Color}".Trim(' ', '|');
        var stickerData = new BarcodeStickerData(
            ShopName: "SASAN THE GADGET LOUNGE",
            ModelName: $"{Brand} {ModelName}",
            Specs: specsText,
            Identifier: identifier,
            Price: SellingPrice,
            MRP: Mrp > 0 ? Mrp : null,
            ConditionGrade: SelectedCondition.ToString().Replace('_', ' '),
            BatteryHealth: BatteryHealthPercentage,
            Warranty: SelectedWarranty.ToString().Replace('_', ' '),
            Category: SelectedCategory.ToString()
        );

        // 1. Generate TSPL Command for direct thermal printer
        string tspl = _barcodeService.GenerateTsplDetailedLabelCommand(
            $"{Brand} {ModelName}",
            specsText,
            identifier,
            SellingPrice,
            SelectedCondition.ToString(),
            BatteryHealthPercentage,
            SelectedWarranty.ToString());

        // 2. Generate vector PDF sticker for preview / universal printing
        var labelDoc = new BarcodeLabelDocument(stickerData, _barcodeService, 50f, 32f);

        string stickerFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SasanERP_Stickers");
        Directory.CreateDirectory(stickerFolder);
        string stickerPdfPath = Path.Combine(stickerFolder, $"Sticker_{identifier}.pdf");

        labelDoc.GeneratePdf(stickerPdfPath);

        StatusMessage = $"[PRINTED] Barcode sticker generated: {stickerPdfPath}";
    }

    private string GenerateSku()
    {
        string b = Brand.Length >= 3 ? Brand.Substring(0, 3).ToUpper() : "GEN";
        string m = ModelName.Length >= 3 ? ModelName.Substring(0, 3).ToUpper() : "MOD";
        string s = StorageCapacity.Replace(" ", "").ToUpper();
        return $"{b}-{m}-{s}".Trim('-');
    }

    private static string GetHsnForCategory(DeviceType type) => type switch
    {
        DeviceType.Smartphone => "8517",
        DeviceType.Laptop => "8471",
        DeviceType.Drone => "8806",
        DeviceType.Charger => "8504",
        DeviceType.PowerBank => "8504",
        DeviceType.Toy => "9503",
        DeviceType.AudioPlayer => "8518",
        _ => "8517"
    };
}
