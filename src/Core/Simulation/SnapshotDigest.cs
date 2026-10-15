using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DarkSpace.Core.Simulation;

/// <summary>Версионированный SHA-256 канонического представления наблюдаемого снимка.</summary>
/// <remarks>
/// Ключи JSON-объектов сортируются ordinal, порядок массивов сохраняется; числа не округляются.
/// Hash чувствителен к точным double и схеме снимка. Это не доказательство детерминизма на всех CPU/ОС
/// и не hash скрытого состояния мира (очереди команд, репутации, конфигурации интегратора).
/// </remarks>
public static class SnapshotDigest
{
    /// <summary>Идентификатор схемы канонизации; изменения формата требуют новой версии и review golden.</summary>
    public const string FormatVersion = "darkspace-snapshot-v1";

    /// <summary>Возвращает SHA-256 в hex нижнего регистра, независимо от культуры и порядка ключей словарей.</summary>
    /// <param name="snapshot">Снимок, не содержащий NaN/Infinity.</param>
    /// <exception cref="ArgumentNullException">Снимок равен null.</exception>
    /// <exception cref="ArgumentException">Снимок содержит NaN/Infinity.</exception>
    public static string Compute(SimulationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Culture = CultureInfo.InvariantCulture,
            TypeNameHandling = TypeNameHandling.None,
        });
        JToken canonical = Canonicalize(JToken.FromObject(snapshot, serializer));
        byte[] bytes = Encoding.UTF8.GetBytes(FormatVersion + "\n" + canonical.ToString(Formatting.None));
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static JToken Canonicalize(JToken token)
    {
        if (token is JObject obj)
        {
            return new JObject(obj.Properties()
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .Select(property => new JProperty(property.Name, Canonicalize(property.Value))));
        }

        if (token is JArray array)
        {
            return new JArray(array.Select(Canonicalize));
        }

        if (token is JValue { Value: double number } && !double.IsFinite(number))
        {
            throw new ArgumentException("Cannot hash a snapshot containing NaN or Infinity.", "snapshot");
        }

        return token.DeepClone();
    }
}
