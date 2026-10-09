using System.Collections.Generic;

namespace DarkSpace.Core.Utils;

/// <summary>
/// Детерминированный генератор псевдослучайных чисел на базе алгоритма SplitMix64.
/// </summary>
/// <remarks>
/// ВАЖНО: во всех процедурных системах проекта запрещено использовать
/// <see cref="System.Random"/> напрямую. Результат генерации обязан воспроизводиться
/// по сиду независимо от версии рантайма, платформы и порядка вызовов.
/// Экземпляр класса не является потокобезопасным: для параллельной генерации создавайте
/// по отдельному экземпляру на поток, производному от того же корневого сида.
/// </remarks>
public sealed class DeterministicRandom
{
    private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;
    private const ulong Mix1 = 0xBF58476D1CE4E5B9UL;
    private const ulong Mix2 = 0x94D049BB133111EBUL;

    /// <summary>Максимальное значение для приведения 64-битного числа к диапазону [0,1).</summary>
    private const double UInt64ToDoubleScale = 1.0 / 18446744073709551616.0; // 1 / 2^64

    private ulong _state;
    private double? _spareGaussian;

    /// <summary>
    /// Создаёт генератор с заданным корневым сидом.
    /// </summary>
    /// <param name="seed">Корневой сид. Одинаковый сид всегда даёт одинаковую последовательность.</param>
    public DeterministicRandom(ulong seed)
    {
        _state = seed;
    }

    /// <summary>Создаёт генератор, производный от родительского сида и соли (тега подсистемы).</summary>
    /// <param name="parentSeed">Родительский сид.</param>
    /// <param name="salt">Соль/тег, уникальный для подсистемы (например, индекс галактики).</param>
    public DeterministicRandom(ulong parentSeed, ulong salt)
        : this(SeedHelper.Combine(parentSeed, salt))
    {
    }

    /// <summary>Возвращает текущее внутреннее состояние (для сохранения/восстановления).</summary>
    public ulong State => _state;

    /// <summary>Возвращает следующее 64-битное число.</summary>
    public ulong NextUInt64()
    {
        ulong z = _state += GoldenGamma;
        z = (z ^ (z >> 30)) * Mix1;
        z = (z ^ (z >> 27)) * Mix2;
        return z ^ (z >> 31);
    }

    /// <summary>Возвращает следующее 32-битное число без знака.</summary>
    public uint NextUInt32() => (uint)(NextUInt64() >> 32);

    /// <summary>Возвращает следующее целое в диапазоне [<paramref name="minInclusive"/>; <paramref name="maxExclusive"/>).</summary>
    /// <exception cref="ArgumentException">Если <paramref name="maxExclusive"/> меньше или равен <paramref name="minInclusive"/>.</exception>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentException(
                $"Недопустимый диапазон: [{minInclusive}; {maxExclusive}).",
                nameof(maxExclusive));
        }

        ulong range = (ulong)(maxExclusive - minInclusive);
        return (int)(minInclusive + (long)(NextUInt64() % range));
    }

    /// <summary>Возвращает следующее целое в диапазоне [0; <paramref name="maxExclusive"/>).</summary>
    public int NextInt(int maxExclusive) => NextInt(0, maxExclusive);

    /// <summary>Возвращает следующее длинное целое в диапазоне [<paramref name="minInclusive"/>; <paramref name="maxExclusive"/>).</summary>
    public long NextLong(long minInclusive, long maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentException(
                $"Недопустимый диапазон: [{minInclusive}; {maxExclusive}).",
                nameof(maxExclusive));
        }

        ulong range = (ulong)(maxExclusive - minInclusive);
        return minInclusive + (long)(NextUInt64() % range);
    }

    /// <summary>Возвращает вещественное число в диапазоне [0.0; 1.0).</summary>
    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / 9007199254740992.0); // /2^53

    /// <summary>Возвращает вещественное число в диапазоне [<paramref name="min"/>; <paramref name="max"/>).</summary>
    public double NextDouble(double min, double max) => min + (NextDouble() * (max - min));

    /// <summary>Возвращает число с плавающей точкой в диапазоне [0.0f; 1.0f).</summary>
    public float NextFloat() => (float)NextDouble();

    /// <summary>Возвращает число с плавающей точкой в диапазоне [<paramref name="min"/>; <paramref name="max"/>).</summary>
    public float NextFloat(float min, float max) => min + (NextFloat() * (max - min));

    /// <summary>Возвращает <see langword="true"/> с заданной вероятностью.</summary>
    /// <param name="probability">Вероятность истины, по умолчанию 0.5.</param>
    public bool NextBool(double probability = 0.5) => NextDouble() < probability;

    /// <summary>Случайным образом выбирает один элемент из списка.</summary>
    /// <exception cref="ArgumentException">Если список пуст.</exception>
    public T Pick<T>(IReadOnlyList<T> items)
    {
        if (items.Count == 0)
        {
            throw new ArgumentException("Список не должен быть пустым.", nameof(items));
        }

        return items[NextInt(items.Count)];
    }

    /// <summary>Возвращает нормально распределённое число (μ = 0, σ = 1) методом Бокса — Мюллера.</summary>
    public double NextGaussian()
    {
        if (_spareGaussian is { } spare)
        {
            _spareGaussian = null;
            return spare;
        }

        double u1 = 1.0 - NextDouble(); // (0,1], чтобы избежать log(0)
        double u2 = NextDouble();
        double magnitude = Math.Sqrt(-2.0 * Math.Log(u1));
        double angle = 2.0 * Math.PI * u2;
        _spareGaussian = magnitude * Math.Sin(angle);
        return magnitude * Math.Cos(angle);
    }

    /// <summary>Возвращает нормально распределённое число с заданными средним и стандартным отклонением.</summary>
    public double NextGaussian(double mean, double standardDeviation)
        => mean + (NextGaussian() * standardDeviation);

    /// <summary>Перемешивает список на месте (алгоритм Фишера — Йетса).</summary>
    public void Shuffle<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = NextInt(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
