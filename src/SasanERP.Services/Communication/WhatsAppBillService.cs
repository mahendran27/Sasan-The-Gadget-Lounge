using System.Diagnostics;
using System.Text;
using System.Text.Json;
using SasanERP.Core.Models;

namespace SasanERP.Services.Communication;

/// <summary>
/// WhatsApp Invoice Dispatch & Customer Communication Engine for Sasan The Gadget Lounge.
/// Generates deep-link URIs for WhatsApp Web / Desktop and prepares Meta Cloud API JSON payloads
/// for zero-cost instant paperless digital delivery of invoices and warranty certificates.
/// </summary>
public class WhatsAppBillService
{
    /// <summary>
    /// Normalizes Indian phone numbers into international E.164 format without '+' (e.g. 919080803070).
    /// </summary>
    public static string NormalizeIndianPhoneNumber(string rawPhone)
    {
        if (string.IsNullOrWhiteSpace(rawPhone)) return string.Empty;
        var digits = new string(rawPhone.Where(char.IsDigit).ToArray());

        if (digits.Length == 10) return "91" + digits;
        if (digits.Length == 11 && digits.StartsWith("0")) return "91" + digits[1..];
        if (digits.Length == 12 && digits.StartsWith("91")) return digits;

        return digits;
    }

    /// <summary>
    /// Generates a luxury branded text message for the customer invoice.
    /// </summary>
    public string GenerateInvoiceTextMessage(Invoice invoice, CompanyProfile company, string? cloudPdfUrl = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🌟 *{company.BusinessName.ToUpperInvariant()}* 🌟");
        sb.AppendLine($"📍 {company.AddressLine1}, {company.City} | 📞 {company.PhoneNumber}");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"வணக்கம் *{invoice.Customer?.FullName ?? "Valued Customer"}*, thank you for choosing Sasan The Gadget Lounge!");
        sb.AppendLine();
        sb.AppendLine($"🧾 *Tax Invoice #:* {invoice.InvoiceNumber}");
        sb.AppendLine($"📅 *Billed On:* {invoice.InvoiceDate.ToLocalTime():dd-MMM-yyyy hh:mm tt}");
        sb.AppendLine();
        sb.AppendLine("📱 *GADGET DETAILS:*");

        foreach (var item in invoice.Items)
        {
            string desc = item.Product?.Name ?? (!string.IsNullOrWhiteSpace(item.ItemDescription) ? item.ItemDescription : "Premium Gadget");
            string serialNote = !string.IsNullOrWhiteSpace(item.IMEI1) ? $" [IMEI: {item.IMEI1}]" : "";
            sb.AppendLine($"• {item.Quantity}x {desc}{serialNote} - *₹{item.LineTotal:N0}*");
        }

        sb.AppendLine();
        sb.AppendLine($"💰 *Grand Total:* ₹{invoice.GrandTotal:N2}");
        sb.AppendLine($"💵 *Amount Paid:* ₹{invoice.AmountPaid:N2}");
        if (invoice.BalanceDue > 0)
        {
            sb.AppendLine($"⚠️ *Balance Due:* ₹{invoice.BalanceDue:N2}");
        }

        sb.AppendLine("🛡️ *Warranty:* Certified Sasan Premium Warranty Included.");

        if (!string.IsNullOrWhiteSpace(cloudPdfUrl))
        {
            sb.AppendLine();
            sb.AppendLine($"📥 *Download PDF Invoice:* {cloudPdfUrl}");
        }

        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"_{company.Tagline}_");
        return sb.ToString();
    }

    /// <summary>
    /// Generates direct click-to-chat WhatsApp deep link URI.
    /// </summary>
    public string GenerateWhatsAppDeepLink(string customerPhone, Invoice invoice, CompanyProfile company, string? cloudPdfUrl = null)
    {
        string normPhone = NormalizeIndianPhoneNumber(customerPhone);
        string text = GenerateInvoiceTextMessage(invoice, company, cloudPdfUrl);
        return $"https://api.whatsapp.com/send?phone={normPhone}&text={Uri.EscapeDataString(text)}";
    }

    /// <summary>
    /// Launches WhatsApp Web or Windows Desktop app with the prefilled message.
    /// </summary>
    public bool LaunchWhatsAppInBrowserOrApp(string customerPhone, Invoice invoice, CompanyProfile company, string? cloudPdfUrl = null)
    {
        try
        {
            string link = GenerateWhatsAppDeepLink(customerPhone, invoice, company, cloudPdfUrl);
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates Meta WhatsApp Business Cloud API JSON payload for automated webhook/backend dispatch.
    /// </summary>
    public string GenerateMetaCloudApiPayload(string customerPhone, Invoice invoice, CompanyProfile company, string pdfMediaUrl)
    {
        string normPhone = NormalizeIndianPhoneNumber(customerPhone);
        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = normPhone,
            type = "document",
            document = new
            {
                link = pdfMediaUrl,
                caption = $"Invoice #{invoice.InvoiceNumber} from {company.BusinessName} (Total: ₹{invoice.GrandTotal:N2})",
                filename = $"{invoice.InvoiceNumber}.pdf"
            }
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }
}
