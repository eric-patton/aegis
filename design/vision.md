# AEGIS: Design Vision

*A terminal RPG about starting from nothing, dying without ending, and outliving worlds.*

This document is the synthesized design. The audit trail with rationale and rejected alternatives lives in `decisions.md` (D-001 through D-025); the research behind it lives in `../research/`.

---

## 1. What Aegis Is

Aegis is a single-player, turn-based, terminal (TUI) RPG written in C#. It aims for the tone and depth of Elden Ring and Kingdom Come: Deliverance with none of the real-time action: the same reads, commitment, danger, and earned mastery, delivered through decisions instead of reflexes.

You are nobody, in a world that was generated last Tuesday and has three hundred years of history anyway. Something ancient has fastened itself to you. It will not let you die. It calls itself the Aegis, and it is the reason you can do the impossible thing: when this world's story is finished, cross into another, and another, each meaner than the last, forever.

### Pillars

1. **Zero to hero, earned.** Every start is humble. Competence comes from doing; power comes from deeds. (KCD's gift)
2. **Classless.** Seven attributes, use-based skills, and the gear you can wield are your only identity. "Warrior" and "witch" are things other people call you afterward. (Elden Ring's gift)
3. **A procedural world that feels authored.** Generated history, factions, and NPCs; handwritten story structure and prose. Every quest traces to a fact that is true about this world. No visible mad-libs, ever.
4. **Death is a setback with teeth, never an ending.** Your gains are at risk; your character never is.
5. **A LOT to do.** Smithing, alchemy, hunting, burglary, gambling, carousing, trading, tournaments. Every activity feeds skill growth, money, reputation, or story, always at least two.
6. **No two playthroughs the same, ad infinitum.** Infinite NG+ into freshly generated, discretely harder worlds. Your finished characters become the mythology of future ones.

---

## 2. The World

### Presentation: layered maps
- **Overworld**: a walkable coarse map where one tile is a landmark or region. Horizon discovery ("what's over that ridge") is preserved; travel itself is play (wilderness activities, encounters, weather).
- **Local tactical maps**: dungeons, ruins, towns, ambush sites. Where combat and exploration-in-the-small live.
- **Prose scenes**: full-screen text for dialogue, events, and quest beats, with choice menus and visible skill checks.

### Worldgen: history first
At world creation, a causal-grammar history generator (the Caves of Qud model) produces the **world fact graph**: settlements, factions, notable NPCs, wars, grudges, shortages, ruins, and relics, all as ID-referenced facts with causes. This graph is the source of truth for everything: quests query it, dialogue cites it, prices derive from it, items are inscribed with it. Generated history the player cannot touch is wasted, so all of it is queryable in-fiction: books (if you can read), bards, gravestones, rumor, and the ask-about system.

Hierarchical seeds (master seed hashed per subsystem/region/site) keep every world reproducible; saves are seed + delta.

### The living world: event-driven factions
Factions are small state vectors with causal transition rules on a coarse tick. Every faction move (seize a pass, raise tithes, erupt into feud) writes a fact into the graph AND ships with a narration hook: a rumor, a notice, refugees on the road, a price spike. If the player could not perceive it, it does not fire. Conflicts carry designed exit conditions; no eternal stalemates. Named leaders and lieutenants form a bounded Nemesis-style roster with memory. Reputation is Fame/Infamy per faction, tracked separately.

---

## 3. Your Character

### Creation: choosing your flavor of nothing
Pick a **race** (familiar anchors: human, dwarf, elf, orc-ish, plus originals TBD; each world regenerates their cultures, clans, and social standing, so a dwarf means something different in every world) and a **background** (seeds starting skills: the hunter reads trails, the apprentice reads books). Every origin starts near the bottom. Some starts are illiterate; literacy is a learnable skill, and books gate recipes, techniques, and history.

### Attributes: seven, bought with deeds
Might, Grace, Vigor, Wits, Mind, Will, Presence. Raised by spending **Essence** at rest points; Essence is earned from meaningful accomplishment (kills, quests, discoveries, feats). The Aegis gathers the essence of your deeds and reshapes you where it anchors. Soft caps everywhere: diminishing, never zero, returns. Mental (Mind/Will) and physical power sit on independent axes so hybrid builds compose freely.

### Skills: eighteen-ish, grown by use
Combat (Blades, Hafted, Polearms, Ranged, Brawling, Warding), Craft (Smithing, Alchemy, Cooking), Wilderness (Hunting, Survival, Athletics), Subterfuge (Stealth, Larceny), Social (Persuasion, Commerce), Mind (Lore, plus magic skills). Skills grow only through use, and every use costs something real: materials, durability, fatigue, time, risk. Grinding is welcome and diegetic (sparring partners, training dummies, dangerous hunting grounds) with diminishing returns; it is never an exploit, because free repeatable actions do not exist.

At skill thresholds you choose one of two or three **perks** (mutually exclusive, KCD-style). **Proficiencies** (discrete know-how: recipes, techniques, faction customs) come from books, mentors, and quests.

### Gear is the other half of your build
Weapons and armor print hard attribute/skill requirements, visible before you invest. Under-requirement use is penalized, not blocked: you can swing the too-big sword, badly, and the sword itself tells you what to become. Respec exists only as a rare "unbinding" of the Aegis, a handful per world, refreshed at each crossing.

---

## 4. Combat

Turn-based on the local tactical maps: a **telegraphed-intent grid**.

- **Intents**: enemies display their next action: attack arcs, windups, guards, repositioning. Your turn is the answer: step out, block, parry, trade, or punish the windup.
- **Commitment both ways**: your heavy attacks take a visible windup that enemies react to. Greed is a bet, exactly like a slow R2.
- **Stamina** fuels attack, parry, dodge, and spellcasting alike: one economy for every build.
- **Posture**: a second bar broken by pressure; breaking it opens ripostes.
- **Weapon movesets**: weapon families change your verbs, not just numbers. Spears brace and control reach; greatswords cleave arcs; daggers reward flanks.
- **Knowledge-sharpened telegraphs**: telegraph clarity scales with Wits, weapon familiarity, and bestiary knowledge of that enemy family. A first-met horror shows only "it is preparing something." Mastery is learnable, diegetic, banked, and carried into NG+, where new enemy families arrive unreadable again.
- **Pacing at epic length**: lethality runs both directions (trash dies in 2-3 turns and can still punish sloppiness), simple enemies have simple intents, and one-keypress overwhelm resolution handles fights far beneath you.

Formal 1v1 duels (judicial combat, arena bouts) may later reuse these verbs as set-pieces.

---

## 5. Magic

Rare, feared, revered. Spells are found, never picked from menus: grimoire pages (literacy required), mentors, shrine rituals tied to world facts. Attunement capacity comes from found world objects, so hybrids are never double-taxed on attributes. Mind scales potency; Will scales control. Casting draws the shared stamina pool; strong spells consume components; overreaching Will risks miscast. Casts are telegraphed windups on the grid, interruptible both ways, for you and against you. Casters accrue social texture: awe, suspicion, and faction attention.

---

## 6. The Life: Activities and Economy

Four activity families, all first-class: **crafting trades** (smithing, alchemy, cooking), **wilderness living** (hunting, tracking, foraging, fishing, camping), **crime** (lockpicking, pickpocketing, burglary, fencing), **town life** (gambling, carousing, tournaments, trading, property). The discipline: every activity feeds at least two of skill growth, money, world-state/reputation, story hooks, or it does not ship. Activities hook into generated facts: the apothecary pays well for herbs because her supplier died in the raid the fact graph remembers.

Coin is fully separate from Essence and must matter all game:
- **Auto-scaling sinks**: upkeep and repair cost a percentage of your own gear's value, so wealth taxes itself.
- **An aspirational ladder**: property, retinue, master training, masterwork commissions; the next rung always priced above your means.
- **Crafting has its own lane**: consumables, augments, regional specialties; it never races loot for best-in-slot.
- **Prices derive from facts**: war makes grain dear; blockades make smuggling pay. Caravan investment exists as productive capital.

---

## 7. Companions

Three niches, no permanent party; combat is balanced around one character:
- **A summon slot**: one autonomous Spirit-Ash-style ally, resource-gated against your own pool. Conjurer builds, zero management.
- **Guest companions**: story-scoped, role-cast from world NPCs, a command verb or two, and *they can permanently die*. Guests carry the mortal stakes you cannot.
- **A pack animal or mount**: logistics and warmth.

The persistent companion voice is the Aegis itself.

---

## 8. Death and the Aegis

The Aegis is a bound intelligence, ancient and sentient, fastened to you. Its nature is the game's central mystery. It speaks: at deaths, at discoveries, at crossings. It is the only continuous character across your entire journey.

When you fall:
- **Banked, untouchable**: stats, skills, owned gear, quests, relationships, recipes, bestiary knowledge.
- **Dropped where you fell**: unspent coin, unbanked expedition loot, unspent Essence. One recovery attempt; a second death forfeits it. Carrying too much value is a choice, and greed is what kills you.
- **Wounded**: a temporary, visible, debt-framed debuff. Never a power loss; time and money, not spirals.
- **Death's Toll**: a visible meter that fills on death and drains over time. Routine deaths never scar. Clustered or boss-tier deaths risk a scar: a lost eye, a crushed hand, a haunted look. A handful per playthrough at most, each with a costly path back to parity, each a dialogue hook. NPCs who watch you fall remember.

Saves are part of the design: autosave fires at the instant of death, before the penalty screen, carrying all consequence state. Manual saves exist for life reasons; an optional Ironman toggle enforces the stakes architecturally.

---

## 9. Story

### The engine (five layers)
1. **World fact graph**: the source of truth, generated at worldgen, grown by play.
2. **Storylets**: every quest beat, scene, and event is an atomic, precondition-gated, handwritten unit. Content scales additively forever, which infinite NG+ requires.
3. **Role-casting**: pivotal scenes are written against role slots ("an NPC who owes you a debt") and cast from whoever exists in this world.
4. **NPC memory**: notable NPCs keep structured logs of what passed between you (favors, betrayals, witnessed deaths); dialogue is memory-driven selection over large authored line banks. Never open generation.
5. **Pacing director**: decides when content surfaces, keeping tension in a band. Hostility tiers tune it.

Two iron rules: no quest generates unless it traces to an existing fact, and important facts appear on multiple surfaces (quest, rumor, inscription) so the world reads as knowing things. Filler jobs stay honest ambient texture.

### The spine (two layers)
Per world, the main quest is drawn from a growing pool of authored **world-story templates** (the usurped throne, the creeping blight, the war of faiths): structure and beats handwritten, cast and geography filled from the fact graph. A beeline takes 10-15 hours; the world holds 30+.

Above all worlds runs the **Aegis arc**: the mystery of the thing that carries you, advancing at world completions and crossings, unfolding over the first several cycles before resolving into an earned steady state. Canon: The Ledger (D-026), fully specified in `story/aegis-arc.md`; template contract and launch templates in `story/world-story-templates.md`.

### Dialogue
Dramatic scenes use choice menus with visible skill checks. Ordinary NPCs also expose **ask-about** topics drawn live from the fact graph: the mechanism that makes generated history touchable.

### Determinism
No LLM at runtime, ever: the game is deterministic, testable, offline, and seed-reproducible. AI is an authoring-time tool for mass-producing storylet prose and line banks into the deterministic format.

---

## 10. The Endless Journey (NG+)

Finish a world's story and the Aegis offers the crossing. Three buckets:
- **Character: full carry.** Stats, skills, gear, recipes, lore, bestiary knowledge.
- **World: always fresh.** A new seed, new map, new factions, new history. NG+4 is not the same castle with fatter knights; it is a place no one has seen.
- **Legend: the bounded meta-layer.** Titles, the Hall of Legends, small boons on a diminishing-returns curve. The first hour of each new world feels like a visible step up; it never trivializes one.

Coin does not cross. In your final days, wealth converts through **patronage**: endow a guild, raise a monument, fund a shrine, commission your statue. Patronage becomes Legend credit and candidate facts for future worldgen.

Difficulty comes from two dials:
- **World Hostility Tier** (the NG+ number): a generation input, never a bolt-on multiplier. Each rung adds new enemy families, hazards, hostile-faction facts, scarcer havens, and only a modest stat bump. Every tier is backed by designed content.
- **Covenants**: optional stackable modifiers chosen at world creation (harsher winters, vengeful factions, shrine-only anchoring), totaling a visible Threat score whose rewards are legend and cosmetic, never raw power.

Death scales in magnitude, never in shape: tighter reclaim windows, longer wounds, a faster-filling Toll, failures that echo further.

And the loop closes on itself: **your finished characters enter the mythology.** Any future world may generate their statues, ballads, tombs (holding their actual gear), and cults. Multiple characters are not parallel saves; they are one mythology, written by you.

---

## 11. Technology

- **C# / .NET**, NativeAOT single-file publish. TUI via a lean custom render layer (or Terminal.Gui v2); the hard 80% is the deterministic simulation core, not the rendering.
- **Save architecture**: versioned seed contract for regenerable content, delta journal for authored/player-mutated state.
- **RNG**: hierarchical seed tree (master seed hashed with stable subsystem/region/site identifiers); subsystems never share a stream.
- **Content**: storylets and line banks; format v1 spec'd in `storylets.md` (C# catalog now, designed to map 1:1 onto data files when volume demands).

## 12. Open Items

- Final race list (which originals join the anchors)
- Spell list and magic-school content design
- Storylet external data-file format and condition/effect vocabulary (v1 C# format spec'd; see `storylets.md` sec. 6)
- Covenant, scar, Legend-boon, and hostility-tier content catalogs (first tier band landed in D-033: the barrow at tier 2+; tiers 3+ still need their own)
- Story content open items: final names for the arc's entities, bottle-episode playability, threshold-scene dressing, templates 4-5 (see `story/aegis-arc.md` sec. 11, `story/world-story-templates.md` sec. 9)

## 13. Document Map

- `decisions.md`: the decision audit trail with rationale (41 and counting)
- `storylets.md`: storylet format and fact-graph schema spec (D-030)
- `story/aegis-arc.md`: the trans-world Aegis arc spec (Canon: The Ledger; D-026)
- `story/world-story-templates.md`: the world-story template contract, iron rules, and three launch templates
- `../research/00-overview.md`: research synthesis; `01`-`06`: narrative, progression, death, NG+, tech, quests; `07`-`10`: magic, factions, companions, economy
- `../research/11-ffxiv-storytelling/`: FFXIV storytelling study; `00-synthesis.md` is the playbook
- `../_external_resources/ffxiv_good_story/`: FFXIV story summaries (user-provided reference)
