using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Enums;
using SasanERP.Core.Models;
using SasanERP.Data.Context;

namespace SasanERP.Services.Wireless;

/// <summary>
/// Service managing rapid RFID / NFC physical stock reconciliation audits.
/// Reconciles live physical RFID tag sweeps against the ERP database inventory,
/// highlighting matched devices, missing stock, and unauthorized anomalies.
/// </summary>
public class StockAuditService
{
    private readonly AppDbContext _context;

    public StockAuditService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Executes a reconciliation audit comparing physically detected tags with ERP database records
    /// </summary>
    public async Task<StockAuditSession> ReconcileStockAuditAsync(
        IEnumerable<UhfTagModel> physicallyDetectedTags, 
        string auditorName = "Manager",
        string zone = "Main Showroom")
    {
        var session = new StockAuditSession
        {
            StartedAtUtc = DateTime.UtcNow,
            AuditorName = auditorName,
            LocationZone = zone
        };

        // 1. Fetch all serialized devices from database
        var dbUnits = await _context.SerializedUnits
            .Include(u => u.Product)
            .ToListAsync();

        var inStockDbUnits = dbUnits.Where(u => u.Status == UnitStatus.InStock).ToList();
        session.TotalExpectedInDb = inStockDbUnits.Count;

        var detectedList = physicallyDetectedTags.ToList();
        session.TotalScannedPhysical = detectedList.Count;

        // Build fast lookup maps by IMEI
        var physicalMapByImei = new Dictionary<string, UhfTagModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in detectedList)
        {
            if (!string.IsNullOrEmpty(tag.IMEI))
            {
                physicalMapByImei[tag.IMEI] = tag;
            }
        }

        // 2. Evaluate all DB In-Stock units
        foreach (var unit in inStockDbUnits)
        {
            string imei = unit.IMEI1 ?? unit.SerialNumber ?? string.Empty;
            if (physicalMapByImei.TryGetValue(imei, out var detectedTag))
            {
                // Matched!
                session.AuditItems.Add(new StockAuditItem
                {
                    Identifier = imei,
                    DeviceDescription = $"{unit.Product?.Name} ({unit.StorageCapacity})",
                    Color = unit.Color ?? "",
                    SystemCost = unit.PurchaseCost,
                    SystemPrice = unit.TaggedPrice,
                    SystemStatus = unit.Status,
                    MatchStatus = AuditMatchStatus.MatchedInStock,
                    DetectedRssi = detectedTag.RssiDb,
                    DetectedAt = detectedTag.LastSeenUtc,
                    Remarks = "VERIFIED IN STOCK [OK]"
                });
                session.MatchedCount++;
            }
            else
            {
                // Missing from shelf!
                session.AuditItems.Add(new StockAuditItem
                {
                    Identifier = imei,
                    DeviceDescription = $"{unit.Product?.Name} ({unit.StorageCapacity})",
                    Color = unit.Color ?? "",
                    SystemCost = unit.PurchaseCost,
                    SystemPrice = unit.TaggedPrice,
                    SystemStatus = unit.Status,
                    MatchStatus = AuditMatchStatus.MissingFromShelf,
                    DetectedRssi = null,
                    DetectedAt = null,
                    Remarks = "ALERT: NOT DETECTED ON SHELF (POSSIBLE SHRINKAGE/MISPLACED)"
                });
                session.MissingCount++;
            }
        }

        // 3. Evaluate unexpected physical tags detected on shelf
        var dbUnitsByImei = dbUnits.ToDictionary(u => u.IMEI1 ?? u.SerialNumber ?? "", StringComparer.OrdinalIgnoreCase);
        foreach (var tag in detectedList)
        {
            string imei = tag.IMEI ?? string.Empty;
            if (string.IsNullOrEmpty(imei)) continue;

            if (dbUnitsByImei.TryGetValue(imei, out var dbUnit))
            {
                if (dbUnit.Status == UnitStatus.Sold)
                {
                    // Device marked SOLD in system but physically found on shelf!
                    session.AuditItems.Add(new StockAuditItem
                    {
                        Identifier = imei,
                        DeviceDescription = $"{dbUnit.Product?.Name} ({dbUnit.StorageCapacity})",
                        Color = dbUnit.Color ?? "",
                        SystemCost = dbUnit.PurchaseCost,
                        SystemPrice = dbUnit.TaggedPrice,
                        SystemStatus = dbUnit.Status,
                        MatchStatus = AuditMatchStatus.SoldDeviceAlert,
                        DetectedRssi = tag.RssiDb,
                        DetectedAt = tag.LastSeenUtc,
                        Remarks = "CRITICAL: MARKED AS SOLD IN SYSTEM BUT DETECTED ON SHELF!"
                    });
                    session.UnexpectedCount++;
                }
            }
            else
            {
                // Unknown tag not in Sasan ERP
                session.AuditItems.Add(new StockAuditItem
                {
                    Identifier = imei,
                    DeviceDescription = tag.DeviceName ?? "Unknown RF Tag",
                    Color = "N/A",
                    SystemCost = 0,
                    SystemPrice = tag.TaggedPrice,
                    SystemStatus = null,
                    MatchStatus = AuditMatchStatus.UnregisteredTag,
                    DetectedRssi = tag.RssiDb,
                    DetectedAt = tag.LastSeenUtc,
                    Remarks = "UNKNOWN TAG: Not found in Sasan The Gadget Lounge database."
                });
                session.UnexpectedCount++;
            }
        }

        session.CompletedAtUtc = DateTime.UtcNow;
        return session;
    }
}
