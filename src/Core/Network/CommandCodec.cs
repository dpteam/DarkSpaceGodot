using System;
using DarkSpace.Core.Simulation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DarkSpace.Core.Network;

/// <summary>
/// Кодек игровых команд: сериализует полиморфные <see cref="IGameCommand"/> в JSON с явным дискриминатором.
/// </summary>
/// <remarks>
/// В отличие от <c>TypeNameHandling</c>, дискриминатор — собственное поле <c>type</c>,
/// сопоставляемое с <see cref="CommandType"/>. Это безопаснее (нет сборки произвольных типов
/// из данных) и явно управляемо.
/// </remarks>
public static class CommandCodec
{
    private const string TypeField = "type";
    private const string PlayerField = "playerId";
    private const string SequenceField = "sequence";

    /// <summary>Кодирует команду в JSON-строку.</summary>
    /// <exception cref="ArgumentNullException">Если <paramref name="command"/> равен <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">Если тип команды неизвестен.</exception>
    public static string Encode(IGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var obj = new JObject
        {
            [TypeField] = command.Type.ToString(),
            [PlayerField] = command.PlayerId,
            [SequenceField] = command.Sequence,
        };

        switch (command)
        {
            case MoveCommand move:
                obj["thrustX"] = move.ThrustX;
                obj["thrustY"] = move.ThrustY;
                break;
            case JumpCommand jump:
                obj["targetGalaxy"] = jump.TargetGalaxyIndex;
                break;
            case BuyItemCommand buy:
                obj["itemId"] = buy.ItemId;
                obj["quantity"] = buy.Quantity;
                break;
            case SellItemCommand sell:
                obj["itemId"] = sell.ItemId;
                obj["quantity"] = sell.Quantity;
                break;
            case ChatCommand chat:
                obj["text"] = chat.Text;
                break;
            default:
                throw new NotSupportedException($"Неизвестный тип команды: {command.GetType().Name}.");
        }

        return obj.ToString(Formatting.None);
    }

    /// <summary>Декодирует команду из JSON-строки.</summary>
    /// <exception cref="ArgumentException">Если строка пуста.</exception>
    /// <exception cref="FormatException">Если строка некорректна.</exception>
    public static IGameCommand Decode(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        JObject obj;
        try
        {
            obj = JObject.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException("Некорректная команда.", ex);
        }

        try
        {
            var type = Enum.Parse<CommandType>((string?)obj[TypeField] ?? throw new FormatException("Нет поля type."));
            int playerId = (int?)obj[PlayerField] ?? throw new FormatException("Нет поля playerId.");
            long sequence = (long?)obj[SequenceField] ?? throw new FormatException("Нет поля sequence.");

            return type switch
            {
                CommandType.Move => new MoveCommand(playerId, sequence, (double?)obj["thrustX"] ?? 0.0, (double?)obj["thrustY"] ?? 0.0),
                CommandType.Jump => new JumpCommand(playerId, sequence, (int?)obj["targetGalaxy"] ?? 0),
                CommandType.BuyItem => new BuyItemCommand(playerId, sequence, (int?)obj["itemId"] ?? -1, (int?)obj["quantity"] ?? 0),
                CommandType.SellItem => new SellItemCommand(playerId, sequence, (int?)obj["itemId"] ?? -1, (int?)obj["quantity"] ?? 0),
                CommandType.Chat => new ChatCommand(playerId, sequence, (string?)obj["text"] ?? string.Empty),
                _ => throw new FormatException($"Неизвестный тип команды: {type}."),
            };
        }
        catch (FormatException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidCastException or ArgumentException or FormatException)
        {
            throw new FormatException("Не удалось разобрать команду.", ex);
        }
    }
}
