# Aegis handoff (updated 2026-07-22, end of the D-166 session)

This file exists so any assistant (or human) can pick the project up cold and keep
moving. It records the things that were living only in session memory: working
conventions, the verification workflow, current baselines, and what is queued next.
The canonical design truth stays where it always was:

- `CLAUDE.md` (repo root): project instructions, roadmap discipline, operating notes.
- `design/roadmap.md`: the living feature tracker. Check items off as they land.
- `design/decisions.md`: numbered decision log. D-001..D-063 ascending, then a
  newest-first block (D-166 currently at its head), then the parking lot of open
  questions at the end. New decisions go at the HEAD of the newest-first block.
- `design/vision.md`: the unified design doc. Line 5 carries the counter, currently
  "(D-001 through D-166)". Bump it whenever a decision lands.
- `design/plan-2026-07.md`: the current build plan. The original sequence and the first
  Path to 1.0 tranche are done; V1-02 is next (see "What is next" below).
- `design/plan-1.0.md`: the canonical nine-card implementation queue. V1-01 is Verified;
  V1-02 through V1-09 are Approved and pending in that order.
- `design/story/`: arc spec and world-story templates. Full story detail lives there.
- `docs/dev-harness.md`: the pilot/sim/journey harness.

## Current state

- Latest completed work: D-166, V1-01, the fourth high-fells site and finite fishing
  loop. The preceding commit is `c2c576c`, D-165's approved V1-09 design and release gate.
- Save format: `SaveCodec.Version = 92` (v91 = D-154, v92 = D-166; history comments in
  `src/Aegis.Core/SaveCodec.cs`).
- Tests: 848 green (`dotnet test tests/Aegis.Core.Tests/Aegis.Core.Tests.csproj -c Release
  --no-build`).
- The D-166 Release build, full test run, sweep, replay, and worldgen purity gate are green.

## Working conventions (were in user-level config, not visible to a new tool)

- **Never use em dashes** (or en dashes as sentence separators) in ANYTHING written
  for this project: prose, docs, code comments, commit messages, chat. Use commas,
  colons, parentheses, or separate sentences. Plain hyphens in ranges and compound
  words are fine.
- Always use raw string literals (`"""`) for multi-line strings in C#.
- Git identity: author is `eric-patton <ursine.blue@proton.me>`. **Never** list an
  AI as author or co-author; no `Co-Authored-By: Claude` trailers or similar.
- Committing without asking is fine. This repo is **local only, no remote: never
  push**. No destructive git ever: no `reset --hard`, `push --force` (any variant),
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
  - `aegis sim --seed N --keys "<journal>"` replays keys and prints a JSON
    snapshot. Replaying a journey's journal must reproduce the journey's end state
    exactly (keys, cycle, turn). Truncated journals replay safely, which makes
    prefix-replay a valid probe of mid-run state.
  - `aegis worldgen --json` generates every world twice and hash-compares; exit 0
    is the purity gate (exit 2 = nondeterminism).

### The sweep discipline (run after every engine change)

Run seeds `1, 7, 99, 2024, 88888` through `journey --cycles 12 --emit-keys`
TWICE each (twins), save outputs as `vNN-sw{seed}.txt` / `vNN-tw{seed}.txt`, and
hash-compare each pair: twins must be byte-identical (determinism). Then compare
against the previous baseline set to see (and justify) drift. Then sim-replay at
least seed 1's journal and require exact key/cycle/turn match. Then run the
worldgen purity gate.

**Current baselines are v95**, stored under `artifacts/aegis-sweep/`. The prior comparison
set is v94. Every v95 twin pair is byte-identical:

| seed  | keys  | turns | deaths | fish caught | outcome                    |
|-------|-------|-------|--------|-------------|----------------------------|
| 1     | 25135 | 24009 | 7      | 96          | cycle 13, 12 crossings     |
| 7     | 26128 | 24798 | 10     | 96          | cycle 13, 12 crossings     |
| 99    | 29256 | 27491 | 12     | 96          | cycle 13, 12 crossings     |
| 2024  | 25103 | 24090 | 9      | 96          | cycle 13, 12 crossings     |
| 88888 | 28012 | 26304 | 7      | 92          | cycle 13, 12 crossings     |

Seed 1 sim replay: 25,135 keys, cycle 13, turn 24,009, line owned, three fresh reaches
in the new world. Worldgen: 240 worlds, zero digest mismatches, 240 qualifying sites,
all with three reaches and no resident enemy.

## Engine invariants (break these and old saves die)

- Saves are seed + key journal, replayed on load. Any change to worldgen draws or
  to what a key does requires a `SaveCodec.Version` bump with a history comment.
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
- Movement keys h/j/k/l/y/u/b/n, `.` wait, `>` enter, `<` exit, `g` grab,
  `r` rest, `v` read (shrine only), `m` camp, `i` gear, `c` sheet, `e` eat,
  `o` order.
- Pilot streams must be BOM-less UTF-8 (a BOM deadlocks the pipe).
- The character sheet (`Presenter.DrawSheet`) is full at 14 skills: the NEXT
  skill added needs the skills column-paired (comment in place).

## Test conventions

- Creation key sequence: folk digit, past digit (`'1'` Soldier, `'3'` HedgeHealer,
  `'5'` ScribesWard), `'0'` shaping done, thing digit, `"00."` extras, `'.'` seals
  the name. E.g. `"150400.."` makes a ScribesWard (lettered, Lore 1).
- Offer digit in a talk menu: `(char)('1' + game.Topics.Count + offerIndex)`.
- Shared internal helpers: `NpcTests.BumpNpc`, `NewsTests.BumpTowner/OfferKey`,
  `FrontierTests.ClimbFells`, `TownDepthTests`' enter-town pattern.
- Debug hooks on Game for tests: `Debug_SetPlayerPos`, `Debug_SetMount`,
  `Debug_ClearSite(kind)`, `Debug_GiveBook`, `Debug_BankLore`,
  `Debug_SetFellWinter(int)`, `Debug_HoldTheDeck`, `Debug_SetSky`.
- Story-pinned seeds exist (e.g. master 42/43 cycle-2 stories); when a test breaks
  on a story draw, check whether a deliberate re-pin is recorded in decisions.md
  before "fixing" the world.

## What is next (queued, in recommended order)

1. **V1-02: Weather and seasons v1** (`design/plan-1.0.md`, D-158). Its design is
   Approved and implementation-ready. Build the shared seasonal calendar, three climate
   bands, deterministic weather hands, forecasts, exposure rules, enabled stead events,
   pilot behavior, persistence, presentation, evaluation, and its v93 save bump exactly
   as the card specifies.
2. **Verify V1-02 through this full sweep discipline**, then record its implementation
   decision, check off tranche 2, and advance to V1-03.
3. **V1-03 through V1-09 remain Approved in queue order.** Do not silently widen a card.
   If implementation reveals a substantive contract conflict, present options and a
   recommendation to the user before changing the approved design.

## Handoff hygiene

When a feature lands: verify (tests + the sweep discipline above), commit, write
the decision entry (newest-first block head), tick the roadmap, bump the vision
counter, and note deferred work in the decision's deferrals so it is never lost.
Keep this file's baselines and next-card pointer current with each completed tranche;
the detailed state lives in the docs above.
