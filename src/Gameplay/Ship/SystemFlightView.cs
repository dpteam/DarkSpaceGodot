using System;
using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Network;
using DarkSpace.Core.Simulation;
using DarkSpace.Core.Systems.Orbital;
using DarkSpace.Core.Utils.Logging;
using DarkSpace.Gameplay;
using DarkSpace.UI.Widgets;
using Godot;

namespace DarkSpace.Gameplay.Ship;

/// <summary>
/// 3D-полёт по звёздной системе: корабль летает между планетами, обращающимися по орбитам,
/// с наведением на цель и стыковкой. Как и тренажёр, работает через локальный хост
/// (<see cref="GameServer"/> + <see cref="GameClient"/> поверх loopback) — тот же путь команд,
/// что и в мультиплеере.
/// </summary>
/// <remarks>
/// Управление: W/S — вперёд/назад, A/D — влево/вправо, Space/Ctrl — вверх/вниз,
/// T — сменить цель, F — стыковка, колесо — дистанция камеры, Esc — меню.
/// </remarks>
public partial class SystemFlightView : Node3D
{
    private const double FixedDelta = 1.0 / 60.0;
    private const float CamMinDistance = 8.0f;
    private const float CamMaxDistance = 60.0f;

    private GameServer _server = null!;
    private GameClient _client = null!;
    private StarSystemModel _model = null!;

    private Node3D _shipNode = null!;
    private Camera3D _camera = null!;
    private Label _hud = null!;
    private Label _help = null!;
    private Label _targetInfo = null!;

    private readonly List<MeshInstance3D> _planetNodes = new();

    private double _accumulator;
    private long _commandSequence;
    private Vector3D _lastThrust = new(999.0, 999.0, 999.0);
    private float _camDistance = 22.0f;
    private readonly HashSet<Key> _pressedLastFrame = new();

    /// <inheritdoc />
    public override void _Ready()
    {
        Log.AddSink(new GodotLogSink());
        GameContext.EnsureSaveDirectory();

        _hud = GetNode<Label>("UI/Hud");
        _help = GetNode<Label>("UI/Help");
        _targetInfo = GetNode<Label>("UI/TargetInfo");
        _help.Text = "W/S — вперёд/назад · A/D — влево/вправо · Space/Ctrl — вверх/вниз · T — цель · F — стыковка · Esc — меню";

        StartLocalHost();
        BuildScene();

        // Автоматически выбираем первую планету целью для наглядности.
        if (_model.BodyCount > 0)
        {
            _client.Send(new SetTargetCommand(1, _commandSequence++, 0));
        }

        Log.Info("SystemFlightView", "3D-полёт по системе запущен (локальный хост).");
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

        // Та же детерминированная генерация, что и в симуляции, — для отрисовки тел.
        _model = StarSystemModel.Create(sessionGalaxy(), 0);
    }

    private GalaxyData sessionGalaxy()
    {
        // Текущая галактика сессии (по умолчанию индекс 0).
        return GameContext.GetOrCreate().GetGalaxy(0);
    }

    private void BuildScene()
    {
        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.008f, 0.010f, 0.024f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.16f, 0.18f, 0.26f),
            AmbientLightEnergy = 0.4f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic,
        };
        AddChild(new WorldEnvironment { Name = "Environment", Environment = environment });

        // Звезда.
        (Color starColor, float energy) = PlanetVisuals.Star(_model.System.Star);
        var starMaterial = new StandardMaterial3D
        {
            AlbedoColor = starColor,
            EmissionEnabled = true,
            Emission = starColor,
            EmissionEnergyMultiplier = energy,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        AddChild(new MeshInstance3D
        {
            Name = "Star",
            Mesh = new SphereMesh { Radius = 2.0f, Height = 4.0f, RadialSegments = 32, Rings = 16 },
            MaterialOverride = starMaterial,
        });

        if (_model.System.Star != SpectralClass.BlackHole)
        {
            AddChild(new OmniLight3D
            {
                Name = "StarLight",
                LightColor = starColor,
                LightEnergy = energy,
                OmniRange = 800.0f,
            });
        }

        // Планеты (позиции обновляются каждый кадр по орбитам).
        for (int i = 0; i < _model.BodyCount; i++)
        {
            PlanetVisuals.Palette palette = PlanetVisuals.For(_model.Body(i).Planet.Type);
            float radius = PlanetVisuals.Radius(_model.Body(i).Planet.Type) * 1.6f;

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
                Mesh = new SphereMesh { Radius = radius, Height = radius * 2.0f, RadialSegments = 24, Rings = 12 },
                MaterialOverride = material,
            };
            AddChild(mesh);
            _planetNodes.Add(mesh);
        }

        // Корабль.
        var shipMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.49f, 0.99f, 0.0f),
            EmissionEnabled = true,
            Emission = new Color(0.2f, 0.5f, 0.05f),
            EmissionEnergyMultiplier = 0.5f,
        };
        _shipNode = new Node3D { Name = "Ship" };
        _shipNode.AddChild(new MeshInstance3D
        {
            Mesh = new PrismMesh { Size = new Vector3(1.0f, 0.8f, 2.4f) },
            MaterialOverride = shipMaterial,
        });
        AddChild(_shipNode);

        _camera = new Camera3D { Name = "Camera", Current = true, Fov = 65.0f };
        AddChild(_camera);
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.Escape))
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

        UpdateVisuals((float)delta);
        UpdateHud();
    }

    private void HandleCommands()
    {
        if (PressedOnce(Key.T))
        {
            CycleTarget();
        }

        if (PressedOnce(Key.F))
        {
            _client.Send(new DockCommand(1, _commandSequence++));
        }

        Vector3D thrust = ReadThrust();
        if (thrust.X != _lastThrust.X || thrust.Y != _lastThrust.Y || thrust.Z != _lastThrust.Z)
        {
            _client.Send(new MoveCommand(1, _commandSequence++, thrust.X, thrust.Y, thrust.Z));
            _lastThrust = thrust;
        }
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

    private static Vector3D ReadThrust()
    {
        double x = 0.0;
        double y = 0.0; // орбитальная Y → «вперёд/назад»
        double z = 0.0; // орбитальная Z → «вверх/вниз»

        if (Input.IsKeyPressed(Key.A))
        {
            x -= 1.0;
        }

        if (Input.IsKeyPressed(Key.D))
        {
            x += 1.0;
        }

        if (Input.IsKeyPressed(Key.W))
        {
            y += 1.0;
        }

        if (Input.IsKeyPressed(Key.S))
        {
            y -= 1.0;
        }

        if (Input.IsKeyPressed(Key.Space))
        {
            z += 1.0;
        }

        if (Input.IsKeyPressed(Key.Ctrl) || Input.IsKeyPressed(Key.C))
        {
            z -= 1.0;
        }

        return new Vector3D(x, y, z).Length > 1.0 ? new Vector3D(x, y, z).Normalized() : new Vector3D(x, y, z);
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

    /// <inheritdoc />
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true } button)
        {
            if (button.ButtonIndex == MouseButton.WheelUp)
            {
                _camDistance = Mathf.Max(CamMinDistance, _camDistance * 0.9f);
            }
            else if (button.ButtonIndex == MouseButton.WheelDown)
            {
                _camDistance = Mathf.Min(CamMaxDistance, _camDistance * 1.1f);
            }
        }
    }

    private void UpdateVisuals(float delta)
    {
        SimulationSnapshot? s = _client.Latest;
        if (s is null)
        {
            return;
        }

        // Планеты — по орбитам на модельном времени.
        for (int i = 0; i < _planetNodes.Count; i++)
        {
            _planetNodes[i].Position = ToGodot(_model.BodyPosition(i, s.SimTime));
        }

        // Корабль.
        Vector3 shipPos = ToGodot(s.Position);
        _shipNode.Position = shipPos;

        Vector3 velocity = ToGodot(s.Velocity);
        if (velocity.LengthSquared() > 0.01f)
        {
            // Разворот носом по направлению движения.
            SafeLookAt(_shipNode, shipPos + velocity.Normalized());
        }

        // Камера — позади корабля.
        Vector3 backward = -_shipNode.Transform.Basis.Z.Normalized();
        Vector3 camPos = shipPos + (backward * _camDistance) + (Vector3.Up * 6.0f);
        _camera.GlobalPosition = camPos;
        SafeLookAt(_camera, shipPos);
    }

    /// <summary>
    /// Безопасный разворот узла на цель: подбирает вектор «вверх», не параллельный направлению,
    /// иначе <see cref="Node3D.LookAt(Vector3, Vector3?)"/> выбрасывает исключение.
    /// </summary>
    private static void SafeLookAt(Node3D node, Vector3 target)
    {
        Vector3 direction = target - node.GlobalPosition;
        if (direction.LengthSquared() < 1e-6f)
        {
            return;
        }

        Vector3 up = Mathf.Abs(direction.Normalized().Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;
        node.LookAt(target, up);
    }

    private void UpdateHud()
    {
        SimulationSnapshot? s = _client.Latest;
        if (s is null)
        {
            _hud.Text = "Ожидание состояния...";
            return;
        }

        double speed = s.Velocity.Length;
        _hud.Text =
            $"Тик {s.Tick}   ·   система «{s.SystemName}» (галактика #{s.GalaxyIndex})\n" +
            $"Скорость   {speed,8:0.0}\n" +
            $"Кредиты    {s.Credits,8:0}\n" +
            $"Топливо    {s.Fuel,8:0.0}" +
            (s.Destroyed ? "\n[color=red]КОРАБЛЬ УНИЧТОЖЕН[/color]" : string.Empty);

        if (s.DockedBodyIndex >= 0)
        {
            PlanetData planet = _model.Body(s.DockedBodyIndex).Planet;
            _targetInfo.Text = $"СОСТЫКОВАНО: {planet.Name}";
            return;
        }

        if (s.TargetBodyIndex >= 0)
        {
            SystemBody body = _model.Body(s.TargetBodyIndex);
            double distance = _model.DistanceToBody(s.TargetBodyIndex, s.Position, s.SimTime);
            string prompt = distance <= WorldSimulation.DockRange ? "   → F: стыковка" : string.Empty;
            _targetInfo.Text = $"Цель: {body.Planet.Name}  ·  {distance:0.0} ед.{prompt}";
        }
        else
        {
            _targetInfo.Text = "Цель не выбрана (T — выбрать ближайшую/следующую)";
        }
    }

    private static Vector3 ToGodot(Vector3D v) => new((float)v.X, (float)v.Z, (float)v.Y);
}
