# Companions and Hirelings in a Solo-Centric RPG

## Overview

Aegis is built around a single character in deep 1-v-many intent-telegraph combat: the Souls "lone adventurer against the world" fantasy, but with a turn-based tactical layer that assumes the player is reading every enemy's wind-up and choosing a response. That premise sets a hard constraint before any companion design starts: a companion cannot become a second unit the player must also command turn-by-turn, or the combat stops being about reading the world and starts being about administering a squad.

The research below surveys how other games drew this line, where they drew it differently, and what broke when they got it wrong. The throughline that emerges:

- Games that kept the lone-hero fantasy intact treat companions as one of three things: a single, low-bandwidth combat presence with almost no UI of its own; a narrative fixture whose value is memory and story rather than damage output; or a temporary, quest-scoped guest who leaves before they become a management burden.
- Games that let companion count or companion gear scale up (Diablo 2's late-game mercenary meta, Darkest Dungeon, Battle Brothers) necessarily become party-management games. That is a different genre contract than the one Aegis has already signed, and none of its locked decisions ask for it.
- The strongest precedent for "companions matter without becoming a second character sheet" is to decouple the *relationship* (which can be deep, permanent, and narratively rich) from the *combat unit* (which should stay thin, disposable, and optional).

## How Notable Games Solved This

### Elden Ring: Spirit Ashes and NPC summons (companionship without party management)

FromSoftware's answer to "the player should sometimes not be alone" is deliberately the thinnest possible layer:

- Spirit Ashes are summoned with the Spirit Calling Bell and cost FP, a resource that does not regenerate passively, so it competes directly with the player's own spells and incantations rather than being free.
- They can only be triggered at specific points (near a Rebirth Monument), not anywhere in the world.
- Only one Ash Spirit can be active at a time: summoning a second automatically ends the first, so there is never a "party" to think about.
- There is no positioning order, no AI behavior selection, and no inventory for the summon. It fights autonomously on a fixed, non-configurable move-set, and if it dies, it is simply gone until the player can afford to resummon it.

(GamesRadar, Den of Geek, Fextralife wiki: https://eldenring.wiki.fextralife.com/Spirit+Ashes, https://www.gamesradar.com/elden-ring-summons-ash-spirit-npcs/, https://www.denofgeek.com/games/elden-ring-spirit-ashes-summon-npc/)

Crucially, summoning a Spirit Ash does not scale up boss HP or damage the way it would in a co-op game, so the summon is a pure difficulty softener, not a "build around your ally" system (RPG Site: https://www.rpgsite.net/feature/12440-elden-ring-spirit-ashes-summons-how-to-use-and-upgrade-your-npc-allies). The design intent is legible: give the player a resource-gated crutch for hard fights without ever asking them to think about a second character's kit.

Separately, Elden Ring's *named* NPC summons (Blaidd, Alexander, Millicent at specific bosses) are narrative rather than mechanical: they exist to make a boss fight feel like the climax of that NPC's personal questline, they have no inventory or growth the player manages, and they are gone (dead, moved on, or storyline-locked) as soon as their arc resolves. That is a second, non-combat pattern worth separating from Ashes: a companion who shows up for exactly one fight because the *story*, not the player's build, put them there.

### Kingdom Come: Deliverance: Mutt (companion without a command menu)

Mutt (added with the "A Woman's Lot" DLC via the Houndmaster perk line) is the clearest model for "a companion whose main value is not combat":

- Mutt takes a small set of contextual commands (Sic to occupy an enemy in a 1v1, Search to sniff out loot or a hidden path, Guard/Follow), each one keypress on whatever the player is currently hovering over, not a menu of orders (Escorenews guide: https://escorenews.com/en/article/66235-complete-guide-to-mutt-tips-commands-perk-and-skill-build-secret-dog-interactions-with-in-kcd2).
- He is not built to solo fights or tank damage. "Sic" briefly distracts one enemy so Henry gets an opening: a tactical nudge, not a second combatant.
- His narrative hook (formerly the Skalitz butcher's dog, orphaned by the same massacre that orphans Henry) is delivered almost entirely through incidental barks and behavior rather than dialogue trees.

This is a template for a low-UI, high-flavor animal companion: no inventory, no death spiral to manage, one verb that matters in combat, and a backstory that reinforces the world's fiction for free.

### Skyrim/Fallout: single-follower systems (pack mule + flavor vs. real tactical presence)

Bethesda's follower model caps the party at one (followers do not stack), and in practice players gravitate to two uses that have nothing to do with tactics:

- **Pack mule.** Unlimited inventory overflow is explicitly discussed in the community as a primary use case for Lydia and other followers (Fanpop poll, Nexus "Mule in Skyrim" mod: https://www.fanpop.com/clubs/elder-scrolls-v-skyrim/picks/results/896104/use-lydia-solely-pack-mule, https://www.nexusmods.com/skyrim/mods/59612).
- **Ambient flavor.** A small amount of banter and companion-specific quests, layered on top of a generic combat AI (Pocket Tactics follower guide: https://www.pockettactics.com/skyrim/followers).

Followers are essentially unkillable in vanilla Skyrim (they go unconscious and recover), which removes any stakes from bringing them into a fight, and their combat AI is generic enough that most players describe them as "in the way" as often as helpful.

Fallout 4 refined the model with a companion-perk system that decouples relationship from combat presence:

- Each companion has a hidden 0-1000 affinity meter.
- Maxing it unlocks a *permanent* passive perk the player keeps even after dismissing them (Killshot from Cait, Automatic Weapon damage from MacCready, and so on) (TheGamer companion perk guide: https://www.thegamer.com/fallout-4-companion-perk-guide/).

The companion's *presence in the field* is nearly irrelevant, but the *relationship built with them* pays out a permanent, build-defining reward. That is a good precedent for turning companionship into a permanent character-building resource without ever needing the companion to be a good combatant.

### Darkest Dungeon and Battle Brothers: what full parties cost

These are the cautionary comparison, not a pattern to adopt, but worth stating precisely because Aegis has already ruled out this shape.

Darkest Dungeon's design postmortem (GDC Vault: https://gdcvault.com/play/1023089/Darkest-Dungeon-A-Design) frames the game as deliberately built around party composition, positioning, and attrition:

- Four heroes, each with a position-locked skill list.
- Camp management and a stress/affliction system layered on top of raw HP, so every fight is really a management problem across four characters' resources simultaneously.
- Permadeath is real (a dead hero leaves only a tombstone in the Hamlet cemetery), which is the entire emotional engine of the game, but it only works *because* the roster is large and disposable: heroes are explicitly designed to be replaceable line items recruited from a stagecoach, not the singular protagonist.

Battle Brothers pushes the same shape further with 12-person companies, per-character gear, injuries, and permadeath across a whole mercenary band.

The UI and cognitive cost this imposes is the actual lesson for Aegis. Four to twelve independently-equipped, independently-leveled units each need their own inventory pane, skill list, position/formation state, and status-effect readout, and the player is expected to context-switch between all of them every combat round. That is a fundamentally different UI and pacing budget than a single-character intent-telegraph combat game can afford, doubly so in a terminal UI where screen real estate for status readouts is already scarce. Any Aegis companion whose kit needs its own equipment slots, skill tree, or turn-order entry is importing this cost wholesale, one companion at a time.

### Diablo 2 mercenaries and Diablo 3 followers: hireling-as-gear-sink

Diablo 2's Act mercenaries (Rogue archer, Desert Guard spearman, Iron Wolf sorcerer, Barbarian) started as a throwaway feature and were substantially reworked in the Lord of Destruction expansion:

- In original Diablo 2, a dead hireling was gone forever, which the community regarded as making them barely worth using.
- Lord of Destruction let mercenaries carry across Acts, gave them an equipment screen, and let a dead one be resurrected for a gold fee with level and gear intact (Diablo Wiki, Maxroll: https://diablo.fandom.com/wiki/Mercenaries, https://maxroll.gg/d2/resources/mercenary).
- Mechanically they have no Vitality/Mana/Stamina and gain no benefit from stats that key off those pools, only from flat skill/damage bonuses and a single class-restricted weapon slot, which keeps their build space deliberately small.

Diablo 3 went further in Patch 2.7: followers got a nearly full 13-slot equipment set plus an "Emanate" mechanic that lets the player farm build-enabling legendary effects onto a follower and receive the effect themselves (Maxroll follower guide: https://maxroll.gg/d3/resources/follower-mechanics). This was explicitly aimed at strengthening solo play without adding a second controlled unit.

The pattern across both games: a hireling is a *loadout target* (a place to put gear and buffs that pays off the player) rather than a *tactical unit the player directs turn to turn*, deliberately built for the solo player who wants presence without command overhead.

### Dungeon Crawl Stone Soup and NetHack: roguelike allies and pets

DCSS's own design manifesto explicitly favors accessibility and low-friction systems over deep micromanagement (RogueBasin: https://www.roguebasin.com/index.php/Dungeon_Crawl_Stone_Soup). Its god-granted allies (Beogh's orcish followers, Yredelemnul's undead servants, Fedhas's plant allies) are autonomous: the player receives them as a passive benefit of worship, issues no per-turn orders, and the god's system replaces or regenerates them rather than the player individually curating a roster.

NetHack's pet system is the oldest and lightest-touch version of the same idea:

- The player's pet (a dog or cat by default, expandable via taming) fights automatically with no commands at all.
- It can be ridden if strong and fast enough to serve as a steed.
- It has emergent utility that doubles as game economy: a pet standing on an item can be walked into a shop without triggering "shoplifting," and watching what a pet refuses to stand on can help identify cursed items (NetHack Wiki: https://nethackwiki.com/wiki/Pet, https://nethackwiki.com/wiki/Taming).

The lesson from both roguelikes: a pet needs zero UI beyond "it's there," and it pays for its screen space with small emergent utility (a scouting function, a distraction function, an identification trick), not combat numbers.

### Escort quests: the design failure mode to avoid repeating

Escort missions are close to universally disliked, and the reasons are well documented (Game Developer: https://www.gamedeveloper.com/design/can-we-fix-escort-mission-game-design-):

- The escorted NPC is slow, has combat-attracting AI, and cannot be healed or buffed the way the player can.
- The mission removes player-controlled pacing, forcing the player to react to the NPC's poor choices rather than to the encounter itself.
- The core problem is a mechanical mismatch: escort logic usually runs on a different rule set than the game's main combat, so the player is fighting two systems at once.

The article's proposed fixes map directly onto solvable Aegis problems:

- Let the player control the escortee's pace with a small command set (wait / follow / go there), exactly what KCD gives Mutt.
- Give the escortee parity access to defensive tools the player has, so a bad early exchange doesn't cascade into an unrecoverable failure state.
- Prefer *contained* protection scenarios (Resident Evil 4's Ashley, Bioshock Infinite's Elizabeth) where the escortee is either invulnerable-by-design or actively useful (Elizabeth throws the player ammo and salts) rather than a pure liability.

The worst version of an Aegis escort quest is a fragile NPC with opaque AI wandering into enemy telegraphs. The fix is either narrative invulnerability (enemies scripted not to target them, matching the "every quest must trace to a world fact" rule so the reason for their safety is diegetic) or a hard one-to-two command verb set the player can use to hold them out of danger.

## Companion Tiers at a Glance

| Tier | Precedent | Player commands | Own inventory/skills? | Can be lost? | Terminal UI footprint |
|---|---|---|---|---|---|
| Combat summon | Elden Ring Spirit Ashes | None (autonomous) | No | Yes, cheaply (resummon cost) | One status line |
| Utility animal | KCD Mutt, NetHack pet | 1-2 contextual verbs | No | Rarely, recoverable | One status line, no panel |
| Pack animal / mount | Skyrim pack-mule use, D2 stash mule | Load/unload, ride | Carry capacity only | No (logistics only) | Inventory-screen note only |
| Hireling / gear sink | Diablo 2/3 mercenaries and followers | Equip only, no orders | Small, class-restricted | Yes, recoverable for a fee | One equipment sub-screen |
| Story guest | Elden Ring named NPCs, escort NPCs | 1-2 hold/advance verbs | No | Story-scoped, can be permanent | One status line, scene-bound |
| Full party member | Darkest Dungeon, Battle Brothers | Full per-unit orders | Full: gear, skills, position | Permanent (by design) | A dedicated panel per unit |

Aegis should live entirely in the top five rows. The bottom row is listed only to make the boundary explicit: it is the point at which a companion system becomes a different game.

## Permadeath Asymmetry: Precedent for Companions Dying When the Player Cannot

Aegis has already decided the player cannot permanently die (Wounded debuff, Death's Toll scars, Aegis-driven resurrection). Several respected RPGs establish that this asymmetry, companions with real stakes around a protagonist who is narratively guaranteed to continue, is a proven pattern rather than a contradiction:

- **Tyranny, Wildermyth, and Fallout: New Vegas's hardcore mode** all let companions die permanently while the protagonist's own failure state is softer or entirely different (Gamerant roundups: https://gamerant.com/best-rpgs-permadeath-ranked-wasteland-2-valkyria-chronicles/, https://gamerant.com/rpgs-allow-party-members-can-die-killed/).
- **Wildermyth** in particular, already a locked reference point for Aegis's own role-casting system, builds its entire emotional arc around companions who can die, lose limbs, or age out over a campaign, while the player's throughline (the world and its legacy) persists across characters. This is the single closest precedent to Aegis's own shape: a persistent world/player-adjacent frame around disposable, mortal companions.
- **Mass Effect 2's Suicide Mission** is the sharpest illustration of stakes-through-asymmetry: the protagonist's survival is assumed, but loyalty missions and mid-mission choices determine which named companions live, making companion mortality the entire dramatic payload of the finale.

The design conclusion: a companion who *can* actually die when the player cannot is a proven way to make stakes legible without contradicting the player's own immortality contract, as long as it stays sparse and story-scoped rather than becoming a roster the player must protect at all times (which reintroduces party-management overhead).

## Pitfalls / Failure Modes (Cross-Game Synthesis)

- **Second character sheet creep.** The moment a companion has its own inventory, its own skill list, and its own turn in the initiative order, the game has quietly become Darkest Dungeon or Battle Brothers regardless of what the marketing says. Diablo 2/3 avoid this by giving hirelings a tiny, class-restricted gear slot set and no stat pools of their own; that is the right amount of "customization" for a game that wants one protagonist.
- **Unkillable followers remove stakes; unmanageable followers remove fun.** Skyrim's essentially-immortal followers make companions feel weightless in combat. The opposite failure (a companion who dies constantly and must be babysat, the pre-expansion D2 mercenary problem) makes players stop using them entirely. The sweet spot precedent (D2 LoD, Elden Ring Ashes) is: companions *can* be lost in the short term (die mid-fight, need FP to resummon) but the loss is recoverable and cheap, not a permanent narrative event, unless the companion is specifically a story-scoped character whose death is meant to land.
- **Escort AI that fights the player's own combat system.** If a companion's pathing or aggro logic doesn't respect the same telegraph and posture rules the player reads, every fight with them present becomes about babysitting an NPC that doesn't play by the rules it's asking the player to master.
- **Banter/relationship systems with no payoff feel like padding.** Pillars of Eternity's writers deliberately let some companion arcs end without full closure, which some players read as unsatisfying (Pillars wiki: https://pillarsofeternity.fandom.com/wiki/Pillars_of_Eternity_companion_banter). A companion whose narrative arc is Aegis's actual payload (memory, betrayal, a fact in the world graph) needs a mechanical or story capstone, not just ambient dialogue, or it reads as wasted authoring effort.
- **The pack-mule niche is real and should be granted on purpose, not emerge as an exploit.** Multiple communities note that a large fraction of single-follower playtime in Bethesda games is inventory logistics, not combat. Rather than treating this as a workaround, a mount/pack-animal role can be an explicit, intentionally low-fidelity slot: no combat AI to write, no death spiral to balance, pure carry capacity and travel-speed value.
- **A summon that scales encounter difficulty punishes the exact player it was meant to help.** Elden Ring's Ashes deliberately do not increase boss HP or damage when summoned (unlike its co-op invasions), because a difficulty-scaling summon stops being a crutch for a struggling player and starts being a wash. Any Aegis combat-companion tier should follow this rule strictly: encounters are not authored around "assume a companion is present."

## Recommendations for Aegis

1. **Adopt an Elden-Ring-Ashes-shaped combat-companion tier, not a follower tier.** One summon-able presence, gated by a resource that competes with the player's own resource economy (an Essence- or stamina-adjacent cost, not free), autonomous behavior with zero per-turn commands, and a short list of pre-baked "kits" (tank, distraction, ranged support) rather than an open build space. This preserves the 1-v-many read-the-telegraph combat loop: the companion is a dial the player turns before a fight, never a unit they pilot during it. In a terminal UI this is one status line ("Ally: Wolfhound, 40/40, engaged") with no dedicated panel, exactly the Ashes/NetHack-pet budget.

2. **Split "companion" into two non-overlapping product types and never let them merge: the story companion and the pack animal.** A story companion (KCD Mutt template) exists for narrative reasons traceable to the world fact graph, has one or two contextual command verbs at most (Sic/Distract, Search/Scout), no inventory, and no permanent death unless the story specifically calls for it (Blaidd-style, resolved at a fixed narrative beat). A pack animal or mount exists purely for the pack-mule niche Bethesda players self-selected into: carry capacity and travel speed, zero combat AI to maintain, granted openly (a stable, a trained mule) rather than emerging as an inventory-overflow hack.

3. **Let companions die when the player cannot, and treat that asymmetry as the design's emotional payload, not a bug to patch around.** A companion who can be permanently lost, per the Tyranny/Wildermyth/New Vegas-hardcore precedent, is the cheapest way to reintroduce real stakes into a world where the protagonist's death has been deliberately softened. This should be sparse and story-scoped (a handful of named companions across the critical path, not a roster), and any companion loss should generate a durable fact in the world graph (so NPCs can reference it, matching the Nemesis-style memory requirement already locked in) rather than silently vanishing.

4. **Give the ash-tier combat ally a hard concurrency cap of one, with no stacking and no positioning micromanagement, and make it visibly cost something the player wants for themselves.** Elden Ring's FP-competition and Diablo 3's Emanate mechanic both show the same trick: make the companion's benefit come out of a pool the player would otherwise spend on their own power, so the companion reads as a tactical trade-off (call for help now vs. save the resource for a spell or perk) rather than a free stat stick. This also keeps the UI cost near zero: a single summon-charge readout the player already tracks for other reasons.

5. **Treat quest-scoped "guest" companions as a narrative device with an explicit expiry, and build their combat safety diegetically rather than through invisible invulnerability flags.** Following the escort-mission postmortems, any temporary companion who must survive a sequence should have a *world-fact reason* enemies don't fully commit to killing them (a rival wants them alive, they're protected by an oath, the antagonist needs them as leverage), which both satisfies "every quest traces to a world fact" and avoids the ludonarrative dissonance of an NPC who's obviously unkillable for no in-fiction reason. Give the player one or two hold/advance commands over them (matching Mutt's command-on-hover pattern) so a bad AI moment is always recoverable by the player, never a silent mission-fail.

6. **Do not build companion inventories, companion skill progression, or companion positioning/formation systems.** This is the line Darkest Dungeon and Battle Brothers cross that Aegis should not: those systems only pay for themselves in a game whose UI and pacing budget is built around managing several units at once. Any companion feature that starts to require its own gear panel or its own turn in initiative should be read as scope creep toward a genre Aegis is not building, and cut or folded back into a passive perk. Fallout 4's permanent affinity-perk model is the safe outlet for "I want my relationship with this NPC to matter mechanically" without adding a second combatant to manage.
