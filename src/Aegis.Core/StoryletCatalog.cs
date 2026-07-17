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
