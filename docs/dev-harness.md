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

## World stories and template selection (D-035)

Each world tells one story, chosen at worldgen among eligible templates (tier 1:
always the Raided Stead; tier 2+ worlds with a barrow may draw the Creeping Blight
instead). `state` carries `storyTemplate`. Blight worlds: the plea comes from the
cast "afflicted" villager, evidence deep in the barrow (x >= 19) changes the ending
the deed fires, and saves are format v4 (the tier-2+ selection draw).

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

## Determinism contract

- All randomness derives from the master seed via the seed tree (`Rng.cs`); subsystems never share streams.
- Game state advances only on commands; there is no wall-clock time in the engine.
- The tests in `tests/Aegis.Core.Tests` pin this down: same seed = identical world and identical scripted runs.

## Architecture notes

- `Aegis.Core` has zero console or I/O dependencies. `Presenter.Render(game)` produces a `Frame` (cell grid); `Frame.ToTextLines()` is what the pilot serves.
- `Aegis.Cli` owns the frontends: `ConsoleRenderer` (VT diff renderer), `ConsoleInput`, `PilotServer`/`PilotClient`, `SimRunner`.
- The engine runs single-threaded: console keys and pilot requests are marshalled onto one channel (`GameHost`), so concurrent input sources cannot corrupt determinism.
