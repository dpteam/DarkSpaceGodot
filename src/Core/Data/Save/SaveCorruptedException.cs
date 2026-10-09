using System;

namespace DarkSpace.Core.Data.Save;

/// <summary>
/// Исключение, возникающее при обнаружении повреждённого сохранения.
/// </summary>
/// <remarks>
/// Наличие этого исключения — сигнал системе менеджмента сохранений попытаться
/// восстановиться из последнего рабочего бэкапа.
/// </remarks>
public sealed class SaveCorruptedException : Exception
{
    /// <summary>Создаёт исключение с сообщением.</summary>
    public SaveCorruptedException(string message)
        : base(message)
    {
    }

    /// <summary>Создаёт исключение с сообщением и внутренним исключением.</summary>
    public SaveCorruptedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
