namespace Aegis.Core;

public enum Command
{
    None,
    MoveN, MoveS, MoveW, MoveE, MoveNW, MoveNE, MoveSW, MoveSE,
    Wait,
    Enter,
    Exit,
    Grab,
    Rest,
    Eat,
    Gear,
    Sheet,
    Loose,
    Thrust,
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
        'r' => Command.Rest,
        'e' => Command.Eat,
        'i' => Command.Gear,
        'c' => Command.Sheet,
        'f' => Command.Loose,
        't' => Command.Thrust,
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
