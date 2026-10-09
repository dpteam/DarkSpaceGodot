using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DarkSpace.Core.Data.Save;

/// <summary>
/// Менеджер сохранений: атомарная запись, резервное копирование и восстановление после повреждений.
/// </summary>
/// <remarks>
/// <para>
/// Все операции ввода-вывода асинхронны. Запись атомарна: данные сначала пишутся во временный
/// файл, затем заменяют основной, что исключает появление «полузаписанного» сохранения.
/// </para>
/// <para>
/// Класс не зависит от Godot: каталог сохранений передаётся извне (в слое движка —
/// <c>ProjectSettings.GlobalizePath("user://saves")</c>).
/// </para>
/// </remarks>
public sealed class SaveManager
{
    /// <summary>Имя основного файла сохранения.</summary>
    public const string SaveFileName = "savegame.json";

    /// <summary>Имя подкаталога для бэкапов.</summary>
    public const string BackupDirectoryName = "backups";

    private const string TempFileName = "savegame.tmp.json";

    private readonly SaveBackupManager _backupManager;

    /// <summary>Каталог, в котором хранятся сохранения.</summary>
    public string BaseDirectory { get; }

    /// <summary>Полный путь к основному файлу сохранения.</summary>
    public string SavePath { get; }

    /// <summary>Полный путь к каталогу бэкапов.</summary>
    public string BackupDirectory { get; }

    /// <summary>
    /// Создаёт менеджер сохранений.
    /// </summary>
    /// <param name="baseDirectory">Каталог сохранений. Создаётся при необходимости.</param>
    /// <param name="maxBackups">Сколько последних бэкапов хранить.</param>
    /// <exception cref="ArgumentException">Если <paramref name="baseDirectory"/> пуст.</exception>
    public SaveManager(string baseDirectory, int maxBackups = SaveBackupManager.DefaultMaxBackups)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseDirectory);

        BaseDirectory = baseDirectory;
        SavePath = Path.Combine(baseDirectory, SaveFileName);
        BackupDirectory = Path.Combine(baseDirectory, BackupDirectoryName);
        _backupManager = new SaveBackupManager(maxBackups);
    }

    /// <summary>
    /// Сохраняет игру: создаёт бэкап текущего файла, затем атомарно записывает новое сохранение.
    /// </summary>
    /// <param name="save">Состояние игры для записи.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="save"/> равен <see langword="null"/>.</exception>
    public async Task SaveAsync(SaveGame save)
    {
        ArgumentNullException.ThrowIfNull(save);

        Directory.CreateDirectory(BaseDirectory);

        // Бэкап предыдущего сохранения перед перезаписью.
        if (File.Exists(SavePath))
        {
            await _backupManager.CreateBackupAsync(SavePath, BackupDirectory).ConfigureAwait(false);
        }

        save.Metadata.SavedAtUtc = DateTimeOffset.UtcNow;
        string json = SaveSerializer.ToJson(save);

        string tempPath = Path.Combine(BaseDirectory, TempFileName);
        await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);

        // Атомарная замена основного файла.
        File.Move(tempPath, SavePath, overwrite: true);
    }

    /// <summary>
    /// Загружает игру, при повреждении основного файла автоматически откатываясь к бэкапу.
    /// </summary>
    /// <returns>Результат загрузки со статусом, сохранением и источником.</returns>
    public async Task<SaveLoadResult> LoadAsync()
    {
        if (!File.Exists(SavePath))
        {
            return new SaveLoadResult(SaveLoadStatus.NotFound, null, null, null);
        }

        // 1) Пытаемся прочитать основной файл.
        string? lastError = null;
        try
        {
            if (await TryLoadFileAsync(SavePath).ConfigureAwait(false) is { } primary)
            {
                return new SaveLoadResult(SaveLoadStatus.Loaded, primary, SavePath, null);
            }
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
        }

        // 2) Основной файл повреждён — откатываемся по бэкапам от новых к старым.
        foreach (string backup in _backupManager.ListBackups(BackupDirectory))
        {
            try
            {
                if (await TryLoadFileAsync(backup).ConfigureAwait(false) is { } recovered)
                {
                    // Восстанавливаем основной файл из рабочего бэкапа.
                    string json = SaveSerializer.ToJson(recovered);
                    await File.WriteAllTextAsync(SavePath, json).ConfigureAwait(false);
                    return new SaveLoadResult(SaveLoadStatus.Recovered, recovered, backup, null);
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
            }
        }

        return new SaveLoadResult(
            SaveLoadStatus.AllFailed,
            null,
            null,
            lastError ?? "Все доступные сохранения повреждены.");
    }

    /// <summary>
    /// Создаёт бэкап текущего сохранения вручную (например, перед важным событием или при выходе).
    /// </summary>
    /// <returns>Путь к созданному бэкапу или <see langword="null"/>, если сохранять нечего.</returns>
    public Task<string?> CreateBackupAsync()
        => _backupManager.CreateBackupAsync(SavePath, BackupDirectory);

    /// <summary>Список доступных бэкапов (от новых к старым).</summary>
    public IReadOnlyList<string> ListBackups() => _backupManager.ListBackups(BackupDirectory);

    /// <summary>Удаляет все сохранения и бэкапы (для тестов и сброса прогресса).</summary>
    public void DeleteAll()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }

        if (Directory.Exists(BackupDirectory))
        {
            Directory.Delete(BackupDirectory, recursive: true);
        }
    }

    private static async Task<SaveGame?> TryLoadFileAsync(string path)
    {
        string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
        SaveGame? save = SaveSerializer.FromJson(json);

        if (save is null)
        {
            return null;
        }

        return SaveMigrations.Migrate(save);
    }
}
