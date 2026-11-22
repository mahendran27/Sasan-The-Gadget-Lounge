using System.IO;
using Microsoft.EntityFrameworkCore;
using SasanERP.Data.Context;
using SasanERP.Services.Security;

namespace SasanERP.Services.Backup;

public record BackupFileInfo(string FileName, string FullPath, long SizeInBytes, DateTime CreatedAt);

/// <summary>
/// Automated Local Database Backup Engine for Sasan The Gadget Lounge ERP.
/// Ensures zero data loss, automatic timestamped snapshots, and rolling retention of 30 backups.
/// </summary>
public class DatabaseBackupService
{
    private readonly AppDbContext _context;
    private readonly AuditLogService _auditLogService;
    private readonly string _dbPath;
    private readonly string _backupDirectory;

    public DatabaseBackupService(AppDbContext context, AuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;

        _dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SasanTheGadgetLounge",
            "SasanERP.db");

        _backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SasanTheGadgetLounge",
            "Backups");

        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    /// <summary>
    /// Custom constructor for verification and testing
    /// </summary>
    public DatabaseBackupService(AppDbContext context, AuditLogService auditLogService, string customDbPath, string customBackupDir)
    {
        _context = context;
        _auditLogService = auditLogService;
        _dbPath = customDbPath;
        _backupDirectory = customBackupDir;

        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    /// <summary>
    /// Creates an immediate timestamped snapshot of the SQLite database.
    /// Uses SQLite WAL checkpoint to ensure all uncommitted WAL pages are flushed first.
    /// </summary>
    public async Task<BackupFileInfo> CreateBackupAsync(string reason = "Automated")
    {
        // 1. Flush SQLite WAL to main DB file
        try
        {
            if (_context.Database.IsSqlite())
            {
                await _context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);");
            }
        }
        catch
        {
            // Proceed even if pragma checkpoint isn't supported in memory mode
        }

        if (!File.Exists(_dbPath))
        {
            throw new FileNotFoundException($"Cannot create backup: Database file not found at '{_dbPath}'");
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupFileName = $"SasanERP_Backup_{timestamp}.db";
        string backupFilePath = Path.Combine(_backupDirectory, backupFileName);

        // Copy file safely
        File.Copy(_dbPath, backupFilePath, overwrite: true);

        var fi = new FileInfo(backupFilePath);
        var result = new BackupFileInfo(backupFileName, backupFilePath, fi.Length, fi.CreationTime);

        // Record security audit
        await _auditLogService.LogAsync(
            "BackupCreated", 
            "Database", 
            backupFileName, 
            $"Backup created: {fi.Length / 1024.0:F2} KB (Reason: {reason})");

        // Rotate backups: keep latest 30 snapshots
        RotateBackups(keepCount: 30);

        return result;
    }

    /// <summary>
    /// Returns list of all available local backups ordered from newest to oldest.
    /// </summary>
    public List<BackupFileInfo> GetAvailableBackups()
    {
        if (!Directory.Exists(_backupDirectory))
            return new List<BackupFileInfo>();

        var di = new DirectoryInfo(_backupDirectory);
        return di.GetFiles("SasanERP_Backup_*.db")
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new BackupFileInfo(f.Name, f.FullName, f.Length, f.CreationTime))
            .ToList();
    }

    /// <summary>
    /// Deletes older backups keeping only the most recent N snapshots.
    /// </summary>
    public void RotateBackups(int keepCount = 30)
    {
        try
        {
            var backups = GetAvailableBackups();
            if (backups.Count > keepCount)
            {
                var toDelete = backups.Skip(keepCount);
                foreach (var b in toDelete)
                {
                    if (File.Exists(b.FullPath))
                    {
                        File.Delete(b.FullPath);
                    }
                }
            }
        }
        catch
        {
            // Prune error is non-fatal
        }
    }

    /// <summary>
    /// Returns the current size of the primary SQLite database file in bytes.
    /// </summary>
    public long GetDatabaseSizeInBytes()
    {
        if (File.Exists(_dbPath))
        {
            return new FileInfo(_dbPath).Length;
        }
        return 0;
    }

    public string DatabasePath => _dbPath;
    public string BackupDirectory => _backupDirectory;
}
