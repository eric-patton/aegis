namespace Aegis.Core;

/// <summary>
/// The authored storylet content, in catalog form (design/storylets.md sec 2).
/// Slice content proves the format both ways: facts as preconditions (grievance,
/// deed, met, echo) and facts as output (met, boon). Voice follows the register-one
/// rules in design/story/aegis-arc.md sec 4.
/// </summary>
public static class StoryletCatalog
{
    public static readonly IReadOnlyList<Storylet> All =
    [
        // The Aegis's first words after the wake-up scene, once per character ever.
        new Storylet
        {
            Id = "first-light",
            Trigger = StoryletTrigger.Arrival,
            Scope = StoryletScope.Character,
            Lines =
            [
                ("The Aegis settles against your collarbone, warm as held breath.", LogTone.Aegis),
            ],
        },

        // Visiting the settlement before the deed: the grievance gets a human voice,
        // and the meeting is written to the graph for later content to build on.
        new Storylet
        {
            Id = "grievance-voiced",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("grievance")],
            Forbids = [new FactPattern("deed", "camp_cleared")],
            Lines =
            [
                ("A shutter opens a finger's width. A tired voice: \"{r0.detail}\"", LogTone.Info),
                ("\"Three winters we fed them to keep the peace. There is no more to give.\"", LogTone.Info),
            ],
            Effect = g => g.World.Facts.Add("met", "worried_villager", g.World.SettlementName,
                "A villager who spoke of the goblin raids through a shuttered window."),
        },

        // Only exists if you met the villager first, then finished the deed: chained gating.
        new Storylet
        {
            Id = "gratitude-of-the-stead",
            Trigger = StoryletTrigger.NearHouse,
            Requires =
            [
                new FactPattern("deed", "camp_cleared"),
                new FactPattern("met", "worried_villager"),
            ],
            Lines =
            [
                ("The shutter stands open today. The villager presses a worn pouch into your hands.", LogTone.Reward),
                ("\"Five coin. It is not thanks enough. The nights are quiet now.\"", LogTone.Info),
            ],
            Effect = g =>
            {
                g.Player.Coin += 5;
                g.World.Facts.Add("boon", "stead_pouch", g.World.SettlementName,
                    "A worn pouch of five coin, given in thanks for the quiet nights.");
            },
        },

        // First shrine rest, once per character: the relationship stated plainly.
        new Storylet
        {
            Id = "first-tally",
            Trigger = StoryletTrigger.Rest,
            Scope = StoryletScope.Character,
            Lines =
            [
                ("\"You sit. I count. This is how it will always be between us.\"", LogTone.Aegis),
                ("\"The first tally is small. They all begin small.\"", LogTone.Aegis),
            ],
        },

        // First touch of any waygate: the honest-recovery drip (the Aegis does not
        // remember its purpose yet; every recollection arrives in pieces).
        new Storylet
        {
            Id = "arch-remembered",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.Waygate,
            Scope = StoryletScope.Character,
            Lines =
            [
                ("\"I know this arch. I do not remember knowing it. Give me time; it is coming back in pieces.\"", LogTone.Aegis),
            ],
        },

        // Cycle 2+: the mythology pipe surfacing in the world, not just the arrival text.
        new Storylet
        {
            Id = "echo-ballad-hummed",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("echo", "deed")],
            Lines =
            [
                ("Through a doorway, someone hums a tune you half know. The words: \"{r0.detail}\"", LogTone.Info),
                ("\"Your deeds travel ahead of you. I do not yet remember why.\"", LogTone.Aegis),
            ],
        },

        // First conversation with anyone, once per character: the Aegis notices people.
        new Storylet
        {
            Id = "first-voices",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Lines =
            [
                ("\"So many voices. I count those too, in my way. Speak with them; what they know is weight.\"", LogTone.Aegis),
            ],
        },

        // First step onto any barrow mound (tier 2+ worlds only): the honest-recovery
        // drip brushing the arc's themes without explaining them.
        new Storylet
        {
            Id = "barrow-shadow",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.BarrowEntrance,
            Scope = StoryletScope.Character,
            Lines =
            [
                ("Under your feet, the turf gives like something breathing very slowly.", LogTone.Danger),
                ("\"The dead here were left holding something. I know the weight of standing a post too long.\"", LogTone.Aegis),
            ],
        },

        // The stead's answer to the barrow falling quiet: a small keepsake, and the
        // deed written into the settlement's memory alongside the camp's.
        new Storylet
        {
            Id = "stillness-repaid",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("deed", "barrow_stilled")],
            Lines =
            [
                ("An old man stops you by the well. He presses a bent silver pin into your palm and does not explain it.", LogTone.Reward),
                ("\"My grandmother's. She always said the mound would go quiet in her lifetime. Wrong by two lifetimes, near enough.\"", LogTone.Info),
            ],
            Effect = g => g.World.Facts.Add("boon", "grave_token", g.World.SettlementName,
                "A bent silver pin from the barrow-age, given the day the mound went quiet."),
        },

        // Meeting the wandering mender in a LATER world than the first meeting: the
        // first thread a player can pull that runs between worlds. Completes its own
        // small answer (these wanderers know you) and unlocks asking about it.
        new Storylet
        {
            Id = "unbinder-again",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 10,
            When = g => g.TalkNpc?.Kind == NpcKind.Unbinder
                && g.Player.FirstUnbinderCycle > 0
                && g.Cycle > g.Player.FirstUnbinderCycle,
            Lines =
            [
                ("A different face, a different name, a different trade. But you are looked at the way the last mender looked at you, in a world that is shut now: unsurprised.", LogTone.Info),
                ("These wanderers know you. That much is now certain.", LogTone.Danger),
            ],
            Effect = g => g.World.Facts.Add("noticed", "unbinder", "",
                "The bearer has marked the wandering menders: in every world one waits, and knows the bearer on sight."),
        },

        // ---- The arc ladder, rungs 2 and 3 (D-037, design/story/aegis-arc.md sec 6).
        // Every rung gates on the previous rung's flag, never on a cycle count.

        // Cycle 1 ambient: one stranger, once, unlabeled. The motif said wrong and
        // never explained: significance withheld, to be re-filed by rung 2.
        new Storylet
        {
            Id = "stranger-on-the-road",
            Trigger = StoryletTrigger.AmbientTurn,
            Scope = StoryletScope.Character,
            Priority = 5,
            Weight = 100,
            When = g => g.Cycle == 1 && g.Turn >= 30,
            Lines =
            [
                ("A traveler passes the other way: neither old nor young, dressed a little wrong for the season. They look at you too long, and not at your face.", LogTone.Info),
                ("As they pass: \"All is counted, little shield.\" Said kindly. Said wrong. When you look back, the road is empty.", LogTone.Danger),
            ],
        },

        // Cycle 2 ambient anchor: bearer-myths surfacing in settlement talk. The
        // fact is planted by worldgen in tier 2+ worlds; delivered as ambient
        // voice rather than a topic so villager menus stay within their digits.
        new Storylet
        {
            Id = "tomb-of-the-undying",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("bearer_myth")],
            Weight = 5,
            Lines =
            [
                ("Two elders argue by the well, clearly not for the first time. \"{r0.detail}\"", LogTone.Info),
                ("\"Your kind is sung about before it arrives, bearer. I begin to suspect the songs are not wrong.\"", LogTone.Aegis),
            ],
        },

        // Cycle 2 cold open: the forge-name fragment, turned over like a coin.
        new Storylet
        {
            Id = "forge-name-recovered",
            Trigger = StoryletTrigger.Arrival,
            Scope = StoryletScope.Character,
            Priority = 10,
            When = g => g.Cycle >= 2,
            Lines =
            [
                ($"\"A word came back to me in the crossing. {AegisVoice.ForgeName}. It is mine: my maker's word for me, the way a smith names a blade.\"", LogTone.Aegis),
                ("\"I will turn it over a while. A name is a small thing to be handed back. It does not feel small.\"", LogTone.Aegis),
            ],
        },

        // Rung 2's micro-reveal, paid at the moment it is earned (the study's
        // complete-every-touch rule): the stranger-kind are former bearers. The
        // crossing scene confirms and deepens it; this is where it lands.
        new Storylet
        {
            Id = "severed-truth",
            Trigger = StoryletTrigger.DeedWritten,
            Scope = StoryletScope.Character,
            Priority = 20,
            Requires = [new FactPattern("deed", "severed_laid")],
            Lines =
            [
                ("It spoke while it came apart, without rancor, finishing a story told many times: \"A new grip on the old shield. It carried me once, what you carry. Ask it where it put me down.\"", LogTone.Danger),
                ("\"Bearer. The stranger-kind. I remember now what they are, and I would rather have remembered anything else. At the next crossing, where nothing listens, I will say all of it.\"", LogTone.Aegis),
            ],
            Effect = g => g.Player.SeveredTruthHeard = true,
        },

        // Rung 3's bottle beat (witnessed vision, arc sec 11's cheaper option):
        // what the Aegis is, cut off before what the orders were for.
        new Storylet
        {
            Id = "vision-of-the-forging",
            Trigger = StoryletTrigger.Rest,
            Scope = StoryletScope.Character,
            Priority = 20,
            When = g => g.Player.CrossingGuiltHeard,
            Lines =
            [
                ("The shrine's hum deepens, and the Aegis pulls you under, into a memory that is not yours:", LogTone.Aegis),
                ("A hall of anvils beneath a sky the color of quenching-water. Rows of shields being made: not hammered but argued into being, by smiths who speak each blow. The Shieldwrights.", LogTone.Info),
                ("One of the shields is yours. You watch words laid into it like inlay: catch, carry, count. And over the anvil a commission is spoken: \"Find and temper a soul fit to keep the...\"", LogTone.Info),
                ("The memory frays there, mid-word. The Aegis reaches after it and closes on nothing.", LogTone.Info),
                ("\"A made thing. That much I knew. I did not remember being made. There were orders, bearer: I heard every word except what they were for.\"", LogTone.Aegis),
            ],
            Effect = g => g.Player.VisionSeen = true,
        },

        // Reveal tier 1: the Unbinder confronted knowingly for the first time,
        // gated on the vision. They admit their age, decline the rest, and aim
        // the player back at the Aegis. Unlocks the "long road" topic.
        new Storylet
        {
            Id = "unbinder-known",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 20,
            When = g => g.TalkNpc?.Kind == NpcKind.Unbinder && g.Player.VisionSeen,
            Lines =
            [
                ("You describe what the shrine showed you: the hall of anvils, the argued shields. The mender goes very still, and for a moment the guise sits on them like a coat on a chairback.", LogTone.Info),
                ("\"So it is remembering. Slower than I hoped. Faster than I feared.\" They feed the fire a stick. \"I am older than this world, bearer. Older than several. That is all you get from me today.\"", LogTone.Info),
                ("\"You want the rest? Ask it what it counts. Make it say the word aloud.\"", LogTone.Info),
                ("The Aegis is silent in a way that has a temperature.", LogTone.Aegis),
            ],
            Effect = g => g.Player.UnbinderRevealTier = Math.Max(g.Player.UnbinderRevealTier, 1),
        },

        // Ambient flavor: repeatable, cooldown-gated, deliberately slight.
        new Storylet
        {
            Id = "wind-over-grass",
            Trigger = StoryletTrigger.AmbientTurn,
            Once = false,
            CooldownTurns = 60,
            Weight = 10,
            Lines = [("Wind combs the grass in long silver rows.", LogTone.Info)],
        },
        new Storylet
        {
            Id = "far-bells",
            Trigger = StoryletTrigger.AmbientTurn,
            Once = false,
            CooldownTurns = 90,
            Weight = 5,
            Lines = [("Somewhere far off, a bell no one rings anymore is ringing.", LogTone.Info)],
        },
    ];
}
