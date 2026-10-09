using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Network;
using DarkSpace.Core.Simulation;
using DarkSpace.Core.Utils.Logging;
using DarkSpace.Gameplay;
using Godot;

namespace DarkSpace.Gameplay.Ship;

/// <summary>
/// Лётный тренажёр: демонстрация геймплейного цикла на базе авторитарной тик-симуляции.
/// </summary>
/// <remarks>
/// <para>
/// Здесь наглядно показан архитектурный принцип «одиночная игра = локальный хост»:
/// в одном процессе запущены <see cref="GameServer"/> и <see cref="GameClient"/>,
/// связанные <see cref="LoopbackTransport"/>. Управление (тяга) идёт <b>командами</b>,
/// а состояние на экране берётся из серверных снимков — ровно так же, как это будет
/// работать в кооперативе, только вместо loopback будет реальная сеть.
/// </para>
/// <para>
/// Управление: WASD/стрелки — тяга; B — купить, N — продать; J — прыжок; R — сброс; Esc — выход.
/// </para>
/// </remarks>
public partial class FlightSandbox : Control
{
    private const double FixedDelta = 1.0 / 60.0;
    private const float WorldToScreen = 1.5f;
    private const float GridSpacing = 64.0f;

    private GameServer _server = null!;
    private GameClient _client = null!;
    private Label _hud = null!;
    private Label _help = null!;

    private double _accumulator;
    private long _commandSequence;
    private Vector2D _lastSentThrust = new(999.0, 999.0);
    private readonly HashSet<Key> _pressedLastFrame = new();

    /// <inheritdoc />
    public override void _Ready()
    {
        Log.AddSink(new GodotLogSink());

        _hud = GetNode<Label>("Hud");
        _help = GetNode<Label>("Help");
        _help.Text = "WASD/стрелки — тяга · B — купить · N — продать · J — прыжок · R — сброс · Esc — меню";

        StartLocalHost();
        Log.Info("FlightSandbox", "Локальный хост запущен (server + client через loopback).");
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
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.Escape))
        {
            GetTree().ChangeSceneToFile("res://src/UI/Menus/MainMenu.tscn");
            return;
        }

        if (PressedOnce(Key.R))
        {
            StartLocalHost();
        }

        if (PressedOnce(Key.B))
        {
            _client.Send(new BuyItemCommand(1, _commandSequence++, 12, 1));
        }

        if (PressedOnce(Key.N))
        {
            _client.Send(new SellItemCommand(1, _commandSequence++, 12, 1));
        }

        if (PressedOnce(Key.J))
        {
            _client.Send(new JumpCommand(1, _commandSequence++, 5));
        }

        // Ввод тяги → команда (отправляем только при изменении).
        Vector2D thrust = ReadThrustInput();
        if (thrust.X != _lastSentThrust.X || thrust.Y != _lastSentThrust.Y)
        {
            _client.Send(new MoveCommand(1, _commandSequence++, thrust.X, thrust.Y));
            _lastSentThrust = thrust;
        }

        // Серверный цикл с фиксированным шагом.
        _accumulator += delta;
        while (_accumulator >= FixedDelta)
        {
            _server.Step(FixedDelta);
            _accumulator -= FixedDelta;
        }

        UpdateHud();
        QueueRedraw();
    }

    private static Vector2D ReadThrustInput()
    {
        double x = 0.0;
        double y = 0.0;
        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
        {
            x -= 1.0;
        }

        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
        {
            x += 1.0;
        }

        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
        {
            y -= 1.0;
        }

        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
        {
            y += 1.0;
        }

        return new Vector2D(x, y).ClampLength(1.0);
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

    private void UpdateHud()
    {
        SimulationSnapshot? s = _client.Latest;
        if (s is null)
        {
            _hud.Text = "Ожидание состояния...";
            return;
        }

        int cargo = 0;
        foreach (int qty in s.Cargo.Values)
        {
            cargo += qty;
        }

        string lastChat = s.ChatLog.Count > 0 ? s.ChatLog[^1] : "—";
        _hud.Text =
            $"Тик {s.Tick}   ·   система «{s.SystemName}» (галактика #{s.GalaxyIndex})\n" +
            $"Позиция  {s.Position.X,8:0.0} , {s.Position.Y,8:0.0}\n" +
            $"Скорость {s.Velocity.X,8:0.0} , {s.Velocity.Y,8:0.0}\n" +
            $"Кредиты  {s.Credits,8:0}   ·   топливо {s.Fuel,5:0.0}   ·   Δv {s.RemainingDeltaV,6:0}\n" +
            $"Трюм     {cargo} ед." +
            (s.Destroyed ? "\n[color=red]КОРАБЛЬ УНИЧТОЖЕН[/color]" : string.Empty);
    }

    /// <inheritdoc />
    public override void _Draw()
    {
        Vector2 size = Size;
        DrawRect(new Rect2(Vector2.Zero, size), new Color(0.01f, 0.012f, 0.03f));

        SimulationSnapshot? s = _client.Latest;
        if (s is null)
        {
            return;
        }

        var center = new Vector2(size.X * 0.5f, size.Y * 0.5f);
        float ox = (float)(s.Position.X * WorldToScreen);
        float oy = (float)(s.Position.Y * WorldToScreen);

        DrawGrid(size, center, ox, oy);

        // Корабль в центре (камера следует за ним).
        var nose = new Vector2(center.X, center.Y - 16.0f);
        var left = new Vector2(center.X - 11.0f, center.Y + 12.0f);
        var right = new Vector2(center.X + 11.0f, center.Y + 12.0f);
        Color shipColor = s.Destroyed ? new Color(0.6f, 0.15f, 0.15f) : new Color(0.49f, 0.99f, 0.0f);
        DrawColoredPolygon(new[] { nose, left, right }, shipColor);

        // Направление тяги.
        var thrust = new Vector2((float)s.Thrust.X, (float)s.Thrust.Y);
        if (thrust.LengthSquared() > 0.001f)
        {
            DrawLine(center, center - (thrust.Normalized() * 40.0f), new Color(1.0f, 0.5f, 0.1f), 3.0f);
        }
    }

    private void DrawGrid(Vector2 size, Vector2 center, float ox, float oy)
    {
        var gridColor = new Color(0.10f, 0.14f, 0.24f);
        float startX = center.X - Mathf.PosMod(ox, GridSpacing);
        for (float x = startX; x >= 0.0f; x -= GridSpacing)
        {
            DrawLine(new Vector2(x, 0.0f), new Vector2(x, size.Y), gridColor);
        }

        for (float x = startX + GridSpacing; x <= size.X; x += GridSpacing)
        {
            DrawLine(new Vector2(x, 0.0f), new Vector2(x, size.Y), gridColor);
        }

        float startY = center.Y - Mathf.PosMod(oy, GridSpacing);
        for (float y = startY; y >= 0.0f; y -= GridSpacing)
        {
            DrawLine(new Vector2(0.0f, y), new Vector2(size.X, y), gridColor);
        }

        for (float y = startY + GridSpacing; y <= size.Y; y += GridSpacing)
        {
            DrawLine(new Vector2(0.0f, y), new Vector2(size.X, y), gridColor);
        }
    }
}
