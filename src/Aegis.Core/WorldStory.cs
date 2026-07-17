namespace Aegis.Core;

/// <summary>What a template sees when judging eligibility and compiling (D-035).</summary>
public sealed record StoryTemplateContext(
    List<Npc> Villagers,
    string SettlementName,
    FactGraph Facts,
    List<Site> Sites,
    int Tier);

/// <summary>Compiles a template against a world; draws only from the world-story stream.</summary>
public delegate List<Storylet> CompileTemplate(ref Rng rng, StoryTemplateContext ctx);

/// <summary>One entry in the template pool (design/story/world-story-templates.md sec 1).</summary>
public sealed record StoryTemplate(string Id, Func<StoryTemplateContext, bool> Eligible, CompileTemplate Compile);

/// <summary>
/// The world-story template compiler (D-032 compiled one template; D-035 selects
/// among eligible ones). A template casts role slots onto generated entities at
/// worldgen and emits per-world storylets bound to that cast, plus role facts the
/// rest of the game can read. Selection is part of worldgen: one world, one spine.
/// </summary>
public static class WorldStories
{
    public static readonly StoryTemplate[] All =
    [
        RaidedSteadTemplate.Template,
        CreepingBlightTemplate.Template,
    ];

    /// <summary>
    /// Picks this world's story and compiles it. When exactly one template is
    /// eligible there is NO selection draw, which is what keeps tier-1 worlds
    /// (where only the Raided Stead qualifies) consuming the RNG they always did.
    /// Repeat-weighting (D-040): the previous world's template, when it is in the
    /// running, carries half the weight of every other candidate. Both paths
    /// consume exactly one draw, and the unweighted path is unchanged, so worlds
    /// with no previous story (direct seeds, cycle 1) generate what they always did.
    /// </summary>
    public static List<Storylet> CompileForWorld(ref Rng rng, StoryTemplateContext ctx, string? prevStory = null)
    {
        var eligible = All.Where(t => t.Eligible(ctx)).ToList();
        if (eligible.Count == 0) return [];

        StoryTemplate chosen;
        if (eligible.Count == 1)
        {
            chosen = eligible[0];
        }
        else if (prevStory is null || eligible.All(t => t.Id != prevStory))
        {
            chosen = eligible[rng.Next(eligible.Count)];
        }
        else
        {
            var weighted = eligible.SelectMany(t => Enumerable.Repeat(t, t.Id == prevStory ? 1 : 2)).ToList();
            chosen = weighted[rng.Next(weighted.Count)];
        }

        ctx.Facts.Add("story", chosen.Id, "", $"This world's story is {chosen.Id}.");
        return chosen.Compile(ref rng, ctx);
    }
}

/// <summary>
/// The Raided Stead: the slice story formalized (D-032). Act 1 plants the grievance
/// in a person's mouth (the plaintiff, cast from the settlement's NPCs), act 2 is
/// the deed, act 3 is the witnessed ending and the kept promise. Skipping act 1 is
/// a legitimate playthrough: the promise chain simply never opens.
/// </summary>
public static class RaidedSteadTemplate
{
    public const string Id = "raided-stead";

    public static readonly StoryTemplate Template = new(Id, _ => true, Compile);

    public static List<Storylet> Compile(ref Rng rng, StoryTemplateContext ctx)
    {
        if (ctx.Villagers.Count == 0) return [];

        var plaintiff = rng.Pick(ctx.Villagers);
        ctx.Facts.Add("role", "plaintiff", plaintiff.Id,
            $"{plaintiff.Name} carries {ctx.SettlementName}'s grievance to whoever will hear it.");

        string plaintiffId = plaintiff.Id;
        string name = plaintiff.Name;
        string settlementName = ctx.SettlementName;

        return
        [
            // Act 1: the grievance, personally. Only from the plaintiff, only before the deed.
            new Storylet
            {
                Id = "rs-plea",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Forbids = [new FactPattern("deed", "camp_cleared")],
                When = g => g.TalkNpc?.Id == plaintiffId,
                Lines =
                [
                    ($"{name} grips your arm. \"It was my mother's ewe they took last, and her gate-latch with it.\"", LogTone.Info),
                    ("\"Whatever you are, whatever that thing at your throat is: end this. Please.\"", LogTone.Info),
                ],
                Effect = g => g.World.Facts.Add("promise", "end_the_raids", plaintiffId,
                    $"{name} asked the bearer to end the goblin raids."),
            },

            // Act 3, beat 1: the ending is witnessed the moment it happens (iron rule 6).
            // Gated on THIS story's deed: other deeds (the barrow) fire DeedWritten too.
            new Storylet
            {
                Id = "rs-witnessed-ending",
                Trigger = StoryletTrigger.DeedWritten,
                Priority = 10,
                Requires = [new FactPattern("deed", "camp_cleared")],
                Lines =
                [
                    ($"Somewhere behind you, in {settlementName}, {name} will hear of this by nightfall.", LogTone.Info),
                ],
                Effect = g => g.World.Facts.Add("story_complete", Id, plaintiffId),
            },

            // Act 3, beat 2: the kept promise pays in weight, not coin. Requires having
            // actually been asked: a world where you never met the plaintiff plays differently.
            new Storylet
            {
                Id = "rs-kept-promise",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires =
                [
                    new FactPattern("deed", "camp_cleared"),
                    new FactPattern("promise", "end_the_raids"),
                ],
                When = g => g.TalkNpc?.Id == plaintiffId,
                Lines =
                [
                    ($"{name} says nothing for a long moment. Then: \"The nights are quiet. My house does not forget.\"", LogTone.Reward),
                    ("\"A promise asked, kept, and witnessed. That weighs more than the deed alone.\"", LogTone.Aegis),
                ],
                Effect = g => g.Player.Essence += 3,
            },
        ];
    }
}

/// <summary>
/// The Creeping Blight at slice scale (D-035, template 2 of
/// design/story/world-story-templates.md sec 6): something wrong seeps downhill
/// from the barrow, and the story everyone tells about it is wrong. Exercises the
/// two contract features the Raided Stead lacks: an accepted-history fact flipped
/// by evidence the player actively finds, and endings that branch on whether the
/// truth was found before the deed. Eligible only where a barrow exists (tier 2+).
/// </summary>
public static class CreepingBlightTemplate
{
    public const string Id = "creeping-blight";

    public static readonly StoryTemplate Template = new(
        Id,
        ctx => ctx.Villagers.Count > 0 && ctx.Sites.Any(s => s.Kind == SiteKind.Barrow),
        Compile);

    /// <summary>Deep in the barrow: at or past the third chamber's mouth.</summary>
    private const int DeepX = 19;

    public static List<Storylet> Compile(ref Rng rng, StoryTemplateContext ctx)
    {
        var afflicted = rng.Pick(ctx.Villagers);
        ctx.Facts.Add("role", "afflicted", afflicted.Id,
            $"{afflicted.Name}'s pastures lie under the mound, and the creep takes them first.");

        // The accepted history: planted at compile as ambient fact, voiced by a beat,
        // flipped by evidence found in the barrow itself (iron rule: the mid-turn
        // complicates the story, it does not just invert it).
        ctx.Facts.Add("history", "mound_curse", ctx.SettlementName,
            "The mound-folk were cursed for oath-breaking in the old age, and the curse seeps out of their hill yet. So it has always been told.");

        string afflictedId = afflicted.Id;
        string name = afflicted.Name;
        string settlementName = ctx.SettlementName;

        return
        [
            // Act 1: the creep, personally. Only from the afflicted, only before the stilling.
            new Storylet
            {
                Id = "cb-plea",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Forbids = [new FactPattern("deed", "barrow_stilled")],
                When = g => g.TalkNpc?.Id == afflictedId,
                Lines =
                [
                    ($"{name} pulls you aside, voice low. \"The ewes on the mound-side pasture drop their lambs dead. The grass greys a little further down the hill each month.\"", LogTone.Info),
                    ("\"It is coming for the stead, whatever it is. If the mound is the spring of it, someone must go up and still the source.\"", LogTone.Info),
                ],
                Effect = g => g.World.Facts.Add("promise", "end_the_creep", afflictedId,
                    $"{name} asked the bearer to still whatever seeps from the mound."),
            },

            // The accepted history gets a mouth: everyone knows whose fault the hill is.
            new Storylet
            {
                Id = "cb-accepted-history",
                Trigger = StoryletTrigger.NearHouse,
                Requires = [new FactPattern("history", "mound_curse")],
                Forbids = [new FactPattern("deed", "barrow_stilled")],
                Lines =
                [
                    ("An old woman salts her doorstep, unhurried, thorough. \"{r0.detail}\"", LogTone.Info),
                    ("\"Oath-breakers under the turf. What would you expect to grow downhill of that?\"", LogTone.Info),
                ],
            },

            // The mid-turn: evidence, found by walking deep enough to read the stones.
            // The flip complicates: not cursed oath-breakers, but paid keepers, and the
            // stead below has forgotten it ever needed keeping.
            new Storylet
            {
                Id = "cb-evidence",
                Trigger = StoryletTrigger.EnterTile,
                Tile = Terrain.Floor,
                Priority = 10,
                When = g => g.CurrentSite?.Kind == SiteKind.Barrow && g.Player.Pos.X >= DeepX,
                Lines =
                [
                    ("Past the third chamber the carvings change. These are not wards against the dead. They face inward, past them: the dead are the wards.", LogTone.Info),
                    ("The grave goods are not tribute. They are wages: counted, sealed, and sworn on. No one here was cursed. They were hired.", LogTone.Info),
                    ("\"A watch was bought, and the buyers are dust, and the watch is failing. That is what seeps downhill.\"", LogTone.Aegis),
                ],
                Effect = g => g.World.Facts.Add("evidence", "mound_truth", settlementName,
                    "The mound's dead were not cursed; they were set as a paid watch, and the watch is failing."),
            },

            // Act 3, ending A: stilled with the truth in hand. The stead gets its
            // history back, whether it wants it or not.
            new Storylet
            {
                Id = "cb-ending-truth",
                Trigger = StoryletTrigger.DeedWritten,
                Priority = 10,
                Requires =
                [
                    new FactPattern("deed", "barrow_stilled"),
                    new FactPattern("evidence", "mound_truth"),
                ],
                Lines =
                [
                    ($"By the time this reaches {settlementName}, it will not be a curse outlasted. It will be a debt found, called in, and paid, and the stead will have to learn what its founders bought.", LogTone.Info),
                ],
                Effect = g =>
                {
                    g.World.Facts.Add("story_complete", Id, settlementName);
                    g.World.Facts.Add("coda", "truth_published", settlementName,
                        "The stead learned the mound was a paid watch, not a curse. Its history is heavier now, and truer.");
                },
            },

            // Act 3, ending B: stilled without ever reading the stones. The old story
            // survives the thing it was wrong about.
            new Storylet
            {
                Id = "cb-ending-story",
                Trigger = StoryletTrigger.DeedWritten,
                Priority = 10,
                Requires = [new FactPattern("deed", "barrow_stilled")],
                Forbids = [new FactPattern("evidence", "mound_truth")],
                Lines =
                [
                    ($"In {settlementName} they will say the old curse is broken at last. It is a good story, and it is wrong, and no one left alive knows better.", LogTone.Info),
                ],
                Effect = g =>
                {
                    g.World.Facts.Add("story_complete", Id, settlementName);
                    g.World.Facts.Add("coda", "truth_buried", settlementName,
                        "The stead believes a curse was broken. The truth of the paid watch went back under the turf.");
                },
            },

            // Act 3, the kept promise: same shape as the stead's, earned uphill.
            new Storylet
            {
                Id = "cb-kept-promise",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires =
                [
                    new FactPattern("deed", "barrow_stilled"),
                    new FactPattern("promise", "end_the_creep"),
                ],
                When = g => g.TalkNpc?.Id == afflictedId,
                Lines =
                [
                    ($"{name} meets you at the pasture gate. \"The grass is greening back up the hill. Whatever you did in there, the mound is only a hill now.\"", LogTone.Reward),
                    ("\"A promise asked, kept, and witnessed. It weighs the more for the walking uphill.\"", LogTone.Aegis),
                ],
                Effect = g => g.Player.Essence += 3,
            },
        ];
    }
}
