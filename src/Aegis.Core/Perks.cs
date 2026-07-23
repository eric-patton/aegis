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
    RoomOnTheRack, SecondSteeping,
    LongStride, KeptBreath,
    DeeperHush, QuietHarness,
    RichKist, RoadPrice,
    SoftTouch, PatientWards,
    ForwardEdge, ReturningEdge,
    WholeWeight, RootedHaft,
    CrowdingHands, CaughtWrist,
    DeepSet, EasyGuard,
    ForwardDraw, WaitingString,
    FullWord, SpareSyllable,
    AnsweringWord, DeepWell,
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
        new(SkillId.Alchemy, 2,
        [
            new(PerkId.RoomOnTheRack, "room on the rack", "carry one more hale-draught",
                "You learn which glass may safely touch. One more vial finds a sure place in the rack."),
            new(PerkId.SecondSteeping, "the second steeping", "every second self-brew asks one fewer herb",
                "The second pot answers the warmth left by the first. Every other steeping asks one sprig less."),
        ]),
        new(SkillId.Athletics, 2,
        [
            new(PerkId.LongStride, "the long stride", "rush three clear cells",
                "The second stride stops being an ending. When the ground stays open, the third comes with it."),
            new(PerkId.KeptBreath, "the kept breath", "rushes ask one less wind",
                "You stop spending breath on the start. The ground still passes, and more wind stays yours."),
        ]),
        new(SkillId.Stealth, 2,
        [
            new(PerkId.DeeperHush, "the deeper hush", "soft tread cuts notice one cell further",
                "You learn the half-step before silence. Eyes must come one stride nearer to find you."),
            new(PerkId.QuietHarness, "the quiet harness", "worn metal adds no notice",
                "Buckle answers buckle, ring rests against ring. The iron keeps its counsel while you do."),
        ]),
        new(SkillId.Larceny, 2,
        [
            new(PerkId.RichKist, "the rich kist", "clean burglary takes three more coin",
                "You learn where a household keeps the purse behind the purse. A clean entry reaches it."),
            new(PerkId.RoadPrice, "the road price", "fenced heirlooms pay two more coin",
                "You know what silence is worth two valleys on. The cart begins paying the road's true price."),
        ]),
        new(SkillId.Sleight, 2,
        [
            new(PerkId.SoftTouch, "the soft touch", "pickpocketing gains ten points",
                "A pocket need not know the hand was ever there. Your touch has learned how little touch is needed."),
            new(PerkId.PatientWards, "the patient wards", "lockpicking gains ten points",
                "Old iron speaks slowly. You stop hurrying its answer, and more locks give it."),
        ]),
        new(SkillId.Spellcraft, 2,
        [
            new(PerkId.FullWord, "the full word", "blood, mending, and ward deepen by one",
                "You stop clipping the ending. The whole word stands in your mouth now, and the world hears all of it."),
            new(PerkId.SpareSyllable, "the spare syllable", "every second successful spent word refunds one focus",
                "You learn which breath the word did not need. Every second good saying leaves a little of the well untouched."),
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
        new(SkillId.Spellcraft, 4,
        [
            new(PerkId.AnsweringWord, "the answering word", "a working that answers an intent refunds one focus",
                "You hear the hostile shape inside the word and make your own its answer. The meeting gives something back."),
            new(PerkId.DeepWell, "the deep well", "one more focus, always",
                "The word reaches down and finds another depth beneath the depth you knew."),
        ]),
        new(SkillId.Blades, 6,
        [
            new(PerkId.ForwardEdge, "the forward edge", "pressing blade cuts take one more blood",
                "You give the forward edge your whole step. A pressing cut arrives with the body behind it."),
            new(PerkId.ReturningEdge, "the returning edge", "blade parries return one more pressure",
                "The edge does not only meet the blow. It comes home through the meeting and takes the striker's guard with it."),
        ]),
        new(SkillId.Hafted, 6,
        [
            new(PerkId.WholeWeight, "the whole weight", "pressing hafted heaves add one pressure",
                "You stop saving a piece of yourself from the heave. The whole weight goes through the haft."),
            new(PerkId.RootedHaft, "the rooted haft", "guarded haft subtracts one more committed pressure",
                "The haft finds the ground through your hands. Set behind it, you give less of your guard away."),
        ]),
        new(SkillId.Brawling, 6,
        [
            new(PerkId.CrowdingHands, "the crowding hands", "a pressing shove carries up to two clear cells",
                "One stride is no longer the end of the shove. Your hands stay with the body until the ground truly stops it."),
            new(PerkId.CaughtWrist, "the caught wrist", "an unarmed parry asks one wind",
                "You meet the wrist before the blow has become its whole arm. Less wind is needed when the catch is early."),
        ]),
        new(SkillId.Warding, 6,
        [
            new(PerkId.DeepSet, "the deep set", "guarded armor turns one more blood",
                "You settle the iron into the guarded line until the blow must travel farther to find you."),
            new(PerkId.EasyGuard, "the easy guard", "a successful parry sheds one bearer pressure",
                "The guard comes back without being hauled. Each clean meeting leaves your own line easier than it found it."),
        ]),
        new(SkillId.Ranged, 6,
        [
            new(PerkId.ForwardDraw, "the forward draw", "pressing shafts take one more blood",
                "The pressing foot goes into the string. The shaft leaves with the step still behind it."),
            new(PerkId.WaitingString, "the waiting string", "a shaft into an intent adds one pressure",
                "You hold for the instant the mark commits. The string answers the opening before it can close."),
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
        PerkId.RoomOnTheRack => "room_on_the_rack",
        PerkId.SecondSteeping => "second_steeping",
        PerkId.LongStride => "long_stride",
        PerkId.KeptBreath => "kept_breath",
        PerkId.DeeperHush => "deeper_hush",
        PerkId.QuietHarness => "quiet_harness",
        PerkId.RichKist => "rich_kist",
        PerkId.RoadPrice => "road_price",
        PerkId.SoftTouch => "soft_touch",
        PerkId.PatientWards => "patient_wards",
        PerkId.ForwardEdge => "forward_edge",
        PerkId.ReturningEdge => "returning_edge",
        PerkId.WholeWeight => "whole_weight",
        PerkId.RootedHaft => "rooted_haft",
        PerkId.CrowdingHands => "crowding_hands",
        PerkId.CaughtWrist => "caught_wrist",
        PerkId.DeepSet => "deep_set",
        PerkId.EasyGuard => "easy_guard",
        PerkId.ForwardDraw => "forward_draw",
        PerkId.WaitingString => "waiting_string",
        PerkId.FullWord => "full_word",
        PerkId.SpareSyllable => "spare_syllable",
        PerkId.AnsweringWord => "answering_word",
        PerkId.DeepWell => "deep_well",
        _ => id.ToString().ToLowerInvariant(),
    };
}
