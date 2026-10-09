namespace DarkSpace.Core.Network;

/// <summary>Тип сетевого сообщения.</summary>
public enum NetMessageType
{
    /// <summary>Приветствие/рукопожатие.</summary>
    Hello,

    /// <summary>Команда игрока, отправляемая на авторитарный сервер.</summary>
    Command,

    /// <summary>Снимок состояния мира, рассылаемый сервером.</summary>
    Snapshot,

    /// <summary>Разрыв соединения.</summary>
    Disconnect,
}
