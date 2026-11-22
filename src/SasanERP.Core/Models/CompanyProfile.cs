using SasanERP.Core.Enums;

namespace SasanERP.Core.Models;

/// <summary>
/// Business Profile and Statutory Configuration for Sasan The Gadget Lounge
/// </summary>
public class CompanyProfile
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = "Sasan The Gadget Lounge";
    public string Tagline { get; set; } = "Premium New & Pre-Owned Gadgets, Laptops & Accessories";
    public string AddressLine1 { get; set; } = "106/7, Karthik Mahall, Subban St";
    public string AddressLine2 { get; set; } = "Opposite Theni, Suppan Ragavan Colony, NRT Nagar";
    public string City { get; set; } = "Theni";
    public string State { get; set; } = "Tamil Nadu";
    public string Pincode { get; set; } = "625531";
    public string StateCode { get; set; } = "33"; // Tamil Nadu GST State Code
    public string PhoneNumber { get; set; } = "9080803070";
    public string? Email { get; set; } = "sasanthegadgetlounge@gmail.com";
    public string? GSTIN { get; set; } = "33ABCDE1234F1Z5"; // Configurable GSTIN
    public string? BankName { get; set; } = "State Bank of India";
    public string? BankAccountNumber { get; set; } = "38920192831";
    public string? BankIFSC { get; set; } = "SBIN0000932";
    public string? BankBranch { get; set; } = "Theni Main Branch";
    public string? UpiId { get; set; } = "9080803070@upi"; // For dynamic UPI QR generation
    public string InvoicePrefix { get; set; } = "SGL";
    public string? LogoPath { get; set; } = "Assets/logo.png"; // Bundled circular logo
    public bool IsMonochromePrintMode { get; set; } = false; // Toggle for Color vs Black & White laser print
    
    // Configured System Preferences (Choices 1-12, 1-6, 1-7)
    public InvoiceTemplateStyle DefaultInvoiceTemplate { get; set; } = InvoiceTemplateStyle.LuxuryGadgetLounge;
    public AppThemeMode DefaultThemeMode { get; set; } = AppThemeMode.ElectricCyberCyan;
    public BarcodeLabelStyle DefaultBarcodeStyle { get; set; } = BarcodeLabelStyle.Flagship_50x30mm;

    public string TermsAndConditions { get; set; } = 
        "1. Goods once sold will be accepted for return only under shop warranty terms.\n" +
        "2. Brand warranty items must be claimed directly through authorized service centers.\n" +
        "3. Second-hand / pre-owned gadgets carry shop testing warranty as stated on this invoice.\n" +
        "4. Physical or liquid damage is strictly excluded from all warranties.\n" +
        "5. Disputes are subject to Theni jurisdiction only.";
}
