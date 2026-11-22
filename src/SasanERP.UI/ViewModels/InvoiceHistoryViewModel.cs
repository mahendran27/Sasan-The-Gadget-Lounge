using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Services.Communication;
using SasanERP.Services.Invoicing;

namespace SasanERP.UI.ViewModels;

/// <summary>
/// ViewModel for searching past sales invoices, inspecting device IMEI telemetry,
/// re-printing receipts, exporting vector PDFs across all 12 templates, and WhatsApp sharing.
/// </summary>
public partial class InvoiceHistoryViewModel : ObservableObject
{
    private readonly InvoiceHistoryService _historyService;
    private readonly WhatsAppBillService? _whatsAppService;

    public ObservableCollection<InvoiceSummaryDto> Invoices { get; } = new();

    public ObservableCollection<InvoiceTemplateStyle> AvailableTemplates { get; } =
        new(Enum.GetValues<InvoiceTemplateStyle>());

    [ObservableProperty]
    private InvoiceSummaryDto? _selectedSummary;

    partial void OnSelectedSummaryChanged(InvoiceSummaryDto? value)
    {
        if (value != null)
        {
            _ = LoadDetailsAsync(value.Id);
        }
        else
        {
            SelectedInvoiceDetails = null;
        }
    }

    [ObservableProperty]
    private Invoice? _selectedInvoiceDetails;

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        _ = SearchInvoicesAsync();
    }

    [ObservableProperty]
    private InvoiceTemplateStyle _selectedTemplate = InvoiceTemplateStyle.LuxuryGadgetLounge;

    [ObservableProperty]
    private string _statusMessage = "Ready. Search by Invoice #, Customer Phone, Name, or IMEI.";

    [ObservableProperty]
    private bool _isBusy;

    public InvoiceHistoryViewModel(InvoiceHistoryService historyService, WhatsAppBillService? whatsAppService = null)
    {
        _historyService = historyService;
        _whatsAppService = whatsAppService;
    }

    public async Task InitializeAsync()
    {
        await SearchInvoicesAsync();
    }

    [RelayCommand]
    public async Task SearchInvoicesAsync()
    {
        IsBusy = true;
        try
        {
            var results = await _historyService.SearchInvoicesAsync(SearchText);
            Invoices.Clear();
            foreach (var inv in results)
            {
                Invoices.Add(inv);
            }

            if (Invoices.Any() && SelectedSummary == null)
            {
                SelectedSummary = Invoices.First();
            }

            StatusMessage = $"Found {Invoices.Count} invoices matching '{SearchText}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadDetailsAsync(int invoiceId)
    {
        IsBusy = true;
        try
        {
            SelectedInvoiceDetails = await _historyService.GetInvoiceDetailsAsync(invoiceId);
            StatusMessage = SelectedInvoiceDetails != null
                ? $"Loaded Invoice #{SelectedInvoiceDetails.InvoiceNumber} with {SelectedInvoiceDetails.Items.Count} items."
                : "Invoice details not found.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Launches WhatsApp with branded invoice text and luxury gadget details
    /// </summary>
    [RelayCommand]
    public async Task ShareViaWhatsAppAsync()
    {
        if (SelectedInvoiceDetails == null)
        {
            StatusMessage = "Please select an invoice to send via WhatsApp.";
            return;
        }

        string phone = SelectedInvoiceDetails.Customer?.PhoneNumber ?? string.Empty;
        if (string.IsNullOrWhiteSpace(phone))
        {
            StatusMessage = "Customer phone number is missing on this invoice.";
            return;
        }

        IsBusy = true;
        try
        {
            var company = await _historyService.GetCompanyProfileAsync();
            var service = _whatsAppService ?? new WhatsAppBillService();
            bool launched = service.LaunchWhatsAppInBrowserOrApp(phone, SelectedInvoiceDetails, company);
            StatusMessage = launched
                ? $"[WHATSAPP LAUNCHED] Message opened for customer {phone} (#{SelectedInvoiceDetails.InvoiceNumber})."
                : $"[WHATSAPP LINK] Generated link for customer {phone}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"WhatsApp dispatch error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Re-prints the selected invoice or dispatches it to default print spooler
    /// </summary>
    [RelayCommand]
    public async Task ReprintInvoiceAsync()
    {
        if (SelectedInvoiceDetails == null)
        {
            StatusMessage = "Please select an invoice to re-print.";
            return;
        }

        IsBusy = true;
        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SasanERP_Reprint");
            Directory.CreateDirectory(tempDir);
            string pdfPath = Path.Combine(tempDir, $"{SelectedInvoiceDetails.InvoiceNumber.Replace('/', '_')}_Reprint.pdf");

            await _historyService.ExportPdfAsync(SelectedInvoiceDetails, SelectedTemplate, pdfPath);

            // Open in default PDF printer / viewer for physical dispatch
            Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            StatusMessage = $"[RE-PRINT DISPATCHED] Invoice #{SelectedInvoiceDetails.InvoiceNumber} opened for printing ({SelectedTemplate}).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[PRINT ERROR] Failed to re-print: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Opens a Save File Dialog allowing the merchant to export the invoice PDF to any chosen location
    /// </summary>
    [RelayCommand]
    public async Task ExportPdfAsync()
    {
        if (SelectedInvoiceDetails == null)
        {
            StatusMessage = "Please select an invoice to export.";
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = $"Export Invoice #{SelectedInvoiceDetails.InvoiceNumber} as PDF",
            Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*",
            FileName = $"{SelectedInvoiceDetails.InvoiceNumber.Replace('/', '_')}_{SelectedTemplate}.pdf",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (saveDialog.ShowDialog() == true)
        {
            IsBusy = true;
            try
            {
                string targetPath = saveDialog.FileName;
                await _historyService.ExportPdfAsync(SelectedInvoiceDetails, SelectedTemplate, targetPath);
                StatusMessage = $"[EXPORT SUCCESS] PDF saved successfully to: {targetPath}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"[EXPORT ERROR] Failed to export PDF: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    /// <summary>
    /// Opens the vector PDF directly in the operating system viewer
    /// </summary>
    [RelayCommand]
    public async Task OpenPdfPreviewAsync()
    {
        if (SelectedInvoiceDetails == null)
        {
            StatusMessage = "Please select an invoice to view preview.";
            return;
        }

        IsBusy = true;
        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "SasanERP_Preview");
            Directory.CreateDirectory(tempDir);
            string pdfPath = Path.Combine(tempDir, $"{SelectedInvoiceDetails.InvoiceNumber.Replace('/', '_')}_Preview.pdf");

            await _historyService.ExportPdfAsync(SelectedInvoiceDetails, SelectedTemplate, pdfPath);
            Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
            StatusMessage = $"[PREVIEW OPENED] Showing #{SelectedInvoiceDetails.InvoiceNumber} in vector viewer.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"[PREVIEW ERROR] Failed to open preview: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
