# Kingdom Death: Monster's Settlement Phase, and "Home Base Between Excursions" Design

## Overview

Kingdom Death: Monster (KDM) is a cooperative board game (2015, revised as 1.5 in 2017 and 1.6 since) built around a three-phase loop: hunt a monster, fight it in a tactical showdown, then return home for a Settlement Phase where the survivors spend what they harvested to grow their settlement. One full loop is a "lantern year," and the default campaign, People of the Lantern, runs about 25 to 30 lantern years, ending in a finale boss fight ([Wikipedia](https://en.wikipedia.org/wiki/Kingdom_Death:_Monster), [People of the Lantern](https://kingdomdeath.fandom.com/wiki/People_of_the_Lantern)). Wikipedia's summary pegs a full campaign at roughly 60 to 180 hours of table time.

The reason KDM is relevant to Aegis is a structural inversion: in KDM the settlement is the persistent protagonist and the individual survivors are semi-expendable. Survivors die permanently and often, but the settlement's innovations, locations, gear, principles, and record sheet persist, so long-term progress lives in the home base, not in any one character. The Settlement Phase is the strategic layer where that persistence is built, and the campaign timeline is a visible, modifiable script of future events (nemesis attacks arriving on schedule, story beats at fixed years) that gives the whole campaign a sense of impending, plannable doom. That combination, "a camp you build up between fights, where scheduled and random things happen," is almost certainly the half-remembered system that prompted this research.

This file documents the campaign structure, the Settlement Phase mechanics in detail, the settlement event and timeline systems, the survivor attachment loop, why it works emotionally and where it fails, a short survey of comparable "home base between excursions" systems, and a concrete recommendation for what (and how little) of this Aegis should adopt.

Sourcing note: the primary rules references (the [Kingdom Death Fandom wiki](https://kingdomdeath.fandom.com/wiki/Settlement_Phase) and [kingdomdeath.wiki](https://kingdomdeath.wiki/wiki/Innovations)) blocked full-page fetches during this research, so details below are assembled from search extracts of those wikis, the [official FAQ](https://kingdomdeath.com/rules/faq), [BoardGameGeek rules threads](https://boardgamegeek.com/thread/1444003/how-does-the-innovation-deck-work), and secondary write-ups. Facts I could not confirm from at least one extract are explicitly marked as uncertain.

## Campaign structure: the three-phase loop across lantern years

Each lantern year cycles through three phases ([Wikipedia](https://en.wikipedia.org/wiki/Kingdom_Death:_Monster)):

1. **Hunt Phase.** Players choose a quarry monster and its level, then walk a linear track of face-down hunt event cards, resolving each: ambushes, strange encounters, resource finds, injuries before the fight even starts.
2. **Showdown Phase.** Tactical grid combat against the monster, driven by the monster's own AI deck and hit location deck rather than a game master. Deaths here are permanent.
3. **Settlement Phase.** The survivors return home. This is the strategic layer: spend harvested monster resources and "endeavors" to innovate, build, craft, breed, and prepare, then nominate the survivors who depart on the next hunt.

Some years there is no hunt at all: **nemesis monsters** (the Butcher, the King's Man, the Hand) arrive at the settlement on scheduled lantern years, skipping the Hunt Phase entirely and going straight to a showdown you cannot decline ([Wikipedia](https://en.wikipedia.org/wiki/Kingdom_Death:_Monster)). Per a 1.6 People of the Lantern reference sheet found in search extracts ([Scribd copy](https://www.scribd.com/document/835673287/1-6-People-of-the-Lantern-Half-Page-v2)), the schedule is approximately: Butcher level 1 at lantern year 4, King's Man level 1 at year 9, the Hand level 1 at year 13, the "Watched" story event at year 20 leading to the Watcher, and the Gold Smoke Knight finale at year 30, with higher-level repeats of each nemesis in the intervening years. (Exact years vary between the 1.0, 1.5, and 1.6 revisions; treat the specific numbers as approximate.) Losing the finale, or having the population hit zero, ends the campaign.

The strategic texture this creates: every Settlement Phase decision is made against a visible calendar of future threats. You know the King's Man is four years out and your best spearman just died, so this year's endeavors go into armor crafting and a birth roll rather than a luxury innovation. The settlement phase is not downtime; it is the planning layer that makes the fights mean something.

## Settlement Phase mechanics in detail

### Phase sequence and endeavors

The Settlement Phase runs as an ordered checklist. Per rules extracts ([NamuWiki rules page](https://en.namu.wiki/w/Kingdom%20Death:%20Monster/%EA%B7%9C%EC%B9%99), [Fandom Settlement Phase](https://kingdomdeath.fandom.com/wiki/Settlement_Phase)), the early steps are: set up the settlement, returning survivors (each survivor who came back alive rejoins, sheds tokens and light injuries), gain endeavors, update the timeline (advance the lantern year and trigger anything printed on it), update the death count, and check milestones. Later steps cover the development activities below and finally nominating and equipping departing survivors; the official FAQ confirms gear grids reset at step 8 ([kingdomdeath.com/rules/faq](https://kingdomdeath.com/rules/faq)).

**Endeavors are the phase's action currency.** The settlement gains 1 endeavor per returning survivor (so a wipe or heavy losses starve the settlement of actions the very year it most needs them), plus occasional bonuses from innovations and principles ([Fandom: Endeavors](https://kingdomdeath.fandom.com/wiki/Endeavors)). Endeavors are spent on: innovating, building new locations, and the special one-off actions printed on innovation and location cards (augury at the Stone Circle, intimacy under Hovel, and so on). Unspent endeavors do not carry over to the next year, so every Settlement Phase is a small knapsack problem: too few actions, too many worthwhile uses. This scarcity, directly coupled to combat survival, is the single most elegant piece of the design.

### Innovations: a semi-random tech tree

Innovations are the settlement's cultural and technological advances: Language, Drums, Paint, Lantern Oven, Hovel, Inner Lantern, Symposium, and dozens more, loosely tagged into themes (science, faith, home, education, art, music). Mechanically ([Fandom: Innovations](https://kingdomdeath.fandom.com/wiki/Innovations), [Fandom: Innovation Deck](https://kingdomdeath.fandom.com/wiki/Innovation_Deck), [BGG rules thread](https://boardgamegeek.com/thread/1444003/how-does-the-innovation-deck-work)):

- The settlement has a personal **innovation deck** that starts small and grows over the campaign. Whenever the settlement gains an innovation, every innovation listed as a **consequence** of it is shuffled into the deck. Consequences are the prerequisite system: you cannot draw Symposium until Language is in play, because Symposium enters the deck as a consequence of Language.
- The **innovate** action costs 1 endeavor: draw from the innovation deck and add one drawn card to the settlement (the standard rule is draw two, keep one, returning the other; I could not re-verify the exact draw count against a primary source during this research, so treat "two" as probable rather than confirmed).
- Innovations grant persistent settlement-wide effects: unlocking survival actions (Language unlocks Encourage), raising the settlement's **survival limit** (the cap on the survival points any one survivor can bank), granting departing-survivor bonuses, and opening new endeavor options ([Fandom: Innovations](https://kingdomdeath.fandom.com/wiki/Innovations)).

The result is a tech tree that is directed but not deterministic: prerequisites shape what is possible, the draw decides what is offered, and the player decides what is taken. Two campaigns diverge culturally (a faith settlement versus a science settlement) without anyone choosing from a menu.

### Locations and gear crafting

Settlement **locations** (Bone Smith, Skinnery, Organ Grinder, Weapon Crafter, Leather Worker, Stone Circle, and more) are buildings constructed by spending an endeavor plus specific monster resources ([Fandom: Settlement Locations](https://kingdomdeath.fandom.com/wiki/Settlement_Locations)). Each location is a crafting menu: it lists the gear it can produce and the resource cost of each item (a Bone Smith turns bone resources into bone blades and darts; the Skinnery turns hides into leather armor). Monster resources are typed (bone, hide, organ) plus monster-specific rarities, so what you hunt determines what you can build, and wanting a specific armor set is a reason to hunt a specific monster at a specific level. Gear itself is settlement property, pooled and re-assigned to whoever departs next, which reinforces the settlement-as-protagonist frame.

### Population, intimacy, and naming

Population is a tracked number on the settlement sheet, and growing it is a survival necessity since deaths are constant. The **intimacy** endeavor nominates two survivors and rolls on a table; outcomes range from nothing, through complications that can kill a parent, up to a newborn survivor (or twins with the right innovations such as Hovel) ([Fandom: Intimacy story event](https://kingdomdeath.fandom.com/wiki/Intimacy_(Story_Event)); exact table entries vary by campaign and expansion). Newborns start as blank survivors, and players name every survivor by hand on a physical record sheet. Naming is mechanically almost free but is repeatedly cited in reviews and community writing as the single strongest attachment hook in the game: you grieve "Briar, who mastered the spear and survived three Butchers," not "survivor #14."

### Principles and milestones

**Principles** are permanent moral choices with mechanical consequences, presented as either/or story events ([Fandom: Principle](https://kingdomdeath.fandom.com/wiki/Principle)). The core four:

- **New Life** (triggered the first time a child is born): Protect the Young versus Survival of the Fittest (safer birth rolls versus harsher births but tougher newborns).
- **Death** (triggered the first time the death count is updated): Cannibalize versus Graves (eat the dead for resources versus mourn them for survivor bonuses).
- **Society** (triggered when population reaches 15): Collective Toil versus Accept Darkness.
- **Conviction** (triggered by a scheduled timeline entry mid-campaign): Barbaric versus Romantic (raw combat bonus versus growth through understanding). The exact trigger year for Conviction was not verifiable in extracts; the first three triggers are confirmed ([Fandom: Principle](https://kingdomdeath.fandom.com/wiki/Principle)).

The choices are permanent, settlement-defining, and tied to the settlement's story ("we ate our dead, and we were never haunted by them, but we never mourned either"). **Milestones** are the checkboxes on the settlement sheet that fire these principle events plus other story events when thresholds are crossed (first child, first death, population 15, an innovation-count threshold); survivors additionally have personal milestones (age, weapon mastery, courage and understanding thresholds) that fire personal story events.

## Settlement events: things happen between hunts

Each Settlement Phase, when the timeline is updated, the settlement draws one card from a shuffled **settlement event deck** (a 1.5-era addition; 1.0 had scheduled story events but no per-year random draw). Other effects can schedule additional specific settlement events, and the same event cannot fire twice in one phase ([Fandom: Settlement Event](https://kingdomdeath.fandom.com/wiki/Settlement_Event), [BGG: settlement event deck](https://boardgamegeek.com/thread/1895347/settlement-event-deck)).

The deck of roughly twenty core cards mixes disasters, boons, and strangeness: Murder (a survivor is found dead and the settlement must respond), Plague, Heat Wave, Acid Storm, Cracks in the Ground (which can swallow people and buildings), Haunted, Rivalry, Skull Eater, Weird Dream, Open Maw, Phantom, Clinging Mist, Dark Trader and Dark Dentist (sinister visitors offering bargains), Glossolalia, and Nickname (a survivor earns a nickname and a small bonus, a pure attachment card). Exact card text varies by printing; the list above is drawn from the wiki's card index ([Fandom: Settlement Event](https://kingdomdeath.fandom.com/wiki/Settlement_Event)) and community playthrough logs ([Exhausted Lantern Hoard campaign log](https://exhaustedlanternhoard.com/season-1/)).

Design-wise this is exactly "things happen at home while the game's attention was elsewhere": one card per year is a very low bookkeeping cost, but because the deck contains both a murdered survivor and a beloved nickname, every draw carries dread and the settlement accumulates texture that no player chose. The random draw layered on top of the scheduled timeline gives each year one authored beat and one unauthored one.

## The timeline: a visible, modifiable script of the future

The campaign timeline is a printed sheet of numbered lantern years, each optionally carrying story events and nemesis encounters. Its two load-bearing properties:

1. **Scheduled dread.** Players can read that the Butcher comes in year 4 and the King's Man in year 9 ([1.6 reference sheet](https://www.scribd.com/document/835673287/1-6-People-of-the-Lantern-Half-Page-v2)). Preparation becomes the game's real strategic verb: the fights test what the settlement phases built.
2. **The timeline is itself mutable state.** Story events, hunt events, and settlement events frequently instruct players to write new events into future years, move them, or cross them out. Choosing an expansion campaign rewrites whole stretches of the timeline before play begins ([Monster Nodes, official](https://shop.kingdomdeath.com/pages/nodes-in-monster-campaigns)). "In year 20 you are Watched; the Watcher follows" is written into the future by an earlier event, and the players can see it sitting there for years before it lands.

This is the loveliest single mechanism in the game for Aegis's purposes: the future is data, events edit that data, and the player is allowed to read (some of) it. Dread comes not from surprise but from a promise the world visibly intends to keep.

## The survivor attachment loop

Individual survivors accumulate an extraordinary amount of personal state: hunt XP and age milestones, weapon proficiency ranks (specialist at 3, master at 8), fighting arts (capped at three), disorders (mental scars, also capped at three), courage and understanding tracks with story events at their thresholds, permanent severe injuries (lost limbs, blindness, destroyed organs), and a name someone at the table chose. Then they die, permanently, often to a single bad hit location draw, and the death count goes up by one ([Wikipedia](https://en.wikipedia.org/wiki/Kingdom_Death:_Monster), [Know Direction overview](https://knowdirectionpodcast.com/2025/06/kingdomdeathmonster/)).

The inversion is that this loss is survivable *because the settlement is the character*. Innovations, locations, gear, principles, and the timeline all persist; a dead master spearman leaves behind the spear, the Weapon Crafter that made it, and possibly a child. The Know Direction write-up frames it exactly this way: the loss of a developed survivor is a significant setback, but survivors "leave legacies through innovations, crafted gear, or descendants." Standard RPGs bank progress in the character and treat the world as scenery; KDM banks progress in the place and treats characters as the renewable resource. Grief is designed into the loop rather than designed out of it, and the cap structure (three fighting arts, three disorders) keeps even a long-lived survivor legible on one sheet.

## Why it works emotionally, and the criticisms

What the reviews and community writing consistently credit:

- **Scarcity with teeth.** Endeavors scale with survivors who came home; resources come only from monsters you beat. Every good thing was paid for in risk, so every good thing is felt.
- **Permanence.** No rewinds: deaths, principle choices, and timeline entries are ink on paper. The record sheet accumulates crossed-out names.
- **Ritualized bookkeeping as diary.** The settlement sheet, hand-written names, the death count, and nickname events turn accounting into memorialization. The bookkeeping *is* the storytelling medium, the same way a save file never is.
- **Readable doom.** The timeline makes the future a promise, so mid-campaign years feel like preparation for something, not episodic filler.

The criticisms, equally consistent ([Wikipedia reception section](https://en.wikipedia.org/wiki/Kingdom_Death:_Monster), [Shut Up & Sit Down review](https://www.shutupandsitdown.com/videos/review-kingdom-death-monster/)):

- **Bookkeeping weight.** The same ritual that creates the diary is a real cost: sheets, decks, tokens, and rulebook page-flipping make a settlement phase take a long evening late in the campaign. A computer game gets this ritual nearly free, which is the biggest reason the pattern transfers well to Aegis.
- **Swinginess and luck-heavy death.** A maxed survivor can die to one unlucky AI or hit-location draw; a bad early intimacy or event roll can doom a campaign hours before the players know it. Defenders call this the point; detractors call it hours of investment burned by a d10.
- **Length and price.** 60 to 180 hours and a very high buy-in; campaigns are frequently abandoned mid-timeline.
- **Tone.** SUSD's review praises the campaign structure while criticizing the gratuitous pin-up miniature line surrounding it. Not mechanically relevant to Aegis; noted for completeness.

## Comparable "home base between excursions" systems

- **Darkest Dungeon, the Hamlet** ([wiki](https://darkestdungeon.fandom.com/wiki/Hamlet)). Buildings (Blacksmith, Guild, Abbey, Tavern, Sanitarium) are permanently upgraded with heirloom currencies looted from expeditions; heroes are a semi-expendable hired roster who queue for stress treatment between runs. What it adds over KDM: upgrades gate per-hero services (better gear ranks, cheaper treatment), making the base a multiplier on a rotating roster. What it lacks: almost nothing *happens* in the Hamlet uninvited (town events arrived only in DLC), so the base feels like a menu, not a place.
- **This War of Mine** ([Wikipedia](https://en.wikipedia.org/wiki/This_War_of_Mine)). Day phase at the shelter (build stoves, beds, radios; visitors knock asking for help), night phase scavenging. Its lesson is that scarcity plus morally coded home decisions (turn away the neighbor, or feed her from your last cans) produces KDM-grade attachment with zero randomness theater; its home is a survival machine, not a growing legacy.
- **Frostpunk** ([Wikipedia](https://en.wikipedia.org/wiki/Frostpunk)). The purest "city as protagonist" design, and its Book of Laws is the closest videogame analogue to KDM principles: permanent either/or social choices (child labor, radical treatment, faith versus order) with mechanical and narrative consequences. Also demonstrates scheduled dread done right: the great storm is announced long before it arrives.
- **X-COM (Enemy Unknown)** ([Wikipedia](https://en.wikipedia.org/wiki/XCOM:_Enemy_Unknown)). Base facilities plus a research tree are the innovation analogue (tech unlocked from harvested alien materials, prerequisites throughout), soldier permadeath with a memorial wall is the attachment analogue, and the escalating alien doom clock is the timeline analogue. Proof the whole KDM triangle (base, roster, schedule) works in a videogame loop.
- **Fire Emblem: Three Houses, Garreg Mach monastery** ([Wikipedia](https://en.wikipedia.org/wiki/Fire_Emblem:_Three_Houses)). A calendar plus a scarce weekly activity-point currency (an endeavor twin) spent on teaching, meals, and socializing. Its cautionary lesson: repeated mandatory hub chores on every cycle turn ritual into tedium; the endeavor idea sours when the base *demands* attention instead of rewarding it.
- **Suikoden series castles** ([Wikipedia](https://en.wikipedia.org/wiki/Suikoden)). The base grows by *recruiting people*, up to 108, each visibly moving in and adding a shop, service, or scene. Attachment through named residents rather than upgrade tiers: the base gets fuller, not just stronger. Directly relevant to named stead folk.
- **Assassin's Creed 2, Monteriggioni villa** ([wiki](https://assassinscreed.fandom.com/wiki/Monteriggioni)). Spend florins renovating shops and landmarks; the town visibly improves, shop prices drop, and an income chest fills. Beloved as an aspirational coin sink, criticized because the income loop eventually trivializes money. Lesson: investment should pay out in capability and texture, not compounding currency.
- **Hades, the House Contractor** ([wiki](https://hades.fandom.com/wiki/House_Contractor)). A roguelite hub where run currencies buy permanent cosmetic and functional home upgrades, and hub NPC dialogue advances between every run. Lesson: even tiny persistent home changes between excursions ("the lounge got a new rug, and Achilles noticed") carry outsized continuity feeling per unit of dev cost.

The comparative pattern: KDM is unique in combining all four legs (player-directed base investment, expendable named people, a random home-event stream, and a visible modifiable schedule of future threats). Every videogame above has two or three legs; the ones that feel most alive (Frostpunk, X-COM, Suikoden) are the ones where the base has both a growth arc and a threat calendar.

## Lessons for Aegis: should Aegis have this?

Aegis already has more of this than it may appear. The stead keeps per-world regard and a shame ladder, its economy reacts to raids (bread prices, the larder/stores vector), factions act on a coarse world tick while the player is below (raids land, the pack animal can be taken, the stead can post a watch or call a levy), and guest companions already die permanently. In KDM terms: Aegis has the settlement event *consequences*, the faction tick, and part of the attachment loop. What it does not yet have is the **player-directed investment arc** (nothing the player builds at the stead), the **storylet-shaped home event stream** (raids are simulation output, not authored beats), the **visible schedule of the future**, and **named stead folk** as attachment surface. Those are the four candidate additions, plus the crossing question.

### Option A: stead facilities as a coin/resource investment ladder

Player-directed building: spend coin (and perhaps hauled materials) on a short fixed list of named facilities with one to three tiers each. Candidates that plug directly into existing systems: a palisade (reduces raid damage to the larder vector), a smithy tier (unlocks repair or a craft recipe locally), a stillroom extension (better provisions), a stable upgrade (pack animal safety when a raid lands), a watchtower (improves the existing watch posting), a granary (raises larder cap or slows drain).

- *For:* This is the "A LOT to do" pillar and the roadmap's aspirational-coin-sink line in one feature; every tier has a legible effect on systems that already exist, so no new simulation is needed, only modifiers. Monteriggioni shows how loved this is; KDM's locations show it reads best when each building is a capability, not a stat stick.
- *Against:* Art/UI surface (the stead screen must show what is built), balance work so tiers matter without being mandatory, and the crossing question (Option E).
- *Verdict: adopt.* Small fixed list (5 to 8 facilities), tiers hand-authored, costs in coin plus at most one "notable material" per tier as a quest hook. Explicitly not a tech tree and not randomized: Aegis's legibility pillar favors a menu the player can read over KDM's deck draw. KDM's semi-random innovation deck is its most board-game-shaped mechanism, existing to create between-campaign variety at a physical table; Aegis gets variety from worldgen already.

### Option B: settlement events as a storylet deck on the coarse tick

A pool of authored stead storylets that the world tick can fire between the player's excursions, gated by stead state: regard band, shame rung, larder level, facilities built, season, raid pressure. Mixed valence like KDM's deck: a fire in the stillroom, a wedding, a feverish winter, a nickname the stead folk give the player (regard-flavored), a peddler with a strange offer, a quarrel between two named residents.

- *For:* This is precisely the half-remembered "things happen while you're away, you come home to news." It reuses the committed storylet engine and the existing tick; one event per tick window is KDM's proven low-bookkeeping rate. It converts the existing simulation (raids, prices) from silent state changes into narrated beats, which file 08's research says is where perceived aliveness actually comes from.
- *Against:* Pure content cost; needs a narration channel (news on return, a stead notice board, gate gossip).
- *Verdict: adopt.* This is the highest value-per-effort item on the list, and the KDM lesson to copy exactly is the *mixed deck*: boons and nicknames in the same pool as disasters, so returning home is hope plus dread rather than a damage report.

### Option C: a visible, modifiable schedule of future events

KDM's best mechanism, restated for Aegis: scheduled future events written into the fact graph as first-class facts ("in 12 ticks the hard winter comes," "the raiders' war band returns in the spring," "the levy musters at midsummer"), which storylets and world events can add, move, or cancel, and which the player can partially read through diegetic foreshadowing (an almanac, an elder's warning, a raider prisoner's taunt).

- *For:* Near-zero new architecture: the fact graph and tick already exist, and this is just facts with a due-tick that the pacing director consumes. It gives the stead loop what Frostpunk's storm and KDM's timeline prove out: preparation as a strategic verb, and dread from a promise rather than a surprise. It also gives Options A and B their point (build the palisade *because* the war band returns in spring).
- *Against:* Needs discipline about what is visible versus hidden, and cancellation paths must exist so schedules respond to player action (clear the camp, the spring raid entry is struck out; that strikethrough is itself a satisfying narration beat).
- *Verdict: adopt, strongly.* Recommend this become a general engine facility (scheduled facts with foreshadowing hooks), not a stead-only feature.

### Option D: named stead folk who can die

A small named roster (roughly 6 to 10: the miller, the smith, the alewife, an elder, a couple of children) with faces in the fact graph, referenced by storylets, and *killable* when raids land or events fire. Graves persist; regard interacts (the stead remembers who you saved, and who died while you were below).

- *For:* This is KDM's and Suikoden's real attachment engine: named people, not numbers. Aegis already accepts permadeath for guest companions, so the precedent and the emotional register exist; this extends it to people the player did not choose to risk, which is a different and sharper feeling (KDM's Murder card, not a battle loss). It also gives the shame ladder and regard system faces to speak through.
- *Against:* Content cost per named person (dialogue, storylet roles); risk of the RimWorld problem where deaths become noise if the roster churns too fast. Deaths must be rare, legible, and tied to failures the player could have prevented (no palisade, no watch posted), or they read as slot-machine cruelty, the exact KDM criticism to avoid.
- *Verdict: adopt in lite form.* No birth/genealogy simulation, no aging, no KDM population mechanics: a fixed cast per world, generated at worldgen with the rest of the stead, casualty-eligible only through a small set of authored event outcomes.

### Option E: what crosses at NG+

KDM's settlement is wiped when a campaign ends, which structurally mirrors Aegis's crossing into a fresh world. Options: (1) full reset, stead investment is per-world like regard; (2) full carry, the new world's stead starts upgraded; (3) split: physical facilities and named folk reset with the World bucket, while *knowledge* (recipes, techniques the player learned, i.e. the innovation-shaped part) rides the Character bucket, plus a small "legacy echo" storylet in the next world (a keepsake, a song about the old stead, a founder's bonus discount on the first facility tier).

- *Verdict: option 3.* Full carry would break the NG+ contract already locked in the overview (World bucket always regenerates) and would deflate the new world's early economy; full reset with no echo wastes an attachment payoff that KDM cannot offer but Aegis can (KDM campaigns end; Aegis characters continue). Facility *prices or first-tier access* as a modest crossing echo also gives the meta-progression bucket a warm, diegetic member.

### What to avoid

- **An endeavor-style second action currency.** KDM needs endeavors because a board game has no clock; Aegis has a real time/turn economy and coin. A stead-scoped currency would compete with coin and Essence and add exactly the bookkeeping weight KDM is criticized for. Facilities cost coin and materials; stead actions cost time. Three Houses' activity points show how an endeavor clone curdles into chores.
- **The semi-random innovation deck.** Charming at a table, illegible in a TUI menu. Fixed facility list, with at most a few tiers *unlocked by deeds* (clearing the raider camp unlocks palisade timber) to keep the directed-but-not-menu feeling.
- **Population simulation.** Births, intimacy tables, and genealogy are KDM's renewable-roster machinery; Aegis's protagonist is a persistent character, so this solves a problem Aegis does not have.
- **Principle-style permanent morality cards.** The shame ladder and regard already occupy this space organically; a separate either/or card system would be a redundant moral currency. If ever wanted, express it as rare stead-level storylet choices with permanent facts, not a subsystem.
- **Mandatory per-visit stead upkeep.** The stead should reward visits, never demand them; KDM's late-campaign settlement-phase slog and the monastery's weekly chores are the same failure from two directions.

### Recommended scope, in order

1. **Scheduled future facts with foreshadowing and cancellation** (Option C): engine-level, cheap, powers everything else.
2. **Stead event storylets on the tick** (Option B): the "come home to news" loop, reusing committed systems.
3. **Facility investment ladder, 5 to 8 facilities** (Option A): the coin sink, each tier a modifier on existing raid/larder/watch/stable systems.
4. **Named stead folk lite** (Option D): fixed per-world cast, rare authored-event mortality, graves persist.
5. **Crossing split** (Option E): facilities and folk reset with the world; recipes/knowledge carry; one small legacy echo.

Items 1 and 2 are the minimal version and stand alone; 3 through 5 layer on cleanly. The KDM sentence worth pinning above the whole feature: the player should leave for a dungeon *worried about home for reasons they can name, and act on*. That is the entire emotional payload of the Settlement Phase, and Aegis can deliver it with a schedule, a storylet deck, and a palisade line-item, no d10 tables required.
