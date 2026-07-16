# Aegis Design Decision Log

Codename "Aegis": a terminal (TUI) single-player RPG. Elden Ring / Kingdom Come: Deliverance tone and depth, turn-based instead of real-time. High fantasy, classless (stats + skills + equipment define capability), mostly procedural world with authored-feeling storylines, death as setback not permadeath, no two playthroughs alike, infinite NG+ where a finished character enters a fresh harder world, multiple characters with selectable races / starting stats / starting gear.

Research backing these decisions lives in `research/` (00-overview.md is the synthesis).

## Pillar emphasis (user-stated, 2026-07-16)

What each inspiration actually contributes:
- From KCD: starting from nothing and earning competence (zero-to-hero), and a LOT of different things to spend your time doing (activity breadth). NOT the fencing simulation specifically.
- From Elden Ring: classless design, equipment and stats as identity, grinding as a legitimate and enjoyable loop, the danger of what dying does, exploration, and story.

## Decided

### D-001: Presentation is layered maps (2026-07-16)
Walkable coarse overworld (one tile = a landmark or region), local tactical tile maps for dungeons/towns/sites worth exploring, and full-screen prose scenes for dialogue, events, and quest beats.
Rationale: keeps real "what's over that ridge" map exploration (Elden Ring's core joy) while giving narrative a first-class surface (skill checks and dialogue in prose scenes), and keeps procgen cost sane at epic world scale. Rejected: full local-scale map everywhere (Qud-style; enormous procgen surface at epic scale, story becomes skimmable pop-ups) and prose-first travel (loses map exploration).

### D-002: Tech stack is C# / .NET (2026-07-16)
Rationale: the hard 80% of Aegis is a deterministic simulation core (world fact graph, storylets, hierarchical-seed procgen, seed+delta saves), and C# has the best iteration loop, debugger, and query ergonomics (LINQ over the fact graph) for that work. TUI layer will be a lean custom render layer or Terminal.Gui v2; NativeAOT single-file publish for distribution. Runner-up was Rust + ratatui (research's pick on rendering/binary-size grounds, but its costs land on the sim core, which is the hard part). Research: `research/05-terminal-ui-tech-stack.md`.

### D-003: Epic world, medium critical path (2026-07-16)
The world carries an epic amount of content (30h+ if you explore), but the authored main storyline is finishable in roughly 10-15h if you beeline. Elden Ring's own structure.
Rationale: preserves both pillars, deep storylines AND actually reaching NG+. Skipped content is not wasted because the next world is different. Matches research pattern: compact authored spine as the fixed anchor, procedural everything-else (`research/01-procedural-world-plus-authored-narrative.md`).

### D-004: Combat is a telegraphed-intent grid, lean version (2026-07-16)
Turn-based combat on the local tactical maps. Enemies display intents for their next action (attack arcs, windups, guards, repositioning); the player's turn is a response (step out, block, parry, trade, punish the windup). Commitment runs both ways: heavy player attacks take a visible windup enemies react to. Core modules at launch: stamina economy (attack/parry/dodge draw one pool), weapon-family movesets (weapons change verbs, not just numbers), posture/stagger bar, and knowledge-sharpened telegraphs (telegraph clarity scales with Perception, weapon familiarity, and bestiary knowledge of that enemy family; banked knowledge carries into NG+).
Shelved as possible later additions: zoned armor, body-part targeting menus, directional guards (KCD fencing sim de-emphasized per pillar statement). Menu-duel verbs may return later as formal 1v1 duel set-pieces reusing the same systems.
Pacing at epic length: lethality in both directions (trash dies in 2-3 turns), simple intents for simple enemies, and one-keypress overwhelm resolution when the player's threat massively exceeds the enemy's.
Rejected chassis: plain grid only (no reads, no duel feel), full timeline system (party-scale machinery for a mostly-solo game; we keep only action-speed differences and windup interrupts), menu duel as main system (no positioning, starves ranged/stealth/mage builds), deck-driven (genre transplant, randomness undermines reads).

### D-005: Uniformly humble starts (2026-07-16)
Every origin starts near the bottom of the power curve. Race/background decides WHICH skills are seeded and the flavor of your nothing (the hunter reads trails, the apprentice reads books), never how rich or strong you start. Literacy is a learnable skill; some origins start illiterate, and books gate recipes, techniques, and lore.
Rejected: wide origin spread (origin-as-difficulty dilutes zero-to-hero), universal destitution (kills early-hours replay variety).

### D-006: All four activity families are must-haves (2026-07-16)
Crafting trades (smithing, alchemy/herbalism, cooking), wilderness living (hunting, tracking, foraging, fishing, camping), crime (lockpicking, pickpocketing, burglary, fencing goods), and town life (gambling, carousing, tournaments, trading, eventually property).
Design discipline: every activity must feed at least two of (skill growth, money, world-state/reputation, story hooks), or it does not ship. Activities should hook into generated world facts (e.g. the apothecary buys herbs because her supplier died in the generated bandit raid).

### D-007: Growth is organic AND deliberately grindable (2026-07-16)
Use-based skill growth happens organically through normal play, and purpose-built diegetic grind loops also exist (sparring partners, training dummies, hunting grounds, dangerous farm spots) with diminishing returns and real costs (time passes, materials, durability, fatigue, danger). Every repeatable action costs something real, so grind is always a fair in-world investment and never an exploit (the Skyrim degenerate-loop failure is designed out).

### D-008: Death loop: banked vs at-risk, corpse run, Wounded (2026-07-16)
On death you respawn at your last rest point. Banked and untouchable, always: stats, skills, owned/worn gear, quest and relationship progress, recipes, bestiary knowledge. At risk: unspent coin plus all unbanked loot gathered since you last reached safety; it drops where you fell, one recovery attempt, gone forever if you die again first. Creates the expedition rhythm (carrying too much value is a decision; greed kills). You also wake Wounded: a temporary, visible, debt-framed debuff (e.g. reduced stamina cap) that fades with rest or can be worked off. Never any stat/skill/power loss (EverQuest death-spiral lesson). Every penalty is legible at the moment it lands.
Dependency resolved by D-014: unspent Essence joins the at-risk bucket, completing the full runes loop (coin + unbanked loot + unspent Essence drop on death).

### D-009: Scars via the Death's Toll meter (2026-07-16)
A visible meter fills on each death and drains over in-game time. Routine, spaced deaths never scar. Clustered deaths or boss-tier deaths risk converting into a scar: a rare, permanent-ish, characterful consequence (lost eye, crushed hand, haunted look) with mechanical weight and dialogue hooks. Calibration anchor: Wildermyth, a handful per playthrough. Every scar has a costly, in-world path back to parity (surgeon, pilgrimage, salve, occasionally a superior prosthetic). The meter makes the fairness legible before the consequence lands.

### D-010: The Aegis as diegetic device (2026-07-16)
The codename becomes the fiction. The Aegis (exact flavor TBD in narrative session: artifact, pact, ward) is what catches your soul at death and returns you to your last rest point, why rest points work (the Aegis anchors there), why you wake Wounded (the Aegis is spent), and what carries you between worlds at NG+ transition. The main storyline is about it. Bearers are singular, giving the chosen-one frame a mechanical justification.

### D-011: NG+ contract and scaling (2026-07-16)
Three-bucket carry-over at NG+ transition:
- Character: full carry (stats, skills, gear, recipes, lore, bestiary knowledge).
- World: always fully regenerated (map, NPCs, factions, quests, dungeons, history).
- Legend: bounded meta-layer with diminishing returns (0.5-0.8 exponent per incremental-game research): titles, Hall of Legends, small boons. Big enough that a new world's first hour feels like a visible step up; never big enough to trivialize content.
Difficulty composition: discrete World Hostility Tier as a first-class GENERATION input (each rung adds new enemy families, hazards, hostile-faction world facts, scarcer havens, modest stat bump; never a post-hoc multiplier, avoiding both the Elden Ring NG+7 cap-out and the Risk of Rain 2 decoupled-scalar failure) PLUS optional stackable covenants chosen at world creation (Hades Pact-style) totaling a visible Threat score whose rewards are legend/cosmetic/challenge currencies, never raw power.
Death consequences scale with tier in magnitude, never in shape: tighter reclaim windows, longer Wounded durations, faster Death's Toll fill, death reputation spreading further. (White space: no shipped game scales the death penalty across NG+ tiers.)
Amendment (2026-07-16): coin does NOT carry across NG+; it is converted at the crossing (conversion target TBD with the economy design: leading candidates are Legend credit or physical carryables). Keeps each fresh world's humble economy meaningful; gear and Essence-bought power still carry per the Character bucket.

### D-012: Save system (2026-07-16)
Autosave fires at the moment of death, before the penalty screen, carrying all consequence state (scars, reputation, world flags), so a naive reload lands post-death. Manual saves exist for normal life reasons. Optional Ironman toggle at character creation enforces single-slot autosave-only. Penalties stay mild enough that save-scumming is not worth the friction.

### D-013: Past characters enter world mythology (2026-07-16)
Completed characters' stories become candidate facts for future world generation (any new world: fresh character or NG+): statues, ballads, tombs containing their actual gear, factions that revere or curse their name. Multiple characters stop being parallel saves and become a personal mythology. (Precedent: Wildermyth legacy heroes.)

### D-014: Two-track growth: use-based skills + Essence-bought attributes (2026-07-16)
Skills grow by use, with every gain cost-gated per D-007 (ingredients, durability, fatigue, risk; skill XP never derives from gold value, closing the Skyrim alchemy loop by construction). Attributes are raised by spending Essence at rest points; Essence is earned from meaningful deeds (kills, quests, discoveries, first-time feats). Fiction: the Aegis gathers the essence of your deeds and reshapes you where it anchors (D-010). Unspent Essence is at-risk on death (amends D-008).
This overrules the research's pure-use-based recommendation deliberately: the research's real target was the Morrowind/Oblivion coupling of skill choices to attribute budgets, which Souls-style deed-currency does not have. Essence restores the runes loop, aspirational stat grinding toward gear requirements, and grind agency, all named user loves.

### D-015: Seven attributes with soft caps; gear prints requirements (2026-07-16)
Might (melee power, heavy gear, carry), Grace (speed, evasion, finesse, quiet movement), Vigor (health, stamina pool, resistances), Wits (perception, initiative, ranged aim, telegraph clarity per D-004), Mind (learning, lore, magic reservoir), Will (posture, fear resistance, magic control, Death's Toll resilience), Presence (social gravity, persuasion, leadership). Mind/Will reserved as casting stats pending the magic session; mental and physical power kept on independent axes (Qud lesson) so hidden classes never re-emerge. All scaling stats soft-capped (diminishing, never zero, returns). Weapons and armor print hard attribute/skill requirements visible before investment; under-requirement use is penalized, not blocked.

### D-016: Skills (~18 draft), perks, proficiencies, respec (2026-07-16)
Draft skill list (content, freely editable): Combat: Blades, Hafted, Polearms, Ranged, Brawling, Warding. Craft: Smithing, Alchemy, Cooking. Wilderness: Hunting, Survival, Athletics. Subterfuge: Stealth, Larceny. Social: Persuasion, Commerce. Mind: Lore (incl. literacy), plus reserved magic skill slots. Kept compact deliberately (no PoE-scale graph; equipment combinatorics carry build variety).
Perks: at skill thresholds, choose 1 of 2-3 mutually exclusive perks (KCD pattern). Proficiencies: discrete know-how (recipes, techniques, faction customs) from books, mentors, quests (CDDA pattern; where literacy pays off).
Respec: attributes only, via a rare lore-flavored "unbinding" of the Aegis, a handful per world, refreshed at NG+ transition (Larval Tear pattern). Skills never respec: use-based skills only ever reflect what you actually did.

### D-017: Races: familiar anchors, procedural cultures (2026-07-16)
Recognizable races (human, dwarf, elf, orc-ish, plus 1-2 originals TBD) as fixed anchors; each generated world rerolls their cultures, clans, feuds, and social standing. Mechanically a race gives small attribute tilts, one qualitative racial trait, and a social position in the generated world; the separately chosen background seeds starting skills. All starts uniformly humble per D-005.

### D-018: The narrative engine stack (2026-07-16)
Five layers, adopted per research consensus (`research/01`, `research/06`):
1. World fact graph as source of truth: causal-grammar history generation at worldgen (settlements, factions, notable NPCs, grudges, wars, relics as ID-referenced facts), extended at runtime by player deeds; past characters' legends (D-013) enter as facts.
2. Storylets as the only content unit: atomic, precondition-gated, handwritten chunks indexed by qualities; scales additively forever (the infinite-NG+ requirement). Accepted cost: a long "unimpressive middle" until the library is dense.
3. Role-casting (Wildermyth): pivotal scenes written against role slots, cast at runtime from existing world NPCs.
4. NPC memory (Nemesis): structured per-NPC logs of player interactions (including witnessed player deaths per D-008); dialogue is memory-driven SELECTION over large authored line banks, never open generation.
5. Pacing director (RimWorld): decides when content surfaces, separate from what it is; NG+ hostility tiers tune it.
Two enforced disciplines: every generated quest must trace to an existing world fact or is not generated; every important fact appears on multiple surfaces (quest, rumor, item text). Filler jobs stay honest ambient texture with salience-picked flavor variety.

### D-019: The Aegis is a bound intelligence (2026-07-16)
Ancient, sentient, fastened to the player; its nature is the game's central mystery. Provides a narrative voice at every death (Hades pattern), diegetic tutorialization, and the only continuous character across all worlds and NG+ cycles. (Refines D-010.)

### D-020: Two-layer main quest (2026-07-16)
Per world: an authored world-story spine template drawn from a growing pool (launch target ~3: e.g. usurped throne, creeping blight, war of faiths), structure handwritten and never proceduralized, cast/geography/factions filled from that world's fact graph. Across worlds: the authored Aegis mystery arc, advancing at world completions and NG+ transitions, unfolding over the first several cycles and resolving into an earned steady state (Hall of Legends, covenants, and fresh world-stories carry motivation beyond it).
Reference to study when writing the arc: FFXIV story summaries at `_external_resources/ffxiv_good_story/` (user-provided; the model for long-arc setup and payoff).

### D-021: Hybrid dialogue + no runtime LLM (2026-07-16)
Dramatic storylet scenes use choice menus with visible skill checks. Ordinary NPCs additionally expose a Morrowind-style "ask about" topic system with topics drawn live from the fact graph (the delivery mechanism that makes generated history queryable in-fiction).
Runtime is fully deterministic: zero LLM in the live loop (testability, reproducible seeds per D-011, self-contained offline binary). AI is an authoring-time tool only: mass-producing storylet prose and line-bank variants into the deterministic format, structure human-designed and machine-validated.

### D-022: Magic: rare and feared, diegetic acquisition, one economy (2026-07-16)
Magic is uncommon in the world and treated with awe or suspicion (grounded tone; caster reputation interplays with Presence and factions). Spells are acquired only diegetically: grimoire pages as loot (literacy gates them), mentors, shrine rituals tied to worldgen facts; never a level-up menu. Spell attunement capacity comes from found world objects (Elden Ring Memory Stone pattern) so attributes never double-tax hybrids; Mind scales potency, Will scales control. No separate mana bar: casting draws the shared stamina pool (one economy for true martial/magical hybrids), strong spells consume components, overreaching Will risks miscast. Casts are telegraphed windups on the intent grid, interruptible both ways. Use-based magic skill growth is spam-proofed by D-007. Research: `research/07-magic-systems-classless.md`.

### D-023: Factions: event-driven world-state layer (2026-07-16)
No full agent simulation (X4/A-Life cost-and-invisibility trap). Each faction is a small state vector with causal-grammar transition rules evaluated on a coarse tick; every transition writes a fact into the world graph AND carries a mandatory narration hook (rumor, notice, refugees, price change): if the player cannot perceive a change, it does not fire. Conflicts have designed exit conditions (Bannerlord stalemate lesson). Named leaders/lieutenants form a bounded Nemesis-style memory roster. Reputation is dual-scalar Fame/Infamy per faction (New Vegas). The pacing director throttles faction events off summary stats; hostility tiers raise faction aggression as a generation input. Research: `research/08-living-world-factions.md`.

### D-024: Companions: summon + mortal guests + pack animal (2026-07-16)
Three non-overlapping niches, no permanent tactical party (combat stays balanced around one character):
- One Spirit-Ash-style summon slot: autonomous, resource-gated against the player's own pool, no inventory/orders/scaling; conjurer build expression with near-zero UI.
- Story-scoped guest companions, role-cast from world NPCs, with one or two contextual command verbs, and they CAN permanently die: guests carry the mortal stakes the immortal player cannot (Wildermyth/Tyranny precedent).
- Pack animal / mount for logistics (carry capacity, travel).
The Aegis (D-019) remains the persistent banter/relationship voice. Research: `research/09-companions-hirelings.md`.

### D-025: Economy: auto-scaling sinks, crafting lane, patronage crossing (2026-07-16)
Coin is fully separate from Essence and must matter all game: upkeep/repair costs scale as a percentage of the player's own gear value (KCD pattern, auto-scales with wealth), plus a rising-cost aspirational sink ladder (property, retinue, master training, masterwork commissions) whose next rung is always priced above current means. Crafting lives in its own lane (consumables, augments/sockets, regional specialties), never racing loot for best-in-slot. Prices derive from world facts (regional scarcity, war, blockades); caravan/arbitrage play exists as productive capital. At the NG+ crossing, coin does not carry (D-011 amendment): it converts via patronage deeds (endow guilds, raise monuments, fund shrines, commission statues) into Legend credit AND candidate facts for future worldgen (D-013), so later characters encounter what past wealth built. Research: `research/10-single-player-economy.md`.

### D-026: Aegis-arc canon: The Ledger (2026-07-16)
FFXIV study complete (`research/11-ffxiv-storytelling/`, synthesis in `00-synthesis.md`). Canon A "The Ledger" adopted: the worlds are links on a Chain descending toward the Hearth where worlds are kindled; the Aegis is a ward-intelligence forged by the Shieldwrights to find and temper a soul fit to keep it; Essence is the scale, the Severed are prior bearers who broke or refused, the Unbinder (respec service) is the first bearer and fixed ideological anchor, hostility tiers are depth on the Chain, past-character mythology is bearer echoes. Arc structure: five-cycle reveal ladder (every touch a complete micro-reveal), ~15% content budget concentrated at world edges plus the crossing scene, threshold choice at cycle 5 (endings differ in fiction and voice, never mechanics), steady state converts the Aegis to a candid equal and makes covenant selection diegetic, no new grand mystery layered on. Full spec: `design/story/aegis-arc.md`. Rejected alternatives preserved there: B "The Loom" (worlds-are-woven; risks unrealizing mortal companion stakes), C "The Warden" (bearer carries a sealed danger; single-spend twist, fights the earned-heroism fantasy).
World-story templates spec'd canon-light at `design/story/world-story-templates.md`: template contract, twelve iron rules, shared role-slot library, three launch templates (usurped throne, creeping blight, war of faiths).

## Under discussion

## Not yet raised (parking lot)

- Final race list: which 1-2 original races join the familiar anchors (D-017)
- Spell list / magic schools content design (architecture set by D-022)
- Storylet data format and fact-graph schema (implementation-level; D-018 sets the architecture)
- Content authoring format (storylet data files)
- Catalogs to design later: covenant list, scar list, Legend-track boons, hostility-tier content bands
- Story open items listed in `design/story/aegis-arc.md` sec. 11 and `design/story/world-story-templates.md` sec. 9 (names, bottle-episode playability, threshold dressing, template 4-5 candidates)
