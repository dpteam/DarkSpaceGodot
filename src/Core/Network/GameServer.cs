using System;
using System.Collections.Generic;
using DarkSpace.Core.Simulation;
using DarkSpace.Core.Utils.Logging;

namespace DarkSpace.Core.Network;

/// <summary>
/// Авторитарный игровой сервер: единственный источник истины по состоянию мира.
/// </summary>
/// <remarks>
/// <para>
/// Сервер принимает <see cref="NetMessageType.Command"/> от игроков, декодирует их, проверяет
/// принадлежность отправителю и передаёт в <see cref="WorldSimulation"/>. По завершении тика
/// (<see cref="Step"/>) он рассылает всем игрокам снимок состояния.
/// </para>
/// <para>
/// Транспорт абстрагирован (<see cref="IMessageTransport"/>), поэтому сервер одинаково работает
/// и поверх loopback (тесты, локальный хост), и поверх реальных сокетов.
/// </para>
/// </remarks>
public sealed class GameServer
{
    private readonly WorldSimulation _simulation;
    private readonly Dictionary<int, IMessageTransport> _connections = new();
    private long _outSequence;

    /// <summary>Создаёт сервер с заданным мировым сидом.</summary>
    /// <param name="worldSeed">Мировой сид вселенной.</param>
    public GameServer(ulong worldSeed)
    {
        _simulation = new WorldSimulation(worldSeed);
    }

    /// <summary>Текущий снимок состояния мира.</summary>
    public SimulationSnapshot CurrentSnapshot => _simulation.CaptureSnapshot();

    /// <summary>
    /// Гравитационный параметр μ текущей системы — клиенты используют его для отрисовки орбит
    /// и предсказания движения (единая физика клиента и сервера).
    /// </summary>
    public double GravitationalParameter => _simulation.SystemModel.Mu;

    /// <summary>Количество подключённых игроков.</summary>
    public int ConnectionCount => _connections.Count;

    /// <summary>
    /// Регистрирует соединение игрока и немедленно отправляет ему текущий снимок.
    /// </summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    /// <param name="transport">Транспорт со стороны сервера.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="transport"/> равен <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Если игрок с таким идентификатором уже подключён.</exception>
    public void AddConnection(int playerId, IMessageTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);

        if (_connections.ContainsKey(playerId))
        {
            throw new InvalidOperationException($"Игрок {playerId} уже подключён.");
        }

        _connections[playerId] = transport;
        transport.MessageReceived += message => HandleMessage(playerId, message);
        Log.Info("GameServer", $"Игрок {playerId} подключён. Всего игроков: {_connections.Count}.");

        SendSnapshotTo(playerId, transport);
    }

    /// <summary>Отключает игрока.</summary>
    /// <param name="playerId">Идентификатор игрока.</param>
    public void RemoveConnection(int playerId)
    {
        if (_connections.Remove(playerId, out IMessageTransport? transport))
        {
            transport.Disconnect();
            Log.Info("GameServer", $"Игрок {playerId} отключён.");
        }
    }

    /// <summary>
    /// Выполняет один серверный тик и рассылает снимок всем игрокам.
    /// </summary>
    /// <param name="deltaSeconds">Длительность тика в секундах.</param>
    public void Step(double deltaSeconds)
    {
        _simulation.Advance(deltaSeconds);
        BroadcastSnapshot();
    }

    private void HandleMessage(int playerId, NetworkMessage message)
    {
        if (message.Type != NetMessageType.Command)
        {
            return;
        }

        IGameCommand command;
        try
        {
            command = CommandCodec.Decode(message.Payload);
        }
        catch (FormatException ex)
        {
            Log.Warning("GameServer", $"Игрок {playerId}: некорректная команда — {ex.Message}");
            return;
        }

        // Авторитарная проверка: игрок может управлять только своей сущностью.
        if (command.PlayerId != playerId)
        {
            Log.Warning("GameServer", $"Игрок {playerId} попытался отправить команду от лица {command.PlayerId}. Отклонено.");
            return;
        }

        _simulation.Enqueue(command);
    }

    private void BroadcastSnapshot()
    {
        foreach (KeyValuePair<int, IMessageTransport> connection in _connections)
        {
            SendSnapshotTo(connection.Key, connection.Value);
        }
    }

    private void SendSnapshotTo(int playerId, IMessageTransport transport)
    {
        string payload = MessageCodec.EncodePayload(_simulation.CaptureSnapshot());
        transport.Send(new NetworkMessage(NetMessageType.Snapshot, 0, _outSequence++, payload));
    }
}
