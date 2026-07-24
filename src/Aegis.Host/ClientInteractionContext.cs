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
    public string Prompt { get; init; } = "";
    public string Detail { get; init; } = "";
    public int? ProgressStep { get; init; }
    public int? ProgressTotal { get; init; }

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
        ClientAction[] actions = surface switch
        {
            ClientSurface.World or ClientSurface.DirectionPrompt => [],
            ClientSurface.Conversation => ConversationActions(game),
            _ => ScanActions(frame),
        };
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
        var context = new ClientInteractionContext(surface, title, actions, [.. game.Log.Entries]);
        if (game.InCreation)
        {
            context = context with
            {
                Prompt = CreationPrompt(game),
                Detail = CreationDetail(game),
                ProgressStep = (int)game.CreationStage + 1,
                ProgressTotal = Enum.GetValues<CreationStage>().Length,
            };
        }
        return context;
    }

    private static string CreationPrompt(Game game) => game.CreationStage switch
    {
        CreationStage.Folk => "What folk bore you?",
        CreationStage.Past => "What were your hands before?",
        CreationStage.ShapeRaise => $"Where did your body rise? {game.ShapingsLeft} shaping choices remain.",
        CreationStage.ShapePay => $"What paid for the rise in {AttributeSet.NameOf(game.ShapeRaise!.Value)}?",
        CreationStage.Thing => game.PickingSecondThing
            ? "What else came through?"
            : "What came through with you?",
        CreationStage.Burden => "Will you carry more for more?",
        CreationStage.Vow => "What do you walk for?",
        CreationStage.Face => "Name someone your character remembers, if you wish.",
        CreationStage.Name => "What is your character called?",
        CreationStage.Review => "Review your character before beginning.",
        _ => "Character creation",
    };

    private static string CreationDetail(Game game) => game.CreationStage switch
    {
        CreationStage.Face or CreationStage.Name => game.NameEntry,
        CreationStage.Review => string.Join(
            "  •  ",
            new[]
            {
                game.Player.Folk is { } folk ? CreationCatalog.FolkOf(folk).Name : "Folk not chosen",
                game.Player.Past is { } past ? CreationCatalog.PastOf(past).Name : "Past not chosen",
                game.Player.Name.Length > 0 ? game.Player.Name : game.NameEntry,
            }.Where(value => value.Length > 0)),
        _ => "",
    };

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

    private static ClientAction[] ConversationActions(Game game)
    {
        var actions = new List<ClientAction>();
        for (int i = 0; i < game.Topics.Count; i++)
        {
            actions.Add(new ClientAction(
                (char)('1' + i),
                $"Ask about {game.Topics[i].Label}",
                0,
                0,
                0));
        }

        if (game.TalkNpc?.Kind == NpcKind.Unbinder)
        {
            actions.Add(new ClientAction(
                (char)('1' + actions.Count),
                $"The unbinding ({game.UnbindingsLeft} left this world)",
                0,
                0,
                0));
        }
        else
        {
            foreach (var offer in game.Offers)
            {
                actions.Add(new ClientAction(
                    (char)('1' + actions.Count),
                    offer.Label,
                    0,
                    0,
                    0));
            }
        }

        return [.. actions];
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
