using System;

namespace DarkSpace.Core.Systems.Physics;

/// <summary>
/// Физический профиль корабля: масса, тяга и скорость истечения.
/// </summary>
/// <remarks>
/// <para>
/// Динамика описывается законами Ньютона и ракетным уравнением Циолковского:
/// ускорение a = F·throttle / m (m = сухая масса + остаток топлива), расход массы mdot = F·throttle / v_e,
/// полный запас Δv = v_e·ln(m₀/m_f). Поэтому по мере выгорания топлива корабль разгоняется быстрее,
/// а дальность ограничена бюджетом Δv — как в реальной космонавтике и в Elite/Frontier.
/// </para>
/// <para>Единицы самосогласованы: масса — условные единицы, тяга — масса·SU/с², v_e — SU/с.</para>
/// </remarks>
/// <param name="DryMass">Сухая масса (без топлива).</param>
/// <param name="FuelMass">Начальная масса топлива.</param>
/// <param name="Thrust">Максимальная тяга (сила).</param>
/// <param name="ExhaustVelocity">Эффективная скорость истечения v_e.</param>
public readonly record struct ShipPhysicsProfile(double DryMass, double FuelMass, double Thrust, double ExhaustVelocity)
{
    /// <summary>Профиль корабля игрока по умолчанию.</summary>
    public static ShipPhysicsProfile Default => new(DryMass: 50.0, FuelMass: 50.0, Thrust: 4000.0, ExhaustVelocity: 1500.0);

    /// <summary>Полная масса снаряжённого корабля.</summary>
    public double TotalMass => DryMass + FuelMass;

    /// <summary>Полный запас характеристической скорости (бюджет Δv).</summary>
    public double DeltaVBudget => Astro.RocketDeltaV(ExhaustVelocity, TotalMass, DryMass);

    /// <summary>
    /// Текущее ускорение при заданной доле тяги и остатке топлива (второй закон Ньютона).
    /// </summary>
    /// <param name="throttle">Доля тяги 0..1.</param>
    /// <param name="remainingFuelMass">Остаток массы топлива.</param>
    public double Acceleration(double throttle, double remainingFuelMass)
    {
        double mass = DryMass + Math.Max(0.0, remainingFuelMass);
        return mass <= 0 ? 0.0 : (Thrust * Math.Clamp(throttle, 0.0, 1.0)) / mass;
    }

    /// <summary>Текущий секундный расход массы при заданной доле тяги.</summary>
    public double MassFlowRate(double throttle)
        => Astro.MassFlowRate(Thrust * Math.Clamp(throttle, 0.0, 1.0), ExhaustVelocity);

    /// <summary>
    /// Остаток характеристической скорости при текущей массе топлива: v_e·ln((m_сух+m_топл)/m_сух).
    /// </summary>
    /// <param name="remainingFuelMass">Остаток массы топлива.</param>
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
