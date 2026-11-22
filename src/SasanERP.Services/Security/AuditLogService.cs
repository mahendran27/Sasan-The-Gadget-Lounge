using Microsoft.EntityFrameworkCore;
using SasanERP.Core.Models;
using SasanERP.Data.Context;

namespace SasanERP.Services.Security;

/// <summary>
/// Centralized Security & Financial Audit Logging Service.
/// Records audit trail entries for financial transactions, stock adjustments, and administrative overrides.
/// </summary>
public class AuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Writes an audit log entry asynchronously without interrupting the calling transaction.
    /// </summary>
    public async Task LogAsync(
        string action, 
        string entityType, 
        string entityId, 
        string details, 
        string performedBy = "Admin")
    {
        try
        {
            var log = new AuditLog
            {
                TimestampUtc = DateTime.UtcNow,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                PerformedBy = performedBy,
                Details = details,
                MachineName = Environment.MachineName
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Fail-safe: Audit logging should never bring down core business operations
        }
    }

    /// <summary>
    /// Fetches the latest audit logs for security review and compliance inspection.
    /// </summary>
    public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 100)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(l => l.TimestampUtc)
            .Take(count)
            .ToListAsync();
    }
}
