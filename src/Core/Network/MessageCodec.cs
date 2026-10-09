using System;
using Newtonsoft.Json;

namespace DarkSpace.Core.Network;

/// <summary>
/// Кодек сетевых сообщений и их полезной нагрузки (JSON, Newtonsoft.Json).
/// </summary>
/// <remarks>
/// <see cref="TypeNameHandling.None"/> выбран осознанно (защита от подмены типов в полезной
/// нагрузке). Для полиморфных команд используется явный дискриминатор в <see cref="CommandCodec"/>.
/// </remarks>
public static class MessageCodec
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Ignore,
        TypeNameHandling = TypeNameHandling.None,
    };

    /// <summary>Кодирует сообщение в строку.</summary>
    /// <exception cref="ArgumentNullException">Если <paramref name="message"/> равен <see langword="null"/>.</exception>
    public static string Encode(NetworkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return JsonConvert.SerializeObject(message, Settings);
    }

    /// <summary>Декодирует сообщение из строки.</summary>
    /// <exception cref="ArgumentException">Если строка пуста.</exception>
    /// <exception cref="FormatException">Если строка не является корректным сообщением.</exception>
    public static NetworkMessage Decode(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            NetworkMessage? message = JsonConvert.DeserializeObject<NetworkMessage>(json, Settings);
            return message ?? throw new FormatException("Пустое сетевое сообщение.");
        }
        catch (JsonException ex)
        {
            throw new FormatException("Некорректное сетевое сообщение.", ex);
        }
    }

    /// <summary>Кодирует произвольную полезную нагрузку в JSON-строку.</summary>
    /// <typeparam name="T">Тип полезной нагрузки.</typeparam>
    /// <exception cref="ArgumentNullException">Если <paramref name="value"/> равен <see langword="null"/>.</exception>
    public static string EncodePayload<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonConvert.SerializeObject(value, Settings);
    }

    /// <summary>Декодирует полезную нагрузку из JSON-строки.</summary>
    /// <typeparam name="T">Тип полезной нагрузки.</typeparam>
    /// <returns>Декодированное значение или <see langword="default"/>, если строка пуста.</returns>
    /// <exception cref="FormatException">Если строка не является корректным JSON для типа <typeparamref name="T"/>.</exception>
    public static T? DecodePayload<T>(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return default;
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Некорректная полезная нагрузка типа {typeof(T).Name}.", ex);
        }
    }
}
