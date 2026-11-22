using System.Text;
using System.Text.Json;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;

namespace SasanERP.Services.Wireless;

/// <summary>
/// High-Frequency NFC (13.56 MHz - ISO 14443A / NTAG213, 215, 216) Programmer & Editor Service.
/// Formats raw chips, encodes NDEF payloads (URL & Device specs), computes APDUs, and manages 32-bit PWD locks.
/// </summary>
public class NfcTagService
{
    private static readonly byte[] NdefUriHeader = new byte[] { 0x00 }; // 0x00 = No prefix, 0x01 = http://www., 0x04 = https://
    public const string DefaultMasterPasswordHex = "53415331"; // "SAS1" in ASCII Hex
    public const string DefaultPackHex = "5347";              // "SG" (Sasan Gadget) in ASCII Hex

    /// <summary>
    /// Encodes a complete Sasan Gadget Lounge NDEF Payload into standard NFC Forum Type 2 Tag bytes
    /// </summary>
    public byte[] EncodeNdefMessage(NfcTagModel tag)
    {
        var records = new List<byte[]>();

        // 1. Record 1: Customer Phone Tap URI Record (e.g. https://sasanthegadgetlounge.in/explore?imei=...)
        string url = !string.IsNullOrWhiteSpace(tag.CustomerVerifyUrl)
            ? tag.CustomerVerifyUrl
            : $"https://sasanthegadgetlounge.in/explore?imei={tag.IMEI ?? tag.SerialNumber ?? "SASAN"}";

        records.Add(BuildUriRecord(url));

        // 2. Record 2: Structured JSON Telemetry Record (Model, Battery, Grade, Warranty, Price)
        var telemetry = new
        {
            store = tag.StoreId,
            imei = tag.IMEI,
            model = tag.ModelName,
            specs = tag.Specs,
            batt = tag.BatteryHealthPercentage,
            grade = tag.ConditionGrade?.ToString(),
            warranty = tag.WarrantyStatus?.ToString(),
            price = tag.TaggedPrice,
            locked = tag.IsPasswordProtected
        };

        string jsonPayload = JsonSerializer.Serialize(telemetry);
        records.Add(BuildMimeRecord("application/json", jsonPayload));

        // Assemble into standard NDEF Message with TLV Envelope
        return PackageIntoNdefTlv(records);
    }

    /// <summary>
    /// Decodes raw NDEF bytes read from an NFC tag back into structured NfcTagModel
    /// </summary>
    public NfcTagModel DecodeNdefMessage(byte[] rawBytes, string tagUid = "04:A1:B2:C3:D4:E5:F6")
    {
        var model = new NfcTagModel
        {
            TagUid = tagUid,
            MemoryCapacityBytes = rawBytes.Length
        };

        try
        {
            // Simple TLV parser: Look for NDEF Message TLV (Type = 0x03)
            int index = 0;
            while (index < rawBytes.Length)
            {
                byte type = rawBytes[index++];
                if (type == 0x00) continue; // NULL TLV
                if (type == 0xFE) break;    // Terminator TLV

                int length = rawBytes[index++];
                if (length == 0xFF) // 3-byte length format
                {
                    length = (rawBytes[index++] << 8) | rawBytes[index++];
                }

                if (type == 0x03 && index + length <= rawBytes.Length) // NDEF Message TLV
                {
                    byte[] ndefData = new byte[length];
                    Array.Copy(rawBytes, index, ndefData, 0, length);
                    ParseNdefRecords(ndefData, model);
                    break;
                }

                index += length;
            }
        }
        catch
        {
            // Fallback for non-standard payloads
        }

        return model;
    }

    /// <summary>
    /// Generates APDU commands to write data and set 32-bit password lock on NTAG213/215/216 chips
    /// </summary>
    public List<byte[]> GenerateWriteApdus(NfcTagModel tag)
    {
        var apdus = new List<byte[]>();
        byte[] ndefBytes = EncodeNdefMessage(tag);

        // NTAG Type 2 write commands: FF D6 00 [Page] 04 [4 Bytes Data]
        // User memory starts at Page 04 (each page is 4 bytes)
        int page = 4;
        for (int i = 0; i < ndefBytes.Length; i += 4)
        {
            byte[] pageData = new byte[4];
            int len = Math.Min(4, ndefBytes.Length - i);
            Array.Copy(ndefBytes, i, pageData, 0, len);

            byte[] apdu = new byte[] { 0xFF, 0xD6, 0x00, (byte)page, 0x04, pageData[0], pageData[1], pageData[2], pageData[3] };
            apdus.Add(apdu);
            page++;
        }

        // Add Terminator TLV (0xFE)
        apdus.Add(new byte[] { 0xFF, 0xD6, 0x00, (byte)page, 0x04, 0xFE, 0x00, 0x00, 0x00 });

        // If password protection is requested, write 32-bit PWD and 16-bit PACK
        if (tag.IsPasswordProtected)
        {
            byte[] pwdBytes = ConvertHexToBytes(tag.AccessPasswordHex ?? DefaultMasterPasswordHex, 4);
            byte[] packBytes = ConvertHexToBytes(tag.PackHex ?? DefaultPackHex, 2);

            // NTAG215: PWD is at page 85h (133), PACK is at 86h (134)
            byte pwdPage = tag.ChipType switch
            {
                NfcChipType.NTAG213 => 0x2B, // Page 43
                NfcChipType.NTAG215 => 0x85, // Page 133
                NfcChipType.NTAG216 => 0xE5, // Page 229
                _ => 0x85
            };

            // Write PWD
            apdus.Add(new byte[] { 0xFF, 0xD6, 0x00, pwdPage, 0x04, pwdBytes[0], pwdBytes[1], pwdBytes[2], pwdBytes[3] });
            // Write PACK
            apdus.Add(new byte[] { 0xFF, 0xD6, 0x00, (byte)(pwdPage + 1), 0x04, packBytes[0], packBytes[1], 0x00, 0x00 });
        }

        return apdus;
    }

    /// <summary>
    /// Simulates a real hardware NFC tap session for testing, verification, or offline development
    /// </summary>
    public NfcTagModel SimulateHardwareTagRead(SerializedUnit unit, CompanyProfile company)
    {
        string uid = $"04:{unit.IMEI1?.Substring(unit.IMEI1.Length - 8, 2) ?? "A1"}:" +
                     $"{unit.IMEI1?.Substring(unit.IMEI1.Length - 6, 2) ?? "B2"}:" +
                     $"{unit.IMEI1?.Substring(unit.IMEI1.Length - 4, 2) ?? "C3"}:" +
                     $"{unit.IMEI1?.Substring(unit.IMEI1.Length - 2, 2) ?? "D4"}:90:E1";

        var tag = new NfcTagModel
        {
            TagUid = uid,
            ChipType = NfcChipType.NTAG215,
            MemoryCapacityBytes = 504,
            StoreId = "SASAN_THENI",
            IMEI = unit.IMEI1,
            SerialNumber = unit.SerialNumber,
            ModelName = unit.Product?.Name ?? "Smartphone",
            Specs = $"{unit.StorageCapacity} | {unit.Color}".Trim(' ', '|'),
            BatteryHealthPercentage = unit.BatteryHealthPercentage,
            ConditionGrade = unit.ConditionGrade,
            WarrantyStatus = unit.WarrantyStatus,
            TaggedPrice = unit.TaggedPrice,
            CustomerVerifyUrl = $"https://sasanthegadgetlounge.in/explore?imei={unit.IMEI1}",
            IsPasswordProtected = true,
            AccessPasswordHex = DefaultMasterPasswordHex,
            PackHex = DefaultPackHex,
            ProgrammedAtUtc = DateTime.UtcNow
        };

        return tag;
    }

    #region Helper Methods

    private static byte[] BuildUriRecord(string uri)
    {
        // NDEF URI Record Header: MB=1, ME=0, SR=1, TNF=0x01 (Well-Known), Type='U' (0x54)
        byte[] uriBytes = Encoding.UTF8.GetBytes(uri);
        byte[] record = new byte[5 + uriBytes.Length];

        record[0] = 0x91; // MB=1, ME=0, CF=0, SR=1, IL=0, TNF=001 (Well-Known)
        record[1] = 0x01; // Type Length = 1 ('U')
        record[2] = (byte)(1 + uriBytes.Length); // Payload Length
        record[3] = 0x55; // 'U'
        record[4] = 0x00; // Identifier code 0x00 = No prefix
        Array.Copy(uriBytes, 0, record, 5, uriBytes.Length);

        return record;
    }

    private static byte[] BuildMimeRecord(string mimeType, string text)
    {
        byte[] typeBytes = Encoding.ASCII.GetBytes(mimeType);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(text);

        byte[] record = new byte[3 + typeBytes.Length + payloadBytes.Length];
        record[0] = 0x52; // MB=0, ME=1, CF=0, SR=1, IL=0, TNF=010 (MIME Media)
        record[1] = (byte)typeBytes.Length;
        record[2] = (byte)payloadBytes.Length;

        Array.Copy(typeBytes, 0, record, 3, typeBytes.Length);
        Array.Copy(payloadBytes, 0, record, 3 + typeBytes.Length, payloadBytes.Length);

        return record;
    }

    private static byte[] PackageIntoNdefTlv(List<byte[]> records)
    {
        int totalPayloadLen = records.Sum(r => r.Length);
        var tlv = new List<byte> { 0x03 }; // NDEF Message TLV Type

        if (totalPayloadLen < 255)
        {
            tlv.Add((byte)totalPayloadLen);
        }
        else
        {
            tlv.Add(0xFF);
            tlv.Add((byte)(totalPayloadLen >> 8));
            tlv.Add((byte)(totalPayloadLen & 0xFF));
        }

        foreach (var r in records)
            tlv.AddRange(r);

        tlv.Add(0xFE); // Terminator TLV
        return tlv.ToArray();
    }

    private static void ParseNdefRecords(byte[] ndefData, NfcTagModel model)
    {
        int i = 0;
        while (i < ndefData.Length)
        {
            byte header = ndefData[i++];
            bool isShort = (header & 0x10) != 0;
            int typeLength = ndefData[i++];
            int payloadLength = isShort ? ndefData[i++] : ((ndefData[i++] << 24) | (ndefData[i++] << 16) | (ndefData[i++] << 8) | ndefData[i++]);

            byte[] typeBytes = new byte[typeLength];
            Array.Copy(ndefData, i, typeBytes, 0, typeLength);
            i += typeLength;

            byte[] payload = new byte[payloadLength];
            Array.Copy(ndefData, i, payload, 0, payloadLength);
            i += payloadLength;

            string typeStr = Encoding.ASCII.GetString(typeBytes);
            if (typeStr == "U" && payload.Length > 1)
            {
                model.CustomerVerifyUrl = Encoding.UTF8.GetString(payload, 1, payload.Length - 1);
            }
            else if (typeStr == "application/json")
            {
                try
                {
                    string json = Encoding.UTF8.GetString(payload);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("imei", out var pImei)) model.IMEI = pImei.GetString();
                    if (root.TryGetProperty("model", out var pModel)) model.ModelName = pModel.GetString() ?? "";
                    if (root.TryGetProperty("specs", out var pSpecs)) model.Specs = pSpecs.GetString() ?? "";
                    if (root.TryGetProperty("price", out var pPrice)) model.TaggedPrice = pPrice.GetDecimal();
                    if (root.TryGetProperty("batt", out var pBatt)) model.BatteryHealthPercentage = pBatt.GetInt32();
                }
                catch { }
            }
        }
    }

    private static byte[] ConvertHexToBytes(string hex, int targetLen)
    {
        byte[] bytes = new byte[targetLen];
        hex = hex.Replace(" ", "").Replace(":", "");
        for (int i = 0; i < Math.Min(targetLen, hex.Length / 2); i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    #endregion
}
