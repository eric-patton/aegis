# Dev Harness: Headless Sim and the Pilot Channel

How to run, drive, and observe Aegis without touching the game window. Design rationale is logged as D-027 and D-028 in `../design/decisions.md`.

## Run modes

```
aegis                            play normally in the terminal
aegis --seed 42                  play a specific world
aegis --save myslot              play in a named save slot (load-or-create)
aegis saves                      list save slots
aegis --pilot                    play, with the control channel open alongside
aegis --headless --pilot         no console at all; driven entirely via the pilot
aegis --session name             name the pilot channel (default: "default")
```

## Saves (D-012 / D-028)

A save is the seed plus the input journal: because the engine is deterministic and
advances only on keys, replaying every applied key IS loading. `--save slot` appends
each key to `%LOCALAPPDATA%\Aegis\saves\slot.aegis` and flushes immediately, so death
consequences are durable the instant they happen (autosave-on-death by construction)
and quitting at any moment loses nothing. `--save-dir` overrides the directory
(useful for tests). A slot open in a running game is locked against double-opening;
`aegis saves` can still list it. The format is versioned; a version bump invalidates
old saves until a migration exists. Checkpoint compression is a later optimization,
not a format change.

## NG+ crossings (D-011 / D-029)

Each world has a waygate (`O` on the map), shut until the camp is cleared, then `>` on
it crosses: attributes/essence carry, coin converts to Legend, the next world generates
one Hostility Tier deeper from a seed derived off the master. A crossing is just more
keys in the journal, so saves span world boundaries with no extra machinery. The pilot
status line shows `cyc N leg N` once either is nonzero; `state` carries `cycle`, `tier`,
`legend`, and `gateX/gateY`.

## Sites and tier bands (D-033)

Enterable places are `Site`s (goblin camp everywhere; the barrow, `n` on the map, at
tier 2+ only). `state` carries `barrowX/barrowY` (`-1` when the world has none),
`barrowCleared`, and `currentSite`. Wights step only on even turns, drain stamina on
hit, and telegraph a heavier blade; kite them and dodge sideways. Clearing the barrow
is optional: only the camp deed opens the waygate. Worldgen for tier 2+ changed with
D-033, so saves are format v2 (v1 journals that had crossed would replay wrong and
are refused).

## World stories and template selection (D-035, weighting D-040)

Each world tells one story, chosen at worldgen among eligible templates (tier 1:
always the Raided Stead; tier 2+ worlds with a barrow may draw the Creeping Blight
instead). `state` carries `storyTemplate`. Blight worlds: the plea comes from the
cast "afflicted" villager, evidence deep in the barrow (x >= 19) changes the ending
the deed fires, and saves are format v4 (the tier-2+ selection draw). Since D-040
each crossing hands the finished world's story id into the next world's draw and
the selection halves that template's weight: repeats still happen, roughly one
world in three instead of one in two, and worlds generated from a direct `--seed`
(no previous story) draw exactly as before.

## The quarry and the graven men (D-040)

Tier 3+ worlds hold the old quarry (`x` on the map; `state` carries
`quarryX/quarryY`, `-1` when absent, and `quarryCleared`): one open pit with
freestanding pillars, tenanted by graven men (`m`). They stand as statues until
the bearer comes within five tiles in their line of sight (or strikes one), then
hold their ground and hurl stone at telegraphed cells out to nine tiles wherever
line of sight allows. Pillars block the sight line, a moving bearer is never hit
by a throw (it lands on the cell, one turn later), and adjacent they trade a
heavy telegraphed fist worth dodging like the barrow blade. They walk a step
every third turn, so breaking line of sight and kiting both work. Clearing the
pit is optional (the waygate stays camp-keyed) and writes its own deed. Saves
are format v9 (tier-3+ worldgen changed, and crossed worlds draw stories with
the D-040 weighting).

## The arc ladder and the hollow (D-037)

Tier 2+ worlds hold the hollow (`o` on the map): a stone ring with a single
severed one. It pursues at full speed with proper pathing, telegraphs a
sundering cut (always dodge it), and its bare touch drains 1 essence along with
hp; felling it pays 15 essence and no coin, and it is the hardest optional
fight in the band. `state` carries `hollowX/hollowY` (`-1` when absent),
`hollowCleared`, and `arcProgress`: a comma list of spoiler-free rung flags
(`truth`, `guilt`, `vision`, `ledger`, `peace`, `cost`, `tierN`, `commission`)
that live tests should assert instead of echoing story prose. Story beats gate
on earlier flags, not cycle counts.

Cycle-4 content (D-038): tier 3+ worlds also cast a severed hermit (`p` in
magenta, camped like the Unbinder; `state` carries `severedNpcX/severedNpcY`,
`-1` when absent). Their beat, the hollow-threshold witness scene (fires on
stepping onto the `o` tile, no entry needed), the Unbinder's second reveal,
and the commission crossing all gate on the ledger flag and each other. Saves
are format v7 (tier-3+ worldgen changed).

Cycle-5 content (D-039): tier 5+ worlds hold the last stair (`v` on the map;
`state` carries `thresholdX/thresholdY`, `-1` when absent). The door at its
foot opens only once the commission flag is held; inside, walking the corridor
fires the approach beats and stepping to the site's heart opens the keeping
menu (`state` carries `inThresholdMenu`; `1`/`2` answer, anything else steps
back, and the menu never reopens once answered). The answer appends `kept` or
`refused` to `arcProgress` and swaps voice registers permanently (death,
crossing, and rest lines; a new Unbinder topic). The two answers are
mechanics-identical by design and by test; assert `arcProgress`, never prose.
Saves are format v8 (tier-5+ worldgen changed and menu digits gained meaning).

## Gear and the smith (D-041)

Every stead has a smith (`p` beside the houses; `state` carries `smithX/smithY`).
Their talk menu is their own: two topics, then the plain stock (woodsman's axe,
quilted jack, riveted shirt), each entry printing price and any requirement, then
"Have your gear seen to" whenever anything you own carries wear. Sold pieces stay
listed as owned so menu digits never shift. Deep chests hold the other iron: the
barrow's grave-iron blade and the quarry's carver's maul (once per character; a
twin is left where it lies). `i` opens the pack anywhere: digits wield or wear,
`*` marks held pieces, and requirements you miss print with a `!`. Weapons add to
every full swing and wear per swing; armor thins every hit (never below 1) and
wears per turned blow; a worn piece gives half its good until repaired. Repair is
priced per item off its value (half at full wear). Under-requirement use is
penalized, never blocked: half the bonus and an extra stamina per swing. Gear is
banked on death (the remnant takes only coin and essence) and crosses whole.
`state` carries `weaponId/weaponWear`, `armorId/armorWear`, `packGear`,
`repairPrice`, and `inGearMenu`. Saves are format v10 (a person and a key gained
meaning at every tier).

## Skills and the sheet (D-042)

Four use-grown skills: Blades, Hafted, and Brawling split the melee swing by
what is in hand (axe and maul are Hafted, the grave-iron blade is Blades, bare
fists are Brawling); Warding is armor-craft, fed only by blows the worn iron
turns. Counted uses are the only state; levels are derived (thresholds 8, 20,
36, 56, 80...), and each level pair adds +1 (damage for the family swung,
absorb for Warding while armor is worn). Free actions teach nothing: a winded
feeble swing and an unturned raw-1 bite both count for nothing. `c` opens the
sheet anywhere (turn-free): seven attributes and four skills with progress.
Skills are banked on death and cross whole. `state` carries `skills` (a comma
list of `name:level:uses`) and `inSheetMenu`. Saves are format v11 ('c'
gained meaning, and swings now change later damage).

## Trade and provisions (D-036)

The steadholder sells rations (talk menu, last entries after the topics; the menu
stays open across purchases) and the herbwife dresses wounds for coin priced by
remaining convalescence. `e` eats a carried ration anywhere: +6 hp, +3 stamina,
takes a turn. Rations (cap 5) survive death and crossings; coin does neither.
Ration price reads the fact graph: 6 coin while a blight story stands uncompleted,
4 otherwise. `state` carries `rations`, `rationPrice`, and `mendPrice` (0 when
whole). Purchase keys are journaled, so saves are format v5.

## The Unbinder and respec (D-034)

Every world casts a wandering mender (`p`, camped away from the stead; villagers'
"The wanderer" topic gives the direction). Bump to talk; the last menu entry opens
the unbind menu: `1-7` loosens a raised attribute, refunding exactly what re-buying
it costs, three times per world (refreshed at each crossing). `state` carries
`unbinderX/unbinderY`, `unbindingsLeft`, and `inUnbindMenu`. Worldgen changed at
every tier, so saves are format v3.

Build and run from the repo root:

```
dotnet build
.\src\Aegis.Cli\bin\Debug\net10.0\aegis.exe --headless --pilot --seed 42
```

Native (AOT) publish, from a shell where `vswhere.exe` is on PATH (the VS installer
does not add it; it lives in `C:\Program Files (x86)\Microsoft Visual Studio\Installer`):

```
dotnet publish src\Aegis.Cli\Aegis.Cli.csproj -c Release -r win-x64
```

This produces a self-contained ~3 MB `aegis.exe` under `bin\Release\...\publish\` whose
sim output is byte-identical to the Debug build (verified on seed 42).

## The pilot channel

A named-pipe control server (`aegis.pilot.<session>`) that a shell, script, or agent uses to drive a live game. The game renders to an in-memory frame; the pilot serves that exact frame as text, so observing the game never involves screenshots or window focus.

The TUI itself scales to the terminal window (the map viewport flexes; sidebar and log stay fixed; below 80x24 the layout crops), and repaints on resize. **The pilot always renders at the fixed 80x24 baseline regardless of any window**, so agent-visible screens and screen-based tests stay deterministic.

Client commands (each connects, acts, prints, exits):

```
aegis pilot screen               print the current 80x24 screen as text
aegis pilot keys "kkllj>"        inject key presses; prints resulting screen + status line
aegis pilot state                print the full game state as JSON
aegis pilot ping                 check a session is alive
aegis pilot quit                 stop the game
aegis pilot ... --session name   target a named session
```

Keys are the same everywhere (TUI, pilot, sim): `hjkl`/`yubn` move, `.` wait, `g` grab, `>`/`<` enter/exit, `q` quit. Arrow keys work in the TUI and map to `hjkl`. Moving into a villager (`p`) opens the talk menu; digits ask topics and any other key closes it (menu keys are journaled like all others).

Two usage patterns:

- **Shared session**: the user runs `aegis --pilot` in their terminal and plays; an agent connects to the same session to observe (`screen`, `state`) or assist (`keys`). Both see the same game.
- **Headless session**: `aegis --headless --pilot --session x` runs the game invisibly; everything happens through the pilot. This is the default way for an agent to playtest.

Wire protocol (for non-CLI clients): one JSON object per line over the pipe, e.g. `{"cmd":"keys","keys":"llj"}` returns `{"ok":true,"screen":[...],"state":{...}}`. Commands: `ping`, `screen`, `state`, `keys`, `quit`. UTF-8 without BOM; a BOM written into a fresh pipe deadlocks both ends (learned the hard way; see PilotServer.cs).

Troubleshooting: set `AEGIS_PILOT_TRACE=1` to get stderr traces from both server and client.

## Headless simulation

```
aegis sim --seed 42 --keys "llll....jjjj" [--quiet]
```

Builds the world, applies the key script synchronously, prints JSON: seed, keys applied, the full message log (`--quiet` omits it), and the final state snapshot. Deterministic: same seed and keys always produce byte-identical results. Intended for balance sweeps, regression checks, and CI.

## Journey and pacing diagnostics

```
aegis journey --seed 42 --cycles 12 --emit-keys
aegis journey --seed 42 --cycles 12 --json
```

The journey drives ordinary player keys through repeated crossings. `--emit-keys` adds
the exact replay journal as the final line. The JSON form includes the same run measures
plus full crossing records.

D-160's pacing diagnostics are deliberately harness-only. The prose report separates
natural deals, Press-forced deals, Space suppressions, protected and empty Press calls,
spent Space episodes, quiet and deal-gap bounds, cadence-roll count, and per-card natural
versus forced arrivals. The JSON adds those aggregates plus one record for every coarse
tick, including the call fixed before the night, cadence result, protected claim, outcome,
and dealt card key. Ordinary game presentation exposes none of this editorial machinery.

## Worldgen and prose curation

`worldgen` batch-generates each requested world twice, compares the complete WorldEval
digest, and returns exit code 2 for generator impurity or a hard prose-catalog failure.
The default run covers 30 seeds across tiers 1-8.

```
aegis worldgen
aegis worldgen --json
aegis worldgen --dump
aegis worldgen --dump --json
```

The ordinary report charts generated-world measures and the family-aware prose audit.
Its JSON shape includes per-kind surface counts, family coverage, failures, warnings, and
authored-versus-observed variation. `--dump` is the readable generate-then-curate view,
grouped by family and source metadata. Combining `--dump --json` emits one compact
`ProseDumpRecord` per line for scripts. Fixed legacy prose stays visible but is not a hard
variation failure. Invalid templates, duplicate ids, unmet budgets, unresolved values,
missing declared variable content, and generator impurity are hard failures. Distribution
skew, fixed-heavy categories, and cross-family collisions remain advisory warnings.

## Determinism contract

- All randomness derives from the master seed via the seed tree (`Rng.cs`); subsystems never share streams.
- Game state advances only on commands; there is no wall-clock time in the engine.
- The tests in `tests/Aegis.Core.Tests` pin this down: same seed = identical world and identical scripted runs.

## Architecture notes

- `Aegis.Core` has zero console or I/O dependencies. `Presenter.Render(game)` produces a `Frame` (cell grid); `Frame.ToTextLines()` is what the pilot serves.
- `Aegis.Cli` owns the frontends: `ConsoleRenderer` (VT diff renderer), `ConsoleInput`, `PilotServer`/`PilotClient`, `SimRunner`.
- The engine runs single-threaded: console keys and pilot requests are marshalled onto one channel (`GameHost`), so concurrent input sources cannot corrupt determinism.
