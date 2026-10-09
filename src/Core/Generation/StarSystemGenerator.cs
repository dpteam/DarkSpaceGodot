using DarkSpace.Core.Data;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Generation;

/// <summary>
/// Процедурный генератор звёздных систем.
/// </summary>
/// <remarks>
/// Генерация детерминирована по тройке (galaxySeed, galaxyIndex, systemIndex).
/// Система содержит от 1 до 12 планет; возможны чёрные дыры, нейтронные звёзды и кратные системы.
/// </remarks>
public static class StarSystemGenerator
{
    /// <summary>Минимальное число планет в системе.</summary>
    public const int MinPlanets = 1;

    /// <summary>Максимальное число планет в системе.</summary>
    public const int MaxPlanets = 12;

    /// <summary>
    /// Генерирует одну звёздную систему.
    /// </summary>
    /// <param name="galaxy">Галактика-родитель.</param>
    /// <param name="systemIndex">Индекс системы внутри галактики (0..<see cref="GalaxyData.StarCount"/> - 1).</param>
    /// <returns>Детерминированное описание системы.</returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="galaxy"/> равен <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Если индекс системы вне допустимого диапазона.</exception>
    public static StarSystemData GenerateSystem(GalaxyData galaxy, int systemIndex)
    {
        ArgumentNullException.ThrowIfNull(galaxy);

        if (systemIndex < 0 || systemIndex >= galaxy.StarCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(systemIndex),
                systemIndex,
                $"Индекс системы должен быть в диапазоне 0–{galaxy.StarCount - 1}.");
        }

        ulong seed = SeedHelper.Combine(galaxy.Seed, "system", (ulong)systemIndex);
        var random = new DeterministicRandom(seed);

        string name = NameGenerator.StarSystemName(random);
        SpectralClass star = PickSpectralClass(random);

        int planetCount = star == SpectralClass.BlackHole
            ? random.NextInt(MinPlanets, 4) // у чёрных дыр мало тел
            : random.NextInt(MinPlanets, MaxPlanets + 1);

        bool hasBelt = random.NextBool(0.55);
        bool hasStation = random.NextBool(0.65);
        EconomyType economy = PickEconomy(random);
        FactionType faction = PickFaction(random);

        (double x, double y, double z) = GeneratePosition(random, galaxy);

        return new StarSystemData(
            galaxy.Index,
            systemIndex,
            name,
            star,
            planetCount,
            hasBelt,
            hasStation,
            economy,
            faction,
            x,
            y,
            z,
            seed);
    }

    private static (double X, double Y, double Z) GeneratePosition(DeterministicRandom random, GalaxyData galaxy)
    {
        // Упрощённая модель распределения: спиральные — вдоль рукавов, прочие — облаком.
        double radius = random.NextDouble(2.0, 40.0);

        double x;
        double y;
        double z;
        if (galaxy.Type == GalaxyType.Spiral)
        {
            double arm = random.NextInt(3) * (2.0 * Math.PI / 3.0);
            double angle = arm + (radius * 0.35);
            x = Math.Cos(angle) * radius;
            y = random.NextDouble(-1.5, 1.5);
            z = Math.Sin(angle) * radius;
        }
        else
        {
            double angle = random.NextDouble(0.0, 2.0 * Math.PI);
            double jitter = random.NextDouble(0.6, 1.4);
            x = Math.Cos(angle) * radius * jitter;
            y = random.NextGaussian(0.0, 2.5);
            z = Math.Sin(angle) * radius * jitter;
        }

        return (Math.Round(x, 4), Math.Round(y, 4), Math.Round(z, 4));
    }

    private static SpectralClass PickSpectralClass(DeterministicRandom random)
    {
        // Реалистичное распределение: красных карликов больше всего, O и чёрных дыр — редкость.
        int roll = random.NextInt(1000);
        return roll switch
        {
            < 3 => SpectralClass.O,
            < 15 => SpectralClass.B,
            < 45 => SpectralClass.A,
            < 100 => SpectralClass.F,
            < 200 => SpectralClass.G,
            < 350 => SpectralClass.K,
            < 900 => SpectralClass.M,
            < 940 => SpectralClass.NeutronStar,
            < 960 => SpectralClass.BlackHole,
            _ => SpectralClass.Binary,
        };
    }

    private static EconomyType PickEconomy(DeterministicRandom random) => random.NextInt(6) switch
    {
        0 => EconomyType.Agricultural,
        1 => EconomyType.Industrial,
        2 => EconomyType.Technocratic,
        3 => EconomyType.Religious,
        4 => EconomyType.Military,
        _ => EconomyType.Pirate,
    };

    private static FactionType PickFaction(DeterministicRandom random) => random.NextInt(4) switch
    {
        0 => FactionType.EmpireOfDawn,
        1 => FactionType.VoidSyndicate,
        2 => FactionType.OrderOfKeepers,
        _ => FactionType.FreeClans,
    };
}
