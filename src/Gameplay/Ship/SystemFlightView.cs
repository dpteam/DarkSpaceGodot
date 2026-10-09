using System;
using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Network;
using DarkSpace.Core.Simulation;
using DarkSpace.Core.Systems.Orbital;
using DarkSpace.Core.Systems.Physics;
using DarkSpace.Core.Utils.Logging;
using DarkSpace.Gameplay;
using DarkSpace.UI.Widgets;
using Godot;

namespace DarkSpace.Gameplay.Ship;

/// <summary>
/// Полёт по звёздной системе в духе Elite: <b>вид от первого лица</b> из кабины, звёздная пыль,
/// летящая мимо (ощущение скорости), прицел по центру и приборная панель со сканером.
/// </summary>
/// <remarks>
/// <para>
/// Модель полёта — элитовская (см. <see cref="FlightMode.EliteAligned"/>): скорость направлена вдоль
/// носа, тангаж/крен для разворота. Камера стоит в носу корабля и смотрит вперёд, поэтому при
/// манёвре вращается всё звёздное окружение.
/// </para>
/// <para>
/// Звёздная пыль — облако частиц вокруг корабля с заворачиванием (wrap) по границам бокса: корабль
/// «пролетает» сквозь бесконечное поле пыли, что и даёт ощущение скорости.
/// </para>
/// <para>
/// Управление (как в Elite): W/S — тангаж, A/D — крен, Q/E — рыскание, Shift — разгон, Ctrl — торможение,
/// T — цель, F — стыковка, Esc — меню.
/// </para>
/// </remarks>
public partial class SystemFlightView : Node3D
{
    private const double FixedDelta = 1.0 / 60.0;
    private const int DustCount = 420;
    private const float DustBox = 90.0f;
    private const float CameraFar = 40000.0f;

    private GameServer _server = null!;
    private GameClient _client = null!;
    private StarSystemModel _model = null!;

    private Camera3D _camera = null!;
    private MeshInstance3D _starNode = null!;
    private MeshInstance3D _stationNode = null!;
    private ShipHud _hud = null!;
    private MarketPanel _marketPanel = null!;
    private bool _marketOpen;
    private bool _prevDockedStation;
    private Label _status = null!;
    private Label _targetInfo = null!;
    private Label _help = null!;

    private readonly List<MeshInstance3D> _planetNodes = new();

    private MultiMesh _dust = null!;
    private Vector3[] _dustPositions = null!;

    private double _accumulator;
    private long _commandSequence;
    private Vector3D _lastThrust = new(123.0, 123.0, 123.0);
    private Vector3D _lastRotation = new(123.0, 123.0, 123.0);
    private readonly HashSet<Key> _pressedLastFrame = new();

    /// <inheritdoc />
    public override void _Ready()
    {
        Log.AddSink(new GodotLogSink());
        GameContext.EnsureSaveDirectory();

        _hud = GetNode<ShipHud>("Hud/Scanner");
        _marketPanel = GetNode<MarketPanel>("Hud/Market");
        _status = GetNode<Label>("Hud/Status");
        _targetInfo = GetNode<Label>("Hud/TargetInfo");
        _help = GetNode<Label>("Hud/Help");
        _help.Text = "W/S — тангаж · A/D — крен · Q/E — рыскание · Shift — разгон · Ctrl — торможение · T — цель · F — планета · G — станция · M — рынок · Esc — меню";
        _marketPanel.Visible = false;

        StartLocalHost();
        BuildScene();
        BuildStardust();

        if (_model.BodyCount > 0)
        {
            _client.Send(new SetTargetCommand(1, _commandSequence++, 0));
        }

        Log.Info("SystemFlightView", "Полёт (вид от первого лица) запущен.");
    }

    private void StartLocalHost()
    {
        ulong worldSeed = GameContext.GetOrCreate().WorldSeed;
        _server = new GameServer(worldSeed);

        LoopbackTransport.CreatePair(out LoopbackTransport clientEnd, out LoopbackTransport serverEnd);
        _client = new GameClient(clientEnd);
        _server.AddConnection(1, serverEnd);

        _commandSequence = 0;
        _accumulator = 0.0;
        _model = StarSystemModel.Create(GameContext.GetOrCreate().GetGalaxy(0), 0);
    }

    private void BuildScene()
    {
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.004f, 0.005f, 0.014f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.18f, 0.20f, 0.28f),
            AmbientLightEnergy = 0.45f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
        };
        AddChild(new WorldEnvironment { Name = "Environment", Environment = environment });

        (Color starColor, float energy) = PlanetVisuals.Star(_model.System.Star);
        var starMaterial = new StandardMaterial3D
        {
            AlbedoColor = starColor,
            EmissionEnabled = true,
            Emission = starColor,
            EmissionEnergyMultiplier = energy,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        float starRadius = (float)_model.StarRadius;
        _starNode = new MeshInstance3D
        {
            Name = "Star",
            Mesh = new SphereMesh { Radius = starRadius, Height = starRadius * 2.0f, RadialSegments = 48, Rings = 24 },
            MaterialOverride = starMaterial,
        };
        AddChild(_starNode);

        if (_model.System.Star != SpectralClass.BlackHole)
        {
            AddChild(new OmniLight3D
            {
                Name = "StarLight",
                LightColor = starColor,
                LightEnergy = energy,
                OmniRange = 20000.0f,
                OmniAttenuation = 0.4f,
            });
        }

        for (int i = 0; i < _model.BodyCount; i++)
        {
            PlanetVisuals.Palette palette = PlanetVisuals.For(_model.Body(i).Planet.Type);
            float radius = (float)_model.Body(i).Radius;

            var material = new StandardMaterial3D { AlbedoColor = palette.Land };
            if (palette.Emissive > 0.01f)
            {
                material.EmissionEnabled = true;
                material.Emission = palette.EmissiveColor;
                material.EmissionEnergyMultiplier = palette.Emissive * 1.5f;
            }

            var mesh = new MeshInstance3D
            {
                Name = $"Planet_{i}",
                Mesh = new SphereMesh { Radius = radius, Height = radius * 2.0f, RadialSegments = 32, Rings = 16 },
                MaterialOverride = material,
            };
            AddChild(mesh);
            _planetNodes.Add(mesh);
        }

        BuildStation();
        _camera = new Camera3D { Name = "Camera", Current = true, Fov = 78.0f, Near = 0.1f, Far = CameraFar };
        AddChild(_camera);
    }

    private void BuildStation()
    {
        if (!_model.HasStation)
        {
            _stationNode = new MeshInstance3D { Visible = false };
            AddChild(_stationNode);
            return;
        }

        float r = (float)SystemScale.StationRadius;
        var material = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.6f, 0.65f, 0.7f),
            Metallic = 0.6f,
            Roughness = 0.4f,
            EmissionEnabled = true,
            Emission = new Color(0.1f, 0.3f, 0.4f),
            EmissionEnergyMultiplier = 0.5f,
        };

        // Корпус станции: куб с «ушами» (упрощённый Кориолис).
        _stationNode = new MeshInstance3D
        {
            Name = "Station",
            Mesh = new BoxMesh { Size = new Vector3(r, r, r) },
            MaterialOverride = material,
        };
        _stationNode.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(r * 0.4f, r * 1.8f, r * 0.4f) },
            MaterialOverride = material,
        });
        AddChild(_stationNode);
    }

    private void BuildStardust()
    {
        var quad = new QuadMesh { Size = new Vector2(0.5f, 0.5f) };
        quad.Material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            AlbedoColor = new Color(0.9f, 0.95f, 1.0f),
            DisableReceiveShadows = true,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
        };

        _dust = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            Mesh = quad,
            InstanceCount = DustCount,
        };

        AddChild(new MultiMeshInstance3D { Name = "Stardust", Multimesh = _dust });

        var rng = new RandomNumberGenerator();
        rng.Randomize();
        _dustPositions = new Vector3[DustCount];
        // Floating origin: корабль в начале координат, поэтому пыль строится вокруг нуля.
        for (int i = 0; i < DustCount; i++)
        {
            _dustPositions[i] = RandomInBox(rng, DustBox);
            _dust.SetInstanceTransform(i, new Transform3D(Basis.Identity, _dustPositions[i]));
        }
    }

    private static Vector3 RandomInBox(RandomNumberGenerator rng, float half) => new(
        rng.RandfRange(-half, half),
        rng.RandfRange(-half, half),
        rng.RandfRange(-half, half));

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        // Esc закрывает рынок, если он открыт; иначе — выход в меню.
        if (Input.IsKeyPressed(Key.Escape) && !_marketOpen)
        {
            GetTree().ChangeSceneToFile("res://src/UI/Menus/MainMenu.tscn");
            return;
        }

        HandleCommands();

        _accumulator += delta;
        while (_accumulator >= FixedDelta)
        {
            _server.Step(FixedDelta);
            _accumulator -= FixedDelta;
        }

        SimulationSnapshot? s = _client.Latest;
        if (s is null)
        {
            return;
        }

        UpdatePlanets(s);
        UpdateCamera(s);
        UpdateStardust(s, delta);
        UpdateHud(s);
    }

    private void HandleCommands()
    {
        // Экран рынка: управление только выбором товара и торговлей.
        if (_marketOpen)
        {
            if (PressedOnce(Key.M) || PressedOnce(Key.Escape))
            {
                _marketOpen = false;
            }
            else if (PressedOnce(Key.Up) || PressedOnce(Key.W))
            {
                _marketPanel.Selected = System.Math.Max(0, _marketPanel.Selected - 1);
            }
            else if (PressedOnce(Key.Down) || PressedOnce(Key.S))
            {
                int count = _client.Latest?.Market.Count ?? 0;
                _marketPanel.Selected = System.Math.Min(count - 1, _marketPanel.Selected + 1);
            }
            else if (PressedOnce(Key.B))
            {
                _client.Send(new BuyCommodityCommand(1, _commandSequence++, _marketPanel.Selected, 1));
            }
            else if (PressedOnce(Key.N))
            {
                _client.Send(new SellCommodityCommand(1, _commandSequence++, _marketPanel.Selected, 1));
            }

            return;
        }

        if (PressedOnce(Key.T))
        {
            CycleTarget();
        }

        if (PressedOnce(Key.F))
        {
            _client.Send(new DockCommand(1, _commandSequence++));
        }

        if (PressedOnce(Key.G))
        {
            _client.Send(new DockStationCommand(1, _commandSequence++));
        }

        if (PressedOnce(Key.M) && (_client.Latest?.DockedStation ?? false))
        {
            _marketOpen = true;
            _marketPanel.Selected = 0;
        }

        Vector3D rotation = ReadRotation();
        if (rotation.X != _lastRotation.X || rotation.Y != _lastRotation.Y || rotation.Z != _lastRotation.Z)
        {
            _client.Send(new RotateCommand(1, _commandSequence++, rotation.X, rotation.Y, rotation.Z));
            _lastRotation = rotation;
        }

        Vector3D thrust = ReadThrust();
        if (thrust.X != _lastThrust.X || thrust.Y != _lastThrust.Y || thrust.Z != _lastThrust.Z)
        {
            _client.Send(new MoveCommand(1, _commandSequence++, thrust.X, thrust.Y, thrust.Z));
            _lastThrust = thrust;
        }
    }

    private static Vector3D ReadRotation()
    {
        double pitch = 0.0;
        double yaw = 0.0;
        double roll = 0.0;

        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
        {
            pitch += 1.0;
        }

        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
        {
            pitch -= 1.0;
        }

        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
        {
            roll -= 1.0;
        }

        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
        {
            roll += 1.0;
        }

        if (Input.IsKeyPressed(Key.Q))
        {
            yaw -= 1.0;
        }

        if (Input.IsKeyPressed(Key.E))
        {
            yaw += 1.0;
        }

        return new Vector3D(pitch, yaw, roll);
    }

    private static Vector3D ReadThrust()
    {
        double forward = 0.0;
        if (Input.IsKeyPressed(Key.Shift) || Input.IsKeyPressed(Key.Space))
        {
            forward += 1.0;
        }

        if (Input.IsKeyPressed(Key.Ctrl))
        {
            forward -= 1.0;
        }

        return new Vector3D(forward, 0.0, 0.0);
    }

    private void CycleTarget()
    {
        int count = _model.BodyCount;
        if (count == 0)
        {
            return;
        }

        int current = _client.Latest?.TargetBodyIndex ?? -1;
        int next = current < 0 ? 0 : (current + 1) % count;
        _client.Send(new SetTargetCommand(1, _commandSequence++, next));
    }

    private bool PressedOnce(Key key)
    {
        bool now = Input.IsKeyPressed(key);
        bool was = _pressedLastFrame.Contains(key);
        if (now && !was)
        {
            _pressedLastFrame.Add(key);
            return true;
        }

        if (!now && was)
        {
            _pressedLastFrame.Remove(key);
        }

        return false;
    }

    private void UpdatePlanets(SimulationSnapshot s)
    {
        // Floating origin: корабль в начале координат, тела рисуются относительно него.
        Vector3 ship = ToGodot(s.Position);
        Vector3 starRelative = -ship;
        _starNode.Position = starRelative;

        for (int i = 0; i < _planetNodes.Count; i++)
        {
            _planetNodes[i].Position = ToGodot(_model.BodyPosition(i, s.SimTime)) - ship;
        }

        if (_model.HasStation)
        {
            _stationNode.Position = ToGodot(_model.StationPosition(s.SimTime)) - ship;
        }
    }

    private void UpdateCamera(SimulationSnapshot s)
    {
        Vector3 forward = ToGodot(s.Orientation.Rotate(ShipAxes.Forward));
        Vector3 up = ToGodot(s.Orientation.Rotate(ShipAxes.Up));

        // Камера в носу корабля (корабль в начале координат), смотрит вперёд — вид от первого лица.
        Vector3 eye = up * 0.35f;
        _camera.GlobalTransform = new Transform3D(CameraBasis(forward, up), eye);
    }

    /// <summary>
    /// Строит корректный (не зеркальный) базис камеры: x — вправо, y — вверх, z — назад (−z смотрит вперёд).
    /// </summary>
    private static Basis CameraBasis(Vector3 forward, Vector3 up)
    {
        Vector3 f = forward.Normalized();
        Vector3 u = up - (f * up.Dot(f)); // ортогонализация
        u = u.LengthSquared() > 1e-8f ? u.Normalized() : Vector3.Up;
        Vector3 r = f.Cross(u);
        r = r.LengthSquared() > 1e-8f ? r.Normalized() : Vector3.Right;
        return new Basis(r, u, -f);
    }

    private void UpdateStardust(SimulationSnapshot s, double delta)
    {
        // Floating origin: корабль стоит в центре, поэтому пыль летит навстречу (минус скорость)
        // и заворачивается по границам бокса — бесконечное поле пыли, дающее ощущение скорости.
        Vector3 v = ToGodot(s.Velocity);
        Vector3 step = v * (float)delta;
        for (int i = 0; i < _dustPositions.Length; i++)
        {
            Vector3 p = _dustPositions[i] - step;
            p.X = Wrap(p.X, 0f);
            p.Y = Wrap(p.Y, 0f);
            p.Z = Wrap(p.Z, 0f);
            _dustPositions[i] = p;
            _dust.SetInstanceTransform(i, new Transform3D(Basis.Identity, p));
        }
    }

    private static float Wrap(float value, float center)
    {
        float span = DustBox * 2f;
        float rel = value - center;
        rel -= Mathf.Floor((rel + DustBox) / span) * span;
        return center + rel;
    }

    private void UpdateHud(SimulationSnapshot s)
    {
        _hud.MaxSpeed = ShipPhysicsProfile.Default.MaxSpeed;
        _hud.ScannerRange = 300f;
        _hud.Snapshot = s;

        _hud.Blips.Clear();
        for (int i = 0; i < _model.BodyCount; i++)
        {
            Vector3D rel = _model.BodyPosition(i, s.SimTime) - s.Position;
            double localRight = Vector3D.Dot(rel, s.Orientation.Rotate(ShipAxes.Right));
            double localForward = Vector3D.Dot(rel, s.Orientation.Rotate(ShipAxes.Forward));
            _hud.Blips.Add(new ShipHud.ScannerBlip((float)localRight, (float)localForward, localForward > 0, i == s.TargetBodyIndex));
        }

        if (_model.HasStation)
        {
            Vector3D rel = _model.StationPosition(s.SimTime) - s.Position;
            double localRight = Vector3D.Dot(rel, s.Orientation.Rotate(ShipAxes.Right));
            double localForward = Vector3D.Dot(rel, s.Orientation.Rotate(ShipAxes.Forward));
            _hud.Blips.Add(new ShipHud.ScannerBlip((float)localRight, (float)localForward, localForward > 0, false));
        }

        _status.Text =
            $"Скорость {s.Speed:0.0}\n" +
            $"Δv {s.RemainingDeltaV:0}\n" +
            $"Топливо {s.Fuel:0.0}";

        // Рынок: если состыкованы — панель доступна, иначе закрывается.
        if (!s.DockedStation)
        {
            _marketOpen = false;
        }

        _marketPanel.Visible = _marketOpen;
        _marketPanel.Snapshot = s;

        if (s.DockedStation)
        {
            _targetInfo.Text = $"СТЫКОВКА: «{s.StationName}»   → M: рынок   ·   тангаж для отхода";
            return;
        }

        if (s.DockedBodyIndex >= 0)
        {
            _targetInfo.Text = $"ПОСАДКА: {_model.Body(s.DockedBodyIndex).Planet.Name}";
            return;
        }

        // Подсказка о стыковке со станцией, если близко.
        if (_model.HasStation)
        {
            double stationDistance = _model.DistanceToStation(s.Position, s.SimTime);
            if (stationDistance <= _model.StationDockRadius * 3.0)
            {
                _targetInfo.Text = $"Станция «{s.StationName}» · {stationDistance:0.0} ед.   → G: стыковка";
                return;
            }
        }

        if (s.TargetBodyIndex >= 0)
        {
            SystemBody body = _model.Body(s.TargetBodyIndex);
            double distance = _model.DistanceToBody(s.TargetBodyIndex, s.Position, s.SimTime);
            string prompt = distance <= _model.DockRadius(s.TargetBodyIndex) ? "   → F: посадка" : string.Empty;
            _targetInfo.Text = $"Цель: {body.Planet.Name}  ·  {distance:0.0} ед.{prompt}";
        }
        else
        {
            _targetInfo.Text = "Цель не выбрана (T)";
        }
    }

    private static Vector3 ToGodot(Vector3D v) => new((float)v.X, (float)v.Z, (float)v.Y);
}
