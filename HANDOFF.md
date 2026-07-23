# Aegis handoff (updated 2026-07-22, end of the D-170 session)

This file exists so any assistant (or human) can pick the project up cold and keep
moving. It records the things that were living only in session memory: working
conventions, the verification workflow, current baselines, and what is queued next.
The canonical design truth stays where it always was:

- `CLAUDE.md` (repo root): project instructions, roadmap discipline, operating notes.
- `design/roadmap.md`: the living feature tracker. Check items off as they land.
- `design/decisions.md`: numbered decision log. D-001..D-063 ascending, then a
  newest-first block (D-170 currently at its head), then the parking lot of open
  questions at the end. New decisions go at the HEAD of the newest-first block.
- `design/vision.md`: the unified design doc. Line 5 carries the counter, currently
  "(D-001 through D-170)". Bump it whenever a decision lands.
- `design/plan-2026-07.md`: the current build plan. The original sequence and the first
  five Path to 1.0 tranches are done; V1-06 is next (see "What is next" below).
- `design/plan-1.0.md`: the canonical nine-card implementation queue. V1-01 through V1-05
  are Verified; V1-06 through V1-09 are Approved and pending in that order.
- `design/story/`: arc spec and world-story templates. Full story detail lives there.
- `docs/dev-harness.md`: the pilot/sim/journey harness.

## Current state

- Latest completed work: D-170, V1-05, the guild loft and fitted workshop, law-day
  lists and judicial challenge, stable six-book shelf, and complete journey diagnostics.
  D-169's V1-04 implementation is commit `936115c`.
- Save format: `SaveCodec.Version = 95` (v91 = D-154, v92 = D-166, v93 = D-167,
  v94 = D-169, v95 = D-170; history comments in `src/Aegis.Core/SaveCodec.cs`).
- Tests: 900 green (`dotnet test tests/Aegis.Core.Tests/Aegis.Core.Tests.csproj -c Release
  --no-build`).
- The D-170 Release build, full test run, town diagnostics, sweep, replay, and worldgen
  purity gate are green.

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

**Current baselines are v99**, stored under `artifacts/aegis-sweep/`. The prior comparison
set is v98. Every v99 twin pair is byte-identical to its mate. Drift is expected and
justified by added town travel and sittings, formal bouts, book purchases, and the recurring
property and workshop economy, which can shift later routes in either direction:

| seed  | keys  | turns | deaths | entries / bouts | lofts / workshops | outcome                |
|-------|-------|-------|--------|-----------------|-------------------|------------------------|
| 1     | 26386 | 25172 | 8      | 8 / 24          | 8 / 8             | cycle 13, 12 crossings |
| 7     | 26507 | 25181 | 8      | 7 / 21          | 7 / 7             | cycle 13, 12 crossings |
| 99    | 27481 | 26176 | 10     | 9 / 27          | 9 / 9             | cycle 13, 12 crossings |
| 2024  | 26312 | 25217 | 9      | 8 / 24          | 8 / 8             | cycle 13, 12 crossings |
| 88888 | 26751 | 25471 | 9      | 8 / 24          | 8 / 8             | cycle 13, 12 crossings |

Every entry above ends in a championship and every formal bout in a yield. Every eligible
world boxes and retrieves coin, rests in the loft, commissions its workshop, and completes
one real wear-moving workshop sitting. Each final run records four desk sittings and all
six books owned and read. Judicial results are zero by design because the pilot stays
crime-free; focused tests prove both outcomes.

Seed 1 sim replay: 26,386 keys, cycle 13, turn 25,172, eight deaths, autumn at position
zero, line owned, all six books owned and read, and three fresh reaches in the new world.
All five final JSON journeys parse and match their prose metrics. Worldgen: 240 worlds,
zero digest mismatches, 87,722 prose surfaces (12,689 fact details, 20,752 topics, 50,663
storylet lines, 3,138 scene lines, 240 rumors, 240 ledger entries), five families at their
declared coverage, zero hard failures, and the two expected warnings for legacy fixed
surfaces and fixed-heavy composition.

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
- Every live season-deck card explicitly declares pacing metadata. The teller fixes one
  call before the coarse tick advances, consumes one ordinary cadence roll per live tick,
  and may steer only Elastic cards. Scheduled futures, faction work, weather, durations,
  player-triggered content, combat, sites, and worldgen remain protected. Crossing resets
  pacing carry and Space authority but keeps the run-wide diagnostic book (D-169).
- The guild loft, boxed purse, fitted workshop, lists entry, and judicial challenge are
  world-scoped. Books and their reading progress remain character-scoped. Formal combat
  uses ordinary actions and time but converts a lethal result to a yield, with no death,
  remnant, loot, Essence, bestiary study, faction kill, guest, or summon (D-170).
- Movement keys h/j/k/l/y/u/b/n, `.` wait, `>` enter, `<` exit, `g` grab,
  `r` rest, `v` read (shrine or owned loft desk), `m` camp, `i` gear, `c` sheet, `e` eat,
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
- D-169 adds `Debug_SetPacingCarry`, `Debug_QueueDeckCadence`, live deck metadata
  validation, and per-reading `PacingDeckOutcome` diagnostics.
- D-170 adds `Debug_FireStorylet` and `Debug_ResolveFormalBout` for focused, deterministic
  proof of character-scoped fact gates and both sides of nonlethal formal resolution.
- Story-pinned seeds exist (e.g. master 42/43 cycle-2 stories); when a test breaks
  on a story draw, check whether a deliberate re-pin is recorded in decisions.md
  before "fixing" the world.

## What is next (queued, in recommended order)

1. **V1-06: character and activity breadth** (`design/plan-1.0.md`, D-162). Its design is
   Approved and implementation-ready. Read the complete card before editing and preserve
   every stated boundary and dependency.
2. **Verify V1-06 under its card and the HANDOFF discipline**, then record its
   implementation decision, check off tranche 6, and advance to V1-07.
3. **V1-07 through V1-09 remain Approved in queue order.** Do not silently widen a card.
   If implementation reveals a substantive contract conflict, present options and a
   recommendation to the user before changing the approved design.

## Handoff hygiene

When a feature lands: verify (tests + the sweep discipline above), commit, write
the decision entry (newest-first block head), tick the roadmap, bump the vision
counter, and note deferred work in the decision's deferrals so it is never lost.
Keep this file's baselines and next-card pointer current with each completed tranche;
the detailed state lives in the docs above.
