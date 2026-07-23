# Aegis handoff (updated 2026-07-23, end of the D-173 session)

This file exists so any assistant (or human) can pick the project up cold and keep
moving. It records the things that were living only in session memory: working
conventions, the verification workflow, current baselines, and what is queued next.
The canonical design truth stays where it always was:

- `CLAUDE.md` (repo root): project instructions, roadmap discipline, operating notes.
- `design/roadmap.md`: the living feature tracker. Check items off as they land.
- `design/decisions.md`: numbered decision log. D-001..D-063 ascending, then a
  newest-first block (D-173 currently at its head), then the parking lot of open
  questions at the end. New decisions go at the HEAD of the newest-first block.
- `design/vision.md`: the unified design doc. Line 5 carries the counter, currently
  "(D-001 through D-173)". Bump it whenever a decision lands.
- `design/plan-2026-07.md`: the current build plan. The original sequence and the first
  eight Path to 1.0 tranches are done; V1-09 is next (see "What is next" below).
- `design/plan-1.0.md`: the canonical nine-card implementation queue. V1-01 through V1-08
  are Verified; V1-09 is Approved and pending.
- `design/story/`: arc spec and world-story templates. Full story detail lives there.
- `docs/dev-harness.md`: the pilot/sim/journey harness.

## Current state

- Latest completed work: D-173, V1-08, companion combat parity, the grain road, bounded
  companion memories, faction readers, beast warmth and recognition, scar aftermath,
  the fitted brace, tier-scaled Toll, and the final two launch oaths.
- Save format: `SaveCodec.Version = 98` (v91 = D-154, v92 = D-166, v93 = D-167,
  v94 = D-169, v95 = D-170, v96 = D-171, v97 = D-172, v98 = D-173; history
  comments in `src/Aegis.Core/SaveCodec.cs`).
- Tests: 968 green (`dotnet test tests/Aegis.Core.Tests/Aegis.Core.Tests.csproj -c Release
  --no-build`).
- The D-173 Release build, full test run, companion diagnostics, sweep, default and
  companion replays, and worldgen purity gate are green.

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

**Current baselines are v102**, stored under `artifacts/aegis-sweep/`. The prior comparison
set is v101. Every v102 twin pair is byte-identical to its mate. Drift is expected and
justified by nearest-body targeting, automatic fellow evasion, full-lane physical
resolution, fewer deaths and recovery loops, and bounded pilot-route repairs:

| seed  | keys  | turns | deaths | drift keys / turns / deaths | outcome                |
|-------|-------|-------|--------|------------------------------|------------------------|
| 1     | 26891 | 25825 | 9      | -59 / -60 / 0                | cycle 13, 12 crossings |
| 7     | 32920 | 25931 | 9      | +55 / +43 / 0                | cycle 13, 12 crossings |
| 99    | 31218 | 27006 | 8      | -520 / -572 / -2             | cycle 13, 12 crossings |
| 2024  | 35273 | 33414 | 7      | +172 / +159 / 0              | cycle 13, 12 crossings |
| 88888 | 33008 | 31845 | 7      | -2869 / -2290 / -1           | cycle 13, 12 crossings |

Seed 1 sim replay: 26,891 keys, cycle 13, turn 25,825, nine deaths. The opt-in
companion seed-6 route completes twelve crossings in 38,588 keys and 33,481 turns. It
demonstrates every required live companion, faction, beast, scar, Toll, brace, and oath
route and replays exactly through `sim --keys-file`. Its report records 19 guest starts,
14 completions, five organic guest deaths, 47 care acts, 19 physical target choices,
15 evasions, one refused shot, ten grain deliveries, six warm camps, all three beast
recognitions, 12 pony tamings, 91 ridden steps, both bounded memories, one cured scar,
312 brace parries, a 40-point capped tier contribution, and both new oaths. The route
never deliberately kills a guest. All five default JSON journeys and the companion JSON
journey match their prose metrics. Worldgen: 240 worlds, zero digest mismatches, 89,402
prose surfaces (12,689 fact details, 20,752 topics, 52,343 storylet lines, 3,138 scene
lines, 240 rumors, 240 ledger entries), five families at their declared coverage, zero
hard failures, and the same two expected warnings for legacy fixed surfaces and
fixed-heavy composition.

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
- Movement keys h/j/k/l/y/u/b/n, `.` wait, `>` enter, `<` exit, `g` grab,
  `r` rest, `v` read (shrine or owned loft desk), `m` camp, `i` gear, `c` sheet, `e` eat,
  `o` order. Uppercase H/J/K/L/Y/U/B/N rush on local combat maps.
- Pilot streams must be BOM-less UTF-8 (a BOM deadlocks the pipe).
- The character sheet (`Presenter.DrawSheet`) holds all 18 skills in two stable
  enum-ordered columns of nine.

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
- D-171's activity acceptance lives in `ActivityBreadthTests`: all eight rush directions,
  two-turn soft tread arithmetic, awareness, Alchemy, crime separation, and knack edges.
- D-172's focused acceptance lives in `CombatMagicDepthTests`: catalogs, flank geometry,
  enemy follow-ons, martial and Spellcraft knacks, hostile and player magic,
  presentation, persistence, and replay.
- D-173's focused acceptance lives in `CompanionConsequencesTests`: target selection,
  footprints, evasion, guest arcs and memories, faction and beast state, scars, Toll,
  brace behavior, oaths, presentation, persistence, and replay. Legacy guest, shade,
  charge, pacing, Toll, and weather tests pin the changed shared contracts.
- Story-pinned seeds exist (e.g. master 42/43 cycle-2 stories); when a test breaks
  on a story draw, check whether a deliberate re-pin is recorded in decisions.md
  before "fixing" the world.

## What is next (queued, in recommended order)

1. **V1-09: next region and 1.0 release closure** (`design/plan-1.0.md`, D-165).
   Its design is Approved and implementation-ready. Read the complete card before editing
   and preserve every stated boundary and dependency.
2. **Verify V1-09 under its card and the HANDOFF discipline**, then record its
   implementation decision, check off tranche 9, and complete the release and roadmap
   audits required by the card.
3. Do not silently widen the final card. If implementation reveals a substantive contract
   conflict, present options and a recommendation to the user before changing the approved
   design.

## Handoff hygiene

When a feature lands: verify (tests + the sweep discipline above), commit, write
the decision entry (newest-first block head), tick the roadmap, bump the vision
counter, and note deferred work in the decision's deferrals so it is never lost.
Keep this file's baselines and next-card pointer current with each completed tranche;
the detailed state lives in the docs above.
