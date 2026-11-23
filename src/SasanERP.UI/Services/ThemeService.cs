using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using SasanERP.Core.Enums;
using Wpf.Ui.Appearance;

namespace SasanERP.UI.Services;

/// <summary>
/// Dynamic UI Theme and Ergonomic Display Mode Manager (Choices 1 to 6).
/// Seamlessly switches between Dark, Light, Mica/Frost, Kiosk Touch, Terminal HUD, and Adaptive Day/Night.
/// </summary>
public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    private DispatcherTimer? _adaptiveTimer;
    private AppThemeMode _currentMode = AppThemeMode.ElectricCyberCyan;

    public AppThemeMode CurrentMode => _currentMode;

    public event Action<AppThemeMode>? ThemeChanged;

    public void ApplyTheme(AppThemeMode mode)
    {
        _currentMode = mode;

        if (mode == AppThemeMode.AdaptiveDayNightShift)
        {
            StartAdaptiveTimer();
            ApplyAdaptiveDayNight();
        }
        else
        {
            StopAdaptiveTimer();
            ApplyModeInternal(mode);
        }

        ThemeChanged?.Invoke(_currentMode);
    }

    private void ApplyModeInternal(AppThemeMode mode)
    {
        var app = Application.Current;
        if (app == null) return;

        switch (mode)
        {
            case AppThemeMode.ElectricCyberCyan: // Choice 1: Dark Obsidian + Cyan + Amber
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                SetBrush(app, "SasanPrimaryBrush", "#08101E");
                SetBrush(app, "SasanCyanAccentBrush", "#00E5FF");
                SetBrush(app, "SasanAmberAccentBrush", "#FF9100");
                SetBrush(app, "SasanCardBgBrush", "#101D33");
                SetBrush(app, "SasanBorderBrush", "#1F3354");
                SetBrush(app, "SasanForegroundBrush", "#F8FAFC");
                SetBrush(app, "SasanSubTextBrush", "#94A3B8");
                SetBrush(app, "SasanNavBgBrush", "#070E1A");
                SetBrush(app, "SasanInputBgBrush", "#0A1424");
                SetBrush(app, "SasanHeaderBgBrush", "#070E1A");
                break;

            case AppThemeMode.WarmTitaniumQuartz: // Choice 2: Architectural Light
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                SetBrush(app, "SasanPrimaryBrush", "#F1F5F9");
                SetBrush(app, "SasanCyanAccentBrush", "#0284C7");
                SetBrush(app, "SasanAmberAccentBrush", "#D97706");
                SetBrush(app, "SasanCardBgBrush", "#FFFFFF");
                SetBrush(app, "SasanBorderBrush", "#CBD5E1");
                SetBrush(app, "SasanForegroundBrush", "#0F172A");
                SetBrush(app, "SasanSubTextBrush", "#64748B");
                SetBrush(app, "SasanNavBgBrush", "#E2E8F0");
                SetBrush(app, "SasanInputBgBrush", "#FFFFFF");
                SetBrush(app, "SasanHeaderBgBrush", "#F1F5F9");
                break;

            case AppThemeMode.NordicFrost: // Choice 3: Acrylic Frost / Sky Blue Glow
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                SetBrush(app, "SasanPrimaryBrush", "#0F172A");
                SetBrush(app, "SasanCyanAccentBrush", "#38BDF8");
                SetBrush(app, "SasanAmberAccentBrush", "#A855F7");
                SetBrush(app, "SasanCardBgBrush", "#1E293B");
                SetBrush(app, "SasanBorderBrush", "#38BDF8");
                SetBrush(app, "SasanForegroundBrush", "#F0FDF4");
                SetBrush(app, "SasanSubTextBrush", "#94A3B8");
                SetBrush(app, "SasanNavBgBrush", "#0B132B");
                SetBrush(app, "SasanInputBgBrush", "#1E293B");
                SetBrush(app, "SasanHeaderBgBrush", "#0F172A");
                break;

            case AppThemeMode.KioskTouchMode: // Choice 4: High Contrast Touch POS (Large buttons, emerald accents)
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                SetBrush(app, "SasanPrimaryBrush", "#000000");
                SetBrush(app, "SasanCyanAccentBrush", "#10B981");
                SetBrush(app, "SasanAmberAccentBrush", "#F59E0B");
                SetBrush(app, "SasanCardBgBrush", "#18181B");
                SetBrush(app, "SasanBorderBrush", "#3F3F46");
                SetBrush(app, "SasanForegroundBrush", "#FFFFFF");
                SetBrush(app, "SasanSubTextBrush", "#A1A1AA");
                SetBrush(app, "SasanNavBgBrush", "#09090B");
                SetBrush(app, "SasanInputBgBrush", "#18181B");
                SetBrush(app, "SasanHeaderBgBrush", "#000000");
                break;

            case AppThemeMode.TerminalSpeedHud: // Choice 5: Bloomberg CRT Terminal HUD (Neon green on black)
                ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                SetBrush(app, "SasanPrimaryBrush", "#050505");
                SetBrush(app, "SasanCyanAccentBrush", "#00FF66");
                SetBrush(app, "SasanAmberAccentBrush", "#FFB000");
                SetBrush(app, "SasanCardBgBrush", "#0D0D0D");
                SetBrush(app, "SasanBorderBrush", "#1F2937");
                SetBrush(app, "SasanForegroundBrush", "#00FF66");
                SetBrush(app, "SasanSubTextBrush", "#4ADE80");
                SetBrush(app, "SasanNavBgBrush", "#000000");
                SetBrush(app, "SasanInputBgBrush", "#0D0D0D");
                SetBrush(app, "SasanHeaderBgBrush", "#050505");
                break;
        }
    }

    private void ApplyAdaptiveDayNight()
    {
        int hour = DateTime.Now.Hour;
        // Day mode: 08:00 to 18:00 (Light), Night mode: 18:00 to 08:00 (Dark Cyber Cyan)
        if (hour >= 8 && hour < 18)
        {
            ApplyModeInternal(AppThemeMode.WarmTitaniumQuartz);
        }
        else
        {
            ApplyModeInternal(AppThemeMode.ElectricCyberCyan);
        }
    }

    private void StartAdaptiveTimer()
    {
        if (_adaptiveTimer == null)
        {
            _adaptiveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(10)
            };
            _adaptiveTimer.Tick += (s, e) => ApplyAdaptiveDayNight();
        }
        _adaptiveTimer.Start();
    }

    private void StopAdaptiveTimer()
    {
        _adaptiveTimer?.Stop();
    }

    private static void SetBrush(Application app, string resourceKey, string hexColor)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            app.Resources[resourceKey] = new SolidColorBrush(color);
        }
        catch
        {
            // Fallback gracefully
        }
    }
}
