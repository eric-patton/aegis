# Aegis handoff (updated 2026-07-23, D-180 implemented and sweep-verified)

This file exists so any assistant (or human) can pick the project up cold and keep
moving. It records the things that were living only in session memory: working
conventions, the verification workflow, current baselines, and what is queued next.
The canonical design truth stays where it always was:

- `CLAUDE.md` (repo root): project instructions, roadmap discipline, operating notes.
- `design/roadmap.md`: the living feature tracker. Check items off as they land.
- `design/decisions.md`: numbered decision log. D-001..D-063 ascending, then a
  newest-first block (D-180 currently at its head), then the parking lot of open
  questions at the end. New decisions go at the HEAD of the newest-first block.
- `design/vision.md`: the unified design doc. Line 5 carries the counter, currently
  "(D-001 through D-180)". Bump it whenever a decision lands.
- `design/plan-2026-07.md`: the current build plan. The original sequence is complete;
  V1-09 is built, and D-175 supersedes its terminal candidate.
- `design/plan-1.0.md`: the canonical ten-card implementation queue. V1-01 through V1-08
  are Verified; V1-09 and V1-10 are Implemented.
- `design/sadconsole-client-migration.md`: the implemented V1-10 contract.
- `design/story/`: arc spec and world-story templates. Full story detail lives there.
- `docs/dev-harness.md`: the pilot/sim/journey harness.

## Current state

- Latest approved direction: D-175 replaces the terminal-owned player with a SadConsole
  10.10.1 and MonoGame DesktopGL client before 1.0 signoff. D-176 approves the complete
  contract, D-177 implements it without changing the engine, and D-178 builds the clean
  replacement candidate.
- D-180 implements the bounded guided-playtest remediation package: the presentation-only
  iron rose compass, canonical keyboard and mouse parity, modern interactive screens,
  creation backtracking at save v100, contextual teaching, readability repairs, and
  reproduction-led behavior repair. The full automated gate is green. A clean package
  and fresh guided campaign are the remaining release steps.
- Latest completed product work: D-174 and V1-09. Its engine, content, release journey,
  audits, and automated evidence remain green, but its terminal package is superseded
  and cannot receive final signoff.
- The isolated compatibility spike is under `artifacts/sadconsole-spike/`. It renders the
  real Aegis frame with an owned palette and font, keeps the whole 120 by 40 frame under
  `Fit` resizing, launches under Native AOT with explicit assembly roots, and accepts
  canonical named-pipe keys without changing the foreground window. See `RESULTS.md`.
- Save format: `SaveCodec.Version = 100` (v91 = D-154, v92 = D-166, v93 = D-167,
  v94 = D-169, v95 = D-170, v96 = D-171, v97 = D-172, v98 = D-173,
  v99 = D-174, v100 = D-180; history
  comments in `src/Aegis.Core/SaveCodec.cs`).
- Generator format: campaign-scoped generator 1, recorded separately in v100 saves.
- Product version: 1.0.0.
- Tests: 1007 green (`dotnet test Aegis.slnx -c Release --no-build`).
- The D-180 Release build, focused and full tests, default and release sweeps, both
  seed-1 replays, and the 240-world generator-1 purity gate are green.
- D-180 adds semantic presentation actions, focus-free local UI pilot commands, the
  interactive guide, log, sheet, pack, conversations, creation review and backtracking,
  Windows DPI ownership, and the reproduced ranged-pursuit repair.
- D-180 candidate: source commit `4fa8ed54ee1997bec0fb415a4c1412d4ecef548c`,
  archive `artifacts/aegis-1.0.0-win-x64.zip`, SHA-256
  `ea87eeb6c41c567161f3420cc2fc0d7d06ea80bcb8dee92900dd7ceeaf493459`.
  Both Native AOT publishes, warning pinning, clean-extraction tools, pilot, structured
  frame, save creation and reload, shutdown, manifest, and archive hashes pass.

## Working conventions (were in user-level config, not visible to a new tool)

- **Never use em dashes** (or en dashes as sentence separators) in ANYTHING written
  for this project: prose, docs, code comments, commit messages, chat. Use commas,
  colons, parentheses, or separate sentences. Plain hyphens in ranges and compound
  words are fine.
- Always use raw string literals (`"""`) for multi-line strings in C#.
- Git identity: author is `eric-patton <ursine.blue@proton.me>`. **Never** list an
  AI as author or co-author; no `Co-Authored-By: Claude` trailers or similar.
- Committing and a normal push are fine. The GitHub remote is
  `git@github.com:eric-patton/aegis.git`. No destructive git ever: no `reset --hard`,
  `push --force` (any variant),
  `clean -f`, `branch -D`, `rebase`, `filter-branch`, `stash drop/clear`.
- Commit message register matches the log: lowercase evocative title in the game's
  own diction, decision number in parens, e.g.
  `The wolf-gill: the fells' third site, the she-wolf, and the great pelt (D-150)`.
- **No story spoilers in chat.** Names are fine; never describe reveal content.
  The user plays the game fresh. Docs and code hold full detail.
- Design sessions: present options WITH a recommendation, and discuss big tradeoffs
  in chat before asking for a pick. The user decides substantive design questions;
  use a structured Q&A when several picks are pending.

## Build and verification workflow

- **Kill the exe before rebuilding** or the build fails silently:
  `Get-Process aegis -ErrorAction SilentlyContinue | Stop-Process -Force`
- Build: `dotnet build Aegis.slnx -c Release`. The Release exe is
  `src\Aegis.Cli\bin\Release\net10.0\aegis.exe`.
- Note: `dotnet test` does NOT rebuild the Cli exe. Build the solution before
  running journeys or you drive a stale binary.
- **Drive the game through the pilot channel, never screen automation.** Never run
  bare `aegis` (it takes over the terminal). The engine-honest surfaces:
  - `aegis journey --seed N --cycles 12 --emit-keys` plays a full bot run and
    prints stats; its last line is `keys (N): <journal>`, strip the prefix with
    `-replace "^keys \(\d+\): ",""` to get the raw key journal.
  - `aegis sim --seed N --keys "<journal>"` or
    `aegis sim --seed N --keys-file "<path>"` replays keys and prints a JSON snapshot.
    Use the file form for journals beyond the Windows command-line limit. Replaying a
    journey's journal must reproduce the journey's end state exactly (keys, cycle, turn).
    Truncated journals replay safely, which makes prefix-replay a valid probe of mid-run
    state.
  - `aegis worldgen --json` generates every world twice and hash-compares; exit 0
    is the purity gate (exit 2 = nondeterminism).

### The sweep discipline (run after every engine change)

Run seeds `1, 7, 99, 2024, 88888` through `journey --cycles 12 --emit-keys`
TWICE each (twins), save outputs as `vNN-sw{seed}.txt` / `vNN-tw{seed}.txt`, and
hash-compare each pair: twins must be byte-identical (determinism). Then compare
against the previous baseline set to see (and justify) drift. Then sim-replay at
least seed 1's journal and require exact key/cycle/turn match. Then run the
worldgen purity gate.

**Current default baselines are D-180**, stored as
`artifacts/v1-10-d180-default-{seed}-{a,b}.json`. The prior comparison set is
`artifacts/v1-10-default-{seed}-a.json`. Every D-180 twin pair is byte-identical to
its mate. Drift is expected and justified by the ranged wolf pursuit repair:

| seed  | keys  | turns | deaths | drift keys / turns / deaths | outcome                |
|-------|-------|-------|--------|------------------------------|------------------------|
| 1     | 34828 | 33702 | 10     | -266 / -279 / -1             | cycle 13, 12 crossings |
| 7     | 40865 | 33841 | 8      | +134 / +151 / -1             | cycle 13, 12 crossings |
| 99    | 40604 | 36373 | 10     | +9 / -13 / +1                | cycle 13, 12 crossings |
| 2024  | 43059 | 41185 | 6      | -118 / -116 / -1             | cycle 13, 12 crossings |
| 88888 | 41801 | 40566 | 10     | -370 / -382 / 0              | cycle 13, 12 crossings |

Seed 1 default sim replay is exact at 34,828 keys, cycle 13, turn 33,702, and 10
deaths. The five release twin pairs are stored as
`artifacts/v1-10-d180-release-{seed}-{a,b}.json`; all are byte-identical, reach cycle
13 with twelve crossings, and pass all nine matrix rows. Release seed 1 sim replay is
exact at 40,898 keys, cycle 13, turn 42,137, and 19 deaths. Worldgen generator 1:
240 worlds, zero digest mismatches, zero hard failures, and a SHA-256 exactly matching
the D-178 baseline.

## Engine invariants (break these and old saves die)

- Saves are seed + key journal, replayed on load. Any change to worldgen draws or
  to what a key does requires a `SaveCodec.Version` bump with a history comment.
- Save v100 records campaign-scoped generator 1 in the header. Replay threads the
  recorded generator through every crossing. Unsupported save or generator versions
  reject explicitly before generation (D-174).
- **End-append enums only** (SkillId, SiteKind, Terrain, MonsterKind, TradeGood,
  LessonId, BookId...): inserting mid-enum shifts serialized values.
- **New worldgen RNG streams draw AFTER all existing draws** (derive a named
  stream, e.g. `Derive(fellSeed, "gill")`), so pinned worlds keep their layouts.
- **Combat/economy modifiers are additive AFTER dice** (never extra rolls), so
  rng streams never move and old journals replay.
- Talk menus cap at 9 digit entries (topics + offers). Watch this when adding
  topics; deep-world steadholders already sit at the cap.
- Coarse world tick = 160 turns (`SteadRaids.TickTurns`); the ScheduledFact
  calendar (`Upcoming`) runs on it.
- Every world opens in autumn. The seed-drawn hard-winter tick 3-5 begins winter;
  later seasons advance every three coarse ticks. Lowlands, road, and fells draw
  independent deterministic three-card weather hands. Weather advances before schedule
  and cadence work on each tick (D-167).
- Prose variants derive only from world seed, fact id, family id, and surface kind. They
  never read gameplay or worldgen RNG, cycle on reread, or enter save state. WorldEval's
  disposable topic catalog may exercise gated answers, but never mutates the measured
  world or a live Game (D-168).
- Every live season-deck card explicitly declares pacing metadata. The teller fixes one
  call before the coarse tick advances, consumes one ordinary cadence roll per live tick,
  and may steer only Elastic cards. Scheduled futures, faction work, weather, durations,
  player-triggered content, combat, sites, and worldgen remain protected. Crossing resets
  pacing carry and Space authority but keeps the run-wide diagnostic book (D-169).
- The guild loft, boxed purse, fitted workshop, lists entry, and judicial challenge are
  world-scoped. Books and their reading progress remain character-scoped. Formal combat
  uses ordinary actions and time but converts a lethal result to a yield, with no death,
  remnant, loot, Essence, bestiary study, faction kill, guest, or summon (D-170).
- Hostile-site awareness is separate from authored dormancy. Soft tread spends up to two
  ordinary causal turns, and uppercase local movement validates a complete rush before
  spending stamina or time. Sleight owns pockets and locks; Larceny owns pilfering,
  burglary, and fencing (D-171).
- Physical enemy intent chooses the nearest visible living bearer, mortal guest, or shade,
  with ties favoring the bearer. Marked physical footprints strike every occupied cell.
  Following fellows take one stable legal escape from imminent visible marks before other
  behavior, while held fellows keep the ordered risk. Bearer-shaped magic remains
  bearer-only (D-173).
- Companion success and beloved-loss memories are separate, character-scoped, bounded,
  later-world Aegis remembrances. The grain delivery is world-scoped and delayed one
  coarse tick. Beast recognition and the fitted brace are character-scoped. Oaths remain
  world-scoped, and `ClosedDoor` plus `LongCount` are end-appended ids 8 and 9 (D-173).
- Toll tier contribution is additive after the ordinary or heavy base, capped at 40
  before the existing Will reduction and floor. Scar and mend facts use stable scar ids.
  A current crushed-hand scar suppresses the fitted-brace parry edge until repaired
  (D-173).
- The Salt Fen is the fourth country. Its independent climate stream is derived after
  every earlier draw, its mounted travel is one cell, its three pans are finite, and its
  adder may cross one water cell but must end every turn on walkable ground. Regional
  completion schedules at most one capped peddler restock per world (D-174).
- Movement keys h/j/k/l/y/u/b/n, `.` wait, `>` enter, `<` exit, `g` grab,
  `r` rest, `v` read (shrine or owned loft desk), `m` camp, `i` gear, `c` sheet, `e` eat,
  `o` order. Uppercase H/J/K/L/Y/U/B/N rush on local combat maps.
- Pilot streams must be BOM-less UTF-8 (a BOM deadlocks the pipe).
- Presentation-only pilot actions use `aegis-tools pilot ui <action>` with
  `dismiss-help`, `guide`, `compass`, `log`, `close`, `next`, `previous`, or
  `activate`. These commands do not reach `Game.ApplyKey`, the journal, turns, or RNG.
- The character sheet (`Presenter.DrawSheet`) holds all 18 skills in two stable
  enum-ordered columns of nine.

## Test conventions

- Creation key sequence: folk digit, past digit (`'1'` Soldier, `'3'` HedgeHealer,
  `'5'` ScribesWard), `'0'` shaping done, thing digit, `"00."` extras, `'.'` seals
  the name, then `'.'` confirms the final review. E.g. `"150400..."` makes a
  ScribesWard (lettered, Lore 1).
- Offer digit in a talk menu: `(char)('1' + game.Topics.Count + offerIndex)`.
- Shared internal helpers: `NpcTests.BumpNpc`, `NewsTests.BumpTowner/OfferKey`,
  `FrontierTests.ClimbFells`, `TownDepthTests`' enter-town pattern.
- Debug hooks on Game for tests: `Debug_SetPlayerPos`, `Debug_SetMount`,
  `Debug_ClearSite(kind)`, `Debug_GiveBook`, `Debug_BankLore`,
  `Debug_SetFellWinter(int)`, `Debug_HoldTheDeck`, `Debug_SetSky`.
- D-167 adds `Debug_SetWeather(ClimateBand, WeatherFamily)`, `Debug_SetSeason`, and
  `Debug_SteadEventEligible` for focused weather and event choreography.
- D-168 exposes live `TopicSurfaces`; `WorldEval.ProseSurfaces` is the complete curation
  inventory, and `ProseAudit.ValidateFamilies` is the hard catalog gate.
- D-169 adds `Debug_SetPacingCarry`, `Debug_QueueDeckCadence`, live deck metadata
  validation, and per-reading `PacingDeckOutcome` diagnostics.
- D-170 adds `Debug_FireStorylet` and `Debug_ResolveFormalBout` for focused, deterministic
  proof of character-scoped fact gates and both sides of nonlethal formal resolution.
- D-171's activity acceptance lives in `ActivityBreadthTests`: all eight rush directions,
  two-turn soft tread arithmetic, awareness, Alchemy, crime separation, and knack edges.
- D-172's focused acceptance lives in `CombatMagicDepthTests`: catalogs, flank geometry,
  enemy follow-ons, martial and Spellcraft knacks, hostile and player magic,
  presentation, persistence, and replay.
- D-173's focused acceptance lives in `CompanionConsequencesTests`: target selection,
  footprints, evasion, guest arcs and memories, faction and beast state, scars, Toll,
  brace behavior, oaths, presentation, persistence, and replay. Legacy guest, shade,
  charge, pacing, Toll, and weather tests pin the changed shared contracts.
- D-174's focused acceptance lives in `FenTests` and `ReleaseToolTests`: generator and
  terrain structure, crossings, weather-gated six-turn work, finite pans, adder movement
  and intent, equal conclusions, restock, promoted readers, generator and save rejection,
  release matrix behavior, package inputs, AOT, hashes, smokes, and spoiler-free docs.
- Story-pinned seeds exist (e.g. master 42/43 cycle-2 stories); when a test breaks
  on a story draw, check whether a deliberate re-pin is recorded in decisions.md
  before "fixing" the world.

## What is next (queued, in recommended order)

1. Restart the fresh packaged manual campaign in
   `design/release-audit-1.0.0.md`. Only explicit user approval can make V1-09 and V1-10
   Verified and close Aegis 1.0.

## Handoff hygiene

When a feature lands: verify (tests + the sweep discipline above), commit, write
the decision entry (newest-first block head), tick the roadmap, bump the vision
counter, and note deferred work in the decision's deferrals so it is never lost.
Keep this file's baselines and next-card pointer current with each completed tranche;
the detailed state lives in the docs above.
