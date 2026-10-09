namespace DarkSpace.Core.Data;

/// <summary>Тип галактики. Влияет на распределение и плотность звёздных систем.</summary>
public enum GalaxyType
{
    /// <summary>Спиральная галактика с выраженными рукавами.</summary>
    Spiral,

    /// <summary>Эллиптическая галактика, равномерно заполненная старыми звёздами.</summary>
    Elliptical,

    /// <summary>Неправильная галактика без чёткой структуры.</summary>
    Irregular,

    /// <summary>Карликовая галактика с малым числом систем.</summary>
    Dwarf,

    /// <summary>Кольцеобразная галактика с полым ядром.</summary>
    Ring,

    /// <summary>Линзовидная галактика — переходная форма между спиральной и эллиптической.</summary>
    Lenticular,

    /// <summary>Пекулярная галактика с аномальной структурой (результат столкновений и катаклизмов).</summary>
    Peculiar,
}

/// <summary>Спектральный класс звезды либо её экзотический тип.</summary>
public enum SpectralClass
{
    /// <summary>Голубой сверхгигант, самый горячий класс.</summary>
    O,

    /// <summary>Бело-голубая звезда высокой светимости.</summary>
    B,

    /// <summary>Белая звезда.</summary>
    A,

    /// <summary>Жёлто-белая звезда.</summary>
    F,

    /// <summary>Жёлтый карлик, аналог Солнца.</summary>
    G,

    /// <summary>Оранжевый карлик.</summary>
    K,

    /// <summary>Красный карлик, наиболее распространённый класс.</summary>
    M,

    /// <summary>Нейтронная звезда — сверхплотный остаток сверхновой.</summary>
    NeutronStar,

    /// <summary>Чёрная дыра. Позволяет совершить межгалактический прыжок с риском.</summary>
    BlackHole,

    /// <summary>Двойная (кратная) звёздная система.</summary>
    Binary,
}

/// <summary>Тип планеты.</summary>
public enum PlanetType
{
    /// <summary>Каменистая планета.</summary>
    Rocky,

    /// <summary>Газовый гигант.</summary>
    GasGiant,

    /// <summary>Ледяная планета.</summary>
    Ice,

    /// <summary>Океаническая планета.</summary>
    Oceanic,

    /// <summary>Вулканическая планета.</summary>
    Volcanic,

    /// <summary>Токсичная планета с агрессивной атмосферой.</summary>
    Toxic,

    /// <summary>Радиоактивная планета.</summary>
    Radioactive,

    /// <summary>Пустынная планета.</summary>
    Desert,

    /// <summary>Джунглевая планета.</summary>
    Jungle,

    /// <summary>Экзотическая планета неизвестной природы.</summary>
    Exotic,
}

/// <summary>Тип экономики звёздной системы или поселения.</summary>
public enum EconomyType
{
    /// <summary>Аграрная экономика.</summary>
    Agricultural,

    /// <summary>Индустриальная экономика.</summary>
    Industrial,

    /// <summary>Технократическая экономика.</summary>
    Technocratic,

    /// <summary>Религиозная экономика.</summary>
    Religious,

    /// <summary>Военная экономика.</summary>
    Military,

    /// <summary>Пиратская экономика.</summary>
    Pirate,
}

/// <summary>Крупная фракция игрового мира.</summary>
public enum FactionType
{
    /// <summary>«Империя Зари» — милитаристы, порядок и экспансия.</summary>
    EmpireOfDawn,

    /// <summary>«Синдикат Пустоты» — торговцы и контрабандисты.</summary>
    VoidSyndicate,

    /// <summary>«Орден Хранителей» — религиозные фанатики.</summary>
    OrderOfKeepers,

    /// <summary>«Свободные Кланы» — анархисты.</summary>
    FreeClans,
}

/// <summary>Класс корабля.</summary>
public enum ShipType
{
    /// <summary>Разведчик — быстрый и хрупкий.</summary>
    Scout,

    /// <summary>Торговец — большой трюм, слабое вооружение.</summary>
    Trader,

    /// <summary>Фрегат — сбалансированный боевой корабль.</summary>
    Frigate,

    /// <summary>Крейсер — тяжёлый боевой корабль.</summary>
    Cruiser,

    /// <summary>Линкор — максимальная огневая мощь и броня.</summary>
    Battleship,

    /// <summary>Шахтёр — буровые модули и большой трюм для руды.</summary>
    Miner,

    /// <summary>Курьер — максимальная скорость и малая грузоподъёмность.</summary>
    Courier,

    /// <summary>Научное судно — сканеры и исследовательское оборудование.</summary>
    Science,
}

/// <summary>Категория предмета.</summary>
public enum ItemCategory
{
    /// <summary>Оружие.</summary>
    Weapon,

    /// <summary>Броня.</summary>
    Armor,

    /// <summary>Модуль корабля.</summary>
    ShipModule,

    /// <summary>Расходный предмет.</summary>
    Consumable,

    /// <summary>Квестовый предмет.</summary>
    QuestItem,

    /// <summary>Реликвия.</summary>
    Relic,
}

/// <summary>Игровая валюта.</summary>
public enum CurrencyType
{
    /// <summary>Кредиты — основная валюта.</summary>
    Credits,

    /// <summary>Реликвии — выдаются за религиозные квесты.</summary>
    Relics,

    /// <summary>Тёмные кристаллы — добываются в опасных зонах.</summary>
    DarkCrystals,
}
