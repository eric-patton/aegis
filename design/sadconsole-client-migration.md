# SadConsole client migration

Status: Approved for implementation

Roadmap card: V1-10

Governing decisions: D-175 and D-176

Spike evidence: `../artifacts/sadconsole-spike/RESULTS.md`

## Goal

Replace the terminal-owned player interface with an Aegis-owned SadConsole
window before the 1.0 release candidate is rebuilt. The deterministic engine,
presentation model, save journal, and verification tools remain intact.

The client must look and behave consistently across supported Windows systems.
It must also remain fully observable and controllable by an agent without
moving focus, synthesizing operating-system input, or interrupting the user's
other work.

## Locked direction

- SadConsole 10.10.1 with MonoGame DesktopGL 3.8.4.1 is the 1.0 player host.
- `Aegis.Core` remains I/O-free and has no SadConsole or MonoGame reference.
- `Frame` and `Presenter` remain the canonical player presentation.
- Aegis owns the font, RGB palette, logical grid, window, and resizing.
- Windows x64 Native AOT remains the release target.
- The existing pilot protocol remains the exclusive automation surface.
- Pilot input never uses `SendInput`, window messages, focus changes, or screen
  automation.
- The old terminal client remains available as a comparison surface until the
  SadConsole client is verified. It is not the final 1.0 player.
- The existing 1.0 candidate is superseded. Manual release signoff restarts
  from a newly packaged SadConsole candidate.

## Recommended project boundary

### `Aegis.Core`

No dependency changes. It continues to own:

- Deterministic world and game state
- Canonical character commands
- `Game.ApplyKey`
- `Frame`, `Presenter`, and snapshots
- Save replay semantics

### `Aegis.Host`

Add a small frontend-neutral class library extracted from the current CLI host.
It owns:

- The single-threaded game-session queue
- Save loading, append, and slot locking
- Pilot request and response types
- The named-pipe server and client
- Canonical input batching and response completion
- Fixed-size text and structured-frame observations

It must not reference SadConsole, MonoGame, or `System.Console`.

### `Aegis.Client`

Add a Windows `WinExe` project that produces the shipping `aegis.exe`. It owns:

- SadConsole and MonoGame startup
- Window title, size, resizing, and shutdown
- Font and palette assets
- SadConsole keyboard and mouse translation
- Copying `Frame` cells into the SadConsole surface
- User-facing settings that are local presentation state only

### `Aegis.Cli`

Retain the console project for developer and release tools:

- `pilot`
- `sim`
- `journey`
- `worldgen`
- Release diagnostics
- The legacy terminal player during migration

The packaged tool executable should be named `aegis-tools.exe` so it cannot be
confused with the player. The final package decision may omit the legacy player
entry point while retaining the source and automated comparison tests.

## Rendering contract

- Canonical logical grid: 120 columns by 40 rows.
- Default font: the packaged IBM 8x16 tiled font proven by the spike.
- Default client area: 960 by 640 pixels before operating-system scaling.
- Resize mode: `Fit`, preserving the entire logical grid.
- Letterboxing is allowed. Cropping required information is not.
- Aegis maps every `Hue` to an exact RGB foreground and background color.
- No color is inherited from the terminal, Windows theme, or desktop theme.
- Glyph, placement, and text remain the primary information channels. Color is
  supplemental, so color-vision differences do not hide required state.
- The renderer updates only after a processed input, resize, exposed redraw, or
  explicit presentation refresh. It does not advance game time.
- The window may redraw at any frame rate without changing deterministic state.

The initial palette and 120 by 40 layout are the spike-proven baseline. Palette
and spacing can be tuned during visual review without changing engine behavior.

## Input contract

All game input becomes a canonical character before it reaches the engine.

Physical input path:

```text
SadConsole key transition
  -> client key mapper
  -> host queue
  -> Game.ApplyKey
  -> save append
  -> render
```

Pilot input path:

```text
named-pipe keys request
  -> host queue as one atomic batch
  -> Game.ApplyKey for each canonical character
  -> save append for each accepted character
  -> render
  -> response with resulting screen and state
```

The two sources therefore share the same engine and save path. Neither may call
the engine from a background thread.

The existing canonical character bindings remain unchanged. SadConsole adds
only physical aliases:

- Arrow keys map to the four cardinal movement characters.
- Escape maps to the established close or quit character.
- Printable keys preserve their character and case where case has meaning.
- Shifted movement remains distinct from ordinary movement.
- Key repeat is allowed only through ordinary SadConsole key-repeat events. The
  client must not invent additional repeats.
- Mouse support may select an already visible menu entry or map cell only when
  it resolves to an existing canonical character. Mouse input must not create a
  second command language for 1.0.

If implementation changes the meaning of any journaled character, save v99 is
invalid and the change must stop for a new decision, save-version bump, and full
engine sweep. The intended migration does not change key meaning.

## Focus-free pilot contract

The pilot is opt-in through `--pilot` and keeps the named session behavior.
Visible and headless sessions are both supported.

Required commands:

- `ping`: prove the session is alive.
- `screen`: return the fixed canonical text frame.
- `state`: return the structured snapshot.
- `keys`: apply one atomic canonical-key batch and return resulting screen and
  state.
- `frame`: return the canonical 120 by 40 cell grid with glyph and resolved RGB
  colors for focus-free visual inspection.
- `quit`: request an orderly client shutdown.

Security and concurrency:

- The Windows pipe uses current-user-only access.
- A session name is validated and bounded before it becomes a pipe name.
- The server never accepts file paths or arbitrary commands.
- A request has a bounded completion timeout.
- One `keys` request is atomic relative to physical key messages.
- Separate requests and physical keys retain queue arrival order.
- Pilot provenance is diagnostic only. It never enters the save journal or
  changes game behavior.

Agent operating rule:

- Use the pilot for every game interaction and observation.
- Do not use `cwin`, Computer Use, `SendInput`, UI Automation, or foreground
  keyboard events to play the game.
- Use a headless pilot session for automated tests.
- Attach to a visible shared session only when the user has launched it with
  `--pilot`.
- Do not launch the visible client while the user is working unless the user
  asks for it.

The spike proved this contract in Release and Native AOT. A Brave window kept
the same foreground process and title while canonical keys advanced the game,
returned a complete encoded frame, and shut down the SadConsole client.

## Save and determinism contract

- Product version remains 1.0.0.
- Save version remains 99 if canonical key meaning and engine behavior remain
  unchanged.
- Generator version remains 1.
- Presentation settings are stored separately from game saves.
- Window size, font scale, palette, and pilot connection state never enter the
  journal or snapshot.
- Rendering and pilot observation never consume RNG or advance a turn.
- Default and release journey outputs must remain byte-identical to the D-174
  engine baselines.

## Native AOT and packaging

The client project preserves these assemblies for reflection:

```xml
<TrimmerRootAssembly Include="SadConsole" />
<TrimmerRootAssembly Include="SadConsole.Host.MonoGame" />
```

The Windows package contains at least:

- `aegis.exe`
- `SDL2.dll`
- `openal.dll`
- `aegis-tools.exe`
- Spoiler-free README
- Release notes
- Third-party notices
- SHA-256 manifest

The previous strict single-file expectation is replaced by a self-contained zip
with a small, fixed native dependency set.

SadConsole, MonoGame, Newtonsoft.Json, and framework AOT analysis currently emit
known IL2104 and IL3053 warnings. The migration must:

- Record the exact approved third-party warning set.
- Fail release verification on any new warning or changed warning source.
- Keep ordinary Release builds and tests at zero warnings.
- Run Native AOT startup, render, pilot, input, and shutdown smokes from a clean
  extraction.
- Never suppress warnings without an accompanying explanation and test.

## Accessibility and usability

- The full required frame stays visible at every supported window size.
- A minimum window size prevents controls from becoming too small to read.
- Window resizing and maximization are supported.
- The player can choose packaged integer font scales without changing the
  logical grid or save.
- Focus loss pauses physical input naturally but never pauses or advances game
  state.
- Returning focus cannot replay stale held keys.
- All required controls remain available from the keyboard.
- The first-run help identifies arrow aliases, canonical keys, resizing, and
  the location of presentation settings.

## Migration sequence

1. Add `Aegis.Host` and move the frontend-neutral session, save, and pilot code
   without changing behavior.
2. Prove the existing CLI and pilot tests still pass.
3. Add `Aegis.Client` with the spike-proven SadConsole startup, font, palette,
   and fixed grid.
4. Map physical input into the shared host queue.
5. Add `frame` observation and current-user-only pipe access.
6. Add packaged presentation settings and accessibility controls.
7. Update release tooling for the client, tools executable, native libraries,
   licenses, hashes, and clean-extraction smokes.
8. Compare the SadConsole and legacy terminal cell frames over a representative
   screen corpus.
9. Run focused input, save, pilot, AOT, packaging, and visual checks.
10. Run the complete repository tests and D-174 release sweeps. Require
    byte-identical engine results.
11. Build a new clean candidate and restart the guided manual campaign.
12. Verify V1-09 and V1-10 only after explicit user signoff.

## Acceptance criteria

- The client starts from a clean Windows x64 extraction without a terminal.
- The packaged font and palette render identically regardless of terminal or
  Windows color theme.
- The full 120 by 40 frame remains visible through supported resizes.
- Every current keyboard command has a physical SadConsole path.
- A physical keyboard campaign can create, play, save, quit, and reload.
- Pilot `keys` advances the same visible session without changing foreground
  window identity.
- Pilot `screen`, `state`, and `frame` observe the resulting state without
  focusing the client.
- Physical and pilot input share one serialized command queue.
- Save v99 replay remains exact, or a separately approved version bump and full
  sweep replaces this criterion.
- Native AOT launches, renders, accepts physical and pilot input, reloads a
  save, and exits cleanly.
- Clean package hashes and third-party notices cover every shipped file.
- All focused tests, all repository tests, default and release twin journeys,
  both seed-1 replays, and worldgen purity pass.
- The replacement guided manual campaign receives explicit user approval.

## Explicit exclusions

- No engine mechanics, content, balance, world generation, or story changes.
- No new canonical gameplay command.
- No animated tiles, particles, sound, controller support, or touch support.
- No mouse-only control.
- No installer, updater, telemetry, networking, cloud save, or code signing.
- No Linux or macOS package in the 1.0 gate.
- No runtime-selectable third-party fonts or themes.
- No deletion of the legacy terminal renderer before V1-10 verification.
