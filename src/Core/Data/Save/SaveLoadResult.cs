namespace DarkSpace.Core.Data.Save;

/// <summary>Результат операции загрузки сохранения.</summary>
public enum SaveLoadStatus
{
    /// <summary>Файл сохранения не найден.</summary>
    NotFound,

    /// <summary>Сохранение успешно загружено из основного файла.</summary>
    Loaded,

    /// <summary>Основной файл повреждён, но удалось восстановиться из бэкапа.</summary>
    Recovered,

    /// <summary>Не удалось загрузить ни основной файл, ни бэкапы.</summary>
    AllFailed,
}

/// <summary>
/// Результат загрузки сохранения.
/// </summary>
/// <param name="Status">Итоговый статус загрузки.</param>
/// <param name="Save">Загруженное сохранение или <see langword="null"/>.</param>
/// <param name="SourcePath">Путь к файлу, из которого загружено сохранение (если успешно).</param>
/// <param name="Error">Описание ошибки при неудаче.</param>
public sealed record SaveLoadResult(
    SaveLoadStatus Status,
    SaveGame? Save,
    string? SourcePath,
    string? Error);
