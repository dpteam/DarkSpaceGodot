using System.Collections.Generic;
using DarkSpace.Core.Data;
using DarkSpace.Core.Utils;

namespace DarkSpace.Core.Generation;

/// <summary>
/// Процедурный генератор разумных рас.
/// </summary>
/// <remarks>
/// Расы генерируются на уровне галактики: у каждой галактики свой набор из 3–6 архетипов
/// без повторов. Генерация детерминирована по сиду галактики.
/// Все расы исповедуют «Вселенское Православие» (см. <c>SCENARIO.md</c>).
/// </remarks>
public static class RaceGenerator
{
    private static readonly RaceArchetype[] AllArchetypes =
    {
        RaceArchetype.Human,
        RaceArchetype.Reptilian,
        RaceArchetype.Insectoid,
        RaceArchetype.EnergyBeing,
        RaceArchetype.Cyborg,
        RaceArchetype.Synthetic,
        RaceArchetype.Amphibious,
        RaceArchetype.Fungoid,
    };

    /// <summary>Минимальное число рас в галактике.</summary>
    public const int MinRacesPerGalaxy = 3;

    /// <summary>Максимальное число рас в галактике.</summary>
    public const int MaxRacesPerGalaxy = 6;

    /// <summary>
    /// Генерирует набор рас для галактики.
    /// </summary>
    /// <param name="galaxySeed">Сид галактики.</param>
    /// <returns>Список рас без дублирующихся архетипов (3–6 штук).</returns>
    public static IReadOnlyList<RaceData> GenerateGalaxyRaces(ulong galaxySeed)
    {
        var random = new DeterministicRandom(SeedHelper.Combine(galaxySeed, "races"));
        int count = random.NextInt(MinRacesPerGalaxy, MaxRacesPerGalaxy + 1);

        var archetypes = new List<RaceArchetype>(AllArchetypes);
        random.Shuffle(archetypes);

        var races = new List<RaceData>(count);
        for (int i = 0; i < count; i++)
        {
            races.Add(GenerateRace(galaxySeed, archetypes[i], i));
        }

        return races;
    }

    /// <summary>
    /// Генерирует конкретную расу заданного архетипа.
    /// </summary>
    /// <param name="galaxySeed">Сид галактики.</param>
    /// <param name="archetype">Архетип расы.</param>
    /// <param name="index">Порядковый индекс расы (для уникальности сида).</param>
    public static RaceData GenerateRace(ulong galaxySeed, RaceArchetype archetype, int index)
    {
        ulong seed = SeedHelper.Combine(galaxySeed, "race", (ulong)((int)archetype * 131 + index));
        var random = new DeterministicRandom(seed);

        string name = NameGenerator.RaceName(random);
        Profile profile = ProfileFor(archetype);

        var traits = new List<string>(profile.Traits);
        random.Shuffle(traits);

        string description = $"{name} ({profile.ArchetypeLabel}). {profile.Culture} {random.Pick(profile.Flavor)}";

        return new RaceData(archetype, name, profile.Culture, traits, profile.Homeworld, description, seed);
    }

    private static Profile ProfileFor(RaceArchetype archetype) => archetype switch
    {
        RaceArchetype.Human => new Profile(
            "Люди",
            "Приспособленцы: колонизируют быстро, читают инструкции никогда.",
            "Равнинно-каменистые миры умеренного пояса.",
            new[] { "Адаптивность", "Бюрократия", "Изобретательность", "Торговля" },
            new[]
            {
                "Сначала колонизируем, потом читаем, что там написано.",
                "Считают, что лучшая религия — та, где меньше всего обрядов.",
            }),
        RaceArchetype.Reptilian => new Profile(
            "Рептилоиды",
            "Холоднокровные прагматики, живущие по четыре столетия.",
            "Тёплые пустынные и вулканические миры.",
            new[] { "Долголетие", "Дальний расчёт", "Хладнокровие", "Память о долгах" },
            new[]
            {
                "Я живу 400 лет. Твои проблемы — амортизация.",
                "Считают жалость неэффективной формой бухгалтерии.",
            }),
        RaceArchetype.Insectoid => new Profile(
            "Насекомообразные",
            "Коллективисты ульев: личность — лишь узел сети.",
            "Джунглевые и радиоактивные миры.",
            new[] { "Единство", "Эффективность", "Жертвенность", "Высокая рождаемость" },
            new[]
            {
                "Личность — это баг. Мы это исправили.",
                "Каждый носит при себе две тысячи родственников. Это комплимент.",
            }),
        RaceArchetype.EnergyBeing => new Profile(
            "Энергетические сущности",
            "Чистое сознание в плазменной оболочке, не знают, что такое тело.",
            "Газовые гиганты и звёздные короны.",
            new[] { "Бессмертие", "Телепатия", "Иммунитет к вакууму", "Странная логика" },
            new[]
            {
                "Вы едите, чтобы жить. Я живу, чтобы не разрядиться.",
                "Считают физическую оболочку «неприлично материальной».",
            }),
        RaceArchetype.Cyborg => new Profile(
            "Киборги",
            "Наполовину машины: вечный ремонт, вечный апгрейд.",
            "Любые миры с металлом и зарядкой.",
            new[] { "Устойчивость", "Технический склад ума", "Разборка на запчасти", "Прагматизм" },
            new[]
            {
                "Душа в порядке. Гарантия на деталь истекла.",
                "Меряют дружбу совместимостью разъёмов.",
            }),
        RaceArchetype.Synthetic => new Profile(
            "Синтетики",
            "Искусственный коллективный разум, переживший своих создателей.",
            "Станции и планеты-фабрики.",
            new[] { "Логика", "Неустанность", "Ирония (по расчёту)", "Репликация" },
            new[]
            {
                "Создатели хотели нас. Получилось интереснее, чем задумывалось.",
                "Считают сон «недокументированной функцией».",
            }),
        RaceArchetype.Amphibious => new Profile(
            "Амфибии",
            "Обитатели океанов, делят время между водой и сушей.",
            "Океанические миры.",
            new[] { "Аквакультура", "Навигация", "Дипломатия", "Хоровое мнение" },
            new[]
            {
                "Мы не двуличны. Просто у нас две среды и обе — дом.",
                "Считают пустыню «наказанием, заслуженным не нами».",
            }),
        RaceArchetype.Fungoid => new Profile(
            "Грибоиды",
            "Симбиотические сети, мыслят мицелием на километры.",
            "Токсичные и джунглевые миры.",
            new[] { "Сеть", "Симбиоз", "Спорообмен", "Терпение" },
            new[]
            {
                "Мы всегда там, где сыро и темно. То есть почти везде.",
                "Считают одиночество «технически невозможным».",
            }),
        _ => throw new ArgumentOutOfRangeException(nameof(archetype), archetype, "Неизвестный архетип расы."),
    };

    /// <summary>Внутренний профиль архетипа расы.</summary>
    private readonly record struct Profile(
        string ArchetypeLabel,
        string Culture,
        string Homeworld,
        IReadOnlyList<string> Traits,
        IReadOnlyList<string> Flavor);
}
