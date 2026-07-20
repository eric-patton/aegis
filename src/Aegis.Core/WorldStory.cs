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
        UsurpedThroneTemplate.Template,
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
                // A story ends once (D-112): evidence read only after the deed
                // must not fire the truth ending off some later deed's hook when
                // the buried-truth ending has already closed the story.
                Forbids = [new FactPattern("story_complete", Id)],
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

/// <summary>
/// The Usurped Throne at slice scale (D-112, template 1 of
/// design/story/world-story-templates.md sec 5): the seat that rules the dens was
/// taken, not given, the taking was lied about, and the lie is now load-bearing.
/// The throne reads broadly by design, and at this scale it is the camp's own
/// chieftaincy: D-110's roster supplies the ruler-by-lie (the sitting chief), the
/// displaced line (a lieutenant cast as the old blood), and live succession for
/// the restoration beats. The dens' official telling hangs the old chief's death
/// on a stead arrow, which is what keeps the raids righteous; the evidence in the
/// camp complicates rather than inverts (iron rule: the flip must complicate),
/// because the war the old chief was raising would have burned the stead. Eligible
/// tier 2+ so the first world keeps its single crafted story.
/// </summary>
public static class UsurpedThroneTemplate
{
    public const string Id = "usurped-throne";

    public static readonly StoryTemplate Template = new(
        Id,
        ctx => ctx.Tier >= 2 && ctx.Villagers.Count > 0
            && ctx.Sites.Any(s => s.Kind == SiteKind.GoblinCamp),
        Compile);

    public static List<Storylet> Compile(ref Rng rng, StoryTemplateContext ctx)
    {
        var camp = ctx.Sites.First(s => s.Kind == SiteKind.GoblinCamp);
        var named = camp.Spawns.Where(s => s.Epithet is not null).ToList();
        string chief = named.First(s => s.Chief).Epithet!;
        var lieutenants = named.Where(s => !s.Chief).Select(s => s.Epithet!).ToList();

        // The cast: a stead voice to tell the story, the old blood among the
        // lieutenants, and the dead chief named from the story's own stream so
        // the roster's draws stay put.
        var teller = rng.Pick(ctx.Villagers);
        string claimant = rng.Pick(lieutenants);
        string old = NameGen.Raider(ref rng, [chief, .. lieutenants]);

        ctx.Facts.Add("role", "teller", teller.Id,
            $"{teller.Name} keeps the stead's account of the dens' seat and how it was come by.");
        ctx.Facts.Add("role", "claimant", claimant,
            $"{claimant} of the camp is {old}'s own blood, standing lieutenant to the seat the death left empty.");

        // The accepted history: the taking, lied about outward. Blaming the stead
        // is what makes the lie load-bearing: every raid since is collected as a
        // debt the stead never actually incurred.
        ctx.Facts.Add("history", "seat_taken", ctx.SettlementName,
            $"Before {chief} there was {old}, and the dens' own telling is that a stead arrow took {old} off the palisade on a raid night, and that {chief} rose swearing to pay the death back. Every raid since has been collected as that debt.");

        // Deep as the dens' own sleeping-ground: mirrored from the camp's spawn
        // depth so the cairn is always at least as far in as the deepest raider.
        int deepAt = Math.Min(10, camp.Spawns.Max(s => s.Pos.Manhattan(camp.EntryPos)));
        Pos entry = camp.EntryPos;

        string tellerId = teller.Id;
        string tellerName = teller.Name;
        string settlementName = ctx.SettlementName;

        return
        [
            // Act 1: the story told, personally. Only from the teller, only while
            // the seat still stands. Hearing it is what the settling beats need:
            // a story never asked for cannot later be settled (the cold path).
            new Storylet
            {
                Id = "ut-the-story-told",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires = [new FactPattern("history", "seat_taken")],
                Forbids = [new FactPattern("deed", "camp_cleared")],
                When = g => g.TalkNpc?.Id == tellerId,
                Lines =
                [
                    ($"{tellerName} tips their head at the hills. \"{{r0.detail}}\"", LogTone.Info),
                    ($"\"And the second voice at the fires is {claimant}'s, they say: {old}'s own blood, standing lieutenant to the very seat the death left empty. Blood serves strangely up there.\"", LogTone.Info),
                    ("\"A seat, a death the story blames outward, and the dead one's blood standing under the one who profited. Count who the telling serves, bearer.\"", LogTone.Aegis),
                ],
                Effect = g => g.World.Facts.Add("heard", "seat_story", tellerId,
                    $"{tellerName} told the bearer the dens' account of how {chief} came to the seat."),
            },

            // The mid-turn: evidence, found by walking past the fires to where the
            // dens keep their dead. The flip complicates: the usurper's knife
            // killed a war that would have burned the stead, and the lie that
            // hides the knife is the same lie that keeps the raids righteous.
            new Storylet
            {
                Id = "ut-evidence",
                Trigger = StoryletTrigger.EnterTile,
                Tile = Terrain.Floor,
                Priority = 10,
                When = g => g.CurrentSite?.Kind == SiteKind.GoblinCamp
                    && g.Player.Pos.Manhattan(entry) >= deepAt,
                Lines =
                [
                    ("Behind the fires, past where the dens keep their sleeping, stones stand stacked in the old way over a long body: a chief's cairn, kept but not visited.", LogTone.Info),
                    ($"The bones tell it plainly to any eye that stays: no arrow did this. A den-blade went in under the ribs, a hand's length away. {old} died looking at someone trusted.", LogTone.Info),
                    ($"And among the cairn-goods, the rest of it: war-tokens gathered from every den, sworn and counted for one night of fire against {settlementName}. {chief} killed the war with its maker, hung the death on a stead arrow, and has fed the dens on that debt since.", LogTone.Info),
                    ("\"So the tyrant's knife bought the stead its standing walls, and the lie that hides the knife is what keeps the raids coming. A truth like this does not make the next blow simpler, bearer. It only makes it honest.\"", LogTone.Aegis),
                ],
                Effect = g => g.World.Facts.Add("evidence", "seat_truth", settlementName,
                    $"{old} died on a den-blade, not a stead arrow. {chief} killed the maker of a war that would have burned {settlementName}, and lied the seat into holding."),
            },

            // The line restored: D-110's succession crowns the old blood live, and
            // the stead reads the new voice off the night-fires. No truth needed:
            // the claimant's blood is public den-telling either way.
            new Storylet
            {
                Id = "ut-line-restored",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires = [new FactPattern("nemesis", "risen", claimant)],
                Forbids = [new FactPattern("deed", "camp_cleared")],
                When = g => g.TalkNpc?.Kind == NpcKind.Villager,
                Lines =
                [
                    ($"\"The howls have a new name in them since the old voice fell: {claimant}, over the fires now. The old blood back on the seat, if den-talk is worth its salt, which it is not.\"", LogTone.Info),
                    ("\"The dead chief's blood over the dead chief's fires. Whatever the truth of the taking, the wheel has turned it back, and grudges inherit better than seats do.\"", LogTone.Aegis),
                ],
            },

            // The line passed over: some other named voice rose instead, and the
            // old blood stood aside or was stood aside. {r0.object} is the risen
            // heir off the succession's own fact.
            new Storylet
            {
                Id = "ut-line-passed-over",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires = [new FactPattern("nemesis", "risen")],
                Forbids =
                [
                    new FactPattern("nemesis", "risen", claimant),
                    new FactPattern("deed", "camp_cleared"),
                ],
                When = g => g.TalkNpc?.Kind == NpcKind.Villager,
                Lines =
                [
                    ($"\"There is a new voice over the fires, and it is not the old blood's: {{r0.object}} rose, and {claimant} stood aside or was stood aside. Passed-over blood in a den full of blades: that will keep.\"", LogTone.Info),
                    ("\"Twice now the seat has skipped the line that bred it. A habit like that becomes a story of its own, and stories like that end in knives.\"", LogTone.Aegis),
                ],
            },

            // Act 3, ending A: the seat falls with the truth in hand. What comes
            // down the hill is the ledger, not just the quiet.
            new Storylet
            {
                Id = "ut-ending-truth",
                Trigger = StoryletTrigger.DeedWritten,
                Priority = 10,
                Requires =
                [
                    new FactPattern("deed", "camp_cleared"),
                    new FactPattern("evidence", "seat_truth"),
                ],
                // A story ends once: a cairn read only after the fall must not
                // fire this off some later deed's hook when the lie's ending has
                // already closed the story. The late truth still reaches the
                // teller through the settling; it does not rewrite the fall.
                Forbids = [new FactPattern("story_complete", Id)],
                Lines =
                [
                    ($"The fires above {settlementName} are out, and the true telling of the seat comes down the hill with you: a war killed with its maker, a death hung on a stead arrow, and a lie that held the dens together until nothing did.", LogTone.Info),
                ],
                Effect = g =>
                {
                    g.World.Facts.Add("story_complete", Id, settlementName);
                    g.World.Facts.Add("coda", "seat_truth_carried", settlementName,
                        $"The bearer carried the truth of the taken seat out of the dens: {chief}'s lie is on record where the tellings can find it.");
                },
            },

            // Act 3, ending B: the seat falls and the lie outlives it. A story can
            // end without ever being known.
            new Storylet
            {
                Id = "ut-ending-lie",
                Trigger = StoryletTrigger.DeedWritten,
                Priority = 10,
                Requires = [new FactPattern("deed", "camp_cleared")],
                Forbids = [new FactPattern("evidence", "seat_truth")],
                Lines =
                [
                    ($"The fires above {settlementName} are out. In the only telling anyone keeps, {chief} avenged {old} against the stead to the last, and the seat's true story goes cold under the cairn-stones with no one left to dig for it.", LogTone.Info),
                ],
                Effect = g =>
                {
                    g.World.Facts.Add("story_complete", Id, settlementName);
                    g.World.Facts.Add("coda", "seat_lie_stands", settlementName,
                        $"The dens fell with their telling intact: {old} dead to a stead arrow, {chief} the avenger. The truth stayed under the cairn.");
                },
            },

            // The witnessed settling, truth found: the teller takes the ledger
            // back. Pays what every template's settled story pays; the endings
            // differ in what the world now believes, never in the coin.
            new Storylet
            {
                Id = "ut-telling-truth",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires =
                [
                    new FactPattern("deed", "camp_cleared"),
                    new FactPattern("evidence", "seat_truth"),
                    new FactPattern("heard", "seat_story"),
                ],
                When = g => g.TalkNpc?.Id == tellerId,
                Lines =
                [
                    ($"{tellerName} hears the whole of it without once looking away: the cairn, the blade, the war that never marched. \"Every door in {settlementName} has feared that hill by a story that was never true. You have not given us back our dead. You have given us back the ledger.\"", LogTone.Reward),
                    ("\"A story asked for, dug out, and carried home. That is a bard's deed and a gravedigger's both, and it weighs like both.\"", LogTone.Aegis),
                ],
                Effect = g => g.Player.Essence += 3,
            },

            // The witnessed settling, truth left buried: the teller closes the
            // book unread, and is content to.
            new Storylet
            {
                Id = "ut-telling-quiet",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires =
                [
                    new FactPattern("deed", "camp_cleared"),
                    new FactPattern("heard", "seat_story"),
                ],
                Forbids = [new FactPattern("evidence", "seat_truth")],
                When = g => g.TalkNpc?.Id == tellerId,
                Lines =
                [
                    ($"{tellerName} nods slowly at the quiet hills. \"So {chief} goes under still owed a death by us, by the dens' own telling. Let the hill keep its stories. The nights are ours again, and that is the only telling I need.\"", LogTone.Reward),
                    ("\"The seat's truth goes under with its holders, unread. A story can be finished without ever being known, bearer. Most are.\"", LogTone.Aegis),
                ],
                Effect = g => g.Player.Essence += 3,
            },
        ];
    }
}
