using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Simulation;
using Godot;

namespace DarkSpace.Gameplay.Ship;

/// <summary>
/// Кокпит-HUD в духе Elite: прицел по центру и приборная панель снизу со сканером.
/// </summary>
/// <remarks>
/// Рисуется в 2D поверх 3D-вида. Данные берутся из <see cref="Snapshot"/> и списка блипов сканера
/// (<see cref="Blips"/>), который заполняет вид полёта. Стиль — минималистичный, зелёно-циановый,
/// как в Elite.
/// </remarks>
public partial class ShipHud : Control
{
    /// <summary>Блип на сканере (объект относительно корабля).</summary>
    /// <param name="Right">Смещение вправо (положительное — справа).</param>
    /// <param name="Forward">Смещение вперёд (положительное — впереди).</param>
    /// <param name="Ahead">Объект впереди корабля (для отрисовки).</param>
    /// <param name="IsTarget">Объект является текущей целью.</param>
    public readonly record struct ScannerBlip(float Right, float Forward, bool Ahead, bool IsTarget);

    /// <summary>Дальность сканера в игровых единицах.</summary>
    public float ScannerRange = 90.0f;

    /// <summary>Максимальная скорость (для шкалы скорости).</summary>
    public double MaxSpeed = 60.0;

    /// <summary>Текущий снимок симуляции.</summary>
    public SimulationSnapshot? Snapshot;

    /// <summary>Блипы для сканера (заполняется видом полёта каждый кадр).</summary>
    public readonly List<ScannerBlip> Blips = new();

    private const float DashboardHeight = 112f;
    private static readonly Color Green = new(0.49f, 0.99f, 0.0f);
    private static readonly Color Cyan = new(0.0f, 0.9f, 0.9f);
    private static readonly Color PanelBg = new(0.015f, 0.02f, 0.035f, 0.94f);
    private static readonly Color DimGreen = new(0.2f, 0.5f, 0.2f, 0.8f);

    /// <inheritdoc />
    public override void _Process(double delta) => QueueRedraw();

    /// <inheritdoc />
    public override void _Draw()
    {
        Vector2 size = Size;
        DrawCrosshair(size);
        DrawDashboard(size);
    }

    private void DrawCrosshair(Vector2 size)
    {
        var c = new Vector2(size.X * 0.5f, size.Y * 0.5f);
        const float len = 9f;
        const float gap = 14f;

        DrawLine(new Vector2(c.X - gap - len, c.Y), new Vector2(c.X - gap, c.Y), Green, 2f);
        DrawLine(new Vector2(c.X + gap, c.Y), new Vector2(c.X + gap + len, c.Y), Green, 2f);
        DrawLine(new Vector2(c.X, c.Y - gap - len), new Vector2(c.X, c.Y - gap), Green, 2f);
        DrawLine(new Vector2(c.X, c.Y + gap), new Vector2(c.X, c.Y + gap + len), Green, 2f);
        DrawRect(new Rect2(c.X - 2f, c.Y - 2f, 4f, 4f), Green);
    }

    private void DrawDashboard(Vector2 size)
    {
        float top = size.Y - DashboardHeight;
        DrawRect(new Rect2(0f, top, size.X, DashboardHeight), PanelBg);
        DrawLine(new Vector2(0f, top), new Vector2(size.X, top), Green, 1.5f);

        DrawSpeed(size, top);
        DrawPitchRoll(size, top);
        DrawScanner(size, top);
        DrawSystems(size, top);
    }

    private void DrawSpeed(Vector2 size, float top)
    {
        double speed = Snapshot?.Speed ?? 0.0;
        float frac = (float)Mathf.Clamp(speed / MaxSpeed, 0.0, 1.0);

        float x = 28f;
        float barTop = top + 46f;
        float barHeight = 48f;
        var frame = new Rect2(x, barTop, 22f, barHeight);
        DrawRect(frame, new Color(0f, 0f, 0f, 0.6f));
        DrawRect(frame, DimGreen, false, 1.5f);

        float fill = barHeight * frac;
        DrawRect(new Rect2(x + 2f, barTop + (barHeight - fill) + 2f, 18f, Mathf.Max(0f, fill - 3f)), Green);
        DrawTextSmall(new Vector2(x - 4f, top + 38f), "СПД");
    }

    private void DrawPitchRoll(Vector2 size, float top)
    {
        // Индикатор крена: горизонтальная шкала с меткой банка корабля.
        Vector2 center = new(size.X * 0.5f, top + 22f);
        float half = 120f;
        DrawLine(new Vector2(center.X - half, center.Y), new Vector2(center.X + half, center.Y), DimGreen, 1.5f);
        DrawLine(new Vector2(center.X, center.Y - 6f), new Vector2(center.X, center.Y + 6f), DimGreen, 1.5f);

        if (Snapshot is { } s)
        {
            // Банк = проекция локального «вверх» корабля на мировое «вправо» (в плоскости эклиптики).
            var up = s.Orientation.Rotate(DarkSpace.Core.Systems.Physics.ShipAxes.Up);
            double roll = Vector3D.Dot(up, new Vector3D(1.0, 0.0, 0.0));
            float marker = center.X + (float)Mathf.Clamp(roll, -1.0, 1.0) * half;
            DrawLine(new Vector2(marker, center.Y - 8f), new Vector2(marker, center.Y + 8f), Cyan, 2f);
        }
    }

    private void DrawScanner(Vector2 size, float top)
    {
        float sw = Mathf.Min(size.X * 0.46f, 520f);
        float sh = 80f;
        float sx = (size.X - sw) * 0.5f;
        float sy = top + 26f;

        DrawRect(new Rect2(sx, sy, sw, sh), new Color(0f, 0f, 0f, 0.55f));
        DrawRect(new Rect2(sx, sy, sw, sh), DimGreen, false, 1.5f);

        // Центральная линия (курс) и дальние риски.
        DrawLine(new Vector2(sx + (sw * 0.5f), sy), new Vector2(sx + (sw * 0.5f), sy + sh), DimGreen, 1f);
        for (int i = 1; i <= 3; i++)
        {
            float gy = sy + (sh * i / 4f);
            DrawLine(new Vector2(sx, gy), new Vector2(sx + 10f, gy), DimGreen, 1f);
            DrawLine(new Vector2(sx + sw - 10f, gy), new Vector2(sx + sw, gy), DimGreen, 1f);
        }

        // Блипы объектов (впереди — выше, сзади — у нижнего края).
        foreach (ScannerBlip blip in Blips)
        {
            float nx = Mathf.Clamp(blip.Right / ScannerRange, -0.5f, 0.5f);
            float nf = Mathf.Clamp(blip.Forward / ScannerRange, -1f, 1f);
            float bx = sx + (sw * 0.5f) + (nx * sw);
            float by = blip.Ahead
                ? sy + sh - (nf * sh * 0.9f)
                : sy + sh - 3f;

            Color col = blip.IsTarget ? Green : Cyan;
            float r = blip.IsTarget ? 3.5f : 2.5f;
            DrawRect(new Rect2(bx - r, by - r, r * 2f, r * 2f), col);
        }

        // Метка нашего корабля — внизу по центру.
        DrawRect(new Rect2(sx + (sw * 0.5f) - 3f, sy + sh - 5f, 6f, 5f), Green);
        DrawTextSmall(new Vector2(sx, top + 18f), "СКАНЕР");
    }

    private void DrawSystems(Vector2 size, float top)
    {
        // Правый блок: топливо/энергия/прочность (заглушки-бары).
        double fuelFrac = Snapshot is { } s && s.ShipMass > 0
            ? Mathf.Clamp(s.Fuel / 50.0, 0.0, 1.0)
            : 0.0;

        float x = size.X - 190f;
        DrawBar(new Vector2(x, top + 30f), 150f, 12f, (float)fuelFrac, Green, "ТОПЛИВО");
        DrawBar(new Vector2(x, top + 62f), 150f, 12f, 0.85f, Cyan, "ЭНЕРГИЯ");
        DrawBar(new Vector2(x, top + 94f), 150f, 12f, 1.0f, Cyan, "ЩИТЫ");
    }

    private void DrawBar(Vector2 pos, float width, float height, float fraction, Color color, string label)
    {
        DrawRect(new Rect2(pos.X, pos.Y, width, height), new Color(0f, 0f, 0f, 0.6f));
        DrawRect(new Rect2(pos.X, pos.Y, width, height), DimGreen, false, 1f);
        DrawRect(new Rect2(pos.X + 1f, pos.Y + 1f, (width - 2f) * Mathf.Clamp(fraction, 0f, 1f), height - 2f), color);
        DrawTextSmall(new Vector2(pos.X, pos.Y - 6f), label);
    }

    private void DrawTextSmall(Vector2 pos, string text)
    {
        Font font = ThemeDB.FallbackFont;
        if (font is null)
        {
            return;
        }

        DrawString(font, pos, text, HorizontalAlignment.Left, -1f, 11, new Color(0.55f, 0.75f, 0.55f));
    }
}
