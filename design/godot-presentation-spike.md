# Aegis Godot presentation spike

Status: Implemented and verified under D-181; player host verdict pending

Date: 2026-07-24

## Purpose

The second packaged review showed that the fixed-cell SadConsole client cannot yet meet
the required presentation standard. This spike tests whether Godot .NET can replace only
the player-facing client while preserving the deterministic game, save files, commands,
and verification tools.

The spike is a decision gate, not approval for a complete migration. Its result must be
reviewed before the shipping client changes.

## Fixed boundaries

- Keep `Aegis.Core` as the I/O-free deterministic engine.
- Keep `Aegis.Host` as the session, save, and focus-free pilot boundary.
- Keep save v100 and campaign generator 1.
- Keep every canonical gameplay character and journal meaning.
- Keep `Aegis.Cli` and its sim, journey, worldgen, and release verification surfaces.
- Keep `Frame` and `Presenter` available for the CLI and regression tests.
- Do not make a gameplay or world-generation change during the spike.
- Do not bump the save format.
- Do not approve or remove the SadConsole client during the spike.

## Architecture to prove

Add a bounded `Aegis.Godot` C# client that references `Aegis.Core` and `Aegis.Host`.
Physical input, pointer actions, and pilot batches must still enter one serialized
`GameSession` queue and reach the same canonical `Game.ApplyKey` path.

The Godot client must not merely paint the existing 120 by 40 `Frame` into a new window.
It needs a semantic presentation model:

- the map remains a crisp glyph grid with stable cell geometry;
- prose and menus use responsive Godot `Control` nodes and native wrapping;
- creation owns the full window while it is active;
- conversations allocate space from the current viewport and scroll where needed;
- the compass is presentation state and survives a conversation round trip;
- theme, focus, hover, scroll, and other presentation state never enter the journal.

The legacy frame remains an observation and compatibility surface. The semantic model
must be derived from the same live game state and may not invent a second gameplay
language.

## Visual direction

The visual system is quiet field instrumentation around one expressive object, the iron
rose. It should feel made for Aegis rather than like Godot's default controls.

### Palette

Dark iron:

- Coal: `#10161A`
- Forged panel: `#182126`
- Bone text: `#E7E1D2`
- Weathered cyan: `#72C7CC`
- Ember: `#D69A48`
- Blood mark: `#A9534F`

Light field:

- Day paper: `#E9E2D3`
- Ash panel: `#D8D0C1`
- Ink: `#20282B`
- Deep teal: `#2D7278`
- Ochre: `#9C6828`
- Wound: `#8F403E`

### Type and layout

- Use a bundled vector monospace face for the map and compact data.
- Use a bundled readable vector face for prose, menus, and creation.
- Text size must scale cleanly with the window and Windows display scaling.
- Use responsive containers, minimum sizes, and scroll containers instead of fixed
  character counts and manual clipping.
- Use visible keyboard focus and keep mouse and keyboard paths equivalent.

### Signature

The iron rose is the single expressive element. It remains compact on the world view,
shows all eight directions plus wait, and preserves its open state across temporary
interaction screens.

## Required spike scenes

### Full-window creation

- No live map, sidebar, or world log appears behind creation.
- The current stage, progress, choice list or text field, Back, Continue, and review are
  clear at the current viewport size.
- Oversized choice labels wrap without clipping.
- Existing creation checkpoint behavior and canonical keys remain unchanged.

### World

- Render a real generated Aegis map from the current engine.
- Show readable status and recent messages without forcing a fixed 120 by 40 window.
- Toggle and operate the iron rose by keyboard, pointer, and focus-free pilot action.
- Preserve the open rose across a conversation and back to the world.

### Conversation

- Use a responsive action pane and transcript pane.
- Wrap deliberately oversized action labels and transcript entries.
- Scroll both panes when their content exceeds available height.
- Keep focus, Enter, Escape, number shortcuts, and pointer selection coherent.
- Never truncate a required action label to a hard-coded character count.

### Themes and scaling

- Dark iron and light field are complete designed themes, not automatic inversion.
- Switching themes is presentation-only.
- Both themes preserve readable focus, selection, disabled, warning, and accent states.
- Vector text remains crisp at representative Windows display scales and resized
  windows.

## Acceptance gate

The spike succeeds only if all of these are demonstrated:

- Godot 4.7.1 .NET opens and builds the project on Windows.
- The Godot C# target can reference the current .NET 10 Core and Host projects, or a
  narrow compatibility adjustment is identified and proven without changing behavior.
- A real v100 campaign can be created, saved, closed, and reloaded through `Aegis.Host`.
- Creation, world, conversation, dark theme, light theme, scaling, and persistent
  compass behavior are visibly reviewable.
- Oversized labels and prose wrap without clipping.
- Keyboard and pointer paths emit canonical gameplay characters.
- The existing named-pipe pilot can control gameplay and local UI without foreground
  focus or operating-system input.
- Identical canonical key journals produce identical game snapshots through the Godot
  client and the existing host or CLI path.
- The project produces a clean Windows x64 export with documented launch instructions.
- Release build and the complete existing test suite remain green.

Because the spike changes presentation only, the five-seed engine sweep is not required
unless implementation touches engine behavior, world generation, RNG, canonical key
meaning, or save replay. If any of those boundaries move, work stops for a separate
decision and the full HANDOFF sweep.

## Implementation result

The bounded spike passes its automated and agent-visible gate:

- Godot 4.7.1 .NET builds and runs the new `src/Aegis.Godot` client on Windows.
- `Aegis.Core` and `Aegis.Host` multi-target .NET 8 and .NET 10. The Godot client uses
  .NET 8 for engine compatibility. The CLI, tools, SadConsole client, and tests remain
  on .NET 10.
- Creation is a dedicated full-window surface. The world and conversation surfaces use
  responsive Godot controls rather than repainting the fixed observation frame.
- Azeret Mono and Literata vector fonts, dark iron and light field themes, wrapped
  labels, independently scrolling conversation panes, and the persistent iron rose are
  present and visibly exercised.
- Background pointer clicks changed the theme and advanced a canonical creation choice
  without taking foreground focus. Pilot actions controlled gameplay and presentation
  through the existing named pipe.
- A real v100 save loaded twice through the exported client with exact SHA-256 snapshot
  parity:
  `92834354423CFB9AA20560CA62BEF2D8E69A3A623D3B2D63EB06349A5D55C34B`.
- The Windows x64 export opens, reports generator 1 and save v100 through the pilot,
  accepts presentation actions, and exits cleanly.
- Release build: zero warnings and zero errors.
- Focused Host tests: 22 passed.
- Complete suite: 1,007 passed, zero failed, zero skipped.

The spike did not change engine behavior, world generation, RNG consumption, canonical
key meaning, save format, or replay. The five-seed engine sweep was therefore not
triggered by the approved boundary.

The implementation report and launch commands are in `src/Aegis.Godot/RESULTS.md`.

## Review outcome

After the spike, the player chooses one of three outcomes:

1. Approve a complete Godot client migration and replacement release candidate.
2. Return to a bounded SadConsole remediation pass.
3. Request a narrower second spike for a specific unresolved risk.

No outcome is inferred from automated evidence alone.

Engineering recommendation: approve the complete Godot client migration. The spike
resolves the presentation-host risks that remained structural in SadConsole while
preserving the deterministic engine and verification boundaries. This recommendation
does not become the project decision until the player reviews the visible result.
