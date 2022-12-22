using System.IO.Compression;
using Microsoft.Data.Sqlite;

namespace WakeUpMachine.Service.Maintenance;

internal class BackupCreator
{
    private readonly ILogger<BackupCreator> _logger;
    private readonly IConfiguration _configuration;

    public BackupCreator(ILogger<BackupCreator> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Make backup zip with file name: yyyy-dd-M--HH-mm-ss.zip
    /// </summary>
    public async Task Backup()
    {
        var backupDateTimeFileFormat = $"{DateTime.Now:yyyy-dd-M--HH-mm-ss}";
        _logger.LogInformation("Backup {backupDt} started..", backupDateTimeFileFormat);
        
        // Prepare backup folder

        var rootBackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WakeUpMachine",
            "backups");

        _logger.LogInformation("Root backup folder: {folder}", rootBackupFolder);

        var backupFolder = Path.Combine(rootBackupFolder, backupDateTimeFileFormat);

        _logger.LogInformation("Creating a new backup folder: {folder}", backupFolder);

        if (!Directory.Exists(backupFolder))
            Directory.CreateDirectory(backupFolder);

        // Backup

        _logger.LogInformation("Coping database file to backup folder..");
        await CopyDatabaseFileTo(backupFolder);

        _logger.LogInformation("Coping config file to backup folder..");
        await CopyConfigFileTo(backupFolder);

        // Archive

        var zipFile = Path.Combine(rootBackupFolder, $"{backupDateTimeFileFormat}.zip");

        _logger.LogInformation("Archiving backup folder to zip: {zip}", zipFile);
        ZipFile.CreateFromDirectory(backupFolder, zipFile);

        // Clean
        
        _logger.LogInformation("Cleaning root backup folder..");
        Directory.Delete(backupFolder, true);
        
        _logger.LogInformation("Backup done");
    }

    private static async Task CopyFileAsync(string sourcePath, string destinationPath)
    {
        await using Stream source = File.Open(sourcePath, FileMode.Open);
        await using Stream destination = File.Create(destinationPath);
        await source.CopyToAsync(destination);
    }
    
    private async Task CopyDatabaseFileTo(string backupFolder)
    {
        var csBuilder = new SqliteConnectionStringBuilder(_configuration["ConnectionStrings:Default"]);

        var databaseFile = Path.IsPathFullyQualified(csBuilder.DataSource)
            ? csBuilder.DataSource
            : Path.GetFullPath(csBuilder.DataSource);

        _logger.LogInformation("Database file extracted from config: {file}",databaseFile );
        
        if (!File.Exists(databaseFile))
        {
            _logger.LogCritical("Database file does not exist");
            throw new InvalidOperationException("Database file does not exist.");
        }

        await CopyFileAsync(databaseFile, Path.Combine(backupFolder, Path.GetFileName(databaseFile)));
    }

    private async Task CopyConfigFileTo(string backupFolder)
    {
        var configFile = Path.GetFullPath("appSettings.json");
        
        if (!File.Exists(configFile))
        {
            _logger.LogCritical("Config file does not exist");
            throw new InvalidOperationException("Config file does not exist.");
        }
        
        await CopyFileAsync(configFile, Path.Combine(backupFolder, Path.GetFileName(configFile)));
    }
}