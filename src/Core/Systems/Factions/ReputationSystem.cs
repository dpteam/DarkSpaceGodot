using System;
using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Systems.Factions;

/// <summary>
/// Система репутации: границы, уровни отношений, влияние на цены, квесты и реплики NPC.
/// </summary>
/// <remarks>
/// Репутация измеряется в диапазоне [<see cref="Min"/>; <see cref="Max"/>] (-1000..+1000)
/// и хранится отдельно по каждой фракции (см. <see cref="DarkSpace.Core.Data.Save.PlayerState.Reputation"/>).
/// </remarks>
public static class ReputationSystem
{
    /// <summary>Минимальная репутация.</summary>
    public const int Min = -1000;

    /// <summary>Максимальная репутация.</summary>
    public const int Max = 1000;

    /// <summary>Максимальный размер скидки/наценки на цену покупки (доля от базовой цены).</summary>
    private const double PriceSwing = 0.20;

    /// <summary>Ограничивает значение репутации допустимым диапазоном.</summary>
    public static int Clamp(int reputation) => Math.Clamp(reputation, Min, Max);

    /// <summary>Прибавляет дельту к репутации с ограничением диапазона.</summary>
    public static int Add(int reputation, int delta) => Clamp(reputation + delta);

    /// <summary>Возвращает уровень отношения для значения репутации.</summary>
    public static ReputationLevel Level(int reputation)
    {
        int value = Clamp(reputation);
        return value switch
        {
            <= -600 => ReputationLevel.Hated,
            <= -200 => ReputationLevel.Hostile,
            < 0 => ReputationLevel.Unfriendly,
            < 200 => ReputationLevel.Neutral,
            < 600 => ReputationLevel.Friendly,
            _ => ReputationLevel.Honored,
        };
    }

    /// <summary>
    /// Множитель цены покупки: хорошая репутация удешевляет, плохая — удорожает.
    /// </summary>
    /// <returns>Значение от 0.80 (максимум) до 1.20 (минимум).</returns>
    public static double BuyPriceModifier(int reputation)
    {
        double normalized = Clamp(reputation) / (double)Max; // -1..+1
        return 1.0 - (PriceSwing * normalized);
    }

    /// <summary>
    /// Множитель цены продажи: хорошая репутация повышает выкуп, плохая — снижает.
    /// </summary>
    /// <returns>Значение от 1.20 (максимум) до 0.80 (минимум).</returns>
    public static double SellPriceModifier(int reputation)
    {
        double normalized = Clamp(reputation) / (double)Max;
        return 1.0 + (PriceSwing * normalized);
    }

    /// <summary>Проверяет, доступен ли квест с требуемой репутацией.</summary>
    /// <param name="reputation">Текущая репутация.</param>
    /// <param name="requiredReputation">Минимально требуемая репутация.</param>
    public static bool IsQuestAvailable(int reputation, int requiredReputation)
        => Clamp(reputation) >= requiredReputation;

    /// <summary>
    /// Возвращает детерминированную реплику NPC в зависимости от фракции и репутации.
    /// </summary>
    /// <param name="faction">Фракция NPC.</param>
    /// <param name="reputation">Репутация игрока у этой фракции.</param>
    public static string GetGreeting(FactionType faction, int reputation)
    {
        ReputationLevel level = Level(reputation);
        var random = new DeterministicRandom(SeedHelper.Combine((ulong)faction, "greet", (ulong)Clamp(reputation)));

        // Для враждебных уровней есть фракционные реплики; в остальных — общие.
        if (level is ReputationLevel.Hated or ReputationLevel.Hostile
            && FactionGreetings.TryGetValue(faction, out var lines))
        {
            return random.Pick(lines);
        }

        return random.Pick(GenericGreetings[level]);
    }

    private static readonly Dictionary<ReputationLevel, string[]> GenericGreetings = new()
    {
        [ReputationLevel.Hated] = new[]
        {
            "Убирайся, пока я не вспомнил, что у меня есть оружие больше твоего.",
            "Мы уже отправили жалобу на тебя. В трёх экземплярах. С приложением твоих грехов.",
        },
        [ReputationLevel.Hostile] = new[]
        {
            "О, это ты. Я уже вызвал полицию, но они сказали, что приедут только после обеда. Так что у тебя есть 20 минут.",
            "Держись подальше. У нас тут и без тебя список врагов недельный.",
        },
        [ReputationLevel.Unfriendly] = new[]
        {
            "Чего надо? Только быстро, у меня обед.",
            "Я тебя не знаю. И, честно говоря, не стремлюсь.",
        },
        [ReputationLevel.Neutral] = new[]
        {
            "Приветствую. Товар — в наличии, вежливость — по тарифу.",
            "Проходи. Но руками ничего не трогай, они не сертифицированы.",
        },
        [ReputationLevel.Friendly] = new[]
        {
            "Рад тебя видеть! Придержи-ка вот эту скидку, только никому.",
            "Для тебя — по себестоимости. Почти. Ну, с небольшой наценкой на дружбу.",
        },
        [ReputationLevel.Honored] = new[]
        {
            "Наш человек! Бери что хочешь, оформлю как «гуманитарную помощь».",
            "Для тебя вход открыт. Пароль прежний: «не спрашивать про отчётность».",
        },
    };

    private static readonly Dictionary<FactionType, string[]> FactionGreetings = new()
    {
        [FactionType.EmpireOfDawn] = new[]
        {
            "Вы арестованы за нарушение общественного спокойствия. Ваши права будут предоставлены по запросу. В трёх экземплярах.",
            "Империя не забывает. Империя просто ведёт список. Вы в нём, кстати, выделены маркером.",
        },
        [FactionType.VoidSyndicate] = new[]
        {
            "Твой долг перед Синдикатом растёт быстрее ВВП. Это, знаешь ли, талант.",
            "Мы всё посчитали. Ты должен нам столько, что проще расстаться с кораблём. И с почками.",
        },
        [FactionType.OrderOfKeepers] = new[]
        {
            "Ты не читал Мануалы и, хуже того, ты их не читал с удовольствием. Прочь.",
            "Ступай с миром. Желательно — далёко отсюда и во тьму.",
        },
        [FactionType.FreeClans] = new[]
        {
            "У нас тут демократия: один корабль — один голос. Я — корабль. Голосуй как я.",
            "Кланы тебя не любят. Кланы, впрочем, никого не любят. Ты просто в этом списке повыше.",
        },
    };
}
