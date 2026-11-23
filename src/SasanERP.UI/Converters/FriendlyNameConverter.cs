using System;
using System.Globalization;
using System.Windows.Data;
using SasanERP.Core.Enums;

namespace SasanERP.UI.Converters;

/// <summary>
/// Converts technical enum identifiers into elegant, concise human-readable names for UI display.
/// Prevents ComboBox text clipping and eliminates ellipsis truncation across all display resolutions.
/// </summary>
public class FriendlyNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AppThemeMode theme)
        {
            return theme switch
            {
                AppThemeMode.ElectricCyberCyan => "Cyber Cyan (Dark)",
                AppThemeMode.WarmTitaniumQuartz => "Titanium Quartz",
                AppThemeMode.NordicFrost => "Nordic Frost",
                AppThemeMode.KioskTouchMode => "Kiosk Touch",
                AppThemeMode.TerminalSpeedHud => "Terminal HUD",
                AppThemeMode.AdaptiveDayNightShift => "Auto Day/Night",
                _ => theme.ToString()
            };
        }

        if (value is InvoiceTemplateStyle style)
        {
            return style switch
            {
                InvoiceTemplateStyle.LuxuryGadgetLounge => "Luxury Gadget Lounge",
                InvoiceTemplateStyle.Rule32_5_MarginSchemeAudit => "Rule 32(5) Audit",
                InvoiceTemplateStyle.DarkCyberLounge => "Dark Cyber Lounge",
                InvoiceTemplateStyle.ModernNeoBrutalist => "Modern Neo-Brutalist",
                InvoiceTemplateStyle.ClassicCorporateGst => "Classic Corporate",
                InvoiceTemplateStyle.ThermalRoll_80mm => "Thermal 80mm Roll",
                InvoiceTemplateStyle.EcoDigitalWhatsApp => "Digital WhatsApp",
                InvoiceTemplateStyle.DeviceHealthCertificate => "Health Certificate",
                InvoiceTemplateStyle.DualCopyCounterfoil => "Dual Counterfoil",
                InvoiceTemplateStyle.CompactTechDocket => "Compact Docket",
                InvoiceTemplateStyle.EliteBoutiqueWatermark => "Boutique Watermark",
                InvoiceTemplateStyle.ExecutiveMinimalist => "Executive A4",
                _ => style.ToString()
            };
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
