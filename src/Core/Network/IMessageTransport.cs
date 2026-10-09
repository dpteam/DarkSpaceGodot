using System;

namespace DarkSpace.Core.Network;

/// <summary>
/// Абстракция транспорта для обмена сетевыми сообщениями.
/// </summary>
/// <remarks>
/// Ядро не знает, как именно передаются байты. В тестах и в одиночной игре используется
/// <see cref="LoopbackTransport"/>; в слое движка будет реализация поверх
/// <c>ENetMultiplayerPeer</c>/<c>WebSocketMultiplayerPeer</c> (Этап 4).
/// </remarks>
public interface IMessageTransport
{
    /// <summary>Признак активного соединения.</summary>
    bool IsConnected { get; }

    /// <summary>Событие поступления сообщения.</summary>
    event Action<NetworkMessage>? MessageReceived;

    /// <summary>Отправляет сообщение на противоположный конец соединения.</summary>
    /// <param name="message">Сообщение.</param>
    void Send(NetworkMessage message);

    /// <summary>Закрывает соединение.</summary>
    void Disconnect();
}
