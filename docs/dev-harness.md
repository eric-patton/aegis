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

Build and run from the repo root:

```
dotnet build
.\src\Aegis.Cli\bin\Debug\net10.0\aegis.exe --headless --pilot --seed 42
```

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

Keys are the same everywhere (TUI, pilot, sim): `hjkl`/`yubn` move, `.` wait, `g` grab, `>`/`<` enter/exit, `q` quit. Arrow keys work in the TUI and map to `hjkl`.

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
