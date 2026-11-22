using SasanERP.Core.Enums;
using SasanERP.Core.Models;

namespace SasanERP.Services.Wireless;

/// <summary>
/// Unified Wireless Hardware and Tag Event Orchestrator.
/// Coordinates NFC Proximity Readers (13.56 MHz) and UHF RFID Readers (860-960 MHz),
/// dispatching tag tap events to POS Billing Cart and Stock Audit modules.
/// </summary>
public class UnifiedWirelessManager
{
    public NfcTagService NfcService { get; }
    public UhfRfidService UhfService { get; }

    public bool IsNfcReaderConnected { get; private set; } = true;
    public bool IsUhfReaderConnected { get; private set; } = true;

    public string ActiveNfcReaderName { get; private set; } = "ACS ACR122U USB NFC Reader (Simulated)";
    public string ActiveUhfReaderName { get; private set; } = "Impinj Indy R2000 UHF Gen2 Reader (Simulated)";

    public event Action<string, RfFrequencyType>? OnTagTappedForCheckout;
    public event Action<string>? OnStatusLogged;

    public UnifiedWirelessManager(NfcTagService nfcService, UhfRfidService uhfService)
    {
        NfcService = nfcService;
        UhfService = uhfService;

        // Forward UHF Tag detections to checkout if in single tag mode
        UhfService.TagDetected += tag =>
        {
            if (!string.IsNullOrEmpty(tag.IMEI))
            {
                OnTagTappedForCheckout?.Invoke(tag.IMEI, RfFrequencyType.UhfLongRange_860_960MHz);
            }
        };
    }

    /// <summary>
    /// Simulates a physical tap on the counter reader (e.g. from POS shortcut or test bench)
    /// </summary>
    public void SimulateCounterTap(string imei, RfFrequencyType frequency = RfFrequencyType.NfcProximity_13_56MHz)
    {
        OnStatusLogged?.Invoke($"[WIRELESS TAP] Tag detected ({frequency}) with identifier: {imei}");
        OnTagTappedForCheckout?.Invoke(imei, frequency);
    }
}
