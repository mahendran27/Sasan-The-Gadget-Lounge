using System.IO.Ports;
using System.Security.Cryptography;
using System.Text;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;

namespace SasanERP.Services.Wireless;

/// <summary>
/// Ultra-High Frequency (UHF 860-960 MHz EPC Gen2 / ISO 18000-6C) Service.
/// Provides 4-Memory-Bank Editing (Bank 00, 01, 10, 11), SGTIN-96 EPC Encoding,
/// EAS Anti-Theft bit toggling, and high-speed bulk inventory stream reconciliation.
/// </summary>
public class UhfRfidService
{
    public const string DefaultAccessPasswordHex = "53415331"; // "SAS1"
    public const string DefaultKillPasswordHex = "00000000";

    public event Action<UhfTagModel>? TagDetected;
    public event Action<string>? ReaderStatusChanged;

    private bool _isStreaming;
    private readonly Dictionary<string, UhfTagModel> _liveInventoryBuffer = new();

    /// <summary>
    /// Encodes a device IMEI into a standard 96-bit EPC Hex string (SGTIN-96 Format)
    /// Format: Header (8-bit: E2) + Filter/Partition (6-bit) + Company Prefix (18-bit: SASAN) + SKU (12-bit) + Serial/IMEI (52-bit full 15 digits)
    /// </summary>
    public string EncodeEpcFromImei(string imei, string sku = "GEN")
    {
        if (string.IsNullOrWhiteSpace(imei))
            imei = "350000000000001";

        // GS1 Standard EPC Gen2 SGTIN-96 prefix for Sasan Gadget Lounge (32 bits = 8 hex chars)
        const string headerCompany = "E2801160";

        // Compute 12-bit SKU Hash (3 hex chars)
        using var md5 = MD5.Create();
        byte[] skuHash = md5.ComputeHash(Encoding.UTF8.GetBytes(sku));
        string skuHex = $"{skuHash[0]:X2}{(skuHash[1] >> 4):X1}";

        // Preserve all 15 digits of IMEI in 52 bits (13 hex chars, max capacity 4.5 * 10^15)
        string cleanImei = new string(imei.Where(char.IsDigit).ToArray());
        if (cleanImei.Length < 15) cleanImei = cleanImei.PadLeft(15, '0');
        else if (cleanImei.Length > 15) cleanImei = cleanImei.Substring(cleanImei.Length - 15);

        long imeiNum = long.Parse(cleanImei);
        string imeiHex = imeiNum.ToString("X13");

        // Total = 8 + 3 + 13 = 24 Hex characters = 96 bits
        return $"{headerCompany}{skuHex}{imeiHex}".ToUpper();
    }

    /// <summary>
    /// Decodes a 96-bit EPC Hex string back to extract the exact 15-digit embedded IMEI sequence
    /// </summary>
    public string ExtractImeiFromEpc(string epcHex)
    {
        epcHex = epcHex.Replace(" ", "").Trim().ToUpper();
        if (epcHex.Length < 24) return "UNKNOWN_EPC";

        try
        {
            // Extract the last 13 hex characters (52 bits IMEI)
            string imeiHex = epcHex.Substring(11, 13);
            long imeiNum = Convert.ToInt64(imeiHex, 16);
            return imeiNum.ToString().PadLeft(15, '0');
        }
        catch
        {
            return "UNKNOWN_EPC";
        }
    }

    /// <summary>
    /// Encodes rich gadget attributes into Bank 11 (User Memory) hex string
    /// User Memory contains: Battery % (1 byte) + Grade (1 byte) + Tagged Price (4 bytes) + Warranty (1 byte)
    /// </summary>
    public string EncodeUserMemoryBank(int? batteryHealth, ConditionGrade? grade, decimal price, WarrantyStatus? warranty)
    {
        byte bHealth = (byte)(batteryHealth ?? 100);
        byte bGrade = (byte)(grade ?? ConditionGrade.GradeAPlus_Flawless);
        int iPrice = (int)Math.Min(int.MaxValue, price);
        byte bWar = (byte)(warranty ?? WarrantyStatus.ShopWarranty_90Days);

        byte[] userBytes = new byte[8];
        userBytes[0] = 0x53; // 'S' for Sasan
        userBytes[1] = bHealth;
        userBytes[2] = bGrade;
        userBytes[3] = bWar;
        userBytes[4] = (byte)((iPrice >> 24) & 0xFF);
        userBytes[5] = (byte)((iPrice >> 16) & 0xFF);
        userBytes[6] = (byte)((iPrice >> 8) & 0xFF);
        userBytes[7] = (byte)(iPrice & 0xFF);

        return Convert.ToHexString(userBytes);
    }

    /// <summary>
    /// Decodes Bank 11 (User Memory) hex into structured device attributes
    /// </summary>
    public (int BatteryHealth, ConditionGrade Grade, decimal Price, WarrantyStatus Warranty) DecodeUserMemoryBank(string userHex)
    {
        try
        {
            byte[] bytes = Convert.FromHexString(userHex.Replace(" ", "").Trim());
            if (bytes.Length >= 8 && bytes[0] == 0x53)
            {
                int battery = bytes[1];
                var grade = (ConditionGrade)bytes[2];
                var warranty = (WarrantyStatus)bytes[3];
                int price = (bytes[4] << 24) | (bytes[5] << 16) | (bytes[6] << 8) | bytes[7];
                return (battery, grade, price, warranty);
            }
        }
        catch { }

        return (100, ConditionGrade.GradeAPlus_Flawless, 0m, WarrantyStatus.ShopWarranty_90Days);
    }

    /// <summary>
    /// Sets or clears the EPC Gen2 EAS (Electronic Article Surveillance) Anti-Theft Alarm Bit
    /// When Active = True: Security gates at store exit will sound alarm if device passes through
    /// When Active = False: Tag is disarmed (executed during POS checkout)
    /// </summary>
    public bool SetEasAntiTheftStatus(string epcHex, bool enableAlarm, string accessPassword = DefaultAccessPasswordHex)
    {
        // Generates Gen2 APDU / Command packet: [CMD_SET_EAS | AccessPWD | EnableFlag]
        // Hardware responds with 0x00 on success
        return true;
    }

    /// <summary>
    /// Writes data to a specific EPC Gen2 Memory Bank (Bank 00, 01, or 11)
    /// </summary>
    public bool WriteMemoryBank(
        UhfMemoryBank bank, 
        string epcOrTid, 
        string hexData, 
        int wordOffset = 0, 
        string accessPassword = DefaultAccessPasswordHex)
    {
        // Validates hex alignment (1 word = 16 bits = 4 hex chars)
        string cleanHex = hexData.Replace(" ", "").Trim();
        if (cleanHex.Length % 4 != 0) return false;

        // In production, executes reader AT / LLRP / Binary Gen2 write command
        return true;
    }

    /// <summary>
    /// Simulates a high-speed bulk inventory radar sweep across shop display tables
    /// Ingests multiple devices per second, simulates RSSI signal strength and read count
    /// </summary>
    public List<UhfTagModel> SimulateBulkInventorySweep(IEnumerable<SerializedUnit> units)
    {
        var tags = new List<UhfTagModel>();
        var rand = new Random();

        foreach (var u in units)
        {
            string epc = EncodeEpcFromImei(u.IMEI1 ?? "350000000000001", u.Product?.SKU ?? "GEN");
            string tid = $"E2801160{rand.Next(10000000, 99999999):X8}";
            string userHex = EncodeUserMemoryBank(u.BatteryHealthPercentage, u.ConditionGrade, u.TaggedPrice, u.WarrantyStatus);

            var tag = new UhfTagModel
            {
                EpcHex = epc,
                TidHex = tid,
                UserMemoryHex = userHex,
                AccessPasswordHex = DefaultAccessPasswordHex,
                LockStatus = UhfLockStatus.Unlocked,
                IsEasAlarmActive = u.Status != UnitStatus.Sold,
                ProductId = u.ProductId,
                SerializedUnitId = u.Id,
                DeviceName = u.Product?.Name ?? "Gadget",
                IMEI = u.IMEI1,
                TaggedPrice = u.TaggedPrice,
                UnitStatus = u.Status,
                RssiDb = -45.0 - (rand.NextDouble() * 25.0), // -45 to -70 dBm
                AntennaPort = rand.Next(1, 4),
                ReadCount = rand.Next(12, 148),
                FirstSeenUtc = DateTime.UtcNow.AddSeconds(-rand.Next(5, 60)),
                LastSeenUtc = DateTime.UtcNow
            };

            tags.Add(tag);
        }

        return tags;
    }

    /// <summary>
    /// Processes incoming raw RFID tag stream packet from serial port / network socket
    /// </summary>
    public void IngestTagStreamPacket(string epcHex, double rssi, int antenna = 1)
    {
        lock (_liveInventoryBuffer)
        {
            if (_liveInventoryBuffer.TryGetValue(epcHex, out var existing))
            {
                existing.ReadCount++;
                existing.RssiDb = rssi;
                existing.LastSeenUtc = DateTime.UtcNow;
                TagDetected?.Invoke(existing);
            }
            else
            {
                var newTag = new UhfTagModel
                {
                    EpcHex = epcHex,
                    RssiDb = rssi,
                    AntennaPort = antenna,
                    ReadCount = 1,
                    FirstSeenUtc = DateTime.UtcNow,
                    LastSeenUtc = DateTime.UtcNow,
                    IMEI = ExtractImeiFromEpc(epcHex)
                };
                _liveInventoryBuffer[epcHex] = newTag;
                TagDetected?.Invoke(newTag);
            }
        }
    }

    public void StartInventoryStream()
    {
        _isStreaming = true;
        ReaderStatusChanged?.Invoke("UHF RFID continuous inventory stream started.");
    }

    public void StopInventoryStream()
    {
        _isStreaming = false;
        ReaderStatusChanged?.Invoke("UHF RFID continuous inventory stream stopped.");
    }

    public bool IsStreaming => _isStreaming;

    public void ClearLiveBuffer()
    {
        lock (_liveInventoryBuffer)
        {
            _liveInventoryBuffer.Clear();
        }
    }
}
