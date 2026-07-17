namespace Aegis.Core;

/// <summary>
/// The world-story template compiler, v0 (D-032). A template casts role slots onto
/// generated entities at worldgen and emits per-world storylets bound to that cast,
/// plus role facts the rest of the game can read. This is the pipeline from
/// design/story/world-story-templates.md; the launch templates compile through the
/// same seam once worlds have the sites and factions they need.
/// </summary>
public static class RaidedSteadTemplate
{
    public const string Id = "raided-stead";

    /// <summary>
    /// Compiles the template against a world's cast. The Raided Stead is the slice
    /// story formalized: act 1 plants the grievance in a person's mouth (the
    /// plaintiff, cast from the settlement's NPCs), act 2 is the deed, act 3 is the
    /// witnessed ending and the kept promise. Skipping act 1 is a legitimate
    /// playthrough: the promise chain simply never opens.
    /// </summary>
    public static List<Storylet> Compile(ref Rng rng, List<Npc> npcs, string settlementName, FactGraph facts)
    {
        if (npcs.Count == 0) return [];

        var plaintiff = rng.Pick(npcs);
        facts.Add("role", "plaintiff", plaintiff.Id,
            $"{plaintiff.Name} carries {settlementName}'s grievance to whoever will hear it.");

        string plaintiffId = plaintiff.Id;
        string name = plaintiff.Name;

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
