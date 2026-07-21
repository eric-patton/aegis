using System.Text;

namespace Aegis.Core;

/// <summary>
/// The save format (D-012, D-028): a header plus the input journal. Because the
/// engine is deterministic and advances only on keys, seed + every applied key IS the
/// complete game state; loading replays the journal. The codec is pure string work,
/// no file I/O, so it lives in Core and tests cover it directly. Checkpoint
/// compression can be layered on later without changing what a save means.
/// Version history: v1 launch format; v2 when D-033 changed tier 2+ worldgen
/// (journals that crossed a waygate would replay into a different world);
/// v3 when D-034 added the Unbinder to every world at every tier (a v2 journal
/// walking their tile would open a talk menu that did not exist when it was played);
/// v4 when D-035 added template selection (tier 2+ worlds draw to choose a story,
/// so a v3 journal that crossed would replay against different content);
/// v5 when D-036 added trade (villager talk menus gained purchase entries, so a
/// v4 journal digit that merely closed a menu could now buy something);
/// v6 when D-037 added the hollow to tier 2+ worlds and new villager topics
/// (talk-menu digits shifted, and old journals never walked a world that held
/// the stone ring);
/// v7 when D-038 added the severed hermit to tier 3+ worlds (a v6 journal that
/// walked their tile kept walking; now it would open a talk menu instead);
/// v8 when D-039 added the last stair to tier 5+ worlds and the keeping menu
/// (a v7 journal deep enough would walk tiles that now hold a stair, and a
/// digit at the Hearth now answers the arc's central question);
/// v9 when D-040 added the quarry to tier 3+ worlds and weighted story
/// selection against repeating the previous world's template (a v8 journal
/// that crossed past tier 1 could replay into a world telling a different
/// story, and deep worlds now hold a site it never walked around);
/// v10 when D-041 added gear: a smith stands at every stead at every tier
/// (a v9 journal walking their tile would open a menu that did not exist),
/// deep chests hand out iron, and 'i' plus new menu digits carry meaning.
/// v11 when D-042 added use-grown skills: swings and turned blows now change
/// later damage, and 'c' (the sheet) gained meaning as a journaled key.
/// v12 when D-044 added the fallen hall to tier 4+ worlds (a v11 journal deep
/// enough would walk tiles that now hold the hall's gate, against a pack that
/// did not exist when it was played).
/// v13 when D-045 added the laying-down menu (a post-resolution bump on a
/// severed one now opens a choice where a v12 journal recorded an attack) and
/// the compounding song fact at crossings.
/// v14 when D-046 added knacks: the sheet's digits now answer threshold
/// questions (a v13 key that merely closed the sheet could now choose one
/// forever), and chosen knacks change later damage, wind, and wear.
/// v15 when D-047 added the terms of the crossing: '>' at an open waygate now
/// opens the oath menu where a v14 '>' crossed at once, digits there swear
/// terms on the next world, and oath-bound worlds generate more tenants.
/// v16 when D-048 derived standing from Legend: the welcome now sets out bread
/// at each arrival, the storied pay a coin less for it, and a fourth unbinding
/// waits at high standing, so a v15 journal would replay to different state.
/// v17 when D-049 rewove name generation: worlds draw from their own larger
/// pool and reroll against the walked list, so every generated name (and the
/// facts and songs that carry them) differs from what a v16 journal saw.
/// v18 when D-050 put a bow in the bearer's hands: 'f' arms and a direction
/// looses, both journaled, the smith's stock gained a fourth ware (shifting
/// the mending entry's digit), and every shaft changes later state.
/// v19 when D-051 grew the terms: digits 5-7 at the crossing menu now swear
/// oaths where a v18 key stepped back, and the hushed name changes prices,
/// bread, the mending count, and which facts a world is born holding.
/// v20 when D-052 added lessons: mentor menus gained teaching entries (a v19
/// digit that merely closed a menu could now buy know-how), a taught step
/// gathers gleanings a v19 walk stepped past, resting tends iron, and eating
/// can dress the wound.
/// v21 when D-053 raised the ringfort in tier-5+ worlds: deep maps changed
/// under old journals (a new gate stands where a v20 walk crossed plain
/// ground, and the gleanings settle differently around it), and the fort
/// holds a watch that did not exist when they were played.
/// v22 when D-054 raised the songhall in every stead: a door tile stands on
/// grass a v21 walk crossed plain, a skald stands where no one stood, and
/// their menu digits pledge coin no old journal could spend.
/// v23 when D-055 opened the level-4 knack questions: a sheet digit a v22
/// journal pressed to no effect now answers a question forever, and the
/// chosen knacks change later damage, wind, and wear.
/// v24 when D-056 gave the iron its verbs: 't' gained meaning, the arc and
/// the answer land blows no v23 journal recorded, and the smith stocks a
/// fifth ware, so the teaching and repair digits shifted under old fingers.
/// v25 when D-057 raised the fen-leaguer in tier-6+ worlds: a mere and its
/// works stand where a v24 walk that deep crossed plain ground, held by a
/// watch that did not exist when the journal was played.
/// v26 when D-058 wound up the heave: 'w' gained meaning (a v25 journal's
/// stray 'w' was ignored and now sets the feet), the heave lands blows no v25
/// journal recorded, and tier-7+ forts post a sword-thegn where a v25 walk
/// that deep met only carls.
/// v27 when D-070 opened the wilds in tier-2+ worlds: a game-trail stands on
/// ground a v26 walk that deep crossed plain, holding fleeing harts a v26
/// journal never met, and a felled hart pays in meat, hide, and the new
/// Hunting skill.
/// v28 when D-071 moved the woodward's teaching behind a trade bench (a v27
/// digit that bought the gleaning now opens the bench instead, and the bench's
/// own digits sell hides), and D-073 gave the hart raw meat in place of a
/// ration and added a cook entry to that bench (a v27 journal that felled a
/// hart or drove the bench replays to different state).
/// v29 when D-074 grew herbs in every world (a forest tile a v28 walk crossed
/// plain now yields a sprig on the step and grows Survival), and the bench
/// gained a herb-sale entry, so a v28 journal that walked the wood or drove the
/// bench replays to different state.
/// v30 when D-077 gave regard its first boon: the first time a stead holds the
/// bearer a friend (rung 2, reached by clearing the camp), its folk gift a coin
/// purse, so a v29 journal that ended a stead's raids replays with a fuller purse
/// from that turn on. (D-076's regard itself was cosmetic and bumped nothing;
/// this was the first faction change to touch replayed state.)
/// v31 when D-078 gave the raiders their wrath: past the dread rung (two raiders
/// slain in a world) their blows land one point the weaker, so a v30 journal
/// that fought deep into a camp replays those late blows softer and every
/// downstream state with them.
/// v32 when D-079 made the raids real: while a camp stands the raiders raid the
/// stead on a coarse tick, each raid pricing bread a coin dearer for the rest
/// of the world, so a v31 journal that bought rations in a raided world now
/// pays more for them and every coin downstream moves.
/// v33 when D-080 opened the friend's price: once a stead holds the bearer a
/// friend the steadholder takes a coin off bread, so a v32 journal that bought
/// rations after ending the raids now keeps more coin from that turn on.
/// v34 when D-081 opened the herbwife's stillroom: her talk menu carries a
/// trade digit where the wound-dressing digit sat (the dressing moved onto the
/// bench inside), so a v33 journal's digit at her menu lands differently, and
/// herbs sold to her pay five a sprig where the wood's edge paid four.
/// v35 when D-085 taught the graph the regard rungs and gated the friend's
/// hearthtale on them: a new Talk storylet enters the weighted draw, so a v34
/// journal's talk events can resolve to different winners from that world on.
/// v36 when D-086 gave the grab key its dark use: g beside an overworld house,
/// which was an inert "nothing here to take" in v35, now pilfers the door (a
/// ration taken, shame raised, a turn spent), so a v35 journal that pressed g
/// near the stead replays to different state from that press on.
/// v37 when D-087 opened the stead's teaching: at the own rung the lessons are
/// shown freely, so a v36 journal that bought a showing while the stead held
/// the bearer its own paid coin this version does not take, and replays richer.
/// v38 when D-088 answered the faction facts with content: three storylets
/// enter the draws (the named thief confronted, the hearthtale carried on the
/// lane, the cellar shown to the stead's own), so a v37 journal's talk and
/// lane events can resolve to different winners from the first eligible hook.
/// v39 when D-089 gave the factions their state vectors: raids ride the dens'
/// boldness (an emboldened raid drains double and the cull can cow a tick to
/// nothing), bread's price rides the stead's stores instead of a frozen raid
/// count, and a cleared world recovers on the tick, so a v38 journal that
/// lived past a raid or a camp-clear replays to different prices and coin.
/// v40 when D-090 opened the stillroom's craft: 'd' gained meaning (a v39
/// journal's stray 'd' was ignored and now drinks), the stillroom bench grew
/// the steeping and the stillcraft entries (digits a v39 press used to close
/// the menu), and a taught rest steeps a draught from carried sprigs.
/// v41 when D-091 taught the deep places their words: 'z' gained meaning (a
/// v40 journal's stray 'z' was ignored and now opens the workings), reading a
/// graven stone takes a word into the bearer, and a death mid-wind-up now
/// drops the held heave, so the rare v40 journal that died with a blow wound
/// up replays without loosing it at the shrine.
/// v42 when D-092 put the asking at the first wake: every new journal begins
/// with the creation answers (folk, past, shapings, thing, name), so a v41
/// journal's first key, meant as a game action, would instead answer who the
/// bearer is and every key after would land one scene out of joint.
/// v43 when D-093 lengthened the asking (burden, the bought second thing, vow,
/// and the remembered face now stand between the thing and the name) and put
/// six new storylets in the draws, so a v42 journal's answers land on the
/// wrong questions and its firings resolve to different winners.
/// v44 when D-094 gave the feet their say: 'x' gained meaning (a v43 journal's
/// stray 'x' was ignored and now shifts the footing, spending a turn under
/// live steel), and the footing moves every exchanged blow by 2 both ways.
/// v45 when D-095 gave the families their verbs: a hafted heave now sunders a
/// linden board for good and staggers a wind-up, a paid cut can carry the
/// bearer's feet off marked ground, and a bare-knuckle blow shoves the body a
/// stride back, so a v44 journal's fights replay onto different cells.
/// v46 when D-096 taught the known kinds their second moves: goblins may cry
/// the camp down on the bearer, wights breathe the grave-cold over marked
/// ground, the thegn's one marked cut lies to any read short of keen, and a
/// landed hound-lunge hauls the bearer toward the pack, so a v45 journal's
/// fights draw different dice from the first goblin lope onward.
/// v47 when D-097 opened the guest door: the huntsman's debt casts the
/// woodward as a walking companion off a talk that used to be plain, and two
/// new Talk storylets enter the eligible draws, so a v46 journal that spoke
/// to the woodward after the stead had bled replays into a different road.
/// v48 when D-098 opened the Death's Toll: clustered deaths now land scars
/// whose weight reaches combat (a crushed hand's dearer swings), the stead's
/// ledgers, and the bread price, so a v47 journal with two close deaths
/// replays into a different bearer from the scar onward.
/// v49 when D-098 stage 2 opened the cure roads: the stillroom, the smith,
/// and the skald each grew a bench entry, and a scarred talk draws a new
/// storylet, so a v48 journal's digits at those counters land differently.
/// v50 when D-099 added the calling: the stones' leanings grew a fifth word
/// (the barrow leans toward it second), so a v49 journal's stone readings can
/// grant a different working, and the cast menu carries a fifth digit.
/// v51 when D-100 stage 1 brought the stead's mule: the wood's-edge bench
/// grew an always-listed entry, mounted overworld steps can cover two cells,
/// and 'o' beside the beast means the saddlebags, so v50 journals that walked
/// those keys replay differently.
/// v52 when D-100 stage 2 gathered the roster: the wood's-edge bench grew the
/// stable digit, the steadholder's talk draws the courser storylet, a wild
/// pony stands the high ground and 'o' beside it feeds, and mortal beasts
/// bolt from uncanny mouths, so v51 journals replay differently around all
/// four.
/// v53 when D-105 gave the stead its own moves on the tick: a greedy raid
/// posts a watch that turns later raids and eats the lofts, the last measure
/// calls a levy that closes the larder and puts its answer on the larder
/// digit, and the levy's ask enters the talk draws, so a v52 journal that
/// lived past a bold raid or bought bread at a thin loft replays differently.
/// v54 when D-106 raised the third faction: grave-goods taken from an
/// unstilled barrow start the mound's grudge, riled wights strike a point
/// harder, the mound raises its slain on the tick, and a new lane storylet
/// enters the draws, so a v53 journal that robbed the barrow early replays
/// differently from that grab on.
/// v55 when D-107 put a light hand on 'p': the key now brushes an adjacent
/// villager's purse (drawing dice, moving coin, shame, and the new Sleight
/// skill) where a v54 journal's stray 'p' fell through as nothing, and the
/// first shame's way-back hint moved from the ladder to the deed that earned
/// it, so old journals holding 'p' replay into different bearers.
/// v56 when D-108 set the bones on the skald's board: the hearth digit is a
/// new always-listed entry after the deeds (a v55 key there fell through as
/// nothing and now stakes coin), the board is its own menu whose every key
/// means something, each game draws six to twelve dice, and a winning streak
/// puts a new storylet in the talk draws.
/// v57 when D-112 put the Usurped Throne in the pool: tier 2+ worlds now
/// draw their story among three templates instead of two, so a v56 journal
/// that crossed the first gate replays into worlds telling different
/// stories, with different casts and different facts, from cycle 2 on.
/// v58 when D-114 raised the valley's second faith: the harrow stands up the
/// valley with an elder and a doorward at its door, a shrinekeeper stands at
/// the stead's shrine, and the founding joins the facts, so every world
/// re-deals and a v57 journal replays differently from the first turn.
/// v59 when D-116 put the War of Faiths in the pool: tier 2+ worlds draw
/// their story among four templates instead of three, so a v58 journal that
/// crossed the first gate replays into worlds telling different stories.
/// v60 when D-117 gave storylets dialogue-tree scenes: the shuttered-window
/// beat now opens a modal scene whose digits are journaled and whose checks
/// draw on the combat stream, so a v59 journal's keys land in a scene that
/// did not exist and every roll after the first check shifts.
/// v61 when D-118 staged the faiths' claim-saying as a scene: a v60 journal
/// that reached the truth-in-hand climax replays into a modal choice that
/// did not exist, and the wielding's check draws on the combat stream.
/// v62 when D-119 staged the blight's and the throne's truth-in-hand endings
/// as scenes: a v61 journal that stilled the barrow or felled the camp with
/// the evidence read replays into a modal choice that did not exist.
/// v63 when D-120 let the unsaid cross: a crossing out of a world with a kept
/// truth presses silence facts the later worlds carry and retell, and the
/// retelling's draw shifts the storylet stream a v62 journal never saw.
/// v64 when D-121 added the Gold Rush to the pool: tier-3+ worlds draw their
/// story among five templates instead of four, so a v63 journal's later
/// worlds re-deal their spines and every draw after the selection shifts.
/// v65 when D-122 set the locked coffer in the fighting deeps: 'g' on the
/// coffer's cell now draws lock dice and moves coin and the Sleight skill
/// where a v64 journal's grab there fell through as nothing.
/// v66 when D-123 stood the room a round: a new always-listed digit after
/// the bones (a v65 key there fell through as nothing and now spends coin
/// and writes a fact), and the stood round and the light purse each put a
/// new storylet in the talk draws.
/// v67 when D-124 camped the peddler on the road: a new person stands on
/// the overworld (a v66 journal's step onto that tile now bumps to talk),
/// pilfering pockets a trinket beside the loaf, and the cart's three
/// digits (bread, hides, the fence) answer where nothing did.
/// v68 when D-125 gave every foe a second bar: paid blows, the heave, and
/// the wall now rock a guard that breaks into a stagger (a v67 journal's
/// long fights resolve differently), and 'a', once nothing, sets a parry.
/// v69 when D-126 gave the bearer the second bar back: the field's landed
/// committed blows rock the bearer's guard against Will's brim, stance and
/// held commitment tilt the pressure, and at the brim the arms refuse two
/// turns while every blow lands deeper (a v68 journal's hardest fights
/// resolve differently, and a refused swing spends no turn where one spent).
/// v70 when D-127 taught the hand to cross the sill: 's', once nothing, now
/// slips a stead door's latch, draws Sleight dice, and moves coin, shame,
/// heirlooms, and skill where a v69 key fell through as nothing.
/// v71 when D-129 closed the guard war: the carl's board-check adds a draw
/// beside every adjacent carl (a v70 journal's fort fights re-deal from
/// there), and the drilled thegn answers the met parry with the bind.
/// v72 when D-130 gave the pool its sixth spine: the Long Siege binds to the
/// fen-leaguer, so tier-6+ worlds draw their story from six candidates where
/// a v71 journal drew from five, and every late world's cast re-deals.
/// v73 when D-132 gave the world a calendar: every valley's hard winter thins
/// the stores on its seed-drawn tick (prices, levies, and storylet draws all
/// move under a v72 journal), and the cull's muster adds a raiding night no
/// v72 world ever kept.
/// v74 when D-133 dealt the season its deck: tick nights may now deal a stead
/// event from a per-world stream (stores, prices, and the calendar all move
/// under a v73 journal), so every replay's season re-deals.
/// v75 when D-134 raised the stead's works: digits at the steadholder's board
/// that fell dead under a v74 journal now open the works bench and move coin,
/// facts, and the raids' own arithmetic.
/// v76 when D-135 grew the ladder's second rung: two bench digits a v75
/// journal never held now raise the stillroom's wing and the smithy, deepen
/// the satchel's vials, file wear off iron, and feed the Smithing craft.
/// v77 when D-138 opened the east road: '>' at a mouth a v76 journal never
/// held now walks a second overworld, 'm' makes camp and passes real nights,
/// and the wayhouse's digits move coin, rations, and rest.
/// v78 when D-139 gave the granary its flood teeth and moved the season's
/// news to the shrinekeeper's door: a washout under a standing granary now
/// takes nothing, and the talk digits at the villagers' and keeper's doors
/// sit differently under a v77 journal wherever season news stood.
/// v79 when D-140 opened the market town at the east road's end: a gate a
/// v78 journal never held now walks the town's lanes, and its stalls' digits
/// move coin, goods, and the Commerce craft.
/// </summary>
public static class SaveCodec
{
    public const int Version = 79;
    private const string Magic = "AEGIS-SAVE";

    public static string EncodeHeader(ulong seed) => $"{Magic} v{Version} seed:{seed}";

    /// <summary>Parses full save-file content into seed + key journal. Throws on malformed or wrong-version content.</summary>
    public static (ulong Seed, string Keys) Parse(string content)
    {
        var lines = content.Split('\n');
        string header = lines[0].TrimEnd('\r');

        var parts = header.Split(' ');
        if (parts.Length != 3 || parts[0] != Magic)
            throw new FormatException("Not an Aegis save file.");
        if (parts[1] != $"v{Version}")
            throw new FormatException($"Save is {parts[1]}; this build reads v{Version}. No migration exists yet.");
        if (!parts[2].StartsWith("seed:") || !ulong.TryParse(parts[2]["seed:".Length..], out ulong seed))
            throw new FormatException("Save header has no readable seed.");

        var keys = new StringBuilder();
        for (int i = 1; i < lines.Length; i++)
            keys.Append(lines[i].TrimEnd('\r'));

        return (seed, keys.ToString());
    }

    /// <summary>Rebuilds a game by replaying the journal. Deterministic: this IS loading.</summary>
    public static Game Replay(ulong seed, string keys)
    {
        var game = new Game(seed, firstWake: true);
        foreach (char key in keys)
        {
            if (!game.Running) break;
            game.ApplyKey(key);
        }
        return game;
    }
}
