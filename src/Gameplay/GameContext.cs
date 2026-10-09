using DarkSpace.Core.Data;
using DarkSpace.Core.Data.Save;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Utils.Logging;

namespace DarkSpace.Gameplay;

/// <summary>
/// Глобальный контекст игровой сессии, разделяемый между сценами.
/// </summary>
/// <remarks>
/// Простая статическая точка доступа: UI-сцены (меню, обзор вселенной) читают и пишут
/// текущую сессию здесь, не завися от порядка загрузки сцен и без Autoload-синглтона.
/// Не хранит игровые данные сверх необходимого: только сессию и каталог сохранений.
/// </remarks>
public static class GameContext
{
    /// <summary>Мировой сид по умолчанию (используется, если сессия ещё не создана).</summary>
    public const ulong DefaultWorldSeed = 0x4F4459414C555441UL; // "ODYALUTA"

    /// <summary>Текущая игровая сессия или <see langword="null"/>, если игра ещё не начата.</summary>
    public static GameSession? Session { get; private set; }

    /// <summary>Каталог сохранений (в слое движка — путь к <c>user://saves</c>).</summary>
    public static string SaveDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Создаёт новую сессию с заданным мировым сидом.
    /// </summary>
    /// <param name="worldSeed">Мировой сид вселенной.</param>
    /// <returns>Созданная сессия.</returns>
    public static GameSession StartNew(ulong worldSeed)
    {
        Session = new GameSession(worldSeed, SaveDirectory);
        Log.Info("GameContext", $"Новая сессия. Мировой сид: 0x{worldSeed:X16}.");
        return Session;
    }

    /// <summary>Возвращает текущую сессию, создавая её с сидом по умолчанию при необходимости.</summary>
    public static GameSession GetOrCreate()
        => Session ?? StartNew(DefaultWorldSeed);

    /// <summary>Генерирует галактику в текущей сессии по индексу.</summary>
    /// <param name="galaxyIndex">Индекс галактики.</param>
    public static GalaxyData GetGalaxy(int galaxyIndex)
        => GetOrCreate().GetGalaxy(galaxyIndex);

    /// <summary>Сбрасывает сессию (например, при выходе в главное меню с опцией «начать заново»).</summary>
    public static void Reset() => Session = null;
}
