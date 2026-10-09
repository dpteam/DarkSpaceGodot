namespace DarkSpace.Core.Systems.Alchemy;

/// <summary>
/// Результат алхимической комбинации двух элементов.
/// </summary>
/// <param name="Name">Сгенерированное имя нового ресурса.</param>
/// <param name="Description">Описание эффекта с элементами чёрного юмора.</param>
/// <param name="Value">Ориентировочная рыночная стоимость в кредитах.</param>
/// <param name="IsDangerous">Опасен ли ресурс для обращения без защитного снаряжения.</param>
/// <param name="IsNovel">Является ли комбинация новым соединением (а не <c>null</c>-результатом).</param>
/// <param name="FirstElementId">Идентификатор первого (по возрастанию id) элемента.</param>
/// <param name="SecondElementId">Идентификатор второго (по возрастанию id) элемента.</param>
public sealed record CombinationResult(
    string Name,
    string Description,
    double Value,
    bool IsDangerous,
    bool IsNovel,
    int FirstElementId,
    int SecondElementId);
