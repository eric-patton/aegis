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
    bool Enabled = true,
    string Detail = "");

public sealed record CreationChoice(
    char Key,
    string Name,
    string Description,
    string Detail,
    bool Enabled = true,
    string DisabledReason = "",
    string Explanation = "",
    string Projection = "");

public sealed record TaskPresentation(
    string Title,
    string Body);

public sealed record CharacterAttributePresentation(
    string Name,
    int Value,
    string Description);

public sealed record CharacterSkillPresentation(
    string Name,
    int Level,
    int Uses,
    int NextLevelUses,
    string Description);

public sealed record NamedDetailPresentation(
    string Name,
    string Detail);

public sealed record KnackOptionPresentation(
    char Key,
    string Name,
    string Detail);

public sealed record CharacterPresentation(
    string Name,
    string Identity,
    int Health,
    int MaxHealth,
    int Stamina,
    int MaxStamina,
    int Focus,
    int MaxFocus,
    CharacterAttributePresentation[] Attributes,
    CharacterSkillPresentation[] Skills,
    NamedDetailPresentation[] Knacks,
    NamedDetailPresentation[] Lessons,
    string Burden,
    string Scars,
    string Standing,
    KnackOptionPresentation[] PendingKnacks);

public sealed record EquippedSlotPresentation(
    string Slot,
    string Item,
    string Summary);

public sealed record GearPresentation(
    char Key,
    string Name,
    string Slot,
    string Benefit,
    string Requirement,
    bool MeetsRequirement,
    bool Equipped,
    int Wear,
    int MaximumWear,
    string Craft,
    string Move);

public sealed record CarriedResourcePresentation(
    string Name,
    string Amount);

public sealed record PackPresentation(
    EquippedSlotPresentation[] Slots,
    GearPresentation[] Gear,
    CarriedResourcePresentation[] Resources);

public sealed record CreationPresentation(
    CreationStage Stage,
    int Step,
    int TotalSteps,
    string Prompt,
    string Entry,
    CreationChoice[] Choices,
    string[] ReviewLines)
{
    public string Guidance { get; init; } = "";
    public string PhaseLabel { get; init; } = "";
}

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
    public CharacterPresentation? Character { get; init; }
    public PackPresentation? Pack { get; init; }

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
            ClientSurface.Menu when game.InScene => SceneActions(game),
            ClientSurface.Character => CharacterActions(game),
            ClientSurface.Equipment => EquipmentActions(game),
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
            ClientSurface.Menu when game.InScene => game.SceneTitle,
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
                    CreationReview(game))
                {
                    Guidance = CreationGuidance(game),
                    PhaseLabel = CreationPhaseLabel(game),
                },
            };
        }
        else if (surface == ClientSurface.Character)
        {
            context = context with { Character = CharacterPresentationFrom(game) };
        }
        else if (surface == ClientSurface.Equipment)
        {
            context = context with { Pack = PackPresentationFrom(game) };
        }
        else if (surface == ClientSurface.Menu)
        {
            context = context with
            {
                Task = game.InScene
                    ? new TaskPresentation(
                        game.SceneTitle,
                        string.Join("\n\n", game.SceneProse.Select(line => line.Text)))
                    : TaskPresentationFrom(frame, actions, title),
            };
            if (context.Title.Length == 0 && context.Task.Title.Length > 0)
                context = context with { Title = context.Task.Title };
        }
        return context;
    }

    private static ClientAction[] CharacterActions(Game game) =>
        game.PendingKnack is { } choice
            ? choice.Options
                .Select((option, index) => new ClientAction(
                    (char)('1' + index),
                    option.Name,
                    0,
                    0,
                    0,
                    Detail: option.Blurb))
                .ToArray()
            : [];

    private static ClientAction[] EquipmentActions(Game game) =>
        game.Player.AllGear
            .Select((item, index) => new ClientAction(
                (char)('1' + index),
                item.Name,
                0,
                0,
                0,
                Detail: GearBenefit(item)))
            .ToArray();

    private static CharacterPresentation CharacterPresentationFrom(Game game)
    {
        Player player = game.Player;
        CharacterAttributePresentation[] attributes = Enumerable.Range(0, AttributeSet.Count)
            .Select(index =>
            {
                Attr attribute = (Attr)index;
                return new CharacterAttributePresentation(
                    AttributeSet.NameOf(attribute),
                    player.Attributes[attribute],
                    AttributeSet.DescriptionOf(attribute));
            })
            .ToArray();
        CharacterSkillPresentation[] skills = Enumerable.Range(0, SkillSet.Count)
            .Select(index =>
            {
                SkillId skill = (SkillId)index;
                int level = player.Skills.Level(skill);
                return new CharacterSkillPresentation(
                    SkillSet.NameOf(skill),
                    level,
                    player.Skills.Uses(skill),
                    SkillSet.UsesForLevel(level + 1),
                    SkillSet.DescriptionOf(skill));
            })
            .ToArray();
        NamedDetailPresentation[] knacks = player.Perks
            .Select(id =>
            {
                PerkDef definition = PerkCatalog.Def(id);
                return new NamedDetailPresentation(definition.Name, definition.Blurb);
            })
            .ToArray();
        NamedDetailPresentation[] lessons = player.Lessons
            .Select(id =>
            {
                LessonDef definition = LessonCatalog.Def(id);
                return new NamedDetailPresentation(definition.Short, "Learned and kept.");
            })
            .ToArray();
        KnackOptionPresentation[] pending = game.PendingKnack is { } choice
            ? choice.Options
                .Select((option, index) => new KnackOptionPresentation(
                    (char)('1' + index),
                    option.Name,
                    option.Blurb))
                .ToArray()
            : [];
        string identity = string.Join(
            "  |  ",
            new[]
            {
                player.Folk is { } folk ? CreationCatalog.FolkOf(folk).Name : "",
                player.Past is { } past ? CreationCatalog.PastOf(past).Name : "",
            }.Where(value => value.Length > 0));
        string burden = player.Burden is { } burdenId
            ? CreationCatalog.BurdenOf(burdenId).Name
            : "None";
        string scars = player.Scars.Count > 0
            ? string.Join(", ", player.Scars.Select(DeathsToll.NameOf))
            : "None";
        string standing = game.Standing > 0
            ? $"{LegendStanding.TitleOf(game.Standing)}  |  {player.Legend} legend"
            : $"{player.Legend} legend";
        return new CharacterPresentation(
            player.Name.Length > 0 ? player.Name : "The bearer",
            identity,
            player.Hp,
            player.EffectiveMaxHp,
            player.Stamina,
            player.MaxStamina,
            player.Focus,
            player.MaxFocus,
            attributes,
            skills,
            knacks,
            lessons,
            burden,
            scars,
            standing,
            pending);
    }

    private static PackPresentation PackPresentationFrom(Game game)
    {
        Player player = game.Player;
        EquippedSlotPresentation[] slots =
        [
            Slot("Weapon", player.Weapon, player),
            Slot("Armor", player.Armor, player),
            Slot("Ranged", player.Bow, player),
        ];
        GearPresentation[] gear = player.AllGear
            .Select((item, index) => new GearPresentation(
                (char)('1' + index),
                item.Name,
                item.Slot.ToString(),
                GearBenefit(item),
                $"{AttributeSet.NameOf(item.ReqAttr)} {item.Req}, you have {player.Attributes[item.ReqAttr]}",
                item.MeetsReq(player.Attributes),
                ReferenceEquals(item, player.Weapon)
                    || ReferenceEquals(item, player.Armor)
                    || ReferenceEquals(item, player.Bow),
                item.Wear,
                item.MaxWear,
                item.Slot == GearSlot.Armor ? "Warding" : SkillSet.NameOf(item.Family),
                item.Move == MoveVerb.None ? "Standard" : item.Move.ToString()))
            .ToArray();
        CarriedResourcePresentation[] resources =
        [
            new("Coin", player.Coin.ToString()),
            new("Essence", player.Essence.ToString()),
            new("Rations", player.Rations.ToString()),
            new("Draughts", player.Draughts.ToString()),
            new("Herbs", player.Herb.ToString()),
            new("Hides", (player.Hide + player.ProtectedHide).ToString()),
            new("Raw meat", player.RawMeat.ToString()),
            new("Tarn trout", player.TarnTrout.ToString()),
            new("Salt", player.Salt.ToString()),
            new("Tarn-iron", player.TarnIron.ToString()),
            new("Iron blooms", player.IronBloom.ToString()),
            new("Trinkets", player.Trinket.ToString()),
            new("Books", player.Books.Count.ToString()),
            new("Fishing line", player.FishingLine ? "Yes" : "No"),
        ];
        return new PackPresentation(slots, gear, resources);
    }

    private static EquippedSlotPresentation Slot(string slot, GearItem? item, Player player) =>
        item is null
            ? new EquippedSlotPresentation(slot, "Empty", "No item equipped")
            : new EquippedSlotPresentation(
                slot,
                item.Name,
                $"{GearBenefit(item)}  |  {item.EffectiveBonus(player.Attributes)} effective");

    private static string GearBenefit(GearItem item) => item.Slot switch
    {
        GearSlot.Armor => $"{item.Bonus} protection",
        GearSlot.Ranged => $"+{item.Bonus} ranged",
        _ => $"+{item.Bonus} melee",
    };

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
        CreationStage.ShapeRaise => "Choose an attribute to raise.",
        CreationStage.ShapePay =>
            $"Choose what balances the rise in {AttributeSet.NameOf(game.ShapeRaise!.Value)}.",
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

    private static string CreationGuidance(Game game) => game.CreationStage switch
    {
        CreationStage.ShapeRaise =>
            $"Add 1 point now, then choose a different attribute to lower by 1. {game.ShapingsLeft} shaping choices remain.",
        CreationStage.ShapePay =>
            $"{AttributeSet.NameOf(game.ShapeRaise!.Value)} has risen by 1. Choose a different attribute to lower by 1 and complete this shaping.",
        _ => "",
    };

    private static string CreationPhaseLabel(Game game) => game.CreationStage switch
    {
        CreationStage.ShapeRaise => "SHAPING  ·  RAISE",
        CreationStage.ShapePay => "SHAPING  ·  BALANCE",
        _ => "",
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
                        $"Current  {player.Attributes[attribute]}",
                        enabled,
                        reason,
                        game.CreationStage == CreationStage.ShapeRaise
                            ? $"Raises {AttributeSet.NameOf(attribute)} by 1. {AttributeSet.DescriptionOf(attribute)}"
                            : $"Lowers {AttributeSet.NameOf(attribute)} by 1. {AttributeSet.DescriptionOf(attribute)}",
                        enabled
                            ? game.CreationStage == CreationStage.ShapeRaise
                                ? $"{player.Attributes[attribute]}  →  {player.Attributes[attribute] + 1}"
                                : $"{player.Attributes[attribute]}  →  {player.Attributes[attribute] - 1}"
                            : "");
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

    private static ClientAction[] SceneActions(Game game) =>
        game.SceneChoices
            .Select((choice, index) => new ClientAction(
                (char)('1' + index),
                choice.Label,
                0,
                0,
                0,
                Detail: choice.Tag))
            .ToArray();

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
