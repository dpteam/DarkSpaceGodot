using System;

namespace DarkSpace.Core.Systems.Physics;

/// <summary>
/// Физический профиль корабля: масса, тяга, скорость истечения и вращательная динамика.
/// </summary>
/// <remarks>
/// <para>
/// Поступательное движение — по законам Ньютона и уравнению Циолковского:
/// ускорение a = F/m (m = сухая масса + топливо), расход mdot = F/v_e,
/// полный запас Δv = v_e·ln(m₀/m_f).
/// </para>
/// <para>
/// Вращательное движение — угловая динамика: момент инерции задаётся через угловое ускорение
/// <see cref="AngularAcceleration"/> (рад/с², эффективно τ/I). Скорость вращения ограничена
/// <see cref="MaxAngularRate"/>. <see cref="AngularDamping"/> моделирует гироскопы/стабилизаторы
/// (реакционная система): при 0 вращение чисто ньютоновское (угловой момент сохраняется),
/// при &gt;0 корабль «доворачивается» и плавно останавливается — как в Frontier.
/// </para>
/// <para>Единицы самосогласованы: масса — условные единицы, тяга — масса·SU/с², v_e — SU/с.</para>
/// </remarks>
/// <param name="DryMass">Сухая масса (без топлива).</param>
/// <param name="FuelMass">Начальная масса топлива.</param>
/// <param name="Thrust">Максимальная тяга (сила).</param>
/// <param name="ExhaustVelocity">Эффективная скорость истечения v_e.</param>
/// <param name="AngularAcceleration">Максимальное угловое ускорение от органов управления (рад/с²).</param>
/// <param name="MaxAngularRate">Максимальная скорость вращения (рад/с).</param>
/// <param name="AngularDamping">Коэффициент углового демпфирования (1/с): 0 — чистый Ньютон, &gt;0 — стабилизаторы.</param>
/// <param name="LateralThrustFactor">Доля тяги латеральных/вертикальных двигателей от основной (0..1).</param>
/// <param name="ReverseThrustFactor">Доля тяги обратного хода от основной (0..1).</param>
/// <param name="MaxSpeed">Максимальная скорость в элитовской модели полёта (SU/с).</param>
/// <param name="SpeedAcceleration">Ускорение изменения скорости в элитовской модели (SU/с²).</param>
/// <param name="TurnResponsiveness">Отзывчивость разворота в элитовской модели (1/с): скорость, с которой
/// угловая скорость выходит на заданную.</param>
public readonly record struct ShipPhysicsProfile(
    double DryMass,
    double FuelMass,
    double Thrust,
    double ExhaustVelocity,
    double AngularAcceleration,
    double MaxAngularRate,
    double AngularDamping,
    double LateralThrustFactor,
    double ReverseThrustFactor,
    double MaxSpeed,
    double SpeedAcceleration,
    double TurnResponsiveness)
{
    /// <summary>Профиль корабля игрока по умолчанию.</summary>
    public static ShipPhysicsProfile Default => new(
        DryMass: 50.0,
        FuelMass: 50.0,
        Thrust: 4000.0,
        ExhaustVelocity: 1500.0,
        AngularAcceleration: 5.0,
        MaxAngularRate: 1.8,
        AngularDamping: 3.0,
        LateralThrustFactor: 1.0,
        ReverseThrustFactor: 1.0,
        MaxSpeed: 220.0,
        SpeedAcceleration: 110.0,
        TurnResponsiveness: 6.0);

    /// <summary>Полная масса снаряжённого корабля.</summary>
    public double TotalMass => DryMass + FuelMass;

    /// <summary>Полный запас характеристической скорости (бюджет Δv).</summary>
    public double DeltaVBudget => Astro.RocketDeltaV(ExhaustVelocity, TotalMass, DryMass);

    /// <summary>
    /// Текущее ускорение при заданной тяге и остатке топлива (второй закон Ньютона).
    /// </summary>
    /// <param name="thrustMagnitude">Модуль тяги (0..Thrust·k; доля указывается через управление).</param>
    /// <param name="remainingFuelMass">Остаток массы топлива.</param>
    public double Acceleration(double thrustMagnitude, double remainingFuelMass)
    {
        double mass = DryMass + Math.Max(0.0, remainingFuelMass);
        return mass <= 0 ? 0.0 : Math.Max(0.0, thrustMagnitude) / mass;
    }

    /// <summary>Текущий секундный расход массы при заданной тяге.</summary>
    public double MassFlowRate(double thrustMagnitude)
        => Astro.MassFlowRate(thrustMagnitude, ExhaustVelocity);

    /// <summary>
    /// Остаток характеристической скорости при текущей массе топлива: v_e·ln((m_сух+m_топл)/m_сух).
    /// </summary>
    public double RemainingDeltaV(double remainingFuelMass)
    {
        double fuel = Math.Max(0.0, remainingFuelMass);
        if (fuel <= 0)
        {
            return 0.0;
        }

        return ExhaustVelocity * Math.Log((DryMass + fuel) / DryMass);
    }
}
