# Aegis 1.0 Design Queue

This is the canonical implementation queue for the finite road to Aegis 1.0 adopted
in D-155 and made design-first in D-157. `design/roadmap.md` remains the source of
truth for feature status. This file owns the approved scope, dependencies, acceptance
criteria, and decision associations for the nine 1.0 tranches.

## Working contract

- Every card has a stable `V1-XX` identifier and one of four design statuses: Draft,
  Approved, Implemented, or Verified.
- Substantive choices are made with the user, recorded in `design/decisions.md`, and
  associated with the card here. Implementation discoveries amend the card through a
  later decision rather than silently changing its contract.
- No 1.0 card enters implementation until every card is Approved. This frontloads
  dependencies and removes design pauses from the build sequence.
- Approval fixes player-facing behavior, system boundaries, persistence, dependencies,
  and acceptance criteria. Exact prose, generated geometry, and balance tuning remain
  adjustable during implementation unless another card depends on them.
- A card reaches Implemented when its approved behavior exists. It reaches Verified only
  after its focused checks and any required engine sweep pass, its roadmap lines close,
  and its documentation is current.
- Work explicitly excluded from all nine cards is post-1.0 unless a later user decision
  promotes it into the gate.

## Queue at a glance

| Card | Tranche | Design status | Decisions | Implementation |
|------|---------|---------------|-----------|----------------|
| V1-01 | High-fells capstone, the black tarn | Approved | D-156 | Pending |
| V1-02 | Weather and seasons v1 | Approved | D-158 | Pending |
| V1-03 | D3 prose-variety infrastructure | Approved | D-159 | Pending |
| V1-04 | D1 pacing steering | Draft | Pending | Blocked on design pass |
| V1-05 | Town and economy depth | Draft | Pending | Blocked on design pass |
| V1-06 | Character and activity breadth | Draft | Pending | Blocked on design pass |
| V1-07 | Combat and magic depth | Draft | Pending | Blocked on design pass |
| V1-08 | Companions, factions, and consequences | Draft | Pending | Blocked on design pass |
| V1-09 | Next region and 1.0 release closure | Draft | Pending | Blocked on design pass |

## V1-01: High-fells capstone, the black tarn

**Design status:** Approved  
**Decisions:** D-156  
**Roadmap association:** Path to 1.0 tranche 1; B4 later regions; wilderness fishing  
**Dependencies:** D-138 camping, D-140 town market, D-146 high fells, D-153 regional
goods, D-155 release sequence  
**Implementation status:** Pending until the full design queue is Approved

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
  exact sim replay, justified drift from v95, and worldgen purity.
- The D-155 path tracker checks off tranche 1 only after implementation and verification.

### Explicit exclusions

- No fishing skill, random catch roll, bait, tackle wear, renewable pools, resident enemy,
  boss, fish spoilage, or within-world re-tenanting.
- Weather-specific catch rules belong to V1-02 and may be added only through that card.

## V1-02: Weather and seasons v1

**Design status:** Approved

**Decisions:** D-158

**Roadmap association:** Path to 1.0 tranche 2; weather and seasons; A2 follow-ons  
**Known dependencies:** Scheduled facts, stead event deck, road sky, wolf-winter, regions,
camping, economy, black tarn  
**Implementation status:** Pending until the full design queue is Approved

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

### Explicit exclusions

- No direct weather damage, combat modifier, weather skill check, catch or gathering yield
  change, spoilage, generic weather clothing, tent, forecast purchase, player calendar
  control, weather-specific enemy, independent town climate, or full atmospheric simulation.

## V1-03: D3 prose-variety infrastructure

**Design status:** Approved
**Decisions:** D-159
**Roadmap association:** Path to 1.0 tranche 3; D3  
**Dependencies:** Fact graph, storylets, scenes, talk topics, worldgen `--dump`, WorldEval
**Implementation status:** Pending until the full design queue is Approved

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

### Explicit exclusions

- No runtime AI, localization framework, external content-file migration, recursive
  grammar engine, procedural story generation, wholesale prose rewrite, text cycling,
  or migration of combat, economy, and mechanical feedback.

## V1-04: D1 pacing steering

**Design status:** Draft  
**Roadmap association:** Path to 1.0 tranche 4; D1; pacing authority question  
**Known dependencies:** Read-only teller, scheduled facts, coarse tick, stead events,
weather and seasons  
**Design pass must settle:** protected clocks, eligible event classes, delay and hasten
bounds, pressure inputs, quiet windows, observability, determinism, pilot measures,
failure behavior, and acceptance.

## V1-05: Town and economy depth

**Design status:** Draft  
**Roadmap association:** Path to 1.0 tranche 5; property, tournaments or duels,
commissions, books, town-life and economy partials  
**Known dependencies:** Town chunks, law, guild, Commerce, Persuasion, Smithing, Lore,
regional trade  
**Design pass must settle:** exact launch features, property scope, competitive play,
crafting or commission scope, remaining book titles, costs and rewards, faction and law
connections, pilot coverage, and acceptance.

## V1-06: Character and activity breadth

**Design status:** Draft  
**Roadmap association:** Path to 1.0 tranche 6; intended roughly eighteen-skill roster  
**Known dependencies:** Existing skill growth and knack questions, activities, crime,
wilderness, crafting, character creation  
**Design pass must settle:** Alchemy, Athletics, Stealth, and Larceny boundaries; costed
use-curves; activity feeds; overlap with Survival, Sleight, and existing craft knowledge;
creation hooks; knack requirements; pilot coverage; and acceptance.

## V1-07: Combat and magic depth

**Design status:** Draft  
**Roadmap association:** Path to 1.0 tranche 7; combat and magic open launch work  
**Known dependencies:** Weapon families, stances, parry, posture, workings, Focus,
Spellcraft, Will  
**Design pass must settle:** launch-sized moveset completion, stance and parry growth,
hostile magical pressure, Will resistance, working growth, caster social texture boundary,
enemy and player readability, pilot coverage, and acceptance.

## V1-08: Companions, factions, and consequences

**Design status:** Draft  
**Roadmap association:** Path to 1.0 tranche 8; companion, faction, and scar follow-ons  
**Known dependencies:** Guest and beast systems, relation ledgers, fact readers,
Death's Toll, scars, storylets  
**Design pass must settle:** exact launch-sized follow-ons, companion combat interactions,
faction consumers and edges, consequence aftermath, catalog additions, pilot coverage,
and acceptance.

## V1-09: Next region and 1.0 release closure

**Design status:** Draft  
**Roadmap association:** Path to 1.0 tranche 9; next full-density region; release audit  
**Known dependencies:** All earlier cards, region machinery, worldgen evaluation,
prose audit, pacing  
**Design pass must settle:** region identity and density contract, sites and activities,
factions and economy, launch story and content closure, generator-version decision,
manual playthrough protocol, post-1.0 classification, defect gate, packaging, and final
acceptance.

## 1.0 gate

The release gate remains the one adopted in D-155 and detailed in
`design/plan-2026-07.md`: all nine tranches Verified, full engine sweep green, a fresh
manual playthrough review complete, no known release-blocking defects, current save/help/
design documentation, every important fact with a reader, every live conflict with a
designed exit, and every remaining roadmap line explicitly promoted or classified as
post-1.0.
