using DarkSpace.Core.Network;
using DarkSpace.Core.Simulation;
using DarkSpace.Core.Systems.Physics;
using Xunit;

namespace DarkSpace.Tests.Integration;

/// <summary>
/// Интеграционные тесты мультиплеера поверх loopback-транспорта:
/// клиент → сервер → снимок состояния, а также детерминизм между инстансами сервера.
/// </summary>
public sealed class MultiplayerLoopbackIntegrationTests
{
    private const double Dt = 1.0 / 30.0;
    private const ulong WorldSeed = 0x4E455432303236UL; // "NET2O26"

    [Fact]
    public void ClientAndServer_ConvergeOverManyTicks()
    {
        var server = new GameServer(WorldSeed);
        LoopbackTransport.CreatePair(out LoopbackTransport clientTransport, out LoopbackTransport serverTransport);
        var client = new GameClient(clientTransport);
        server.AddConnection(1, serverTransport);

        client.Send(new MoveCommand(1, 0, 0.7, 0.2));
        for (int i = 0; i < 120; i++)
        {
            server.Step(Dt);
        }

        Assert.NotNull(client.Latest);
        Assert.Equal(server.CurrentSnapshot.Tick, client.Latest!.Tick);

        // Позиция, вычисленная сервером, доходит до клиента без искажений.
        Assert.Equal(server.CurrentSnapshot.Position, client.Latest.Position);
        Assert.True(client.Latest.Position.X > 0);
    }

    [Fact]
    public void TwoServers_SameCommandScript_ProduceIdenticalSnapshots()
    {
        SimulationSnapshot a = RunServerScript();
        SimulationSnapshot b = RunServerScript();

        Assert.Equal(MessageCodec.EncodePayload(a), MessageCodec.EncodePayload(b));
    }

    [Fact]
    public void TwoPlayers_ReceiveSameAuthoritativeSnapshot()
    {
        var server = new GameServer(WorldSeed);

        LoopbackTransport.CreatePair(out LoopbackTransport c1t, out LoopbackTransport s1t);
        LoopbackTransport.CreatePair(out LoopbackTransport c2t, out LoopbackTransport s2t);
        var client1 = new GameClient(c1t);
        var client2 = new GameClient(c2t);
        server.AddConnection(1, s1t);
        server.AddConnection(2, s2t);

        client1.Send(new MoveCommand(1, 0, 1.0, 0.0));
        client2.Send(new ChatCommand(2, 0, "Лечу рядом."));
        server.Step(Dt);

        Assert.NotNull(client1.Latest);
        Assert.NotNull(client2.Latest);
        Assert.Equal(client1.Latest!.Position, client2.Latest!.Position);
        Assert.Contains("[2] Лечу рядом.", client1.Latest.ChatLog);
    }

    [Fact]
    public void Multiplayer_TradeIsAuthoritative()
    {
        var server = new GameServer(WorldSeed);
        LoopbackTransport.CreatePair(out LoopbackTransport clientTransport, out LoopbackTransport serverTransport);
        var client = new GameClient(clientTransport);
        server.AddConnection(1, serverTransport);

        client.Send(new BuyItemCommand(1, 0, 12, 4));
        server.Step(Dt);

        Assert.NotNull(client.Latest);
        Assert.Equal(4, client.Latest!.Cargo[12]);
        Assert.True(client.Latest.Credits < WorldSimulation.StartingCredits);
    }

    private static SimulationSnapshot RunServerScript()
    {
        var server = new GameServer(WorldSeed);
        LoopbackTransport.CreatePair(out LoopbackTransport clientTransport, out LoopbackTransport serverTransport);
        server.AddConnection(1, serverTransport);

        clientTransport.Send(new NetworkMessage(NetMessageType.Command, 1, 0, CommandCodec.Encode(new MoveCommand(1, 0, 0.4, -0.2))));
        clientTransport.Send(new NetworkMessage(NetMessageType.Command, 1, 1, CommandCodec.Encode(new BuyItemCommand(1, 1, 12, 2))));
        for (int i = 0; i < 90; i++)
        {
            server.Step(Dt);
        }

        return server.CurrentSnapshot;
    }

    [Fact]
    public void Server_HandlesTargetAndDockCommands()
    {
        var server = new GameServer(WorldSeed);
        LoopbackTransport.CreatePair(out LoopbackTransport clientTransport, out LoopbackTransport serverTransport);
        var client = new GameClient(clientTransport);
        server.AddConnection(1, serverTransport);

        client.Send(new SetTargetCommand(1, 0, 1));
        server.Step(Dt);
        Assert.Equal(1, client.Latest!.TargetBodyIndex);

        // Стыковка вдали от планеты не должна срабатывать.
        client.Send(new DockCommand(1, 1));
        server.Step(Dt);
        Assert.Equal(-1, client.Latest!.DockedBodyIndex);
    }

    [Fact]
    public void ThreeDimensionalFlight_IsSynchronized()
    {
        var server = new GameServer(WorldSeed);
        LoopbackTransport.CreatePair(out LoopbackTransport clientTransport, out LoopbackTransport serverTransport);
        var client = new GameClient(clientTransport);
        server.AddConnection(1, serverTransport);

        // Тяга по всем трём осям.
        client.Send(new MoveCommand(1, 0, 0.3, 0.2, -0.4));
        for (int i = 0; i < 60; i++)
        {
            server.Step(Dt);
        }

        Assert.NotNull(client.Latest);
        Assert.Equal(server.CurrentSnapshot.Position, client.Latest!.Position);
        Assert.True(client.Latest.Position.Z < 0, "Тяга по -Z должна привести к отрицательной координате Z.");
    }

    [Fact]
    public void ServerSimulation_ConservesEnergy_WithoutThrust()
    {
        // Без команд корабль движется только под гравитацией: удельная энергия должна сохраняться
        // (проверяем тот же закон, что и в ядре, но сквозь сетевой слой).
        var server = new GameServer(WorldSeed);
        LoopbackTransport.CreatePair(out LoopbackTransport clientTransport, out LoopbackTransport serverTransport);
        server.AddConnection(1, serverTransport);

        double mu = server.GravitationalParameter;
        double Energy(SimulationSnapshot s) => Astro.SoftenedSpecificEnergy(mu, WorldSimulation.GravitySoftening, s.Position, s.Velocity);

        // Придаём импульс один раз, затем обнуляем тягу и даём двигаться только под гравитацией.
        clientTransport.Send(new NetworkMessage(NetMessageType.Command, 1, 0, CommandCodec.Encode(new MoveCommand(1, 0, 1.0, 0.0, 0.3))));
        server.Step(Dt);
        clientTransport.Send(new NetworkMessage(NetMessageType.Command, 1, 1, CommandCodec.Encode(new MoveCommand(1, 1, 0.0, 0.0, 0.0))));
        server.Step(Dt);
        double e0 = Energy(server.CurrentSnapshot);

        for (int i = 0; i < 2000; i++)
        {
            server.Step(Dt);
        }

        double e1 = Energy(server.CurrentSnapshot);
        Assert.True(Math.Abs((e1 - e0) / e0) < 0.01, $"Энергия через сеть должна сохраняться (e0={e0:0.000}, e1={e1:0.000}).");
    }
}
