using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;
using SasanERP.Services.Printing;
using System.Collections.ObjectModel;
using System.IO;

namespace SasanERP.UI.ViewModels;

/// <summary>
/// ViewModel for editing and managing Sasan The Gadget Lounge company profile,
/// physical address, GSTIN, banking details, logo, and invoice printing preferences.
/// </summary>
public partial class CompanySettingsViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    public ObservableCollection<InvoiceTemplateStyle> AvailableTemplates { get; } =
        new(Enum.GetValues<InvoiceTemplateStyle>());

    public ObservableCollection<AppThemeMode> AvailableThemes { get; } =
        new(Enum.GetValues<AppThemeMode>());

    public ObservableCollection<BarcodeLabelStyle> AvailableBarcodeStyles { get; } =
        new(Enum.GetValues<BarcodeLabelStyle>());

    [ObservableProperty] private int _profileId;
    [ObservableProperty] private string _businessName = "Sasan The Gadget Lounge";
    [ObservableProperty] private string _tagline = "Tech. Comfort. Innovation.";
    [ObservableProperty] private string _addressLine1 = "106/7, Karthik Mahall, Subban St";
    [ObservableProperty] private string _addressLine2 = "Opposite Theni, Suppan Ragavan Colony, NRT Nagar";
    [ObservableProperty] private string _city = "Theni";
    [ObservableProperty] private string _state = "Tamil Nadu";
    [ObservableProperty] private string _pincode = "625531";
    [ObservableProperty] private string _stateCode = "33";
    [ObservableProperty] private string _phoneNumber = "9080803070";
    [ObservableProperty] private string? _email = "sasanthegadgetlounge@gmail.com";
    [ObservableProperty] private string? _gstin = "33AAECS4821N1ZH";
    [ObservableProperty] private string? _bankName = "State Bank of India";
    [ObservableProperty] private string? _bankAccountNumber = "38920192831";
    [ObservableProperty] private string? _bankIFSC = "SBIN0000932";
    [ObservableProperty] private string? _bankBranch = "Theni NRT Nagar";
    [ObservableProperty] private string? _upiId = "9080803070@sbi";
    [ObservableProperty] private string _invoicePrefix = "SGL";
    [ObservableProperty] private string? _logoPath = "Assets/logo.png";
    [ObservableProperty] private bool _isMonochromePrintMode = false;
    [ObservableProperty] private InvoiceTemplateStyle _defaultInvoiceTemplate = InvoiceTemplateStyle.LuxuryGadgetLounge;
    [ObservableProperty] private AppThemeMode _defaultThemeMode = AppThemeMode.ElectricCyberCyan;
    [ObservableProperty] private BarcodeLabelStyle _defaultBarcodeStyle = BarcodeLabelStyle.Flagship_50x30mm;
    [ObservableProperty] private string _termsAndConditions = string.Empty;
    [ObservableProperty] private string _statusMessage = "Ready. You can edit any store details and click Save.";
    [ObservableProperty] private bool _isBusy;

    public CompanySettingsViewModel(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Loads company profile from database
    /// </summary>
    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var profile = await _context.CompanyProfiles.FirstOrDefaultAsync();
            if (profile != null)
            {
                ProfileId = profile.Id;
                BusinessName = profile.BusinessName;
                Tagline = profile.Tagline;
                AddressLine1 = profile.AddressLine1;
                AddressLine2 = profile.AddressLine2;
                City = profile.City;
                State = profile.State;
                Pincode = profile.Pincode;
                StateCode = profile.StateCode;
                PhoneNumber = profile.PhoneNumber;
                Email = profile.Email;
                Gstin = profile.GSTIN;
                BankName = profile.BankName;
                BankAccountNumber = profile.BankAccountNumber;
                BankIFSC = profile.BankIFSC;
                BankBranch = profile.BankBranch;
                UpiId = profile.UpiId;
                InvoicePrefix = profile.InvoicePrefix;
                LogoPath = profile.LogoPath ?? "Assets/logo.png";
                IsMonochromePrintMode = profile.IsMonochromePrintMode;
                DefaultInvoiceTemplate = profile.DefaultInvoiceTemplate;
                DefaultThemeMode = profile.DefaultThemeMode;
                DefaultBarcodeStyle = profile.DefaultBarcodeStyle;
                TermsAndConditions = profile.TermsAndConditions;

                StatusMessage = "Shop Profile & Store Address loaded successfully.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Saves all changes to SQLite database and resets logo cache
    /// </summary>
    [RelayCommand]
    public async Task SaveCompanyProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(BusinessName))
        {
            StatusMessage = "[ERROR] Business Name cannot be empty!";
            return;
        }

        IsBusy = true;
        try
        {
            var profile = await _context.CompanyProfiles.FirstOrDefaultAsync();
            if (profile == null)
            {
                profile = new CompanyProfile();
                _context.CompanyProfiles.Add(profile);
            }

            profile.BusinessName = BusinessName.Trim();
            profile.Tagline = Tagline?.Trim() ?? string.Empty;
            profile.AddressLine1 = AddressLine1.Trim();
            profile.AddressLine2 = AddressLine2.Trim();
            profile.City = City.Trim();
            profile.State = State.Trim();
            profile.Pincode = Pincode.Trim();
            profile.StateCode = StateCode.Trim();
            profile.PhoneNumber = PhoneNumber.Trim();
            profile.Email = Email?.Trim();
            profile.GSTIN = Gstin?.Trim().ToUpper();
            profile.BankName = BankName?.Trim();
            profile.BankAccountNumber = BankAccountNumber?.Trim();
            profile.BankIFSC = BankIFSC?.Trim().ToUpper();
            profile.BankBranch = BankBranch?.Trim();
            profile.UpiId = UpiId?.Trim();
            profile.InvoicePrefix = InvoicePrefix?.Trim() ?? "SGL";
            profile.LogoPath = LogoPath?.Trim();
            profile.IsMonochromePrintMode = IsMonochromePrintMode;
            profile.DefaultInvoiceTemplate = DefaultInvoiceTemplate;
            profile.DefaultThemeMode = DefaultThemeMode;
            profile.DefaultBarcodeStyle = DefaultBarcodeStyle;
            profile.TermsAndConditions = TermsAndConditions;

            await _context.SaveChangesAsync();

            // Clear in-memory logo cache so invoices immediately reflect changes
            LogoProvider.ResetCache();

            StatusMessage = "நிறுவனத்தின் முகவரி மற்றும் விவரங்கள் வெற்றிகரமாகச் சேமிக்கப்பட்டன! (Store details saved successfully).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[ERROR] Failed to save settings: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Opens file browser dialog allowing user to choose custom shop logo
    /// </summary>
    [RelayCommand]
    public void BrowseLogoFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Shop Logo Image",
            Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*",
            InitialDirectory = AppContext.BaseDirectory
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string chosenPath = openFileDialog.FileName;
            LogoPath = chosenPath;
            LogoProvider.ResetCache();
            StatusMessage = $"Custom logo selected: {Path.GetFileName(chosenPath)}. Click Save to persist.";
        }
    }

    /// <summary>
    /// Resets address to canonical Sasan The Gadget Lounge Theni address
    /// </summary>
    [RelayCommand]
    public void ResetToDefaultTheniAddress()
    {
        BusinessName = "Sasan The Gadget Lounge";
        Tagline = "Tech. Comfort. Innovation.";
        AddressLine1 = "106/7, Karthik Mahall, Subban St";
        AddressLine2 = "Opposite Theni, Suppan Ragavan Colony, NRT Nagar";
        City = "Theni";
        State = "Tamil Nadu";
        Pincode = "625531";
        StateCode = "33";
        PhoneNumber = "9080803070";
        Email = "sasanthegadgetlounge@gmail.com";
        Gstin = "33AAECS4821N1ZH";
        BankName = "State Bank of India";
        BankAccountNumber = "38920192831";
        BankIFSC = "SBIN0000932";
        BankBranch = "Theni NRT Nagar";
        UpiId = "9080803070@sbi";
        LogoPath = "Assets/logo.png";
        IsMonochromePrintMode = false;
        StatusMessage = "Address fields reset to official Theni location. Click Save to persist.";
    }
}
