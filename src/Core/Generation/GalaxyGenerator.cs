using DarkSpace.Core.Data;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Generation;

/// <summary>
/// Процедурный генератор галактик.
/// </summary>
/// <remarks>
/// Вселенная проекта состоит из 256 галактик с 8-битным индексом (0–255).
/// Генерация детерминирована по паре (worldSeed, galaxyIndex). Галактика с индексом
/// <see cref="MaxGalaxyCount"/> (256) — «закрытая» галактика Odyalutai и не генерируется
/// в обычном потоке; см. <see cref="IsOdyalutaiIndex"/>.
/// </remarks>
public static class GalaxyGenerator
{
    /// <summary>Количество «обычных» галактик в мире.</summary>
    public const int MaxGalaxyCount = 256;

    /// <summary>Индекс закрытой галактики Odyalutai.</summary>
    public const int OdyalutaiIndex = 256;

    /// <summary>Проверяет, относится ли индекс к закрытой галактике Odyalutai.</summary>
    public static bool IsOdyalutaiIndex(int galaxyIndex) => galaxyIndex == OdyalutaiIndex;

    /// <summary>
    /// Генерирует одну галактику по мировому сиду и её индексу.
    /// </summary>
    /// <param name="worldSeed">Мировой сид (общий для всей вселенной).</param>
    /// <param name="galaxyIndex">Индекс галактики (0–255).</param>
    /// <returns>Детерминированное описание галактики.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Если индекс вне диапазона 0–255.</exception>
    public static GalaxyData GenerateGalaxy(ulong worldSeed, int galaxyIndex)
    {
        if (galaxyIndex < 0 || galaxyIndex >= MaxGalaxyCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(galaxyIndex),
                galaxyIndex,
                $"Индекс галактики должен быть в диапазоне 0–{MaxGalaxyCount - 1}.");
        }

        ulong seed = SeedHelper.Combine(worldSeed, "galaxy", (ulong)galaxyIndex);
        var random = new DeterministicRandom(seed);

        GalaxyType type = PickGalaxyType(random);
        string name = NameGenerator.GalaxyName(random);

        // Каждая галактика содержит от 40 до 400 звёздных систем в зависимости от типа.
        (int minStars, int maxStars) = StarCountRange(type);
        int starCount = random.NextInt(minStars, maxStars + 1);

        double density = Math.Round(random.NextDouble(0.2, 1.0), 3);
        bool hasBlackHole = type switch
        {
            GalaxyType.Elliptical => random.NextBool(0.35),
            GalaxyType.Ring => random.NextBool(0.20),
            GalaxyType.Peculiar => random.NextBool(0.50),
            _ => random.NextBool(0.10),
        };

        return new GalaxyData(galaxyIndex, name, type, starCount, density, hasBlackHole, seed);
    }

    /// <summary>
    /// Генерирует все галактики вселенной по мировому сиду.
    /// </summary>
    /// <param name="worldSeed">Мировой сид.</param>
    /// <returns>Массив из <see cref="MaxGalaxyCount"/> галактик в порядке индексов.</returns>
    public static GalaxyData[] GenerateUniverse(ulong worldSeed)
    {
        var galaxies = new GalaxyData[MaxGalaxyCount];
        for (int i = 0; i < MaxGalaxyCount; i++)
        {
            galaxies[i] = GenerateGalaxy(worldSeed, i);
        }

        return galaxies;
    }

    private static GalaxyType PickGalaxyType(DeterministicRandom random)
    {
        // Спиральные встречаются чаще прочих — как и в природе.
        int roll = random.NextInt(100);
        return roll switch
        {
            < 34 => GalaxyType.Spiral,
            < 55 => GalaxyType.Elliptical,
            < 72 => GalaxyType.Irregular,
            < 84 => GalaxyType.Dwarf,
            < 92 => GalaxyType.Ring,
            < 97 => GalaxyType.Lenticular,
            _ => GalaxyType.Peculiar,
        };
    }

    private static (int Min, int Max) StarCountRange(GalaxyType type) => type switch
    {
        GalaxyType.Spiral => (120, 400),
        GalaxyType.Elliptical => (150, 350),
        GalaxyType.Irregular => (60, 200),
        GalaxyType.Dwarf => (40, 90),
        GalaxyType.Ring => (80, 220),
        GalaxyType.Lenticular => (100, 280),
        GalaxyType.Peculiar => (50, 180),
        _ => (60, 200),
    };
}
