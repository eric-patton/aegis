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
- Skills: **6 of ~18** (five combat, plus Hunting from D-070)
- Activity families: **hunting shipped** (D-070, its sell path deferred); the four families still largely unbuilt (a v0 shop economy aside)
- Launch story templates: **2** built (of 3 named, 4-5 planned)
- Major vision pillars unbuilt: magic, factions, companions, crafting, character creation, Toll/scars

---

## Phase map (suggested sequence, not locked)

- **Phase 0: Foundation & tooling.** `[x]` DONE. Engine, combat, martial progression,
  death/NG+ spine, the Aegis arc, and the journey-bot verification harness all ship.
- **Phase 1: First breadth increment (current).** **Hunting v1 shipped (D-070):** the
  wilds site, the fleeing hart, the Hunting skill, and a yield of meat + hide. It
  established the activity -> skill -> yield pattern the other families reuse. Still open
  in this phase: the **hide-sell path** (deferred at the 9-digit menu cap, needs a
  dedicated hide-buyer menu) and, later, the rest of the wilderness family. Deferred
  alternative for a next lane: **character creation** (races + backgrounds).
- **Phase 2: A keystone pillar.** **Factions** (unblocks the two unwritten story templates
  and the reputation layer) or **magic** (activates Mind/Will and the caster build).
- **Phase 3: Remaining pillars & stakes.** Companions, the Death's-Toll/scar layer, the
  other activity families, and the skills those unlock.
- **Ongoing: Breadth & depth.** Catalog growth (templates, monsters, tiers, gear, oaths),
  combat depth (posture, parry, movesets), and narrative depth (dialogue trees).

---

## Feature checklist by pillar

### The Spine (foundation), built

- [x] Deterministic engine: hierarchical seed tree, fact graph, worldgen (D-002, D-013, D-018)
- [x] Layered-map presentation, TUI render layer (Frame/Presenter) (D-001)
- [x] Save system: seed + input journal, replay-on-load, currently v26 (D-012, D-028)
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
- [x] Knacks/perks at level 2 and 4 for the five combat skills (20 options / 10 questions) (D-046, D-055)
- [ ] Craft skills: Smithing, Alchemy, Cooking (need crafting lanes) (vision §3)
- [~] Wilderness skills: Hunting done (D-070); Survival, Athletics pending (vision §3)
- [ ] Subterfuge skills: Stealth, Larceny (vision §3)
- [ ] Social skills: Persuasion, Commerce (vision §3)
- [ ] Mind skills: Lore, magic skills (vision §3)
- [ ] Proficiencies beyond the 3 lessons; book/mentor/quest-taught (D-052)
- [ ] Knacks: level-6+ questions, 3-option questions, knacks for new skills (D-055)

### The Life: activities & economy (0 of 4 families)

- [~] Economy v0: shop, rations, repair, herbwife mend, fact-derived prices (D-036, D-025)
- [x] Patronage deeds at the crossing (3: raised stone, endowed hearth, true verse) (D-054)
- [ ] Crafting trades: smithing, alchemy, cooking as player lanes (D-006, D-025)
- [~] Wilderness living: hunting shipped (D-070, sell path deferred); tracking, foraging, fishing, camping pending (D-006)
- [ ] Crime: lockpicking, pickpocketing, burglary, fencing (D-006)
- [ ] Town life: gambling, carousing, tournaments, property, caravan/arbitrage (D-006)
- [ ] Aspirational sink ladder: property, retinue, master training, commissions (D-025, D-036)
- [ ] A hide-buyer vendor menu (the hunt's sell path, deferred at the 9-digit villager cap, D-070)

### Magic

- [ ] Spell system: found not menu-picked (grimoires, mentors, shrine rituals) (D-022, vision §5)
- [ ] Attunement capacity from found world objects (D-022)
- [ ] Mind = potency, Will = control; casts draw shared stamina; miscast risk (D-022)
- [ ] Telegraphed cast windups, interruptible both ways (D-022)
- [ ] Caster social texture: awe, suspicion, faction attention (D-022)
- [ ] Spell list / school content design (◇ parking lot)

### Factions & the living world

- [ ] Faction state-vectors on a coarse tick, transitions write facts + narration hooks (D-023, vision §2)
- [ ] Fame/Infamy dual reputation per faction (D-023)
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
- [x] Journey-bot autopilot: clears sites, arms, raises, reclaims, loots, answers the sheet, walks the arc, swears oaths (D-062..D-069)
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

- 2026-07-18: **D-070 hunting v1 shipped.** New: the wilds site, the hart, the Hunting skill (6th), meat + hide yield; journey-bot hunts live. Checked off Hunting skill and partial wilderness/Life family. New tracked item: a hide-buyer vendor menu (sell path deferred at the 9-digit menu cap). Save v26 -> v27.
- 2026-07-18: Tracker created. Snapshot of Phase 0 complete (spine + journey-bot through D-069).
