using DarkSpace.Core.Data;
using Godot;

namespace DarkSpace.UI.Widgets;

/// <summary>
/// Процедурные визуальные характеристики планет и звёзд (цвета, размеры).
/// </summary>
/// <remarks>
/// Общий модуль для слоя представления: используется и фоном главного меню
/// (<c>shaders/planet_background.gdshader</c>), и 3D-картой системы.
/// Цвета не хранятся как ассеты — они выводятся из типа планеты/спектрального класса.
/// </remarks>
public static class PlanetVisuals
{
    /// <summary>Цветовая палитра планеты.</summary>
    public readonly record struct Palette(
        Color Deep,
        Color Land,
        Color High,
        Color Atmosphere,
        bool HasWater,
        bool GasBands,
        float SeaLevel,
        float Clouds,
        float Emissive,
        Color EmissiveColor);

    /// <summary>Возвращает палитру для типа планеты.</summary>
    public static Palette For(PlanetType type) => type switch
    {
        PlanetType.Rocky => new(
            new Color(0.14f, 0.12f, 0.11f), new Color(0.42f, 0.35f, 0.27f), new Color(0.62f, 0.60f, 0.56f),
            new Color(0.35f, 0.5f, 0.8f), false, false, 0.5f, 0.05f, 0f, new Color(0, 0, 0)),
        PlanetType.GasGiant => new(
            new Color(0.30f, 0.20f, 0.12f), new Color(0.78f, 0.62f, 0.40f), new Color(0.92f, 0.86f, 0.72f),
            new Color(0.7f, 0.5f, 0.3f), false, true, 0.5f, 0.12f, 0f, new Color(0, 0, 0)),
        PlanetType.Ice => new(
            new Color(0.10f, 0.18f, 0.30f), new Color(0.72f, 0.82f, 0.92f), new Color(0.95f, 0.98f, 1.0f),
            new Color(0.6f, 0.8f, 1.0f), false, false, 0.5f, 0.35f, 0f, new Color(0, 0, 0)),
        PlanetType.Oceanic => new(
            new Color(0.01f, 0.08f, 0.25f), new Color(0.10f, 0.45f, 0.20f), new Color(0.75f, 0.80f, 0.65f),
            new Color(0.25f, 0.6f, 1.0f), true, false, 0.62f, 0.30f, 0f, new Color(0, 0, 0)),
        PlanetType.Volcanic => new(
            new Color(0.10f, 0.02f, 0.01f), new Color(0.22f, 0.10f, 0.08f), new Color(0.45f, 0.20f, 0.12f),
            new Color(1.0f, 0.35f, 0.10f), false, false, 0.5f, 0.10f, 1.0f, new Color(1.0f, 0.30f, 0.05f)),
        PlanetType.Toxic => new(
            new Color(0.06f, 0.12f, 0.04f), new Color(0.35f, 0.42f, 0.12f), new Color(0.60f, 0.68f, 0.20f),
            new Color(0.6f, 0.9f, 0.2f), false, false, 0.5f, 0.25f, 0.5f, new Color(0.7f, 0.9f, 0.2f)),
        PlanetType.Radioactive => new(
            new Color(0.04f, 0.10f, 0.04f), new Color(0.15f, 0.28f, 0.10f), new Color(0.35f, 0.55f, 0.18f),
            new Color(0.5f, 1.0f, 0.3f), false, false, 0.5f, 0.12f, 0.8f, new Color(0.5f, 1.0f, 0.25f)),
        PlanetType.Desert => new(
            new Color(0.20f, 0.14f, 0.08f), new Color(0.72f, 0.55f, 0.30f), new Color(0.88f, 0.78f, 0.55f),
            new Color(0.8f, 0.6f, 0.3f), false, false, 0.5f, 0.02f, 0f, new Color(0, 0, 0)),
        PlanetType.Jungle => new(
            new Color(0.02f, 0.10f, 0.08f), new Color(0.08f, 0.32f, 0.10f), new Color(0.55f, 0.65f, 0.35f),
            new Color(0.3f, 0.8f, 0.5f), true, false, 0.55f, 0.28f, 0f, new Color(0, 0, 0)),
        PlanetType.Exotic => new(
            new Color(0.10f, 0.02f, 0.16f), new Color(0.35f, 0.10f, 0.42f), new Color(0.75f, 0.45f, 0.85f),
            new Color(0.8f, 0.4f, 1.0f), false, false, 0.5f, 0.20f, 0.6f, new Color(0.9f, 0.3f, 1.0f)),
        _ => new(
            new Color(0.10f, 0.10f, 0.12f), new Color(0.40f, 0.38f, 0.35f), new Color(0.65f, 0.63f, 0.60f),
            new Color(0.4f, 0.6f, 0.9f), false, false, 0.5f, 0.10f, 0f, new Color(0, 0, 0)),
    };

    /// <summary>Базовый цвет поверхности планеты (для 3D-сферы).</summary>
    public static Color BaseColor(PlanetType type) => For(type).Land;

    /// <summary>Радиус 3D-сферы планеты в условных единицах сцены.</summary>
    public static float Radius(PlanetType type) => type switch
    {
        PlanetType.GasGiant => 0.95f,
        PlanetType.Oceanic or PlanetType.Jungle => 0.55f,
        PlanetType.Volcanic or PlanetType.Desert => 0.5f,
        PlanetType.Exotic => 0.6f,
        _ => 0.45f,
    };

    /// <summary>Цвет и интенсивность свечения звезды по спектральному классу.</summary>
    public static (Color Color, float Energy) Star(SpectralClass spectral) => spectral switch
    {
        SpectralClass.O => (new Color(0.55f, 0.70f, 1.0f), 3.5f),
        SpectralClass.B => (new Color(0.65f, 0.78f, 1.0f), 3.0f),
        SpectralClass.A => (new Color(0.85f, 0.90f, 1.0f), 2.6f),
        SpectralClass.F => (new Color(1.0f, 0.97f, 0.90f), 2.2f),
        SpectralClass.G => (new Color(1.0f, 0.92f, 0.65f), 2.0f),
        SpectralClass.K => (new Color(1.0f, 0.75f, 0.45f), 1.7f),
        SpectralClass.M => (new Color(1.0f, 0.50f, 0.35f), 1.4f),
        SpectralClass.NeutronStar => (new Color(0.75f, 0.85f, 1.0f), 5.0f),
        SpectralClass.Binary => (new Color(1.0f, 0.90f, 0.70f), 2.5f),
        SpectralClass.BlackHole => (new Color(0.06f, 0.03f, 0.10f), 0.4f),
        _ => (new Color(1.0f, 1.0f, 1.0f), 2.0f),
    };
}
