using Microsoft.Data.Sqlite;

namespace ConstructionCostManager.Data.Services;

public class DatabaseBackupService
{
    private readonly string _databasePath;

    public DatabaseBackupService(string databasePath)
    {
        _databasePath = databasePath;
    }

    public async Task<byte[]> ExportBackupBytesAsync()
    {
        if (!File.Exists(_databasePath))
            throw new FileNotFoundException("ملف قاعدة البيانات غير موجود.", _databasePath);

        var tempBackup = Path.Combine(Path.GetTempPath(), $"ccm_backup_{Guid.NewGuid():N}.db");
        try
        {
            using (var source = new SqliteConnection($"Data Source={_databasePath}"))
            using (var destination = new SqliteConnection($"Data Source={tempBackup}"))
            {
                await source.OpenAsync();
                await destination.OpenAsync();
                source.BackupDatabase(destination);
            }

            return await File.ReadAllBytesAsync(tempBackup);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempBackup))
                File.Delete(tempBackup);
        }
    }

    public async Task RestoreBackupAsync(Stream backupStream)
    {
        var tempUpload = Path.Combine(Path.GetTempPath(), $"ccm_restore_{Guid.NewGuid():N}.db");
        try
        {
            await using (var fs = File.Create(tempUpload))
            {
                await backupStream.CopyToAsync(fs);
            }

            // Validate SQLite magic header ("SQLite format 3\0")
            var header = new byte[16];
            await using (var fs = File.OpenRead(tempUpload))
            {
                var read = await fs.ReadAsync(header);
                if (read < 16 || System.Text.Encoding.ASCII.GetString(header, 0, 15) != "SQLite format 3")
                    throw new InvalidOperationException("الملف المحدد ليس نسخة احتياطية صالحة لقاعدة بيانات SQLite.");
            }

            SqliteConnection.ClearAllPools();

            var dir = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.Copy(tempUpload, _databasePath, overwrite: true);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(tempUpload))
                File.Delete(tempUpload);
        }
    }
}
