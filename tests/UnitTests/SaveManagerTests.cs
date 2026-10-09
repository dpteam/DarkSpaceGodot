using DarkSpace.Core.Data;
using DarkSpace.Core.Data.Save;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты менеджера сохранений и системы бэкапов.</summary>
public sealed class SaveManagerTests : IDisposable
{
    private readonly string _directory;

    public SaveManagerTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "darkspace-tests-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenNoSave_ReturnsNotFound()
    {
        var manager = new SaveManager(_directory);
        SaveLoadResult result = await manager.LoadAsync();
        Assert.Equal(SaveLoadStatus.NotFound, result.Status);
        Assert.Null(result.Save);
    }

    [Fact]
    public async Task SaveThenLoad_RoundTrips()
    {
        var manager = new SaveManager(_directory);
        SaveGame save = SaveGame.CreateNew("Пилот", 42);
        save.Player.Credits = 999;

        await manager.SaveAsync(save);
        SaveLoadResult result = await manager.LoadAsync();

        Assert.Equal(SaveLoadStatus.Loaded, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal("Пилот", result.Save!.Metadata.PlayerName);
        Assert.Equal(999, result.Save.Player.Credits);
    }

    [Fact]
    public async Task SaveAsync_SecondSave_CreatesBackup()
    {
        var manager = new SaveManager(_directory);
        await manager.SaveAsync(SaveGame.CreateNew("Первый", 1));
        await manager.SaveAsync(SaveGame.CreateNew("Второй", 1));

        Assert.Single(manager.ListBackups());
    }

    [Fact]
    public async Task LoadAsync_CorruptedPrimary_RecoversFromBackup()
    {
        var manager = new SaveManager(_directory);
        await manager.SaveAsync(SaveGame.CreateNew("Старый", 7));   // A
        await manager.SaveAsync(SaveGame.CreateNew("Новый", 7));   // B (бэкап A создан)

        // Портим основной файл.
        await File.WriteAllTextAsync(manager.SavePath, "{ повреждено намеренно");

        SaveLoadResult result = await manager.LoadAsync();

        Assert.Equal(SaveLoadStatus.Recovered, result.Status);
        Assert.NotNull(result.Save);
        Assert.Equal("Старый", result.Save!.Metadata.PlayerName);
        Assert.NotNull(result.SourcePath);

        // Основной файл должен быть восстановлен: следующий Load даёт Loaded.
        SaveLoadResult second = await manager.LoadAsync();
        Assert.Equal(SaveLoadStatus.Loaded, second.Status);
    }

    [Fact]
    public async Task SaveAsync_IsAtomic_NoTempFileLeftBehind()
    {
        var manager = new SaveManager(_directory);
        await manager.SaveAsync(SaveGame.CreateNew("A", 1));
        Assert.True(File.Exists(manager.SavePath));
        Assert.False(File.Exists(Path.Combine(_directory, "savegame.tmp.json")));
    }

    [Fact]
    public async Task BackupRotation_KeepsAtMostMaxBackups()
    {
        const int max = 3;
        var manager = new SaveManager(_directory, maxBackups: max);
        for (int i = 0; i < 10; i++)
        {
            await manager.SaveAsync(SaveGame.CreateNew($"Игрок {i}", 1));
        }

        Assert.True(manager.ListBackups().Count <= max,
            $"Бэкапов {manager.ListBackups().Count}, ожидалось не больше {max}.");
    }

    [Fact]
    public async Task BackupManager_ListIsSortedNewestFirst()
    {
        var backupManager = new SaveBackupManager(maxBackups: 5);
        string source = Path.Combine(_directory, "source.json");
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(source, "{}");

        string backupDir = Path.Combine(_directory, "backups");
        var baseTime = DateTimeOffset.UtcNow;
        await backupManager.CreateBackupAsync(source, backupDir, baseTime);
        await backupManager.CreateBackupAsync(source, backupDir, baseTime.AddMinutes(1));

        var backups = backupManager.ListBackups(backupDir);
        Assert.Equal(2, backups.Count);
        Assert.True(string.CompareOrdinal(backups[0], backups[1]) > 0, "Свежий бэкап должен идти первым.");
    }

    [Fact]
    public async Task CreateBackupAsync_ReturnsNull_WhenNoSource()
    {
        var backupManager = new SaveBackupManager();
        string missing = Path.Combine(_directory, "nope.json");
        string? result = await backupManager.CreateBackupAsync(missing, Path.Combine(_directory, "backups"));
        Assert.Null(result);
    }
}
