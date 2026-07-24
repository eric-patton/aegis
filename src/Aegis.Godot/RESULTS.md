# D-181 Godot presentation spike result

Status: Passing bounded spike, player host verdict pending

Date: 2026-07-24

## Result

The spike proves that Godot can replace the player-facing presentation layer without
replacing the deterministic Aegis engine, Host session boundary, save format, command
language, or verification tools.

Engineering recommendation: approve the complete Godot client migration.

This is a recommendation, not the player decision. The existing SadConsole candidate
remains available until the player records the host verdict.

## Proven architecture

- Godot Engine 4.7.1 .NET, Windows x64.
- `Aegis.Godot` targets .NET 8, the supported runtime that loads in Godot 4.7.1.
- `Aegis.Core` and `Aegis.Host` multi-target .NET 8 and .NET 10.
- `Aegis.Client`, `Aegis.Cli`, and `Aegis.Core.Tests` remain on .NET 10.
- Physical keyboard input, pointer buttons, and named-pipe pilot batches all write to
  the existing `GameSession` queue.
- `GameSession` remains the only route to canonical `Game.ApplyKey`.
- `Frame` and `Presenter` remain available to the legacy client and verification tools.

## Presentation checks

- Full-window creation hides the map and world chrome.
- Creation progress, focus, Back, choices, and pointer selection are visible.
- The world uses the real generated glyph map with responsive status and recent words.
- The conversation surface uses responsive action and transcript panes with independent
  scrolling.
- Oversized action labels and prose wrap without clipping.
- The iron rose keeps its open state through a conversation round trip.
- Dark iron and light field are complete designed themes.
- Azeret Mono renders the map and compact data. Literata renders prose.
- A 1760 by 950 resize expands cleanly without fixed-aspect side bars.
- A background pointer click changed the theme without taking focus.
- A background pointer click advanced a canonical creation choice.

Local review images:

- `artifacts/godot-spike-creation-dark.png`
- `artifacts/godot-spike-creation-light.png`
- `artifacts/godot-spike-world-light-compass.png`
- `artifacts/godot-spike-compass-restored.png`
- `artifacts/godot-spike-conversation-light.png`
- `artifacts/godot-spike-conversation-dark-stress.png`
- `artifacts/godot-spike-creation-dark-large.png`
- `artifacts/godot-spike-pointer-theme.png`

## Determinism and save checks

- Save version: 100.
- Generator version: 1.
- A real saved campaign opened through the exported Godot client twice.
- Both loads produced the same canonical snapshot SHA-256:
  `92834354423CFB9AA20560CA62BEF2D8E69A3A623D3B2D63EB06349A5D55C34B`.
- Pilot input controlled gameplay and local UI without foreground focus.
- The packaged client opened its pilot channel, reported state, accepted a theme action,
  and shut down with exit code 0.

The spike changed no engine behavior, world generation, RNG stream or draw order,
canonical key meaning, save format, or replay behavior. D-181 therefore did not trigger
the conditional five-seed engine sweep.

## Build and test evidence

```text
dotnet build Aegis.slnx -c Release
Build succeeded.
0 Warning(s)
0 Error(s)

dotnet test tests\Aegis.Core.Tests\Aegis.Core.Tests.csproj -c Release --no-build --filter FullyQualifiedName~ClientHostTests
Passed: 22
Failed: 0

dotnet test Aegis.slnx -c Release --no-build
Passed: 1007
Failed: 0
Skipped: 0
```

## Export and launch

Export from the repository root:

```powershell
$godot = 'C:\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
& $godot --headless --path 'src\Aegis.Godot' --export-release 'Windows Desktop' 'C:\repos\games\aegis\artifacts\godot-spike\Aegis.exe'
```

The export uses an embedded PCK and produces:

- `Aegis.exe`
- `data_Aegis.Godot_windows_x86_64`
- `THIRD-PARTY-NOTICES.md`

Launch from the extracted package directory:

```powershell
.\Aegis.exe -- --seed 1 --save-dir . --save godot-review
```

The double separator is required. Arguments before `--` belong to Godot. Arguments after
it belong to Aegis.

## Remaining gate

The player reviews the visible spike and chooses one:

1. Approve the complete Godot client migration.
2. Return to a bounded SadConsole remediation pass.
3. Request a narrower second spike for one unresolved risk.

If migration is approved, the next implementation must complete all player surfaces,
package the Godot client as the replacement release candidate, run the applicable
verification gates, and restart the fresh guided campaign.
