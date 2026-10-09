using DarkSpace.Core.Data;

namespace DarkSpace.Core.Systems.Orbital;

/// <summary>
/// Шкала звёздной системы: размеры тел и расстояния, согласованные между моделью и рендером.
/// </summary>
/// <remarks>
/// <para>
/// Реальные пропорции (звезда ~700 000 км, планета ~6000 км, орбита ~150 000 000 км) дают
/// практически невидимые планеты, поэтому берём подход Elite: сохраняем <b>правильное
/// перспективное соотношение</b> (видимый размер ∝ радиус / расстояние) и разумные пропорции
/// (звезда заметно больше планет, планеты — разные, орбиты — далеко и с геометрическим шагом),
/// но в удобном для игры масштабе условных единиц (SU).
/// </para>
/// <para>
/// Единая точка истины: и <see cref="StarSystemModel"/>, и слои представления (карта, полёт)
/// берут размеры отсюда, поэтому «всё сходится».
/// </para>
/// </remarks>
public static class SystemScale
{
    /// <summary>Большая полуось самой внутренней орбиты (SU).</summary>
    public const double InnerOrbit = 80.0;

    /// <summary>Отношение соседних орбит (геометрическая прогрессия — как титус-боде, но проще).</summary>
    public const double OrbitRatio = 1.5;

    /// <summary>Длина сглаживания гравитации (SU) — убирает сингулярность в ядре звезды.</summary>
    public const double GravitySoftening = 20.0;

    /// <summary>Зазор между поверхностью планеты и точкой стыковки (SU).</summary>
    public const double DockingMargin = 6.0;

    /// <summary>Радиус станции (SU).</summary>
    public const double StationRadius = 4.0;

    /// <summary>Зазор между станцией и точкой стыковки (SU).</summary>
    public const double StationDockMargin = 5.0;

    /// <summary>Отступ орбиты станции от поверхности планеты-хозяина (SU).</summary>
    public const double StationOrbitMargin = 30.0;

    /// <summary>Минимальный период обращения станции вокруг планеты (с).</summary>
    public const double StationPeriodMinSeconds = 90.0;

    /// <summary>Максимальный период обращения станции вокруг планеты (с).</summary>
    public const double StationPeriodMaxSeconds = 240.0;

    /// <summary>Максимальная большая полуось, до которой тянется геометрическое распределение (SU).</summary>
    public const double MaxOrbit = 9000.0;

    /// <summary>Радиус звезды (SU) по спектральному классу. Звезда заметно крупнее планет.</summary>
    /// <remarks>
    /// Компактные объекты (нейтронная звезда, чёрная дыра) намеренно малы: нейтронная звезда
    /// физически меньше газового гиганта, поэтому «звезда крупнее планеты» для неё не выполняется.
    /// </remarks>
    public static double StarRadius(SpectralClass star) => star switch
    {
        SpectralClass.O => 46.0,
        SpectralClass.B => 40.0,
        SpectralClass.A => 34.0,
        SpectralClass.F => 30.0,
        SpectralClass.G => 26.0,
        SpectralClass.K => 22.0,
        SpectralClass.M => 18.0,
        SpectralClass.NeutronStar => 6.0,
        SpectralClass.Binary => 32.0,
        SpectralClass.BlackHole => 12.0,
        _ => 24.0,
    };

    /// <summary>Радиус планеты (SU) по её типу. Газовые гиганты крупнее каменистых.</summary>
    public static double PlanetRadius(PlanetType type) => type switch
    {
        PlanetType.GasGiant => 8.0,
        PlanetType.Ice => 3.0,
        PlanetType.Oceanic => 2.8,
        PlanetType.Jungle => 2.6,
        PlanetType.Exotic => 2.4,
        PlanetType.Toxic => 2.1,
        PlanetType.Radioactive => 1.9,
        PlanetType.Volcanic => 1.7,
        PlanetType.Desert => 1.7,
        PlanetType.Rocky => 1.4,
        _ => 2.0,
    };

    /// <summary>
    /// Гравитационный параметр μ звезды (SU³/с²) по спектральному классу. Размер и масса растут
    /// согласованно (крупнее звезда — сильнее гравитация), поэтому периоды орбит «звучат» правдоподобно.
    /// </summary>
    public static double GravitationalParameter(SpectralClass star) => star switch
    {
        SpectralClass.O => 1600.0,
        SpectralClass.B => 1350.0,
        SpectralClass.A => 1100.0,
        SpectralClass.F => 850.0,
        SpectralClass.G => 640.0,
        SpectralClass.K => 470.0,
        SpectralClass.M => 330.0,
        SpectralClass.NeutronStar => 2000.0,
        SpectralClass.Binary => 850.0,
        SpectralClass.BlackHole => 2400.0,
        _ => 500.0,
    };

    /// <summary>Большая полуось орбиты планеты с индексом <paramref name="index"/> (геометрический шаг).</summary>
    /// <param name="index">Индекс планеты (0 — ближайшая к звезде).</param>
    public static double OrbitSemiMajorAxis(int index)
    {
        double a = InnerOrbit * Math.Pow(OrbitRatio, Math.Max(0, index));
        return Math.Min(a, MaxOrbit);
    }
}
