namespace DarkSpace.Core.Simulation;

/// <summary>Тип игровой команды (интента), отправляемой клиентом на исполнение.</summary>
public enum CommandType
{
    /// <summary>Задать вектор тяги корабля.</summary>
    Move,

    /// <summary>Назначить целевую планету в текущей системе.</summary>
    SetTarget,

    /// <summary>Стыковаться с целевой планетой (посадка), если корабль в радиусе.</summary>
    Dock,

    /// <summary>Совершить прыжок через чёрную дыру в другую галактику.</summary>
    Jump,

    /// <summary>Купить предмет.</summary>
    BuyItem,

    /// <summary>Продать предмет.</summary>
    SellItem,

    /// <summary>Отправить сообщение в чат.</summary>
    Chat,
}
