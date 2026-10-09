namespace DarkSpace.Core.Utils;

/// <summary>
/// Хелперы для детерминированного смешивания сидов.
/// </summary>
/// <remarks>
/// Используется совместно с <see cref="DeterministicRandom"/>: например, сид галактики
/// складывается из мирового сида и индекса галактики, а сид системы — из сида галактики
/// и индекса системы.
/// </remarks>
public static class SeedHelper
{
    private const ulong FnvOffsetBasis = 0xCBF29CE484222325UL;
    private const ulong FnvPrime = 0x100000001B3UL;
    private const ulong MixConstant = 0x9E3779B97F4A7C15UL;

    /// <summary>Смешивает произвольный набор сидов в один детерминированный сид.</summary>
    /// <param name="seedA">Первый сид.</param>
    /// <param name="seedB">Второй сид.</param>
    /// <param name="extra">Дополнительные сиды (необязательно).</param>
    public static ulong Combine(ulong seedA, ulong seedB, params ulong[] extra)
    {
        ulong hash = Mix(seedA);
        hash ^= Mix(seedB + MixConstant + (hash << 6) + (hash >> 2));
        if (extra is { Length: > 0 })
        {
            foreach (ulong value in extra)
            {
                hash ^= Mix(value + MixConstant + (hash << 6) + (hash >> 2));
            }
        }

        return hash;
    }

    /// <summary>
    /// Смешивает родительский сид, строковый тег подсистемы и дополнительный сид в один сид.
    /// </summary>
    /// <param name="seedA">Родительский сид.</param>
    /// <param name="tag">Тег подсистемы (например, «galaxy», «system», «planet»).</param>
    /// <param name="seedB">Дополнительный сид (например, индекс).</param>
    public static ulong Combine(ulong seedA, string tag, ulong seedB)
        => Combine(seedA, FromString(tag), seedB);

    /// <summary>Возвращает детерминированный 64-битный хеш строки (алгоритм FNV-1a).</summary>
    /// <exception cref="ArgumentNullException">Если <paramref name="text"/> равен <see langword="null"/>.</exception>
    public static ulong FromString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        ulong hash = FnvOffsetBasis;
        foreach (char c in text)
        {
            hash ^= c;
            hash *= FnvPrime;
        }

        return hash;
    }

    /// <summary>Создаёт новый сид, производный от родительского и строкового тега.</summary>
    public static ulong Derive(ulong parentSeed, string tag) => Combine(parentSeed, FromString(tag));

    /// <summary>Финальное перемешивание значения (финализатор SplitMix64).</summary>
    private static ulong Mix(ulong value)
    {
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
