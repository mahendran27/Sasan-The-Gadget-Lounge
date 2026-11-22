using CommunityToolkit.Mvvm.ComponentModel;
using SasanERP.Core.Enums;
using SasanERP.Core.Specifications;

namespace SasanERP.UI.ViewModels;

/// <summary>
/// Observable Line Item for the POS Billing Cart
/// </summary>
public partial class PosCartItemViewModel : ObservableObject
{
    public int ProductId { get; set; }
    public int? SerializedUnitId { get; set; }

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _hsnCode = "8517";

    [ObservableProperty]
    private string? _imei1;

    [ObservableProperty]
    private string? _imei2;

    [ObservableProperty]
    private string? _serialNumber;

    [ObservableProperty]
    private int? _batteryHealthPercentage;

    [ObservableProperty]
    private ConditionGrade? _conditionGrade;

    [ObservableProperty]
    private WarrantyStatus? _warrantyStatus;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _unitCostPrice;

    [ObservableProperty]
    private decimal _quantity = 1;

    [ObservableProperty]
    private decimal _discount;

    [ObservableProperty]
    private decimal _gstPercentage = 18.0m;

    [ObservableProperty]
    private bool _isMarginScheme;

    [ObservableProperty]
    private decimal _marginAmount;

    [ObservableProperty]
    private decimal _taxableValue;

    [ObservableProperty]
    private decimal _cgstAmount;

    [ObservableProperty]
    private decimal _sgstAmount;

    [ObservableProperty]
    private decimal _lineTotal;

    public bool IsSerialized => SerializedUnitId.HasValue;

    public string FormattedSpecs
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Imei1)) parts.Add($"IMEI: {Imei1}");
            if (!string.IsNullOrEmpty(SerialNumber)) parts.Add($"S/N: {SerialNumber}");
            if (BatteryHealthPercentage.HasValue) parts.Add($"Battery: {BatteryHealthPercentage}%");
            if (ConditionGrade.HasValue) parts.Add($"Grade: {ConditionGrade}");
            if (WarrantyStatus.HasValue) parts.Add($"Warranty: {WarrantyStatus}");
            return string.Join(" | ", parts);
        }
    }

    public void RecalculateTaxes(bool isInterstate = false)
    {
        decimal effectivePrice = Math.Max(0, (UnitPrice * Quantity) - Discount);

        if (IsMarginScheme)
        {
            // Rule 32(5) CGST Margin Scheme for Pre-Owned gadgets
            decimal totalCost = UnitCostPrice * Quantity;
            var marginBreakdown = GstCalculator.ComputeMarginScheme(effectivePrice, totalCost, GstPercentage, isInterstate);

            MarginAmount = marginBreakdown.GrossMargin;
            TaxableValue = marginBreakdown.TaxableMarginValue;
            CgstAmount = marginBreakdown.CgstAmount;
            SgstAmount = marginBreakdown.SgstAmount;
            LineTotal = effectivePrice;
        }
        else
        {
            // Standard Indian GST (Tax Inclusive retail price)
            var taxBreakdown = GstCalculator.ComputeTaxInclusive(effectivePrice, GstPercentage, isInterstate);

            TaxableValue = taxBreakdown.TaxableValue;
            CgstAmount = taxBreakdown.CgstAmount;
            SgstAmount = taxBreakdown.SgstAmount;
            LineTotal = effectivePrice;
        }
    }
}
