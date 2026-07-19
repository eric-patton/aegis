# Aegis Roadmap & Feature Tracker

This is the living map of what is built, what is partial, and what is left. It is the
single place to answer "where are we and what is next." Keep it current.

## How to maintain this file (read before editing)

- **Check things off as they land.** When a feature ships, flip its box from `[ ]` or
  `[~]` to `[x]` and append the decision number, e.g. `[x] The bow (D-050)`. A feature is
  `[x]` only when it is built AND verified (tests or a pilot/journey proof).
- **Add new work the moment it is found.** When a session turns up a missing piece, a
  follow-on, or a newly-scoped feature, add a line under the right pillar right then, so
  nothing lives only in one conversation. If it is a design question rather than a build
  task, put it under "Open design questions" instead.
- **Stay in sync with `decisions.md`.** Every `[x]` here should trace to a decision, and
  the "Open design questions" section should mirror the parking lot at the end of
  `decisions.md`. When they drift, reconcile them.
- **Do not spoil story in chat, but this doc may hold full detail.** It is a design doc.

## Legend

- `[x]` built and verified
- `[~]` partial: a foundation exists, real depth remains
- `[ ]` not started (design exists, no code)
- `◇` open design question (not yet decided; see the parking lot)

---

## Status at a glance

The **spine is deep for a martial (melee + ranged) build** and the full trans-world story
arc ships. The **breadth pillars are the holes**: magic, factions, companions, player
crafting, character creation, and the Death's-Toll/scar layer are design-only, as are three
of the four activity families (crime, town-life, most wilderness). Rough fill levels:

- Attributes: **4 of 7** mechanically active (Mind, Will, Presence are inert)
- Skills: **8 of ~18** (five combat, Hunting from D-070, Cooking from D-073, Survival from D-074)
- Activity families: **wilderness-living core built** (hunting, selling, cooking, foraging: D-070..D-074) and the **craft family opened** (cooking); crime and town life unbuilt
- Launch story templates: **2** built (of 3 named, 4-5 planned)
- **Factions begun (D-076..D-079):** the local-reputation foundation is in (the home stead's
  regard, a per-world Fame earned by perceivable deeds), it pays (D-077, the friend's welcome),
  the ledger went keyed with a second faction (D-078, the raiders' wrath: one notch per
  raider slain, dread softening their blows past its rung), and the coarse tick began (D-079,
  the raids are real: uncleared camps raid the stead every 160 turns, bread a coin dearer per
  raid); the stead-Infamy half (needs a transgression verb) and richer boons remain
- Major vision pillars still unbuilt: magic, companions, crafting, character creation, Toll/scars

---

## Phase map (suggested sequence, not locked)

- **Phase 0: Foundation & tooling.** `[x]` DONE. Engine, combat, martial progression,
  death/NG+ spine, the Aegis arc, and the journey-bot verification harness all ship.
- **Phase 1: First breadth increment (current).** **Hunting v1 shipped (D-070):** the
  wilds site, the fleeing hart, the Hunting skill, and a yield of meat + hide, with its
  **sell path now closed (D-071):** the woodward's trade sub-menu turns cured hides to
  coin, and sets the reusable vendor-menu pattern the rest of the economy will use. It
  established the activity -> skill -> yield -> coin loop the other families reuse. Still
  open in this phase: the rest of the wilderness family, and growing the wood's-edge bench
  (cooking, foraged-goods sale). Deferred alternative for a next lane: **character
  creation** (races + backgrounds).
- **Phase 2: A keystone pillar (current).** **Factions started (D-076..D-078):** the
  local-reputation foundation shipped (the stead's regard, perceivable-deed earning, per-world
  reset), its first boon (D-077, the friend's welcome), and the second faction on a keyed
  ledger (D-078, the raiders' wrath, with the dread softening their blows: a blow to one is
  a favor to the other, live), and the coarse tick's first event (D-079, the raids are real:
  live pressure to clear the camp, price consequence, designed exit). Next in this phase:
  richer regard-gated boons (a friend's price, gated access, a rumor kept from strangers),
  the stead-Infamy half (needs a transgression verb: user steer), and growing the tick
  toward true state vectors. **Magic** remains the alternative keystone (activates
  Mind/Will and the caster build).
- **Phase 3: Remaining pillars & stakes.** Companions, the Death's-Toll/scar layer, the
  other activity families, and the skills those unlock.
- **Ongoing: Breadth & depth.** Catalog growth (templates, monsters, tiers, gear, oaths),
  combat depth (posture, parry, movesets), and narrative depth (dialogue trees).

---

## Feature checklist by pillar

### The Spine (foundation), built

- [x] Deterministic engine: hierarchical seed tree, fact graph, worldgen (D-002, D-013, D-018)
- [x] Layered-map presentation, TUI render layer (Frame/Presenter) (D-001)
- [x] Save system: seed + input journal, replay-on-load, currently v29 (D-012, D-028)
- [x] NG+ crossing: waygate, coin -> Legend, tier-deepening worldgen (D-011, D-029)
- [x] The Aegis as diegetic companion voice (D-010, D-019)
- [x] The full trans-world Aegis story arc: reveal ladder -> the keeping -> the mending -> steady state (D-020, D-026, D-037, D-038, D-039, D-045, D-060)

### Combat

- [x] Telegraphed-intent grid, stamina economy, dodge/strike (D-004)
- [x] Player ranged: the hunting bow (D-050) and the ash-spear reach thrust (D-056)
- [x] Knowledge-sharpened telegraphs: bestiary read tiers, dulling across NG+ (D-061)
- [x] Wits given combat meaning (read clarity) (D-059)
- [~] Enemy variety: 9 monster families across tier bands 2-7, plus the hart (fleeing game, D-070) (D-033, D-040, D-044, D-053, D-057, D-058)
- [ ] Posture / second bar, break-and-riposte (vision §4)
- [ ] Parry as a distinct verb (vision §4)
- [ ] Weapon movesets: family-specific verbs, not just numbers (vision §4; deferred D-041)
- [ ] Monsters that read the player's commitment (the other half of D-004) (D-058)
- [ ] Formal duels / judicial combat set-pieces (vision §4)

### Character identity

- [~] Attributes: 7 defined, **4 active** (Might, Grace, Vigor, Wits); Mind/Will/Presence inert (D-015)
- [ ] Character creation flow (pick at start) (vision §3)
- [ ] Races: familiar anchors + originals, per-world regenerated cultures/standing (D-017)
- [ ] Backgrounds: seed starting skills; some starts illiterate (D-005)
- [ ] Literacy skill + books gating recipes/techniques/lore (D-005, vision §3)

### Skills (6 of ~18)

- [x] Blades, Hafted, Brawling, Warding, Ranged (use-grown, cost-gated) (D-042, D-050)
- [x] Hunting: use-grown, fed by game brought down in the wilds; fattens the hide yield (D-070)
- [x] Cooking: use-grown, raw meat to rations at the wood's-edge fire; fattens the yield (D-073)
- [x] Survival: use-grown, fed by foraging herbs from the wood; fattens the forage (D-074)
- [x] Knacks/perks at level 2 and 4 for the five combat skills (20 options / 10 questions) (D-046, D-055)
- [~] Craft skills: Cooking shipped (D-073); Smithing, Alchemy pending (vision §3)
- [~] Wilderness skills: Hunting (D-070) and Survival (D-074, foraging) done; Athletics pending (vision §3)
- [ ] Subterfuge skills: Stealth, Larceny (vision §3)
- [ ] Social skills: Persuasion, Commerce (vision §3)
- [ ] Mind skills: Lore, magic skills (vision §3)
- [ ] Proficiencies beyond the 3 lessons; book/mentor/quest-taught (D-052)
- [ ] Knacks: level-6+ questions, 3-option questions, knacks for new skills (D-055)

### The Life: activities & economy (2 of 4 families opened: wilderness, craft)

- [~] Economy v0: shop, rations, repair, herbwife mend, hide-sale, fact-derived prices (D-036, D-025, D-071)
- [x] Vendor sub-menu pattern: one talk digit opens a bench with its own nine slots (D-071)
- [x] Patronage deeds at the crossing (3: raised stone, endowed hearth, true verse) (D-054)
- [~] Crafting trades: cooking shipped (D-073); smithing, alchemy as player lanes pending (D-006, D-025)
- [~] Wilderness living: hunting + sell path + cooking + foraging shipped (D-070, D-071, D-073, D-074); tracking, fishing, camping pending (D-006)
- [x] A hide-buyer with room to grow: the woodward's trade sub-menu, hides to coin (D-071)
- [ ] Crime: lockpicking, pickpocketing, burglary, fencing (D-006)
- [ ] Town life: gambling, carousing, tournaments, property, caravan/arbitrage (D-006)
- [ ] Aspirational sink ladder: property, retinue, master training, commissions (D-025, D-036)
- [~] Grow the wood's-edge bench: cooking (D-073) + foraged-goods sale (D-074) shipped; hunting gear/lessons pending (D-071)

### Magic

- [ ] Spell system: found not menu-picked (grimoires, mentors, shrine rituals) (D-022, vision §5)
- [ ] Attunement capacity from found world objects (D-022)
- [ ] Mind = potency, Will = control; casts draw shared stamina; miscast risk (D-022)
- [ ] Telegraphed cast windups, interruptible both ways (D-022)
- [ ] Caster social texture: awe, suspicion, faction attention (D-022)
- [ ] Spell list / school content design (◇ parking lot)

### Factions & the living world

- [~] Fame/Infamy dual reputation per faction (D-023, D-076, D-078): local Fame with the home
  stead built (the regard ladder, perceivable-deed earning, per-world reset, HUD + greeting
  surfacing) and the keyed per-faction ledger shipped with the raiders' wrath (D-078, the
  Infamy-shaped enemy ledger); the true stead-Infamy half (deeds that cost regard, a stead
  that turns cold) needs a transgression verb the game does not yet have (◇ user steer)
- [~] Regard-gated boons and access (D-076, D-077): the friend's welcome shipped (D-077, the
  first faction save-format touch, v30): the stead's folk gift a coin purse when they first
  hold the bearer a friend, deed-earned so the hushed name never silences it. Richer boons
  (a friend's price, a gift of goods, a topic/rumor kept from strangers, gated access) pending
- [x] A second faction with a relationship to the stead (the raiders as its standing enemy, so a
  blow to one is a favor to the other) (D-078: wrath per raider slain on its own faster ladder,
  the dread softening raiders' blows past rung 2, reset at every crossing, save v31)
- [~] Faction state-vectors on a coarse tick, transitions write facts + narration hooks (D-023,
  vision §2, D-079): the tick seam exists and its first event runs (uncleared camps raid the
  stead every 160 turns, capped at 3/world: fact + narration + ration-price consequence, camp
  clear as the designed exit, save v32); true multi-axis state vectors and transition rules pending
- [ ] Bounded Nemesis-style leader/lieutenant roster with memory (D-023)
- [ ] Designed conflict exit conditions (no eternal stalemates) (D-023)
- [ ] (Unblocks: full-form story templates, institution/zealot/warden roles) (D-035)

### Companions

- [ ] Summon slot: one autonomous ally, resource-gated (D-024, vision §7)
- [ ] Guest companions: role-cast from world NPCs, can permanently die (D-024)
- [ ] Pack animal / mount: logistics and warmth (D-024)

### Death, stakes & consequence

- [x] Death loop: banked vs at-risk, corpse run, remnant forfeit (D-008)
- [x] Wounded state (reduced max HP, timed recovery) (D-008)
- [ ] Death's Toll meter: fills on death, drains over time (D-009)
- [ ] Scars: permanent consequences from clustered/boss deaths, costly cures (D-009)

### Narrative & dialogue

- [x] Storylet engine: fact-gated beats, 7 triggers, ~50 storylets (D-030)
- [x] NPCs: 5 kinds, bump-to-talk, ask-about menus from the fact graph (D-031)
- [x] World-story template compiler + selection (D-032, D-035, D-040)
- [~] World-story templates: 2 built (Raided Stead, Creeping Blight) (D-032, D-035)
- [ ] Story templates: Usurped Throne, War of Faiths (need factions) (D-020, blocked by D-023)
- [ ] Story templates 4-5 (◇ parking lot)
- [ ] Dialogue-tree scenes with visible skill checks (D-021; storylets are log lines today)
- [ ] NPC depth: per-role voices, schedules, movement (D-031)

### Content catalogs (breadth, grow over time)

- [~] Gear: 10 items; req axes Might/Vigor/Grace; deep signature verbs pending (D-041, D-056)
- [~] Oaths: 7 (grow with new systems: weather, factions, companions) (D-047, D-051)
- [~] Legend: 5 rungs, 3 hospitality boons; rungs past 5 open (D-048)
- [~] Hostility-tier bands: 2-6 distinct + tier-7 recombination; tier 7+ approach open (D-033..D-058)
- [ ] Scar list (part of the unbuilt Toll system) (D-009)
- [ ] Weather / seasons (referenced only in flavor today) (vision §1, D-051)
- [ ] Region entities, biome names, culture-flavored name pools, world epithets (D-049)

### Tooling & verification, built

- [x] Dev harness: headless pilot pipe, `sim` scripted JSON runs (D-027)
- [x] Journey-bot autopilot: clears sites, arms, raises, reclaims, loots, answers the sheet, walks the arc, swears oaths, hunts, sells hides, cooks meat, forages and sells herbs (D-062..D-075)
- [ ] `--wits` demo mode for the perception build (deferred D-063)
- [ ] Machine-readable journey report for a sweep / CI (deferred D-063)

---

## Open design questions (mirror of the `decisions.md` parking lot)

- ◇ Final race list: which 1-2 originals join the anchors (D-017)
- ◇ Spell list / magic-school content (architecture set by D-022)
- ◇ Storylet external data-file format + condition/effect vocabulary (D-030)
- ◇ Catalogs to grow: more oaths, the scar list, Legend rungs past 5, patron deeds past 3, hostility bands past the fen-leaguer (D-047, D-009, D-048, D-054, D-033+)
- ◇ Story open items: bottle-episode playability, Unbinder guise tells, reveal-tier sharing across characters, template 4-5 candidates (aegis-arc.md §11, world-story-templates.md §9)
- ◇ Tier 7+ content: more distinct bands vs a recombination system (D-058)

---

## Changelog / newly tracked

Newest first. Log when a feature is checked off, or when new work is added to this file.

- 2026-07-19: **D-079 the raids are real: first coarse-tick faction event.** While a camp stands, the raiders raid the stead every 160 turns (cap 3/world): each raid writes an event fact, narrates as it fires (D-023's mandatory hook), and prices bread +1 coin at the steadholder for the rest of the world (inside the hungry-road doubling). Tick counts from world arrival; skipped while the bearer is inside the camp (a den defends its own); camp clear is the designed exit but taken grain stays taken until the crossing. The Raided Stead becomes live pressure; the tick seam in AdvanceTurn is reusable machinery for every later faction event. Save v31 -> v32. 357 tests green (6 new RaidsTests); byte-identical + emit->sim exact + sweep honest (12-25 raids/run). Partial-checked the coarse-tick state-vector item.
- 2026-07-19: **D-078 the raiders' wrath: second faction, keyed ledger.** The single regard scalar became a FactionId-keyed store, and the raiders now keep the enemy ledger: wrath +1 per raider slain (all kill paths), on its own faster ladder (1/2/4: a name the raiders curse / a dread on the raiders / the bane of the dens), reset at every crossing. Past the dread rung a raider's blow lands one point weaker (never below 1), applied after the dice so the draw count never moves. A blow to one is a favor to the other, live: emptying the camp raises stead regard and raider wrath in the same strokes. HUD (red, under the stead's green), snapshot (Wrath/WrathTitle), journey dens line. Save v30 -> v31. 351 tests green (8 new WrathTests); byte-identical + emit->sim exact + five-seed sweep (all peak at the bane of the dens). Checked off the second-faction item; stead-Infamy re-scoped: needs a transgression verb (user steer).
- 2026-07-19: **D-077 the friend's welcome, regard's first boon.** Regard now pays: the first time a stead holds the bearer a friend (rung 2, reached by clearing the camp), its folk gift a coin purse. Coin not bread (stays clear of the arrival-welcome's larder); once per stead (rung-cross gated); NOT silenced by the hushed name (deed-earned, not name-carried). Save v29 -> v30, the first faction touch on the format. 343 tests green (4 new welcome tests + 6 honest crossing-math updates for the +5); byte-identical + emit->sim exact + sweep unchanged in shape. Partial-checked regard-gated boons.
- 2026-07-19: **D-076 factions, first rung: the stead's regard.** The keystone pillar begins. The home stead now keeps a per-world local Fame for the bearer, earned only by deeds it can perceive (camp cleared +3, barrow stilled +2; remote deep-site deeds pass none), on a plain ladder (a known face / a friend to the stead / the stead's own), surfaced on the live HUD and in greetings, reset at every crossing, set beside Legend's cross-world Standing not merged with it. No save-format change (regard gates nothing mechanical yet, so old journals replay identically). 339 tests green (9 new RegardTests); byte-identical + emit-keys->sim exact + five-seed sweep (all peak at the stead's own). Partial-checked Fame/Infamy; new tracked items: regard-gated boons (next, first save touch), a second faction with a relationship, the Infamy half, the coarse tick.
- 2026-07-18: **D-075 bot forages and sells herbs.** The autopilot now deliberately gathers herb spots before crossing and sells the satchel at the bench, so the whole wilderness family (hunt, sell, cook, forage, sell) is exercised live end to end. Master seed forages 44 sprigs for 176 coin, arming it better so deaths fall 7 -> 5. Cli-only, no engine/save touch. Checked off the bot-forage item.
- 2026-07-18: **D-074 foraging shipped.** Herbs grow in every world (own worldgen stream), picked by anyone on the step, growing a new Survival skill (8th) and sold at the woodward's bench for coin. Rounds out the wilderness-living core (hunt/sell/cook/forage). Bot forages incidentally. Checked off Survival, partial wilderness/bench. Save v28 -> v29. New tracked item: bot seeks + sells herbs.
- 2026-07-18: **D-073 cooking shipped, the first craft.** A 7th skill (Cooking); the hart now yields raw meat, cooked into rations at the woodward's bench (D-071), skill-scaled, capped at what a body can carry. Opens the Craft family. Bot cooks live. Checked off Cooking skill, partial Craft family, partial crafting-trades/wilderness/bench. Save v27 -> v28 (also covers D-071's un-bumped bench).
- 2026-07-18: **D-072 journey-bot sells its hides.** The autopilot now cashes the hunt out at the wood's edge (one overworld errand + driving the D-071 bench), so the whole catch-cure-sell-coin loop is exercised live and reproducibly. Master seed sells all 34 hides for 102 coin; sweep all sell their take and reach the mending. Cli-only, no engine/save touch. Checked off the bot-sells item.
- 2026-07-18: **D-071 hide-sell path shipped.** The woodward's "wood's edge" trade sub-menu (a reusable vendor-menu pattern behind one talk digit) turns cured hides to coin and now holds the Gleaning lesson too; hunting feeds three payoffs (skill, food, coin). Checked off the hide-buyer, the vendor sub-menu pattern, and folded the sale into economy v0. New tracked item: teach the journey-bot to sell (close the loop in the autopilot). No save-format change.
- 2026-07-18: **D-070 hunting v1 shipped.** New: the wilds site, the hart, the Hunting skill (6th), meat + hide yield; journey-bot hunts live. Checked off Hunting skill and partial wilderness/Life family. New tracked item: a hide-buyer vendor menu (sell path deferred at the 9-digit menu cap). Save v26 -> v27.
- 2026-07-18: Tracker created. Snapshot of Phase 0 complete (spine + journey-bot through D-069).
