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
    string DisabledReason = "",
    string Explanation = "");

public sealed record TaskPresentation(
    string Title,
    string Body);

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
    public TaskPresentation? Task { get; init; }

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
        else if (surface is ClientSurface.Menu or ClientSurface.Character or ClientSurface.Equipment)
        {
            context = context with { Task = TaskPresentationFrom(frame, actions, title) };
            if (context.Title.Length == 0 && context.Task.Title.Length > 0)
                context = context with { Title = context.Task.Title };
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
                    def.Trait,
                    Explanation: FolkExplanation(def))),
                new CreationChoice(
                    '0',
                    "Leave it to fate",
                    "The shrine decides the whole of you at once.",
                    "All creation choices are rolled together.",
                    Explanation: "Every creation choice is selected together from the same campaign seed."),
            ],
            CreationStage.Past =>
            [
                .. CreationCatalog.Pasts.Select((def, index) => new CreationChoice(
                    (char)('1' + index),
                    def.Name,
                    def.Blurb,
                    SkillSet.NameOf(def.Skill),
                    Explanation: PastExplanation(def))),
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
                        reason,
                        game.CreationStage == CreationStage.ShapeRaise
                            ? $"Raises {AttributeSet.NameOf(attribute)} by 1. {AttributeSet.DescriptionOf(attribute)}"
                            : $"Lowers {AttributeSet.NameOf(attribute)} by 1. {AttributeSet.DescriptionOf(attribute)}");
                }),
                .. game.CreationStage == CreationStage.ShapeRaise
                    ? new[]
                    {
                        new CreationChoice(
                            '0',
                            "Stand as you are",
                            "Finish shaping without spending the remaining choices.",
                            "",
                            Explanation: "Keeps the current attribute values and advances to the next stage."),
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
                        held ? "Already chosen." : "",
                        def.Blurb);
                }),
            ],
            CreationStage.Burden =>
            [
                .. CreationCatalog.Burdens.Select((def, index) => new CreationChoice(
                    (char)('1' + index),
                    def.Name,
                    def.Blurb,
                    def.Price,
                    Explanation: $"{def.Price}. In return, creation includes a second precious thing.")),
                new CreationChoice(
                    '0',
                    "Carry nothing more",
                    "Continue without a burden or a second precious thing.",
                    "",
                    Explanation: "Adds no lasting burden and does not add a second precious thing."),
            ],
            CreationStage.Vow =>
            [
                .. CreationCatalog.Vows.Select((def, index) => new CreationChoice(
                    (char)('1' + index),
                    def.Name,
                    def.Blurb,
                    "",
                    Explanation: "Adds a persistent personal aim that the world can answer.")),
                new CreationChoice(
                    '0',
                    "No vow but the road",
                    "Continue without swearing a private aim.",
                    "",
                    Explanation: "Adds no personal vow. The rest of the campaign remains unchanged."),
            ],
            _ => [],
        };
    }

    private static string FolkExplanation(FolkDef def)
    {
        var parts = new List<string>();
        if (def.TiltUp is { } up)
            parts.Add($"+1 {AttributeSet.NameOf(up)}. {AttributeSet.DescriptionOf(up)}");
        if (def.TiltDown is { } down)
            parts.Add($"-1 {AttributeSet.NameOf(down)}. {AttributeSet.DescriptionOf(down)}");
        parts.Add(def.Id switch
        {
            FolkId.Steadfolk =>
                "Adds a third shaping choice and 10 starting coin.",
            FolkId.Emberwrought =>
                "Adds 1 maximum Focus, increasing the pool spent on workings.",
            FolkId.Cairnborn =>
                "Reads every hostile tell one tier more clearly.",
            FolkId.Heathborn =>
                "Harvesting yields one additional hide or sprig.",
            FolkId.Wrightkin =>
                "Carried gear gains wear half as often.",
            _ => def.Trait,
        });
        return string.Join("\n", parts);
    }

    private static string PastExplanation(PastDef def)
    {
        string skill =
            $"Begins with {SkillSet.NameOf(def.Skill)} at level 1. {SkillSet.DescriptionOf(def.Skill)}";
        string extra = def.Id switch
        {
            PastId.Soldier => "Begins wearing a half-worn quilted jack.",
            PastId.Poacher => "Begins with a hunting bow equipped.",
            PastId.HedgeHealer =>
                "Begins with 3 sprigs, the Stillcraft lesson, and Lore at level 1.",
            PastId.SmithsHand => "Carries the recognition attached to a smith's hand.",
            PastId.ScribesWard => "Also begins with Lore at level 1.",
            PastId.Wayfarer => "Begins with 2 additional rations.",
            PastId.Oathbreaker =>
                "Also begins with Larceny at level 1, while the home stead begins suspicious.",
            _ => "",
        };
        return extra.Length > 0 ? $"{skill}\n{extra}" : skill;
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

    private static TaskPresentation TaskPresentationFrom(
        Frame frame,
        IReadOnlyList<ClientAction> actions,
        string fallbackTitle)
    {
        string[] lines = frame.ToTextLines();
        if (actions.Count == 0)
            return new TaskPresentation(fallbackTitle, ExtractReadableLines(lines));

        int firstActionRow = actions.Min(action => action.Y);
        int left = actions.Min(action => action.X);
        int right = actions.Max(action => action.X + action.Width);
        int top = Math.Max(0, firstActionRow - 12);
        int bottom = Math.Min(lines.Length - 1, actions.Max(action => action.Y) + 4);

        for (int row = firstActionRow - 1; row >= 0; row--)
        {
            string line = lines[row];
            int candidate = IsHorizontalBorder(line)
                ? FindBorderLeft(line, left)
                : -1;
            if (candidate >= 0)
            {
                left = candidate + 1;
                top = row + 1;
                break;
            }
        }

        for (int row = actions.Max(action => action.Y) + 1; row < lines.Length; row++)
        {
            string line = lines[row];
            if (IsHorizontalBorder(line) && FindBorderLeft(line, left) >= 0)
            {
                bottom = row - 1;
                break;
            }
        }

        var content = new List<string>();
        for (int row = top; row <= bottom; row++)
        {
            string line = lines[row];
            int start = Math.Clamp(left, 0, line.Length);
            int length = Math.Clamp(right - left + 8, 0, line.Length - start);
            string value = length > 0 ? line.Substring(start, length) : "";
            value = value.Trim().Trim('|', '+', '-', '─', '│', '┌', '┐', '└', '┘');
            if (value.Length == 0)
            {
                if (content.Count > 0 && content[^1].Length > 0)
                    content.Add("");
                continue;
            }
            if (actions.Any(action => action.Y == row))
                continue;
            content.Add(value);
        }

        while (content.Count > 0 && content[^1].Length == 0)
            content.RemoveAt(content.Count - 1);
        string title = fallbackTitle;
        if (title.Length == 0 && content.Count > 0)
        {
            title = content[0];
            content.RemoveAt(0);
        }
        return new TaskPresentation(title, string.Join("\n", content));
    }

    private static int FindBorderLeft(string line, int before)
    {
        int start = Math.Min(before, line.Length - 1);
        for (int index = start; index >= 0; index--)
        {
            if (line[index] is '|' or '│' or '+' or '┌' or '└')
                return index;
        }
        return -1;
    }

    private static bool IsHorizontalBorder(string line) =>
        line.Count(character => character is '-' or '─') >= 8;

    private static string ExtractReadableLines(IEnumerable<string> lines) =>
        string.Join(
            "\n",
            lines.Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .Take(24));
}
