# Aegis handoff (updated 2026-07-22, end of the D-168 session)

This file exists so any assistant (or human) can pick the project up cold and keep
moving. It records the things that were living only in session memory: working
conventions, the verification workflow, current baselines, and what is queued next.
The canonical design truth stays where it always was:

- `CLAUDE.md` (repo root): project instructions, roadmap discipline, operating notes.
- `design/roadmap.md`: the living feature tracker. Check items off as they land.
- `design/decisions.md`: numbered decision log. D-001..D-063 ascending, then a
  newest-first block (D-168 currently at its head), then the parking lot of open
  questions at the end. New decisions go at the HEAD of the newest-first block.
- `design/vision.md`: the unified design doc. Line 5 carries the counter, currently
  "(D-001 through D-168)". Bump it whenever a decision lands.
- `design/plan-2026-07.md`: the current build plan. The original sequence and the first
  three Path to 1.0 tranches are done; V1-04 is next (see "What is next" below).
- `design/plan-1.0.md`: the canonical nine-card implementation queue. V1-01 through V1-03
  are Verified; V1-04 through V1-09 are Approved and pending in that order.
- `design/story/`: arc spec and world-story templates. Full story detail lives there.
- `docs/dev-harness.md`: the pilot/sim/journey harness.

## Current state

- Latest completed work: D-168, V1-03, the enumerable narrative surface inventory,
  five-family composer, gated topic catalog, and family-aware WorldEval audit. D-167's
  V1-02 implementation is commit `931263a`.
- Save format: `SaveCodec.Version = 93` (v91 = D-154, v92 = D-166, v93 = D-167; history comments in
  `src/Aegis.Core/SaveCodec.cs`).
- Tests: 877 green (`dotnet test tests/Aegis.Core.Tests/Aegis.Core.Tests.csproj -c Release
  --no-build`).
- The D-168 Release build, full test run, sweep, replay, dump, and worldgen purity gate are green.

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

**Current baselines are v97**, stored under `artifacts/aegis-sweep/`. The prior comparison
set is v96. Every v97 twin pair is byte-identical to its mate and byte-identical to v96:

| seed  | keys  | turns | deaths | fish caught | outcome                    |
|-------|-------|-------|--------|-------------|----------------------------|
| 1     | 25260 | 24066 | 7      | 96          | cycle 13, 12 crossings     |
| 7     | 26298 | 24998 | 9      | 96          | cycle 13, 12 crossings     |
| 99    | 26662 | 25392 | 13     | 96          | cycle 13, 12 crossings     |
| 2024  | 25198 | 24149 | 9      | 96          | cycle 13, 12 crossings     |
| 88888 | 28253 | 26536 | 8      | 92          | cycle 13, 12 crossings     |

Seed 1 sim replay: 25,260 keys, cycle 13, turn 24,066, autumn at position zero, line
owned, and three fresh reaches in the new world. Worldgen: 240 worlds, zero digest
mismatches, 86,282 prose surfaces (12,449 fact details, 20,272 topics, 49,943 storylet
lines, 3,138 scene lines, 240 rumors, 240 ledger entries), five families at their declared
coverage, zero hard prose failures, all twelve band-family weather cells populated,
240 qualifying tarn sites, and all with three reaches. Both prose dump formats exit zero.

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
- Every world opens in autumn. The seed-drawn hard-winter tick 3-5 begins winter;
  later seasons advance every three coarse ticks. Lowlands, road, and fells draw
  independent deterministic three-card weather hands. Weather advances before schedule
  and cadence work on each tick (D-167).
- Prose variants derive only from world seed, fact id, family id, and surface kind. They
  never read gameplay or worldgen RNG, cycle on reread, or enter save state. WorldEval's
  disposable topic catalog may exercise gated answers, but never mutates the measured
  world or a live Game (D-168).
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
- D-167 adds `Debug_SetWeather(ClimateBand, WeatherFamily)`, `Debug_SetSeason`, and
  `Debug_SteadEventEligible` for focused weather and event choreography.
- D-168 exposes live `TopicSurfaces`; `WorldEval.ProseSurfaces` is the complete curation
  inventory, and `ProseAudit.ValidateFamilies` is the hard catalog gate.
- Story-pinned seeds exist (e.g. master 42/43 cycle-2 stories); when a test breaks
  on a story draw, check whether a deliberate re-pin is recorded in decisions.md
  before "fixing" the world.

## What is next (queued, in recommended order)

1. **V1-04: D1 pacing steering** (`design/plan-1.0.md`, D-160). Its design is Approved
   and implementation-ready. Only explicitly elastic random-deck cards may move. Preserve
   every protected causal clock, implement Press and Space exactly as the card specifies,
   carry its diagnostics, and run the complete sweep.
2. **Verify V1-04 under its card and the HANDOFF discipline**, then record its
   implementation decision, check off tranche 4, and advance to V1-05.
3. **V1-05 through V1-09 remain Approved in queue order.** Do not silently widen a card.
   If implementation reveals a substantive contract conflict, present options and a
   recommendation to the user before changing the approved design.

## Handoff hygiene

When a feature lands: verify (tests + the sweep discipline above), commit, write
the decision entry (newest-first block head), tick the roadmap, bump the vision
counter, and note deferred work in the decision's deferrals so it is never lost.
Keep this file's baselines and next-card pointer current with each completed tranche;
the detailed state lives in the docs above.
