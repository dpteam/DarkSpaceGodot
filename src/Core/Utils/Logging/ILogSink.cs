namespace DarkSpace.Core.Utils.Logging;

/// <summary>Уровень важности сообщения лога.</summary>
public enum LogLevel
{
    /// <summary>Подробная отладочная информация.</summary>
    Debug,

    /// <summary>Обычное информационное сообщение.</summary>
    Info,

    /// <summary>Предупреждение.</summary>
    Warning,

    /// <summary>Ошибка.</summary>
    Error,
}

/// <summary>
/// Приёмник сообщений лога. Реализации могут писать в консоль движка, файл, сеть и т.д.
/// </summary>
public interface ILogSink
{
    /// <summary>Записывает сообщение указанного уровня.</summary>
    /// <param name="level">Уровень важности.</param>
    /// <param name="category">Категория (обычно имя системы).</param>
    /// <param name="message">Текст сообщения.</param>
    void Write(LogLevel level, string category, string message);
}
