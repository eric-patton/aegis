# Aegis 1.0.0 release audit

This is the durable audit for V1-09 under D-165 and D-174, amended by D-175 through D-181. Automated
evidence may be regenerated. The manual section is a human gate and cannot be inferred
from automated tools.

## Superseded terminal candidate identity

Status: superseded by D-175. This package cannot receive final 1.0 signoff.

- Product version: 1.0.0
- Save version: 99
- Generator version: 1
- Runtime identifier: win-x64
- Package: `aegis-1.0.0-win-x64.zip`
- Commit: recorded in the package's internal `SHA256SUMS.txt`
- Package SHA-256: recorded in the sibling `aegis-1.0.0-win-x64.zip.sha256`
- Verification environment: Windows x64, .NET SDK 10.0.300
- Manual seed and slot: pending user run

## Active SadConsole candidate identity

Status: D-180 automated gates and package smokes passed. Second packaged review found
presentation blockers. D-181 approves a bounded Godot presentation spike before the
shipping-host verdict and replacement candidate.

- Product version: 1.0.0
- Save version: 100
- Generator version: 1
- Runtime identifier: win-x64
- Package: `artifacts/aegis-1.0.0-win-x64.zip`
- Commit: `4fa8ed54ee1997bec0fb415a4c1412d4ecef548c`
- Package SHA-256:
  `ea87eeb6c41c567161f3420cc2fc0d7d06ea80bcb8dee92900dd7ceeaf493459`
- Client: SadConsole 10.10.1 with MonoGame DesktopGL 3.8.4.1
- Payload: `aegis.exe`, `aegis-tools.exe`, `SDL2.dll`, `openal.dll`, README,
  release notes, third-party notices, and `SHA256SUMS.txt`
- Verification environment: Windows x64, .NET SDK 10.0.300
- Manual seed and slot: pending user run

## Godot presentation spike

Status: D-181 bounded spike built and verified. Player host verdict pending.

- Godot: 4.7.1 .NET
- Godot target: .NET 8
- Existing client, tools, and test target: .NET 10
- Save version: 100
- Generator version: 1
- Release build: zero warnings and zero errors
- Focused Host tests: 22 passed
- Complete tests: 1,007 passed, zero failed, zero skipped
- Real save reload snapshot SHA-256:
  `92834354423CFB9AA20560CA62BEF2D8E69A3A623D3B2D63EB06349A5D55C34B`
- Windows x64 export: pilot startup, state, presentation action, pointer action, and
  clean shutdown passed
- Engine sweep: not triggered, because no engine behavior, world generation, RNG,
  canonical command, save format, or replay boundary changed
- Human gate: review visible spike and choose the shipping presentation host

## SadConsole automated recovery gate

- [x] Release build: zero warnings and zero errors
- [x] Complete test suite: 1,007 passed, zero failed, zero skipped
- [x] Focused client, host, input, pilot, save, frame, settings, and package tests
- [x] Five default twin pairs byte-identical to their mates
- [x] Five release twin pairs byte-identical to their mates
- [x] Both seed-1 replays exact
- [x] Generator 1 worldgen report byte-identical to the prior baseline
- [x] D-180 drift against D-178 justified by the reproduced ranged-pursuit repair
- [x] Client and tools Windows x64 Native AOT publishes
- [x] Exact seven-entry approved third-party AOT warning set
- [x] Tools AOT publish with zero warnings
- [x] Clean-extraction tools help, sim, and worldgen smokes
- [x] Clean-extraction current-user pilot ping, keys, state, and frame smokes
- [x] Structured frame: 120 by 40, 4,800 glyph-and-RGB cells
- [x] Named save creation, orderly shutdown, second-process reload, and second shutdown
- [x] Every manifest payload hash and outer archive hash verified

## Automated gate

- [x] Release build, zero warnings and zero errors
- [x] Focused Salt Fen, reader, save, generator, and release-tool tests
- [x] `journey --release --seed 1 --json` reaches cycle 13 with all nine matrix rows true
- [x] Complete test suite: 980 passed, zero failed, zero skipped
- [x] Five default twelve-world journey twin pairs are byte-identical
- [x] Five release twelve-world journey twin pairs are byte-identical
- [x] Seed 1 default journal replays exactly through `sim`
- [x] Seed 1 release journal replays exactly through `sim`
- [x] Generator 1 worldgen purity and structure gate: 240 worlds, zero mismatches
- [x] Clean Windows x64 Native AOT package
- [x] Manifest metadata and every listed file hash verified
- [x] Clean-extraction `--help`, short exact `sim`, and `worldgen --json` smokes

The first release-route proof used seed 1 and reached cycle 13 after 12 crossings. Its
machine matrix reported V1-01 through V1-09 true. The V1-09 row included both legal pan
work and a no-cost weather refusal, 36 finite pans worked, 84 adders taken, 11 bounded
regional conclusions, and 11 one-time restocks. One fighting site was honestly skipped
under the journey's existing site budget, so that world reached the crossing without a
regional conclusion. The following eleven worlds completed the account. This is allowed:
the regional arc never blocks the waygate, and the release matrix still proves its full
path.

The five default baselines are seed 1 at 35,094 keys, turn 33,981, 11 deaths; seed 7
at 40,731 / 33,690 / 9; seed 99 at 40,595 / 36,386 / 9; seed 2024 at
43,177 / 41,301 / 7; and seed 88888 at 42,171 / 40,948 / 10. Each has a
byte-identical emitted-journal twin and reaches cycle 13 with twelve crossings. Drift
from v102 is expected: the default route now traverses and completes the fourth country,
including weather waits, finite work, fights, rest, and its bounded regional account.

The five release routes also have byte-identical twins and pass all nine matrix rows.
Seed 1 replays exactly at 41,128 keys, cycle 13, and turn 42,320. The default seed 1
route replays exactly at 35,094 keys, cycle 13, and turn 33,981. Generator 1 regenerated
240 worlds in two passes with zero digest mismatches and zero prose failures across
93,002 measured surfaces.

## Defect ledger

### Resolved during candidate work

1. Causeway carving replaced the fen-side return-mouth terrain after first placement.
   The pilot could reach the coordinate but `>` had no crossing to invoke, producing a
   free-action loop. The generator now restores the mouth after every causeway is carved,
   with a two-way crossing test and a completed default journey.
2. The pilot used the character-lifetime pan-work counter as a world-local trip gate.
   Later worlds skipped their fresh saltworks and could not close the regional account.
   The pilot now reads the current site's finite pan list. The twelve-world release route
   proves all 36 pans and eleven available full conclusions.
3. The first clean packaging attempt found invalid PowerShell continuation syntax in the
   strengthened exact sim and worldgen smoke assertions. No publish began and no package
   was produced. The conditions now use valid operator continuations, and the release-tool
   test invokes PowerShell's parser over the complete script.

### Superseded-candidate defect resolved by replacement

1. Major presentation portability defect: the terminal client inherited palette, font,
   and theme behavior from the user's terminal environment, so the intended presentation
   is not controlled by Aegis. The user did not approve the terminal candidate and
   approved the SadConsole replacement direction under D-175. D-177 replaced that client
   with an owned window, D-178 built the first SadConsole candidate, and D-180 closes
   its guided-review remediation. The rebuilt package and fresh manual campaign remain
   before the replacement is accepted for release.

## Roadmap classification

Every remaining `[ ]` or `[~]` line in `design/roadmap.md` is classified line by line.
The launch-gate partials are V1-09, V1-10, presentation, and the replacement release
package. All other incomplete families are labeled post-1.0 under D-165. The open
design-question parking lot is also explicitly post-1.0 unless promoted by a later
decision.

## Important-fact and conflict audit

- The Salt Fen's regional, site, work, outcome, compact, and delivery facts are read by
  entry presentation, topics, the bounded regional account, its witnessed aftermath,
  or the peddler delivery.
- `shame/housebroken` has a once-per-world lane reader with no added punishment.
- A present called shade has one villager reader and one moot-warden reader, each
  once per world with no numeric consequence and no line without the live shade.
- Every live launch conflict retains a designed exit. The Salt Fen account closes through
  either equal conclusion or resets at the crossing, and it never blocks travel,
  progression, sites, or work.
- Salt work pays Survival and a carried sale good. The adder pays Hunting, hide, meat,
  and the existing food or sale loops. The bounded account pays a good, a scheduled
  world-state move, facts, and witnessed aftermath.

## Manual packaged campaign protocol

Status: paused after the second packaged review. Do not continue the final campaign
until the presentation blockers and host decision are resolved.

The first guided terminal-candidate playtest rejected release and identified additional
usability and behavior findings beyond terminal-owned presentation. Those findings are
preserved in `design/playtest-remediation-1.0.md`. Before final signoff, the player must
classify them, approve the required 1.0 repairs, and confirm that every promoted blocker
is closed. Visible review of the D-178 client may be used to reproduce and classify
presentation findings, but it does not erase findings that the migration did not touch.

Use only the cleanly extracted package. Do not use a development binary, pilot, debug
hook, edited journal, or an existing slot.

1. Record the package SHA-256, commit, Windows version, display scale, initial window
   size, font scale, seed, and a fresh named save slot.
2. Complete character creation and confirm ordinary map, sidebar, help, movement,
   conversation, inventory, and save behavior are readable.
3. Exercise ordinary life in each activity family: craft, wilderness, crime, and town.
4. Visit and return from all four countries. Confirm their thresholds, forecasts, local
   maps, sites, and roof or camp rules are legible.
5. Exercise representative melee, ranged, and magical play. Confirm telegraphs, marks,
   posture, stamina, Focus, damage, and recovery can be read and answered.
6. Travel with one mortal guest and one called companion. Confirm movement, danger,
   commands or autonomy, and endings are understandable.
7. Die at least once, recover the remnant, confirm the journal persists the consequence,
   quit, reload the named slot, and confirm exact continuation.
8. Advance the main progression through its resolution, cross into a later world, and
   complete at least one further crossing.
9. Record final cycle, turn, deaths, save readability, every defect found, and whether
   any control or supported layout became inaccessible.
10. Give an explicit verdict: approved for Aegis 1.0.0, or rejected with defects.

## Final signoff

- User verdict: D-180 package not approved; presentation host under discussion
- Date: 2026-07-24
- Final cycle and turn: pending
- Defects accepted: none
- V1-09 status: Implemented, not yet Verified
- V1-10 status: Implemented through the D-181 spike, replacement client pending
- Aegis 1.0 status: paused for player spike review, presentation-host verdict, and
  replacement candidate
