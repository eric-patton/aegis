using SadConsole.Input;

namespace Aegis.Client;

public static class SadConsoleInputMapper
{
    public static char? Map(Keys key, char character) => key switch
    {
        Keys.Up => 'k',
        Keys.Down => 'j',
        Keys.Left => 'h',
        Keys.Right => 'l',
        Keys.Escape => 'q',
        _ when character != '\0' => character,
        _ => null,
    };
}
