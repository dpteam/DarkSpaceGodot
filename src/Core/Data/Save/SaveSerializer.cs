using System;
using Newtonsoft.Json;

namespace DarkSpace.Core.Data.Save;

/// <summary>
/// Сериализация и десериализация сохранений в формат JSON (Newtonsoft.Json).
/// </summary>
public static class SaveSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DateParseHandling = DateParseHandling.DateTimeOffset,
        // Не разрешаем сборку произвольных типов из данных: защита от подмены файла сохранения.
        TypeNameHandling = TypeNameHandling.None,
    };

    /// <summary>Сериализует сохранение в строку JSON.</summary>
    /// <exception cref="ArgumentNullException">Если <paramref name="save"/> равен <see langword="null"/>.</exception>
    public static string ToJson(SaveGame save)
    {
        ArgumentNullException.ThrowIfNull(save);

        // Всегда пишем актуальную версию схемы.
        save.Version = SaveGame.CurrentVersion;
        return JsonConvert.SerializeObject(save, Settings);
    }

    /// <summary>
    /// Десериализует сохранение из строки JSON.
    /// </summary>
    /// <returns>Объект сохранения или <see langword="null"/>, если строка пуста.</returns>
    /// <exception cref="ArgumentNullException">Если <paramref name="json"/> равен <see langword="null"/>.</exception>
    /// <exception cref="SaveCorruptedException">Если JSON повреждён или несовместим.</exception>
    public static SaveGame? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            SaveGame? save = JsonConvert.DeserializeObject<SaveGame>(json, Settings);
            return save;
        }
        catch (JsonException ex)
        {
            throw new SaveCorruptedException("Сохранение повреждено и не может быть прочитано.", ex);
        }
    }
}
