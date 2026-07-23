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
    Burgle,
    Rest,
    Read,
    Eat,
    Drink,
    Gear,
    Sheet,
    Loose,
    Thrust,
    Heave,
    Cast,
    Stance,
    Parry,
    Order,
    Camp,
    Help,
    Quit,
    RushN, RushS, RushW, RushE, RushNW, RushNE, RushSW, RushSE,
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
        'H' => Command.RushW,
        'J' => Command.RushS,
        'K' => Command.RushN,
        'L' => Command.RushE,
        'Y' => Command.RushNW,
        'U' => Command.RushNE,
        'B' => Command.RushSW,
        'N' => Command.RushSE,
        '.' => Command.Wait,
        '>' => Command.Enter,
        '<' => Command.Exit,
        'g' => Command.Grab,
        'p' => Command.Lift,
        's' => Command.Burgle,
        'r' => Command.Rest,
        'v' => Command.Read,
        'e' => Command.Eat,
        'd' => Command.Drink,
        'i' => Command.Gear,
        'c' => Command.Sheet,
        'f' => Command.Loose,
        't' => Command.Thrust,
        'w' => Command.Heave,
        'z' => Command.Cast,
        'x' => Command.Stance,
        'a' => Command.Parry,
        'o' => Command.Order,
        'm' => Command.Camp,
        '?' => Command.Help,
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
        Command.RushN => (0, -1),
        Command.RushS => (0, 1),
        Command.RushW => (-1, 0),
        Command.RushE => (1, 0),
        Command.RushNW => (-1, -1),
        Command.RushNE => (1, -1),
        Command.RushSW => (-1, 1),
        Command.RushSE => (1, 1),
        _ => null,
    };

    public static bool IsRush(Command cmd) => cmd is
        Command.RushN or Command.RushS or Command.RushW or Command.RushE
        or Command.RushNW or Command.RushNE or Command.RushSW or Command.RushSE;
}
