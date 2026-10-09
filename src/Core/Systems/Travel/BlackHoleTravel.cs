using System;
using DarkSpace.Core.Data;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Systems.Travel;

/// <summary>
/// Результат прыжка через чёрную дыру.
/// </summary>
/// <param name="Outcome">Итог прыжка.</param>
/// <param name="TargetGalaxyIndex">Индекс галактики назначения.</param>
/// <param name="Narrative">Художественное описание произошедшего.</param>
public sealed record JumpResult(JumpOutcome Outcome, int TargetGalaxyIndex, string Narrative);

/// <summary>
/// Механика прыжка через чёрную дыру в другую галактику.
/// </summary>
/// <remarks>
/// Прыжок рискован: исход зависит от характеристик корабля (дальность прыжка, броня, щиты).
/// Результат детерминирован по переданному сиду, поэтому воспроизводим на клиенте и сервере.
/// </remarks>
public static class BlackHoleTravel
{
    /// <summary>Базовый шанс гибели корабля при прыжке (для «идеально плохого» корабля).</summary>
    public const double BaseDestroyChance = 0.35;

    /// <summary>
    /// Рассчитывает исход прыжка через чёрную дыру.
    /// </summary>
    /// <param name="seed">Сид прыжка (обычно уникален для попытки).</param>
    /// <param name="ship">Корабль, совершающий прыжок.</param>
    /// <param name="targetGalaxyIndex">Индекс галактики назначения (0–256; 256 — Odyalutai).</param>
    /// <returns>Исход прыжка с описанием.</returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="ship"/> равен <see langword="null"/>.</exception>
    public static JumpResult ComputeJump(ulong seed, ShipData ship, int targetGalaxyIndex)
    {
        ArgumentNullException.ThrowIfNull(ship);

        var random = new DeterministicRandom(SeedHelper.Combine(seed, "blackhole", (ulong)targetGalaxyIndex));
        double protection = ComputeProtection(ship);

        double destroyChance = BaseDestroyChance * (1.0 - protection);
        double roll = random.NextDouble();

        if (roll < destroyChance)
        {
            return new JumpResult(
                JumpOutcome.Destroyed,
                targetGalaxyIndex,
                "Корабль сложило в точку раньше, чем успел пискнуть сигнал тревоги. " +
                "Великий Архитектор сделает бэкап вашей души. Когда-нибудь. Возможно.");
        }

        // Выжившие с вероятностью 50% получают повреждения.
        if (random.NextBool(0.5))
        {
            return new JumpResult(
                JumpOutcome.Damaged,
                targetGalaxyIndex,
                "Прыжок удался, но корпус помнит об этом до сих пор. Часть панелей " +
                "теперь работает исключительно на честном слове — то есть штатно.");
        }

        return new JumpResult(
            JumpOutcome.Safe,
            targetGalaxyIndex,
            "Пространство смилостивилось. Вы в новой галактике, целы и всё ещё удивлены этим.");
    }

    /// <summary>
    /// Вычисляет «защиту» корабля от гравитационного разрушения (0 = беззащитен, 1 = идеален).
    /// </summary>
    /// <param name="ship">Корабль.</param>
    public static double ComputeProtection(ShipData ship)
    {
        ArgumentNullException.ThrowIfNull(ship);

        double jumpComponent = Math.Clamp(ship.JumpRange / 45.0, 0.0, 1.0) * 0.4;
        double armorComponent = Math.Clamp(ship.Armor / 800.0, 0.0, 1.0) * 0.3;
        double shieldComponent = Math.Clamp(ship.Shields / 600.0, 0.0, 1.0) * 0.3;

        return Math.Clamp(jumpComponent + armorComponent + shieldComponent, 0.0, 1.0);
    }
}
