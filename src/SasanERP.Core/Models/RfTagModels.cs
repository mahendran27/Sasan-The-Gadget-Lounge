using SasanERP.Core.Enums;

namespace SasanERP.Core.Models;

/// <summary>
/// Data payload stored on an NFC (13.56 MHz) NTAG213/215/216 chip
/// </summary>
public class NfcTagModel
{
    public string TagUid { get; set; } = string.Empty;              // 7-Byte Factory Unique Identifier (e.g. "04:A2:3B:5C:8D:1E:90")
    public NfcChipType ChipType { get; set; } = NfcChipType.NTAG215;
    public int MemoryCapacityBytes { get; set; } = 504;

    // Structured NDEF Business Payload
    public string StoreId { get; set; } = "SASAN_THENI";
    public string? IMEI { get; set; }
    public string? SerialNumber { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Specs { get; set; } = string.Empty;
    public int? BatteryHealthPercentage { get; set; }
    public ConditionGrade? ConditionGrade { get; set; }
    public WarrantyStatus? WarrantyStatus { get; set; }
    public decimal TaggedPrice { get; set; }
    public string CustomerVerifyUrl { get; set; } = string.Empty;   // URL opened when customer smartphone taps tag

    // Security & Locks
    public bool IsPasswordProtected { get; set; }
    public string? AccessPasswordHex { get; set; }                  // 32-bit (4-byte) hexadecimal PWD (e.g. "53415331")
    public string? PackHex { get; set; }                            // 16-bit (2-byte) Password Acknowledge (e.g. "5347")
    public bool IsReadOnly { get; set; }
    public DateTime? ProgrammedAtUtc { get; set; }
}

/// <summary>
/// Data representation of an EPC Gen2 (860-960 MHz) UHF RFID tag across all 4 memory banks
/// </summary>
public class UhfTagModel
{
    // Bank 01: EPC
    public string EpcHex { get; set; } = string.Empty;              // 96-bit or 128-bit EPC Hex (e.g. "E28011606000020468920194")
    
    // Bank 10: TID
    public string TidHex { get; set; } = string.Empty;              // Factory Silicon Chip ID (e.g. "E280116020007289")

    // Bank 11: User Memory
    public string? UserMemoryHex { get; set; }                      // Decoded or raw hex stored in Bank 11

    // Bank 00: Reserved
    public string? AccessPasswordHex { get; set; }                  // 32-bit Access PWD
    public string? KillPasswordHex { get; set; }                    // 32-bit Kill PWD
    public UhfLockStatus LockStatus { get; set; } = UhfLockStatus.Unlocked;
    public bool IsEasAlarmActive { get; set; } = true;              // Electronic Article Surveillance bit for anti-theft gate

    // Resolved ERP Inventory Links
    public int? ProductId { get; set; }
    public int? SerializedUnitId { get; set; }
    public string? DeviceName { get; set; }
    public string? IMEI { get; set; }
    public decimal TaggedPrice { get; set; }
    public UnitStatus? UnitStatus { get; set; }

    // Live RF Telemetry
    public double RssiDb { get; set; } = -55.0;                     // Received Signal Strength Indication (dBm)
    public int AntennaPort { get; set; } = 1;
    public int ReadCount { get; set; } = 1;
    public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Session details for a rapid physical RFID stock audit sweep
/// </summary>
public class StockAuditSession
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string AuditorName { get; set; } = "Admin";
    public string LocationZone { get; set; } = "Theni Showroom Display Table";

    public int TotalExpectedInDb { get; set; }
    public int TotalScannedPhysical { get; set; }
    public int MatchedCount { get; set; }
    public int MissingCount { get; set; }
    public int UnexpectedCount { get; set; }

    public List<StockAuditItem> AuditItems { get; set; } = new();
}

/// <summary>
/// Individual line item in an RFID stock reconciliation audit
/// </summary>
public class StockAuditItem
{
    public string Identifier { get; set; } = string.Empty;          // IMEI, EPC or SKU
    public string DeviceDescription { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal SystemCost { get; set; }
    public decimal SystemPrice { get; set; }
    public UnitStatus? SystemStatus { get; set; }
    public AuditMatchStatus MatchStatus { get; set; }
    public double? DetectedRssi { get; set; }
    public DateTime? DetectedAt { get; set; }
    public string Remarks { get; set; } = string.Empty;
}
