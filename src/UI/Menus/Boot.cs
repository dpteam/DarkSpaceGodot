using System.Threading.Tasks;
using DarkSpace.Core.Data;
using DarkSpace.Core.Data.Save;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Alchemy;
using DarkSpace.Core.Systems.Economy;
using DarkSpace.Core.Systems.Factions;
using DarkSpace.Core.Systems.Items;
using DarkSpace.Core.Systems.Travel;
using DarkSpace.Core.Utils.Logging;
using DarkSpace.Gameplay;
using Godot;

namespace DarkSpace.UI.Menus;

/// <summary>
/// Стартовая сцена (boot). Инициализирует подсистемы и показывает диагностику Этапа 1.
/// </summary>
/// <remarks>
/// Служит временным «экраном загрузки»: регистрирует логгер, создаёт игровую сессию,
/// прогоняет генерацию галактики/системы/планеты/корабля, алхимию и цикл сохранения.
/// </remarks>
public partial class Boot : Control
{
    private const ulong DefaultWorldSeed = 0x4F4459414C555441UL; // "ODYALUTA"

    private RichTextLabel _output = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        // Регистрируем приёмник логов — единственное место вывода в консоль движка.
        Log.AddSink(new GodotLogSink());

        _output = GetNode<RichTextLabel>("Output");
        _output.Clear();

        AppendLine("[color=#7CFC00][b]DARK SPACE[/b][/color] — диагностика Этапов 1–2 (Фундамент + Процедурная генерация)");
        AppendLine("------------------------------------------------------------");

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await RunDiagnosticsAsync();
        }
        catch (Exception ex)
        {
            Log.Error("Boot", $"Сбой инициализации: {ex}");
            AppendLine($"[color=red]Сбой инициализации: {ex.Message}[/color]");
        }
    }

    private async Task RunDiagnosticsAsync()
    {
        string saveDirectory = ProjectSettings.GlobalizePath("user://saves");
        var session = new GameSession(DefaultWorldSeed, saveDirectory);
        Log.Info("Boot", $"Сессия запущена. Каталог сохранений: {saveDirectory}");

        // 1) Галактика.
        GalaxyData galaxy = session.GetGalaxy(0);
        AppendLine($"[color=#00E5FF]Галактика #{galaxy.Index}[/color]: «{galaxy.Name}»");
        AppendLine($"  Тип: {galaxy.Type} · Систем: {galaxy.StarCount} · Плотность: {galaxy.Density:0.00} · " +
                   $"Чёрная дыра в ядре: {(galaxy.HasCentralBlackHole ? "да" : "нет")}");

        // 2) Звёздная система и планеты.
        StarSystemData system = StarSystemGenerator.GenerateSystem(galaxy, 0);
        AppendLine($"[color=#00E5FF]Система[/color]: «{system.Name}» · Звезда: {system.Star} · Планет: {system.PlanetCount}");

        for (int i = 0; i < system.PlanetCount; i++)
        {
            PlanetData planet = PlanetGenerator.GeneratePlanet(system, i);
            AppendLine($"  • {planet.Description}");
        }

        // 3) Корабль.
        ShipData ship = ShipGenerator.GenerateShip(galaxy.Seed, ShipType.Courier);
        AppendLine($"[color=#00E5FF]Корабль[/color]: {ship.Description}");

        // 4) Алхимия.
        Element hydrogen = ElementCatalog.FindByStem("Гидро")!;
        Element uranium = ElementCatalog.FindByStem("Урано")!;
        CombinationResult combo = AlchemySystem.Combine(hydrogen, uranium);
        AppendLine($"[color=#00E5FF]Алхимия[/color]: {combo.Name} — {combo.Description}");

        // 5) Расы галактики.
        var races = session.GetGalaxyRaces(galaxy);
        AppendLine($"[color=#00E5FF]Расы[/color] ({races.Count}):");
        foreach (RaceData race in races)
        {
            AppendLine($"  • {race.Name} — {race.Culture}");
        }

        // 6) Поселения (на первой планете с городами).
        AppendLine("[color=#00E5FF]Поселения[/color]:");
        bool anyCity = false;
        for (int i = 0; i < system.PlanetCount && !anyCity; i++)
        {
            PlanetData planet = PlanetGenerator.GeneratePlanet(system, i);
            foreach (CityData city in CityGenerator.GenerateCities(system, planet))
            {
                AppendLine($"  • {city.Description}");
                anyCity = true;
            }
        }

        if (!anyCity)
        {
            AppendLine("  • В этой системе пока никто не живёт. Что, возможно, к лучшему.");
        }

        // 7) Предмет и рынок.
        Item item = ItemGenerator.GenerateItem(galaxy.Seed);
        long neutralPrice = MarketSystem.GetBuyPrice(item, system.Economy, 1.0, 0);
        long honoredPrice = MarketSystem.GetBuyPrice(item, system.Economy, 1.0, 800);
        AppendLine($"[color=#00E5FF]Торговля[/color]: «{item.Name}» ({item.Rarity}) — {neutralPrice} кр. нейтрально / " +
                   $"{honoredPrice} кр. при уважении. {item.Description}");

        // 8) Репутация (реплика NPC при враждебности).
        AppendLine($"[color=#00E5FF]Репутация[/color]: „{ReputationSystem.GetGreeting(system.ControllingFaction, -500)}“");

        // 9) Прыжок через чёрную дыру.
        ShipData jumpShip = session.GetShip(galaxy, ShipType.Cruiser);
        JumpResult jump = BlackHoleTravel.ComputeJump(galaxy.Seed, jumpShip, 1);
        AppendLine($"[color=#00E5FF]Прыжок[/color]: исход «{jump.Outcome}» → галактика #{jump.TargetGalaxyIndex}. {jump.Narrative}");

        // 10) Сохранение и загрузка.
        await DemonstrationSaveLoadAsync(session, galaxy);

        AppendLine("------------------------------------------------------------");
        AppendLine("[color=#7CFC00]Готово. Космос ждёт, пилот.[/color]");
        Log.Info("Boot", "Диагностика Этапов 1–2 завершена успешно.");
    }

    private async Task DemonstrationSaveLoadAsync(GameSession session, GalaxyData galaxy)
    {
        SaveGame save = SaveGame.CreateNew("Пилот без имени", session.WorldSeed);
        save.Metadata.GalaxyIndex = galaxy.Index;
        save.Player.Credits = 1500;
        save.Ship.Name = "«Утренняя Звезда»";

        await session.SaveManager.SaveAsync(save);
        SaveLoadResult result = await session.SaveManager.LoadAsync();

        if (result.Status is SaveLoadStatus.Loaded or SaveLoadStatus.Recovered && result.Save is { } loaded)
        {
            AppendLine($"[color=#00E5FF]Сохранение[/color]: статус «{result.Status}», игрок «{loaded.Metadata.PlayerName}», " +
                       $"кредитов {loaded.Player.Credits:0}.");
        }
        else
        {
            AppendLine($"[color=red]Сохранение: ошибка — {result.Error}[/color]");
            Log.Error("Boot", $"Не удалось загрузить сохранение: {result.Error}");
        }
    }

    private void AppendLine(string text)
    {
        _output.AppendText(text + "\n");
    }
}
