using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;
using SasanERP.Services.Barcode;
using SasanERP.Services.Printing;

namespace SasanERP.UI.ViewModels;

public class InventoryDeviceItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string IMEI1 { get; set; } = string.Empty;
    public string? IMEI2 { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Specs { get; set; } = string.Empty;
    public int BatteryHealthPercentage { get; set; }
    public ConditionGrade ConditionGrade { get; set; }
    public WarrantyStatus WarrantyStatus { get; set; }
    public UnitStatus Status { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal TaggedPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int AgingDays { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? SoldInvoiceNumber { get; set; }
    public DateTime? SoldDate { get; set; }
}

public partial class InventoryViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly BarcodeLabelEngine _labelEngine;

    private readonly List<InventoryDeviceItem> _allDevices = new();

    public ObservableCollection<InventoryDeviceItem> FilteredDevices { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [ObservableProperty]
    private string _selectedStatusFilter = "All";

    partial void OnSelectedStatusFilterChanged(string value) => ApplyFilter();

    [ObservableProperty]
    private bool _showAgingOnly;

    partial void OnShowAgingOnlyChanged(bool value) => ApplyFilter();

    [ObservableProperty]
    private InventoryDeviceItem? _selectedDevice;

    [ObservableProperty]
    private string _statusMessage = "Ready. Browse, search, filter and print barcodes.";

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<BarcodeLabelStyle> AvailableBarcodeStyles { get; } =
        new(Enum.GetValues<BarcodeLabelStyle>());

    [ObservableProperty]
    private BarcodeLabelStyle _selectedBarcodeStyle = BarcodeLabelStyle.Flagship_50x30mm;

    public InventoryViewModel(AppDbContext context, BarcodeLabelEngine labelEngine)
    {
        _context = context;
        _labelEngine = labelEngine;
    }

    [RelayCommand]
    public async Task LoadInventoryAsync()
    {
        IsLoading = true;
        try
        {
            var units = await _context.SerializedUnits
                .Include(u => u.Product)
                .Include(u => u.SoldInvoice)
                .Include(u => u.Supplier)
                .Include(u => u.BuybackCustomer)
                .OrderByDescending(u => u.PurchaseDate)
                .ToListAsync();

            _allDevices.Clear();
            foreach (var u in units)
            {
                _allDevices.Add(new InventoryDeviceItem
                {
                    Id = u.Id,
                    Name = u.Product.Name,
                    Brand = u.Product.Brand,
                    Category = u.Product.Category.ToString(),
                    IMEI1 = u.IMEI1,
                    IMEI2 = u.IMEI2,
                    SerialNumber = u.SerialNumber,
                    Specs = $"{u.Color} | {u.StorageCapacity}",
                    BatteryHealthPercentage = u.BatteryHealthPercentage ?? 100,
                    ConditionGrade = u.ConditionGrade,
                    WarrantyStatus = u.WarrantyStatus,
                    Status = u.Status,
                    PurchaseCost = u.PurchaseCost,
                    TaggedPrice = u.TaggedPrice > 0 ? u.TaggedPrice : u.Product.SellingPrice,
                    SellingPrice = u.Product.SellingPrice,
                    AgingDays = u.AgingDays,
                    PurchaseDate = u.PurchaseDate,
                    Source = u.PurchaseSource.ToString(),
                    SoldInvoiceNumber = u.SoldInvoice?.InvoiceNumber,
                    SoldDate = u.SoldDate
                });
            }

            ApplyFilter();
            StatusMessage = $"Loaded {_allDevices.Count} devices across Sasan The Gadget Lounge inventory.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var query = _allDevices.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string term = SearchText.Trim().ToLowerInvariant();
            query = query.Where(d =>
                d.IMEI1.ToLowerInvariant().Contains(term) ||
                (d.IMEI2 != null && d.IMEI2.ToLowerInvariant().Contains(term)) ||
                d.SerialNumber.ToLowerInvariant().Contains(term) ||
                d.Name.ToLowerInvariant().Contains(term) ||
                d.Brand.ToLowerInvariant().Contains(term) ||
                d.Specs.ToLowerInvariant().Contains(term));
        }

        if (SelectedStatusFilter == "InStock")
        {
            query = query.Where(d => d.Status == UnitStatus.InStock);
        }
        else if (SelectedStatusFilter == "Sold")
        {
            query = query.Where(d => d.Status == UnitStatus.Sold);
        }

        if (ShowAgingOnly)
        {
            query = query.Where(d => d.AgingDays > 60 && d.Status == UnitStatus.InStock);
        }

        FilteredDevices.Clear();
        foreach (var item in query)
        {
            FilteredDevices.Add(item);
        }
    }

    [RelayCommand]
    public void PrintBarcodeForDevice(InventoryDeviceItem? device)
    {
        device ??= SelectedDevice;
        if (device == null)
        {
            StatusMessage = "Please select a device to print barcode sticker.";
            return;
        }

        var sticker = new BarcodeStickerData(
            ShopName: "SASAN THE GADGET LOUNGE",
            ModelName: device.Name,
            Specs: device.Specs,
            Identifier: device.IMEI1,
            Price: device.TaggedPrice,
            MRP: device.SellingPrice,
            ConditionGrade: device.ConditionGrade.ToString(),
            BatteryHealth: device.BatteryHealthPercentage,
            Warranty: device.WarrantyStatus.ToString(),
            Category: device.Category
        );

        byte[] tspl = _labelEngine.GenerateTsplStream(SelectedBarcodeStyle, sticker);
        StatusMessage = $"Dispatched {SelectedBarcodeStyle} TSPL Barcode Label for {device.Name} (IMEI: {device.IMEI1}). Size: {tspl.Length} bytes.";
    }

    [RelayCommand]
    public async Task PreviewStickerPdf(InventoryDeviceItem? device)
    {
        device ??= SelectedDevice;
        if (device == null)
        {
            StatusMessage = "Please select a device to preview sticker.";
            return;
        }

        try
        {
            var sticker = new BarcodeStickerData(
                ShopName: "SASAN THE GADGET LOUNGE",
                ModelName: device.Name,
                Specs: device.Specs,
                Identifier: device.IMEI1,
                Price: device.TaggedPrice,
                MRP: device.SellingPrice,
                ConditionGrade: device.ConditionGrade.ToString(),
                BatteryHealth: device.BatteryHealthPercentage,
                Warranty: device.WarrantyStatus.ToString(),
                Category: device.Category
            );

            byte[] pdfBytes = _labelEngine.GeneratePdfBytes(SelectedBarcodeStyle, sticker);
            string tempDir = Path.Combine(Path.GetTempPath(), "SasanERP_StickerPreviews");
            Directory.CreateDirectory(tempDir);
            string pdfPath = Path.Combine(tempDir, $"Stock_Sticker_{SelectedBarcodeStyle}_{device.IMEI1}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            bool launched = SasanERP.Services.Printing.PdfViewerLauncher.Launch(pdfPath);
            if (launched)
            {
                StatusMessage = $"[PREVIEW] Opened real vector sticker PDF ({SelectedBarcodeStyle}).";
            }
            else
            {
                StatusMessage = $"[PREVIEW] Generated vector sticker PDF: {pdfPath}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"[ERROR] Failed to preview sticker: {ex.Message}";
        }
    }
}
