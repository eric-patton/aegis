# Aegis, project instructions

Aegis is a terminal/TUI single-player turn-based RPG in C#/.NET 10 (`Aegis.slnx`).
`src/Aegis.Core` is the I/O-free deterministic engine; `src/Aegis.Cli` is the `aegis` exe
(TUI + the pilot/sim/journey verification tools); `tests/Aegis.Core.Tests` is xUnit.

## Roadmap tracking (keep this current)

`design/roadmap.md` is the **living feature tracker and roadmap**: what is built, partial,
not started, and open. Treat it as the source of truth for "where are we, what is left."

- **Check items off as they land.** When a feature ships and is verified, flip its box to
  `[x]` and append the decision number. Update the "at a glance" counts and the changelog.
- **Add newly-found work immediately.** When a session turns up a missing piece, a
  follow-on, or newly-scoped work, add a line under the right pillar (or the changelog) in
  the same session, so it is tracked and not lost to one conversation. Design questions go
  under "Open design questions" instead.
- **Keep it in sync with `design/decisions.md`.** Every `[x]` should trace to a decision,
  and roadmap's "Open design questions" should mirror the parking lot at the end of
  `decisions.md`. Reconcile when they drift.

## Key docs

- `design/roadmap.md`: the feature tracker and roadmap (above).
- `design/vision.md`: the unified design document.
- `design/decisions.md`: the numbered decision log with rationale (record substantive
  decisions here; the D-064+ block is newest-first).
- `design/story/`: the arc spec and world-story templates. Full story detail lives here.

## Operating notes

- **Kill `aegis.exe` before rebuilding**, or the build fails silently:
  `Get-Process aegis -ErrorAction SilentlyContinue | Stop-Process -Force`.
- **Drive the game through the pilot channel, not cwin**: the in-process
  `journey` / `sim` / `pilot` subcommands are the engine-honest way to play and verify.
- **No story spoilers in chat.** Names are fine; never describe reveal content. The user
  plays the game fresh. Design docs and code hold full detail.
