namespace SasanERP.Core.Enums;

/// <summary>
/// Operational RF Frequency band for gadget tracking and identification
/// </summary>
public enum RfFrequencyType
{
    NfcProximity_13_56MHz = 1,     // High Frequency (HF) ISO 14443A: Tap proximity 0-3cm (Customer smartphones & Counter checkout)
    UhfLongRange_860_960MHz = 2     // Ultra High Frequency (UHF) EPC Gen2 / ISO 18000-6C: 1-8 meters (Bulk warehouse & shelf audit)
}

/// <summary>
/// Silicon chip architecture for NFC High-Frequency tags
/// </summary>
public enum NfcChipType
{
    NTAG213 = 1,                   // 144 bytes user memory (Standard for accessories & chargers)
    NTAG215 = 2,                   // 504 bytes user memory (Standard for smartphones & warranty records)
    NTAG216 = 3,                   // 888 bytes user memory (High capacity for MacBooks, diagnostic reports)
    MifareUltralightEV1 = 4,       // 48/128 bytes basic identification
    MifareClassic1K = 5            // 1024 bytes multi-sector authentication
}

/// <summary>
/// Standard EPC Gen2 (ISO 18000-6C) UHF RFID 4-Memory-Bank specification
/// </summary>
public enum UhfMemoryBank
{
    Reserved_00 = 0,               // Bank 00: 32-bit Kill Password + 32-bit Access Password
    Epc_01 = 1,                    // Bank 01: CRC16 (16-bit) + Protocol Control (16-bit) + EPC Data (96 or 128-bit SGTIN)
    Tid_10 = 2,                    // Bank 10: Factory-programmed silicon Tag Identifier (32 to 64-bit unalterable chip ID)
    UserMemory_11 = 3              // Bank 11: Extended user storage (32 to 512+ bits for custom device attributes)
}

/// <summary>
/// Memory Bank lock state under EPC Gen2 protocol
/// </summary>
public enum UhfLockStatus
{
    Unlocked = 0,                  // Open read and write without authentication
    PasswordProtected = 1,         // Read/Write requires 32-bit Access Password
    PermanentlyLocked = 2          // Write disabled permanently (OTP / ROM mode)
}

/// <summary>
/// Operational operational mode of the RFID / NFC Hardware reader
/// </summary>
public enum RfidScanMode
{
    SingleItemTag = 1,             // Single tap encoding or desk inspection
    BulkInventoryStream = 2,       // Rapid multi-tag continuous reading (50-200 tags/sec)
    AuditReconciliation = 3        // Compares physical read tags against ERP database stock
}

/// <summary>
/// Discrepancy reconciliation status during RFID physical stock audit
/// </summary>
public enum AuditMatchStatus
{
    MatchedInStock = 1,            // Device physically detected on shelf and correctly marked 'InStock' in ERP
    MissingFromShelf = 2,          // Device marked 'InStock' in ERP but NOT detected during physical shelf sweep
    UnregisteredTag = 3,           // Unknown tag detected on shelf not registered in Sasan ERP
    SoldDeviceAlert = 4            // Device marked 'Sold' in ERP but physically detected on shelf (Possible return or inventory leak!)
}
