using DarkSpace.Core.Network;
using DarkSpace.Core.Simulation;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты транспорта, кодека сообщений и связки клиент–сервер.</summary>
public sealed class NetworkFoundationTests
{
    [Fact]
    public void Loopback_DeliversMessagesBothWays()
    {
        LoopbackTransport.CreatePair(out LoopbackTransport client, out LoopbackTransport server);

        NetworkMessage? atServer = null;
        NetworkMessage? atClient = null;
        server.MessageReceived += m => atServer = m;
        client.MessageReceived += m => atClient = m;

        client.Send(new NetworkMessage(NetMessageType.Hello, 1, 0, "{}"));
        server.Send(new NetworkMessage(NetMessageType.Snapshot, 0, 0, "{}"));

        Assert.NotNull(atServer);
        Assert.NotNull(atClient);
        Assert.Equal(NetMessageType.Hello, atServer!.Type);
        Assert.Equal(NetMessageType.Snapshot, atClient!.Type);
    }

    [Fact]
    public void Disconnect_StopsDeliveryAndFlipsFlag()
    {
        LoopbackTransport.CreatePair(out LoopbackTransport client, out LoopbackTransport server);
        bool received = false;
        server.MessageReceived += _ => received = true;

        client.Disconnect();

        Assert.False(client.IsConnected);
        Assert.False(server.IsConnected);
        Assert.Throws<InvalidOperationException>(() => client.Send(new NetworkMessage(NetMessageType.Hello, 1, 0, "{}")));
        Assert.False(received);
    }

    [Fact]
    public void MessageCodec_RoundTrips()
    {
        var message = new NetworkMessage(NetMessageType.Command, 7, 99, "{\"x\":1}");
        string json = MessageCodec.Encode(message);
        NetworkMessage decoded = MessageCodec.Decode(json);
        Assert.Equal(message, decoded);
    }

    [Fact]
    public void MessageCodec_Payload_RoundTrips()
    {
        SimulationSnapshot snapshot = new WorldSimulation(5).CaptureSnapshot();
        string json = MessageCodec.EncodePayload(snapshot);
        SimulationSnapshot? decoded = MessageCodec.DecodePayload<SimulationSnapshot>(json);
        Assert.NotNull(decoded);
        Assert.Equal(snapshot.WorldSeed, decoded!.WorldSeed);
        Assert.Equal(snapshot.SystemName, decoded.SystemName);
    }

    [Fact]
    public void Server_SendsInitialSnapshot_OnConnect()
    {
        var server = new GameServer(42);
        LoopbackTransport.CreatePair(out LoopbackTransport clientTransport, out LoopbackTransport serverTransport);
        var client = new GameClient(clientTransport);

        server.AddConnection(1, serverTransport);

        Assert.NotNull(client.Latest);
        Assert.Equal(0, client.Latest!.Tick);
        Assert.Equal(42UL, client.Latest.WorldSeed);
    }

    [Fact]
    public void Server_AppliesClientCommand_AndBroadcastsSnapshot()
    {
        var server = new GameServer(42);
        LoopbackTransport.CreatePair(out LoopbackTransport clientTransport, out LoopbackTransport serverTransport);
        var client = new GameClient(clientTransport);
        server.AddConnection(1, serverTransport);

        // Тяга вперёд (элитовская модель): должна появиться скорость.
        client.Send(new MoveCommand(1, 0, 1.0, 0.0));
        server.Step(1.0 / 30.0);

        Assert.NotNull(client.Latest);
        Assert.Equal(1, client.Latest!.Tick);
        Assert.True(client.Latest.Speed > 0);
    }

    [Fact]
    public void Server_RejectsCommandFromWrongPlayer()
    {
        var server = new GameServer(42);
        LoopbackTransport.CreatePair(out LoopbackTransport clientTransport, out LoopbackTransport serverTransport);
        server.AddConnection(1, serverTransport);

        // Игрок 1 пытается действовать от имени игрока 2 — команда должна быть проигнорирована.
        clientTransport.Send(new NetworkMessage(
            NetMessageType.Command,
            1,
            0,
            CommandCodec.Encode(new MoveCommand(2, 0, 1.0, 0.0))));

        server.Step(1.0 / 30.0);
        Assert.Equal(0, server.CurrentSnapshot.Velocity.X, precision: 6);
    }

    [Fact]
    public void Server_IgnoresMalformedCommand()
    {
        var server = new GameServer(42);
        LoopbackTransport.CreatePair(out LoopbackTransport clientTransport, out LoopbackTransport serverTransport);
        server.AddConnection(1, serverTransport);

        clientTransport.Send(new NetworkMessage(NetMessageType.Command, 1, 0, "{ мусор"));

        // Не должно бросать исключение наружу.
        server.Step(1.0 / 30.0);
        Assert.Equal(1, server.CurrentSnapshot.Tick);
    }
}
