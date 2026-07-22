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
| V1-04 | D1 pacing steering | Approved | D-160 | Pending |
| V1-05 | Town and economy depth | Approved | D-161 | Pending |
| V1-06 | Character and activity breadth | Approved | D-162 | Pending |
| V1-07 | Combat and magic depth | Approved | D-163 | Pending |
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

**Design status:** Approved
**Decisions:** D-145, D-160
**Roadmap association:** Path to 1.0 tranche 4; D1; pacing authority question  
**Dependencies:** Read-only teller, scheduled facts, coarse tick, stead event deck,
weather and seasons  
**Implementation status:** Pending until the full design queue is Approved

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

### Explicit exclusions

- No new events, adaptive difficulty, act or hostility-tier tuning, player pacing controls,
  event backlog, storylet delay, scheduled-future manipulation, raid manipulation, weather
  manipulation, combat steering, new RNG stream, separately serialized pacing state, or
  player-facing teller meter.

## V1-05: Town and economy depth

**Design status:** Approved
**Decisions:** D-161
**Roadmap association:** Path to 1.0 tranche 5; property, tournaments or duels,
commissions, books, town-life and economy partials  
**Dependencies:** Town chunks, law, guild, Commerce, Persuasion, Smithing, Lore,
regional trade  
**Implementation status:** Pending until the full design queue is Approved

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

### Explicit exclusions

- No multiple houses, decoration system, periodic rent, passive income, tenants,
  population simulation, cross-world property, lethal arena, repeatable tournament,
  betting minigame, spectator simulation, NPC attack verb, or positive town Fame ladder.
- No general crafting interface, commissioned combat-stat gear, caravan consignments,
  dynamic supply simulation, vendor liquidity, town burglary, Stealth work, further book
  titles, or Lore knacks. Those remain with later cards or post-1.0 unless the final audit
  promotes them.

## V1-06: Character and activity breadth

**Design status:** Approved
**Decisions:** D-162
**Roadmap association:** Path to 1.0 tranche 6; eighteen-skill launch roster
**Known dependencies:** Existing skill growth, knack questions, hale-draught brewing,
combat movement, monster activity, crime, fencing, character creation, journey pilot
**Recommended implementation point:** sixth card, after V1-05 and before combat and
magic depth

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

**Design status:** Approved
**Decisions:** D-163
**Roadmap association:** Path to 1.0 tranche 7; combat and magic open launch work
**Dependencies:** V1-06 awareness and final skill roster; weapon families, stances,
parry, posture, workings, Focus, Spellcraft, Will, bestiary reads, guests, and shades
**Recommended implementation point:** seventh card, after V1-06 and before companion and
faction depth
**Implementation status:** Pending until the full design queue is Approved

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
