namespace Aegis.Core;

public enum Command
{
    None,
    MoveN, MoveS, MoveW, MoveE, MoveNW, MoveNE, MoveSW, MoveSE,
    Wait,
    Enter,
    Exit,
    Grab,
    Lift,
    Rest,
    Eat,
    Drink,
    Gear,
    Sheet,
    Loose,
    Thrust,
    Heave,
    Cast,
    Stance,
    Order,
    Quit,
}

public static class CommandMap
{
    /// <summary>Single canonical key binding, shared by TUI, pilot, and sim (one input language everywhere).</summary>
    public static Command FromKey(char key) => key switch
    {
        'h' => Command.MoveW,
        'j' => Command.MoveS,
        'k' => Command.MoveN,
        'l' => Command.MoveE,
        'y' => Command.MoveNW,
        'u' => Command.MoveNE,
        'b' => Command.MoveSW,
        'n' => Command.MoveSE,
        '.' => Command.Wait,
        '>' => Command.Enter,
        '<' => Command.Exit,
        'g' => Command.Grab,
        'p' => Command.Lift,
        'r' => Command.Rest,
        'e' => Command.Eat,
        'd' => Command.Drink,
        'i' => Command.Gear,
        'c' => Command.Sheet,
        'f' => Command.Loose,
        't' => Command.Thrust,
        'w' => Command.Heave,
        'z' => Command.Cast,
        'x' => Command.Stance,
        'o' => Command.Order,
        'q' => Command.Quit,
        _ => Command.None,
    };

    public static (int dx, int dy)? Delta(Command cmd) => cmd switch
    {
        Command.MoveN => (0, -1),
        Command.MoveS => (0, 1),
        Command.MoveW => (-1, 0),
        Command.MoveE => (1, 0),
        Command.MoveNW => (-1, -1),
        Command.MoveNE => (1, -1),
        Command.MoveSW => (-1, 1),
        Command.MoveSE => (1, 1),
        _ => null,
    };
}
