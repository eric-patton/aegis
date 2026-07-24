using Aegis.Core;

namespace Aegis.Host;

public enum ClientSurface
{
    World,
    DirectionPrompt,
    Menu,
    CreationChoice,
    CreationText,
    CreationReview,
    Character,
    Equipment,
    Conversation,
}

public sealed record ClientAction(
    char Key,
    string Label,
    int X,
    int Y,
    int Width,
    bool Enabled = true);

public sealed record ClientInteractionContext(
    ClientSurface Surface,
    string Title,
    ClientAction[] Actions,
    LogEntry[] Transcript)
{
    public bool SupportsCompass => Surface == ClientSurface.World;
    public bool SupportsActionFocus => Actions.Length > 0;
    public bool IsCreation => Surface is
        ClientSurface.CreationChoice or
        ClientSurface.CreationText or
        ClientSurface.CreationReview;
    public bool IsCreationText => Surface == ClientSurface.CreationText;

    public static ClientInteractionContext From(Game game, Frame frame)
    {
        ClientSurface surface = SurfaceOf(game);
        ClientAction[] actions = surface is ClientSurface.World or ClientSurface.DirectionPrompt
            ? []
            : ScanActions(frame);
        string title = surface switch
        {
            ClientSurface.Conversation when game.TalkNpc is { } npc => $"{npc.Name}, {npc.Role}",
            ClientSurface.Character => "Character",
            ClientSurface.Equipment => "Inventory and equipment",
            ClientSurface.CreationReview => "Review your character",
            ClientSurface.CreationText => "Character creation",
            ClientSurface.CreationChoice => "Character creation",
            _ => "",
        };
        return new ClientInteractionContext(surface, title, actions, [.. game.Log.Entries]);
    }

    private static ClientSurface SurfaceOf(Game game)
    {
        if (game.InCreation)
        {
            return game.CreationStage switch
            {
                CreationStage.Face or CreationStage.Name => ClientSurface.CreationText,
                CreationStage.Review => ClientSurface.CreationReview,
                _ => ClientSurface.CreationChoice,
            };
        }
        if (game.InTalkMenu) return ClientSurface.Conversation;
        if (game.InGearMenu) return ClientSurface.Equipment;
        if (game.InSheetMenu) return ClientSurface.Character;
        if (game.InScene
            || game.InShrineMenu
            || game.InUnbindMenu
            || game.InTradeMenu
            || game.InBonesMenu
            || game.InThresholdMenu
            || game.InLayingMenu
            || game.InCastMenu
            || game.InCrossingMenu)
            return ClientSurface.Menu;
        if (game.InAim || game.InThrust || game.InHeave || game.InCastLine)
            return ClientSurface.DirectionPrompt;
        return ClientSurface.World;
    }

    private static ClientAction[] ScanActions(Frame frame)
    {
        var actions = new List<ClientAction>();
        string[] lines = frame.ToTextLines();
        for (int y = 0; y < lines.Length; y++)
        {
            string line = lines[y];
            int x = -1;
            for (int candidate = 0; candidate + 2 < line.Length; candidate++)
            {
                if (line[candidate] is < '0' or > '9'
                    || line[candidate + 1] != ')'
                    || candidate > 0 && line[candidate - 1] is not ' ' and not '|')
                    continue;
                x = candidate;
                break;
            }
            if (x < 0)
                continue;

            char key = line[x];
            int end = line.IndexOf('|', x + 2);
            if (end < 0) end = line.Length;
            string label = line[(x + 2)..end].Trim();
            if (label.Length == 0) continue;
            bool enabled = frame[x, y].Fg != Hue.DarkGray;
            actions.Add(new ClientAction(key, label, x, y, Math.Max(3, end - x), enabled));
        }
        return [.. actions];
    }
}
