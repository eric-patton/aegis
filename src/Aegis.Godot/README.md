# Aegis Godot presentation spike

This is the bounded D-181 presentation proof. It is not the shipping client.

## Development launch

From this directory:

```powershell
& 'C:\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --path . --editor
```

Or run the project directly:

```powershell
& 'C:\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe' --path . -- --seed 1 --pilot --session godot-spike
```

Use `--save <slot>` to exercise v100 save creation and reload. Add
`--theme light` to start with the light field theme.

The existing tools remain the focus-free controller:

```powershell
dotnet run --project ..\Aegis.Cli -- pilot --session godot-spike ping
dotnet run --project ..\Aegis.Cli -- pilot --session godot-spike keys 150400...
dotnet run --project ..\Aegis.Cli -- pilot --session godot-spike ui compass
dotnet run --project ..\Aegis.Cli -- pilot --session godot-spike ui theme
dotnet run --project ..\Aegis.Cli -- pilot --session godot-spike ui stress
dotnet run --project ..\Aegis.Cli -- pilot --session godot-spike frame
```

## Windows export

The official Godot 4.7.1 .NET export templates must be installed. From the repository
root:

```powershell
$godot = 'C:\Godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
& $godot --headless --path 'src\Aegis.Godot' --export-release 'Windows Desktop' 'C:\repos\games\aegis\artifacts\godot-spike\Aegis.exe'
```

Launch the exported spike from its package directory:

```powershell
.\Aegis.exe -- --seed 1 --save-dir . --save godot-review
```

The `--` separates Godot arguments from Aegis arguments. Double-clicking `Aegis.exe`
also launches the spike, but does not select a named save.

See `RESULTS.md` for the acceptance evidence and current recommendation.
