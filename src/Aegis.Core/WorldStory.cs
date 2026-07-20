namespace Aegis.Core;

/// <summary>
/// What a template sees when judging eligibility and compiling (D-035). Villagers
/// is the drawable pool for cast-by-lot roles; Npcs is the world's whole standing
/// cast (smith, skald, keeper, harrowers...), for roles that belong to an office
/// rather than a lottery (D-116). Never draw from Npcs: an office is cast by what
/// it is, not by chance.
/// </summary>
public sealed record StoryTemplateContext(
    List<Npc> Villagers,
    string SettlementName,
    FactGraph Facts,
    List<Site> Sites,
    int Tier,
    List<Npc> Npcs);

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
        WarOfFaithsTemplate.Template,
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

/// <summary>
/// The War of Faiths at slice scale (D-116, template 3 of
/// design/story/world-story-templates.md sec 7): two faiths quarreling over an
/// origin both misremember. D-115's institutions supply the whole cast by office:
/// the shrinekeeper and the harrow's elder are the two believer-champions, the
/// doorward is the keeper-of-the-founding-site who knows and has kept silent, and
/// a drawn villager of harrow kin straddles both books. The aggressor is drawn at
/// compile (the spec forbids both-sides mush: the season has a present-tense
/// wrong, done by one side), the two schism accounts are planted against each
/// other, the evidence waits in the harrow at the mother-stone's empty socket, and
/// the climax is the confrontation D-115's rumor line has been promising: the
/// elder coming down to say the claim at the shrine itself, met with the founding
/// truth or with the old answers. At this scale the war is a feud not yet bled,
/// and the best ending is the war that never starts. Eligible tier 2+ so the
/// first world keeps its single crafted story.
/// </summary>
public static class WarOfFaithsTemplate
{
    public const string Id = "war-of-faiths";

    public static readonly StoryTemplate Template = new(
        Id,
        ctx => ctx.Tier >= 2 && ctx.Villagers.Count > 0
            && ctx.Sites.Any(s => s.Kind == SiteKind.Harrow),
        Compile);

    public static List<Storylet> Compile(ref Rng rng, StoryTemplateContext ctx)
    {
        // The champions and the silent keeper hold offices, not lots: only the
        // straddler and the aggressor side are drawn, in that order.
        var keeper = ctx.Npcs.First(n => n.Kind == NpcKind.Keeper);
        var elder = ctx.Npcs.First(n => n.Id == "npc_harrow_elder");
        var doorward = ctx.Npcs.First(n => n.Id == "npc_harrow_doorward");
        var straddler = rng.Pick(ctx.Villagers);
        bool steadDidTheWrong = rng.Next(2) == 0;

        ctx.Facts.Add("role", "stead_champion", keeper.Id,
            $"{keeper.Name}, shrinekeeper, holds the stead's book against the harrow's claim.");
        ctx.Facts.Add("role", "harrow_champion", elder.Id,
            $"{elder.Name}, elder of the harrow, carries the order's claim down the hill.");
        ctx.Facts.Add("role", "straddler", straddler.Id,
            $"{straddler.Name} of {ctx.SettlementName} was born of harrow kin and prays both ways, and the quarrel runs straight through their house.");

        // The present-tense wrong: one side has stopped arguing and started
        // taking. This is what turns two readings into a war.
        if (steadDidTheWrong)
            ctx.Facts.Add("aggressor", "stead", ctx.SettlementName,
                $"This season men of {ctx.SettlementName} went up by night and carted off loose kerb-stones from the harrow's ring: interest, they called it, on a gift the harrow never admits was given.");
        else
            ctx.Facts.Add("aggressor", "harrow", ctx.SettlementName,
                $"This season folk of the harrow came down by night and took the year's offerings off the shrine-stone: collection, they called it, on an account the stead never admits it owes.");

        // The two schism accounts (the spec's paired accepted-history): the same
        // parting, told against each other, one per side of the valley. Both are
        // wrong, and not innocently alike: each makes the other side's founder
        // the liar.
        ctx.Facts.Add("history", "schism_stead", ctx.SettlementName,
            "The stead's account of the parting: the stone was given outright in the founders' day, and when the power settled at the daughter-stone instead of the ring, the harrow's elder of that day could not bear it and swore a loan into the record. A false claim, kept warm ever since for jealousy's sake.");
        ctx.Facts.Add("history", "schism_harrow", "harrow",
            "The harrow's account of the parting: the stead's founder was harrow-raised, asked for the stone, was refused, and carried it down by night, then called the theft a gift so often the valley learned it as one. A theft dressed in thanks, and thanked ever since.");

        string keeperId = keeper.Id;
        string keeperName = keeper.Name;
        string elderId = elder.Id;
        string elderName = elder.Name;
        string doorwardId = doorward.Id;
        string doorwardName = doorward.Name;
        string straddlerId = straddler.Id;
        string straddlerName = straddler.Name;
        string settlementName = ctx.SettlementName;

        return
        [
            // Act 1: the quarrel, personally. The straddler names the season's
            // wrong ({r0.detail} is the aggressor fact) and asks the bearer to
            // stand at the shrine for the claim-saying. A morning never asked
            // for cannot later be settled: the promise gates the settlings.
            new Storylet
            {
                Id = "wf-the-quarrel",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires = [new FactPattern("aggressor")],
                Forbids = [new FactPattern("story_complete", Id)],
                When = g => g.TalkNpc?.Id == straddlerId,
                Lines =
                [
                    ($"{straddlerName} looks up the valley before speaking, which everyone does now. \"{{r0.detail}}\"", LogTone.Info),
                    ("\"My grandmother keeps the ring's fire and my mother swept this shrine, so the quarrel runs straight through my house. The elder means to come down and say the claim at the shrine itself. Be standing there that morning, bearer. Someone should be who owes neither book anything.\"", LogTone.Info),
                ],
                Effect = g => g.World.Facts.Add("promise", "see_it_settled", straddlerId,
                    $"{straddlerName} asked the bearer to stand at the shrine when the harrow's claim is said."),
            },

            // The quarrel at the doors: the wrong voiced where people live, the
            // blight's accepted-history pattern. Ambient priority: a plot beat
            // should still outrank it.
            new Storylet
            {
                Id = "wf-the-talk-of-it",
                Trigger = StoryletTrigger.NearHouse,
                Requires = [new FactPattern("aggressor")],
                Forbids = [new FactPattern("story_complete", Id)],
                Lines =
                [
                    ("Two women split kindling at a door, not talking, until one says, to the wood: \"{r0.detail}\"", LogTone.Info),
                    ("\"And nobody will say sorry for it, because sorry picks a book. You cannot even grieve in this valley now without choosing where to kneel.\"", LogTone.Info),
                ],
            },

            // The stead's schism account, from its champion's own mouth.
            new Storylet
            {
                Id = "wf-stead-account",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires = [new FactPattern("history", "schism_stead")],
                Forbids = [new FactPattern("story_complete", Id)],
                When = g => g.TalkNpc?.Id == keeperId,
                Lines =
                [
                    ($"{keeperName} leans the broom against the stone before answering, which means a long answer. \"{{r0.detail}}\"", LogTone.Info),
                    ("\"That is the account I was handed with the broom. I will not swear to the jealousy; I never met the woman. I will swear the stone has been ours to keep as long as keeping has had a name here.\"", LogTone.Info),
                ],
                Effect = g => g.World.Facts.Add("heard", "stead_account", keeperId,
                    $"{keeperName} told the bearer the stead's account of the parting."),
            },

            // The harrow's schism account, from its champion: and the elder,
            // honest to the bone, asks for the one thing that could unmake it.
            new Storylet
            {
                Id = "wf-harrow-account",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires = [new FactPattern("history", "schism_harrow")],
                Forbids = [new FactPattern("story_complete", Id)],
                When = g => g.TalkNpc?.Id == elderId,
                Lines =
                [
                    ($"{elderName} says it the way the rite is said, without hurry and without doubt. \"{{r0.detail}}\"", LogTone.Info),
                    ("\"I knew the elder who taught me it, who knew the one before. None of them lied. Whether the first telling was true is a question I was not raised to ask. So I ask you, to my shame: if this valley holds a reason to ask it, bring it to me.\"", LogTone.Info),
                ],
                Effect = g => g.World.Facts.Add("heard", "harrow_account", elderId,
                    $"{elderName} told the bearer the harrow's account of the parting."),
            },

            // The mid-turn: the founding truth, cut where only a kneeling keeper
            // would ever read it. Grief, not doctrine, and both tellings invented
            // after: the flip complicates both books instead of crowning either.
            new Storylet
            {
                Id = "wf-evidence",
                Trigger = StoryletTrigger.EnterTile,
                Tile = Terrain.Plinth,
                Priority = 10,
                When = g => g.CurrentSite?.Kind == SiteKind.Harrow,
                Lines =
                [
                    ("The empty socket beside the mother-stone is swept, but the sweeping has kept it, not erased it. Low on the inner face, where a kneeling keeper's eye would fall, old cuts are still legible.", LogTone.Info),
                    ("Two names, cut twice: once side by side over a single rite-mark, and once apart, the second name recut alone under a grave-tally in a winter's count. The stone did not go down the hill over doctrine. It went down in a burying winter, carried by one keeper of two, to stand over the stead's dying while the other stayed and kept the ring.", LogTone.Info),
                    ("No loan is cut here, and no gift either. Two who kept one rite, a winter that took too much, and a parting neither wrote down true. Lent and given were both invented afterward, because the real telling was too heavy to keep.", LogTone.Info),
                    ("\"Two houses, bearer, built on the two halves of one grief. Neither lie is wicked. Both are load-bearing. Mind where you set your feet.\"", LogTone.Aegis),
                ],
                Effect = g => g.World.Facts.Add("evidence", "founding_truth", settlementName,
                    "The shrine-stone went down the hill in a burying winter, carried by one of the harrow's own two rite-keepers to stand over the stead's dying. Neither lent nor given: one grief parted in two, and both tellings invented after."),
            },

            // The complicit authority: the doorward has read that socket for a
            // whole office and said nothing. Silence, not malice, per the spec.
            new Storylet
            {
                Id = "wf-doorward-silence",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires = [new FactPattern("evidence", "founding_truth")],
                When = g => g.TalkNpc?.Id == doorwardId,
                Lines =
                [
                    ($"{doorwardName} does not ask what you found. Door-keeping is knowing what has been walked past. \"You read the socket, then.\"", LogTone.Info),
                    ("\"I rake that floor. I have read those cuts my whole office and said nothing, because a door's work is keeping weather out, and that telling is weather. Most years I would say so still. Now it is out of my keeping, and I find I am glad.\"", LogTone.Info),
                ],
            },

            // The climax, truth in hand, staged as a scene (D-118, the choice
            // D-116 parked on the D-117 machinery): the elder comes down at
            // dawn, the claim is said over the daughter-stone with both
            // champions standing, and the socket's telling is the bearer's to
            // spend or keep. The spec's three-way ending at slice scale: say
            // it whole, wield it for one book, or bury it. Without the truth
            // there is nothing to choose, so wf-claim-cold stays plain lines.
            new Storylet
            {
                Id = "wf-claim-truth",
                Trigger = StoryletTrigger.Rest,
                Priority = 10,
                Requires =
                [
                    new FactPattern("heard", "stead_account"),
                    new FactPattern("heard", "harrow_account"),
                    new FactPattern("evidence", "founding_truth"),
                ],
                Forbids = [new FactPattern("story_complete", Id)],
                Lines = [],
                Scene = new Scene("the-claim-at-dawn", "The claim at dawn",
                [
                    new SceneNode
                    {
                        Id = "open",
                        Lines =
                        [
                            ($"At dawn, as the well has had it for weeks, {elderName} comes down the hill and stands before the shrine, and says the harrow's claim over the daughter-stone, formally, like a debt read out. {keeperName} does not sweep while it is said.", LogTone.Info),
                            ("The claim ends, and the quiet after it is a held breath: the old answers wait word-perfect in the keeper's mouth, the socket's cuts wait in yours, and no one else on this ground knows there is anything to wait for.", LogTone.Info),
                            ("\"You carry the one thing neither book holds, bearer. Truth does not spend itself. Say it whole, spend it crooked, or keep it. I only keep the count.\"", LogTone.Aegis),
                        ],
                        Choices =
                        [
                            new SceneChoice("Say the socket's telling, whole", "shared"),
                            new SceneChoice("Turn the cuts against the harrow's claim", "broken",
                                SceneCheck.OfAttr(Attr.Presence, difficulty: 1), FailNext: "seen"),
                            new SceneChoice("Keep the telling to yourself", "kept"),
                        ],
                    },

                    // Publish: the war that never starts, the best ending this
                    // template owns, exactly as it read before it was a choice.
                    new SceneNode
                    {
                        Id = "shared",
                        Lines =
                        [
                            ("Once, in the open, you say what is cut low in the socket: the two names, the one rite, the burying winter. Neither of them stops you. It lands the way truth lands on people who have spent their lives arguing the wrong question.", LogTone.Info),
                            ($"By full light something has been agreed that neither book has a word for yet: the rite said at both stones, hearth to hearth, and the stone's keeping left where the grief left it, shared. The harrow's folk walk back up the hill unarmed of their claim, and {settlementName} watches them go with nothing to forgive.", LogTone.Reward),
                            ("\"A war ended before it fed, bearer. The claim was a question, and you were carrying the answer, and you spent it whole. Few walks end that cleanly. Mark this one.\"", LogTone.Aegis),
                        ],
                        OnEnter = g =>
                        {
                            g.World.Facts.Add("story_complete", Id, settlementName);
                            g.World.Facts.Add("coda", "one_grief_shared", settlementName,
                                "The founding truth was said at the shrine with both champions standing, and the claim dissolved in it: one rite now, said at both stones, the keeping shared as the grief was.");
                        },
                    },

                    // Side with the stead, and it carries: a true stone laid in
                    // a crooked course. The claim breaks; nothing is shared.
                    new SceneNode
                    {
                        Id = "broken",
                        Lines =
                        [
                            ($"You say the socket's cuts, but you say them the stead's way: the stone carried down to stand over {settlementName}'s dying, seated by grief, and grief keeps what it seats. The burying winter you give them. The two names over one rite-mark you keep. {elderName} asked this valley for a reason to doubt, and you hand over exactly half of one.", LogTone.Info),
                            ($"It carries the way a blade carries. The elder stands a long while, then turns up the hill without the claim, and the harrow's folk follow, and nothing is shared. {keeperName} keeps the stone, the book, and the last word, and looks at you like someone counting what it cost.", LogTone.Info),
                            ("\"Broken, then, on a true stone laid in a crooked course. The stead will call this winning by supper; up the hill they will call it what it is. I keep the count either way, bearer.\"", LogTone.Aegis),
                        ],
                        OnEnter = g =>
                        {
                            g.World.Facts.Add("story_complete", Id, settlementName);
                            g.World.Facts.Add("coda", "claim_broken", settlementName,
                                "The socket's telling was said at the shrine shaped as the stead's answer, and the harrow's claim broke on the half of it. The stone stays, the ring's book is closed against its keepers, and the whole truth is still cut where no one now will climb to read it.");
                        },
                    },

                    // Side with the stead, and the shaping is heard: honest to
                    // the bone cuts both ways. The truth is spent and buys
                    // nothing; the quarrel shelves the old way.
                    new SceneNode
                    {
                        Id = "seen",
                        Lines =
                        [
                            ($"You shape it as you say it, and {elderName} hears the shaping. Honest to the bone cuts both ways: the elder asked this valley for a reason to doubt, not a reason built to order, and one raised hand stops the half-telling before it is done. \"No. Not said so. Whatever you read up there, you have spent it.\"", LogTone.Info),
                            ($"{keeperName} answers the claim with the gift's own catechism, unhurried, word-perfect, and the morning closes the way the last generation's did: spoken, answered, refused, folded away. The truth is still in the socket. No one standing here will believe it now from you.", LogTone.Info),
                            ("\"You carried the answer and bent it, and it broke in the bending. Shelved, then, and both wrong books stand. Truth spent crooked buys what crooked buys, bearer: nothing, at cost.\"", LogTone.Aegis),
                        ],
                        OnEnter = g =>
                        {
                            g.World.Facts.Add("story_complete", Id, settlementName);
                            g.World.Facts.Add("coda", "claim_shelved", settlementName,
                                "The claim was said at the shrine, and the founding truth was offered bent to the stead's shape; the elder refused the shaping, and the quarrel folded away unspent, both wrong tellings standing.");
                        },
                    },

                    // Suppress: peace on the standing lie, chosen. The valley
                    // ends where the cold climax ends; the graph knows why.
                    new SceneNode
                    {
                        Id = "kept",
                        Lines =
                        [
                            ($"You say nothing. {keeperName} answers the claim with the gift's own catechism, unhurried, word-perfect, and nothing new is said, because the one person standing there with anything new to say is not saying it.", LogTone.Info),
                            ("The claim is spoken, answered, refused, and folded away, the way it was in the last generation and the one before. The season's wrong is not paid back; it is shelved. The stead calls it peace by supper, and only you know what it is shelved on.", LogTone.Info),
                            ("\"Kept, then. A war ends on a standing lie as surely as on a truth; it only does not stay ended. You stood the one morning it could be said, and chose the quiet. I keep the count of unsaid things too, bearer.\"", LogTone.Aegis),
                        ],
                        OnEnter = g =>
                        {
                            g.World.Facts.Add("story_complete", Id, settlementName);
                            g.World.Facts.Add("coda", "claim_shelved", settlementName,
                                "The claim was said at the shrine and met with the old answers, and the bearer stood there holding the founding truth and kept it. The quarrel folded away unspent, both wrong tellings standing on a silence freely chosen.");
                            g.World.Facts.Add("withheld", "founding_truth", settlementName,
                                "At the claim-saying the bearer held the socket's telling and said nothing: the founding truth stayed in its stone by choice, not ignorance.");
                        },
                    },
                ]),
            },

            // The climax, cold: the claim said and met with the old answers,
            // because no one present has anything new to say. The quarrel is
            // shelved, not settled, and both wrong books stand.
            new Storylet
            {
                Id = "wf-claim-cold",
                Trigger = StoryletTrigger.Rest,
                Priority = 10,
                Requires =
                [
                    new FactPattern("heard", "stead_account"),
                    new FactPattern("heard", "harrow_account"),
                ],
                Forbids =
                [
                    new FactPattern("evidence", "founding_truth"),
                    new FactPattern("story_complete", Id),
                ],
                Lines =
                [
                    ($"At dawn, as the well has had it for weeks, {elderName} comes down the hill and says the harrow's claim over the daughter-stone, formally, like a debt read out. {keeperName} answers it with the gift's own catechism, unhurried, word-perfect, and nothing new is said, because no one standing there has anything new to say.", LogTone.Info),
                    ("The claim is spoken, answered, refused, and folded away, the way it was in the last generation and the one before. The season's wrong is not paid back; it is shelved. The stead calls it peace by supper, and it will hold the way shelved things hold.", LogTone.Info),
                    ("\"Both of them argued their books, bearer, and both books are wrong, and neither knows it. Not a war this year. But a quarrel standing on a false floor never quite closes. It waits.\"", LogTone.Aegis),
                ],
                Effect = g =>
                {
                    g.World.Facts.Add("story_complete", Id, settlementName);
                    g.World.Facts.Add("coda", "claim_shelved", settlementName,
                        "The claim was said at the shrine and met with the old answers. No truth was on hand; the quarrel folded away unspent, and both wrong tellings stand.");
                },
            },

            // The witnessed settling, truth said: the straddler's two houses are
            // one house this morning. Pays what every settled story pays.
            new Storylet
            {
                Id = "wf-settling-truth",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires =
                [
                    new FactPattern("coda", "one_grief_shared"),
                    new FactPattern("promise", "see_it_settled"),
                ],
                When = g => g.TalkNpc?.Id == straddlerId,
                Lines =
                [
                    ($"{straddlerName} finds your hand before any greeting. \"I prayed at both stones this morning, and for the first time in my life it was one prayer. My grandmother's fire and my mother's broom in one house. I asked you to stand there, and you stood there carrying the one thing worth saying.\"", LogTone.Reward),
                    ("\"A morning asked for, stood, and answered. That weighs like a deed, bearer, though no blow was struck in it. The best ones weigh exactly like that.\"", LogTone.Aegis),
                ],
                Effect = g => g.Player.Essence += 3,
            },

            // The witnessed settling, cold: shelved is not nothing, to someone
            // whose whole life has been lived under the shelf. Same coin: the
            // endings differ in what the valley believes, never in the pay.
            new Storylet
            {
                Id = "wf-settling-cold",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires =
                [
                    new FactPattern("coda", "claim_shelved"),
                    new FactPattern("promise", "see_it_settled"),
                ],
                When = g => g.TalkNpc?.Id == straddlerId,
                Lines =
                [
                    ($"{straddlerName} lets out a breath they have plainly held for a season. \"Shelved, then. I have lived my whole life under shelved, and I can live the rest of it there too. My kin walked back up the hill still owed or still owing, depending which of my doors you ask. But nobody bled, and you stood where I asked you to stand.\"", LogTone.Reward),
                    ("\"A morning asked for and stood, even empty-handed. The claim will come down the hill again in some other lifetime, bearer. It will not be your weather then.\"", LogTone.Aegis),
                ],
                Effect = g => g.Player.Essence += 3,
            },

            // The witnessed settling, claim broken: the straddler's two houses
            // are further apart than ever, and they saw what you did. Same
            // coin: the endings differ in what the valley believes, never in
            // the pay.
            new Storylet
            {
                Id = "wf-settling-broken",
                Trigger = StoryletTrigger.Talk,
                Priority = 10,
                Requires =
                [
                    new FactPattern("coda", "claim_broken"),
                    new FactPattern("promise", "see_it_settled"),
                ],
                When = g => g.TalkNpc?.Id == straddlerId,
                Lines =
                [
                    ($"{straddlerName} does not find your hand. \"My mother's shrine keeps its stone, and my grandmother's fire is a beaten fire now. I prayed at both stones this morning and it was two prayers, further apart than I have ever carried them. You stood where I asked you to stand, and I watched what you did there, and I have not decided what I watched.\"", LogTone.Reward),
                    ("\"A morning asked for, stood, and answered with an edge. It weighs like a deed all the same, bearer. Deeds are not sorted kind from keen before they are weighed.\"", LogTone.Aegis),
                ],
                Effect = g => g.Player.Essence += 3,
            },
        ];
    }
}
