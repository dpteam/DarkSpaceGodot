using System;
using System.Collections.Generic;

namespace DarkSpace.Core.Utils.Logging;

/// <summary>
/// Централизованный логгер проекта.
/// </summary>
/// <remarks>
/// В продакшн-коде запрещено использовать <c>GD.Print</c> и <c>Console.WriteLine</c> напрямую.
/// Слой движка регистрирует приёмник (например, пишущий через <c>GD.PrintRich</c>),
/// а ядро лишь вызывает методы этого класса.
/// </remarks>
public static class Log
{
    private static readonly List<ILogSink> Sinks = new();

    /// <summary>Добавляет приёмник сообщений.</summary>
    /// <exception cref="ArgumentNullException">Если <paramref name="sink"/> равен <see langword="null"/>.</exception>
    public static void AddSink(ILogSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        lock (Sinks)
        {
            Sinks.Add(sink);
        }
    }

    /// <summary>Удаляет все зарегистрированные приёмники (используется в тестах).</summary>
    public static void ClearSinks()
    {
        lock (Sinks)
        {
            Sinks.Clear();
        }
    }

    /// <summary>Записывает отладочное сообщение.</summary>
    public static void Debug(string category, string message) => Emit(LogLevel.Debug, category, message);

    /// <summary>Записывает информационное сообщение.</summary>
    public static void Info(string category, string message) => Emit(LogLevel.Info, category, message);

    /// <summary>Записывает предупреждение.</summary>
    public static void Warning(string category, string message) => Emit(LogLevel.Warning, category, message);

    /// <summary>Записывает сообщение об ошибке.</summary>
    public static void Error(string category, string message) => Emit(LogLevel.Error, category, message);

    private static void Emit(LogLevel level, string category, string message)
    {
        lock (Sinks)
        {
            foreach (ILogSink sink in Sinks)
            {
                sink.Write(level, category, message);
            }
        }
    }
}
