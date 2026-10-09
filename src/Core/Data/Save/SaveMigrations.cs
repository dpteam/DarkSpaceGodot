namespace DarkSpace.Core.Data.Save;

/// <summary>
/// Миграции схемы сохранений между версиями.
/// </summary>
/// <remarks>
/// При изменении структуры <see cref="SaveGame"/> увеличивайте <see cref="SaveGame.CurrentVersion"/>
/// и добавляйте сюда шаг миграции со старой версии на новую. Миграции применяются последовательно.
/// </remarks>
public static class SaveMigrations
{
    /// <summary>
    /// Приводит сохранение к актуальной версии схемы.
    /// </summary>
    /// <param name="save">Загруженное сохранение.</param>
    /// <returns>То же сохранение, обновлённое до <see cref="SaveGame.CurrentVersion"/>.</returns>
    /// <exception cref="System.ArgumentNullException">Если <paramref name="save"/> равен <see langword="null"/>.</exception>
    /// <exception cref="SaveCorruptedException">Если версия сохранения новее, чем поддерживает текущая сборка.</exception>
    public static SaveGame Migrate(SaveGame save)
    {
        System.ArgumentNullException.ThrowIfNull(save);

        if (save.Version > SaveGame.CurrentVersion)
        {
            throw new SaveCorruptedException(
                $"Сохранение версии {save.Version} новее поддерживаемой ({SaveGame.CurrentVersion}). " +
                "Обновите игру или удалите сохранение.");
        }

        // Пример последовательных миграций:
        // if (save.Version < 1) { ...; save.Version = 1; }
        // if (save.Version < 2) { ...; save.Version = 2; }

        save.Version = SaveGame.CurrentVersion;
        return save;
    }
}
