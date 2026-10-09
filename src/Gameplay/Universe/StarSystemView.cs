using System;
using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Orbital;
using DarkSpace.Core.Utils.Logging;
using DarkSpace.Gameplay;
using DarkSpace.UI.Widgets;
using Godot;

namespace DarkSpace.Gameplay.Universe;

/// <summary>
/// 3D-карта звёздной системы в духе SpaceEngine: звезда в центре, планеты обращаются
/// по реальным кеплеровым орбитам, камеру можно вращать и приближать.
/// </summary>
/// <remarks>
/// <para>
/// Вся геометрия строится процедурно в рантайме: орбиты — линии по <see cref="OrbitalMechanics"/>,
/// планеты — сферы с цветом из <see cref="PlanetVisuals"/>. Никаких внешних 3D-ассетов.
/// </para>
/// <para>Управление: ЛКМ-перетаскивание — вращение камеры, колесо — зум, клик по планете — выбор,
/// <c>[</c>/<c>]</c> — перебор планет, <c>+</c>/<c>-</c>/<c>0</c> — скорость/пауза времени, Esc — меню.</para>
/// </remarks>
public partial class StarSystemView : Node3D
{
    private const float MinDistance = 40.0f;
    private const float MaxDistance = 24000.0f;

    private sealed class PlanetNode
    {
        public required OrbitData Orbit { get; init; }
        public required PlanetData Data { get; init; }
        public required MeshInstance3D Mesh { get; init; }
        public required StandardMaterial3D Material { get; init; }
        public required float VisualRadius { get; init; }
        public Vector3 CurrentPosition { get; set; }
    }

    private readonly List<PlanetNode> _planets = new();

    private Camera3D _camera = null!;
    private RichTextLabel _info = null!;
    private Label _help = null!;
    private Label _timeLabel = null!;
    private Label _title = null!;
    private MeshInstance3D _selectionRing = null!;

    private StarSystemData _system = null!;
    private StarSystemModel _model = null!;
    private double _simTime;
    private double _timeScale = 1.0;
    private int _selected = -1;

    private float _yaw = 0.6f;
    private float _pitch = 0.9f;
    private float _distance = 700.0f;
    private bool _dragging;
    private Vector2 _pressPosition;
    private bool _pressMoved;
    private int _hovered = -1;

    /// <summary>Радиус (в пикселях), в пределах которого клик по экрану выбирает планету.</summary>
    private const float PickRadiusPixels = 56.0f;

    /// <summary>Смещение мыши (в пикселях), после которого жест считается вращением камеры, а не кликом.</summary>
    private const float DragThresholdPixels = 6.0f;

    /// <inheritdoc />
    public override void _Ready()
    {
        Log.AddSink(new GodotLogSink());
        GameContext.EnsureSaveDirectory();

        _info = GetNode<RichTextLabel>("UI/Panel/Info");
        _help = GetNode<Label>("UI/Help");
        _timeLabel = GetNode<Label>("UI/Time");
        _title = GetNode<Label>("UI/Title");

        _help.Text = "ЛКМ — вращение · колесо — зум · клик по планете — выбор · [ ] — перебор · +/− — время · 0 — пауза · Esc — меню";

        BuildSystem();
        Log.Info("StarSystemView", $"3D-карта системы «{_system.Name}» построена ({_planets.Count} планет).");
    }

    private void BuildSystem()
    {
        var session = GameContext.GetOrCreate();
        GalaxyData galaxy = session.GetGalaxy(0);
        _system = StarSystemGenerator.GenerateSystem(galaxy, 0);
        _model = new StarSystemModel(_system);

        _title.Text = $"Система «{_system.Name}»   ·   звезда класса {_system.Star}   ·   фракция: {_system.ControllingFaction}";

        BuildEnvironment();
        BuildStar();
        BuildPlanets();
        BuildSelectionRing();
        BuildCamera();

        if (_planets.Count > 0)
        {
            Select(0);
        }
    }

    private void BuildEnvironment()
    {
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.008f, 0.010f, 0.024f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.18f, 0.20f, 0.28f),
            AmbientLightEnergy = 0.35f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
        };

        AddChild(new WorldEnvironment { Name = "Environment", Environment = environment });
    }

    private void BuildStar()
    {
        (Color starColor, float energy) = PlanetVisuals.Star(_system.Star);

        float starRadius = (float)_model.StarRadius;
        var starMesh = new SphereMesh { Radius = starRadius, Height = starRadius * 2.0f, RadialSegments = 48, Rings = 24 };
        var starMaterial = new StandardMaterial3D
        {
            AlbedoColor = starColor,
            EmissionEnabled = true,
            Emission = starColor,
            EmissionEnergyMultiplier = energy,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };

        AddChild(new MeshInstance3D { Name = "Star", Mesh = starMesh, MaterialOverride = starMaterial });

        if (_system.Star != SpectralClass.BlackHole)
        {
            AddChild(new OmniLight3D
            {
                Name = "StarLight",
                LightColor = starColor,
                LightEnergy = energy,
                OmniRange = 20000.0f,
                Position = Vector3.Zero,
            });
        }
    }

    private void BuildPlanets()
    {
        for (int i = 0; i < _model.BodyCount; i++)
        {
            SystemBody body = _model.Body(i);
            OrbitData orbit = body.Orbit;
            PlanetData planet = body.Planet;

            Color baseColor = PlanetVisuals.BaseColor(planet.Type);
            PlanetVisuals.Palette palette = PlanetVisuals.For(planet.Type);

            // На карте планеты — «звёздный график»: слегка увеличены для читаемости, но форма сферы реальная.
            float radius = Mathf.Max((float)body.Radius * 2.5f, 6.0f);

            var mesh = new SphereMesh { Radius = radius, Height = radius * 2.0f, RadialSegments = 32, Rings = 16 };
            var material = new StandardMaterial3D { AlbedoColor = baseColor };
            if (palette.Emissive > 0.01f)
            {
                material.EmissionEnabled = true;
                material.Emission = palette.EmissiveColor;
                material.EmissionEnergyMultiplier = palette.Emissive * 1.5f;
            }

            var meshInstance = new MeshInstance3D { Name = $"Planet_{i}", Mesh = mesh, MaterialOverride = material };
            AddChild(meshInstance);

            AddChild(BuildOrbitRing(orbit));

            _planets.Add(new PlanetNode
            {
                Orbit = orbit,
                Data = planet,
                Mesh = meshInstance,
                Material = material,
                VisualRadius = radius,
            });
        }
    }

    private static MeshInstance3D BuildOrbitRing(OrbitData orbit)
    {
        var immediate = new ImmediateMesh();
        immediate.SurfaceBegin(Mesh.PrimitiveType.LineStrip);

        const int samples = 128;
        for (int s = 0; s <= samples; s++)
        {
            double t = orbit.PeriodSeconds * s / samples;
            immediate.SurfaceAddVertex(ToGodot(OrbitalMechanics.PositionAt(orbit, t)));
        }

        immediate.SurfaceEnd();

        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.25f, 0.35f, 0.55f, 0.7f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };

        return new MeshInstance3D { Name = $"Orbit_{orbit.BodyIndex}", Mesh = immediate, MaterialOverride = material };
    }

    private void BuildSelectionRing()
    {
        var immediate = new ImmediateMesh();
        immediate.SurfaceBegin(Mesh.PrimitiveType.LineStrip);
        const int samples = 48;
        for (int s = 0; s <= samples; s++)
        {
            float a = Mathf.Tau * s / samples;
            immediate.SurfaceAddVertex(new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a)));
        }

        immediate.SurfaceEnd();

        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.49f, 0.99f, 0.0f),
        };

        _selectionRing = new MeshInstance3D { Name = "Selection", Mesh = immediate, MaterialOverride = material, Visible = false };
        AddChild(_selectionRing);
    }

    private void BuildCamera()
    {
        _camera = new Camera3D { Name = "Camera", Current = true, Fov = 60.0f, Near = 1.0f, Far = 60000.0f };
        AddChild(_camera);
        UpdateCamera();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        _simTime += delta * _timeScale;

        UpdateHover();

        for (int i = 0; i < _planets.Count; i++)
        {
            PlanetNode planet = _planets[i];
            Vector3 position = ToGodot(OrbitalMechanics.PositionAt(planet.Orbit, _simTime));
            planet.Mesh.Position = position;
            planet.Mesh.RotateY((float)delta * 0.6f);
            planet.CurrentPosition = position;

            // Подсветка объекта под курсором (легче целиться).
            float scale = i == _hovered && i != _selected ? 1.3f : 1.0f;
            planet.Mesh.Scale = new Vector3(scale, scale, scale);
        }

        if (_selected >= 0 && _selected < _planets.Count)
        {
            Vector3 p = _planets[_selected].CurrentPosition;
            _selectionRing.Position = p;
            _selectionRing.RotateY((float)delta * 1.5f);
            float ringScale = _planets[_selected].VisualRadius * 1.6f;
            _selectionRing.Scale = new Vector3(ringScale, ringScale, ringScale);
            _selectionRing.Visible = true;
        }

        UpdateTimeLabel();
    }

    /// <inheritdoc />
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true } key)
        {
            HandleKey(key.Keycode);
        }
        else if (@event is InputEventMouseButton button)
        {
            HandleMouseButton(button);
        }
        else if (@event is InputEventMouseMotion motion)
        {
            if (_dragging)
            {
                _yaw -= motion.Relative.X * 0.005f;
                _pitch = Mathf.Clamp(_pitch + (motion.Relative.Y * 0.005f), -1.4f, 1.4f);
                UpdateCamera();
            }
            else if (motion.Position.DistanceTo(_pressPosition) > DragThresholdPixels)
            {
                // Начали тянуть — это вращение камеры, а не выбор объекта.
                _pressMoved = true;
            }
        }
    }

    private void HandleKey(Key keycode)
    {
        switch (keycode)
        {
            case Key.Escape:
                GetTree().ChangeSceneToFile("res://src/UI/Menus/MainMenu.tscn");
                break;
            case Key.Bracketleft:
                SelectRelative(-1);
                break;
            case Key.Bracketright:
                SelectRelative(1);
                break;
            case Key.Equal or Key.Plus:
                _timeScale = Math.Min(8.0, _timeScale * 2.0);
                break;
            case Key.Minus:
                _timeScale = Math.Max(0.125, _timeScale / 2.0);
                break;
            case Key.Key0:
            case Key.Space:
                _timeScale = 0.0;
                break;
        }
    }

    private void HandleMouseButton(InputEventMouseButton button)
    {
        switch (button.ButtonIndex)
        {
            case MouseButton.Left when button.Pressed:
                _dragging = true;
                _pressMoved = false;
                _pressPosition = button.Position;
                break;
            case MouseButton.Left:
                _dragging = false;
                if (!_pressMoved)
                {
                    // Это был клик (а не перетаскивание) — выбираем ближайший объект.
                    TryPickPlanetAt(button.Position);
                }

                break;
            case MouseButton.WheelUp when button.Pressed:
                _distance = Math.Max(MinDistance, _distance * 0.88f);
                UpdateCamera();
                break;
            case MouseButton.WheelDown when button.Pressed:
                _distance = Math.Min(MaxDistance, _distance * 1.14f);
                UpdateCamera();
                break;
        }
    }

    /// <summary>
    /// Обновляет индекс планеты под курсором (для подсветки). Использует ту же экранную проекцию,
    /// что и выбор, поэтому подсветка и клик совпадают.
    /// </summary>
    private void UpdateHover()
    {
        _hovered = -1;
        if (_dragging)
        {
            return;
        }

        Vector2 mouse = GetViewport().GetMousePosition();
        float bestScreen = PickRadiusPixels;
        for (int i = 0; i < _planets.Count; i++)
        {
            if (_camera.IsPositionBehind(_planets[i].CurrentPosition))
            {
                continue;
            }

            float screenDistance = _camera.UnprojectPosition(_planets[i].CurrentPosition).DistanceTo(mouse);
            if (screenDistance < bestScreen)
            {
                bestScreen = screenDistance;
                _hovered = i;
            }
        }
    }

    /// <summary>
    /// Выбирает планету по позиции на экране. Проецируем каждый объект на экран и берём ближайший
    /// в пределах <see cref="PickRadiusPixels"/> пикселей — так по движущейся планете куда проще попасть.
    /// </summary>
    private void TryPickPlanetAt(Vector2 screenPosition)
    {
        int hit = -1;
        float bestScreen = float.MaxValue;
        float bestDepth = float.MaxValue;

        for (int i = 0; i < _planets.Count; i++)
        {
            if (_camera.IsPositionBehind(_planets[i].CurrentPosition))
            {
                continue;
            }

            Vector2 sp = _camera.UnprojectPosition(_planets[i].CurrentPosition);
            float screenDistance = sp.DistanceTo(screenPosition);
            if (screenDistance > PickRadiusPixels)
            {
                continue;
            }

            // Если несколько объектов рядом — берём тот, что ближе к камере.
            float depth = _camera.GlobalPosition.DistanceTo(_planets[i].CurrentPosition);
            if (screenDistance < bestScreen - 1.0f || (Mathf.Abs(screenDistance - bestScreen) <= 1.0f && depth < bestDepth))
            {
                bestScreen = screenDistance;
                bestDepth = depth;
                hit = i;
            }
        }

        Select(hit);
    }

    private void SelectRelative(int delta)
    {
        if (_planets.Count == 0)
        {
            return;
        }

        int next = _selected < 0 ? 0 : ((_selected + delta) % _planets.Count + _planets.Count) % _planets.Count;
        Select(next);
    }

    private void Select(int index)
    {
        _selected = index;

        for (int i = 0; i < _planets.Count; i++)
        {
            StandardMaterial3D m = _planets[i].Material;
            if (i == index)
            {
                m.EmissionEnabled = true;
                m.Emission = new Color(0.49f, 0.99f, 0.0f);
                m.EmissionEnergyMultiplier = 0.6f;
            }
            else
            {
                PlanetVisuals.Palette palette = PlanetVisuals.For(_planets[i].Data.Type);
                if (palette.Emissive > 0.01f)
                {
                    m.EmissionEnabled = true;
                    m.Emission = palette.EmissiveColor;
                    m.EmissionEnergyMultiplier = palette.Emissive * 1.5f;
                }
                else
                {
                    m.EmissionEnabled = false;
                }
            }
        }

        _selectionRing.Visible = index >= 0;
        UpdateInfo();
    }

    private void UpdateInfo()
    {
        if (_selected < 0 || _selected >= _planets.Count)
        {
            _info.Text = "Выберите планету (клик или [ ]), чтобы увидеть подробности.";
            return;
        }

        PlanetNode node = _planets[_selected];
        PlanetData planet = node.Data;
        OrbitData orbit = node.Orbit;

        var lines = new List<string>
        {
            $"[b]{planet.Name}[/b]  ({planet.Type})",
            $"Гравитация {planet.Gravity:0.0}g · температура {planet.TemperatureCelsius:0.0}°C",
            $"Атмосфера: {planet.Atmosphere}",
            $"Орбита: a = {orbit.SemiMajorAxis:0.0} · e = {orbit.Eccentricity:0.00} · период {orbit.PeriodSeconds:0.0} с",
            string.Empty,
            planet.Description,
        };

        var cities = CityGenerator.GenerateCities(_system, planet);
        if (cities.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add($"Поселения: {cities.Count}");
            lines.Add("• " + cities[0].Description);
        }

        _info.Text = string.Join('\n', lines);
    }

    private void UpdateCamera()
    {
        var center = Vector3.Zero;
        float cosPitch = Mathf.Cos(_pitch);
        var offset = new Vector3(
            Mathf.Sin(_yaw) * cosPitch,
            Mathf.Sin(_pitch),
            Mathf.Cos(_yaw) * cosPitch) * _distance;

        _camera.Position = center + offset;
        _camera.LookAt(center, Vector3.Up);
    }

    private void UpdateTimeLabel()
    {
        string state = _timeScale <= 0.0 ? "пауза" : $"×{_timeScale:0.##}";
        _timeLabel.Text = $"t = {_simTime:0.0} с    ({state})";
    }

    private static Vector3 ToGodot(Vector3D v) => new((float)v.X, (float)v.Z, (float)v.Y);
}
