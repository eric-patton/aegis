# Aegis

Aegis is a single-player turn-based role-playing game for the Windows terminal.
Every campaign is generated from a seed and saved as an append-only action journal.
It makes no network calls and has no telemetry.

## Run

Extract the release zip to a writable folder and run `aegis.exe`.

The 1.0 package supports Windows x64. Use Windows Terminal or another modern terminal
with UTF-8, ANSI color, and a window of at least 100 columns by 32 rows. A larger window
provides a wider map and message rail.

## Controls

- Move with `h j k l`, diagonals with `y u b n`, or use the arrow keys.
- Wait with `.`.
- Enter or leave with `>` and `<`.
- Interact or gather with `g`.
- Open help in the game with `?`.
- Quit with `q`.

The game presents contextual controls in its sidebar and explains additional combat,
travel, equipment, and conversation commands as they become relevant.

## Saves

Start a named campaign with:

```text
aegis.exe --save my-campaign
```

Named saves live under `%LOCALAPPDATA%\Aegis\saves` by default. Every accepted key is
flushed to the journal immediately. Use `aegis.exe saves` to list slots.

Save version 99 pins world generator version 1. Older save versions are intentionally
rejected because Aegis does not silently migrate or reinterpret an old campaign.

## Verification tools

The executable also carries deterministic local tools:

```text
aegis.exe sim --seed 1 --keys "0...." --quiet
aegis.exe journey --seed 1 --cycles 1 --json
aegis.exe worldgen --seeds 2 --tiers 1-2 --json
```

These tools do not read or alter named saves unless a save path is explicitly supplied
to the normal game command.

## Support boundaries

Version 1.0 has no installer, updater, cloud save, networking, code signing, Linux
package, or macOS package. Keep the extracted folder and your save directory backed up
like any other local game data.
