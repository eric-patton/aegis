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

        // Deep in the quarry (D-040, tier 3+ worlds): world-texture, not arc. The
        // pit's one mystery is a working left mid-stroke, and it stays a mystery.
        new Storylet
        {
            Id = "the-downed-tools",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.Floor,
            Priority = 5,
            When = g => g.CurrentSite?.Kind == SiteKind.Quarry && g.Player.Pos.X >= 14,
            Lines =
            [
                ("Past the spoil heaps the working face rises sheer, and figures stand in it half-freed: a shoulder here, a lifted chin there, each one abandoned between one chisel-blow and the next.", LogTone.Info),
                ("The tools were not dropped. They were set down in rows, edges wiped, as if the crew meant to be back by supper. The dust on them is deeper than the stead is old.", LogTone.Info),
            ],
        },

        // The stead's answer to the quarry going still: same shape as the barrow's,
        // smaller and drier, the way news of far stone arrives.
        new Storylet
        {
            Id = "the-pit-gone-quiet",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("deed", "quarry_hushed")],
            Lines =
            [
                ("A mason's boy runs past with the news, twice around the well before anyone will hold still for it: the figures in the old pit are down.", LogTone.Info),
                ("The woodward only grunts. \"Good stone up there. Maybe now someone will go and get it.\" It is, by stead standards, a eulogy.", LogTone.Info),
            ],
        },

        // Deep in the fallen hall (D-044, tier 4+ worlds): the motif inscription
        // the arc plants in old stone (arc sec 4). The Aegis reads it and offers
        // nothing else; what the words are worth is the player's to notice.
        new Storylet
        {
            Id = "the-lintel-script",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.Floor,
            Priority = 5,
            When = g => g.CurrentSite?.Kind == SiteKind.Hall && g.Player.Pos.X >= 27,
            Lines =
            [
                ("Over the chamber door, sheltered from an age of weather, a line of script survives: fine strokes, sure hands, no language the stead ever spoke.", LogTone.Info),
                ("\"I can read this, bearer. It says: all is counted. ...I do not remember learning this script. Take what you came for, and we will go.\"", LogTone.Aegis),
            ],
        },

        // The stead's answer to the pack going quiet: dusk stops being a debt.
        new Storylet
        {
            Id = "the-quiet-dusk",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("deed", "pack_broken")],
            Lines =
            [
                ("The shepherd is at the well before anyone else wakes, telling it to each comer like a riddle: dusk came, the byre stayed quiet, and the dogs would not stop wagging.", LogTone.Info),
                ("The herbwife hears it through twice. \"Quiet at dusk,\" she says at last, trying the words for cracks. \"Well. Some of us will remember how to sleep, then.\"", LogTone.Info),
            ],
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
                ("\"A made thing. That much I knew. I did not remember being made. I call them the Shieldwrights; whatever they called themselves has not come back. There were orders, bearer: I heard every word except what they were for.\"", LogTone.Aegis),
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

        // ---- The arc ladder, rung 4 (D-038): the argument, in three voices.

        // Rung 4a, the agency model: a severed bearer who chose the cutting and is
        // at peace with it. The Unbinder's argument wearing a face the player might
        // like; the game never disproves it, and the Aegis declines to try.
        new Storylet
        {
            Id = "the-one-at-peace",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 20,
            When = g => g.TalkNpc?.Kind == NpcKind.Severed && g.Player.LedgerHeard,
            Lines =
            [
                ("They read your face the way a ferryman reads weather. \"You carry it differently than the last one who passed. So. You know what I am, and you know about the count. Good; we can skip the shouting.\"", LogTone.Info),
                ("\"I was a bearer, worlds down from here. I heard what you have now heard, and I asked for the knife. Chose it, with both eyes open, and I have not been sorry for one hour of one morning since.\"", LogTone.Info),
                ("\"It costs. Some days I can feel my edges going. But they are my edges now, going at my pace, toward my own ending. I had forgotten what it was to own a thing outright. Your keeper means well, little shield. So does a dam.\"", LogTone.Info),
                ("They pour the tea. It is good tea. That is somehow the worst part.", LogTone.Info),
                ("\"I have nothing to say against them, bearer. That is what makes them dangerous.\"", LogTone.Aegis),
            ],
            Effect = g => g.Player.SeveredPeaceHeard = true,
        },

        // Rung 4b, the essence model: the ring-keeper witnessed instead of fought.
        // Recontextualizes the recurring hollow fight the player already knows: the
        // cost wearing a face the player must pity. Fires at the threshold stones,
        // before any choice to enter; walking away completes the beat too.
        new Storylet
        {
            Id = "what-the-fire-keeps",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.HollowEntrance,
            Scope = StoryletScope.Character,
            Priority = 20,
            When = g => g.Player.LedgerHeard && g.World.HollowSite is { Cleared: false },
            Lines =
            [
                ("From the threshold stones you can see the fire, and its keeper moving around it. You have fought this kind. You have never once watched one.", LogTone.Info),
                ("It sets out two bowls. Fills neither. Says something to the empty side of the fire, tilts its head for the answer, and nods at nothing. Then it clears the bowls, and begins again.", LogTone.Info),
                ("\"No knife, for this one. Its ward broke, and it was dropped, and what you are watching is what remains when everything that chooses has gone: the shape of the days, worn smooth, repeating.\"", LogTone.Aegis),
                ("\"It was not counted out, bearer. It simply stopped being counted. My kind did that. Go in or walk on as you judge; there is no kindness here that fits in a sword, and none that fits in leaving, either.\"", LogTone.Aegis),
            ],
            Effect = g => g.Player.SeveredCostSeen = true,
        },

        // Rung 4c: the Unbinder's second reveal, gated on both witnesses and the
        // first tier (trust and escalation, never a clock). The refusal told from
        // their side, and the threshold offer stated plainly for later.
        new Storylet
        {
            Id = "unbinder-first-bearer",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 25,
            When = g => g.TalkNpc?.Kind == NpcKind.Unbinder
                && g.Player.UnbinderRevealTier >= 1
                && g.Player.SeveredPeaceHeard && g.Player.SeveredCostSeen,
            Lines =
            [
                ("You tell them what you have seen: the one at peace with the kettle, and the one with the bowls. They listen the way stones listen: nothing in the face moving, everything underneath attending.", LogTone.Info),
                ("\"Both true. Then you have earned the other name I carry. I was the first, bearer. First carried, first weighed, first brought the whole way down the chain.\"", LogTone.Info),
                ("\"I stood where it ends, before the fire your keeper still cannot say aloud, and I refused it. I cut myself free on the threshold stone with my ward still shouting in my ears. It is the proudest thing I have ever done, and I have had a very long time to reconsider.\"", LogTone.Info),
                ("\"When you stand there, and you will, I will be there too. Before anything is forced on you, you will be offered a knife. That is a promise, not a threat. Walk at your own pace.\"", LogTone.Info),
                ("\"Bearer. I remember them now, from the other side of that stone. Walk away from this fire, please. I will speak of it when I can do it evenly.\"", LogTone.Aegis),
            ],
            Effect = g => g.Player.UnbinderRevealTier = Math.Max(g.Player.UnbinderRevealTier, 2),
        },

        // ---- The arc ladder, rung 5 (D-039): the threshold. The approach speaks
        // in order down the last stair; the choice itself lives in the keeping
        // menu, and the mandated final beat waits back up in the stead.

        // The stair: the motif in every hand that ever came this far. Withheld
        // significance paying out: the player has read these words since death one.
        new Storylet
        {
            Id = "the-last-stair",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.Floor,
            Scope = StoryletScope.Character,
            Priority = 30,
            When = g => g.CurrentSite?.Kind == SiteKind.Threshold && g.Player.Pos.X >= 8,
            Lines =
            [
                ("The stair bottoms into a corridor, and the walls begin to speak: three words, cut in the shrine-script, over and over, in hands that change every few strides. Carvers by the generation. The same three words.", LogTone.Info),
                ("\"Bearers cut those, coming down. Every one that came this far. I carried some of the hands that held the chisels.\"", LogTone.Aegis),
                ("\"Add yours or not, as you please. The wall does not count. That was always my work.\"", LogTone.Aegis),
            ],
        },

        // The door: the Unbinder present as promised, guise laid down, knife
        // offered and left unforced. The promise from rung 4c, kept to the letter.
        new Storylet
        {
            Id = "the-door-and-the-knife",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.Floor,
            Scope = StoryletScope.Character,
            Priority = 30,
            When = g => g.CurrentSite?.Kind == SiteKind.Threshold && g.Player.Pos.X >= 16,
            Lines =
            [
                ("Ahead, where the corridor opens, someone waits by the last arch: no pack, no tools, no guise at all. You did not pass them on the road, and it does not matter. They were always going to be here.", LogTone.Info),
                ("\"You kept a good pace.\" The Unbinder holds out a knife, handle first: a plain thing, older than any world you have walked. \"As promised. Take it, or wave it away. Nothing is forced here; that is the one law left this deep.\"", LogTone.Info),
                ("\"I will stand where I stood. Whatever you choose, bearer, choose it. The worst thing that ever happened on this stone was a soul that let the stone decide.\"", LogTone.Info),
                ("\"I am here, bearer. Not to argue with them. To be beside you while you look at it. Go and look.\"", LogTone.Aegis),
            ],
        },

        // The chamber: the Hearth and the empty keeping, seen plainly at last.
        // The Aegis speaks as a party to the choice, not a narrator of it.
        new Storylet
        {
            Id = "the-empty-keeping",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.Floor,
            Scope = StoryletScope.Character,
            Priority = 30,
            When = g => g.CurrentSite?.Kind == SiteKind.Threshold && g.Player.Pos.X >= 21,
            Lines =
            [
                ("The chamber. You have stood in great halls; this is not one. It is the size of a room where bread is baked and boots are dried, and at its heart, in a ring of plain stone, burns the Hearth: small, patient, and wrong in no way you can name, except that it burns alone.", LogTone.Info),
                ("Around it, worn into the floor, runs a track: the path a keeper's feet would wear over an age of tending. It is empty. It has been empty long enough that the dust in it has its own dust.", LogTone.Info),
                ("\"This is what I could not remember. Not because it was hidden. Because it hurt. Every world you have bled for was lit from this fire, and no one has kept it since my makers' age guttered out.\"", LogTone.Aegis),
                ("\"Step up to it, bearer. Not because you must. I have carried you a long way; I will not carry you the last three strides. Those are yours.\"", LogTone.Aegis),
            ],
        },

        // The mandated final beat (technique commitment 7): a witnessed morning in
        // the stead, mechanically inert, and the Aegis silent in it. The mystery's
        // resolution is never the last emotional note; this is.
        new Storylet
        {
            Id = "the-morning-after",
            Trigger = StoryletTrigger.NearHouse,
            Scope = StoryletScope.Character,
            Priority = 30,
            When = g => g.Player.Resolution != Resolution.None,
            Requires = [new FactPattern("person", "npc_steadholder")],
            Lines =
            [
                ("Up in the stead, the morning is a plain one: woodsmoke, wet grass, someone arguing mildly about a fence. {r0.object} hails you from a doorway and presses a heel of warm bread into your hands, the way you would to any neighbor passing at this hour.", LogTone.Info),
                ("\"There is porridge on, if you have not eaten. And the fence wants a second opinion, if you have patience for small things.\"", LogTone.Info),
                ("You have walked further down than any soul on this road, and what was at the bottom is what is up here: a fire, kept or not kept, and people worth the keeping either way.", LogTone.Info),
                ("The bread is warm. The fence, on inspection, leans. The morning goes on with you in it.", LogTone.Info),
            ],
        },

        // ---- Steady state (D-045, arc sec 9): small complete stories, no new
        // mystery. The one permitted long thread (the argument) advances a beat
        // at a time, at most one per cycle; everything else pays out and closes.

        // The hermit hears the answer: the keeping, examined by the one voice the
        // game never disproves. Their knife stays theirs; the respect is real.
        new Storylet
        {
            Id = "the-fire-answered-kept",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 20,
            When = g => g.TalkNpc?.Kind == NpcKind.Severed && g.Player.Resolution == Resolution.Kept,
            Lines =
            [
                ("You tell them how it ended: the stone, the two worn places, your hands on the keeping. They are quiet so long the kettle starts talking instead.", LogTone.Info),
                ("\"Then it was a choice, and it was yours, and nothing carried you to it. That is all I ever wanted for any of us.\" They pour. \"I hold to my knife, bearer. I would choose it again by morning. But the fire got a keeper who could have walked away, and I will be turning that over for years.\"", LogTone.Info),
                ("\"They mean it, bearer. That is the strangest thing on this whole long road: everyone I have argued with meant it.\"", LogTone.Aegis),
            ],
        },

        // The hermit hears the other answer: the third road, named by the one
        // soul best placed to know there were only ever supposed to be two.
        new Storylet
        {
            Id = "the-fire-answered-refused",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 20,
            When = g => g.TalkNpc?.Kind == NpcKind.Severed && g.Player.Resolution == Resolution.Refused,
            Lines =
            [
                ("You tell them how it ended: the knife offered and waved away, the commission laid down with your own hands, and the ward still warm at your collarbone.", LogTone.Info),
                ("\"Ha.\" It is not quite a laugh; it is more like a door opening. \"The third road. An age of bearers, an age of knives, and it took you to find it: not severed, not kept. Just walking, the two of you, on no one's errand.\"", LogTone.Info),
                ("\"Sit down. Drink the tea. I want every step of it, and for the first hour of the telling I promise not to argue.\"", LogTone.Info),
                ("\"I am not built to like them, bearer. I find I do anyway.\"", LogTone.Aegis),
            ],
        },

        // The standing irony, surfaced at last (arc sec 7): the first laying-down
        // makes the bearer a colleague in the trade the Unbinder never speaks of.
        new Storylet
        {
            Id = "the-trade-shared",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 30,
            When = g => g.TalkNpc?.Kind == NpcKind.Unbinder && g.Player.SeveredUnbound >= 1,
            Lines =
            [
                ("They know before you say a word. Menders always know whose hands have been doing the work.", LogTone.Info),
                ("\"So. You have taken up the other half of my trade: the half done in the dark, unspoken of at fires, and never once thanked.\" They study their own hands. \"It does not get easier. Do not let it get easier. The day it is easy, put it down.\"", LogTone.Info),
                ("\"That is the whole of my teaching, and I notice you did not need it. Your keeper taught you what things weigh.\"", LogTone.Info),
                ("\"They have carried that alone a very long time, bearer. It is no lighter now. But it is shared.\"", LogTone.Aegis),
            ],
        },

        // The argument, resumed (beat one, per answer): the promise from the
        // threshold kept, one exchange, one point marked, complete in itself.
        new Storylet
        {
            Id = "the-argument-resumed-kept",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 20,
            When = g => g.TalkNpc?.Kind == NpcKind.Unbinder
                && g.Player.Resolution == Resolution.Kept
                && g.Player.ArgumentStage == 0
                && g.Cycle > g.Player.ResolutionCycle,
            Lines =
            [
                ("A new world, a new guise, the same eyes. They set their work down as if you had an appointment. \"Keeper. I promised you an argument; here is my opening. You did not choose the keeping. It chose you an age ago, and called the choosing yours at the very end. A well-made trap looks exactly like a door.\"", LogTone.Info),
                ("\"And a door you can walk back out of is not a trap. We crossed after the choice was made, and the door stood open behind us. It always will.\"", LogTone.Aegis),
                ("The Unbinder smiles, actually smiles. \"Point taken and not conceded. Mark it in your ledger: one to the shield. We resume when the roads cross.\"", LogTone.Info),
            ],
            Effect = g => { g.Player.ArgumentStage = 1; g.Player.ArgumentCycle = g.Cycle; },
        },
        new Storylet
        {
            Id = "the-argument-resumed-refused",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 20,
            When = g => g.TalkNpc?.Kind == NpcKind.Unbinder
                && g.Player.Resolution == Resolution.Refused
                && g.Player.ArgumentStage == 0
                && g.Cycle > g.Player.ResolutionCycle,
            Lines =
            [
                ("A new world, a new guise, the same eyes. They set their work down as if you had an appointment. \"Walker. I promised you an argument, and you have taken half my side of it already, which makes this awkward. Here is what is left: the ward. You laid down the errand, and kept the leash.\"", LogTone.Info),
                ("\"A leash has a held end and a worn end, and no confusion about which is which. Find the held end of us, and I will cut it myself.\"", LogTone.Aegis),
                ("The Unbinder turns that over for a long, honest moment. \"Mark it: one to the shield. I have not been argued with this well in an age. We resume when the roads cross.\"", LogTone.Info),
            ],
            Effect = g => { g.Player.ArgumentStage = 1; g.Player.ArgumentCycle = g.Cycle; },
        },

        // Beat two: the stranger-kind, now that someone holds the count. The one
        // round the argument ever draws, because both sides win it.
        new Storylet
        {
            Id = "the-argument-of-mercies",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 20,
            When = g => g.TalkNpc?.Kind == NpcKind.Unbinder
                && g.Player.ArgumentStage == 1
                && g.Cycle > g.Player.ArgumentCycle,
            Lines =
            [
                ("They pick the thread up mid-sentence, worlds later, as if no time had passed at all. \"The stranger-kind, then. My mercies and your keeper's failures, still walking the deep roads. What is owed them, now that someone holds the count?\"", LogTone.Info),
                ("\"Everything short of forcing it. An ending offered is a mercy; an ending imposed is the old commission wearing kind clothes. We offer. They choose. That is the whole law of it now.\"", LogTone.Aegis),
                ("\"...That is my own law, said back to me in a forged voice.\" They do not smile this time. \"Mark nothing. Some rounds are draws because both sides won them.\"", LogTone.Info),
            ],
            Effect = g => { g.Player.ArgumentStage = 2; g.Player.ArgumentCycle = g.Cycle; },
        },

        // Beat three: the steady state said aloud. Nobody concedes; the argument
        // settles into the one shape that can run forever without going stale.
        new Storylet
        {
            Id = "the-argument-unhurried",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 20,
            When = g => g.TalkNpc?.Kind == NpcKind.Unbinder
                && g.Player.ArgumentStage == 2
                && g.Cycle > g.Player.ArgumentCycle,
            Lines =
            [
                ("\"A last piece, and then the argument can breathe a while. I hold what I have always held: an ending is what makes a thing mean. Nothing you chose has changed my mind.\" They look at you the way a mason looks at old, sound work. \"But I have watched you spend a carried life as if every day of it could end, and I grant this much: you are the best counterargument the shield ever forged. It is still only an argument.\"", LogTone.Info),
                ("\"Granted back: they are still the best question anyone ever asked me. A made thing should keep the question that made it think. We keep theirs.\"", LogTone.Aegis),
                ("\"Then neither of us is finished, and neither of us is in any hurry.\" They shoulder their pack, and the courtesy, for once, is only warmth. \"Walk well, both of you. I will find you deeper down.\"", LogTone.Info),
            ],
            Effect = g => { g.Player.ArgumentStage = 3; g.Player.ArgumentCycle = g.Cycle; },
        },

        // Hearth-signs (arc sec 9): the deep worlds read the answer back. Text
        // only, once per world, never a number: the sec 8 guardrail holds.
        new Storylet
        {
            Id = "hearth-sign-kept",
            Trigger = StoryletTrigger.Rest,
            Priority = 5,
            When = g => g.Player.Resolution == Resolution.Kept && g.World.Tier >= 4,
            Lines =
            [
                ("The shrine's hum settles under your breastbone and stays a moment past the counting, like a held chord.", LogTone.Info),
                ("\"Feel that? The fire knows its keeper crossed. These deep worlds were kindled wild and wild they stay: but the wildness knows whose tread this is now. Nothing here is cruel on purpose tonight.\"", LogTone.Aegis),
            ],
        },
        new Storylet
        {
            Id = "hearth-sign-refused",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.Waygate,
            Priority = 5,
            When = g => g.Player.Resolution == Resolution.Refused && g.World.Tier >= 4,
            Lines =
            [
                ("This deep, the arch's hum has a grain to it: unkept, untuned, and somehow the freer for it, like a bell that answers no ringer.", LogTone.Info),
                ("\"Kindled keeperless, this one, like every world below it. We chose that, and I do not unsay it: hear how it sings anyway. Wild is not the same word as wrong. We walk in the difference.\"", LogTone.Aegis),
            ],
        },

        // The long song: the mythology pipe compounding (D-013 by way of D-045).
        // The stead has the order wrong and a world too many; the correction is
        // the final register doing what it does best, keeping score gently.
        new Storylet
        {
            Id = "the-long-song",
            Trigger = StoryletTrigger.NearHouse,
            Scope = StoryletScope.Character,
            Priority = 10,
            Requires = [new FactPattern("song", "the_descent")],
            When = g => g.Player.Resolution != Resolution.None,
            Lines =
            [
                ("By the well a chain of children are singing a walking-song too long for its tune, a verse for every world, hands slapping the rhythm on the trough. \"{r0.detail}\"", LogTone.Info),
                ("\"They have the order wrong, and one world too many: I was there, and you never wept in any world of glass. Songs are ledgers kept by love instead of weight, bearer. I no longer mind that they balance differently.\"", LogTone.Aegis),
            ],
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
