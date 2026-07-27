# Aegis Godot client

This project is the approved D-182 replacement presentation layer for Aegis. The
deterministic engine, save format, command language, and verification tools remain in
their existing projects.

D-182 Phases 1 through 3 and their D-196 through D-199 review remediation are implemented. Creation, the
map-dominant responsive world, shared filtered Activity and History, Conversation, and
searchable Help use persistent native Godot controls. Character uses the D-187
Character Ledger and Inventory and Equipment use the D-188 Outfitter's Bench, both fed
by typed Host projections. Journal knowledge sections, Settings, and campaign entry
remain in later approved phases, so this is a review checkpoint rather than the final
release candidate.

D-198 repairs creation focus, fixed world-rail sizing, and direction-prompt arrow
routing. D-199 prevents duplicate attribute-shaping submission and brings Character
and Pack to the approved three-region Ledger and shelf-plus-table Outfitter
architectures. The approved mockup architecture is canonical, while
`design/godot-ui-mockup-review.md` separately tracks which screens still need visible
parity work.

The current player-review checkpoint is
`artifacts/aegis-d199-character-pack-parity-win-x64.zip`, built from `6e12f2e`.

## Development launch

From this directory:

```powershell
& 'C:\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --path . --editor
```

Or run the project directly:

```powershell
& 'C:\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --path . -- --seed 1 --pilot --session godot-client
```

Use `--save <slot>` for v100 save creation and reload. Add `--theme light` to start
with the light field theme.

The focus-free pilot controller can drive and inspect the client:

```powershell
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ping
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client keys 150400...
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui activity
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui history
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui help
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui theme
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui scale
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui zoom-in
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui zoom-out
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui zoom-reset
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui focus-check
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client frame
```

`focus-check` succeeds only while a visible Character Creation text field owns keyboard
focus. It supports background verification of the remembered-person and character-name
transitions without taking over the user's desktop.

## Player controls

- Arrow keys or HJKL move in the four cardinal directions.
- Ctrl+Left/Right moves northwest or northeast.
- Alt+Left/Right moves southwest or southeast.
- Ctrl+minus, Ctrl+plus, and Ctrl+0 change map zoom without scaling the interface.
- Character, Pack, Journal, and Help open from the world launcher.
- F6 changes theme.
- F7 changes UI scale.
- Escape closes Help or History, or leaves the current game surface.

The default world shell has no permanent control legend or movement panel. Help owns
control guidance. Theme, UI scale, and map zoom persist in local presentation settings.
The approved Settings screen will replace the temporary F6 and F7 review shortcuts in a
later phase.

## Windows export

The official Godot 4.7.1 .NET export templates must be installed. From the repository
root:

```powershell
$godot = 'C:\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
& $godot --headless --path 'src\Aegis.Godot' --export-release 'Windows Desktop' 'C:\repos\games\aegis\artifacts\godot-client\Aegis.exe'
```

Launch an exported checkpoint from its package directory:

```powershell
.\Aegis.exe -- --seed 1 --save-dir . --save godot-review
```

The `--` separates Godot arguments from Aegis arguments. Double-clicking `Aegis.exe`
also launches the client, but does not select a named save.
