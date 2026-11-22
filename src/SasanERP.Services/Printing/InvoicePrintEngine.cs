using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Services.Barcode;
using SasanERP.Services.Printing.Templates;

namespace SasanERP.Services.Printing;

/// <summary>
/// Unified Invoice Print Engine Factory.
/// Dispatches QuestPDF generation across all 7 Invoice Template Styles.
/// </summary>
public class InvoicePrintEngine
{
    private readonly BarcodeGeneratorService _barcodeService;

    public InvoicePrintEngine(BarcodeGeneratorService barcodeService)
    {
        _barcodeService = barcodeService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Creates the appropriate QuestPDF IDocument for the chosen template style
    /// </summary>
    public IDocument CreateDocument(InvoiceTemplateStyle style, Invoice invoice, CompanyProfile company)
    {
        return style switch
        {
            InvoiceTemplateStyle.ExecutiveMinimalist => new ExecutiveMinimalistDocument(invoice, company, _barcodeService),
            InvoiceTemplateStyle.LuxuryGadgetLounge => new LuxuryGadgetLoungeDocument(invoice, company, _barcodeService),
            InvoiceTemplateStyle.Rule32_5_MarginSchemeAudit => new Rule32_5_MarginSchemeAuditDocument(invoice, company, _barcodeService),
            InvoiceTemplateStyle.ThermalRoll_80mm => new ThermalRoll80mmDocument(invoice, company, _barcodeService),
            InvoiceTemplateStyle.DualCopyCounterfoil => new DualCopyCounterfoilDocument(invoice, company, _barcodeService),
            InvoiceTemplateStyle.EcoDigitalWhatsApp => new EcoDigitalWhatsAppDocument(invoice, company, _barcodeService),
            InvoiceTemplateStyle.DeviceHealthCertificate => new DeviceHealthCertificateDocument(invoice, company, _barcodeService),
            InvoiceTemplateStyle.ModernNeoBrutalist => new ModernNeoBrutalistDocument(invoice, company, _barcodeService),
            InvoiceTemplateStyle.ClassicCorporateGst => new ClassicCorporateGstDocument(invoice, company, _barcodeService),
            InvoiceTemplateStyle.DarkCyberLounge => new DarkCyberLoungeDocument(invoice, company, _barcodeService),
            InvoiceTemplateStyle.CompactTechDocket => new CompactTechDocketDocument(invoice, company, _barcodeService),
            InvoiceTemplateStyle.EliteBoutiqueWatermark => new EliteBoutiqueWatermarkDocument(invoice, company, _barcodeService),
            _ => new LuxuryGadgetLoungeDocument(invoice, company, _barcodeService)
        };
    }

    /// <summary>
    /// Renders the requested invoice template style into in-memory PDF bytes
    /// </summary>
    public byte[] GeneratePdfBytes(InvoiceTemplateStyle style, Invoice invoice, CompanyProfile company)
    {
        var doc = CreateDocument(style, invoice, company);
        return doc.GeneratePdf();
    }

    /// <summary>
    /// Generates and writes the PDF directly to disk
    /// </summary>
    public void SavePdf(InvoiceTemplateStyle style, Invoice invoice, CompanyProfile company, string filePath)
    {
        var doc = CreateDocument(style, invoice, company);
        doc.GeneratePdf(filePath);
    }
}
