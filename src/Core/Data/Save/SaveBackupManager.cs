using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DarkSpace.Core.Data.Save;

/// <summary>
/// Управление резервными копиями сохранений с ротацией.
/// </summary>
/// <remarks>
/// Бэкапы хранятся в отдельной подпапке и именуются по метке времени в формате
/// <c>savebackup-yyyyMMddTHHmmssfff.json</c>. Хранятся только последние <see cref="MaxBackups"/>
/// копий; более старые удаляются при ротации.
/// </remarks>
public sealed class SaveBackupManager
{
    /// <summary>Количество бэкапов, хранимых по умолчанию.</summary>
    public const int DefaultMaxBackups = 5;

    private const string BackupPrefix = "savebackup-";
    private const string BackupExtension = ".json";
    private const string BackupTimestampFormat = "yyyyMMddTHHmmssfff";

    /// <summary>Максимальное количество одновременно хранимых бэкапов.</summary>
    public int MaxBackups { get; }

    /// <summary>
    /// Создаёт менеджер бэкапов.
    /// </summary>
    /// <param name="maxBackups">Сколько последних бэкапов хранить. Должно быть не меньше 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Если <paramref name="maxBackups"/> меньше 1.</exception>
    public SaveBackupManager(int maxBackups = DefaultMaxBackups)
    {
        if (maxBackups < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBackups), maxBackups, "Нужно хранить хотя бы один бэкап.");
        }

        MaxBackups = maxBackups;
    }

    /// <summary>
    /// Создаёт резервную копию файла сохранения и выполняет ротацию.
    /// </summary>
    /// <param name="sourceFile">Путь к существующему файлу сохранения.</param>
    /// <param name="backupDirectory">Каталог для бэкапов (создаётся при необходимости).</param>
    /// <param name="timestamp">Метка времени для имени файла (по умолчанию — текущее UTC).</param>
    /// <returns>Путь к созданному бэкапу или <see langword="null"/>, если исходный файл не существует.</returns>
    public async Task<string?> CreateBackupAsync(
        string sourceFile,
        string backupDirectory,
        DateTimeOffset? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceFile);
        ArgumentException.ThrowIfNullOrEmpty(backupDirectory);

        if (!File.Exists(sourceFile))
        {
            return null;
        }

        Directory.CreateDirectory(backupDirectory);

        string stamp = (timestamp ?? DateTimeOffset.UtcNow).UtcDateTime.ToString(BackupTimestampFormat, CultureInfo.InvariantCulture);
        string backupPath = Path.Combine(backupDirectory, $"{BackupPrefix}{stamp}{BackupExtension}");

        // Гарантируем уникальность имени даже в пределах одной миллисекунды.
        int suffix = 1;
        while (File.Exists(backupPath))
        {
            backupPath = Path.Combine(backupDirectory, $"{BackupPrefix}{stamp}-{suffix}{BackupExtension}");
            suffix++;
        }

        byte[] content = await File.ReadAllBytesAsync(sourceFile).ConfigureAwait(false);
        await File.WriteAllBytesAsync(backupPath, content).ConfigureAwait(false);

        Rotate(backupDirectory);
        return backupPath;
    }

    /// <summary>
    /// Возвращает список бэкапов, отсортированный от самого нового к самому старому.
    /// </summary>
    /// <param name="backupDirectory">Каталог с бэкапами.</param>
    public IReadOnlyList<string> ListBackups(string backupDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(backupDirectory);

        if (!Directory.Exists(backupDirectory))
        {
            return Array.Empty<string>();
        }

        return Directory
            .EnumerateFiles(backupDirectory, $"{BackupPrefix}*{BackupExtension}")
            .OrderByDescending(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Возвращает путь к самому свежему бэкапу или <see langword="null"/>, если бэкапов нет.
    /// </summary>
    public string? GetLatestBackup(string backupDirectory)
        => ListBackups(backupDirectory).FirstOrDefault();

    /// <summary>
    /// Удаляет бэкапы сверх лимита <see cref="MaxBackups"/>.
    /// </summary>
    /// <returns>Количество удалённых файлов.</returns>
    public int Rotate(string backupDirectory)
    {
        IReadOnlyList<string> backups = ListBackups(backupDirectory);
        int removed = 0;

        for (int i = MaxBackups; i < backups.Count; i++)
        {
            try
            {
                File.Delete(backups[i]);
                removed++;
            }
            catch (IOException)
            {
                // Файл занят — пропускаем, удалим при следующей ротации.
            }
        }

        return removed;
    }
}
