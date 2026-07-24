using SadConsole.Input;
using Aegis.Host;

namespace Aegis.Client;

public static class SadConsoleInputMapper
{
    public static char? Map(Keys key, char character) =>
        Map(key, character, ClientSurface.World);

    public static char? Map(Keys key, char character, ClientSurface surface) => key switch
    {
        Keys.Back when surface == ClientSurface.CreationText => '-',
        Keys.Enter when surface is ClientSurface.CreationText or ClientSurface.CreationReview => '.',
        Keys.Escape when surface is
            ClientSurface.CreationChoice or
            ClientSurface.CreationText or
            ClientSurface.CreationReview => '[',
        Keys.Up => 'k',
        Keys.Down => 'j',
        Keys.Left => 'h',
        Keys.Right => 'l',
        Keys.Escape => 'q',
        _ when character != '\0' => character,
        _ => null,
    };
}
