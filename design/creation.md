# Character creation (D-092 stage 1, D-093 stage 2; fork 4 lane 1)

**Status: both stages shipped.** Settled with the user in Q&A 2026-07-19. Stage 1
(D-092) is the in-fiction creation scene (folk, background, swaps, precious
thing, name, random bearer); stage 2 (D-093) adds the burdens, the vows, the
remembered face, and the keepsake's storylet thread.

Supersession note: D-017 proposed familiar races (dwarf, elf, orc-ish). The user
chose original world-grown folk instead; D-017's *structure* (fixed anchors,
per-world recultured, small tilts plus one qualitative trait, humble starts per
D-005) is kept, its example roster is replaced.

## 1. The scene

No menu screen. At the first wake (cycle 1 constructor only, never at
crossings), before the bearer takes a step, the Aegis asks who it has caught.
Each question is a numbered choice in the existing dialog grammar; every answer
key is journaled like all input, so saves replay creation exactly. The scene is
turn-free; the world is already generated (worldgen stays character-blind).

Order: folk, background, shaping (swaps), precious thing, name. At the folk
question, `0` is "let the shrine decide": the whole bearer (all five answers)
is rolled from `SeedTree.Derive(World.Seed, "bearer")`. The pilot always takes
this door, so journeys exercise the roll path and stay seed-deterministic.

## 2. The folk (five, original, world-grown)

Fixed anchors; each generated world rerolls their cultures and standing
(D-017's structure). Each folk = one attribute tilt (+1/-1, applied before
swaps) plus one qualitative trait. All fiction grows from the Ledger canon
(worlds kindled on a Chain; the Shieldwrights' dead order; the old buried
things; the wild edges of freshly kindled lands).

| Folk | Tilt | Trait |
|---|---|---|
| **Steadfolk** | none | *Kith and coin*: a third free swap, and 10 coin from home. |
| **Emberwrought** | +1 Mind, -1 Vigor | *The kindled spark*: +1 MaxFocus, always. |
| **Cairnborn** | +1 Will, -1 Grace | *Keepers of the old dead*: reads come one tier keener (ReadKeen +1). |
| **Heathborn** | +1 Grace, -1 Might | *The wild feeds them*: harvests yield one more (hide and sprig alike). |
| **Wrightkin** | +1 Might, -1 Wits | *Hands that remember the craft*: carried gear wears half as fast. |

Fiction sketches (the in-scene blurbs carry these; one to two lines each):
- **Steadfolk**: the common grain of the kindled worlds; stead-raised, kin-held,
  nothing strange in their blood and everything possible in it.
- **Emberwrought**: lines touched, generations back, by worlds kindled too near
  the Hearth; a warmth behind the eyes that graven words answer.
- **Cairnborn**: the folk who keep barrows and speak for the buried; they grow
  up knowing the shapes of old dead things and are slow to startle.
- **Heathborn**: edge-dwellers of freshly kindled wilds, hunters and foragers
  born where the world is still deciding what it is.
- **Wrightkin**: descendants of the Shieldwrights' bond-crafters; the order is
  ash, but the hands remember, and iron lasts longer in them.

## 3. The backgrounds (seven)

Each grants level 1 in one skill (banked uses, so growth continues naturally)
plus one small concrete extra, and writes a `past` fact the world can react to
(topics now, stage 2 storylets later).

| Background | Skill L1 | Extra |
|---|---|---|
| Soldier | Blades | starts with a `quilted_jack` (half-worn, serviceable) |
| Poacher | Ranged | starts with a `hunting_bow` |
| Hedge-healer | Survival | 3 sprigs in the wallet; lettered (Lore 1, D-148) |
| Smith's-hand | Warding | one mending free at any world's smith (spent once ever) |
| Scribe's-ward | Spellcraft | a graven-stone rumor surfaced at wake; lettered (Lore 1, D-148) |
| Wayfarer | Hunting | 2 rations from the road |
| Oathbreaker (dark past) | Blades **and** Hunting | Shame starts at 1: a stained name the stead already half-knows |

## 4. Shaping (paired swaps)

Up to two swaps (Steadfolk: three): each swap is +1 to one attribute, -1 to
another, chosen in two keys (raise, then pay). No attribute below 3 or above 7
at creation. Presence may be chosen (it is currently inert; it will not be
forever). Applied after the folk tilt.

## 5. The precious thing (one; keepsakes are keepsakes)

Soul-bound: wakes at the shrine with the bearer, never dropped at death.
Words already persist as knowledge; the physical things are flagged keepsake.

1. **A known word**: the spark, carried from wherever the bearer's road began;
   Focus bar shown from turn one, pool full. The Aegis's word-warning line
   moves here.
2. **Fine arms**: a `grave_iron` blade, above anything the early smith sells.
3. **A craft kit**: a brewing satchel: the Stillcraft lesson already taught,
   and 6 sprigs.
4. **A heavy purse**: 25 coin.
5. **An unassuming thing**: a small object the scene refuses to explain.
   Stage 2 gives it its keyed storylet thread (an NPC who recognizes it, a
   quest, a unique reward) and the NG+ placement when unpicked. Stage 1: it is
   carried, described once, flagged as a fact, and does nothing. The scene is
   honest that it seems worthless; picking it is a wager.

## 6. The name

Free-text entry (letters and spaces, cap 14; `-` erases one, `.` seals; all
plain printable keys, so the journal's line format never meets a control
character). An empty name draws one from the folk's stream. The sidebar header and sheet
title carry the name; the world's mouths still say "bearer" and "stranger"
(names are for the log and the sheet first; NPC line banks adopt it later).

## 7. Persistence and verification

- Creation runs once, at cycle 1. Folk, background, name, keepsakes, and words
  cross death and the waygate whole. Swapped attributes are just attributes.
- Save Version 42 (creation keys prefix every new journal; old journals break
  per standing policy, no migration).
- `new Game(seed)` keeps the legacy instant wake for the 400-test suite;
  engine entry points (TUI, sim, journey, save replay) construct with the
  first-wake flag so real play and replay always include the scene.
- Journey/sim baselines are re-recorded (pilot answers `0`; rolled bearers
  change combat outcomes by design). Recorded at D-092: seed 2024 x12 reaches
  cycle 13, turns 10521, deaths 5, regard 5, wrath 6, raids 31, keys 10925
  (twin runs hash-identical, sim replay exact); sweep 1/7/99/88888 x8 all
  finish (turns 6651/6845/7418/6590, deaths 6/6/12/6, raids 11/21/16/17).

## 8. Stage 2 (D-093, shipped)

The asking grows three questions between the thing and the name, and the save
moves to v43 (the answer keys shift; six storylets join the draws).

- **Burdens** (optional, one): each buys a second precious thing (nothing is
  taken twice; the scene refuses and re-asks). *An old wound* (MaxHp -2,
  always), *a hunted past* (every world's raider wrath wakes at 1, reseeded at
  each crossing), *a marked face* (every stead's suspicion wakes at 1; stacks
  with the oathbreaker's stain).
- **Vows** (optional, one): *vengeance* (kept at the first camp cleared, once
  ever: +5 essence and a `vow` fact), *finding* (needs a face: an unnamed one
  is drawn from its own stream; from cycle 2 a villager's half-memory feeds
  the search and writes a `face` fact), *the road's end* (answered at the
  first crossing, once ever).
- **The remembered face** (optional): a typed name; once ever, a villager
  wears it for a blink (texture). Real casting into factions stays tracked.
- **The keepsake's thread**: from cycle 2 the skald recognizes the unassuming
  thing on sight and names it (spoiler content lives in the storylet lines and
  here only by id: `the-thing-named`); a second visit closes the wager
  (`the-song-taken`): the story enters the songs, +3 Legend, the one reward no
  chest holds. Unpicked, the thing waits down the chain (`the-thing-found`,
  arrival from cycle 3, priority-tolerant: if another beat wins a world's
  arrival draw it simply waits for the next) and joins the thread when taken.
- **Fate rolls it all**: the fate door now also rolls burden (and its second
  thing), vow, and face from the same stream.

Verified at D-093: 427 tests green; twins hash-identical; sim replay exact
(seed 2024's fated bearer rolls a hunted past: wrath peaks 7); sweep 1/7/99/
88888 x8 all finish (turns 6651/6899/7228/6678, deaths 6/6/8/8).

## 9. Still tracked beyond stage 2

- NPC line banks adopting the bearer's name; folk-aware recultured societies
  in worldgen (D-017's per-world cultures); folk/past-keyed storylet texture;
  the remembered face truly cast into a faction NPC; deeper keepsake content
  past the song (the verses hint at more chain below).
