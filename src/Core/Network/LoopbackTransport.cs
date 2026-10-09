using System;

namespace DarkSpace.Core.Network;

/// <summary>
/// Транспорт «в памяти»: два соединённых конца доставляют сообщения друг другу синхронно.
/// </summary>
/// <remarks>
/// Используется для тестов, а также для режима «одиночная игра как локальный хост»
/// (клиент и сервер в одном процессе). Не является потокобезопасным: в тестах вызовы
/// синхронны, а в слое движка обмен идёт из главного потока.
/// </remarks>
public sealed class LoopbackTransport : IMessageTransport
{
    private LoopbackTransport? _peer;

    /// <inheritdoc />
    public bool IsConnected { get; private set; }

    /// <inheritdoc />
    public event Action<NetworkMessage>? MessageReceived;

    /// <summary>Создаёт пару соединённых транспортов «клиент — сервер».</summary>
    /// <param name="clientEnd">Транспорт на стороне клиента.</param>
    /// <param name="serverEnd">Транспорт на стороне сервера.</param>
    public static void CreatePair(out LoopbackTransport clientEnd, out LoopbackTransport serverEnd)
    {
        clientEnd = new LoopbackTransport();
        serverEnd = new LoopbackTransport();
        clientEnd._peer = serverEnd;
        serverEnd._peer = clientEnd;
        clientEnd.IsConnected = true;
        serverEnd.IsConnected = true;
    }

    /// <inheritdoc />
    public void Send(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!IsConnected)
        {
            throw new InvalidOperationException("Попытка отправить сообщение по закрытому соединению.");
        }

        _peer?.Deliver(message);
    }

    /// <inheritdoc />
    public void Disconnect()
    {
        IsConnected = false;
        LoopbackTransport? peer = _peer;
        _peer = null;
        if (peer is not null)
        {
            peer.IsConnected = false;
            peer._peer = null;
        }
    }

    private void Deliver(NetworkMessage message) => MessageReceived?.Invoke(message);
}
