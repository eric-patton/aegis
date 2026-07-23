# AEGIS: Design Vision

*A terminal RPG about starting from nothing, dying without ending, and outliving worlds.*

This document is the synthesized design. The audit trail with rationale and rejected alternatives lives in `decisions.md` (D-001 through D-174); the research behind it lives in `../research/`.

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

### Weather and seasons
Every world opens in autumn under one shared seasonal calendar, with winter beginning on
the existing seed-drawn hard-winter tick and later seasons advancing every three coarse
ticks. Lowlands, road, high fells, and the D-165/D-174 Salt Fen express that calendar through
independent deterministic weather hands using four readable families: Calm, Wet, Wind,
and Cold. Weather's direct
weight stays on exposed travel and camping, never hidden combat or resource rolls. One-tick
forecasts at the sidebar and travel thresholds make timing the counterplay; roofs, supper,
waystones, and the great pelt are the answers already in the world. The stead event deck
reads the calendar too, so explicit seasonal facts can move stores, prices, and local deals
without turning every ordinary weather card into an economy modifier (D-158, built and
Verified D-167). The opening autumn holds its first card through any seed-drawn lead beyond
the regular three ticks, then walks the full hand into winter, preserving both the older
hard-winter arrival and the shared cadence.

### Worldgen: history first
At world creation, a causal-grammar history generator (the Caves of Qud model) produces the **world fact graph**: settlements, factions, notable NPCs, wars, grudges, shortages, ruins, and relics, all as ID-referenced facts with causes. This graph is the source of truth for everything: quests query it, dialogue cites it, prices derive from it, items are inscribed with it. Generated history the player cannot touch is wasted, so all of it is queryable in-fiction: books (if you can read), bards, gravestones, rumor, and the ask-about system.

Hierarchical seeds (master seed hashed per subsystem/region/site) keep every world
reproducible; saves are seed plus journal. D-174 builds D-165's generator 1 freeze per
campaign in the v99 save header. A supported old campaign keeps its recorded generator through later
crossings, while new campaigns use the newest supported one; a future build retains the
old entry point or rejects it explicitly, never silently redealing an old journal.

The launch world has four named countries under D-165/D-174. The Salt Fen is the fourth:
a bounded causeway country off the town end of the east road, with its own hamlet and
climate band, firm banks and reeds around impassable bog and water, exactly four regional
sites, three finite salt pans, and one ordinary fen-adder family. Its salters' compact is
an institution in facts and schedule rather than a new reputation scalar. Salt work feeds
Survival and the existing caravan good, while either equal-tier local conclusion schedules
one capped peddler restock. Further regions remain post-1.0.

Fact-driven prose follows the same deterministic discipline. D-159 defines, and D-168 builds,
enumerable narrative surfaces, fact-keyed authored variant bundles, validated contexts, and
stable selection derived outside every gameplay RNG. WorldEval curates the resulting fact
details, storylets, scenes, and ask-about topics with metadata that distinguishes intentional
fixed prose from failed variation. Unvisited gated topics enter through the catalog audit, not
pilot luck. No runtime text generation or recursive grammar enters the game.

### The living world: event-driven factions
Factions are small state vectors with causal transition rules on a coarse tick. Every faction move (seize a pass, raise tithes, erupt into feud) writes a fact into the graph AND ships with a narration hook: a rumor, a notice, refugees on the road, a price spike. If the player could not perceive it, it does not fire. Conflicts carry designed exit conditions; no eternal stalemates. Named leaders and lieutenants form a bounded Nemesis-style roster with memory. Reputation is Fame/Infamy per faction, tracked separately. The first rung landed in D-076: the home stead keeps a per-world **regard** for the bearer (local Fame), earned only by deeds it can perceive (the raids ended, the mound gone quiet) and reset at each crossing, set beside Legend's cross-world standing rather than merged into it. D-077 gave that regard its first boon, the **friend's welcome**: when a stead first holds the bearer a friend, its folk gift a coin purse, deed-earned so (unlike the Legend welcome) the hushed name never silences it. D-078 made the ledger keyed and gave it a second faction: the raiders keep a **wrath** on the bearer, one notch per raider slain on its own faster ladder, and past the dread rung their blows come feared and land the weaker; emptying the camp raises the stead's regard and the raiders' wrath in the same strokes, the first faction relationship live. And the coarse tick began in D-079: **the raids are real**. While a camp stands uncleared, the raiders come down on the stead every 160 turns, each raid writing a fact, narrated as it lands, and pricing bread a coin dearer for the rest of the world; clearing the camp is the designed exit, but taken grain stays taken. The first faction event that is not the bearer's own deed, and the tick machinery every later one reuses. D-080 closed the loop into a small economy: at the friend rung the steadholder takes a standing coin off bread (the **friend's price**, deed-earned, so the hushed name silences the Legend-bought hearth-price and not this one), and the raids topic counts the raids back in talk, so a raid prices bread up, the deed that ends the raids prices it down, and the stead says so. D-085 wrote the rungs into the fact graph itself (known, friend, the stead's own), so any storylet, topic, or template can gate on reputation with one declarative pattern; the first such content is the **friend's hearthtale**, the story a stead tells only to those inside its fence. D-086 opened the Infamy half at home with the first transgression verb: **pilfering**. The grab key beside a house takes a ration's worth and starts the stead's **shame** ladder, one rung per door robbed (watched, unwelcome, named a thief), each rung closing something in its own currency (the hearthtale's telling, the friend's price and purse, finally the larder itself), while the regard stands untouched beside it: a bearer can be a friend to the stead and watched in it at once, and both titles show. Restitution is the designed exit, coin left twice-over on the robbed sill, walking the ladder back down door by door. And D-087 paid the bright ladder's top rung: to **the stead's own**, every lesson the stead sells is shown freely, the boon paid in the one currency in the stead's gift that crosses the arch, closed by suspicion and reopened by restitution. D-088 gave the faction facts their consumers, three storylets that make the graph's writes matter after the hour they land: the named thief is told so to their face (suspicion acting beyond commerce, stilled by restitution), the hearthtale's rumor changes how the lane reads, and the stead's own are shown the deep cellar where its living waits out the worst night, the graph's first `secret`. Every rung on both ladders now pays or costs and is answered in content. And D-089 delivered this section's opening clause in earnest: the factions carry **state vectors on the coarse tick**. The stead's stores stand behind bread's price, drain under raids, bare out into the raids' own dark exit, and mend a measure per tick once the camp falls, so ending a conflict has a visible aftermath instead of a frozen price; the dens' boldness is derived from causes (plunder emboldens toward greedy double-raids, raiders slain cow a tick to nothing), making the cull wrath's first faction-scale consequence. Every transition is narrated as it lands and written to the graph. And D-105 gave the home faction its own moves on that tick: a raid come greedy posts **the watch**, which turns the raiding nights away at a measure of upkeep from the very lofts it guards (protection now, hunger later, and left standing it can bare the stead itself), and the last measure calls **the levy**, closing the larder and taking the bearer's coin against carted grain instead, a perceivable deed the regard counts. And D-106 stood up the third faction: **the long mound's unquiet dead**, giving the relation matrix its second edge, one of fear rather than war. Grave-goods carried out while the dead still walk start the mound's **grudge**: riled wights strike the harder, the mound raises its own slain again on the tick, and the stead speaks of the taller lights at its doors, while the stilling, already the regard's deed, settles the grudge outright: the one ledger whose designed exit is completion rather than payment. And D-109 closed the graph's oldest open loop: every produced fact now has its reader. The debt made right is marked at the bearer's face once every sill and every hand stands paid (both confrontation roads, the reckoning's and the caught hand's, ending at the same document, and writing `made_right` for content that remembers); the cellar secret matters in a raid's morning, read from inside the count; and the lifted purse's secret collides with trust when the fence opens to a hand that has been inside it unseen. And D-110 began this section's roster clause: **the named of the dens**. Every camp's seed names a chief and two lieutenants, the rumor carries the chief's name from the first morning, rank is worn as hide, and the memory rides the replay: a named raider bloodied and left alive keeps the scar, a chief slain over a standing lieutenant hands the camp on with the grudge in the office, and the hand that authors the bearer's death keeps the boast; every grudge arms the hand a point and is spoken to the bearer's face at the next descent. And D-111 gave the roster its stead-side readers and closed this section's no-eternal-stalemates clause: the raids topic reads the dens' order live (the risen chief named at the doors, the fallen-silent camp read as leaderless), the kept boast comes home laughed off at the well (den-talk is not believed, so the stead's epistemology holds and only the Aegis knows the joke's other half), and the exit-conditions audit confirmed every live conflict holds its designed exit, the crossing reset standing as doctrine backstop. D-173 builds the launch Stead-to-Town edge: a bonded Crofter's mortal road and delayed guild cart answer an active levy through Stores, Regard, and narrated facts, while the raids topic reads the watch and levy aloud. Positive Town Fame, raider-to-mound interaction, further desecration, and deeper transgressions remain later growth.

---

## 3. Your Character

### Creation: choosing your flavor of nothing
Pick a **folk** and a **background** (seeds starting skills: the hunter reads trails, the apprentice reads books). Every origin starts near the bottom. Some starts are illiterate; literacy is a learnable skill, and books gate recipes, techniques, and history.

Stage 1 shipped at D-092 as **the asking**: no creation screen, the Aegis takes the bearer's measure at the first wake, one journaled question at a time. The folk went original and world-grown at the user's direction, superseding this section's earlier familiar-anchors sketch (dwarf/elf/orc-ish) while keeping its structure: five fixed anchors (Steadfolk, Emberwrought, Cairnborn, Heathborn, Wrightkin), each one attribute tilt plus one qualitative trait, cultures still to be regenerated per world. Seven pasts bank a skill's first level and one concrete extra each; up to two paired attribute swaps keep the start humble; one precious thing is soul-bound (a known word, fine arms, a craft kit, a heavy purse, or an unassuming thing whose story waits for stage 2); the name is typed in-fiction or drawn from the folk's stream, and the fate door rolls the whole bearer from the seed. Stage 2 shipped at D-093: a burden may be taken (an old wound, a hunted past, a marked face: live weights every world collects on) and buys a second precious thing; a vow (vengeance, finding, the road's end) gives the road something to answer, and it does; a remembered face waits to be half-seen in a stranger; and the unassuming thing's wager pays through the keeper of songs, or waits down the chain if it went unchosen. Full spec in `creation.md`.

### Attributes: seven, bought with deeds
Might, Grace, Vigor, Wits, Mind, Will, Presence. Raised by spending **Essence** at rest points; Essence is earned from meaningful accomplishment (kills, quests, discoveries, feats). The Aegis gathers the essence of your deeds and reshapes you where it anchors. Soft caps everywhere: diminishing, never zero, returns. Mental (Mind/Will) and physical power sit on independent axes so hybrid builds compose freely.

### Skills: eighteen-ish, grown by use
Combat (Blades, Hafted, Polearms, Ranged, Brawling, Warding), Craft (Smithing, Alchemy, Cooking), Wilderness (Hunting, Survival, Athletics), Subterfuge (Stealth, Larceny), Social (Persuasion, Commerce), Mind (Lore, plus magic skills). Skills grow only through use, and every use costs something real: materials, durability, fatigue, time, risk. Grinding is welcome and diegetic (sparring partners, training dummies, dangerous hunting grounds) with diminishing returns; it is never an exploit, because free repeatable actions do not exist.

D-171 closes the launch roster at eighteen under D-162. Alchemy grows through successful
self-brewing, Athletics through stamina-paid rushes under live pressure, Stealth through
two-turn quiet movement across a foe's ordinary notice band, and Larceny through clean
household crime and fenced lots. Sleight remains the separate hand skill for pockets and
locks. Five level-2 questions give each new ledger, plus Sleight, its first permanent
choice.

At skill thresholds you choose one of two or three **perks** (mutually exclusive, KCD-style; in the game's own register they are knacks, shipped at level 2 by D-046). **Proficiencies** (discrete know-how: recipes, techniques, faction customs) come from books, mentors, and quests. D-154 closes the first complete book-to-craft path: a Lore-2 smithing text teaches a permanent bloom-temper, then one tarn-iron bloom gives one eligible iron piece 10 more wear once at the town forge, feeding Smithing without increasing combat power.

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

Combat depth's first sweep shipped at D-094 through D-096: three stances on one key (flat 2-point trades on the blow given and taken, free on quiet ground, a turn under live steel); a signature verb per melee family (the hafted heave sunders a linden board for good and staggers wind-ups, a paid cut carries the feet off marked ground, bare knuckles shove a stride back, the spear's long thrust standing as its identity); and a second telegraphed move for four known kinds (the goblin's rallying cry, the wight's grave-chill, the thegn's measured cut whose mark lies to any read short of keen: the bestiary's tiers paying differently at last, and the hound-lunge that drags toward the pack). Weapon movesets grew real verbs, not just numbers. The second sweep shipped at D-125: every foe carries a posture bar beside its blood (a guard rocked by paid blows, the wall, the heave's weight, and above all a parried blow; broken at the brim into a two-turn stagger and a riposte through the open door), and parry arrived as its own key, the turn committed to the guard against a blow shown at your own ground, with the feint's lying mark unmeetable by construction so the keen read keeps its crown. The third closed the loop at D-126: the field reads the bearer back. The bearer carries the second bar too (Will's brim at last), rocked only by landed committed blows, with the pressure reading the footing and the held wind-up (a pressing or winding body is leaned on harder, a set guard shrugs); at the brim the arms refuse two turns while the feet keep working, and the thegn alone knows the door the break leaves. Commitment now runs both ways in full: every telegraph, stance, and wind-up on either side of the line is read by the other. The guard war got its coda at D-129: the shield-carl's board-check is the field's first pressure verb aimed at the bearer's guard and not the blood (thrown mass at the charge's tier, no dice, met cleanest by the parry it was built to duel), and the drilled thegn answers being parried with the bind, keeping half its force and shoving a point back through the crossed iron. Still open in this lane: parry- and stance-riding knacks, flanking proper, and more second moves.

D-172 builds D-163's launch closure. Exact opposite-cell flanking now works for bearer
and field, the existing family verbs stand as the complete launch movesets, the cry
composes with unaware bands without disturbing authored dormancy, a broken warder board
changes its phase, and the Severed carries a marked three-cell sweep. Five permanent
level-6 questions let every martial family choose between a stance rider and a parry or
counterplay rider without changing any earlier knack.

Formal 1v1 duels (judicial combat, arena bouts) may later reuse these verbs as set-pieces.

---

## 5. Magic

Rare, feared, revered. Spells are found, never picked from menus: grimoire pages (literacy required), mentors, shrine rituals tied to world facts. Attunement capacity comes from found world objects, so hybrids are never double-taxed on attributes. Mind scales potency; Will scales control. Casting draws the shared stamina pool; strong spells consume components; overreaching Will risks miscast. Casts are telegraphed windups on the grid, interruptible both ways, for you and against you. Casters accrue social texture: awe, suspicion, and faction attention.

Magic v1 shipped at D-091 in exactly the found-not-picked shape, with one amendment: casting spends its own small pool (Focus, from Will) rather than the shared stamina, so the caster and martial identities stay distinct. Four workings wait on graven stones, one at the deepest reach of each fighting deep site: the spark, the levin (the caster's own telegraphed wind-up, dodgeable by feet and breakable by a wound the Will fails to hold through), the ward, and the veilsight. Mind drives the weight, Will holds the pool and the grip, and Spellcraft grows only by workings that did work. Words are knowledge: they survive death and cross the waygate whole, and each world's unread stones regrow, so the deep sites carry a prize beyond coin and iron. Still to come from the sketch above: components, attunement objects, the social texture, enemy casters (Will's resist role), and the wider spell list.

The calling made five workings at D-099. D-172 builds D-163's next closure: the
rune-tongue is the first hostile caster, carrying one marked-ground word and one
following binding, both interruptible and both reduced through a visible deterministic
resistance from Will. The severing answers hostile words with posture and the mending
buys renewable but slow blood without treating a wound, bringing the catalog to seven.
Spellcraft gains level-2 and level-4 questions. Seven digits still hold the whole
catalog, so attunement, components, schools, alternative teachers, and systemic caster
reputation remain later growth rather than launch gates.

---

## 6. The Life: Activities and Economy

Four activity families, all first-class: **crafting trades** (smithing, alchemy, cooking), **wilderness living** (hunting, tracking, foraging, fishing, camping), **crime** (lockpicking, pickpocketing, burglary, fencing), **town life** (gambling, carousing, tournaments, trading, property). The discipline: every activity feeds at least two of skill growth, money, world-state/reputation, story hooks, or it does not ship. Activities hook into generated facts: the apothecary pays well for herbs because her supplier died in the raid the fact graph remembers. Alchemy opened at D-090 in exactly this shape: the foraged simples (D-074) steep into a hale-draught at the stillroom (D-081) or, once the stillcraft is taught, at any shrine rest in any world, giving the herb lane its first sink (sell the sprigs or drink them) and the deep sites their first carried remedy. Smithing gains the same sell-or-use shape in D-153/D-154: finite tarn-iron comes down from the fells, the town forge turns it into blooms, and a learned hand either tempers one carried piece for longer service or sells the bloom through the guild. Fishing closes another wilderness verb in D-166: a permanent line opens three finite reaches in each world's high fells, each exposed sitting feeds Survival and yields trout that can feed Cooking and the ration bag or go to the town provisioner for coin. The crime family opened at D-086 (pilfering, the shame ladder, restitution) and in earnest at D-107: pickpocketing on its own deliberate key, the Sleight skill carrying the odds, clean lifts writing the stead's first secret fact and caught lifts riding the same unified suspicion ladder, so crime feeds skill, coin, reputation, and story hooks at once, exactly the discipline above. The family closed on its named verbs by D-127: lockpicking at the deep coffers (D-122, the guilt-free outlet), fencing at the peddler's cart on the road (D-124, the buyer no stead counter could be), and burglary proper (D-127, the crossed sill: one Sleight roll for the whole entry, the biggest home take, the quietest crime or the loudest catch), the first activity family complete on everything D-006 named for it. D-128 gave the family its epilogue: every secret fact its deeds write is now read back on the lane (the bolted dark, the heirloom missed, the two ledgers), the stead pricing its own unease without ever learning whose hand made it. Town life opened at D-108 with the first-named verb, gambling: knucklebones at the skald's hearth, a committed stake, a face-up cast, one throw back as the real decision against a house that plays its odds plainly, and a per-world net the stead talks about when it runs steep either way, coin's first pure sink-or-swell that is not a shop. All four families now have ground broken; carousing, tournaments, and property wait on the aspirational-sink infrastructure above.

Coin is fully separate from Essence and must matter all game:
- **Auto-scaling sinks**: upkeep and repair cost a percentage of your own gear's value, so wealth taxes itself.
- **An aspirational ladder**: property, retinue, master training, masterwork commissions; the next rung always priced above your means.
- **Crafting has its own lane**: consumables, augments, regional specialties; it never races loot for best-in-slot.
- **Prices derive from facts**: war makes grain dear; blockades make smuggling pay. Caravan investment exists as productive capital.

---

## 7. Companions

Three niches, no permanent party; combat is balanced around one character:
- **A summon slot**: one autonomous Spirit-Ash-style ally, resource-gated against your own pool. Conjurer builds, zero management. **Shipped (D-099)**: the calling, a fifth word on the graven stones (the barrow leans toward it second), held rather than spent: 2 Focus stay bound while the called shade walks. The shade rides the guest engine whole in its own slot beside a mortal guest: full body, modest blow doubled on the uncanny kinds, refusing the severed (the laying stays the bearer's choice). Not mortal, on purpose: it unravels without weight (fall, release anywhere, the bearer's death, the waygate), the deliberate contrast with the guest's full-weight mortality.
- **Guest companions**: story-scoped, role-cast from world NPCs, a command verb or two, and *they can permanently die*. Guests carry the mortal stakes you cannot. **Shipped whole (D-097)**: the guest engine walks at your shoulder, holds ground or comes on one contextual key ('o'), fights to their own measure (competence read from who they are), takes real blows and body-blocks marked ground, is tended from your own satchel, and dies for real. The first arc is the huntsman's debt: the woodward walks once the stead has bled, until the camp breaks; loyalty beats bank from blood, care, firesides, and deeds; a death writes the grave and beloved facts, costs stead standing, empties the bench for the whole world, and is remembered aloud; the paid arc ends in a portfolio fact and the walk home.
- **A pack animal or mount**: logistics and warmth. **Shipped whole (D-100)**: three beasts on three roads, one at your side, a per-world stable holding the rest. The stead's mule is bought at the wood's-edge bench (friend-gated); the raiders' stolen courser is given over once the camp breaks; the wild fell pony is won with bread on the high ground. They walk the open land at your side (two strides to a key: grass for all, hills and forest for the courser), wait at site mouths, and their saddlebags bank coin against your fall (the courser's are a racer's tack, capped), at the price that a raid landing while you are below takes the beast whole. Mortal-nerved beasts bolt from the uncanny mouths and hand the bags back; only the fell pony stands that ground. All mortal, all world-bound.

D-173 builds the launch closure approved in D-164. Physical intents judge the nearest
visible body, following fellows step from marked ground while a held one keeps the ordered
risk, and a mortal guest safely blocks a shot line. A nonfighter Crofter supplies the
second guest arc through the grain road, unresolved guests receive a crossing farewell,
and the first completed and first beloved outcomes each earn one later Aegis remembrance
without giving strangers false knowledge. Beasts add one point of warmth to exposed camp
healing and receive one character beat per kind. None of this adds a second sheet or
another command.

The persistent companion voice is the Aegis itself.

---

## 8. Death and the Aegis

The Aegis is a bound intelligence, ancient and sentient, fastened to you. Its nature is the game's central mystery. It speaks: at deaths, at discoveries, at crossings. It is the only continuous character across your entire journey.

When you fall:
- **Banked, untouchable**: stats, skills, owned gear, quests, relationships, recipes, bestiary knowledge.
- **Dropped where you fell**: unspent coin, unbanked expedition loot, unspent Essence. One recovery attempt; a second death forfeits it. Carrying too much value is a choice, and greed is what kills you.
- **Wounded**: a temporary, visible, debt-framed debuff. Never a power loss; time and money, not spirals.
- **Death's Toll**: a visible meter that fills on death and drains over time. Routine deaths never scar. Clustered or boss-tier deaths risk a scar: a lost eye, a crushed hand, a haunted look. A handful per playthrough at most, each with a costly path back to parity, each a dialogue hook. NPCs who watch you fall remember. *Shipped whole (D-098, D-173): the deterministic ledger, the three scars matched to their deaths, each one's costly cure road (the stillroom's knife, the smith's brace, the songhall's laying), the stead noticing the marks, scar and scar-mended facts with consumers, a fitted brace that makes wielded parries cost one stamina after the hand is repaired, capped tier-scaled Toll fill, and scars plus brace beside burdens on the sheet. Further scars remain later catalog growth.*

Saves are part of the design: autosave fires at the instant of death, before the penalty
screen, carrying all consequence state. Manual saves exist for life reasons; an optional
Ironman toggle enforces the stakes architecturally. Product, save-format, and generator
versions are separate. The release candidate is product 1.0.0, save v99, and
campaign-pinned generator 1 (D-165, D-174).

---

## 9. Story

### The engine (five layers)
1. **World fact graph**: the source of truth, generated at worldgen, grown by play.
2. **Storylets**: every quest beat, scene, and event is an atomic, precondition-gated, handwritten unit. Content scales additively forever, which infinite NG+ requires.
3. **Role-casting**: pivotal scenes are written against role slots ("an NPC who owes you a debt") and cast from whoever exists in this world.
4. **NPC memory**: notable NPCs keep structured logs of what passed between you (favors, betrayals, witnessed deaths); dialogue is memory-driven selection over large authored line banks. Never open generation.
5. **Pacing director**: D-145's measured teller and D-160's bounded authority, built and
   Verified under D-169, shape only
   explicitly elastic random deck events. Press guarantees an eligible deal after three
   quiet tick nights; Space may suppress one successful opportunity in a hot episode.
   Scheduled futures, faction clocks, weather, durations, and player-triggered content
   remain protected.

Two iron rules: no quest generates unless it traces to an existing fact, and important facts appear on multiple surfaces (quest, rumor, inscription) so the world reads as knowing things. Filler jobs stay honest ambient texture.

The director uses no RNG of its own and never invents content. It consumes the deck's
ordinary cadence roll, keeps seasonal and state eligibility intact, and resets pressure
when an elastic event actually deals. Its audit lives in journey and diagnostic output;
ordinary play feels the resulting rhythm without exposing an editorial meter.

### The spine (two layers)
Per world, the main quest is drawn from a growing pool of authored **world-story templates** (the usurped throne, the creeping blight, the war of faiths): structure and beats handwritten, cast and geography filled from the fact graph. A beeline takes 10-15 hours; the world holds 30+. Six compile today at slice scale: the raided stead, the creeping blight, the usurped throne (cast on the dens' own seat with the named roster as its players, D-112), the war of faiths (cast by office on the valley's two institutions, D-116), the gold rush (the old quarry's kind lie, D-121), and the long siege (the fen-leaguer's grateful fear, D-130).

Above all worlds runs the **Aegis arc**: the mystery of the thing that carries you, advancing at world completions and crossings, unfolding over the first several cycles before resolving into an earned steady state. Canon: The Ledger (D-026), fully specified in `story/aegis-arc.md`; template contract and launch templates in `story/world-story-templates.md`.

### Dialogue
Dramatic scenes use choice menus with visible skill checks. Ordinary NPCs also expose **ask-about** topics drawn live from the fact graph: the mechanism that makes generated history touchable. *The scene layer shipped (D-117): storylets open modal dialogue trees through the journaled key path, checked choices show their odds before the player commits, and the shuttered window is the first; the catalog and the plot-beat conversions grow from here.*

### Determinism
No LLM at runtime, ever: the game is deterministic, testable, offline, and seed-reproducible. AI is an authoring-time tool for mass-producing storylet prose and line banks into the deterministic format.

---

## 10. The Endless Journey (NG+)

Finish a world's story and the Aegis offers the crossing. Three buckets:
- **Character: full carry.** Stats, skills, gear, recipes, lore, bestiary knowledge.
- **World: always fresh.** A new seed, new map, new factions, new history. NG+4 is not the same castle with fatter knights; it is a place no one has seen.
- **Legend: the bounded meta-layer.** Titles, the Hall of Legends, small boons on a diminishing-returns curve. The first hour of each new world feels like a visible step up; it never trivializes one. The first rungs landed in D-048: standing derives from Legend on a square curve (never spent, never drained), titles speak at the crossing, and the boons are hospitality (the welcome, the hearth-price, the menders' honor), never combat power; the Hall of Legends as a place is still to come.

Coin does not cross. In your final days, wealth converts through **patronage**: endow a guild, raise a monument, fund a shrine, commission your statue. Patronage becomes Legend credit and candidate facts for future worldgen.

Difficulty comes from two dials:
- **World Hostility Tier** (the NG+ number): a generation input, never a bolt-on multiplier. Each rung adds new enemy families, hazards, hostile-faction facts, scarcer havens, and only a modest stat bump. Every tier is backed by designed content.
- **Covenants**: optional stackable modifiers chosen at world creation (harsher winters, vengeful factions, shrine-only anchoring), totaling a visible Threat score whose rewards are legend and cosmetic, never raw power. In the game's own register they are oaths, sworn at the waygate as the terms of the crossing, and the Threat score is the burden; nine are live, with D-173 building the closed door and long count as the final two launch terms approved in D-164.

Death scales in magnitude, never in shape: tighter reclaim windows, longer wounds, a faster-filling Toll, failures that echo further.

And the loop closes on itself: **your finished characters enter the mythology.** Any future world may generate their statues, ballads, tombs (holding their actual gear), and cults. Multiple characters are not parallel saves; they are one mythology, written by you.

---

## 11. Technology

- **C# / .NET**, NativeAOT single-file publish. TUI via a lean custom render layer (or Terminal.Gui v2); the hard 80% is the deterministic simulation core, not the rendering.
- **Save architecture**: versioned seed and campaign-generator contract for regenerable content, append-only key journal for authored/player-mutated state.
- **RNG**: hierarchical seed tree (master seed hashed with stable subsystem/region/site identifiers); subsystems never share a stream.
- **Content**: storylets and line banks; format v1 spec'd in `storylets.md` (C# catalog now, designed to map 1:1 onto data files when volume demands).
- **1.0 release**: Windows x64, self-contained Native AOT zip, spoiler-free README,
  release notes, required notices, SHA-256 manifest, clean-extraction smokes, zero known
  blocker or major defects, and a fresh signed-off manual packaged campaign. Installers,
  telemetry, networking, automatic migration, code signing, and non-Windows packages are
  outside the launch contract. The automated candidate machinery ships under D-174;
  manual packaged signoff remains the final gate (D-165).

## 12. Open Items

- Final race list (which originals join the anchors)
- Spell list growth past the seven V1-07 workings, and whether any school shape ever forms
- Storylet external data-file format and condition/effect vocabulary (v1 C# format spec'd; see `storylets.md` sec. 6)
- Scar, Legend-rung (D-048 landed five, hospitality-boon shaped), further-oath (D-047 landed four), and hostility-tier content catalogs (first tier band landed in D-033: the barrow at tier 2+; tiers 3+ still need their own)
- Story content open items: bottle-episode playability, Unbinder guise tells, template 7+ (see `story/aegis-arc.md` sec. 11, `story/world-story-templates.md` sec. 11; final names settled by D-043; generated world, stead, and person naming rewoven in D-049 with worlds unique per character)

## 13. Document Map

- `roadmap.md`: the living feature tracker and roadmap (what is built, partial, left, open)
- `decisions.md`: the decision audit trail with rationale (172 and counting)
- `storylets.md`: storylet format and fact-graph schema spec (D-030)
- `story/aegis-arc.md`: the trans-world Aegis arc spec (Canon: The Ledger; D-026)
- `story/world-story-templates.md`: the world-story template contract, iron rules, and the template pool (six landed through D-130)
- `../research/00-overview.md`: research synthesis; `01`-`06`: narrative, progression, death, NG+, tech, quests; `07`-`10`: magic, factions, companions, economy
- `../research/11-ffxiv-storytelling/`: FFXIV storytelling study; `00-synthesis.md` is the playbook
- `../_external_resources/ffxiv_good_story/`: FFXIV story summaries (user-provided reference)
