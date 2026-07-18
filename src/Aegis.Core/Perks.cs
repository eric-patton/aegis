namespace Aegis.Core;

/// <summary>
/// Knacks (D-046, the perk half of D-016): at a skill's threshold the training
/// settles into a question of style, and the sheet puts it to you: two ways of
/// the same craft, take one, forgo the other forever. Like the skills that open
/// them, knacks never respec; the way you learned a thing is part of the thing.
/// Every v1 question (level 2) pits doing more against spending less. Every v2
/// question (level 4, D-055) pits the read moment against the even hand: one
/// answer pays only when the fight is read, the other pays a little on every
/// exchange regardless.
/// </summary>
public enum PerkId
{
    DrawnCut, SpareMotion,
    FollowThrough, KindGrip,
    KnuckleAndBone, DeepBreath,
    BracedShoulder, MendedStrap,
    HuntersEye, LightDraw,
    AnsweredCut, StroppedEdge,
    CheckedSwing, TrueArc,
    CaughtArm, ShortPath,
    ShieldWall, FittedIron,
    PickedMoment, WaxedString,
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
    /// at a time): the level-2 wave first, then the level-4 wave (D-055), so an
    /// older question is always put before a deeper one.
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
        new(SkillId.Blades, 4,
        [
            new(PerkId.AnsweredCut, "the answered cut", "wind-ups take 2 deeper",
                "You stop stepping out of the raised blow's shadow and start speaking under it. The blade answers first."),
            new(PerkId.StroppedEdge, "the stropped edge", "the edge wears half as fast",
                "A cut placed well asks nothing of the edge. Every second one of yours is placed well now."),
        ]),
        new(SkillId.Hafted, 4,
        [
            new(PerkId.CheckedSwing, "the checked swing", "landed blows break wind-ups",
                "The weight lands where the blow is still being raised. Most arguments end there."),
            new(PerkId.TrueArc, "the true arc", "blows land 1 deeper",
                "You stop steering the swing mid-flight. The arc was always truer than the arm."),
        ]),
        new(SkillId.Brawling, 4,
        [
            new(PerkId.CaughtArm, "the caught arm", "fists take wind-ups 3 deeper",
                "A raised arm is a door left open. Your fist has learned to walk in."),
            new(PerkId.ShortPath, "the short path", "fists ask 1 less wind",
                "The fist stops traveling and starts arriving. The distance was never the point."),
        ]),
        new(SkillId.Warding, 4,
        [
            new(PerkId.ShieldWall, "the shield-wall", "crowds are turned up to 2 more",
                "Crowded, you stop meeting blows one at a time. The iron closes its ranks and holds the line."),
            new(PerkId.FittedIron, "the fitted iron", "iron turns 1 more, always",
                "You wear the iron the way it was hammered to sit. Every blow finds it already waiting."),
        ]),
        new(SkillId.Ranged, 4,
        [
            new(PerkId.PickedMoment, "the picked moment", "mid-move marks take 2 deeper",
                "A body mid-motion has already spent its next moment. Your shaft arrives in it."),
            new(PerkId.WaxedString, "the waxed string", "the string frays half as fast",
                "Wax, patience, and a dry palm between draws. The string stops paying for your haste."),
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
        PerkId.AnsweredCut => "answered_cut",
        PerkId.StroppedEdge => "stropped_edge",
        PerkId.CheckedSwing => "checked_swing",
        PerkId.TrueArc => "true_arc",
        PerkId.CaughtArm => "caught_arm",
        PerkId.ShortPath => "short_path",
        PerkId.ShieldWall => "shield_wall",
        PerkId.FittedIron => "fitted_iron",
        PerkId.PickedMoment => "picked_moment",
        PerkId.WaxedString => "waxed_string",
        _ => id.ToString().ToLowerInvariant(),
    };
}
