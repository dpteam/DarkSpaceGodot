using System.Collections.Generic;
using DarkSpace.Core.Simulation;
using Godot;

namespace DarkSpace.Gameplay.Ship;

/// <summary>
/// Экран торговли на станции в духе Elite: таблица товаров с ценами покупки/продажи,
/// запасом, вашим трюмом и кредитами.
/// </summary>
/// <remarks>
/// Рисуется в 2D поверх вида полёта. Данные берутся из <see cref="Snapshot"/> (котировки рынка).
/// Выбор товара и действия выполняет вид полёта (см. <see cref="Selected"/>).
/// </remarks>
public partial class MarketPanel : Control
{
    /// <summary>Индекс выбранного товара.</summary>
    public int Selected;

    /// <summary>Текущий снимок симуляции (содержит котировки рынка и трюм).</summary>
    public SimulationSnapshot? Snapshot;

    private const float RowHeight = 26f;
    private static readonly Color Green = new(0.49f, 0.99f, 0.0f);
    private static readonly Color Cyan = new(0.0f, 0.9f, 0.9f);
    private static readonly Color Bg = new(0.01f, 0.015f, 0.03f, 0.94f);
    private static readonly Color DimGreen = new(0.25f, 0.5f, 0.25f);
    private static readonly Color Highlight = new(0.12f, 0.22f, 0.12f);

    /// <inheritdoc />
    public override void _Process(double delta) => QueueRedraw();

    /// <inheritdoc />
    public override void _Draw()
    {
        SimulationSnapshot? s = Snapshot;
        if (s is null)
        {
            return;
        }

        Vector2 size = Size;
        float w = Mathf.Min(size.X * 0.82f, 1000f);
        float h = Mathf.Min(size.Y * 0.8f, 720f);
        float x = (size.X - w) * 0.5f;
        float y = (size.Y - h) * 0.5f;

        DrawRect(new Rect2(x, y, w, h), Bg);
        DrawRect(new Rect2(x, y, w, h), Green, false, 2f);

        Font font = ThemeDB.FallbackFont;
        if (font is null)
        {
            return;
        }

        DrawString(font, new Vector2(x + 20f, y + 30f), $"СТАНЦИЯ «{s.StationName}»   ·   рынок", HorizontalAlignment.Left, -1f, 22, Green);
        DrawString(font, new Vector2(x + w - 360f, y + 30f), $"Кредиты: {s.Credits:0}", HorizontalAlignment.Left, -1f, 22, Cyan);

        // Заголовки колонок.
        float cx = x + 20f;
        float headerY = y + 60f;
        DrawString(font, new Vector2(cx, headerY), "Товар", HorizontalAlignment.Left, -1f, 15, DimGreen);
        DrawString(font, new Vector2(cx + 320f, headerY), "Покупка", HorizontalAlignment.Left, -1f, 15, DimGreen);
        DrawString(font, new Vector2(cx + 440f, headerY), "Продажа", HorizontalAlignment.Left, -1f, 15, DimGreen);
        DrawString(font, new Vector2(cx + 560f, headerY), "Запас", HorizontalAlignment.Left, -1f, 15, DimGreen);
        DrawString(font, new Vector2(cx + 680f, headerY), "В трюме", HorizontalAlignment.Left, -1f, 15, DimGreen);

        IReadOnlyList<DarkSpace.Core.Systems.Economy.CommodityQuote> market = s.Market;
        float rowY = headerY + 26f;
        for (int i = 0; i < market.Count; i++)
        {
            DarkSpace.Core.Systems.Economy.CommodityQuote q = market[i];
            float ry = rowY + (i * RowHeight);
            if (i == Selected)
            {
                DrawRect(new Rect2(cx - 6f, ry - 16f, w - 40f, RowHeight), Highlight);
            }

            Color col = i == Selected ? Green : new Color(0.8f, 0.85f, 0.9f);
            int owned = s.TradeCargo.GetValueOrDefault(q.CommodityId);
            DrawString(font, new Vector2(cx, ry), $"{q.Name} ({q.Unit})", HorizontalAlignment.Left, -1f, 15, col);
            DrawString(font, new Vector2(cx + 320f, ry), $"{q.BuyPrice}", HorizontalAlignment.Left, -1f, 15, col);
            DrawString(font, new Vector2(cx + 440f, ry), $"{q.SellPrice}", HorizontalAlignment.Left, -1f, 15, col);
            DrawString(font, new Vector2(cx + 560f, ry), $"{q.Stock}", HorizontalAlignment.Left, -1f, 15, col);
            DrawString(font, new Vector2(cx + 680f, ry), $"{owned}", HorizontalAlignment.Left, -1f, 15, owned > 0 ? Cyan : col);
        }

        DrawString(font, new Vector2(cx, y + h - 24f),
            "↑/↓ — выбор · B — купить 1 · N — продать 1 · M/Esc — выйти", HorizontalAlignment.Left, -1f, 14, DimGreen);
    }
}
