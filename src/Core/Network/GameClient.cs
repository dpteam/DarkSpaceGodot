using System;
using DarkSpace.Core.Simulation;

namespace DarkSpace.Core.Network;

/// <summary>
/// Клиент: отправляет команды на сервер и хранит последний полученный снимок состояния.
/// </summary>
/// <remarks>
/// Пока реализована базовая синхронизация без предсказания: клиент лишь применяет присланные
/// сервером снимки. Клиентское предсказание и лаг-компенсация — задачи Этапа 4 (см. <c>MEMORY.md</c>).
/// </remarks>
public sealed class GameClient
{
    private readonly IMessageTransport _transport;
    private long _sequence;

    /// <summary>Последний полученный от сервера снимок состояния (или <see langword="null"/>).</summary>
    public SimulationSnapshot? Latest { get; private set; }

    /// <summary>Транспорт соединения.</summary>
    public IMessageTransport Transport => _transport;

    /// <summary>
    /// Создаёт клиент поверх транспорта и подписывается на входящие сообщения.
    /// </summary>
    /// <param name="transport">Транспорт соединения.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="transport"/> равен <see langword="null"/>.</exception>
    public GameClient(IMessageTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _transport = transport;
        _transport.MessageReceived += OnMessage;
    }

    /// <summary>
    /// Отправляет команду на сервер.
    /// </summary>
    /// <param name="command">Команда.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="command"/> равен <see langword="null"/>.</exception>
    public void Send(IGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        string payload = CommandCodec.Encode(command);
        _transport.Send(new NetworkMessage(NetMessageType.Command, command.PlayerId, command.Sequence, payload));
    }

    /// <summary>Отправляет команду с автоматически назначенным порядковым номером.</summary>
    /// <param name="factory">Функция, создающая команду по номеру последовательности.</param>
    public void SendWithNextSequence(Func<long, IGameCommand> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Send(factory(_sequence++));
    }

    private void OnMessage(NetworkMessage message)
    {
        if (message.Type != NetMessageType.Snapshot)
        {
            return;
        }

        SimulationSnapshot? snapshot = MessageCodec.DecodePayload<SimulationSnapshot>(message.Payload);
        if (snapshot is not null)
        {
            Latest = snapshot;
        }
    }
}
