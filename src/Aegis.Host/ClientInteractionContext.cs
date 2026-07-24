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

public sealed record CreationChoice(
    char Key,
    string Name,
    string Description,
    string Detail,
    bool Enabled = true,
    string DisabledReason = "");

public sealed record CreationPresentation(
    CreationStage Stage,
    int Step,
    int TotalSteps,
    string Prompt,
    string Entry,
    CreationChoice[] Choices,
    string[] ReviewLines);

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
    public CreationPresentation? Creation { get; init; }

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
            ClientSurface.CreationChoice => CreationChoices(game)
                .Select(choice => new ClientAction(
                    choice.Key,
                    choice.Name,
                    0,
                    0,
                    0,
                    choice.Enabled))
                .ToArray(),
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
                ProgressStep = CreationStep(game),
                ProgressTotal = 10,
                Creation = new CreationPresentation(
                    game.CreationStage,
                    CreationStep(game),
                    10,
                    CreationPrompt(game),
                    game.NameEntry,
                    CreationChoices(game),
                    CreationReview(game)),
            };
        }
        return context;
    }

    private static int CreationStep(Game game) => game.CreationStage switch
    {
        CreationStage.Folk => 1,
        CreationStage.Past => 2,
        CreationStage.ShapeRaise or CreationStage.ShapePay => 3,
        CreationStage.Thing when !game.PickingSecondThing => 4,
        CreationStage.Burden => 5,
        CreationStage.Thing => 6,
        CreationStage.Vow => 7,
        CreationStage.Face => 8,
        CreationStage.Name => 9,
        _ => 10,
    };

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

    private static CreationChoice[] CreationChoices(Game game)
    {
        var player = game.Player;
        return game.CreationStage switch
        {
            CreationStage.Folk =>
            [
                .. CreationCatalog.Folk.Select((def, index) => new CreationChoice(
                    (char)('1' + index),
                    def.Name,
                    def.Blurb,
                    def.Trait)),
                new CreationChoice(
                    '0',
                    "Leave it to fate",
                    "The shrine decides the whole of you at once.",
                    "All creation choices are rolled together."),
            ],
            CreationStage.Past =>
            [
                .. CreationCatalog.Pasts.Select((def, index) => new CreationChoice(
                    (char)('1' + index),
                    def.Name,
                    def.Blurb,
                    SkillSet.NameOf(def.Skill))),
            ],
            CreationStage.ShapeRaise or CreationStage.ShapePay =>
            [
                .. Enumerable.Range(0, AttributeSet.Count).Select(index =>
                {
                    var attribute = (Attr)index;
                    bool enabled = game.CreationStage == CreationStage.ShapeRaise
                        ? player.Attributes[attribute] < CreationCatalog.ShapeCeiling
                        : attribute != game.ShapeRaise
                            && player.Attributes[attribute] > CreationCatalog.ShapeFloor;
                    string reason = enabled
                        ? ""
                        : game.CreationStage == CreationStage.ShapeRaise
                            ? "Already at the creation ceiling."
                            : attribute == game.ShapeRaise
                                ? "The raised attribute cannot also pay."
                                : "Already at the creation floor.";
                    return new CreationChoice(
                        (char)('1' + index),
                        AttributeSet.NameOf(attribute),
                        AttributeSet.DescriptionOf(attribute),
                        $"Current value {player.Attributes[attribute]}",
                        enabled,
                        reason);
                }),
                .. game.CreationStage == CreationStage.ShapeRaise
                    ? new[]
                    {
                        new CreationChoice(
                            '0',
                            "Stand as you are",
                            "Finish shaping without spending the remaining choices.",
                            ""),
                    }
                    : [],
            ],
            CreationStage.Thing =>
            [
                .. CreationCatalog.Things.Select((def, index) =>
                {
                    bool held = player.Things.Contains(def.Id);
                    return new CreationChoice(
                        (char)('1' + index),
                        def.Name,
                        def.Blurb,
                        held ? "Already carried" : "",
                        !held,
                        held ? "Already chosen." : "");
                }),
            ],
            CreationStage.Burden =>
            [
                .. CreationCatalog.Burdens.Select((def, index) => new CreationChoice(
                    (char)('1' + index),
                    def.Name,
                    def.Blurb,
                    def.Price)),
                new CreationChoice(
                    '0',
                    "Carry nothing more",
                    "Continue without a burden or a second precious thing.",
                    ""),
            ],
            CreationStage.Vow =>
            [
                .. CreationCatalog.Vows.Select((def, index) => new CreationChoice(
                    (char)('1' + index),
                    def.Name,
                    def.Blurb,
                    "")),
                new CreationChoice(
                    '0',
                    "No vow but the road",
                    "Continue without swearing a private aim.",
                    ""),
            ],
            _ => [],
        };
    }

    private static string[] CreationReview(Game game)
    {
        if (game.CreationStage != CreationStage.Review)
            return [];

        Player player = game.Player;
        string things = player.Things.Count > 0
            ? string.Join(", ", player.Things.Select(id => CreationCatalog.ThingOf(id).Name))
            : "None";
        return
        [
            $"Name: {(game.NameEntry.Trim().Length > 0 ? game.NameEntry.Trim() : "Generated at confirmation")}",
            $"Folk: {(player.Folk is { } folk ? CreationCatalog.FolkOf(folk).Name : "Not chosen")}",
            $"Past: {(player.Past is { } past ? CreationCatalog.PastOf(past).Name : "Not chosen")}",
            $"Attributes: {string.Join("  ", Enumerable.Range(0, AttributeSet.Count).Select(index =>
                $"{AttributeSet.NameOf((Attr)index)} {player.Attributes[(Attr)index]}"))}",
            $"Precious things: {things}",
            $"Burden: {(player.Burden is { } burden ? CreationCatalog.BurdenOf(burden).Name : "None")}",
            $"Vow: {(player.Vow is { } vow ? CreationCatalog.VowOf(vow).Name : "None")}",
            $"Remembered person: {(player.RememberedFace.Length > 0 ? player.RememberedFace : "None")}",
        ];
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
