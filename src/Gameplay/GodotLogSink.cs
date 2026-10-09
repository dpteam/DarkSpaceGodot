using DarkSpace.Core.Utils.Logging;
using Godot;

namespace DarkSpace.Gameplay;

/// <summary>
/// Приёмник лога, выводящий сообщения в консоль Godot через <see cref="GD.PrintRich(string)"/>.
/// </summary>
/// <remarks>
/// Единственное место в проекте, где допускается прямой вывод в консоль движка.
/// </remarks>
public sealed class GodotLogSink : ILogSink
{
    /// <inheritdoc />
    public void Write(LogLevel level, string category, string message)
    {
        string color = level switch
        {
            LogLevel.Debug => "gray",
            LogLevel.Info => "white",
            LogLevel.Warning => "orange",
            LogLevel.Error => "red",
            _ => "white",
        };

        GD.PrintRich($"[color={color}][{level}] [{category}][/color] {message}");
    }
}
