using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SasanERP.Services.Security;

namespace SasanERP.Services.Backup;

public record CloudBackupManifest(
    string SnapshotFileName,
    string EncryptedFileName,
    string Sha256Checksum,
    long OriginalSizeBytes,
    long EncryptedSizeBytes,
    DateTime TimestampUtc,
    string MachineName,
    bool IsAes256Encrypted);

/// <summary>
/// Disaster Recovery & AES-256 Encrypted Cloud Vault Sync Engine for Sasan The Gadget Lounge.
/// Prepares bulletproof encrypted snapshots of the SQLite database with SHA-256 integrity verification
/// for mirroring to secondary drives, USB storage, and Cloud Staging (Google Drive / S3 / NAS).
/// </summary>
public class CloudBackupSyncService
{
    private readonly AuditLogService _auditLogService;
    private static readonly byte[] Salt = "SASAN_GADGET_LOUNGE_THENI_SECURE_SALT_2026"u8.ToArray();

    public CloudBackupSyncService(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Computes cryptographically secure SHA-256 checksum of any file.
    /// </summary>
    public static string ComputeSha256(string filePath)
    {
        using var sha = SHA256.Create();
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        byte[] hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Encrypts a raw SQLite database file using AES-256-CBC with PBKDF2 key derivation.
    /// </summary>
    public async Task<string> EncryptDatabaseSnapshotAsync(
        string rawDbPath, 
        string outputEncryptedPath, 
        string masterPassphrase = "SasanLoungeMasterKey2026")
    {
        using var keyDerivation = new Rfc2898DeriveBytes(masterPassphrase, Salt, 100_000, HashAlgorithmName.SHA256);
        byte[] key = keyDerivation.GetBytes(32); // 256 bits

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV(); // 16 bytes random IV

        using var outputStream = new FileStream(outputEncryptedPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        
        // Write IV to file header
        await outputStream.WriteAsync(aes.IV, 0, aes.IV.Length);

        using (var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var inputStream = new FileStream(rawDbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
        {
            await inputStream.CopyToAsync(cryptoStream);
        }

        return outputEncryptedPath;
    }

    /// <summary>
    /// Decrypts an AES-256 encrypted database snapshot back into usable SQLite format.
    /// </summary>
    public async Task<string> DecryptDatabaseSnapshotAsync(
        string encryptedPath, 
        string outputRestoredDbPath, 
        string masterPassphrase = "SasanLoungeMasterKey2026")
    {
        using var keyDerivation = new Rfc2898DeriveBytes(masterPassphrase, Salt, 100_000, HashAlgorithmName.SHA256);
        byte[] key = keyDerivation.GetBytes(32);

        using var inputStream = new FileStream(encryptedPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        
        // Read IV from file header
        byte[] iv = new byte[16];
        int bytesRead = await inputStream.ReadAsync(iv, 0, 16);
        if (bytesRead < 16) throw new InvalidDataException("Invalid encrypted database header.");

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var cryptoStream = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var outputStream = new FileStream(outputRestoredDbPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        await cryptoStream.CopyToAsync(outputStream);

        return outputRestoredDbPath;
    }

    /// <summary>
    /// Creates a complete AES-256 encrypted disaster-recovery bundle with JSON manifest and mirrors to secondary drive/folder.
    /// </summary>
    public async Task<CloudBackupManifest> PrepareCloudDisasterBundleAsync(
        string rawDbPath,
        string targetVaultDirectory,
        string? secondaryMirrorDirectory = null,
        string masterPassphrase = "SasanLoungeMasterKey2026",
        string initiatedBy = "Admin")
    {
        Directory.CreateDirectory(targetVaultDirectory);
        string timeTag = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        string encFileName = $"SasanERP_Encrypted_{timeTag}.aes";
        string targetEncPath = Path.Combine(targetVaultDirectory, encFileName);

        await EncryptDatabaseSnapshotAsync(rawDbPath, targetEncPath, masterPassphrase);

        var originalFi = new FileInfo(rawDbPath);
        var encFi = new FileInfo(targetEncPath);
        string sha256 = ComputeSha256(targetEncPath);

        var manifest = new CloudBackupManifest(
            Path.GetFileName(rawDbPath),
            encFileName,
            sha256,
            originalFi.Length,
            encFi.Length,
            DateTime.UtcNow,
            Environment.MachineName,
            IsAes256Encrypted: true
        );

        string manifestPath = Path.Combine(targetVaultDirectory, $"manifest_{timeTag}.json");
        string json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(manifestPath, json, Encoding.UTF8);

        // Mirror to secondary external USB / Network share if configured
        if (!string.IsNullOrWhiteSpace(secondaryMirrorDirectory))
        {
            try
            {
                Directory.CreateDirectory(secondaryMirrorDirectory);
                string mirrorEncPath = Path.Combine(secondaryMirrorDirectory, encFileName);
                string mirrorManifestPath = Path.Combine(secondaryMirrorDirectory, $"manifest_{timeTag}.json");
                File.Copy(targetEncPath, mirrorEncPath, true);
                File.Copy(manifestPath, mirrorManifestPath, true);
            }
            catch (Exception ex)
            {
                await _auditLogService.LogAsync("MirrorBackupFailed", "CloudBackup", encFileName, $"Mirror copy failed: {ex.Message}", initiatedBy);
            }
        }

        await _auditLogService.LogAsync(
            "CloudDisasterBundleCreated",
            "CloudBackup",
            encFileName,
            $"AES-256 Vault Snapshot created ({encFi.Length / 1024.0:F2} KB). SHA-256: {sha256[..12]}...",
            initiatedBy);

        return manifest;
    }
}
