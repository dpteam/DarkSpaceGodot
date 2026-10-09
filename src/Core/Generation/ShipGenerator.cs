using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Generation;

/// <summary>
/// Процедурный генератор кораблей.
/// </summary>
/// <remarks>
/// Генерация детерминирована по переданному сиду. Характеристики зависят от класса корабля,
/// описание содержит элементы чёрного юмора (см. <see cref="DescriptionGenerator"/>).
/// </remarks>
public static class ShipGenerator
{
    private static readonly string[] ModulePool =
    {
        "Двигатель «Титан-3»", "Щиты «Обманка»", "Буровой лазер", "Сканер дальнего действия",
        "Грузовой отсек", "Ремонтный дрон", "Ракетная установка", "Контрабандный трюм",
        "Оранжерея", "Капсула для медитации", "Кофемашина «Вера»", "Тайник для отчётов",
    };

    /// <summary>
    /// Генерирует корабль заданного класса по сиду.
    /// </summary>
    /// <param name="seed">Сид генерации.</param>
    /// <param name="type">Класс корабля.</param>
    /// <returns>Детерминированное описание корабля.</returns>
    public static ShipData GenerateShip(ulong seed, ShipType type)
    {
        var random = new DeterministicRandom(SeedHelper.Combine(seed, "ship", (ulong)type));

        string name = NameGenerator.ShipName(random);
        ShipProfile profile = ProfileFor(type);

        double cargo = Math.Round(random.NextDouble(profile.CargoMin, profile.CargoMax), 1);
        double speed = Math.Round(random.NextDouble(profile.SpeedMin, profile.SpeedMax), 1);
        double maneuverability = Math.Round(random.NextDouble(profile.ManeuverMin, profile.ManeuverMax), 3);
        double armor = Math.Round(random.NextDouble(profile.ArmorMin, profile.ArmorMax), 1);
        double shields = Math.Round(random.NextDouble(profile.ShieldMin, profile.ShieldMax), 1);
        double powerDraw = Math.Round(cargo * 0.05 + shields * 0.1 + random.NextDouble(0.0, 5.0), 2);
        double jumpRange = Math.Round(random.NextDouble(profile.JumpMin, profile.JumpMax), 2);

        int moduleCount = random.NextInt(2, 5);
        var modules = new List<string>();
        for (int i = 0; i < moduleCount; i++)
        {
            string module = random.Pick(ModulePool);
            if (!modules.Contains(module))
            {
                modules.Add(module);
            }
        }

        var core = new ShipData(
            name,
            type,
            cargo,
            speed,
            maneuverability,
            armor,
            shields,
            powerDraw,
            jumpRange,
            modules,
            string.Empty,
            seed);

        string description = DescriptionGenerator.DescribeShip(random, core);
        return core with { Description = description };
    }

    private static ShipProfile ProfileFor(ShipType type) => type switch
    {
        ShipType.Scout => new ShipProfile(5, 15, 300, 450, 0.7, 0.95, 5, 15, 8, 20, 25, 60),
        ShipType.Trader => new ShipProfile(200, 800, 60, 120, 0.1, 0.35, 40, 120, 20, 80, 8, 25),
        ShipType.Frigate => new ShipProfile(40, 120, 150, 250, 0.4, 0.7, 80, 200, 60, 150, 12, 35),
        ShipType.Cruiser => new ShipProfile(80, 250, 100, 180, 0.25, 0.5, 200, 450, 150, 350, 10, 30),
        ShipType.Battleship => new ShipProfile(120, 350, 70, 130, 0.15, 0.35, 400, 800, 300, 600, 8, 22),
        ShipType.Miner => new ShipProfile(300, 900, 50, 100, 0.1, 0.3, 60, 180, 40, 120, 6, 18),
        ShipType.Courier => new ShipProfile(10, 40, 400, 600, 0.6, 0.9, 10, 30, 15, 45, 20, 45),
        ShipType.Science => new ShipProfile(60, 200, 120, 220, 0.4, 0.7, 50, 160, 50, 140, 15, 40),
        _ => new ShipProfile(50, 150, 100, 200, 0.3, 0.6, 50, 150, 40, 120, 10, 30),
    };

    /// <summary>Числовой профиль характеристик для класса корабля.</summary>
    private readonly record struct ShipProfile(
        double CargoMin,
        double CargoMax,
        double SpeedMin,
        double SpeedMax,
        double ManeuverMin,
        double ManeuverMax,
        double ArmorMin,
        double ArmorMax,
        double ShieldMin,
        double ShieldMax,
        double JumpMin,
        double JumpMax);
}
