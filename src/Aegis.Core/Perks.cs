namespace Aegis.Core;

/// <summary>
/// Knacks (D-046, the perk half of D-016): at a skill's threshold the training
/// settles into a question of style, and the sheet puts it to you: two ways of
/// the same craft, take one, forgo the other forever. Like the skills that open
/// them, knacks never respec; the way you learned a thing is part of the thing.
/// Every v1 question pits doing more against spending less.
/// </summary>
public enum PerkId
{
    DrawnCut, SpareMotion,
    FollowThrough, KindGrip,
    KnuckleAndBone, DeepBreath,
    BracedShoulder, MendedStrap,
    HuntersEye, LightDraw,
}

/// <summary>
/// One answer a threshold offers. Name and Blurb are menu-sized; ChosenLine is
/// what the log says the moment the choice lands.
/// </summary>
public sealed record PerkDef(PerkId Id, string Name, string Blurb, string ChosenLine);

/// <summary>One question the sheet can put: a skill's threshold and its answers.</summary>
public sealed record KnackChoice(SkillId Skill, int Level, PerkDef[] Options);

public static class PerkCatalog
{
    /// <summary>
    /// Every threshold question, in the ledger's order (the sheet puts them one
    /// at a time). v1: one question per skill, at level 2.
    /// </summary>
    public static readonly IReadOnlyList<KnackChoice> Choices =
    [
        new(SkillId.Blades, 2,
        [
            new(PerkId.DrawnCut, "the drawn cut", "blades cut 1 deeper",
                "You stop forcing the edge and start following it. The cut draws itself now."),
            new(PerkId.SpareMotion, "the spare motion", "swings ask 1 less wind",
                "The wasted half of every swing falls away. The blade goes where it was going anyway."),
        ]),
        new(SkillId.Hafted, 2,
        [
            new(PerkId.FollowThrough, "the follow-through", "a kill repays 2 wind",
                "The weight does the work now; you only steer it. A finished swing hands its wind back."),
            new(PerkId.KindGrip, "the kind grip", "the edge wears half as fast",
                "You stop fighting the haft, and the edge stops paying for the argument."),
        ]),
        new(SkillId.Brawling, 2,
        [
            new(PerkId.KnuckleAndBone, "knuckle and bone", "bare fists hit 2 harder",
                "Your fists finish their apprenticeship. Bone has learned where bone gives."),
            new(PerkId.DeepBreath, "the deep breath", "2 more wind, always",
                "You learn where the breath goes when it goes, and keep a little more of it."),
        ]),
        new(SkillId.Warding, 2,
        [
            new(PerkId.BracedShoulder, "the braced shoulder", "iron turns wind-ups 2 more",
                "You read the wind-up now and set your shoulder into it before it lands."),
            new(PerkId.MendedStrap, "the mended strap", "armor wears half as fast",
                "You take the blow along the strap, not across it, and the iron thanks you."),
        ]),
        new(SkillId.Ranged, 2,
        [
            new(PerkId.HuntersEye, "the hunter's eye", "shafts strike 1 deeper",
                "You stop watching the shaft and start watching the mark. The shaft takes the hint."),
            new(PerkId.LightDraw, "the light draw", "a loose asks 1 less wind",
                "The draw finds the bow's own depth and stops there. The string does the rest."),
        ]),
    ];

    public static PerkDef Def(PerkId id) =>
        Choices.SelectMany(c => c.Options).First(o => o.Id == id);

    /// <summary>Stable snake_case id for snapshots and saves-adjacent surfaces.</summary>
    public static string IdOf(PerkId id) => id switch
    {
        PerkId.DrawnCut => "drawn_cut",
        PerkId.SpareMotion => "spare_motion",
        PerkId.FollowThrough => "follow_through",
        PerkId.KindGrip => "kind_grip",
        PerkId.KnuckleAndBone => "knuckle_and_bone",
        PerkId.DeepBreath => "deep_breath",
        PerkId.BracedShoulder => "braced_shoulder",
        PerkId.MendedStrap => "mended_strap",
        PerkId.HuntersEye => "hunters_eye",
        PerkId.LightDraw => "light_draw",
        _ => id.ToString().ToLowerInvariant(),
    };
}
