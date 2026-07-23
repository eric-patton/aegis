# Aegis 1.0 Design Queue

This is the canonical implementation queue for the finite road to Aegis 1.0 adopted
in D-155 and made design-first in D-157. `design/roadmap.md` remains the source of
truth for feature status. This file owns the approved scope, dependencies, acceptance
criteria, and decision associations for the ten 1.0 tranches.

## Working contract

- Every card has a stable `V1-XX` identifier and one of four design statuses: Draft,
  Approved, Implemented, or Verified.
- Substantive choices are made with the user, recorded in `design/decisions.md`, and
  associated with the card here. Implementation discoveries amend the card through a
  later decision rather than silently changing its contract.
- No 1.0 card enters implementation until every unbuilt card is Approved. This frontloads
  dependencies and removes design pauses from the build sequence.
- Approval fixes player-facing behavior, system boundaries, persistence, dependencies,
  and acceptance criteria. Exact prose, generated geometry, and balance tuning remain
  adjustable during implementation unless another card depends on them.
- A card reaches Implemented when its approved behavior exists. It reaches Verified only
  after its focused checks and any required engine sweep pass, its roadmap lines close,
  and its documentation is current.
- Work explicitly excluded from all ten cards is post-1.0 unless a later user decision
  promotes it into the gate.

## Queue at a glance

| Card | Tranche | Design status | Decisions | Implementation |
|------|---------|---------------|-----------|----------------|
| V1-01 | High-fells capstone, the black tarn | Verified | D-156, D-166 | Completed |
| V1-02 | Weather and seasons v1 | Verified | D-158, D-167 | Completed |
| V1-03 | D3 prose-variety infrastructure | Verified | D-159, D-168 | Completed |
| V1-04 | D1 pacing steering | Verified | D-160, D-169 | Completed |
| V1-05 | Town and economy depth | Verified | D-161, D-170 | Completed |
| V1-06 | Character and activity breadth | Verified | D-162, D-171 | Completed |
| V1-07 | Combat and magic depth | Verified | D-163, D-172 | Completed |
| V1-08 | Companions, factions, and consequences | Verified | D-164, D-173 | Completed |
| V1-09 | Next region and 1.0 release closure | Implemented | D-165, D-174 | Original candidate superseded |
| V1-10 | SadConsole client and release recovery | Implemented | D-175, D-176, D-177 | Package and signoff pending |

## V1-01: High-fells capstone, the black tarn

**Design status:** Verified

**Decisions:** D-156, D-166

**Roadmap association:** Path to 1.0 tranche 1; B4 later regions; wilderness fishing  
**Dependencies:** D-138 camping, D-140 town market, D-146 high fells, D-153 regional
goods, D-155 release sequence  
**Implementation status:** Completed and verified 2026-07-22

### Approved behavior

- The black tarn is the fourth and final high-fells site in the current regional-density
  tranche. It is a deterministic water-and-bank map with three reachable fishing reaches.
- It is a wilderness and gathering site, not a fighting deep. It has no resident monster
  or boss. Reaching it, the fells' existing conditions, and three eight-turn fishing
  sittings are its risk and cost.
- The waykeeper sells a permanent hook and line for 6 coin. It occupies no gear slot,
  has no durability, and survives death and crossings.
- Standing at an unworked reach and pressing `g` spends eight exposed turns and yields
  `1 + Survival bonus` tarn trout, capped at 3. Each sitting feeds Survival once. There
  is no success roll, bait inventory, or renewable grind.
- A reach is worked once per world. The site completes when all three reaches are worked.
  Reaches return only in the next world.
- Tarn trout are a distinct carried good that survives death and crossings. A fixed
  cooking fire offers a separate fish-cooking entry, preserving the sell-or-cook choice.
  Camp cooking follows the existing automatic cooking model. One fish becomes one ration,
  the existing Cooking bonus improves the batch, and the five-ration cap remains.
  Successful fish cooking feeds Cooking once.
- The town provisioner buys fish for 3 coin each. Commerce, guild bond, tithe, town law,
  and other general town-sale rules apply normally.
- Site and terrain identifiers are end-appended. Generation uses a named stream after all
  existing fells draws, preserves established placements, and keeps the new entrance clear
  of prior sites, herbs, and tarn-iron.
- Presentation includes live and completed descriptions, distinct glyphs, sidebar guidance,
  a world fact, and a waykeeper reader. The waykeeper's topics and offers remain within the
  nine-digit cap.
- The pilot buys the line once, fishes all three reaches per world, cooks fish when ration
  space exists, and sells surplus in town. Journey prose and JSON count fish caught, cooked,
  sold, and sale coin. Snapshots expose line ownership, carried fish, and remaining reaches.
  Worldgen evaluation records the site and exactly three reachable reaches.
- Implementation advances the save format from v91 to v92 because new menu digits, terrain,
  carried state, and `g` behavior alter replay.

### Acceptance

- Focused tests cover deterministic generation, reachability, purchase and short coin,
  exhaustion, exact yield, Survival growth, cooking, Cooking growth, the ration cap, town
  sale rules, death and crossing persistence, site completion, presentation, and pilot use.
- Worldgen evaluation proves one black tarn with exactly three reachable fishing reaches
  per world across a broad seed sample.
- The full engine sweep passes: clean Release build and tests, five-seed journey twins,
  exact sim replay, justified drift from the previous v94 baseline, and worldgen purity.
- The D-155 path tracker checks off tranche 1 only after implementation and verification.

### Verification record

- Release build: zero warnings and zero errors. Full suite: 848 of 848 tests passed,
  including 10 focused Black Tarn tests and broad deterministic generation coverage.
- Five seeds completed twelve crossings twice each. Every v95 twin pair is byte-identical.
  Across the five first runs the pilot caught and sold 476 trout. Cooking remained at zero
  because the pilot reached the activity with a full ration bag; fixed-fire and camp
  cooking are exercised by focused tests.
- Seed 1 sim replay matches exactly at 25,135 keys, cycle 13, turn 24,009. The worldgen
  purity gate generated 240 worlds with zero digest mismatches; every world contained one
  qualifying site with exactly three reaches and no resident enemy.
- Drift from v94 is expected and accepted: the added fells errand spends travel and
  fishing turns, adds a town sale, and changes later pilot progression. D-166 records the
  implementation and closes tranche 1.

### Explicit exclusions

- No fishing skill, random catch roll, bait, tackle wear, renewable pools, resident enemy,
  boss, fish spoilage, or within-world re-tenanting.
- Weather-specific catch rules belong to V1-02 and may be added only through that card.

## V1-02: Weather and seasons v1

**Design status:** Verified

**Decisions:** D-158, D-167

**Roadmap association:** Path to 1.0 tranche 2; weather and seasons; A2 follow-ons  
**Known dependencies:** Scheduled facts, stead event deck, road sky, wolf-winter, regions,
camping, economy, black tarn  
**Implementation status:** Completed and verified 2026-07-22

### Approved behavior

- One world season is shared across all regions. The valley, road, town, and high fells
  express that common season through their own local weather rather than advancing on
  independent seasonal calendars.
- Existing road sky, hard winter, and wolf-winter behavior will be folded into this
  unified seasonal model. Weather generation remains deterministic, with regional
  weather using independent named streams so one region's draws do not move another's.
- Current conditions and forecasts must remain coherent across local presentation and
  news: the world agrees on the season while each region reports its own expression of it.
- Each world begins in autumn. The existing seed-drawn hard winter at coarse tick 3-5
  marks the arrival of winter, preserving its current warning and landing cadence.
- After winter arrives, the shared season advances every three coarse ticks through
  spring, summer, autumn, and winter, looping if the bearer remains in the world.
- The high fells keep their established local lag: wolf-winter begins one tick after the
  lowland winter arrives and stands for three ticks. That lag is a regional expression of
  the shared season rather than a separate seasonal calendar.
- Weather uses three climate bands: the valley and town share one lowland band, the east
  road has its own band, and the high fells have their own band. Town weather therefore
  cannot contradict the valley around it, while the road and fells retain distinct local
  conditions and forecasts.
- At each season change, every climate band receives a deterministic three-condition
  weather hand, one condition for each coarse tick of that season. The hand guarantees
  one condition characteristic of the season; its other two conditions are weighted
  draws that may repeat, allowing a front to persist without making every season alike.
- Each climate band's hands draw only from that band's independent named stream. Creating
  or changing weather in one band therefore cannot move another band's sequence.
- Weather uses four shared mechanical families: Calm, Wet, Wind, and Cold. Each climate
  band gives those families local wording and severity rather than introducing unrelated
  regional rules.
- Spring guarantees one Wet card, summer one Calm card, autumn one Wind card, and winter
  one Cold card in every regional hand. The other two cards remain season-weighted draws.
- Existing road Clear, Rain, and Cold map to Calm, Wet, and Cold respectively; Wind is the
  road's only new family. Hard winter and wolf-winter remain scheduled overlays rather
  than random cards in a weather hand.
- Direct weather mechanics are confined to exposed outdoor movement and camping. Weather
  does not directly modify combat rolls, damage, skill checks, fishing or gathering
  yields, or ordinary prices.
- Weather may still move the economy and world through authored, forecastable events such
  as hard winter, wolf-winter, and washout. Those explicit facts remain the only route by
  which weather changes stores, prices, or other broader state.
- Calm leaves movement and camping unchanged. Wind halves the healing of an exposed camp.
  Wet suppresses step stamina recovery on the road and fells and halves exposed camp
  healing. Cold has Wet's effects and also refuses a supperless camp on the road or fells.
- Lowland walking always retains its step stamina recovery. Wind, Wet, and Cold can halve
  an exposed lowland camp's healing, but lowland weather never refuses a supperless camp.
- Town, stead, and wayhouse roofs ignore weather. Waystone shelter removes weather's camp
  penalties.
  Fells Cold retains its additional healing reduction unless the great pelt answers it.
  These rules preserve the existing road Rain and Cold arithmetic and the established
  relationship among fells exposure, waystones, and the pelt.
- Forecasts look exactly one coarse tick ahead. The next card in each regional hand can
  be learned, while later cards remain unknown until they advance into the forecast slot.
  This uses the established one-tick omen cadence and keeps longer weather uncertain.
- The sidebar always names the shared season, the current local weather, and the next
  local forecast. A season or local weather change is narrated when its tick lands.
- The road mouth reads the road's current and next weather before travel, and the fells
  track does the same before a climb. Existing season-news and waykeeper conversations
  repeat the relevant forecasts without consuming new talk-menu digits.
- Snapshots expose the season, the current position within it, and current and next
  weather for all three climate bands. This supports the pilot and tests without granting
  the player remote omniscience through the ordinary sidebar.
- V1-02 adds no generic weather clothing, tent, or consumable. Counterplay comes from
  forecasts and timing, roofed stead, town, or wayhouse rest, carrying supper for Cold,
  Held Road waystones when present, and the great pelt's existing answer to fells Cold.
- Waiting for better weather remains a real choice because the coarse world clock and its
  other events continue to advance. Poor weather never requires waiting; the bearer may
  accept the exposure when urgency outweighs comfort.
- The existing stead deck becomes season-gated. Far fields is eligible in spring or
  summer, drovers in autumn, the fords washout in spring, and the banns and wedding in
  summer. Hard winter remains the winter anchor outside the random deck.
- An eligible card that is not drawn waits for the next return of its season. Existing
  consequences and once-per-world fact guards remain unchanged.
- Two once-per-world cards grow the deck from four to six. Haying days is eligible during
  summer Calm, restores one store up to the current maximum, and may lift a levy through
  the existing recovery rules. Late frost is eligible during spring Cold and costs one
  store unless a standing granary prevents the loss.
- Both new cards use the ordinary one-tick weather forecast as their warning, write and
  narrate their own facts, and leave the weather itself unchanged.
- A seventh once-per-world card, the season's bargain, is eligible in autumn while the
  stead is below its store maximum. It opens a one-tick offer in the existing larder trade
  surface without adding a talk digit or a named visitor.
- The bargain restores one store for 6 coin. A bearer at the stead's friend rung or above
  with no standing Shame pays 4 coin; a bearer at the unwelcome Shame rung or above is
  refused. A purchase may lift a levy through the existing recovery rules but grants no
  Regard, preventing a coin-to-reputation loop.
- The offer expires on the next coarse tick whether taken or declined. The pilot accepts
  it only when affordable and the stead can use the store.
- Under Held Road, waystones shelter camps from Wind, Wet, and Cold penalties but do not
  alter weather or restore weather-suppressed step stamina. Grave Market and Horned Law
  do not alter seasons, weather hands, or forecasts.
- Twist selection and regional weather use independent streams. Seasonal events and twist
  consequences may coexist whenever their established clocks and conditions allow, with
  no compatibility table or special exclusions.
- Season, weather hands, forecasts, and temporary seasonal offers are world-scoped runtime
  state rebuilt by journal replay rather than separately serialized. A crossing clears
  them, and the new world begins in autumn with fresh hands and its own seed-drawn winter
  arrival. No weather memory or seasonal advantage crosses on the character.
- New named weather streams derive after existing streams so established world layouts do
  not move. Assuming V1-01 advances save v91 to v92 first, V1-02 advances v92 to v93
  because weather changes movement, camping, event eligibility, and journaled outcomes.
- The pilot travels normally through Calm, Wind, and Wet. Before entering the road or
  fells in Cold, it waits under a roof for at most one coarse tick only when it lacks
  supper or is already badly hurt. It never waits to improve gathering or fishing yields.
- Ordinary camp policy remains in force so weather changes real recovery. The pilot buys
  the season's bargain when affordable and useful.
- Journey prose and JSON report regional ticks by weather family, exposed camps by
  condition, forecast-driven deferrals, Cold camp refusals, Haying days, Late frost,
  granary prevention, and bargain outcomes. Across twelve worlds the journey should
  exercise every family in every band; focused tests cover any branch absent from a
  particular sweep seed.
- On each coarse tick, the shared season advances when due and every climate band turns
  to its forecast condition before other cadence work. Scheduled futures then fire and
  retain first claim on the night. If unclaimed, ordinary raid or store recovery runs,
  followed by the stead deck evaluating the newly current season and weather. Temporary
  durations advance afterward, and the read-only teller observes last. A one-tick offer
  expires at the start of the next coarse tick, before another deck card can be dealt.
- Every seasonal hand reserves one slot for its signature family; either weighted draw may
  add another. Across a full year every family has a nonzero chance in every climate band.
  Lowlands favor Calm and Wet, the road favors Wet and Wind, and the fells favor Wind and
  Cold. Immediate repeats are allowed. Exact integer weights remain implementation tuning
  within those constraints.
- Local presentation names the shared families in the region's own terms: lowland fair,
  rain, hard wind, and frost; road clear, rain, crosswind, and cold; fells clear, wet mist,
  gale, and killing cold. Exact descriptive prose remains adjustable during implementation.
- The deck keeps its existing one-in-three draw chance, at most one card per eligible tick,
  and once-per-world fact guards. Haying days is eligible only below maximum stores. Late
  frost remains eligible under a granary because the prevented loss is narrated and
  recorded; without one, ordinary store, price, levy, and bare-loft consequences apply.
- The season's bargain grants no skill growth or Regard. A refusal for an unwelcome bearer
  spends the card. Weather forecasts reveal the condition that could enable a weather card,
  not a guaranteed deck draw.
- Fishing and cooking keep their approved yields and rules. Their turns advance the world
  clock normally, and any camp involved receives the current weather's recovery effect.
- Current conditions and forecasts remain compact runtime state rather than writing a fact
  every tick. Season transitions and substantive seasonal events write facts. Only a local
  weather change narrates automatically; remote bands remain available through their
  designated readers. Help text explains the families, exposure effects, forecast notation,
  and shelter rules.

### Acceptance

- Focused tests cover the autumn start, seed-drawn winter arrival, three-tick progression,
  year looping, guaranteed signatures, deterministic hands, permitted repeats, independent
  regional streams, and correct current and next forecasts at every transition.
- Presentation tests cover the sidebar, road mouth, fells track, existing conversation
  readers, help, and snapshot fields.
- Mechanical tests cover every movement and camp family, lowland leniency, roofs, waystone
  shelter, supperless Cold, fells Cold, and the great pelt.
- Event tests cover the existing cards' season gates, Haying days, Late frost, granary
  prevention, all season's-bargain terms, expiry, and store, price, and levy consequences.
- Boundary tests prove twist combinations, unchanged combat, fishing, gathering, and
  ordinary price rules, crossing reset, journal replay, and the expected v93 save behavior.
- Pilot tests and journey output cover every new policy and counter. Broad seeded evaluation
  proves all four families occur in all three bands while signature and regional-bias
  constraints hold.
- Implementation is complete only after a clean Release build and tests, five-seed journey
  twins, exact sim replay, justified drift from the then-current baseline, and worldgen
  purity all pass under the HANDOFF sweep discipline.

### Implementation record

- D-167 builds the approved calendar, climate hands, forecasts, exposure rules, deck
  gates, two weather cards, one-tick larder bargain, readers, snapshot, help, pilot,
  journey metrics, and WorldEval coverage. Save v92 advances to v93.
- The opening autumn's first card holds through any seed-drawn lead beyond the regular
  three ticks, then the full three-card hand walks into winter. This preserves D-132's
  tick 3-5 arrival without inventing a fourth or fifth autumn card.
- Release build: zero warnings and zero errors. Tests: 862 passed, including 14 focused
  `WeatherTests` plus the existing road, fells, schedule, facility, twist, economy,
  fishing, gathering, combat, presentation, save, and replay coverage.
- Five v96 twelve-crossing twin pairs for seeds 1, 7, 99, 2024, and 88888 are
  byte-identical. All five reach cycle 13. Seed 1 replays exactly at 25,260 keys and
  turn 24,066. Drift from v95 is bounded and follows seasonal stores, weather recovery,
  and forecast timing.
- The journey sweep records every weather family in every climate band. It exercises
  exposed Wet, Wind, and Cold camps across the five first runs, three forecast deferrals,
  Haying days, Late frost, offered, bought, and expired bargains. Focused tests prove the
  Cold refusal, granary prevention, Shame refusal, friend price, roofs, waystones, pelt,
  lowland leniency, and unchanged ordinary prices.
- Worldgen evaluates 240 worlds twice with zero digest mismatches. Every one of the twelve
  band-family cells has nonzero coverage, with the least common cell still appearing
  2,024 times in the four-year-per-world hand audit.

### Explicit exclusions

- No direct weather damage, combat modifier, weather skill check, catch or gathering yield
  change, spoilage, generic weather clothing, tent, forecast purchase, player calendar
  control, weather-specific enemy, independent town climate, or full atmospheric simulation.

## V1-03: D3 prose-variety infrastructure

**Design status:** Verified
**Decisions:** D-159, D-168
**Roadmap association:** Path to 1.0 tranche 3; D3  
**Dependencies:** Fact graph, storylets, scenes, talk topics, worldgen `--dump`, WorldEval
**Implementation status:** Completed and verified 2026-07-22; V1-04 followed under D-169

### Approved behavior

- D3 owns the player-facing narrative surfaces that express or interpret world facts:
  fact details, storylet lines, scene lines, and ask-about topic answers. Mechanical
  action feedback, combat narration, trade responses, menus, help text, and item
  descriptions remain outside this tranche.
- Every owned surface becomes enumerable as a `ProseSurface` with a stable source id,
  surface kind, optional fact family, variant id, raw text, normalized skeleton, reuse
  policy, and origin. Initial kinds cover fact detail, rumor, topic, ledger, song,
  epitaph, storylet, and scene, including kinds built now for later content.
- A `ProseFamily` binds a fact pattern to one or more surface renderings. Each rendering
  contains authored compatible variant bundles. A bundle may carry several fragments or
  lines, but fragments never mix across bundles. V1 has no recursive grammar and no free
  combinatorial assembly.
- Templates read a validated `ProseContext`: structured fact fields, generated names and
  places, people, and explicit values supplied by the caller. Unknown tokens, missing
  values, duplicate ids, empty variants, and unresolved placeholders are hard errors.
  Migrated surfaces do not compose by embedding another surface's already-rendered text.
- Variant selection is a pure derivation of world seed, fact id, family id, and surface
  kind. It consumes no gameplay, storylet, or worldgen RNG and stores no state. The same
  fact on the same surface is stable within a world, different surfaces derive
  independently, and repeated reading does not cycle the text.
- Every family declares one reuse class. Fixed permits one intentional rendering. Rare
  requires at least two variants, Standard at least three, and Frequent at least four.
  Each migrated fact family renders through at least two surface kinds, and one
  representative family exercises four kinds to prove the broad contract.
- All existing fact details, storylets, scenes, and topics enter the enumerable inventory.
  Most keep their current wording and are marked Fixed. The composed vertical slice
  migrates five representative fact families spanning generated facts, runtime events,
  and reputation or consequence state. Existing output remains unchanged except where a
  family is deliberately migrated.
- The repetition audit becomes family-aware. Fixed prose remains visible but is not
  treated as failed variation. Hard failures are generator impurity, invalid or unresolved
  tokens, duplicate source/family/variant ids, a declared variation budget not met,
  identical normalized variants inside one variable family, or a declared variable absent
  from the curated catalog. Distribution skew, unrelated cross-family skeleton collisions,
  fixed prose dominating a category, and legacy prose outside composition are warnings.
- `aegis worldgen --dump` remains the human curation view, grouped by family and surface
  with source metadata. `aegis worldgen --dump --json` emits one structured surface record
  per line. The ordinary JSON report gains per-kind counts, family coverage, failures,
  warnings, and variation measures. Hard failures return nonzero; distribution findings
  stay advisory.
- No save bump is expected. The composer changes no key meaning, stored state, shared RNG,
  or world-layout draw. Any implementation discovery that breaks that assumption returns
  to design before code continues.

### Acceptance and sweep requirements

- Focused tests cover the surface records, family and token validation, deterministic
  selection, variation budgets, fixed-versus-variable classification, normalization,
  inventory completeness, topic/storylet/scene enumeration, both dump formats, JSON
  measures, warning behavior, and every hard-failure exit path.
- A representative generated fact, runtime event fact, and reputation or consequence fact
  are each proven through their real reader path as well as the direct catalog audit.
- The standard 30-seed, tier-1-through-8 WorldEval run regenerates purely, emits every
  owned source with stable ids, and records the new metadata-aware baseline.
- Because this changes engine presentation infrastructure, the complete HANDOFF sweep is
  required. Five-seed journey twins remain byte-identical, sim replay is exact, worldgen
  purity passes, and any baseline drift is justified as prose-only with gameplay counts
  unchanged.

### Implementation record

- D-168 adds `ProseSurface`, `ProseFamily`, compatible variant bundles, validated
  `ProseContext`, pure fact-and-surface selection, Fixed/Rare/Standard/Frequent budgets,
  normalized skeletons, provenance, and hard catalog validation.
- Five families form the composed slice: one reaches four surface kinds and the other four
  reach two. Generated, runtime-event, and consequence readers use the composer through
  their real topic paths. Legacy fact, topic, storylet, and scene prose remains enumerable
  and Fixed. A disposable topic catalog exercises gated answers without pilot visits or
  mutation of the measured world.
- WorldEval now runs the family-aware audit and retains the old skeleton view as a
  compatibility measure. Human dumps group by family and source. `--dump --json` emits one
  compact record per line. Ordinary JSON reports per-kind counts, family coverage,
  failures, warnings, and authored-versus-observed variation. Hard failures return nonzero;
  distribution findings remain advisory.
- Verification is complete: clean Release build with zero warnings and errors; 877 tests
  green, including 15 focused prose tests; both dump formats exit zero and parse; 240 worlds
  regenerate with zero digest mismatches; 86,282 surfaces include 20,272 unvisited topic
  records and all five families at their declared coverage; five v97 twelve-crossing twin
  pairs are byte-identical to their mates and to v96; and seed 1 replays 25,260 keys exactly
  to cycle 13 and turn 24,066. Save v93 holds. V1-04 followed under D-169.

### Explicit exclusions

- No runtime AI, localization framework, external content-file migration, recursive
  grammar engine, procedural story generation, wholesale prose rewrite, text cycling,
  or migration of combat, economy, and mechanical feedback.

## V1-04: D1 pacing steering

**Design status:** Verified
**Decisions:** D-145, D-160, D-169
**Roadmap association:** Path to 1.0 tranche 4; D1; pacing authority question  
**Dependencies:** Read-only teller, scheduled facts, coarse tick, stead event deck,
weather and seasons  
**Implementation status:** Completed and verified 2026-07-22

### Approved behavior

- The teller receives narrow editorial authority over the random stead-event deck. Every
  deck card must explicitly declare whether it is elastic. The four existing cards and
  the three V1-02 additions are elastic. Missing or invalid classification fails closed
  as protected and is rejected by validation tests.
- A deck card that creates a scheduled future is elastic only at its initial draw. Once
  the future enters the calendar, its warning, due tick, hold, cancellation, and firing
  are protected.
- Protected clocks are never delayed, hastened, cancelled, or reordered by pacing:
  scheduled futures; raids, watch activity, and store recovery; season changes, weather
  hands, forecasts, and regional weather advancement; temporary durations and seasonal-
  offer expiry; player-triggered storylets, scenes, consequences, and deed responses;
  combat, hostile activity, site state, and world generation.
- The teller computes one call from carried state at the beginning of each coarse tick.
  Season and weather advance, expiring offers close, scheduled futures run, raids or
  recovery run, and only then does the random deck apply that already-made call. Duration
  countdowns follow, and the teller observes and records the completed night last.
- Steady preserves the deck's existing one-in-three cadence and ordinary weighted card
  selection.
- On an open night with at least one eligible elastic card, Press guarantees a draw. The
  ordinary cadence roll is still consumed first. A successful roll is a natural Press
  deal; a failed roll is promoted to a forced Press deal. Seasonal gates, state gates,
  once-per-world guards, pending-future exclusions, and weighted selection remain in force.
- Any actual elastic deck deal satisfies the pressure call and resets the quiet streak
  without adding heat. Three new heatless, eventless tick nights must pass before another
  Press call, so Press cannot drain the finite deck on consecutive ticks.
- A continuous Space episode may suppress at most one otherwise-successful elastic draw
  opportunity. No card is selected, stored, or carried forward. A failed cadence roll does
  not spend the suppression, a protected event claiming the night does not spend it, and
  later Space calls in the same episode allow the ordinary one-in-three cadence after the
  single suppression has been used.
- D-145's heat model remains: each death contributes three heat, a scheduled event that
  claims a night contributes two, raid heat follows the actual take, and one heat cools
  per tick. Routine season or weather changes, commerce, offers, and deck cards add no
  heat. A deck deal answers quiet through the separate quiet-streak reset.
- Current player-triggered storylets remain immediate consequences of player action or
  specific world change and receive no pacing classification. A future ambient,
  time-eligible storylet class may opt into an explicit elastic contract after 1.0.
- Press with no eligible hand creates nothing. A protected claim always wins. Space
  creates no backlog, invalidated cards are never forced, and no catch-up deal occurs.
- The teller draws no RNG. Every live tick consumes exactly one ordinary deck cadence
  roll regardless of call; weighted-selection draws occur only when a card actually deals.
  Pacing state remains world-scoped runtime state rebuilt through journal replay. Crossing
  resets heat, quiet streak, and Space-episode authority while preserving the run-wide book.
- Assuming V1-01 and V1-02 retain their planned save changes and V1-03 remains no-bump,
  V1-04 advances v93 to v94 because altered event timing changes stores, offers, facts,
  and journaled outcomes. World generation remains unaffected.
- Pacing stays invisible in ordinary play. Journey prose and JSON report calls, natural
  deals, Press-forced deals, Space suppressions, Press calls blocked by protected nights,
  Press calls with no eligible hand, Space calls after their allowance is spent, longest
  quiet stretch, deal-gap bounds, and natural-versus-steered counts per card. Per-reading
  outcomes remain available to focused tests and diagnostic JSON.
- The pilot gains no new choice policy. Its journeys provide passive evidence that both
  steering directions occur and that protected clocks retain priority.

### Acceptance and sweep requirements

- Focused tests cover explicit classification, fail-closed metadata, call timing, Steady's
  one-in-three cadence, Press's consumed roll and guaranteed eligible deal, natural versus
  forced Press outcomes, the quiet reset, Space's one-suppression bound, claimed nights,
  empty and ineligible hands, once-per-world guards, and crossing reset.
- Boundary tests prove every protected clock remains untouched, a card is never reserved
  across a season or eligibility boundary, and a scheduled future becomes protected as
  soon as an elastic card places it on the calendar.
- Determinism tests prove one cadence draw per live tick, stable weighted selection under
  identical state, journal replay, and complete journey and JSON measures.
- The current five-seed read-only baseline is retained as design evidence: 711 nights,
  67 Space calls, 174 Press calls, 19 deals under Space, and 127 unanswered Press calls.
  Implementation compares the same seeds before and after steering and explains the new
  spacing and card distributions.
- The complete HANDOFF engine sweep gates implementation. Release build and tests pass,
  all five journey twin pairs remain byte-identical, sim replay is exact, worldgen purity
  passes, and deck, store, offer, fact, and journey drift is justified against the then-
  current baseline.

### Verification record

- Release build: zero warnings and zero errors. Full suite: 886 of 886 tests passed,
  including 18 focused pacing tests and the surrounding schedule, weather, deck, save,
  replay, and journey coverage.
- Five seeds completed twelve crossings twice each. Every v98 twin pair is byte-identical,
  and every v98 key journal is byte-identical to v97. Cycle, turn, and death outcomes all
  hold. Seed 1 sim replay matches exactly at 25,260 keys, cycle 13, turn 24,066.
- The immediate v97 comparison covers the same 752 tick nights. Press calls fall from 199
  to 55. The v98 book records 139 natural deals, 27 Press-forced deals, 13 Space
  suppressions, 2 protected Press blocks, 11 empty Press calls, and exactly 752 cadence
  rolls. Deal gaps span 1-10 ticks and the longest quiet stretch is 6 ticks.
- Seasonal distribution moves only through the approved elastic deck. Haying days changes
  from 6 to 9 and Late frost from 10 to 8 across the five runs, while all 28 bargain offers
  and 21 purchases hold. No pilot policy or key route changes.
- Worldgen remains pure across 240 worlds with zero digest mismatches. The 86,282 prose
  surfaces and all generated-world measures remain unchanged. Save v93 advances to v94,
  and D-169 closes tranche 4.

### Explicit exclusions

- No new events, adaptive difficulty, act or hostility-tier tuning, player pacing controls,
  event backlog, storylet delay, scheduled-future manipulation, raid manipulation, weather
  manipulation, combat steering, new RNG stream, separately serialized pacing state, or
  player-facing teller meter.

## V1-05: Town and economy depth

**Design status:** Verified
**Decisions:** D-161, D-170
**Roadmap association:** Path to 1.0 tranche 5; property, tournaments or duels,
commissions, books, town-life and economy partials  
**Dependencies:** Town chunks, law, guild, Commerce, Persuasion, Smithing, Lore,
regional trade  
**Implementation status:** Completed and verified 2026-07-22; V1-06 follows under D-162

### Approved behavior

#### The guild loft

- The launch property is one room inside the existing guildhall plot, bought once per
  world for 80 coin. It requires the carriers' bond, an even town book, and the town-law
  primer read whole.
- The room remains the bearer's if the town book gains marks later. Law can close a
  counter but cannot strand an owner outside their room or strongbox.
- The room contains a settled bed, a reading desk, and a strongbox. The bed gives the
  wayhouse's full settled rest without coin but never opens shrine or Essence work. The
  desk permits the ordinary `v` reading verb in town. The strongbox moves the whole purse
  in or out, keeps boxed coin safe from death and raids, and includes that coin
  automatically in the crossing's ordinary weighing so property never creates a
  forfeiture chore.
- The loft, its box, and every improvement are world-scoped. They end at the crossing.
  There is no periodic rent or upkeep: the eighty-coin purchase recurs in each world and
  is the whole launch carrying cost.

#### The fitted workshop

- The launch masterwork commission is a fitted workshop for the loft, commissioned from
  the town smith once per world for 120 coin. It requires the loft, Smithing 2, and
  ordinary access to the smith's counter.
- Its workbench uses the existing most-worn-item choice and the existing stead-bench
  Smithing arithmetic. A sitting costs no further coin and feeds Smithing only when wear
  truly moves.
- It does not smelt tarn-iron, teach the drawn temper, or perform bloom-tempering. Those
  services remain at the town forge. No commissioned gear gains damage, protection,
  requirements, moves, or other combat power.

#### The law-day lists

- Each town holds one nonlethal tournament per world. Entry costs 15 coin, requires an
  even town book, and requires the bearer to be whole and unwounded. Losing spends the
  entry and closes that world's tournament. There is no retry.
- One entry contains three seeded, escalating formal bouts. The bracket comes from its
  own named derived stream after every existing world-generation draw, and its actors
  reuse the existing combat grammar at the current hostility tier rather than creating a
  separate minigame.
- The bearer may use personal weapons, bows, workings, and consumables. Guests and
  summons do not enter a one-on-one list. Combat meters settle between bouts, but wear
  and consumed supplies remain spent.
- A lethal result becomes a yield on either side. A bout creates no death, scar,
  remnant, loot, Essence, bestiary study, or faction kill. Honest combat actions still
  feed their existing skills, and ordinary world time continues through them.
- Winning all three bouts pays 45 coin and writes one champion fact for town surfaces to
  read. This makes the activity feed skill, coin, and world state without becoming a
  repeatable faucet.
- Reading the town-law primer also unlocks one judicial challenge per world while a mark
  stands. It reuses one formal bout. A win answers exactly one mark; a loss leaves the
  book unchanged. It never feeds Persuasion, so the paid plea remains the tongue's own
  costed use-curve and the only way to argue the book down repeatedly.

#### The last two launch books and the shelf

- `the little book of line and surety` costs 11 coin, asks Lore 1, and takes five shrine
  or loft-desk sittings. Reading it whole unlocks the guild-loft contract and the
  once-per-world judicial challenge. It grants no flat Persuasion or fine bonus.
- `the hearth-book of road and fell` costs 10 coin, asks Lore 1, and takes five sittings.
  Reading it whole permanently enables a three-entry curated, fact-keyed storylet pool.
  Each entry may fire once per character, only when a qualifying world supplies its true
  required facts. The entries never repeat, invent history, or grant a universal coin,
  Essence, or combat bonus; their durable payoff is authored content and facts later
  surfaces may consume.
- Both books append to BookId and the stable catalog, and ownership, progress, and read
  state cross worlds with the other books.
- The scrivener's board gains one stable shelf entry that opens the existing vendor
  submenu pattern. The shelf lists all six books in fixed catalog order with owned,
  unread, and finished states. Direct book digits leave the talk board, preserving room
  under the nine-digit law for later archives.

#### Guild, law, presentation, and persistence

- The guild controls the room, the smith controls its workshop, and the moot controls
  tournament eligibility and judicial challenge. Existing counter-bar and Held Road
  tithe rules apply wherever their current general contracts say they do.
- No positive town Fame scalar is added. The guild bond, town book, champion fact,
  property fact, and workshop fact are the complete launch state.
- The guildhall room and its gated door are added inside the existing fixed plot. The
  lists marshal joins the existing moot plot. Both remain reachable across the town
  stitch, and no new plot displaces the two variable town chunks.
- Context hints name the bed, desk, strongbox, workshop, lists entry, and current gate or
  refusal. Every menu remains at or below nine digits. Snapshots expose stable property,
  box, workshop, tournament, and judicial state.
- Assuming V1-01, V1-02, and V1-04 make their planned bumps while V1-03 remains no-bump,
  this tranche is expected to move save v94 to v95 because the town map and cast change,
  talk digits move into submenus, and new journaled actions change carried and world
  state. The implementation decision owns the final version after prior cards land.

#### Pilot and acceptance

- The journey pilot buys and reads both books. In the first eligible world where its
  reserve permits, it buys the loft, uses the bed, desk, and strongbox, commissions the
  workshop, and completes at least one real wear-moving sitting.
- Once honestly armed, whole, and able to keep its normal bread reserve, the pilot enters
  each world's lists once. It remains crime-free, so judicial challenge is covered by
  focused engine tests rather than an invented pilot offense.
- Journey prose and JSON record entries, bouts, yields, championships, judicial results,
  lofts bought, boxed coin in and out, room rests, desk sittings, workshops commissioned,
  and workshop sittings.
- Focused tests cover the town stitch and reachability, every price and prerequisite,
  later-law access to owned property, strongbox death, raid, and crossing behavior, bed
  and desk parity, workshop boundaries, both sides of nonlethal resolution, skill and
  resource accounting, tournament closure, judicial success and failure, both books,
  all three fact-gated storylets, stable menus, snapshots, and journal replay.
- Implementation receives the complete HANDOFF engine sweep: clean Release build, full
  tests, five seeded twelve-world twin journeys, justified baseline drift, seed 1 sim
  replay, and the worldgen purity gate.

### Implementation verification

- D-170 completes the card at save v95. The Release build is clean with zero warnings,
  all 900 tests pass, and the 14 focused TownPropertyTests cover the approved property,
  workshop, shelf, formal-combat, storylet, snapshot, and replay contracts.
- Seeds 1, 7, 99, 2024, and 88888 each completed two byte-identical twelve-world journeys.
  Every eligible run bought and exercised the loft and workshop, completed both new books,
  and resolved the full three-bout lists entry. The pilot remains crime-free, so zero
  judicial results are expected in these journeys and both outcomes are focused-test proven.
- Seed 1's 26,386 emitted keys replay exactly to cycle 13 and turn 25,172 with all six books
  owned and read. All five JSON reports agree with their prose counterparts.
- The 240-world generation gate exits cleanly with zero digest mismatches across 87,722
  surfaces. Drift from v98 is justified by new town travel, reading, formal combat, and the
  recurring property and workshop economy. The v99 journeys are the new baseline.

### Explicit exclusions

- No multiple houses, decoration system, periodic rent, passive income, tenants,
  population simulation, cross-world property, lethal arena, repeatable tournament,
  betting minigame, spectator simulation, NPC attack verb, or positive town Fame ladder.
- No general crafting interface, commissioned combat-stat gear, caravan consignments,
  dynamic supply simulation, vendor liquidity, town burglary, Stealth work, further book
  titles, or Lore knacks. Those remain with later cards or post-1.0 unless the final audit
  promotes them.

## V1-06: Character and activity breadth

**Design status:** Verified
**Decisions:** D-162, D-171
**Roadmap association:** Path to 1.0 tranche 6; eighteen-skill launch roster
**Known dependencies:** Existing skill growth, knack questions, hale-draught brewing,
combat movement, monster activity, crime, fencing, character creation, journey pilot
**Recommended implementation point:** sixth card, after V1-05 and before combat and
magic depth

**Implementation status:** Completed and verified 2026-07-23; V1-07 follows under D-163

### Approved behavior

#### The eighteen-skill ledger and sheet

- End-append `Alchemy`, `Athletics`, `Stealth`, and `Larceny` after Lore in `SkillId`.
  Sleight remains a distinct skill. `SkillSet.Count` becomes eighteen.
- New skills use the existing counted-use curve, persistence, level derivation, and
  no-respec rule. Their uses survive death and crossing with the character bucket.
- Pair the character sheet into two columns of nine skills. Preserve enum order, level,
  use fraction, chosen knack names, the taught row, and the pending-question flow.
- No existing skill is renamed or re-indexed. The four additions are end-appended for
  journal and enum stability.

#### Alchemy: preparation, not gathering or literacy

- Survival continues to gather herbs. Lore continues to open written formulae.
  Stillcraft remains the proficiency that permits the bearer to brew independently.
  Alchemy is the use-grown preparation skill.
- A successful self-brew at a settled rest spends the existing two or three herbs,
  depending on WortCunning, grants one Alchemy use, and makes
  `1 + SkillSet.Bonus(Alchemy)` hale-draughts, bounded by the current rack.
- A draught drawn by the herbwife grants no Alchemy use because her hands did the work.
  Drinking never grants a use. A refusal for ingredients or rack space spends nothing
  and teaches nothing.
- The hale-draught remains the one launch formula. Its healing and wound-cut effects stay
  unchanged, WortCunning keeps its existing thrift, and the stillroom extension keeps
  its existing rack increase.

#### Athletics: wind spent for speed

- Uppercase movement directions on a local combat map attempt a rush exactly two clear
  cells in one turn. Lowercase movement keeps its present meaning.
- A rush costs `max(2, 4 - SkillSet.Bonus(Athletics))` stamina. The ordinary step
  regeneration resolves normally after the move.
- Both cells must be in bounds, walkable, unoccupied, and free of transitions, loot,
  remnants, or other notable interaction tiles. A rush never crosses a creature or
  skips an interaction. If both cells cannot be taken, it is refused without time,
  stamina, movement, or training.
- A completed rush grants one Athletics use only while a living, awake hostile is
  actively engaged on that local map. Safe travel, empty-site laps, and uppercase
  overworld directions do not train it.

#### Stealth: time spent for concealment

- `s` remains burglary beside a settlement door. In a hostile local site before open
  engagement, `s` toggles soft tread. The mode is visible in the HUD and cancels on
  attack, discovery, leaving the site, death, or crossing.
- Foes in hostile sites begin unaware, separately from special authored dormancy. They
  wake when they detect the bearer, take harm, or an existing authored group alarm
  reaches them. Once aware, their current combat behavior is unchanged.
- Ordinary movement uses the foe's existing notice distance and line of sight. Soft
  tread reduces that distance by two, then by the bearer's Grace bonus and
  `SkillSet.Bonus(Stealth)`, with a floor of one. Worn tarn-temperable metal armor adds
  one cell back; the quilted jack does not. Detection uses no random roll.
- A quiet step costs no extra stamina. It commits up to two honest turns. The first turn
  is the careful setting of the foot at the current cell. If detection occurs there,
  the movement is canceled and control returns immediately. If the bearer remains
  unseen, the second turn moves one ordinary cell and resolves normally.
- Both turns advance every causal clock through the ordinary engine path: scheduled
  futures, faction pressure, weather and season state, durations, recovery, monster
  awareness, and any later systems reading turns. No partial or cosmetic clock exists.
- Stealth gains one use per foe per site when a quiet step crosses that foe's ordinary
  notice band without waking it. The same foe cannot train the bearer twice, ordinary
  safe movement teaches nothing, and detection teaches nothing.
- Soft tread grants no damage multiplier, instant kill, pickpocket modifier, or burglary
  modifier. Its reward is position, bypass, and encounter control.

#### Sleight and Larceny: hand-work versus the criminal trade

- Sleight remains the precision skill for pickpocketing and lockpicking. Their existing
  base odds, feeds, finite targets, failures, and 85 percent caps remain.
- Larceny owns pilfering, burglary, and fencing. Burglary uses Larceny level in its
  existing risk curve instead of Sleight level.
- A clean pilfer and a clean burglary each grant one Larceny use. Fencing grants one use
  per sold lot, not per heirloom. Failed or refused crimes teach nothing.
- Each fenced heirloom pays `7 + SkillSet.Bonus(Larceny)` coin before any later explicit
  modifier. Existing facts, shame, town marks, restitution, finite-house rules, peddler
  access, and story readers remain intact.
- No crime combines Sleight, Stealth, and Larceny into multiple required checks. The
  three skills remain independently viable rather than becoming a hidden thief class.

#### Creation hooks

- The hedge-healer banks Alchemy 1 instead of Survival 1 and begins with Lore 1,
  Stillcraft, and the existing three herbs.
- The wayfarer banks Athletics 1 instead of Hunting 1 and keeps the existing rations.
- The oathbreaker keeps Blades 1 and banks Larceny 1 instead of Hunting 1, alongside
  the existing opening Shame.
- No past starts with Stealth. Grace improves soft tread from the first site, and any
  bearer can begin training it honestly.
- The craft kit continues to grant Stillcraft and six herbs but grants no Alchemy uses
  or level. Knowledge and practiced skill remain separate.
- Do not add or reorder folk, past, thing, burden, or vow enum values.

#### Five level-2 knack questions

- Alchemy asks between one additional draught of rack capacity and one fewer herb on
  every second successful self-brew. The thrift stacks with WortCunning but never lowers
  a batch below one herb.
- Athletics asks between a three-cell rush under the same path rules and one less
  stamina per rush, still bounded by a minimum cost of one after the knack.
- Stealth asks between one further cell of notice reduction and ignoring the one-cell
  metal-armor noise term.
- Larceny asks between three additional coin from each clean burglary and two additional
  coin per fenced heirloom.
- Sleight asks between ten percentage points on pickpocketing and ten percentage points
  on lockpicking. Both remain capped at 85 percent.
- Each question is permanent, mutually exclusive, end-appended in `PerkId`, announced
  through the existing threshold path, and answered through the paired sheet.

#### Pilot and observability

- The default journey stays crime-free. In each eligible world it must self-brew when
  ingredients and rack space allow, complete an honest live-pressure rush, and cross at
  least one ordinary notice band on soft tread.
- Add opt-in `journey --rogue`. It exercises pickpocketing, lockpicking where available,
  pilfering, burglary, fencing, restitution or town-law consequences as applicable, and
  the Sleight and Larceny feeds without changing default journey choices.
- Journey prose and JSON report all four new skill uses and levels, rushes completed,
  quiet bands crossed, discoveries during soft tread, clean pilfers and burglaries,
  fenced lots and goods, and the five new knack choices.
- The generic Snapshot skill ledger remains enum-driven and must list all eighteen in
  stable order. Add explicit awareness and soft-tread state only where replay diagnosis
  needs it.

### Acceptance and sweep requirements

- Focused tests pin end-append enum order, eighteen-skill storage, the two-column sheet,
  creation starts, death and crossing persistence, save replay, and every growth line.
- Alchemy tests cover self-brew-only feeding, herb and rack refusals, skill-scaled yield,
  WortCunning composition, rack limits, herbwife exclusion, and both knacks.
- Athletics tests cover every direction, exact two-cell and three-cell paths, stamina
  arithmetic, hostile-only feeding, notable-cell and occupancy refusal, no partial move,
  and both knacks.
- Stealth tests cover contextual `s`, visible mode state, deterministic line of sight,
  Grace, skill, and armor arithmetic, first-turn discovery cancellation, second-turn
  movement, every protected causal clock, authored dormancy and alarms, once-per-foe
  feeding, cancellation conditions, and both knacks.
- Crime tests prove the Sleight and Larceny split, unchanged facts and consequences,
  finite targets, burglary odds, fenced price arithmetic, one use per lot, failure and
  refusal behavior, the 85 percent caps, and all four related knack effects.
- Pilot tests prove the default route remains crime-free and the opt-in rogue route is
  deterministic, consequence-honest, and complete. Snapshot and journey JSON additions
  are pinned.
- Assuming V1-01 through V1-05 land first, implementation advances save v95 to v96
  because four skill indices, uppercase movement, contextual `s`, creation starts,
  monster awareness, crime odds, prices, and knack keys alter replay semantics.
- Kill `aegis.exe`, build Release, run the complete test suite, run seeds 1, 7, 99,
  2024, and 88888 through two byte-identical twelve-world journeys each, compare and
  justify drift from v95, replay seed 1 to exact keys/cycle/turn, exercise the rogue
  journey, and pass `worldgen --json`.

### Implementation verification

- D-171 completes the card at save v96. The Release build is clean with zero warnings
  and all 920 tests pass. Focused acceptance covers stable enum order and storage,
  two-column presentation, creation, death and crossing persistence, self-brewing and
  both Alchemy knacks, all eight rush directions and both Athletics knacks, deterministic
  awareness and quiet-step arithmetic, the crime-ledger split, caps, prices, and feeds.
- Seeds 1, 7, 99, 2024, and 88888 each completed two byte-identical twelve-world
  journeys. Every run exercised successful self-brewing, live-pressure rushing, and
  quiet-band crossings. Every default crime-action counter remained zero. The opt-in
  rogue seed-1 route exercised Sleight and Larceny, clean pilfering and burglary, fencing,
  lock, pocket, and burglary attempts, and restitution.
- Seed 1's 32,587 emitted keys replay exactly to cycle 13 and turn 30,609 with seven
  deaths. All five JSON reports agree with their prose counterparts.
- The 240-world generation gate exits cleanly with zero digest mismatches across 87,722
  surfaces. Drift from v99 is justified by the new activity errands, two-turn quiet
  movement, rushing, and awareness behavior. The v100 journeys are the new baseline.

### Explicit exclusions

- No additional formulae, reagents, poison system, consumable selection menu, or general
  crafting interface.
- No swimming, climbing system, parkour, encumbrance, or fatigue meter.
- No sneak-attack multiplier, assassination, instant takedown, darkness simulation,
  propagated sound field, searching AI, or town-wide stealth simulation.
- No organized crime, heists, criminal faction, new fence, or second justice system.
- No new pasts, folk, recultured societies, or character-creation stages.
- No Polearms skill split and no rewrite of existing combat families.
- No level-4 or level-6 noncombat knack wave, three-option questions, or general knack
  pass for older activity skills. Those remain catalog growth for later classification.
- No hostile magic, broader movesets, stance/parry growth, or working catalog growth;
  those belong to V1-07.

## V1-07: Combat and magic depth

**Design status:** Verified
**Decisions:** D-163, D-172
**Roadmap association:** Path to 1.0 tranche 7; combat and magic open launch work
**Dependencies:** V1-06 awareness and final skill roster; weapon families, stances,
parry, posture, workings, Focus, Spellcraft, Will, bestiary reads, guests, and shades
**Recommended implementation point:** seventh card, after V1-06 and before companion and
faction depth
**Implementation status:** Completed and verified 2026-07-23; V1-08 follows under D-164

### Approved behavior

#### Launch movesets and flanking

- Add no combat skill, weapon category, gear tier, or command key. The launch movesets
  close through existing acts plus the geometry and growth rules below.
- A foe is player-flanked only when the bearer and a living guest or shade occupy exact
  opposite neighboring cells around it. A paid melee blow in that geometry deals two
  additional posture pressure. If the weapon family is Blades, it also deals one
  additional blood damage.
- The bearer is enemy-flanked only when two living, aware hostiles occupy exact opposite
  neighboring cells around the bearer. A committed blow landing from either adds one
  bearer posture pressure. Incidental bites and other uncommitted trades remain outside
  the posture system.
- Flanking uses no random roll, broad adjacency count, diagonal exception, or hidden
  facing. The same eight-direction opposite-cell test governs both sides.
- Record the existing launch movesets as complete after the geometry lands: Blades has
  cut, heave, answered step, and flank reward; hafted axes and mauls have swing, arc,
  heave, and sunder; spears have swing and reach thrust; Brawling has shove and wall
  pressure; Ranged has the directional loose and intent reward; Warding has armor use,
  stance defense, and parry.

#### Enemy follow-ons

- A resolved goblin cry alerts every unaware living goblin in its site. It composes with
  V1-06's awareness state and the established group-alarm route. It does not erase the
  distinction between ordinary unawareness and authored dormancy.
- A warder whose board is sundered abandons its ranged rim behavior and closes to melee.
  It no longer begins lofted-stone intents after the break and uses its existing close
  attack. The broken board stays broken.
- Add one Severed sweep as a one-turn telegraphed intent. It marks the three adjacent
  cells in the facing arc toward the bearer, and only those cells resolve. Leaving the
  footprint is the ordinary answer; a legal parry may meet it when the bearer remains in
  the marked adjacent arc. Its damage and pressure sit in the existing heavy committed
  band rather than inventing a new scale.
- Blur always shows the marked cells. Read names the sweep. Keen adds its heavy weight
  and whether a parry is legal, following the existing bestiary doctrine.

#### Five level-6 martial questions

- Append one permanent two-option level-6 question for each existing combat skill after
  every earlier threshold question. Append all ten `PerkId` values and stable snapshot
  ids; never reorder or reinterpret an existing option.
- Blades asks between the forward edge and the returning edge. The forward edge adds one
  blood to paid blade cuts while Pressing. The returning edge adds one posture pressure
  to a successful blade parry.
- Hafted asks between the whole weight and the rooted haft. The whole weight adds one
  posture pressure to a Pressing hafted heave. The rooted haft subtracts one additional
  incoming committed posture pressure while Guarded, floored at zero.
- Brawling asks between crowding hands and the caught wrist. Crowding hands lets a
  Pressing shove carry its target up to two consecutive clear cells, stopping honestly
  after one if the second is blocked. The caught wrist lowers an unarmed parry from two
  stamina to one.
- Warding asks between the deep set and the easy guard. The deep set lets worn armor turn
  one additional blood while Guarded. The easy guard removes one bearer posture damage
  after a successful parry, floored at zero.
- Ranged asks between the forward draw and the waiting string. The forward draw adds one
  blood to a shaft while Pressing. The waiting string adds one posture pressure when a
  shaft strikes a foe carrying an active telegraphed intent.
- Refused, winded, missed, or otherwise unpaid acts never collect a knack rider.

#### The hostile caster and Will resistance

- End-append `RuneTongue` to `MonsterKind`. From tier 5 onward, exactly one eligible
  existing fighting site receives one rune-tongue per world. Choose its site and legal
  placement from a named derived RNG stream created after every existing worldgen draw.
  Earlier layouts, names, facts, and spawn draws must remain pinned.
- The rune-tongue has ordinary blood, posture, movement, reads, and a weak close attack.
  It has no hidden Focus or random miscast system. After a working resolves or is
  interrupted, it spends one visible recovery turn before choosing another working.
- Will resistance is `clamp(Will - 5, 0, 4)`. Print it as a derived value on the
  character sheet and expose it in snapshots needed for replay diagnosis.
- The falling word is a one-turn ground intent. It marks a five-cell cross centered on
  the bearer's position at commitment: the center plus its four cardinal neighbors.
  Only those legal map cells resolve. A landing deals a combat draw from 7 through 10
  minus Will resistance, minimum one. Armor and parry do not answer magical force.
  Moving outside the cross does.
- The binding word is a two-turn bearer lock rather than a cell target. It is canceled
  if the rune-tongue is wounded, posture-broken, killed, or lacks line of sight at
  resolution. Movement alone does not evade a maintained line. If completed, it removes
  `max(1, 4 - resistance)` stamina and `max(0, 2 - resistance)` Focus, floored at the
  resources held, and deals no blood.
- Any blood damage interrupts either held enemy working. A posture break and death keep
  their existing universal interruption behavior. An interrupted caster still enters
  the one-turn recovery.
- Even a Blur read distinguishes a ground working from a following binding and shows the
  fair answer. Read adds names and timing. Keen adds the exact footprint, base effect,
  resistance arithmetic, and interruption routes. The sidebar names a following binding
  separately from marked ground and shows recovery.

#### Two new workings

- End-append Severing and Mending after Calling, bringing `SpellCatalog` to seven. Extend
  every site's stone preference after all five existing entries, distributing the two
  additions across complementary preferences so both normally appear during the first
  two worlds. Do not reorder the existing five.
- Both new workings enter the existing known-word creation pool. The cast menu stays in
  learn order and remains within its digit limit. Knowledge persists through death and
  crossing exactly as every existing working does.
- The severing costs two Focus and asks for a direction. It travels the existing spell
  range of four until stone stops it and cancels the first hostile intent in that line
  tagged as magical. A successful severing also deals two posture pressure to the caster
  and grants one Spellcraft use. The tag includes falling word, binding word, and the
  existing grave-chill, and is extensible to later hostile workings.
- Choosing an empty or blocked line spends the turn and Focus but grants no use. The
  severing deals no blood, does not dispel ordinary physical intents, and cannot erase
  historical facts or permanent states.
- The mending costs three Focus and is a one-turn self-targeted wind-up. It refuses at
  full blood without time or Focus. On commitment it spends Focus and records blood for
  the existing levin-style grip check. If still held next turn, it restores
  `5 + Player.SpellBonus + Skills.Bonus(Spellcraft)` blood up to the effective maximum
  and grants one Spellcraft use.
- A wound during the hold uses the existing Will and Spellcraft grip curve. A broken word
  loses its committed Focus and grants no use. The mending never reduces wound duration,
  so a hale-draught remains the stronger remedy and the only one of the two that treats
  the wound itself.

#### Spellcraft questions

- Add permanent two-option Spellcraft questions at levels 2 and 4, appended in the
  established threshold order with four new stable `PerkId` values.
- At level 2, the full word adds one blood to damaging workings, one blood to the
  mending, and one turn to the ward. The spare syllable refunds one Focus after every
  second successful Focus-spending working, using the Spellcraft use ledger for stable
  parity.
- At level 4, the answering word refunds one Focus when a successful working cancels an
  intent or strikes a foe while that foe carries an intent. The deep well raises maximum
  Focus by one.
- Refunds may compose when one working satisfies both owned conditions but never exceed
  maximum Focus. Failed, refused, empty, or purely preparatory acts grant no refund.
  Calling is excluded because its Focus is held rather than spent. Veilsight keeps its
  qualitative effect and receives no artificial numeric rider from the full word.

#### Social boundary, pilot, and observability

- The game log and the Aegis may acknowledge the first hostile working and first
  successful severing. Casting creates no suspicion, infamy, faction attention, price
  change, law mark, or general NPC reaction in this card.
- The default journey must dodge the falling cross, interrupt or break sight against a
  binding, use the severing when it is the safest answer, and use the mending only while
  hurt and outside an imminent marked threat. It must understand the new flanking,
  sweep, alarm, warder, stance, and parry rules without becoming a caster-only policy.
- Add opt-in `journey --caster`. It chooses a caster-oriented creation, prioritizes Mind
  and Will, learns and demonstrates all seven workings, and answers Spellcraft with the
  full word at level 2 and the answering word at level 4.
- Journey prose and JSON report casts and successful effects by working, rune-tongue
  encounters, hostile workings begun, interrupted, resisted, and landed, Will resistance,
  flanks on both sides, sweeps dodged or landed, boardless warder closures, and all new
  knack choices. The emitted journal must replay exactly through `sim`.

### Acceptance and sweep requirements

- Combat tests pin the exact opposite-cell flank geometry, ally and enemy sides, Blades
  blood, posture arithmetic, no broad adjacency reward, and all ten level-6 perk effects,
  ids, threshold order, persistence, and replay.
- Enemy tests cover the awareness alarm without collapsing authored dormancy, the
  boardless warder's close phase, the Severed sweep's three-cell footprint, dodge, parry,
  damage, pressure, and all read tiers.
- Worldgen tests prove zero rune-tongues below tier 5, exactly one above it, legal and
  deterministic placement, end-appended enum order, and byte-stable prior generation
  before the new named stream.
- Hostile magic tests cover both tells, footprints, timing, every interruption route,
  line of sight, recovery, armor and parry exclusions, resistance at its floor and cap,
  resource floors, sheet, sidebar, snapshot, and read-tier wording.
- Player magic tests cover catalog order, stone preferences, creation, death and crossing
  persistence, menu capacity, costs, blocked and empty lines, magical tags, posture,
  mending grip and cap, wound non-treatment, growth gates, and all four Spellcraft perks
  including composed refunds.
- Pilot tests prove default and caster policies deterministic, threat-honest, complete,
  and exactly replayable. Prose, JSON, and sim snapshots pin the new metrics.
- Assuming V1-01 through V1-06 land first, implementation advances save v96 to v97.
  The implementation decision owns the final number after prior cards land.
- Kill `aegis.exe`, build Release, run the complete test suite, run seeds 1, 7, 99,
  2024, and 88888 through two byte-identical twelve-world journeys each, compare and
  justify drift from v96, replay seed 1 to exact keys/cycle/turn, exercise the caster
  journey, and pass `worldgen --json`.

### Implementation verification

- D-172 completes the card at save v97. The Release build is clean with zero warnings
  and all 940 tests pass. Twenty focused checks cover stable catalogs and threshold
  order, both exact flank geometries, all ten martial answers, alarms and dormancy,
  the boardless warder phase, the sweep's footprint, dodge, parry, and read tiers, legal
  rune-tongue generation, both hostile words and their interruption and resistance
  rules, Severing, Mending, all four Spellcraft answers, persistence, presentation,
  snapshots, and replay.
- Seeds 1, 7, 99, 2024, and 88888 each completed two byte-identical twelve-world
  journeys at 26950, 32865, 31738, 35101, and 35877 keys. Every run reached cycle 13,
  encountered hostile workings, and used Severing and Mending. The new v101 baseline
  drift from v100 is justified by rune-tongue placement and combat, flank and sweep
  resolution, the two new words, and the pilot's safe responses.
- Seed 1's 26950 emitted keys replay exactly to cycle 13 and turn 25885 with nine
  deaths. The opt-in caster route completes twelve crossings in 27182 keys and 26065
  turns, successfully demonstrates all seven workings, takes the full word and
  answering word, and replays exactly. All five default JSON reports and the caster
  JSON report agree with their prose counterparts.
- The 240-world generation gate exits cleanly with zero digest mismatches across 87722
  surfaces, five families at their declared coverage, zero hard failures, and the two
  expected legacy fixed-surface warnings.

### Explicit exclusions

- No new skill, command key, weapon category, gear tier, site, region, faction, or
  companion command.
- No attunement capacity, components, schools, grimoires, mentors, rituals, or other
  acquisition source.
- No enemy Focus bar, random enemy miscast, summon, teleportation, or darkness system.
- No broad caster reputation, faction response, price response, or law consequence.
- No changes to existing knack meanings, level-8 combat wave, or wider noncombat knack
  pass.
- No formal-duel implementation from V1-05, companion or faction expansion from V1-08,
  or region and release content from V1-09.

## V1-08: Companions, factions, and consequences

**Design status:** Verified

**Decisions:** D-164, D-173

**Roadmap association:** Path to 1.0 tranche 8; companion, faction, and scar follow-ons

**Dependencies:** V1-02 weather, V1-05 guild and town closure, V1-07 flanking and
physical footprints; guest and beast systems, relation ledgers, scheduled facts,
Death's Toll, scars, storylets, oaths, and the journey pilot

**Recommended implementation point:** eighth card, after V1-07 and before the final
region and release card

**Implementation status:** Completed and verified 2026-07-23; V1-09 follows under D-165

### Approved behavior

#### Companion combat parity

- Keep the current ceiling of one mortal guest plus one shade. Add no companion
  equipment, inventory, skills, posture, resurrection, or command key. The bearer remains
  the only unit the player directs turn by turn.
- A targeted physical intent may choose the nearest visible living body among the bearer,
  mortal guest, and shade when that body meets the intent's existing range and line-of-
  sight rules. Ties favor the bearer. Bearer-shaped magic, including the V1-07 binding,
  remains bearer-only.
- Charges, sweeps, and other marked physical footprints resolve once against every body
  that occupies a struck cell. Damage uses the intent's established arithmetic for that
  body and consumes no additional random draw for empty cells.
- On a fellow's ordinary turn, before attacking or following, a fellow not ordered to
  hold looks at all visible physical marks that will resolve before its next turn. If its
  cell is marked and a legal adjacent unmarked cell exists, it takes a stable shortest
  step to safety. Stable direction order breaks ties. It then ends that fellow turn.
- A held fellow never takes the automatic escape. Holding is the existing explicit order
  to keep a cell, including its danger. If no safe legal step exists, a following fellow
  continues its ordinary behavior rather than gaining immunity.
- Fellows gain no parry, armor, posture, resistance, hidden dodge roll, or exception from
  ordinary occupancy. Enemy awareness and authored dormancy continue to follow V1-06 and
  V1-07 rules.
- A directional loose is refused without time, stamina, ammunition, or skill use when a
  living mortal guest is the first occupied cell before the first foe in that line. A
  shade does not stop a shaft. A guest behind the first foe does not block the shot.

#### The grain road, a second guest arc

- Add one live Crofter-role arc, cast from an eligible named stead villager. It may begin
  once per world while the levy stands, the carriers' bond is sworn, no mortal guest is
  active, and an eligible villager remains alive and at home.
- The arc uses the ordinary storylet casting seam and adds no talk-menu digit. The Crofter
  is a nonfighter under the existing role rule and uses the same HP, care, follow, hold,
  door, death-wake, and loyalty-beat machinery as every mortal guest.
- Bringing the living Crofter to the town guildhall completes the arc. The NPC leaves
  guest state, the world writes a portfolio fact, and a grain delivery is scheduled for
  the next coarse tick. Completion pays no coin, Essence, skill use, or immediate Regard.
- When the delivery lands, it restores up to two Stores, runs the existing levy-lift
  check, writes and narrates a faction fact, and grants one Regard. It lands even if the
  stores filled in the meantime, with the gain capped honestly at the current maximum.
  No second delivery can be earned in the world.
- A Crofter death uses the established guest-fell, beloved, memorial, missing-NPC, and
  Shame consequences. It schedules no delivery and adds no further punishment.
- Crossing with any unresolved living mortal guest now produces a farewell and returns
  the NPC to the outgoing world's roster before the world is left. It grants no portfolio,
  Regard, Shame, or cross-world companion state.

#### Bounded companion memory

- The first completed mortal guest arc arms one character-scoped Aegis remembrance. The
  first beloved guest death arms a separate remembrance. Each becomes eligible at the
  first shrine rest in a later world and fires once per character.
- These two memories carry only through the Aegis and name only events it witnessed.
  Later strangers do not inherit the outgoing world's knowledge. Additional successes or
  losses create no repeating scene, relationship currency, perk, price, or reputation.
- Snapshots expose stable armed and consumed flags for replay diagnosis. The ordinary
  character sheet does not become a companion ledger.

#### Faction readers and the Stead-to-Town edge

- Extend the existing raids topic so it reads the live watch and levy states accurately.
  This changes no menu count and invents no state.
- The grain road is the launch Stead-to-Town edge: the carriers' bond opens the arc, the
  stead supplies the mortal role, the guildhall receives it, and the scheduled cart moves
  Stores, levy state, Regard, and facts. Every transition is narrated under D-023.
- Add no positive Town Fame scalar. The guild bond, town book, champion, property, and
  workshop facts remain the complete launch town state. The new edge composes with them
  rather than replacing them.

#### Beast warmth and recognition

- At an exposed overworld camp, one living unstabled beast beside the bearer adds one HP
  after all ordinary season, weather, shelter, pelt, and camp-healing arithmetic.
- Warmth never permits a supperless camp refused by Cold, reduces Wounded duration,
  changes step stamina, improves a roof or waystone, or stacks through additional beasts
  in the stable.
- The first camp made with each of the mule, courser, and fell pony opens one distinct
  character-scoped recognition beat. Each fires once per character and grants no numeric
  reward.
- The opt-in companion pilot must tame and ride the fell pony, closing D-104's remaining
  journey gap.

#### Scar facts, the fitted brace, and tier-scaled Toll

- When a scar lands, write a stable `scar` fact whose subject is the scar's stable id.
  When a cure removes it, write the matching `scar-mended` fact. The landing fact feeds a
  once-per-world aftermath beat; each mend has one authored aftercare consumer on an
  existing appropriate surface.
- The first cure of the crushed hand records a permanent fitted-brace character mark.
  While the bearer does not currently carry the crushed hand, a parry with a wielded
  weapon costs one stamina instead of two. Unarmed parries remain two unless their own
  V1-07 knack changes them.
- A later crushed-hand scar suppresses the brace benefit while carried. Repairing that
  scar restores the existing benefit. The mark never stacks, never changes attack costs,
  and never grants armor, damage, or posture.
- Toll fill is calculated as the existing ordinary or heavy base plus
  `min(40, 10 * max(0, tier - 4))`, then reduced by the existing Will rule and bounded by
  the existing floor. Tiers 1 through 4 remain unchanged; tiers 5 through 8 add 10, 20,
  30, and 40, and all deeper tiers remain at the 40-point cap.
- Do not alter the judgment-before-fill order, line, one-point ordinary drain, scar
  matching, cure prices, crossing reset, or existing scar effects.
- Put current scars and the fitted brace beside the creation burden on the character
  sheet. Snapshots expose the brace and the exact tier contribution to Toll fill.

#### Two final launch oaths

- End-append `ClosedDoor` and `LongCount` after `HushedName` in `OathId`, bringing the
  crossing menu from seven entries to its nine-digit limit. Preserve every existing id,
  order, label, weight, and effect.
- The closed door has weight one. While it stands, every Stead Regard rung requires one
  additional point: thresholds 2, 4, and 6 instead of 1, 3, and 5. Regard gains and their
  facts do not otherwise change.
- The long count has weight one. While it stands, Death's Toll drains on every second
  completed turn instead of every completed turn. Fill, line, scar judgment, and crossing
  reset remain unchanged.
- Both oaths are world-scoped, printed in the terms and snapshot surfaces, contribute
  normally to Burden and Legend, and use no random draw.

#### Pilot and observability

- The default journey retains its Huntsman, shade, and beast policies. It understands the
  new target selection, companion evasion, shot refusal, and unresolved-guest farewell,
  and must not stall when any of them occurs.
- Add opt-in `journey --companion`. Across its route it begins and completes both mortal
  guest arcs, tends a guest, safely demonstrates follow and hold, earns a V1-07 flank,
  observes physical targeting and automatic evasion, attempts one refused shot, receives
  a grain delivery, camps with beast warmth, tames and rides the fell pony, cures every
  scar it receives, uses the fitted brace, and carries both new oaths.
- The companion route never deliberately kills a guest. Guest-fell, beloved, memorial,
  and cross-world loss memories remain focused-test branches.
- Journey prose and JSON report guest offers, starts, completions, farewells, deaths and
  beats; physical target choices, evasions, held impacts and shot refusals; cart state,
  store movement and levy lifts; beast recognition, warmth and pony use; Toll base, tier
  contribution, scars, cures and brace parries; and both oath effects.
- Every emitted default and companion journal must replay exactly through `sim`.

### Acceptance and sweep requirements

- Companion tests cover nearest-body selection, tie priority, range and line of sight,
  bearer-only magic, each physical footprint, stable safe-step choice, hold behavior, no
  safe cell, shade parity, shot refusal and every no-cost boundary.
- Guest-arc tests cover every offer gate, role casting, nonfighter behavior, guildhall
  completion, delayed cart timing, capped store restoration, levy lifting, Regard, facts,
  death, single-use closure, and crossing farewell.
- Memory tests prove both Aegis echoes arm only on the approved outcomes, wait for a later
  world, fire once per character, never teach an NPC, and replay through death and crossing.
- Faction and beast tests cover topic truth, the complete Stead-to-Town causal chain,
  narration, every weather and shelter combination, Cold refusal, non-stacking warmth,
  and all three recognition guards.
- Toll tests cover scar and mend facts, every consumer, fitted-brace acquisition,
  suppression and restoration, wielded and unarmed parry costs, exact tier arithmetic,
  Will reduction, heavy fill, cap, line, drain, crossing reset, sheet, and snapshots.
- Oath tests pin end-append order, nine-entry menu capacity, weights, exact Regard
  thresholds, alternating drain, Burden, Legend, crossing scope, and replay.
- Pilot tests prove default and companion policies deterministic, consequence-honest,
  complete, and exactly replayable. Prose and JSON fields are pinned.
- Assuming V1-01 through V1-07 land first, implementation advances save v97 to v98.
  The implementation decision owns the final number after prior cards land.
- Kill `aegis.exe`, build Release, run the complete test suite, run seeds 1, 7, 99,
  2024, and 88888 through two byte-identical twelve-world journeys each, compare and
  justify drift from v97, replay seed 1 to exact keys, cycle, and turn, exercise the
  companion journey, and pass `worldgen --json`.

### Implementation and verification

D-173 completes this card at save v98. Release builds with zero warnings and all 968
tests pass, including 28 focused companion-consequence checks. Sweep v102 holds five
byte-identical twin pairs and every seed reaches cycle 13. Default seed 1 replays all
26,891 keys exactly to turn 25,825. The companion seed-6 route completes twelve crossings
and replays all 38,588 keys exactly to turn 33,481. It exercises the required guest,
faction, beast, scar, Toll, brace, and oath diagnostics without deliberately killing a
guest. The 240-world purity gate reports zero digest mismatches and zero hard prose
failures across 89,402 surfaces. `sim --keys-file` supplies exact replay for journals
longer than the Windows command-line limit.

### Explicit exclusions

- No permanent party, second controllable unit, companion inventory, equipment, skill
  tree, leveling, posture, parry, armor, resurrection, or additional order key.
- No new faction, positive Town Fame ladder, raider-to-mound edge, further desecration,
  deeper transgression, town violence system, retinue, or population simulation.
- No predator attacks on waiting beasts, beast combat, stable raids beyond existing
  rules, breeding, feed meter, barding, or further mount kind.
- No dragging-step scar, further scar catalog, changed cure prices, prosthetic upgrade
  tree, random scarring, or additional death penalty shape.
- No companion-specific oath, oath pagination, Threat redesign, further Legend rung, or
  more than nine launch oaths.
- No Calling social follow-ons, systemic caster reputation, V1-09 region content, or
  release packaging work.

## V1-09: Next region and 1.0 release closure

**Design status:** Implemented

**Decisions:** D-165, D-174

**Roadmap association:** Path to 1.0 tranche 9; B4 later regions; A1 peddler restock;
housebreaker and Calling readers; generator freeze; release audit and packaging

**Dependencies:** V1-01 through V1-08; region, road, town, weather, fact, schedule,
storylet, companion, save, WorldEval, journey, sim, and Native AOT machinery

**Recommended implementation point:** ninth and final card, after V1-08

**Implementation status:** Built, automated gates verified, and clean package verified
2026-07-23. The fresh packaged manual campaign and explicit user signoff remain before
this card becomes Verified.

### Approved behavior

#### The Salt Fen, the fourth country

- Add the Salt Fen as the fourth named region and fourth overworld. End-append `Fens`
  to `Area`, draw its region and hamlet names from their own derived streams, and keep
  every existing area, region, name, and layout draw stable.
- A causeway mouth near the town end of the east road enters the fens; the matching home
  mouth returns to the same road cell. Both crossings speak the generated country name.
  The map is bounded and fully connected from its home mouth by construction.
- Fen ground is a walkable mixture of firm bank, reed ground, and raised causeway around
  impassable bog and open water. Do not add swimming, boating, tides, or runtime terrain
  replacement.
- Beasts follow into the region and wait at ordinary site mouths. No beast gains a
  two-cell stride there; uncertain banks make every mounted fen step one cell. Existing
  site, death, remnant, guest, shade, and crossing behavior extends through `Area.Fens`
  without a special exception.
- The fens use the shared seasonal calendar and a fourth climate band. Their deterministic
  three-card hand uses the existing Calm, Wet, Wind, and Cold families, weighted toward
  Wet and Wind. Forecast, exposed camp, beast warmth, roof, supper, and Cold rules remain
  the V1-02 and V1-08 rules. Local labels may change, not the four effects.

#### Density contract, hamlet, and four sites

- Generate one roofed fen hamlet plus exactly four regional sites. The hamlet is a small
  settlement surface, not a second market town: at least three named regional roles,
  ordinary rest and ration access, compact and carriers' topics, and no law book,
  property, tournament, school, guild rank, or independent reputation ladder.
- The four sites are one salt-making worksite, one wilderness site, and two fighting
  deeps. Each has its own stable site id, kind, entrance terrain, authored map contract,
  fact, entrance and completion presentation, topic reader, and deterministic reachable
  placement.
- Every site writes at least one fact consumed by the regional arc or its aftermath.
  Clearing or working a site never exists only for a chest count. The wilderness site
  and fighting deeps reuse ordinary chest, stone, coffer, guest, beast, and site-clear
  machinery where their authored contract permits it.
- Add the fen adder as one end-appended ordinary monster family. It may cross one water
  cell during a move but must end every turn on walkable ground, so melee always retains
  an answer. It has an adjacent bite and one readable two-cell straight coil-strike.
  It gains no poison meter, hidden evasion, water immunity while standing, elite kind,
  or bespoke resistance.
- A felled fen adder pays the wilderness loop in raw meat and hide and feeds Hunting under
  the existing paid-kill rule. Existing cooking and hide-sale paths consume those goods.
  Its read tiers, glyph, intents, posture, remains, companion targeting, and V1-07
  moveset interactions follow the same contracts as every other ordinary family.

#### Salt work and the completed caravan source

- Put three finite workable pans in the salt worksite. Each pan may be worked once per
  world. A work attempt is legal only under Calm or Wind in the fen climate band.
- A legal attempt spends six completed turns, yields one sack of the existing Salt good,
  feeds one paid Survival use, replaces the worked pan with exhausted ground, and writes
  a stable work fact. Wet or Cold refuses before time, skill, or state is spent.
- Salt keeps its existing death, crossing, peddler purchase, town sale, Commerce, and
  price rules. Add no new material, container, crafting recipe, regional currency, price
  simulation, or weather-scaled yield.
- Either conclusion of the regional arc schedules one peddler restock for the next coarse
  tick. The delivery restores up to two sacks on the cart, capped at that world's original
  tier-derived stock, writes and narrates a fact, and can occur only once per world. It
  completes A1's outstanding peddler-restock follow-on without making stock infinite.

#### The compact, the carriers, and the regional arc

- The salters' compact is a named regional institution expressed through roles, facts,
  topics, scheduled state, and the local arc. It is not a new `FactionId`, Fame or Infamy
  scalar, universal relation ledger, or population simulation.
- The carriers' guild is the Town-to-Fen edge. Its existing bond opens the compact's
  freight-facing surfaces, and the completed arc schedules the cart restock above. No
  outcome grants positive Town Fame or changes the Stead-to-Town grain road.
- Add one bounded regional arc per world. It is independent of the selected world-story
  template, uses the hamlet and all four sites through produced and consumed facts, and
  reaches one of two authored conclusions. The two conclusions have the same reward tier:
  one sack of salt, the one restock schedule, an outcome fact, and witnessed aftermath.
  They grant no coin, Essence, Regard, permanent character mark, or new reputation.
- Every transition is perceivable. The compact's unresolved pressure has explicit exits
  through either conclusion or the ordinary crossing reset. The arc never blocks the
  waygate, the selected world story, the Aegis arc, or access to the region's ordinary
  sites and salt work.
- Keep the six existing world-story templates as the 1.0 pool. A seventh template and
  full-form expansion of the existing slice-scale templates remain post-1.0.

#### Three launch readers

- Add one once-per-world reader for `shame/housebroken` in which the known housebreaker
  meets a lane that knows. It is mechanically inert, names no unknown culprit, and adds
  no further Shame, fine, bar, or reward.
- Add the two Calling follow-ons already tracked under D-099: one bounded villager notice
  of a present shade and one warder response to the called uncanny. Each is a once-per-world
  reader, changes no allegiance or reward, and cannot give an NPC knowledge it lacks.
- The peddler restock is the third promoted reader and A1 closure. Together these items,
  the regional arc, and the final audit are the complete launch-content closure. No other
  unchecked catalog line is silently promoted.

#### Generator version and save freeze

- Add a generator version to the save header, separate from the save-format and product
  versions. The 1.0 generator is version 1. Save replay passes the recorded generator
  version through every world generated by that campaign.
- The pin is campaign-scoped because seed plus the append-only key journal rebuilds the
  entire campaign from its first world. An existing campaign therefore keeps its recorded
  generator for later crossings; a new campaign uses the newest supported generator.
- A future build that changes generation must retain the old entry point for supported
  campaigns or reject that generator version explicitly. It may never silently rebuild
  an existing journal against a different generator.
- Worldgen, sim, journey, snapshots, and release evidence expose the generator version.
  `worldgen` can select any supported version for regression audits, and unknown versions
  fail before generation.
- Assuming V1-01 through V1-08 land in order, V1-09 advances save v98 to v99 and writes
  generator version 1. Product version `1.0.0` remains an independent assembly and package
  version. Pre-1.0 save formats remain explicitly rejected; no migration is invented.

#### Release journey and observability

- The default journey learns the fen route, weather, banks, adder, hamlet, sites, and
  return path and cannot stall when the new region is present.
- Add `journey --release`, a deterministic twelve-world release route that composes the
  approved demonstrations from V1-01 through V1-09. It traverses and completes the fens,
  works all three pans, observes at least one weather refusal without wasting a turn,
  resolves the regional arc, receives its restock, and fights the fen adder honestly.
- The release route carries a machine-readable coverage matrix for every tranche. It
  exits nonzero when any mandatory demonstration is absent, and every emitted journal
  replays exactly through `sim`.
- Journey prose and JSON report fen visits and crossings, hamlet and site outcomes, pan
  attempts, refusals, work, salt, adder reads and kills, arc state and conclusion,
  restock state, save version, generator version, and the coverage matrix.
- WorldEval and `worldgen --json` record the fourth region and climate band, generated
  hamlet, exact site mix, reachable mouths and entrances, fen terrain counts, three pans,
  adder tenancy, regional facts and prose surfaces, and generator version. All region
  generation participates in the purity digest.

#### Manual release-candidate playthrough

- The manual gate uses a clean extraction of the candidate package and one fresh named
  save, with no pilot, debug hook, edited journal, or development build.
- The playthrough covers creation, ordinary exploration and trade, all four regions,
  every activity family, representative combat and magic, a mortal guest and shade,
  death, remnant recovery, Wounded, Toll and scar recovery, world-story completion, the
  complete Aegis progression through resolution, and at least one post-resolution crossing.
- Record product, save, and generator versions; seed; commit and package hashes; terminal
  and Windows version; start and finish times; final cycle; and every defect. The public
  checklist records coverage and verdicts without recounting story reveals.
- The user's explicit sign-off is required. Automated journeys, tests, or a prior
  development playthrough cannot substitute for this fresh packaged run.

#### Defect gate, documentation, and packaging

- A release blocker or major defect is any crash, hang, data loss, corrupt or
  nondeterministic save, replay disagreement, unreachable required progression, conflict
  without an exit, package startup failure, inaccessible required control, unreadable
  supported terminal layout, or material contradiction of an Approved launch contract.
- 1.0 requires zero known blocker or major defects. A minor prose or cosmetic defect may
  remain only when it is written in the release audit and explicitly accepted. Any engine
  fix after candidate evidence invalidates that candidate and reruns verification in
  proportion to the change, including the full sweep for an engine change.
- The initial release target is Windows x64. Publish the CLI in Release as Native AOT and
  self-contained, then package `aegis-1.0.0-win-x64.zip` from a clean output directory.
- The zip contains `aegis.exe`, a spoiler-free README with controls, saves, terminal
  expectations and platform scope, 1.0 release notes, and required third-party notices.
  Generate a SHA-256 manifest carrying commit, product version, save version, generator
  version, runtime identifier, and every packaged file hash.
- A repeatable local PowerShell release script kills `aegis.exe`, publishes, stages,
  hashes, zips, and smoke-tests a clean extraction with `--help`, a short exact sim, and
  `worldgen --json`. It must fail on dirty or missing package inputs but never alters
  source or save data.
- Add no installer, updater, telemetry, network call, automatic save migration, code
  signing, or remote release machinery. Linux and macOS packages wait for native-host
  build and verification. This card makes no source-license decision.

#### Post-1.0 classification and final acceptance

- During V1-09 implementation, annotate every roadmap line still unchecked or partial
  after V1-01 through V1-08. Each must either cite this card as promoted or say
  `post-1.0` with its governing decision. Category-level prose never substitutes for
  line-level classification.
- Unless an earlier card completes them, the post-1.0 families are: further gear, enemy,
  oath, scar, Legend, working and template catalogs; folk reculturing, world epithets and
  deeper creation integration; NPC schedules, named-population simulation, retinues and
  broader companion arcs; tracking, swimming, climbing, further recipes and further
  activity depth; deeper faction relations, caster reputation and town law; playable
  memory scenes and other recurring-story expansion; external content files,
  localization and modding; and non-Windows packages.
- Aegis is 1.0-ready only when all ten cards are Verified; the complete engine and
  release journeys pass; save and generator contracts pass; the clean package passes
  smoke tests; the manual campaign is signed off; the roadmap has no unclassified open
  line; every important shipped fact has a reader; every live conflict has an exit;
  every activity pays at least two D-006 outputs; and the defect ledger has no blocker
  or major entry.

### Acceptance and sweep requirements

- Region tests cover stable end-appended identities, independent seed streams, name
  uniqueness, connected generation, both road crossings, all terrain and mount rules,
  the climate band, camp and forecast behavior, death and crossing, and every hamlet and
  site surface.
- Site and combat tests cover exact site mix and reachability, produced and consumed
  facts, adders crossing one water cell but ending on ground, both attacks and marks,
  reads, posture, companions, Hunting, remains, rewards, and the no-unreachable-enemy
  invariant.
- Economy and arc tests cover all three pans, six-turn labor, weather refusal and every
  no-cost boundary, salt persistence and sale, both arc conclusions, equal rewards,
  schedules, cap, single restock, compact and carriers' facts, witnessed aftermath, and
  crossing reset.
- Reader tests prove the housebreaker and both Calling beats fire only with honest
  knowledge, once per world, with no numeric consequence. The fact audit proves every
  important launch fact has at least one reachable reader.
- Save tests pin the v99 header shape, generator 1, campaign-scoped replay, unknown and
  unsupported rejection, unchanged key journaling, and exact replay through multiple
  crossings. Worldgen tests regenerate every supported generator identically.
- Release-tool tests pin the coverage matrix, failure exit, JSON, clean staging, manifest
  content, hash verification, package name, and clean-extraction smoke commands without
  mutating user saves.
- Kill `aegis.exe`, build Release, run the complete test suite, run seeds 1, 7, 99,
  2024, and 88888 through two byte-identical twelve-world default journeys and two
  byte-identical twelve-world release journeys, compare and justify drift from v98,
  replay seed 1 from both modes to exact keys, cycle, and turn, and pass the version-1
  `worldgen --json` purity and structural gates.
- Publish the Windows x64 candidate, verify its manifest and clean-extraction smokes,
  then complete the manual protocol. A documentation-only correction after packaging
  requires a new package and hashes; an engine change requires the full sweep again.

### Explicit exclusions

- No fifth region, second fen settlement, fifth fen site, boat, swimming, tide simulation,
  dynamic flooding, bridge building, mounted fen stride, poison system, elite adder,
  additional ordinary monster family, or new regional crafting material.
- No new reputation scalar, `FactionId`, positive Town Fame, regional property, law book,
  tournament, school, guild rank, population simulation, or persistent compact state
  across worlds.
- No seventh world-story template, full-form template expansion, playable Aegis memory,
  changed Aegis resolution, new companion arc, retinue, caster reputation, or further
  faction edge.
- No further gear, recipe, skill, knack, working, oath, scar, Legend rung, patron deed,
  hostility band, folk culture, background, world epithet, NPC schedule, or line-bank
  catalog beyond the explicitly promoted readers and regional content.
- No installer, updater, telemetry, network dependency, code signing, cloud save,
  automatic migration, remote CI or release, Linux package, or macOS package.

## V1-10: SadConsole client and release recovery

**Design status:** Implemented

**Decisions:** D-175, D-176, and D-177

**Roadmap association:** Path to 1.0 tranche 10; presentation; tooling and verification;
Windows x64 release package

**Dependencies:** V1-09 implemented under D-174; existing D-027 pilot and sim contract;
D-175 compatibility and focus-free control spike

**Implementation status:** Complete; clean replacement package and user signoff pending

### Player-facing outcome

- Aegis launches into its own tiled window rather than inheriting colors, font, and
  layout behavior from a terminal application.
- The packaged font, exact RGB palette, 120 by 40 logical frame, `Fit` resize behavior,
  and letterboxing make the supported presentation consistent across Windows themes and
  terminal settings.
- Existing canonical keyboard commands remain valid. Arrow keys remain physical aliases.
- The complete required frame stays visible through supported resizing.
- A fresh save can be created, played, closed, and reloaded through the new client.

### Architecture and automation

- `Aegis.Core`, `Frame`, `Presenter`, canonical characters, save v99, and generator 1
  remain unchanged unless implementation discovers a separately approved engine need.
- Add a frontend-neutral `Aegis.Host` library for the serialized session queue, saves,
  and pilot transport.
- Add an `Aegis.Client` Windows `WinExe` using SadConsole and MonoGame. It produces the
  shipping `aegis.exe`.
- Retain `Aegis.Cli` as `aegis-tools.exe` for pilot, sim, journey, worldgen, release
  diagnostics, and the legacy comparison client during migration.
- Physical and pilot inputs share one queue and one `Game.ApplyKey`, save append, and
  render path.
- Pilot control is opt-in and current-user-only. It never uses focus changes,
  operating-system input injection, UI Automation, or screen automation.
- Preserve `ping`, `screen`, `state`, `keys`, and `quit`. Add `frame`, returning the
  120 by 40 glyph and resolved RGB cell grid for focus-free visual inspection.

### Release recovery

- The D-174 terminal candidate is superseded and cannot receive final signoff.
- The replacement Windows x64 zip contains the SadConsole player, SDL2, OpenAL, tools,
  spoiler-free documents, third-party notices, and a complete SHA-256 manifest.
- Keep Windows x64 Native AOT with explicit roots for `SadConsole` and
  `SadConsole.Host.MonoGame`.
- Pin the exact known third-party IL2104 and IL3053 warning set. Any new or changed
  warning fails release verification. Ordinary Release builds and tests stay at zero
  warnings.
- Rebuild the candidate from clean output, run clean-extraction startup, render, pilot,
  input, save, tool, hash, and shutdown smokes, then restart the fresh guided campaign.

### Acceptance and verification

- Focused tests pin every physical key alias, case-sensitive command, resize contract,
  palette mapping, cell copy, pilot command, current-user-only pipe, request timeout,
  save append, and orderly shutdown.
- Frame parity tests compare the SadConsole cell model with the existing Presenter output
  over creation, overworld, local, menu, combat, death, and later-world surfaces without
  asserting story prose in release logs.
- A Windows integration test launches the packaged client, advances it through pilot
  keys, observes state and frame, and quits while a different foreground window identity
  remains unchanged.
- A manual physical-keyboard checkpoint covers arrows, printable keys, shifted movement,
  Enter where supported, Backspace where supported, Escape, held-key behavior, focus
  loss, and return from focus loss without stale repeats.
- Kill all Aegis processes, build Release, run all repository tests, run the complete
  default and release twin journeys, replay both seed-1 journals, and pass worldgen
  purity. With no engine change, every D-174 engine result must remain byte-identical.
- Publish and verify the clean SadConsole package, then complete a fresh packaged
  campaign. V1-09 and V1-10 become Verified only after explicit user approval.

### Explicit exclusions

- No engine mechanic, content, balance, world generation, or story change.
- No new canonical gameplay character.
- No animation, particles, sound, controller, touch, or mouse-only command.
- No runtime third-party font or theme loading.
- No installer, updater, telemetry, networking, cloud save, signing, Linux package, or
  macOS package.
- No removal of the legacy terminal renderer before V1-10 verification.

The complete implementation contract is `design/sadconsole-client-migration.md`.

## 1.0 gate

The release gate remains the one adopted in D-155 and detailed in
`design/plan-2026-07.md`, made executable by D-165 and amended by D-175: all ten
tranches Verified, full engine and release sweeps green, a clean Windows x64 SadConsole
package, a fresh manual packaged playthrough signed off, no known blocker or major
defects, current save, generator, help, release, and design documentation, every
important fact with a reader, every live conflict with a designed exit, and every
remaining roadmap line explicitly promoted or classified as post-1.0.
