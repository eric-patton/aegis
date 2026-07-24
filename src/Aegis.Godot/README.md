# Aegis Godot client

This project is the approved D-182 replacement presentation layer for Aegis. The
deterministic engine, save format, command language, and verification tools remain in
their existing projects.

D-182 Phase 2 is implemented. Creation, the responsive world, full colored History,
conversation, and the draggable iron rose use persistent native Godot controls. The
Character, Inventory, and Journal work remains in later approved phases, so this is a
review checkpoint rather than the final release candidate.

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
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui compass
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui history
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui theme
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client ui scale
dotnet run --project ..\Aegis.Cli -- pilot --session godot-client frame
```

## Player controls

- Arrow keys or HJKL move in the four cardinal directions.
- Ctrl+Left/Right moves northwest or northeast.
- Alt+Left/Right moves southwest or southeast.
- Tilde opens or closes the iron rose.
- F6 changes theme.
- F7 changes UI scale.
- Escape closes History or leaves the current game surface.

The iron rose can be dragged by its MOVE or IRON ROSE handle. Its open state and
normalized position persist in local presentation settings.

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
