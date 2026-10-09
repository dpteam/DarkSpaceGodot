using DarkSpace.Core.Network;
using DarkSpace.Core.Simulation;
using Xunit;

namespace DarkSpace.Tests.Unit;

/// <summary>Тесты кодека игровых команд.</summary>
public sealed class CommandCodecTests
{
    public static IEnumerable<object[]> AllCommands()
    {
        yield return new object[] { new MoveCommand(1, 10, 0.5, -0.5) };
        yield return new object[] { new MoveCommand(1, 11, 0.5, -0.5, 0.25) };
        yield return new object[] { new RotateCommand(1, 12, 0.3, -0.4, 0.5) };
        yield return new object[] { new SetTargetCommand(2, 12, 3) };
        yield return new object[] { new DockCommand(2, 13) };
        yield return new object[] { new JumpCommand(2, 20, 42) };
        yield return new object[] { new BuyItemCommand(3, 30, 7, 5) };
        yield return new object[] { new SellItemCommand(4, 40, 9, 2) };
        yield return new object[] { new ChatCommand(5, 50, "Привет, пилот!") };
    }

    [Theory]
    [MemberData(nameof(AllCommands))]
    public void RoundTrip_PreservesCommand(IGameCommand command)
    {
        string json = CommandCodec.Encode(command);
        IGameCommand decoded = CommandCodec.Decode(json);
        Assert.Equal(command, decoded);
    }

    [Fact]
    public void Encode_ContainsDiscriminator()
    {
        string json = CommandCodec.Encode(new MoveCommand(1, 2, 0.1, 0.2));
        Assert.Contains("\"type\":\"Move\"", json);
        Assert.Contains("\"lift\"", json);
    }

    [Fact]
    public void MoveCommand_DefaultsLiftToZero()
    {
        var command = new MoveCommand(1, 0, 0.5, 0.5);
        Assert.Equal(0.0, command.Lift);
        Assert.Equal(command, CommandCodec.Decode(CommandCodec.Encode(command)));
    }

    [Fact]
    public void RotateCommand_RoundTrips()
    {
        var command = new RotateCommand(1, 3, 0.4, -0.2, 0.6);
        Assert.Contains("\"type\":\"Rotate\"", CommandCodec.Encode(command));
        Assert.Equal(command, CommandCodec.Decode(CommandCodec.Encode(command)));
    }

    [Fact]
    public void Decode_Invalid_Throws()
    {
        Assert.Throws<ArgumentException>(() => CommandCodec.Decode(string.Empty));
        Assert.Throws<FormatException>(() => CommandCodec.Decode("{ это не json"));
        Assert.Throws<FormatException>(() => CommandCodec.Decode("{\"type\":\"Unknown\",\"playerId\":1,\"sequence\":0}"));
    }

    [Fact]
    public void Null_Encode_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CommandCodec.Encode(null!));
    }
}
