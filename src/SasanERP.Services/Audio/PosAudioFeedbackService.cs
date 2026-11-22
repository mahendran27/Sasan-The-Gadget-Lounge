using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace SasanERP.Services.Audio;

public enum PosSoundType
{
    ScanSuccess,
    ScanError,
    CheckoutComplete,
    EasTheftAlarm
}

/// <summary>
/// Sub-10ms Acoustic Feedback Service for POS Counter & Warehouse Operations.
/// Generates crisp audio tones for barcode/NFC scans, error alerts, and triumphant checkout chimes.
/// Operates completely asynchronously to ensure zero UI thread latency.
/// </summary>
[SupportedOSPlatform("windows")]
public class PosAudioFeedbackService
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MessageBeep(uint uType);

    private const uint MB_OK = 0x00000000;
    private const uint MB_ICONHAND = 0x00000010;
    private const uint MB_ICONEXCLAMATION = 0x00000030;
    private const uint MB_ICONASTERISK = 0x00000040;

    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Plays affirmative high-frequency chirp for successful barcode / IMEI scan.
    /// </summary>
    public void PlayScanSuccess()
    {
        if (!IsEnabled) return;
        Task.Run(() =>
        {
            try
            {
                Console.Beep(1850, 75);
            }
            catch
            {
                MessageBeep(MB_ICONASTERISK);
            }
        });
    }

    /// <summary>
    /// Plays low warning buzz for duplicate scans, invalid IMEIs, or stock depletion.
    /// </summary>
    public void PlayScanError()
    {
        if (!IsEnabled) return;
        Task.Run(() =>
        {
            try
            {
                Console.Beep(420, 180);
            }
            catch
            {
                MessageBeep(MB_ICONHAND);
            }
        });
    }

    /// <summary>
    /// Plays ascending 2-tone melodic chime when bill is finalized and payment is captured.
    /// </summary>
    public void PlayCheckoutComplete()
    {
        if (!IsEnabled) return;
        Task.Run(() =>
        {
            try
            {
                Console.Beep(1046, 80); // C6
                Thread.Sleep(20);
                Console.Beep(1318, 120); // E6
            }
            catch
            {
                MessageBeep(MB_ICONEXCLAMATION);
            }
        });
    }

    /// <summary>
    /// Plays urgent pulsing alarm tone when EAS theft bit is detected on unauthorized item.
    /// </summary>
    public void PlayEasAlarmWarning()
    {
        if (!IsEnabled) return;
        Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    Console.Beep(2400, 100);
                    Console.Beep(900, 100);
                }
            }
            catch
            {
                MessageBeep(MB_ICONHAND);
            }
        });
    }

    /// <summary>
    /// Dispatches appropriate sound based on POS event enum.
    /// </summary>
    public void Play(PosSoundType soundType)
    {
        switch (soundType)
        {
            case PosSoundType.ScanSuccess: PlayScanSuccess(); break;
            case PosSoundType.ScanError: PlayScanError(); break;
            case PosSoundType.CheckoutComplete: PlayCheckoutComplete(); break;
            case PosSoundType.EasTheftAlarm: PlayEasAlarmWarning(); break;
        }
    }
}
