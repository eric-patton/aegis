namespace Aegis.Core;

/// <summary>
/// A visible check on a scene choice (D-117, cashing D-021's oldest line): the odds
/// are computed from the bearer's own sheet and shown on the choice row before the
/// player commits, then rolled on the gameplay stream when the choice is taken.
/// Exactly one of Skill or Attr names what is being weighed; Difficulty steps the
/// odds down a tenth apiece. The floor and ceiling keep every check a real one.
/// </summary>
public sealed record SceneCheck(SkillId? Skill, Attr? Attr, int Difficulty)
{
    public static SceneCheck OfSkill(SkillId skill, int difficulty = 0) => new(skill, null, difficulty);
    public static SceneCheck OfAttr(Attr attr, int difficulty = 0) => new(null, attr, difficulty);

    public string Name => Skill is { } s ? SkillSet.NameOf(s) : AttributeSet.NameOf(Attr!.Value);

    /// <summary>Half for an unremarkable bearer, a twentieth per point of edge, a tenth off per step of difficulty.</summary>
    public double ChanceFor(Game g)
    {
        int edge = Skill is { } s
            ? g.Player.Skills.Level(s)
            : g.Player.Attributes[Attr!.Value] - AttributeSet.Baseline;
        return Math.Clamp(0.5 + 0.05 * edge - 0.1 * Difficulty, 0.05, 0.95);
    }
}

/// <summary>
/// One numbered answer in a scene node. Next names the node the choice leads to,
/// "" ends the scene. A choice with a check goes to Next only when the roll
/// carries; FailNext is where a failed check lands instead.
/// </summary>
public sealed record SceneChoice(string Label, string Next, SceneCheck? Check = null, string FailNext = "");

/// <summary>
/// One moment of a scene: its prose (landed in the log as it plays, so the log
/// stays the full transcript), the choices it offers, and an optional entry
/// effect. A node with no choices is terminal: any key closes the scene.
/// </summary>
public sealed record SceneNode
{
    public required string Id { get; init; }
    public required (string Text, LogTone Tone)[] Lines { get; init; }
    public SceneChoice[] Choices { get; init; } = [];
    public Action<Game>? OnEnter { get; init; }
}

/// <summary>
/// A dialogue-tree scene (D-117): the modal surface a storylet can open instead of
/// speaking only in log lines. Nodes[0] is the entry. Scenes ride the same key
/// journal as every menu, so a save mid-scene replays back into the same moment.
/// </summary>
public sealed record Scene(string Id, string Title, SceneNode[] Nodes)
{
    public SceneNode NodeById(string id) => Nodes.First(n => n.Id == id);
}
