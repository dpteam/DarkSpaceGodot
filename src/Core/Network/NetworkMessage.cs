namespace DarkSpace.Core.Network;

/// <summary>
/// Сетевое сообщение — единица обмена между клиентом и сервером.
/// </summary>
/// <remarks>
/// Полезная нагрузка (<paramref name="Payload"/>) — строка, обычно JSON, закодированная
/// <see cref="CommandCodec"/> (для команд) или <see cref="MessageCodec"/> (для снапшотов).
/// Такая схема не зависит от конкретного транспорта: реализация (loopback, ENet, WebSocket)
/// лишь доставляет <see cref="NetworkMessage"/> целиком.
/// </remarks>
/// <param name="Type">Тип сообщения.</param>
/// <param name="SenderId">Идентификатор отправителя.</param>
/// <param name="Sequence">Порядковый номер сообщения у отправителя.</param>
/// <param name="Payload">Полезная нагрузка (JSON).</param>
public sealed record NetworkMessage(NetMessageType Type, int SenderId, long Sequence, string Payload);
