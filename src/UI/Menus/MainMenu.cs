using System.IO;
using System.Threading.Tasks;
using DarkSpace.Core.Data;
using DarkSpace.Core.Data.Save;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Utils;
using DarkSpace.Core.Utils.Logging;
using DarkSpace.Gameplay;
using Godot;

namespace DarkSpace.UI.Menus;

/// <summary>
/// Главное меню игры с процедурным фоном в стиле SpaceEngine.
/// </summary>
/// <remarks>
/// Фон рисуется шейдером <c>assets/shaders/planet_background.gdshader</c>. Планета, система
/// и цветовая палитра выбираются процедурно: каждый запуск/обновление берёт случайную
/// звёздную систему из вселенной и генерирует планету, а её тип определяет вид фона.
/// Никаких растровых ассетов не используется — всё считает GPU.
/// </remarks>
public partial class MainMenu : Control
{
    private const float SpinSpeed = 0.05f;
    private const string UniverseOverviewScene = "res://src/UI/Menus/UniverseOverview.tscn";

    private ColorRect _background = null!;
    private ShaderMaterial _material = null!;
    private Label _title = null!;
    private Label _planetInfo = null!;
    private PanelContainer _settingsPanel = null!;
    private Button _continueButton = null!;
    private CheckButton _spinToggle = null!;
    private CheckButton _starsToggle = null!;

    /// <summary>Сид, из которого сгенерирован текущий фон меню.</summary>
    private ulong _backgroundSeed;

    private float _aspect = 1.6f;

    /// <inheritdoc />
    public override void _Ready()
    {
        Log.AddSink(new GodotLogSink());
        GameContext.SaveDirectory = ProjectSettings.GlobalizePath("user://saves");

        _background = GetNode<ColorRect>("Background");
        _material = (ShaderMaterial)_background.Material;
        _title = GetNode<Label>("Layout/Title");
        _planetInfo = GetNode<Label>("PlanetInfo");
        _settingsPanel = GetNode<PanelContainer>("SettingsPanel");
        _continueButton = GetNode<Button>("Layout/Menu/ContinueButton");
        _spinToggle = GetNode<CheckButton>("SettingsPanel/Content/SpinToggle");
        _starsToggle = GetNode<CheckButton>("SettingsPanel/Content/StarsToggle");

        WireButtons();

        _settingsPanel.Visible = false;
        UpdateContinueAvailability();

        _title.Text = "DARK SPACE";

        RefreshBackground();
        Log.Info("MainMenu", "Главное меню загружено.");
    }

    private void WireButtons()
    {
        GetNode<Button>("Layout/Menu/NewGameButton").Pressed += OnNewGame;
        _continueButton.Pressed += OnContinue;
        GetNode<Button>("Layout/Menu/SettingsButton").Pressed += () => _settingsPanel.Visible = !_settingsPanel.Visible;
        GetNode<Button>("Layout/Menu/QuitButton").Pressed += () => GetTree().Quit();

        GetNode<Button>("SettingsPanel/Content/RerollButton").Pressed += RefreshBackground;
        GetNode<Button>("SettingsPanel/Content/CloseButton").Pressed += () => _settingsPanel.Visible = false;

        _spinToggle.Toggled += on => _material.SetShaderParameter("spin_speed", on ? SpinSpeed : 0.0f);
        _starsToggle.Toggled += on => _material.SetShaderParameter("star_amount", on ? 1.0f : 0.0f);
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        Vector2 size = GetViewportRect().Size;
        float aspect = size.Y > 0 ? size.X / size.Y : 1.6f;
        if (Mathf.Abs(aspect - _aspect) > 0.001f)
        {
            _aspect = aspect;
            _material.SetShaderParameter("aspect", _aspect);
            _material.SetShaderParameter("planet_center", new Vector2(_aspect * 0.28f, 0.0f));
        }
    }

    private void OnNewGame()
    {
        ulong seed = ((ulong)GD.Randi() << 32) ^ GD.Randi();
        GameContext.StartNew(seed);
        GetTree().ChangeSceneToFile(UniverseOverviewScene);
    }

    private void OnContinue()
    {
        _ = ContinueAsync();
    }

    private async Task ContinueAsync()
    {
        var session = GameContext.GetOrCreate();
        SaveLoadResult result = await session.SaveManager.LoadAsync();

        if (result.Status is SaveLoadStatus.Loaded or SaveLoadStatus.Recovered && result.Save is { } save)
        {
            GameContext.StartNew(save.World.WorldSeed);
            Log.Info("MainMenu", $"Продолжение игры. Игрок «{save.Metadata.PlayerName}».");
            GetTree().ChangeSceneToFile(UniverseOverviewScene);
        }
        else
        {
            Log.Warning("MainMenu", $"Не удалось продолжить игру: {result.Status} / {result.Error}");
        }
    }

    private void UpdateContinueAvailability()
    {
        string savePath = Path.Combine(GameContext.SaveDirectory, SaveManager.SaveFileName);
        _continueButton.Disabled = !File.Exists(savePath);
    }

    /// <summary>
    /// Заново генерирует фон: выбирает случайную звёздную систему текущей вселенной,
    /// генерирует планету и задаёт параметры шейдера.
    /// </summary>
    public void RefreshBackground()
    {
        _backgroundSeed = ((ulong)GD.Randi() << 32) ^ GD.Randi();
        var rng = new DeterministicRandom(_backgroundSeed);

        ulong worldSeed = GameContext.GetOrCreate().WorldSeed;
        int galaxyIndex = rng.NextInt(0, GalaxyGenerator.MaxGalaxyCount);
        GalaxyData galaxy = GalaxyGenerator.GenerateGalaxy(worldSeed, galaxyIndex);
        int systemIndex = rng.NextInt(0, galaxy.StarCount);
        StarSystemData system = StarSystemGenerator.GenerateSystem(galaxy, systemIndex);
        int planetIndex = rng.NextInt(0, system.PlanetCount);
        PlanetData planet = PlanetGenerator.GeneratePlanet(system, planetIndex);

        ApplyPlanet(planet);
        _planetInfo.Text = $"Система «{system.Name}» · {planet.Name} · {planet.Type} · сигнал 0x{planet.Seed & 0xFFFFFF:X6}";
        Log.Info("MainMenu", $"Фон меню: {system.Name} / {planet.Name} ({planet.Type}).");
    }

    private void ApplyPlanet(PlanetData planet)
    {
        PlanetPalette palette = PaletteFor(planet.Type);

        _material.SetShaderParameter("aspect", _aspect);
        _material.SetShaderParameter("planet_center", new Vector2(_aspect * 0.28f, 0.0f));
        _material.SetShaderParameter("planet_radius", 0.33f);
        _material.SetShaderParameter("color_deep", ToVec3(palette.Deep));
        _material.SetShaderParameter("color_land", ToVec3(palette.Land));
        _material.SetShaderParameter("color_high", ToVec3(palette.High));
        _material.SetShaderParameter("color_atmosphere", ToVec3(palette.Atmosphere));
        _material.SetShaderParameter("seed", (planet.Seed % 1000000) / 1000000.0f);
        _material.SetShaderParameter("has_water", palette.HasWater ? 1.0f : 0.0f);
        _material.SetShaderParameter("gas_bands", palette.GasBands ? 1.0f : 0.0f);
        _material.SetShaderParameter("sea_level", palette.SeaLevel);
        _material.SetShaderParameter("cloud_amount", palette.Clouds);
        _material.SetShaderParameter("emissive", palette.Emissive);
        _material.SetShaderParameter("emissive_color", ToVec3(palette.EmissiveColor));
        _material.SetShaderParameter("spin_speed", _spinToggle.ButtonPressed ? SpinSpeed : 0.0f);
        _material.SetShaderParameter("star_amount", _starsToggle.ButtonPressed ? 1.0f : 0.0f);
    }

    private static Vector3 ToVec3(Color c) => new(c.R, c.G, c.B);

    /// <summary>Цветовая палитра планеты по её типу.</summary>
    private readonly record struct PlanetPalette(
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

    private static PlanetPalette PaletteFor(PlanetType type) => type switch
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
}
