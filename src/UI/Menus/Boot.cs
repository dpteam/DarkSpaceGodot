using System.Text;
using System.Threading.Tasks;
using DarkSpace.Core.Data;
using DarkSpace.Core.Data.Save;
using DarkSpace.Core.Generation;
using DarkSpace.Core.Systems.Alchemy;
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

        AppendLine("[color=#7CFC00][b]DARK SPACE[/b][/color] — диагностика Этапа 1 (Фундамент)");
        AppendLine("------------------------------------------------------------");

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
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

        // 5) Сохранение и загрузка.
        await DemonstrationSaveLoadAsync(session, galaxy);

        AppendLine("------------------------------------------------------------");
        AppendLine("[color=#7CFC00]Готово. Космос ждёт, пилот.[/color]");
        Log.Info("Boot", "Диагностика Этапа 1 завершена успешно.");
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
