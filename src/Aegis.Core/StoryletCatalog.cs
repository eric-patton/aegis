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

        // The signs read at the well (D-132): the winter omen's reader, alive
        // only in the gap between the warning and the weather: a future the
        // stead can see coming is a future the stead talks about.
        new Storylet
        {
            Id = "the-signs-read",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("omen", "hard_winter")],
            Forbids = [new FactPattern("event", "hard_winter")],
            Lines =
            [
                ("An old man on a bench is splitting withies and not looking at the sky, in the way of a man who has already looked. \"Geese knew first,\" he says. \"They always do. Get your wood in, whoever you are.\"", LogTone.Info),
                ("\"A stead that can see winter coming is already spending the warning, bearer. Watch what they do with it. It is the same craft I am teaching you.\"", LogTone.Aegis),
            ],
        },

        // The river read at the bank (D-133): the washout omen's reader,
        // alive only in the gap between the warning and the water.
        new Storylet
        {
            Id = "the-river-read",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("omen", "fords_washout")],
            Forbids = [new FactPattern("event", "fords_washout")],
            Lines =
            [
                ("Two boys run up from the river with the day's news: the water has eaten the sandbar and is working on the willow. A woman carrying sacks uphill from the low granary does not stop to hear it. \"Told you,\" she says, to nobody in particular.", LogTone.Info),
                ("\"Count who is carrying sacks uphill, bearer. That is a stead spending a warning while it is still worth something.\"", LogTone.Aegis),
            ],
        },

        // The banns at the well (D-133): the wedding omen's reader, alive in
        // the gap between the promise and the feast, or the putting-off.
        new Storylet
        {
            Id = "the-banns-heard",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("omen", "banns_read")],
            Forbids = [new FactPattern("event", "wedding"), new FactPattern("event", "wedding_put_off")],
            Lines =
            [
                ("An argument drifts over a fence: geese, and whose beer, and whether the smith's trestles will bear dancing. It is the first argument you have heard in this stead that nobody needed to win.", LogTone.Info),
                ("\"They have put a good day on the calendar next to the hard ones, bearer. Watch how they defend it. A stead fights differently when one of the things behind the wall is a date.\"", LogTone.Aegis),
            ],
        },

        // Visiting the settlement before the deed: the grievance gets a human voice,
        // and the meeting is written to the graph for later content to build on.
        // The first dialogue-tree scene (D-117): the same beat, now answered instead
        // of overheard, with the format's first visible check on the pressing.
        new Storylet
        {
            Id = "grievance-voiced",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("grievance")],
            Forbids = [new FactPattern("deed", "camp_cleared")],
            Lines = [],
            Effect = g => g.World.Facts.Add("met", "worried_villager", g.World.SettlementName,
                "A villager who spoke of the goblin raids through a shuttered window."),
            Scene = new Scene("the-shuttered-window", "The shuttered window",
            [
                new SceneNode
                {
                    Id = "open",
                    Lines =
                    [
                        ("A shutter opens a finger's width. A tired voice: \"{r0.detail}\"", LogTone.Info),
                        ("\"Three winters we fed them to keep the peace. There is no more to give.\"", LogTone.Info),
                    ],
                    Choices =
                    [
                        new SceneChoice("Press them for the whole of it", "whole",
                            SceneCheck.OfAttr(Attr.Presence, difficulty: 1), FailNext: "shut"),
                        new SceneChoice("Give your word on the camp", "word"),
                        new SceneChoice("Leave them to their evening", ""),
                    ],
                },
                new SceneNode
                {
                    Id = "whole",
                    Lines =
                    [
                        ("The shutter swings wide. The voice drops low and quick, glad to be asked at last.", LogTone.Info),
                        ("\"They come at dusk, along the dry ground, never the marsh. Count the fires before you count the knives.\"", LogTone.Info),
                    ],
                    OnEnter = g => g.World.Facts.Add("counsel", "camp_ways", g.World.SettlementName,
                        "The raiders come at dusk along the dry ground, never the marsh; their fires are an honest count of them."),
                },
                new SceneNode
                {
                    Id = "shut",
                    Lines =
                    [
                        ("A pause. Then, flat: \"Fine words. The last who talked so ate our bread a week and left.\"", LogTone.Info),
                        ("The shutter draws to, and a bolt slides home behind it.", LogTone.Info),
                    ],
                },
                new SceneNode
                {
                    Id = "word",
                    Lines =
                    [
                        ("\"Words are thin blankets in this cold. But I will keep yours, and count the nights by it.\"", LogTone.Info),
                        ("The shutter stays open a finger's width until you are well down the lane.", LogTone.Info),
                    ],
                    OnEnter = g => g.World.Facts.Add("promise", "quiet_nights", g.World.SettlementName,
                        "The bearer gave a villager their word, through a shuttered window, that the camp would be dealt with."),
                },
            ]),
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

        // The unsaid travels too (D-120): a truth kept at its saying-moment left
        // a wrong story standing, and the wrong story crosses arches the way any
        // good story does. Once per world, near the houses, the bearer hears it
        // retold for true by folk with no way to know better, because the one
        // way to know better stayed unsaid. Deliberately not hushed-gated: the
        // hushed name stills the songs about the bearer, and this was never one
        // of those. r0 is the oldest silence in the count.
        new Storylet
        {
            Id = "silence-retold",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("silence")],
            Lines =
            [
                ("By a doorway, a walker off the trade-road is telling a story from over the arch, and the stead leans in, because it is a good one. \"{r0.detail}\"", LogTone.Info),
                ("\"Told for true, and no one under this sky can say otherwise but you. It crossed on its own legs, bearer: what was never said cannot be hushed. I keep the count of unsaid things, and the count travels.\"", LogTone.Aegis),
            ],
        },

        // The burden made visible (D-051): the stead lives one season under terms
        // it never swore, and only the Aegis can say whose they are.
        new Storylet
        {
            Id = "the-hard-season",
            Trigger = StoryletTrigger.NearHouse,
            When = g => g.Burden > 0,
            Lines =
            [
                ("By the byre door a woman counts sacks, loses the count, and starts again. \"A lean year,\" she says, to nobody. \"Lean, and long.\"", LogTone.Info),
                ("\"They do not know whose terms hold over their season, bearer. I do. The count honors what is carried; it does not ask who else carries it.\"", LogTone.Aegis),
            ],
        },

        // Standing walks ahead (D-051): the first time a stead knows the bearer
        // before a word is said. Once per character: being known stops being news.
        new Storylet
        {
            Id = "the-known-face",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 10,
            When = g => g.TalkNpc?.Kind == NpcKind.Villager
                && g.Standing >= 2
                && !g.World.Oaths.Contains(OathId.HushedName),
            Lines =
            [
                ("They put a hand up before you can begin: not a stranger's greeting. \"We know. The song came through days ahead of you, and half the stead has watched the road since.\"", LogTone.Info),
                ("\"So the third ledger walks ahead of us now. Mind it, bearer: a door opened before you knock is a kind of debt.\"", LogTone.Aegis),
            ],
        },

        // Suspicion made walkable (D-086): the first authored content the shame
        // fact opens, D-085's seam read from the dark side. Once per world, gated
        // on live shame too, so a bearer who has paid every sill back stops
        // being watched at the doors as well as on the ledger.
        new Storylet
        {
            Id = "the-closed-doors",
            Trigger = StoryletTrigger.NearHouse,
            Priority = 8,
            Requires = [new FactPattern("shame", "watched")],
            When = g => g.Shame > 0,
            Lines =
            [
                ("A door ahead of you closes. Not slammed: eased shut, the way a thing is done when the doing of it is meant to be seen. Somewhere behind it, a bar comes down.", LogTone.Info),
                ("\"They have not stopped weighing your deeds, bearer. They have started weighing your hands.\"", LogTone.Aegis),
            ],
        },

        // The rumor kept from strangers (D-085): the last of D-077's named
        // friend-rung boons, carried by the storylet channel since the villagers'
        // talk menu has no digit to spare (D-080). Gated on the regard fact the
        // rung crossing writes, so this is the first authored content that
        // reputation opens: a friend hears the story the stead keeps inside its
        // own fence, once per world, and no stranger ever does.
        new Storylet
        {
            Id = "the-friends-hearthtale",
            Trigger = StoryletTrigger.Talk,
            Priority = 8,
            Requires = [new FactPattern("regard", "friend")],
            // Suspicion closes the fence (D-086): the inside story is not told
            // while any door's count stands. Gated on live shame, not the shame
            // fact, because the fact is history and restitution reopens the telling.
            When = g => g.TalkNpc?.Kind == NpcKind.Villager && g.Shame == 0,
            Lines =
            [
                ("They glance down the lane before they speak, which is how you know it is no stranger's story. Then they tell you the part the stead keeps inside its own fence: the winter it nearly broke, the name not said at the well, why the door-posts are tarred and the third field lies fallow.", LogTone.Info),
                ("\"No song bought that telling, bearer. You stood for these folk, and they have let you inside the fence of it. I keep many ledgers; there is none that buys this.\"", LogTone.Aegis),
            ],
            Effect = g => g.World.Facts.Add("rumor", "stead_hearthtale", g.World.SettlementName,
                "The stead's own story, told to a friend: the hard winter, the unsaid name, the tarred door-posts."),
        },

        // The stead says its piece (D-088): suspicion acting beyond commerce at
        // last. The shame fact's first consumer from the confrontation side:
        // a named thief is told so, to their face, once per stead. Gated on the
        // live rung, not the fact alone, so paying even one sill back ends the
        // saying before it lands; the fact it writes is history for later
        // content (a making-right beat, a grudge carried to the next talk).
        new Storylet
        {
            Id = "the-steads-reckoning",
            Trigger = StoryletTrigger.Talk,
            Priority = 12,
            Requires = [new FactPattern("shame", "thief")],
            When = g => g.TalkNpc?.Kind == NpcKind.Villager
                && SteadShame.RungFor(g.Shame) >= SteadShame.BarredRung,
            Lines =
            [
                ("There is no good day this time. They look past your shoulder while they speak, at the doors you opened, and their voice is the whole stead's voice: \"Three sills stand robbed, and the name said at the well was yours. Make them right, or keep to the road and off our ground. That is the stead's piece, and now it is said.\"", LogTone.Danger),
                ("\"No knife in that, bearer, and none needed. A stead this small is one ledger with doors, and you are written in it. I carry what you took the same as what you earned; the way back has not moved. The same sills. The same coin.\"", LogTone.Aegis),
            ],
            Effect = g => g.World.Facts.Add("shame", "confronted", g.World.SettlementName,
                "The stead said its piece to the bearer's face: three sills robbed, the name given at the well, the road named as theirs to keep to."),
        },

        // The debt made right (D-109): the confronted fact's consumer, the
        // making-right beat the roadmap promised. Both producers feed it (the
        // reckoning at the barred rung, D-088, and the caught hand, D-107),
        // and it is gated on live shame back at zero, so it plays only when
        // every sill and every hand stands paid. Narrative and a fact,
        // deliberately no coin and no regard: restitution must never turn a
        // profit, or the ladder becomes a market. The made_right fact is the
        // reward, fuel for content that remembers the one who made it right.
        new Storylet
        {
            Id = "the-debt-made-right",
            Trigger = StoryletTrigger.Talk,
            Priority = 9,
            Requires = [new FactPattern("shame", "confronted")],
            When = g => g.TalkNpc?.Kind == NpcKind.Villager && g.Shame == 0,
            Lines =
            [
                ("They step into your road by the well, which is how the stead says a thing is official. \"It is paid, then. Every door, every hand, and into the right ones. What was said of you here is done being said.\" A nod, witnessed, and the lane goes back to its work. At a stead's well, a nod is a document.", LogTone.Info),
                ("\"Mark what came back to you here, bearer, because it was not coin. A stead this small cannot afford forgetting, so it has done the dearer thing and forgiven with its eyes open. I have written it beside the wrong, and the two lines hold each other still.\"", LogTone.Aegis),
            ],
            Effect = g => g.World.Facts.Add("shame", "made_right", g.World.SettlementName,
                "The debt was paid down to nothing and the stead marked it at the well: the naming ended, the book kept open at both lines."),
        },

        // The mended page (D-113): the made_right fact's consumer in its turn,
        // the roadmap's "stead remembering the one who made right." Not a
        // clean slate: the stead keeps the mend where it shows, deliberately,
        // because a stead's memory is its only wall. Fires on a later talk
        // than the making-right itself (it requires the fact that beat writes),
        // so the remembering reads as remembering, not as the event.
        new Storylet
        {
            Id = "the-mended-page",
            Trigger = StoryletTrigger.Talk,
            Priority = 8,
            Requires = [new FactPattern("shame", "made_right")],
            When = g => g.TalkNpc?.Kind == NpcKind.Villager,
            Lines =
            [
                ("There is a beat, when you come up the lane, where the talk does not change around you, and that quiet was bought. \"We tell it now, you should know. Not the taking, or not only that. The paying back, every door of it, into the right hands. A stead needs one story where the road back stayed open and somebody walked it the whole way.\"", LogTone.Info),
                ("\"Hear what the stead has made of you, bearer: not a clean page. A mended one, and it keeps the mend where the young can see the stitching. Of every wall this place has raised, that is the one that works.\"", LogTone.Aegis),
            ],
        },

        // The two memories (D-113): the made_right thread meeting the roster's
        // memory. The valley keeps books on both its hillsides: the stead's,
        // closed by payment at the well, and the dens', opened by a death and
        // handed down the roster with the office. The villager speaks only
        // what a stead can perceive (the fires' new voice, the hill's habit
        // of keeping tallies); the Aegis reads the far book's owner aloud.
        // Above the mended page while both stand, so the meeting lands first.
        new Storylet
        {
            Id = "the-two-memories",
            Trigger = StoryletTrigger.Talk,
            Priority = 9,
            Requires =
            [
                new FactPattern("shame", "made_right"),
                new FactPattern("nemesis", "risen"),
            ],
            When = g => g.TalkNpc?.Kind == NpcKind.Villager,
            Lines =
            [
                ("\"Two sides of this valley keep books, bearer. Ours we closed the day you paid yours out at the well; it is told as a mend now, not a hole. The hill keeps its own, if the old folk are right, and {r1.object} sits over the fires with the old voice's whole tally in hand. No one has ever come down that hill to settle one.\"", LogTone.Info),
                ("\"The stead cannot read that far, so I will. Your name is in the hill's book, in the hand of the one who inherited it. You have stood in both of this valley's memories now: the kind that can be paid, and the kind that can only be outlived.\"", LogTone.Aegis),
            ],
        },

        // The door that held (D-109): the cellar secret's consumer, the
        // roadmap's "cellar mattering in a later raid." Gated on both facts,
        // so it can only fire for a bearer the stead's own showed the door
        // and a world the raiders have already reached into. Pure perception,
        // once per world: the same raid morning every stranger walks past
        // reads differently to one inside the count.
        new Storylet
        {
            Id = "the-door-that-held",
            Trigger = StoryletTrigger.NearHouse,
            Priority = 7,
            Requires =
            [
                new FactPattern("secret", "stead_cellar"),
                new FactPattern("event", "raid"),
            ],
            Lines =
            [
                ("The lane wears the raid's morning: chaff on the wind, a byre-door leaning on its one good hinge. By the turf bank a woman folds straw bedding into a basket, small and neat, and when she sees where your eye has gone she does not move to stand in front of it. The low door is ajar to air. The children slept the burning night below, every one of them, and came up to daylight. You knew where they were. That is what the showing was for.", LogTone.Info),
                ("\"Weigh the showing again, bearer. The day they put that door in your knowing, it was an honor. Last night it was the stead's whole wager, and it held. What its own know is what a stead is, and you were inside the count when it mattered.\"", LogTone.Aegis),
            ],
        },

        // The two ledgers (D-109): the lifted purse's consumer, the
        // trust-collision beat. A clean lift has no restitution road (there
        // is no hand that knows to be paid), so the secret stands for the
        // world's life; this is where its weight is felt: the stead opens its
        // fence to a friend whose hand has already been inside it unseen.
        // Priority 6, under the hearthtale and the showing, so the trust
        // lands before the weight of it does.
        new Storylet
        {
            Id = "the-two-ledgers",
            Trigger = StoryletTrigger.Talk,
            Priority = 6,
            Requires =
            [
                new FactPattern("secret", "lifted_purse"),
                new FactPattern("regard", "friend"),
            ],
            When = g => g.TalkNpc?.Kind == NpcKind.Villager,
            Lines =
            [
                ("They lean on the fence rail to talk, easy, unhurried, the way the stead only stands with folk it counts its own. Somewhere up this lane is a purse that has been lighter since a day nobody marked, and the hand they are glad to see come up the road is the hand that lightened it.", LogTone.Info),
                ("\"I keep both of these, bearer, and I will not pretend they sit quietly together. Their ledger of you is open to the friendly page. Mine holds a line theirs is missing. Nothing needs doing; there is no door for this coin to go back through. It is only true, and only we two carry it.\"", LogTone.Aegis),
            ],
        },

        // The bolted dark (D-128): the burgled house's consumer, the stead
        // reading an entered house with no name to charge it to. The secret
        // only exists for a clean entry, so the lane's answer is spent
        // against a shape, not a face: bright iron on grey wood, a dog kept
        // in, talk that stops at one door. Forbidden once shame/housebroken
        // stands: a lane that has seen a face come out of a doorway has a
        // name for its trouble, and this beat's whole weight is the lack of
        // one. Pure perception, once per world, priority 6 in the
        // two-ledgers register: the knowing is the payload.
        new Storylet
        {
            Id = "the-bolted-dark",
            Trigger = StoryletTrigger.NearHouse,
            Priority = 6,
            Requires = [new FactPattern("secret", "burgled_house")],
            Forbids = [new FactPattern("shame", "housebroken")],
            Lines =
            [
                ("There is new iron on the lane: a bolt bright as a coin on a door whose wood went grey forty winters ago, and the dog that used to sleep in the yard now sleeps behind it. Two women pause their talk as you pass, and it is not you they lower their voices for. The house knows it was entered. The lane knows the house knows. Nobody knows one thing more, and the not-knowing is louder than a cry of thief would have been.", LogTone.Info),
                ("\"Count what one crossed sill is costing them, bearer: iron, a dog's outdoor warmth, and the ease of a lane that used to talk about weather. A stead spends hardest against the wrong it cannot name. Two of us in this valley could end the spending with a sentence, and my ledgers stay shut.\"", LogTone.Aegis),
            ],
        },

        // The heirloom missed (D-128): the fenced goods' consumer, the
        // roadmap's "heirloom missed on the lane." The cart's whole craft is
        // distance, so the stead's search aims the only ways a valley can
        // aim: down, under, behind, never up the road. Gated on live shame
        // at zero like the tale carried: "nobody here would take it" is only
        // said to a face the stead is not currently pricing. Once per world,
        // priority 6, no mechanics: the grief is spoken to the very hand.
        new Storylet
        {
            Id = "the-heirloom-missed",
            Trigger = StoryletTrigger.Talk,
            Priority = 6,
            Requires = [new FactPattern("secret", "fenced_goods")],
            When = g => g.TalkNpc?.Kind == NpcKind.Villager && g.Shame == 0,
            Lines =
            [
                ("\"You will think it a small thing.\" Their hands shape it in the air, a palm's width of nothing: a horse, or near enough, whittled by a grandfather with more love than eye. \"Off the mantel since I cannot say when. It has not walked off by itself, and nobody here would take it, so I have had the floor up twice thinking I mislaid it. It will turn up. Things do not leave a valley like this one.\"", LogTone.Info),
                ("\"It has left the valley, bearer; that was what the buying bought. They will search down and under and behind for a thing that is miles gone and merely coin now, and they will search for years, because the one direction a stead cannot imagine is away. Of everything the cart carried off, the knowing where it went is the heaviest thing still here, and you are the one carrying it.\"", LogTone.Aegis),
            ],
        },

        // The boast come home (D-111): the slew_bearer fact's consumer on the
        // stead's side. The dens howled a kill the night the bearer fell, and
        // the stead heard the name in the howling; now the killed stands at
        // the well. The stead's epistemology holds: den-talk is not believed
        // at the doors, so the truest boast the dens ever made is the one the
        // stead laughs off, and only the Aegis and the bearer hold the joke's
        // other half. Once per world, under the hearthtale's priority.
        new Storylet
        {
            Id = "the-boast-come-home",
            Trigger = StoryletTrigger.Talk,
            Priority = 7,
            Requires = [new FactPattern("nemesis", "slew_bearer")],
            When = g => g.TalkNpc?.Kind == NpcKind.Villager,
            Lines =
            [
                ("\"There was howling off the hills one night, the long kind they keep for a kill they are proud of, and the name in it was {r0.object}'s, boasting a body by the fires. And the roads still carry you, so that is what a den's word is worth.\" They say it lightly, and watch you a beat too long to mean it lightly.", LogTone.Info),
                ("\"The stead has the joke backward, bearer, and only we two are in on it. The boast was honest. What it could not count was you getting up.\"", LogTone.Aegis),
            ],
        },

        // The levy's ask (D-105): the stead's move on the tick given a voice.
        // While the levy stands a villager says what the closed larder means
        // and where its answer is taken; the mechanical answer stays on the
        // steadholder's own digit, a deliberate press, never a storylet's
        // silent hand in the bearer's purse. Gated on the live state, so a
        // lifted levy ends the asking, and not said to a barred thief: the
        // stead does not ask alms of the hand it named.
        new Storylet
        {
            Id = "the-steads-levy",
            Trigger = StoryletTrigger.Talk,
            Priority = 11,
            When = g => g.LevyStands && g.TalkNpc?.Kind == NpcKind.Villager && !g.LarderBarred,
            Lines =
            [
                ("They glance at the empty sack hanging by their door before they speak. \"You have seen the tally at the well. The lofts are on their last measure, so the stead has called it in: every door gives what it can, and the holder's board takes coin against carted grain, if any is minded to give it. No one here will ask you twice. But no one would forget it, either.\"", LogTone.Info),
                ("\"Mark this, bearer: the stead is acting now, not only being acted on. A levy is a small move as factions go. It is still a move, and it has left a space in it shaped like your hand.\"", LogTone.Aegis),
            ],
        },

        // The lights on the mound (D-106): the stead perceiving the third
        // faction, the relation matrix's second edge spoken as content. The
        // fear is gated on the live grudge, not the fact alone, so a stilled
        // mound ends the talk of it the way restitution stills the reckoning.
        new Storylet
        {
            Id = "the-lights-on-the-mound",
            Trigger = StoryletTrigger.NearHouse,
            Priority = 6,
            Requires = [new FactPattern("event", "mound_restless")],
            When = g => g.Grudge > 0,
            Lines =
            [
                ("A man stands at his door looking east past you, at the long mound on its hill, and does not pretend otherwise. \"They burned taller again last night, those lights. My grandmother had a word for a mound that will not settle, and she would not say it after dark. We tar the posts against raiders, stranger. There is no tar for that.\"", LogTone.Info),
                ("\"He is right to look east, bearer, and you know why the lights burn taller. Two ledgers now stand open against you in this valley, and only one of them can be paid in coin.\"", LogTone.Aegis),
            ],
        },

        // The lucky hand (D-108): the hearth game's winnings perceived by the
        // stead, gated on the live net like the levy's ask, so a streak given
        // back across the board ends the talk of it. The fact stays history
        // either way; the talking rides the standing luck.
        new Storylet
        {
            Id = "the-lucky-hand",
            Trigger = StoryletTrigger.Talk,
            Priority = 8,
            Requires = [new FactPattern("game", "lucky_hand")],
            When = g => g.BonesNet >= Knucklebones.TalkedAboutAt && g.TalkNpc?.Kind == NpcKind.Villager,
            Lines =
            [
                ("They nod toward the songhall before they say anything else. \"You are the one whose bones keep coming up, then. The skald has stood rounds on lighter purses than the one you have made him. Sit at that board with my husband and I will send you home in your shirt, mind.\"", LogTone.Info),
                ("\"Luck is also a kind of regard, bearer, though no ledger I keep. The stead counts what crosses a table the same as what crosses a fold wall: aloud, and to everyone.\"", LogTone.Aegis),
            ],
        },

        // The light purse (D-123): the loss ledger's consumer, the one D-108
        // wrote the fact for. Gated on the live net like the luck's talk, so
        // coin won back across the board ends the reading; the fact stays
        // history either way. Dry sympathy, no mechanics: the stead prices a
        // stranger's season aloud, which is all a stead this small ever does.
        new Storylet
        {
            Id = "the-light-purse",
            Trigger = StoryletTrigger.Talk,
            Priority = 8,
            Requires = [new FactPattern("game", "light_purse")],
            When = g => g.BonesNet <= -Knucklebones.TalkedAboutAt && g.TalkNpc?.Kind == NpcKind.Villager,
            Lines =
            [
                ("They look at your belt before your face, the way stead folk price a stranger's season. \"You are the one feeding the skald's board, then. He plays fair and wins anyway; that is what a house is. My mother said the bones only ever teach the one lesson, and it costs what it costs.\"", LogTone.Info),
                ("\"No ledger of mine, bearer, and no wrong done. But a stead this small counts what crosses a table aloud, and it has counted yours going one way. Coin spent on a lesson is only wasted if the lesson is.\"", LogTone.Aegis),
            ],
        },

        // The round remembered (D-123): the stood round's reader, in the
        // D-088 discipline: nothing gained and nothing needing to be. The
        // evening mattered because the lane says so the next day.
        new Storylet
        {
            Id = "the-round-remembered",
            Trigger = StoryletTrigger.Talk,
            Priority = 6,
            Requires = [new FactPattern("game", "round_stood")],
            When = g => g.TalkNpc?.Kind == NpcKind.Villager,
            Lines =
            [
                ("\"You were at the hall when the horns went round.\" It is not a question. \"My husband came home singing the miller's fence with the verses in the wrong order, and woke up kind about it. A stead remembers who poured, walker. It is a short list, and you are on it.\"", LogTone.Info),
                ("\"Mark the arithmetic, bearer: five coin, and the lane greets you before you speak. No rung moved and no ledger opened. Some of what a stead keeps is not kept in books.\"", LogTone.Aegis),
            ],
        },

        // The telling carried (D-088): the rumor fact's first consumer, the
        // hearthtale mattering after the hour it was told. Nothing is gained
        // and nothing needs to be: the payoff is the lane reading differently
        // because of a story, which is the whole wager of the fact graph.
        new Storylet
        {
            Id = "the-tale-carried",
            Trigger = StoryletTrigger.NearHouse,
            Priority = 6,
            Requires = [new FactPattern("rumor", "stead_hearthtale")],
            When = g => g.Shame == 0,
            Lines =
            [
                ("You pass the tarred door-posts on the lane, black to head height, and for the first time you know which winter taught the stead that trick and what the learning cost. A woman splitting kindling follows your look. She does not explain them. To you, now, she does not have to.", LogTone.Info),
                ("\"Mark that, bearer: not a word passed, and something was still said. A story told once keeps telling. You walk the same lane you first came up, and not one stone of it reads the same.\"", LogTone.Aegis),
            ],
        },

        // The showing above the showings (D-088): the own rung's narrative
        // sibling to D-087's teaching, and the regard fact's first storylet
        // consumer from the top of the ladder. The friend rung is told a story;
        // the own rung is shown a place. Priority 7, one under the hearthtale,
        // so when both rungs cross in one stroke the lesser telling leads and
        // the ladder keeps its order. Suspicion closes it on the live count,
        // same as every fence in the stead's gift.
        new Storylet
        {
            Id = "what-the-stead-keeps",
            Trigger = StoryletTrigger.Talk,
            Priority = 7,
            Requires = [new FactPattern("regard", "own")],
            When = g => g.TalkNpc?.Kind == NpcKind.Villager && g.Shame == 0,
            Lines =
            [
                ("A child is sent to fetch you, and nobody says where you are walking. Past the byres, under the turf bank, a low door you have passed a dozen times and never once marked: the stead's deep cellar. Seed-corn hung in slings, straw laid for beds, water in stone, room enough for every child in the valley to wait out a burning night. \"Now you know where it is,\" is all that is said. \"That is the whole of the showing.\"", LogTone.Info),
                ("\"Weigh what this is, bearer. Coin they lend a friend, and craft they show their own; this is neither. This is where the stead means to keep its living through the worst night it can imagine, and they have put the door of it in your knowing. There is no rung above this one.\"", LogTone.Aegis),
            ],
            Effect = g => g.World.Facts.Add("secret", "stead_cellar", g.World.SettlementName,
                "The stead's deep cellar under the turf bank: seed-corn, straw beds, and room for the children, its door shown only to the stead's own."),
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

        // Inside the ringfort's ward (D-053, tier 5+ worlds): world-texture.
        // Duty outliving its master is the deep bands' one recurring story,
        // told here in scratches on stone.
        new Storylet
        {
            Id = "the-tally-wall",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.Floor,
            Priority = 5,
            When = g => g.CurrentSite?.Kind == SiteKind.Ringfort && g.Player.Pos.X >= 14,
            Lines =
            [
                ("Inside the rampart the stone is scratched waist-high: tally-strokes in fives, weathered shallow, running on past any counting worth the name.", LogTone.Info),
                ("Watches kept, and no one ever came to collect the count. Toward the end the strokes grow smaller, as if the hand were saving stone.", LogTone.Info),
            ],
        },

        // Closing on the sword-thegn (D-058, tier 7+ forts): world-texture tied
        // to the encounter itself. The deep bands' story of duty outliving its
        // object, worn now as a fighter's drill: the even hand still keeping the
        // form long past anyone left to keep it for.
        new Storylet
        {
            Id = "the-even-hand",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.Floor,
            Priority = 6,
            When = g => g.CurrentSite?.Kind == SiteKind.Ringfort
                && g.LiveMonstersHere.Any(m => m.Kind == MonsterKind.Thegn && m.Pos.Chebyshev(g.Player.Pos) <= 2),
            Lines =
            [
                ("One of the watch does not come at you and does not run. It settles its weight, turns its point down, and waits, eyes on your hands: the old drill, never the first blow, only the answer.", LogTone.Danger),
                ("There is no one left to answer for, and it has forgotten that, or was never told. It only knows the form, and the form is patient. Give it nothing to answer, or make the answer cost more than the opening you hand it.", LogTone.Danger),
            ],
        },

        // The stead's answer to the watch standing down: the far grazing opens.
        new Storylet
        {
            Id = "the-walls-gone-quiet",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("deed", "watch_relieved")],
            Lines =
            [
                ("A drover is telling it at the well with his hat in both hands: he walked the old lanes at first light, the whole ring of them, and nothing on the walls turned to mark him.", LogTone.Info),
                ("The steadholder hears him out, then studies the hills a while. \"Good grass between those rings.\" By dusk half the stead has said it after.", LogTone.Info),
            ],
        },

        // On the holm's crown (D-057, tier 6+ worlds): world-texture. The deep
        // bands' recurring story, duty outliving its master, at its furthest
        // turn: here the duty has outlived its object too.
        new Storylet
        {
            Id = "the-bare-holm",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.Floor,
            Priority = 5,
            When = g => g.CurrentSite?.Kind == SiteKind.Leaguer
                && g.Player.Pos.X >= WorldGen.HolmMinX && g.Player.Pos.X <= WorldGen.HolmMaxX
                && g.Player.Pos.Y >= WorldGen.HolmMinY && g.Player.Pos.Y <= WorldGen.HolmMaxY,
            Lines =
            [
                ("The holm is bare rock, old nests, and a fire-ring with an age of moss in it. Whatever the leaguer was raised against left long before the grass grew over the banks.", LogTone.Info),
                ("Out across the water the slings keep their watch on you all the same. The siege goes on. There has been nothing to besiege for a very long time.", LogTone.Info),
            ],
        },

        // The stead's answer to the leaguer lifting: the low road across the fen.
        new Storylet
        {
            Id = "the-low-road",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("deed", "siege_lifted")],
            Lines =
            [
                ("A carter is telling it at the well: he took the low road past the mere, the one the old folk strike off every map they teach, and nothing on the banks rose whirring to mark him.", LogTone.Info),
                ("The steadholder chews on it a while. \"Half a day saved to the far grazings, if the low road holds.\" By evening two more carts have gone that way, loaded light, just in case.", LogTone.Info),
            ],
        },

        // Inside the songhall (D-054, every world): world-texture. The hall is
        // the counter-room to the deep bands: the one interior where nothing
        // fights back and everything keeps.
        new Storylet
        {
            Id = "the-keeping-of-songs",
            Trigger = StoryletTrigger.EnterTile,
            Tile = Terrain.Floor,
            Priority = 5,
            When = g => g.CurrentSite?.Kind == SiteKind.Songhall,
            Lines =
            [
                ("Under the turf roof the years hang in verses, cut small along the east wall, five summers to a plank. Benches worn pale where the same folk have sat since before they were the same folk.", LogTone.Info),
                ("Nothing here is bought to be carried out. What the hall takes in, it keeps, and what it keeps, it sings.", LogTone.Info),
            ],
        },

        // The stead's answer to a stone it raised on the songs' word alone
        // (D-054): the trace fact travels at the crossing, never by worldgen.
        new Storylet
        {
            Id = "the-stone-at-the-door",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("patronage", "raised_stone")],
            Lines =
            [
                ("By the well they are talking of the stone at the songhall door: raised to a walker no one here has met, on no order anyone here gave. The mason only says the work was paid for, and would not say by what.", LogTone.Info),
                ("An old man runs his thumb along the cut name and allows that it wants weather. \"Stone learns its keep,\" he says. \"Same as anyone.\"", LogTone.Info),
            ],
        },

        // The builder's echo in the founding talk (D-136, plan 2026-07 A4):
        // the one surface the legacy fact has. A built stead leaks into the
        // next world as story only, by the road the patronage traces walk;
        // nothing here stands pre-built, and the works bench opens bare.
        new Storylet
        {
            Id = "the-builders-hand",
            Trigger = StoryletTrigger.NearHouse,
            Requires = [new FactPattern("legacy", "builders_hand")],
            Lines =
            [
                ("Talk by the well, out of a drover's mouth: \"{r0.detail}\"", LogTone.Info),
                ("An old woman takes it up like a thing she has said before. \"A stead is not raised by wishing at it. Whoever that was wants finding.\" She looks at your pack a beat too long.", LogTone.Info),
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

        // The deeper grace, shared (D-060): the mender who spent an age teaching
        // that all things must be let to end meets a bearer who found the seam in
        // that law. Countered, never conceded (arc sec 7): the Unbinder does not
        // soften, but grants the cost of their own long certainty. Priority 31,
        // just above the trade shared, so if a bearer somehow banks both unspoken
        // the deeper act leads and the trade keeps for the next visit; either way
        // the sharing lands before the argument (priority 20) resumes.
        new Storylet
        {
            Id = "the-mending-shared",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 31,
            When = g => g.TalkNpc?.Kind == NpcKind.Unbinder && g.Player.SeveredRestored >= 1,
            Lines =
            [
                ("They know before you speak, the way menders always know. But this time their courtesy has gone very still, and it stays still.", LogTone.Info),
                ("\"You did not close one. You carried it whole.\" A long quiet. \"I have spent an age teaching the single lesson: that a thing must be let to end. And you have gone and found the seam in it. Not everything that cannot end wants stopping. Some were only ever waiting to be held.\"", LogTone.Info),
                ("\"I do not concede it. I will not. An ending is still what makes a life mean anything.\" They shoulder the pack. \"But I have cut a great many loose who might, in a steadier hand than mine, have been kept whole instead. I will carry that the rest of the road. It is only fair. You carry heavier.\"", LogTone.Info),
                ("\"That is nearer a blessing than the Unbinder has come in all our meeting, bearer. Mark it in no ledger. Some counts are truer left unwritten.\"", LogTone.Aegis),
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

        // The one woven in (D-060): the mythology pipe (D-013) run the one way it
        // was never aimed. The bearer mended a severed one in an earlier world;
        // now the songs of that mending run ahead down the chain, and a world the
        // bearer has never walked already carries the restored one's face. Fires
        // once, in a world later than the mending: runtime text, no worldgen read.
        new Storylet
        {
            Id = "the-one-woven-in",
            Trigger = StoryletTrigger.Arrival,
            Scope = StoryletScope.Character,
            Priority = 10,
            When = g => g.Player.SeveredRestored >= 1 && g.Cycle > g.Player.SeveredRestoredCycle,
            Lines =
            [
                ("You wake in the new world to a story you did not plant here: somewhere past the shrine a voice is half-singing about a bearer who was lost and then was not, who walks the deep roads now with their whole count carried and not one page of it torn out.", LogTone.Info),
                ("\"Hear that? We are downstream of our own mending, for once. The one you caught went into the songs, and the songs run on ahead of us, and a world we have never walked already knows a face we set right.\"", LogTone.Aegis),
                ("\"That is the whole machinery of this place turned the one way it was never aimed, bearer: a lost thing remembered forward, instead of ground under to kindle the next cruelty. Come. Let us be strangers to a world that already sings one of ours.\"", LogTone.Aegis),
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

        // ---- The asking's long shadows (D-093): vows, the face, the keepsake. ----

        // The vow of vengeance, kept: the camp's fall was sworn before it was owed.
        new Storylet
        {
            Id = "vow-kept-vengeance",
            Trigger = StoryletTrigger.DeedWritten,
            Scope = StoryletScope.Character,
            Priority = 12,
            Requires = [new FactPattern("deed", "camp_cleared")],
            When = g => g.Player.Vow == VowId.Vengeance,
            Lines =
            [
                ("You stand in the emptied camp a moment longer than the fight asked. Whatever the raiding kind took from you, this is the first payment back, and you find you can breathe around it now.", LogTone.Info),
                ("\"The vow is not spent; a vow this old has more than one den in it. But it is begun, and beginnings weigh. All is counted.\"", LogTone.Aegis),
            ],
            Effect = g =>
            {
                g.Player.Essence += 5;
                g.World.Facts.Add("vow", "vengeance_begun", g.World.SettlementName,
                    "The first den fell to a vow older than this world.");
            },
        },

        // The vow of the road's end: the first crossing is the road's first answer.
        new Storylet
        {
            Id = "the-road-answers",
            Trigger = StoryletTrigger.Arrival,
            Scope = StoryletScope.Character,
            Priority = 12,
            When = g => g.Player.Vow == VowId.Return && g.Cycle >= 2,
            Lines =
            [
                ("A second world. So the road does go on past its own horizon; you had sworn to find out, and here is the finding.", LogTone.Info),
                ("\"You vowed to walk until the road answers. This is not the answer; it is the road clearing its throat. Keep walking. I begin to think it has one.\"", LogTone.Aegis),
            ],
        },

        // The remembered face, half-seen in a stranger: texture, once ever.
        new Storylet
        {
            Id = "the-half-known-face",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 6,
            When = g => g.Player.RememberedFace.Length > 0 && g.TalkNpc?.Kind == NpcKind.Villager,
            Lines =
            [
                ("For one blink, this stranger has the wrong face: the one you carry with you, the one from before the catching. Then the blink ends and they are only themselves again, mid-sentence, unaware.", LogTone.Info),
                ("\"You looked at that one like a door you used to live behind. I felt the name go through you. I did not catch it; you held it too tight.\"", LogTone.Aegis),
            ],
        },

        // The vow of finding, fed: the search learns which way down the chain runs.
        new Storylet
        {
            Id = "the-search-carried",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 7,
            When = g => g.Player.Vow == VowId.Finding && g.Player.RememberedFace.Length > 0
                && g.TalkNpc?.Kind == NpcKind.Villager && g.Cycle >= 2,
            Lines =
            [
                ("You say the name you carry, the way you have said it in every stead. This time the villager does not shrug: they frown, slow, at something half-remembered. \"A stranger asked our gate-ward for the deep road once. Before my time. The old ones still argue about which way they went.\"", LogTone.Info),
                ("\"Down. They went down; the worlds only run the one way. Bearer: if the one you look for walks ahead of you on this chain, then every crossing is a step closer. I will count them so.\"", LogTone.Aegis),
            ],
            Effect = g => g.World.Facts.Add("face", "rumor_of_the_lost", g.World.SettlementName,
                "A stranger once asked this stead's gate-ward for the deep road."),
        },

        // The keepsake, named: the keeper of songs knows the unassuming thing on
        // sight, and the wager placed at the asking begins to pay.
        new Storylet
        {
            Id = "the-thing-named",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 12,
            When = g => g.Player.Keepsake && !g.Player.KeepsakeKnown
                && g.TalkNpc?.Kind == NpcKind.Skald && g.Cycle >= 2,
            Lines =
            [
                ("The skald's practiced patter stops mid-word. They are looking at the small worn thing you carry, and their face has gone the color of someone meeting a story in daylight. \"Where did you get that? No: forgive me. Things like that are not gotten. They are kept.\"", LogTone.Info),
                ("\"That is a shieldwright's touch-piece, bearer: the maker's own thumb-worn proof, carried against the palm through every forging. There is one in the songs, and only one, and the songs say it went down the chain with the first bearer of all. Bring it back to me when you have heard this; I must find the verses. I must find ALL the verses.\"", LogTone.Info),
                ("\"...so that is what you kept warm. Bearer, I know that weight now that it is named. My makers' hands are on it. Carry it carefully; it is older than I am, and I am not young.\"", LogTone.Aegis),
            ],
            Effect = g =>
            {
                g.Player.KeepsakeKnown = true;
                g.World.Facts.Add("keepsake", "named_by_the_skald", g.World.SettlementName,
                    "The keeper of songs knew the unassuming thing on sight: a shieldwright's touch-piece.");
            },
        },

        // The keepsake, sung: the second visit closes the wager. The reward is
        // the one thing no chest holds: the songs themselves take the story in.
        new Storylet
        {
            Id = "the-song-taken",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.Character,
            Priority = 12,
            CooldownTurns = 2,
            When = g => g.Player.KeepsakeKnown && !g.Player.KeepsakeSung
                && g.TalkNpc?.Kind == NpcKind.Skald,
            Lines =
            [
                ("The skald has been waiting. They sing it quietly, for you alone: the forge-hall in the last age, the maker who would not let a shield go out unproven, the thumb pressed to the touch-piece at every quenching: a small worn proof that someone, once, checked their work with their own hands and meant it.", LogTone.Info),
                ("\"The verses are yours now, and the thing was always yours: things like that choose their pockets. Every songhall on every road you walk will know this story from tonight. I have no better payment than that, and neither does anyone.\"", LogTone.Reward),
                ("\"So the songs carry my makers now, as they carry every bearer. Good. Someone should check that work too. All is counted, and tonight I am glad of it.\"", LogTone.Aegis),
            ],
            Effect = g =>
            {
                g.Player.KeepsakeSung = true;
                g.Player.Legend += 3;
                g.World.Facts.Add("keepsake", "sung_into_the_halls", g.World.SettlementName,
                    "The touch-piece's story entered the songs, and the songs travel.");
            },
        },

        // The keepsake unpicked: the thing waits down the chain anyway (the wager's
        // other side, promised at the asking's design). Found, it joins the thread.
        new Storylet
        {
            Id = "the-thing-found",
            Trigger = StoryletTrigger.Arrival,
            Scope = StoryletScope.Character,
            Priority = 8,
            When = g => !g.Player.Keepsake && g.Cycle >= 3,
            Lines =
            [
                ("At the shrine's foot, half-sunk in the dust of a world older than the last, lies a small thing worn smooth by older hands than yours. You did not choose it, once. It appears to have chosen anyway.", LogTone.Info),
                ("\"Pick it up. Some things are owed a pocket, and this one has waited longer than most. Do not ask me how it got here; I have asked, and it is not telling.\"", LogTone.Aegis),
            ],
            Effect = g => g.Player.Keepsake = true,
        },

        // The one who walks with you (D-097). The huntsman's debt: once the
        // stead has bled (a raid suffered, or raider blood on the bearer's own
        // hands), the woodward sets down the scales and asks to walk until the
        // camp is broken. World-scoped: each world's woodward carries their own
        // grievance, and each may take the road once.
        new Storylet
        {
            Id = "the-huntsmans-debt",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.World,
            Priority = 10,
            When = g => g.TalkNpc is { Kind: NpcKind.Villager, Id: "npc_woodward" }
                && !g.CampCleared && g.Guest is null && (g.Raids >= 1 || g.Wrath >= 1),
            Lines =
            [
                ("The woodward sets the hide-scales down mid-weighing and looks at you the way a bow is drawn. \"Those dens have had a winter of this wood's blood and my kin's peace-meat, and given back neither. You are going at them. I have watched you go at things.\"", LogTone.Info),
                ("\"I know every deer-slot and dead-fall between here and that cave mouth, and I owe that camp a debt with my own hands on it. Let me walk with you until it is paid. I will not slow you, and I do not miss.\"", LogTone.Info),
                ("\"A mortal walks beside you now, bearer. Mind it. I can catch only you.\"", LogTone.Aegis),
            ],
            Effect = g => g.CastTalkNpcAsGuest(GuestRole.Huntsman),
        },

        // The memorial thread: a guest who fell with beats enough banked is
        // remembered aloud, once, by the stead that lost them. The beloved
        // fact is written at the fall itself; this is where it is cashed.
        new Storylet
        {
            Id = "the-name-kept-warm",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.World,
            Priority = 8,
            Requires = [new FactPattern("guest-beloved")],
            When = g => g.TalkNpc?.Kind == NpcKind.Villager,
            Lines =
            [
                ("The talk finds its way, as stead talk always does, to the bench that stands unweighed. \"{r0.detail}\" The villager says it looking straight at you, and does not look away until you have heard all of it.", LogTone.Info),
                ("\"That is how the mortal ones hold a name, bearer: they pass it hand to hand so it stays warm. I keep a colder ledger. I find I do not prefer it.\"", LogTone.Aegis),
            ],
        },

        // The marks they carry (D-098 stage 2): the vision promised every scar
        // is a dialogue hook. Once per world, a villager notices what the road
        // has kept of the bearer, and does not pretend otherwise.
        new Storylet
        {
            Id = "the-marks-they-carry",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.World,
            Priority = 6,
            When = g => g.TalkNpc?.Kind == NpcKind.Villager && g.Player.Scars.Count > 0,
            Lines =
            [
                ("The villager's eyes go where stead eyes always go, to what the road has kept of you, and they do not pretend otherwise. \"You have paid for standing between us and it. We see that here. We are not in the habit of forgetting it.\"", LogTone.Info),
                ("\"They keep their own count of you, bearer. A rougher arithmetic than mine, and kinder.\"", LogTone.Aegis),
            ],
        },

        // The raiders' courser (D-100 stage 2): the second road to a beast.
        // The camp's breaking left the stolen animal loose on the land, and
        // the stead gives it over to the deed's own hand, once per world.
        new Storylet
        {
            Id = "the-raiders-courser",
            Trigger = StoryletTrigger.Talk,
            Scope = StoryletScope.World,
            Priority = 6,
            Requires = [new FactPattern("deed", "camp_cleared")],
            When = g => g.TalkNpc?.Id == "npc_steadholder"
                && g.Mount?.Kind != MountKind.Courser
                && g.Stable.All(m => m.Kind != MountKind.Courser),
            Lines =
            [
                ("\"There is one more thing out of that camp with your name on it. The raiders kept a courser, stolen off some far road, and it has been loose on the land since the fires went out. None of us dares its teeth. By the deed, it is yours to dare.\"", LogTone.Info),
                ("The courser is exactly where the steadholder said, and exactly as unimpressed. It considers you a long moment over the grass, files you under the same heading as the broken camp, and consents to be caught.", LogTone.Reward),
                ("\"Fast, that one. Stolen things usually are. It is counted to you, bearer: the stead keeps no ledger line for what it was too afraid to hold.\"", LogTone.Aegis),
            ],
            Effect = g => g.GrantTheCourser(),
        },
    ];
}
