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
| V1-02 | Weather and seasons v1 | Draft | Pending | Blocked on design pass |
| V1-03 | D3 prose-variety infrastructure | Draft | Pending | Blocked on design pass |
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

**Design status:** Draft  
**Roadmap association:** Path to 1.0 tranche 2; weather and seasons; A2 follow-ons  
**Known dependencies:** Scheduled facts, stead event deck, road sky, wolf-winter, regions,
camping, economy, black tarn  
**Design pass must settle:** scope and calendar, regional variation, mechanical effects,
forecasting, player counters, event-deck additions, economy interactions, persistence,
twist interactions, presentation, pilot policy, and acceptance.

## V1-03: D3 prose-variety infrastructure

**Design status:** Draft  
**Roadmap association:** Path to 1.0 tranche 3; D3  
**Known dependencies:** Fact graph, storylets, talk topics, worldgen `--dump`, WorldEval  
**Design pass must settle:** surface inventory, fragment contract, composition rules,
authored variation budget, skeleton detection, curation output, thresholds, CI role,
content migration boundary, and acceptance.

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
