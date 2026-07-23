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
arc ships, the **magic pillar opened (D-091/D-099)**: five workings found on graven stones,
Focus, and Spellcraft, and **character creation shipped whole (D-092/D-093)**: the
asking at the first wake, five original folk, seven pasts, shapings, the precious
thing, and stage 2's burdens, vows, remembered face, and keepsake thread. The
**companions pillar opened with guests whole (D-097)**: the ally engine plus the
huntsman's-debt arc, loyalty beats, full death weight, and the paid ending, run live by
every journey. The **Death's-Toll pillar shipped whole (D-098)**: the deterministic
ledger, three scars matched to their deaths, their live weight, and each one's costly
cure road. The **companions pillar shipped whole (D-097, D-099, D-100)**: guests, the
calling's shade, and the three-beast roster with its stable. The remaining **breadth
holes**: none untouched; all four activity families have broken ground, and **crime is the first family with every named verb shipped** (D-127: pilfering, pickpocketing, lockpicking, fencing, and burglary proper), and since D-128 every secret fact its deeds write has a reader on the lane; town life thickened in D-108/D-123 (the bones, the round, and both ledgers read), though it still trails wilderness and craft. Rough fill levels:

- Attributes: **7 of 7** mechanically active (D-091 woke Mind and Will; D-117's scene checks wake Presence, the last)
- Skills: **18 built of 18** (D-171 builds D-162's Alchemy, Athletics, Stealth,
  and Larceny contracts)
- Activity families: **wilderness-living core built** (hunting, selling, cooking, foraging:
  D-070..D-074; finite high-fells fishing D-166; live-pressure Athletics rush D-171), the **craft family opened** (cooking
  D-073; alchemy v1 D-090, with its self-brewing skill curve D-171), and the **crime family
  complete on its named verbs** (pilfering D-086; pickpocketing + Sleight D-107;
  lockpicking D-122; fencing at the peddler's cart D-124; burglary proper D-127; the
  Sleight/Larceny split and deterministic soft tread D-171); **town life opened** (knucklebones D-108;
  carousing + the light-purse read D-123)
- Story templates: **6** built (Raided Stead plus Blight, Throne, Faiths, Gold Rush, Long Siege)
- Path to 1.0: **8 of 10 tranches Verified** (V1-01 D-166, V1-02 D-167, V1-03 D-168,
  V1-04 D-169, V1-05 D-170, V1-06 D-171, V1-07 D-172, V1-08 D-173); V1-09 is
  Implemented under D-174, but its terminal candidate is superseded by D-175. V1-10 is
  Implemented under D-177; the replacement package and explicit signoff remain
- **Factions begun (D-076..D-089, D-105, D-106, D-109..D-112):** the local-reputation foundation is in (the home stead's
  regard, a per-world Fame earned by perceivable deeds), it pays (D-077, the friend's welcome),
  the ledger went keyed with a second faction (D-078, the raiders' wrath: one notch per
  raider slain, dread softening their blows past its rung), the coarse tick began (D-079,
  the raids are real: uncleared camps raid the stead every 160 turns, bread a coin dearer per
  raid), it pays again at the friend rung (D-080, the friend's price; D-085, the hearthtale
  kept from strangers, riding regard rungs written as facts), and the Infamy half opened
  (D-086, the stead's suspicion: pilfering a door climbs a three-rung shame ladder that
  closes the friend's boons and bars the larder, with coin on the robbed sill the way back),
  and the bright ladder's top rung pays (D-087, the stead's teaching: lessons shown freely
  to the stead's own, closed by suspicion, reopened by restitution), and the facts got their
  consumers (D-088, three storylets: the named thief confronted, the hearthtale carried on
  the lane, the deep cellar shown to the stead's own), and the state vectors landed (D-089,
  stores + boldness on the tick: raids embolden and take double, culls cow the dens, bared
  lofts end the raids, cleared worlds recover), and the stead now moves on the tick itself
  (D-105, the watch posted against greed and the levy called at the last measure, with the
  levy answerable by the bearer for regard), and the third faction stands (D-106, the long
  mound's grudge: the relation matrix has its second edge), and the roster went named
  (D-110, the camp's chief and lieutenants: the bounded Nemesis-style memory begun),
  and the roster is read aloud with every conflict's exit audited (D-111: the risen
  voice and the leaderless dens at the doors, the boast laughed off at the well, the
  D-023 no-eternal-stalemates box closed)
- Major vision pillars: all broken ground (magic opened D-091; the craft family
  opened D-073/D-090; character creation shipped both stages, D-092/D-093; companions
  shipped whole, D-097/D-099/D-100: guests, the shade, and the beast roster; the
  Death's Toll shipped whole, D-098: ledger, scars, and cure roads)
- NG+ world twists: **3 of 3 opening laws built and verified** from tier 7 onward,
  one independently dealt law per world through the no-repeat shuffle bag (D-151/D-152)

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
  (cooking, foraged-goods sale). The deferred alternative lane landed: **character
  creation stage 1 (D-092)**, folk + pasts + shapings + things at the first wake.
- **Phase 2: A keystone pillar (current).** **Factions started (D-076..D-078):** the
  local-reputation foundation shipped (the stead's regard, perceivable-deed earning, per-world
  reset), its first boon (D-077, the friend's welcome), and the second faction on a keyed
  ledger (D-078, the raiders' wrath, with the dread softening their blows: a blow to one is
  a favor to the other, live), and the coarse tick's first event (D-079, the raids are real:
  live pressure to clear the camp, price consequence, designed exit). The friend-rung boons
  shipped (D-080 the friend's price, D-085 the hearthtale), and the stead-Infamy half opened
  on the first transgression verb (D-086, pilfering with restitution as the designed exit).
  The bright ladder is fully paid (D-087, the stead's teaching at the own rung), and the
  faction facts have their first consumers (D-088: the thief confronted, the tale carried,
  the keeping shown), and the keystone clause itself landed (D-089: state vectors on the
  tick, stores and boldness, with recovery as the exit's aftermath), and the stead now
  acts on it (D-105: the watch and the levy), and the third faction stands (D-106: the
  mound's grudge, the relation matrix's second edge), and every produced fact now has its
  reader (D-109: the debt made right, the door that held, the two ledgers), and the roster
  went named (D-110: the camp's chief and lieutenants, the scar, the succession, and the
  boast: D-023's bounded Nemesis-style memory begun), and the roster is read aloud with
  the exits audited (D-111: the risen voice and the leaderless dens in the raids topic,
  the boast laughed off at the well, and every conflict confirmed to hold a designed
  exit, the D-023 box closed), and the first faction-hungry story template stands
  (D-112: the Usurped Throne at slice scale, the dens' seat as the taken throne, with
  the roster as its cast and the endings-fire-once rule hardened in both evidence
  templates), and the follow-on consumers are cashed (D-113: the mended page, the two
  memories, the mound topic's grudge, the chief drawn apart). The War of Faiths
  shipped whole across three sessions: scoped (D-114), its institutions raised
  (D-115: the order at the harrow, the keeper at the stead's shrine, the founding
  in history), and the template built (D-116: the whole cast by office, the drawn
  aggressor, the paired schism accounts, the socket's truth, and the claim said at
  the shrine), closing the named launch-template list. **Magic, the alternative keystone, landed (D-091):** Mind and Will wake,
  the caster build exists (graven stones, four workings, Focus, Spellcraft), and the deep
  sites carry a prize beyond coin and gear.
- **Phase 3: Remaining pillars & stakes.** `[x]` essentially DONE: companions shipped
  whole (D-097/D-099/D-100), the Death's Toll shipped whole (D-098), all four activity
  families opened and crime complete on its named verbs (D-127).
- **Ongoing: Breadth & depth.** Catalog growth (templates, monsters, tiers, gear, oaths),
  combat depth (posture, parry, movesets), and narrative depth (dialogue trees).
- **The next stretch (2026-07, adopted D-131): see `design/plan-2026-07.md`.** Four
  lanes: the stead layer first (Phase A), multi-region (Phase B), breadth interleaved
  (Phase C), pacing/worldgen/NG+ freshness (Phase D). The plan holds the reasoning and
  the 16-step suggested sequence; the trackable boxes live under the pillars below (the
  stead-layer and pacing-and-freshness sections are new; the B and C lanes mostly attach
  to boxes that already existed). This entry supersedes the phase numbering above as the
  sequencing story.
- **The path to 1.0 (adopted D-155, design-first D-157, amended D-175 through D-177): see
  `design/plan-1.0.md`.** The original
  sixteen-step sequence is complete. Ten ordered tranches now form a finite finish
  line: the fells capstone; weather and seasons; D3 prose variety; D1 pacing steering;
  town and economy depth; character and activity breadth; combat and magic depth;
  companions, factions, and consequence depth; then the next full-density region and
  release audit; then the SadConsole client and release recovery. Open-ended catalogs
  remain tracked but do not block 1.0 unless a later decision explicitly promotes them
  into the gate. V1-01 through V1-08 are built and Verified under D-166 through D-173.
  V1-09 and V1-10 are Implemented. The replacement package and explicit signoff are the
  active release gate.

### Path to 1.0 tracker (ordered, adopted D-155/D-157)

- [x] 1. B4 capstone: fourth high-fells site at full regional density
  (V1-01 design Approved D-156; built and Verified D-166)
- [x] 2. Weather and seasons v1 plus the enabled A2 event follow-ons
  (V1-02 design Approved D-158; built and Verified D-167)
- [x] 3. D3 prose-variety infrastructure and repetition audit
  (V1-03 design Approved D-159; built and Verified D-168)
- [x] 4. D1 pacing steering
  (V1-04 design Approved D-160; built and Verified D-169)
- [x] 5. Town and economy depth tranche
  (V1-05 design Approved D-161; built and Verified D-170)
- [x] 6. Character and activity breadth tranche, completing the intended skill roster
  (V1-06 design Approved D-162; built and Verified D-171)
- [x] 7. Combat and magic depth tranche
  (V1-07 design Approved D-163; built and Verified D-172)
- [x] 8. Companions, factions, and consequences depth tranche
  (V1-08 design Approved D-164; built and Verified D-173)
- [~] 9. Next full-density region, launch content closure, and the 1.0 release audit
  (V1-09 design Approved D-165; built D-174; terminal candidate superseded D-175,
  replacement packaged manual playthrough and explicit user signoff pending)
- [~] 10. SadConsole client migration and replacement 1.0 candidate
  (V1-10 direction Approved D-175; complete migration contract Approved D-176;
  Implemented D-177; replacement package and explicit user signoff pending)

The detailed tranche contents and design statuses live in `design/plan-1.0.md`; the
1.0-ready gate also remains in `design/plan-2026-07.md`. A tranche flips
to `[x]` only when its decisions are built and verified. At the final audit, every other
unchecked or partial roadmap line must be explicitly classified as post-1.0 or promoted
into the gate. This keeps the backlog honest without making open-ended catalog growth an
infinite prerequisite for completion.

---

## Feature checklist by pillar

### The Spine (foundation), built

- [x] Deterministic engine: hierarchical seed tree, fact graph, worldgen (D-002, D-013, D-018)
- [~] Layered-map presentation: Frame and Presenter are built and verified (D-001);
  SadConsole shipping client, owned font and palette, resizing, and focus-free control
  are Implemented under D-175 through D-177; visible packaged signoff remains
- [x] Save system: seed + input journal, replay-on-load, currently v99 with campaign-scoped
  generator 1 (D-012, D-028, D-166, D-167, D-169, D-170, D-171, D-172, D-173, D-174)
- [x] NG+ crossing: waygate, coin -> Legend, tier-deepening worldgen (D-011, D-029)
- [x] The Aegis as diegetic companion voice (D-010, D-019)
- [x] The full trans-world Aegis story arc: reveal ladder -> the keeping -> the mending -> steady state (D-020, D-026, D-037, D-038, D-039, D-045, D-060)

### Combat

- [x] Telegraphed-intent grid, stamina economy, dodge/strike (D-004)
- [x] Player ranged: the hunting bow (D-050) and the ash-spear reach thrust (D-056)
- [x] Knowledge-sharpened telegraphs: bestiary read tiers, dulling across NG+ (D-061)
- [x] Wits given combat meaning (read clarity) (D-059)
- [~] Enemy variety: the launch roster now includes the fen adder and rune-tongue beside
  the established tier bands and game families; further families are post-1.0 catalog
  growth under D-165 (D-033, D-040, D-044, D-053, D-057, D-058, D-163, D-165, D-174)
- [x] Posture / second bar, break-and-riposte: every foe's guard rocked by paid blows
  (1), the wall (2), the heave (3), and the parry (4); at the brim the stagger and the
  flat +4 riposte through the open door (D-125)
- [x] Parry as a distinct verb: 'a' against a blow shown at your own ground, no dice,
  turn-committed; the feint's lying mark can never be met (D-125)
- [x] Weapon movesets: family-specific verbs plus exact flank geometry complete the
  launch set (V1-07 design Approved D-163; built and Verified D-172; vision §4,
  deferred D-041)
- [x] Stances: measured/pressing/guarded on 'x', flat 2-point trades, turn-cost under
  live steel (D-094)
- [x] Weapon identity: signature verb per family (blades' answered step, hafted sunder
  riding the heave, brawling shove; the spear's long thrust already stood) (D-095)
- [x] Enemy second moves: goblin cry, thegn feint (lies below a keen read), hound drag,
  wight grave-chill (D-096)
- [x] More second moves and awareness-aware alarm composition (V1-07 design Approved
  D-163; built and Verified D-172; D-096)
- [x] Flanking proper, warder close phase after the board breaks (V1-07 design Approved
  D-163; built and Verified D-172; D-095; the brawling wall-slam cost shipped as guard
  pressure, D-125)
- [x] Monsters that read the player's commitment and stance (the other half of D-004):
  the thegn's heave-counter (D-058); the field's pressure reading the footing and the
  held wind-up, and the thegn knowing the beaten-open guard's door (D-126)
- [x] The bearer's own posture bar: landed committed blows as the field's pressure
  verbs, Will's brim (D-015's posture clause), the two-turn armless stagger with the
  feet kept working (D-126)
- [x] A kind with a pressure verb of its own: the shield-carl's board-check, thrown
  mass aimed at the bearer's guard and not the blood, met cleanest by the parry (D-129)
- [x] A kind that answers being parried: the drilled thegn's bind, half its force kept
  and a point shoved back through the crossed iron (D-129)
- [x] Parry- and stance-riding level-6 knacks (V1-07 design Approved D-163; built and
  Verified D-172; D-125, D-094)
- [x] Formal duels / judicial combat set-pieces (V1-05 design Approved D-161;
  built and Verified D-170)

### Character identity

- [x] Attributes: 7 defined and **7 active** (Might, Grace, Vigor, Wits; D-091 wakes Mind as
  working-power and Will as the Focus pool and the wind-up's grip; D-117 wakes Presence
  through visible scene checks) (D-015, D-091, D-117)
- [x] Character creation flow: the asking at the first wake, in-fiction, journaled keys,
  fate door for the rolled bearer (D-092)
- [~] Folk: five original anchors shipped, tilt + trait each (D-092 supersedes D-017's
  example roster, keeps its structure); per-world regenerated cultures and standing are
  post-1.0 under D-165
- [~] Backgrounds: seven pasts seed starting skills, extras, and a `past` fact (D-092);
  illiterate starts live (D-148: scribe's-ward and hedge-healer wake lettered at Lore 1,
  the other five learn at the scrivener's desk); D-171 rehooks hedge-healer to Alchemy,
  wayfarer to Athletics, and oathbreaker's second skill to Larceny under D-162;
  per-world recultured societies are post-1.0 under D-165
- [x] Creation stage 2: burdens (one buys a second thing), vows, the remembered face, the
  keepsake's keyed storylet thread + NG+ placement when unpicked (D-093)
- [ ] Creation follow-ons: the face cast into real faction NPCs; NPC line banks adopting
  the name; folk-aware recultured societies in worldgen; keepsake content past the song
  (post-1.0 under D-165; D-093)
- [x] Literacy skill + books gating recipes/techniques/lore (D-148: Lore 1 is literacy;
  three opening books with concrete keeps; D-154 adds the fourth, a smithing text whose
  recipe spends tarn-iron; the scrivener teaches letters for coin; D-170 adds the
  town-law primer, folk-tales, and stable six-book shelf submenu under D-161;
  Lore knacks remain open) (D-005, vision §3)

### Skills (18 built of 18; V1-06 built and Verified D-171)

- [x] Blades, Hafted, Brawling, Warding, Ranged (use-grown, cost-gated) (D-042, D-050)
- [x] Hunting: use-grown, fed by game brought down in the wilds; fattens the hide yield (D-070)
- [x] Cooking: use-grown, raw meat to rations at the wood's-edge fire; fattens the yield (D-073)
- [x] Survival: use-grown, fed by foraging herbs from the wood; fattens the forage (D-074)
- [x] Knacks/perks at level 2 and 4 for the five combat skills (20 options / 10 questions) (D-046, D-055)
- [~] Craft skills: Cooking shipped (D-073); Alchemy's self-brewing use-curve ships
  under D-162/D-171 after the original stillcraft opening (D-090); Smithing seeded at
  the stead's bench (D-135: use-grown by filing wear off owned iron) with its town school open
  (D-141: the forge files for coin away from home, and the drawn-temper lesson deepens every sitting),
  then fed by smelting the fells' tarn-iron into blooms (D-153) and using a bloom in the
  first book-taught durability recipe (D-154); further recipes and skill feeds are
  post-1.0 under D-165
- [~] Wilderness skills: Hunting (D-070), Survival (D-074, foraging), and Athletics
  rushes (D-171) ship; broader wilderness verbs are post-1.0 under D-165 (vision §3)
- [x] Subterfuge skills: deterministic two-turn Stealth and the distinct Larceny trade
  ship under D-162/D-171 (vision §3)
- [~] Social skills: Commerce seeded at the market town (D-140: use-grown, fed only by
  lots sold above the valley's own price, its level added in coin to every town lot);
  Persuasion seeded at the moot-stone (D-142: use-grown, fed only by pleas that truly
  moved the warden's book, the fine itself the cost, its level shaving the fine toward
  the floor); further feeds are post-1.0 under D-165 (vision §3)
- [x] Mind skills: Spellcraft shipped (D-091: use-grown, fed only by workings that did work,
  feeding power and the levin's grip); Lore shipped (D-148: literacy IS Lore 1, fed by the
  scrivener's sittings and pages not yet worked through; graven script exempt by doctrine)
- [~] Proficiencies: 7 lessons shipped (D-052 clean dressing/tended iron/gleaning; D-090 the
  stillcraft, paying D-087's deferred fourth slot; D-141 the drawn temper, the town school's
  showing, gated on Smithing 1; D-148 the wort-cunning, the first book-taught lesson, the
  herbal its only price; D-154 the bloom-temper, learned from the red smithing book and
  practiced with one tarn-iron bloom); quest-taught growth is post-1.0 under D-165
- [~] Knacks: level-2 questions for Alchemy, Athletics, Stealth, Larceny, and Sleight
  ship under D-162/D-171; level-4/6 noncombat and 3-option questions
  are post-1.0 catalog growth under D-165 (D-055)

### The Life: activities & economy (all 4 families opened: wilderness, craft, crime, town life)

- [~] Economy v0: shop, rations, repair, herbwife mend, hide-sale, fact-derived prices (D-036,
  D-025, D-071); first regional price spread shipped (D-081: herbs 5c at the stillroom vs 4c
  at the wood's edge); second spread shipped (D-124: hides 4c at the peddler's cart vs 3c at
  the woodward's bench, plus bread at the road's 6c: the first traveling vendor); third
  spread shipped (D-140: hides 5c and herbs 6c at the town's market with bread at the
  market's 4c: the arbitrage ladder's town rung, priced in the walk east, feeding
  Commerce); the first regional production chain shipped (D-153: finite tarn-iron seams,
  town smelting, and guild bloom sales), then gained its first use-versus-sale choice
  (D-154: one bloom gives one eligible iron piece 10 more wear once); caravan/arbitrage
  at scale is post-1.0 under D-165
- [x] Vendor sub-menu pattern: one talk digit opens a bench with its own nine slots (D-071);
  proven general by the second bench (D-081, the herbwife's stillroom: herbs at the
  apothecary's 5c vs the wood's-edge 4c, the first price-choice/arbitrage in the economy;
  the wound-dressing moved onto her bench, save v34)
- [x] Patronage deeds at the crossing (3: raised stone, endowed hearth, true verse) (D-054)
- [~] Crafting trades: cooking shipped (D-073); alchemy v1 shipped (D-090, v40: the hale-draught,
  three sprigs steeped at the stillroom or, taught, at any shrine rest; 'd' drinks it on the road:
  the herb lane's first sink and the first remedy that walks into a deep site; D-171 builds
  self-brewing as the Alchemy skill's costed use-curve under D-162); smithing opened
  as a player trade by repair and forge schooling (D-135/D-141), then deepened by smelting
  tarn-iron to blooms (D-153); its first recipe now ships (D-154: the Lore-2 red book teaches
  a one-bloom, one-time +10 wear temper for eligible ironwork); more recipes, commissions,
  and worked goods are post-1.0 under D-165 (D-006, D-025)
- [~] Wilderness living: hunting + sell path + cooking + foraging shipped (D-070, D-071,
  D-073, D-074); camping shipped with the road (D-138: 'm' anywhere plain on an
  overworld, the supper's ration buying the mending, Survival deepening it, the kill
  cooked at the fire); fells extraction feeds Survival through finite tarn-iron seams
  (D-153); fishing ships as three finite high-fells reaches with a cook-or-sell trout
  yield (D-166); live-pressure Athletics rushes ship under D-162/D-171; tracking and broader
  climbing and tracking are post-1.0, and swimming is excluded from 1.0 under D-165 (D-006)
- [x] A hide-buyer with room to grow: the woodward's trade sub-menu, hides to coin (D-071)
- [x] Crime: all four of D-006's named verbs shipped. Pickpocketing (D-107, v55: 'p' beside one
  of the stead's folk, the Sleight skill's dice, one try per pocket per world; clean lifts pay
  coin and write the secret fact, caught lifts ride the unified shame ladder with restitution
  in the wronged hand); pilfering + restitution shipped earlier (D-086); lockpicking (D-122,
  v65: the locked coffer in each fighting deep whose makers were the locking kind, opened on
  Sleight dice at 'g', one sitting per lock per world, the guilt-free outlet with no shame and
  no facts); fencing (D-124, v67: pilfering pockets a trinket off the mantel, and the peddler's
  cart buys everything with a past at 7c apiece, no questions, writing secret/fenced_goods);
  burglary proper (D-127, v70: 's' beside a stead door slips the latch, Sleight dice between
  the pocket's and the coffer's, one try per door per world; a clean entry pays the kist's
  4-9 coin and an heirloom for the cart and writes secret/burgled_house, a caught entry jumps
  the shame ladder two rungs with restitution at the crossed sill at twice a door's coin).
  D-171 builds D-162's launch split: Sleight keeps pockets and locks, Larceny takes pilfering,
  burglary, and fencing, and deterministic Stealth governs hostile-site movement. Deeper
  crime (a nemesis, organized work) is future texture, not a named verb
- [x] Fencing wants a fence: a peddler or second settlement before stolen goods have a buyer (D-107, built D-124: the peddler)
- [x] A consumer for the secret/fenced_goods fact (the heirloom missed on the lane) (D-124,
  built D-128: the grief spoken to the very hand, gated on live shame at zero)
- [x] A consumer for the secret/burgled_house fact (the stead reading an entered house) (D-127,
  built D-128: the bolted dark, the lane's new iron and kept-in dog, forbidden forever once
  shame/housebroken gives the lane a face)
- [x] A consumer for shame/housebroken in its turn (the named housebreaker meeting the lane
  that knows) (V1-09 design Approved D-165; built and tested D-174)
- [x] The peddler's stock growing with tiers: exotic goods, a second arbitrage leg, the
  caravan seed (D-124, D-025; built D-144: salt on the cart, two and the tier capped at
  six, bought at 5 and resold at the provisioner's 8, the bearer as the caravan)
- [x] Sleight's level-2 knack question: pockets versus locks (D-107, D-162, D-171)
- [~] Town life: gambling shipped (D-108, v56: knucklebones at the skald's hearth, 3 coin the
  throw, the one throw back as the real decision, the house's odds played plainly; the world's
  net ledger writes lucky_hand/light_purse facts at nine either way, the lucky hand talked
  about while the streak stands); carousing shipped (D-123, v66: the standing round at the
  skald's hearth, 5 coin once per world, no rung and no ledger, the lane remembering who
  poured); caravan/arbitrage shipped (D-144: the salt leg, the first buy-to-resell
  trade); the law-day lists, judicial challenge, guild loft, and fitted workshop ship under
  D-161/D-170; additional town-life growth is post-1.0 under D-165 (D-006)
- [x] A consumer for the light_purse fact (the stead reading a fleeced bearer, gated on the
  live net like the lucky hand's talk) (D-108, built D-123)
- [x] Carousing as a round-standing verb at the hearth (D-108, built D-123)
- [~] Aspirational sink ladder: property, retinue, master training, commissions (D-025, D-036);
  the stead half shipped whole as the five-work facility ladder (A3 v1 D-134, A3 v2 D-135),
  the town half now includes the world-scoped guild loft and fitted workshop under
  D-161/D-170; retinue and broader master training are post-1.0 under D-165
- [~] Grow the wood's-edge bench: cooking (D-073) and foraged-goods sale (D-074) shipped;
  hunting gear and lessons are post-1.0 under D-165 (D-071)

### The stead layer (plan 2026-07 Phase A, adopted D-131)

The home stead becomes something the bearer invests in and comes home to, riding
existing machinery (fact graph, storylets, the coarse tick, vendor benches). The
KDM-derived lane; full reasoning in `design/plan-2026-07.md`.

- [x] A1: Scheduled future facts: a `due` tick, a foreshadow hook, cancellation and
  hold conditions on the coarse tick; first uses: the hard winter (seed-drawn,
  foreshadowed, uncancellable) and the dens' muster (set by the cull, broken by the
  camp emptied first) (D-131, built D-132)
- [x] A1 follow-ons (D-132): the peddler's one capped Salt Fen restock ships under
  D-165/D-174; the calendar in the Snapshot for D4 and the deck-on-the-calendar plus
  after-the-fact talk readers landed with A2 (D-133)
- [x] A2: Stead event storylets on the tick beyond the raids: small, mixed-valence,
  consequence-dense, several foreshadowed via A1 (the fanfic test applies) (D-131,
  built D-133: four cards, two through the calendar, one cancellable)
- [x] A2 follow-ons (D-133, D-158, built D-167): the existing deck is season-gated;
  Haying days and Late frost read the lowland hand; the one-tick season's bargain reads
  stores, Regard, and Shame without adding a talk digit. Arrivals with names still want A5
- [x] A3 v1: Facility ladder, first three (palisade, watchtower, granary): coin sinks
  that modify existing systems (raids, watch, stores); this is the stead half of the
  open D-025/D-036 sink-ladder box; regard capped at a one-time acknowledgment per
  facility, never recurring (D-131, built D-134: the steadholder's works bench, each
  work funded once per world)
- [x] A3 v1 follow-ons (D-134, built D-139): the shared-nine topic-budget audit ran and
  found the real overflow it suspected (a full world fills all nine general villager
  digits, and every villager door carries an offer digit besides, so the season's-news
  topic had no key to live on: it moved to the shrinekeeper's door, the one board with
  room, with a fullest-world guard test pinning every talking door at or under nine);
  and the granary got its weather teeth (a washout under a standing granary takes
  nothing: the staddle stones hold the grain above the water, the flood still claims
  its night, the stead credits whose boards held)
- [x] A3 v2: Facility ladder, next two (stillroom extension, smithy bench; the Smithing
  skill seeded at home) (D-131, built D-135: the wing racks a third vial, and the smithy's
  fund digit becomes the bench verb that files wear and grows the craft)
- [x] A4: The crossing split: facilities and stead state reset with the World bucket;
  knowledge carries as it already does; light legacy echo via the traces' road (D-131,
  built D-136: audited clean by construction, one hygiene fix; the builder's echo is
  story only, one hushed-gated fact read as founding talk, never mechanical)
- ◇ A5: Named stead folk, deferred not rejected: revisit after A1-A3 are live and the
  raids can threaten something with a name (parking lot) (D-131)

### Multi-region (plan 2026-07 Phase B, adopted D-131)

The world widens to a handful of regions at valley density, one at a time, the road
between them as play. Starts after the stead layer's core lands.

- [x] B1: The road: travel-as-play before the destination (built D-138: the east road as
  a second overworld behind a mouth on the valley's edge; road weather ruling the step
  and the night; camping shipped, the D-006 box opened, cooking the kill and feeding
  Survival; the half-way glade as the seeded encounter site; the wayhouse and its keeper
  at the far end selling road bread and a bed, B2's signpost; the pilot walks it all
  live). Follow-ons tracked: road encounters beyond the glade, exposure past the
  stamina/camp clause, the far country opening in B2 (D-131, D-138)
- [x] B2: The market town: first cut built (D-140, plan step 9): the town stands behind
  its gate at the east road's end as the world's first peopled site (a walled 46x26 map
  of authored chunks stitched per seed: market row and moot yard always, the rest drawn
  and shuffled from a library; four townsfolk with site placement, the NPC machinery's
  new half); the market pays the road's best prices (hides 5c, herbs 6c, bread 4c: the
  arbitrage ladder's third rung) and Commerce, the 12th skill, is seeded there (fed one
  use per lot sold above the valley's price, its level added in coin to every town lot);
  the moot-warden speaks the law and the guild as topics (the web surfaced in a mouth);
  the pilot walks the market every world. Step 10's first half built (D-141): the forge
  and the guildhall now stand in every town beside market and moot; the forge-smith
  files iron for coin and feeds Smithing away from home, teaches the drawn temper
  (lesson 5, gated on Smithing 1); the carriers' guild is the town's first faction with
  a real ledger (the bond sworn once per world on a proven Commerce name, the mark
  worth a coin on every town lot); the pilot works the school and swears the bond
  live. Step 10's second half built (D-142): the law's book is machinery (the lift works
  inside the wall; a caught hand goes into the warden's book, not the stead's shame; a
  standing mark kills the haggle coin and two shut the counters, never the moot) and
  Persuasion, the 13th skill, is seeded in the plea at the moot-stone (the fine the cost,
  the level shaving the fine). Plan step 10 is whole; B2's core stands. D-170 completes
  D-161's next town cut: the world-scoped guild loft and fitted workshop, law-day lists
  and judicial challenge, and the stable six-book shelf. Still tracked beyond it: road
  encounters and the peddler's restock (D-138/D-132), plus later town breadth named by
  the remaining Path to 1.0 cards
- [x] B3: Region machinery, done once and kept general (D-131; built D-143 + D-144).
  Region entities and name pools (the D-049 box: Region as identity, two named
  countries per world on their own stream, the naming perceivable at the crossings and
  the doors both ends); cross-region news that travels (word rides the calendar two
  ticks with the drovers, restitution or pleas the designed exits; eastbound word kills
  the town haggle while home shame stands, westbound word is talk alone at the doors);
  and caravan/arbitrage between stead and town (D-144: salt bought at the cart's 5,
  sold at the town's 8, the stock dealt by tier, the bearer as the caravan seed, the
  pilot working the leg live). Per-region price spreads stand as the three-rung ladder
  (D-081/D-124/D-140) plus the salt leg; fact-derived pricing stays D-025's later vision
- [~] B4: Later regions after the town proves the machinery: the wild frontier opened
  D-146 (the high fells: third overworld and third named country off the road's north
  shoulder, heath/scree/tarns, the moor-wolf's pack with the pounce tell, no roof so
  the camp is the only rest, cold quartering the mend, the hunt ladder doubled through
  it, pilot leg live); filled in D-147 (the combe recut from the fells' own stone, and
  the high cairn: new site kind on the tops, wights over grave-gold and a graven word,
  the pilot's climb taking both errands); the season and its news D-149 (the wolf-winter
  chained off the valley's hard winter, the fang, and scarcity pricing hides in town at
  freight speed); the third site D-150 (the wolf-gill, the great she-wolf as the first
  elite beast, the pelt answering the fells' cold, the drover's cache); the country's
  own goods loop D-153 (four finite tarn-iron seams, Survival-scaled extraction, town
  smelting into blooms, guild sale, pilot and worldgen measure live), deepened D-154 with
  the first bloom recipe and the sell-or-use choice; brought to full current density in
  D-166 with the fourth site, three finite fishing reaches, permanent tackle, trout as a
  cook-or-sell good, and full pilot and worldgen proof. V1-09 builds the Salt Fen as the
  fourth named country with a roofed hamlet, four sites, salt work, the fen adder, a
  bounded local arc, and release observability (D-165, D-174). Regions beyond the fens
  are post-1.0 under D-165 (D-131, D-146, D-147, D-149, D-150, D-153, D-154, D-156)

### Magic (v1 shipped, D-091)

- [x] Spell system: found not menu-picked (D-091: graven stones, one per fighting deep site,
  the word decided at the reading so worldgen stays blind; grimoires/mentors/rituals remain
  as future sources) (D-022, vision §5)
- [x] Mind = potency, Will = pool + grip (D-091 amends D-022's shared-stamina sketch: casts
  spend their own Focus, 3 at baseline Will, +1 per point, regen on the road, full at rest;
  miscast lives only in the levin's broken grip, not on every cast)
- [x] Telegraphed cast wind-ups, interruptible both ways (D-091: the levin marks its ground a
  turn ahead, dodged by feet; a wound mid-hold can knock the word crooked, Will holds it)
- [x] First five workings: the spark (instant line, boards no answer), the levin (the
  wind-up), the ward (thickened air, teaches only when it turns a blow), the veilsight
  (names the floor, sharpens the reads, shows the pretenders), and the calling (held Focus
  gives the shade shape) (D-091, D-099)
- [ ] Attunement capacity from found world objects (D-022; excluded from V1-07 by D-163,
  post-1.0 under D-165)
- [ ] Caster social texture: awe, suspicion, faction attention (D-022; excluded from
  V1-07 by D-163, post-1.0 under D-165)
- [x] Severing and Mending as the sixth and seventh workings (V1-07 design Approved
  D-163; built and Verified D-172)
- [x] The rune-tongue, hostile magical pressure, and deterministic Will resistance
  (V1-07 design Approved D-163; built and Verified D-172)
- [x] Spellcraft level-2 and level-4 knacks (V1-07 design Approved D-163; built and
  Verified D-172)
- [x] The pilot learning to read stones and say words (policy increment, D-072/D-082 line) (D-091) (D-103)
- [x] Character creation hook: a known word as one possible precious starting thing (D-091 -> D-092)
- [ ] Spell list growth past the seven V1-07 workings / school content design
  (post-1.0 under D-165; ◇ parking lot; D-163)

### Factions & the living world

- [x] Fame/Infamy dual reputation per faction (D-023, D-076, D-078, D-086): local Fame with the
  home stead built (the regard ladder, perceivable-deed earning, per-world reset, HUD + greeting
  surfacing), the keyed per-faction ledger shipped with the raiders' wrath (D-078, the
  Infamy-shaped enemy ledger), and the home faction's own Infamy live (D-086, the stead's
  suspicion: pilfering as the first transgression verb, a three-rung shame ladder beside the
  regard it never cancels, restitution as the designed exit, save v36). Both axes now exist on
  both factions' books; deeper transgressions (violence, oath-breaking) are future verbs
- [x] Regard-gated boons and access (D-076, D-077, D-080, D-085, D-087): the friend's welcome
  (D-077, v30, a one-time coin purse), the friend's price (D-080, v33, a standing coin off bread
  that the hushed name never silences), the rumor kept from strangers (D-085, v35: regard rungs
  now write facts, and a friend-gated Talk storylet tells the stead's own story once per world,
  the seam for all authored reputation-gated content), and the stead's teaching (D-087, v37: at
  the own rung every lesson is shown freely, the boon paid in the one currency that crosses,
  closed by suspicion and reopened by restitution). Every rung now pays, and the follow-on
  consumers landed in D-088 (checked below)
- [x] A second faction with a relationship to the stead (the raiders as its standing enemy, so a
  blow to one is a favor to the other) (D-078: wrath per raider slain on its own faster ladder,
  the dread softening raiders' blows past rung 2, reset at every crossing, save v31)
- [x] Content consuming the rumor and shame facts (D-085, D-086; delivered D-088, v38): the
  hearthtale carried on the lane (the rumor fact's consumer), the deep cellar shown only to
  the stead's own (the rung-3 beat D-087 deferred, writing the graph's first `secret` fact),
  and the stead saying its piece to a named thief's face (suspicion acting beyond commerce,
  stilled by restitution on the live rung, writing a `confronted` fact)
- [x] Content consuming the `confronted` and `secret` facts in their turn (D-088; delivered
  D-109, no save bump): the making-right beat at the well (both producers feed it, the
  reckoning and the caught hand, gated on live shame back at zero; writes `made_right`,
  deliberately no coin or regard so restitution never turns a profit), the cellar mattering
  in a raid (the door that held: gated on the showing and the raid fact, the raid's morning
  read from inside the count), and the lifted purse colliding with trust (the two ledgers:
  the fence opened at the friend rung to a hand that has been inside it unseen)
- [x] Content consuming the `made_right` fact in its turn (D-109; delivered D-113): the
  mended page, the stead remembering the one who made right, kept where the stitching
  shows; no coin, no regard, being well-remembered is the reward
- [x] Faction state-vectors on a coarse tick, transitions write facts + narration hooks (D-023,
  vision §2, D-079; delivered D-089, v39): two causal axes ride the tick: the stead's stores
  (raids drain it, bread's price rides it, bared lofts are the raids' own dark exit, and a
  cleared world recovers a measure per tick until the lofts stand full) and the dens' boldness
  (derived: plunder emboldens toward greedy double-raids, raiders slain cow a tick to nothing:
  wrath's first faction-scale consequence). Every transition narrated + written (lofts_bare,
  dens_cowed, lofts_full); Snapshot carries both axes
- [x] The stead acting on the tick (D-089; delivered D-105, v53): both named moves landed:
  the watch posted the morning after a greedy raid (raiding nights turned away with nothing,
  a measure of upkeep per tick, standing down at the cull, the camp-fall, or the bare loft it
  can cause itself) and the levy called at the last measure (larder closed, the ration digit
  becoming the levy's answer: coin against a carted measure, +1 regard, the stores axis'
  first bearer-side input; lifted by answers or the season's recovery, the ask voiced by
  storylet)
- [x] Levy and watch follow-ons (D-105, V1-08 design Approved D-164; built and Verified
  D-173): the raids topic reads both states aloud, and the Crofter's grain road gives
  the levy a bonded Stead-to-Town answer. A separate default-pilot levy rung remains
  unnecessary unless implementation journeys show the opt-in companion route is not enough
- [x] A third faction, giving the relation matrix its second edge (D-078, D-089; delivered
  D-106, v54): the long mound's unquiet dead. Grave-goods taken while the dead still walk
  start the grudge (one rung, "marked by the long mound"): riled wights strike a point
  harder and the mound raises its slain again on the tick (capped at three, narrated,
  mound_restless written); the stead speaks of the taller lights at its doors (the fear
  edge: the mound stilled was already the regard's deed). The designed exit is the
  stilling itself: dead laid to rest keep no ledgers
- [~] Mound follow-ons (D-106): the villagers' mound topic reads the grudge aloud
  (D-113: the pacing lights and the mound's tally, the bearer never named, the dogs
  knowing more). A raiders-mound edge and a desecration verb beyond grave-goods are
  post-1.0 under D-165
- [x] Bounded Nemesis-style leader/lieutenant roster with memory (D-023; delivered D-110,
  no save bump): the camp's chief and two lieutenants named from their own seed stream,
  the stead's rumor and a nemesis/chief fact carrying the name from the first morning,
  rank worn as hide (+4/+2 Hp). Memory rides the replay: a named raider bloodied and
  left alive keeps the scar (nemesis/scarred), a chief slain over a standing lieutenant
  hands the camp to a named heir with the grudge in the office (nemesis/risen, ending in
  the no-heir silence), and the hand that authors the bearer's death keeps the boast
  (nemesis/slew_bearer). A held grudge arms the hand one point on the dread's own rail,
  and is spoken to the bearer's face at the next descent, once per memory
- [x] Nemesis fact consumers (D-110 follow-on; delivered D-111, no save bump): the
  goblin-raids topic reads the dens' order live (the risen chief named at the doors,
  the leaderless camp read as quieter about its plans), and the kept boast comes home
  as a Talk storylet, laughed off to the standing bearer's face (den-talk is not
  believed, so the stead's epistemology holds). The scar deliberately has no stead-side
  reader: no stead can see it
- [x] Roster follow-ons (D-110/D-111; delivered D-113): the two memories, the
  made_right thread meeting the roster's memory (the valley's two ledgers read side
  by side, the risen heir named, one closed by payment and one only outlived); and
  the chief told apart on the map (capital G among its lowercase raiders)
- [x] A named figure for another faction if one earns its keep (D-110, deferred
  again by D-113; answered D-116: the harrow's elder carries the order's claim as
  its champion, a role fact the whole template hangs on, and the keeper stands as
  the stead's opposite number)
- [x] Designed conflict exit conditions (no eternal stalemates) (D-023; audited D-111):
  every live conflict has its exit: raids end at the camp-fall or the bared loft, the
  watch stands down three ways, the levy lifts by answers or recovery, shame exits by
  restitution, the mound's grudge at the stilling, wrath's actors go extinct at the
  camp-fall, nemesis grudges die with their holders, and every ledger resets at the
  crossing
- [x] A player-mediated Stead-to-Town faction edge (V1-08 design Approved D-164;
  built and Verified D-173): an active levy, a sworn carriers' bond, a mortal Crofter road,
  and a delayed guild cart move Stores, levy state, Regard, and narrated facts without a
  new faction scalar
- [ ] Full-form story templates and broader institution, zealot, and warden roles
  (post-1.0 under D-165; D-035)

### Companions

- [x] Summon slot: the calling, a fifth word on the graven stones (the barrow leans
  toward it second); the called shade walks on the guest engine in its own slot beside
  a mortal guest, gated by 2 Focus held (bound, never spent, freed when it ends), no
  clock, released by saying the word again anywhere; modest blow doubled on the wight
  and graven kinds, refusing the severed (the laying stays the bearer's choice); full
  guest physics; unravels without weight on fall, release, the bearer's death, and the
  waygate (D-024, D-099, vision §7)
- [x] Pilot cast policy: the warded delver + shade doctrine, every journey now reads the
  stones, says the ward, and stands a shade on uncanny ground (D-103)
- [x] Calling follow-ons: a villager noticing the shade and the warder's response to the
  called uncanny ship once per world without numeric consequence (D-165, D-174)
- [x] Guest companions: role-cast from world NPCs, can permanently die (D-024, D-097):
  the guest engine (follow, fight to their measure, body-block, 'o' contextual order
  key, tending from the satchel, doors/death-wake shared) plus the first full arc:
  the huntsman's debt (woodward cast off a talk once the stead has bled, until the
  camp breaks), loyalty beats from all four sources, full death weight (grave fact,
  guest-beloved at 3+ beats, stead shame, the bench empty all world, memorial
  storylet), and the paid ending (portfolio fact, NPC home, +1 regard)
- [x] Guest combat and arc closure (D-097, V1-08 design Approved D-164; built and
  Verified D-173): nearest-body physical targeting, marked-footprint parity, deterministic
  automatic escapes unless holding, free refusal when a mortal guest blocks a shaft,
  the Crofter grain-road arc, unresolved crossing farewells, and two bounded Aegis-only
  cross-world memories. Broader roles and arcs remain post-1.0
- [x] Pack animal / mount (D-024, D-100, shipped whole in two stages): the stead's mule
  bought at the wood's-edge bench (friend-gated, 40 coin), the raiders' courser given
  over by storylet once the camp breaks, the wild fell pony won with bread on the high
  ground; following on the overworld, the ridden stride (two cells to a key: grass for
  all, hills and forest for the courser), waiting at site mouths, saddlebags banking
  coin (courser capped at 25) at the price that a raid landing while the bearer is
  below takes the beast whole; mortal-nerved beasts bolt from uncanny mouths shedding
  the bags, only the fell pony stands them; the stable's one cycling digit holds the
  roster safe from the raid; all mortal, world-bound
- [x] Pilot beast policy: courser forward, mule banks; every journey now buys, banks,
  claims the deed's courser, turns the stable, and brings the bank home pre-arch (D-104)
- [x] Beast warmth and recognition (D-100, V1-08 design Approved D-164; built and
  Verified D-173): one blood after weather-modified exposed camp healing, one character beat per
  beast kind, and an opt-in pilot road for the fell pony
- [ ] Predators at wild mouths (post-1.0 under D-164/D-165)

### Death, stakes & consequence

- [x] Death loop: banked vs at-risk, corpse run, remnant forfeit (D-008)
- [x] Wounded state (reduced max HP, timed recovery) (D-008)
- [x] Death's Toll meter: deterministic ledger, fills on death (boss hands fill more, Will
  shaves it), drains a turn at a time, converts above the line with no roll, sidebar rail,
  wiped at the waygate (D-098 stage 1)
- [x] Scars: three land matched to the death that made them (the taken eye dulls the read a
  tier, the crushed hand asks a breath more wind a swing, the haunted look cools regard and
  dears bread), carried across worlds until cured (D-098)
- [x] Scar cure roads: the stillroom's longest work for the eye (30c), the smith's brace
  for the hand (24c, D-009's prosthetic hook), the haunting sung to rest at the songhall
  (8 essence, the walk is the pilgrimage); every mend on the Aegis's parity line; the
  marks-they-carry talk storylet (once per world) (D-098 stage 2)
- [x] Pilot cure policy: the journey walks all three cure roads when scarred and funded,
  the laying's essence held back from the shrine (D-101)
- [x] Toll and scar launch closure (D-098, V1-08 design Approved D-164; built and
  Verified D-173): scar and scar-mended facts with consumers, the fitted brace's wielded-parry
  edge, capped tier-scaled Toll fill, and sheet plus snapshot presentation
- [ ] The dragging step and further scar catalog (post-1.0 under D-164/D-165; D-098)

### Narrative & dialogue

- [x] Storylet engine: fact-gated beats, 7 triggers, ~50 storylets (D-030)
- [x] NPCs: 5 kinds, bump-to-talk, ask-about menus from the fact graph (D-031)
- [x] World-story template compiler + selection (D-032, D-035, D-040)
- [x] World-story templates: 6 built (Raided Stead, Creeping Blight, Usurped Throne, War of Faiths, Gold Rush, Long Siege) (D-032, D-035, D-112, D-116, D-121, D-130)
- [x] Story template: Usurped Throne at slice scale (dens' seat, roster-cast, tier 2+) (D-020, D-112)
- [x] The two-faiths worldgen lane: the order's site (the harrow) and its folk, the keeper cast at the stead's shrine, the founding planted in history, topics both sides (D-114, built D-115)
- [x] Story template: War of Faiths at slice scale (cast by office on D-115's institutions, tier 2+) (D-020, scoped D-114, built D-116)
- [x] Story template: Gold Rush at slice scale (the old quarry fenced by a kind lie, tier 3+) (D-121)
- [x] Story template 6: the Long Siege at slice scale (the fen-leaguer's grateful fear, tier 6+) (D-130)
- [ ] Story template 7+ (post-1.0 under D-165; ◇ parking lot: the dying god's succession)
- [x] Dialogue-tree scenes with visible skill checks (D-021, built D-117: storylets open modal scenes through the journaled key path; the shuttered window is the first, with the game's first visible check)
- [x] Scene conversion: the faiths' claim at the shrine as the three-way choice D-116 parked (say it whole, wield it behind a visible check, keep it) (D-117 follow-on, built D-118)
- [x] Scene conversions for the remaining plot beats: the blight and throne climaxes (D-117 follow-on, built D-119)
- [x] Cross-world withheld consumer: the unsaid crosses as a silence fact, hushed or not, and later worlds retell the wrong story for true (D-118/D-119 deferral, built D-120)
- [ ] NPC depth: per-role voices, schedules, movement (post-1.0 under D-165; D-031)

### Pacing, worldgen & NG+ freshness (plan 2026-07 Phase D, adopted D-131)

The procgen-lessons lane: editorial authority above the tick, worlds that differ in
kind, and the evaluation harness that hardens every generator change.

- [x] D1: The pacing layer: D-145's measured teller gains D-160's bounded authority and
  ships under D-169. All seven live random-deck cards explicitly declare elasticity.
  Press guarantees an eligible deal after three quiet nights, while one successful
  opportunity may be suppressed per Space episode. Scheduled futures, faction clocks,
  seasons and weather, durations, player-triggered content, combat, and world state stay
  protected. The full diagnostic book is verified across the five-seed sweep
  (D-131, D-145, D-160, D-169)
- [x] D2: The twist library: D-151 settles the contract and D-152 builds it. Tier
  7+ carries exactly one mandatory mixed-valence law, independently drawn from the
  world story through a deterministic no-repeat shuffle bag; the Held Road, Grave
  Market, and Horned Law open the library. Twists are the primary structural answer
  to tier 7+ freshness, while new bands remain optional catalog growth. All three
  laws, their readers, pilot policy, snapshots, journey report, and worldgen measures
  are verified across the five-seed sweep (D-131, D-151, D-152)
- [x] D3: Prose variety infrastructure (design Approved D-159, built and Verified D-168):
  enumerable fact details, storylets, scenes, and ask-about topics; fact-keyed authored
  variant bundles composed per surface; stable pure-hash selection outside every gameplay
  RNG; declared Fixed, Rare, Standard, and Frequent budgets; and a metadata-aware
  repetition audit that distinguishes intentional constants from failed variation. It
  shares WorldEval and `aegis worldgen --dump` with D4, adds structured dump output, and
  hard-gates invalid contracts while keeping distribution findings advisory. The first
  five-family slice and the unvisited topic catalog are green across 86,282 surfaces
  (D-131, D-137, D-159, D-168)
- [x] D4: Worldgen evaluation harness: batch-generate worlds and chart expressive-range
  metrics (template mixes, fact-graph composition, site tenancy, name variety, prose
  skeletons); built D-137 as `aegis worldgen` on a pure WorldEval core, with `--json`
  for CI, `--dump` as D3's generate-then-curate feed, and a double-generate purity
  gate; landed before the generator grows again, as the plan sequenced. Play-reached
  states stay `journey --json`'s (D-083); B3's regional spread has landed, and D-153
  adds the fells' tarn-iron seam count to every world measure and digest (D-131, D-137,
  D-153)

### Content catalogs (breadth, grow over time)

- [~] Gear: 10 items; req axes Might/Vigor/Grace; further item and signature-verb
  catalogs are post-1.0 under D-165 (D-041, D-056)
- [~] Oaths: 9 live; the launch roster is full with the closed door and long count
  end-appended at V1-08 (D-047, D-051, D-164, D-173); further terms are post-1.0 growth
  under D-165
- [~] Legend: 5 rungs, 3 hospitality boons; rungs past 5 are post-1.0 under D-165 (D-048)
- [~] Hostility-tier bands: 2-6 distinct + tier-7 recombination; the D-151/D-152 twist
  library is the recurring tier 7+ structure while further bands are post-1.0 catalog
  growth under D-165 (D-033..D-058, D-151, D-152)
- [~] Scar list: three shipped (eye, hand, look); the dragging step and further scars
  are post-1.0 under D-165 (D-098)
- [x] Weather / seasons (design Approved D-158, built and Verified D-167): one shared
  season, three independent climate bands, deterministic three-card hands, four weather
  families, one-tick forecasts, exposed travel and camp effects, and the A2 follow-ons
- [~] Region entities, biome names, culture-flavored name pools, world epithets (D-049;
  region entities and region names built D-143: two named countries per world on their
  own stream; culture-flavored pools and world epithets are post-1.0 under D-165)

### Tooling & verification, built

- [x] Dev harness: headless pilot pipe, `sim` scripted JSON runs (D-027)
- [~] SadConsole player and focus-free shared pilot (D-175 through D-177, V1-10
  Implemented): the owned client, serialized host queue, current-user pilot, structured
  frame, Release and Native AOT smokes, settings, help, resizing, and package route are
  built; visible packaged and final physical-keyboard verification remain
- [x] Journey-bot autopilot: clears sites, arms, raises, reclaims, loots, answers the sheet, walks the arc, swears oaths, hunts, sells hides, cooks meat, forages and sells herbs (D-062..D-075)
- [x] Pilot footing and steeping policy: stance held to the blood's line (free presses on
  quiet ground, one bought downshift mid-fight), vials steeped before the sale and drunk
  where the road hurts; the runner's herb ledger untangled from the pot (D-102)
- [x] `--wits` demo mode for the perception build (D-084: Wits raised to baseline+2 first, so
  every mastered kind holds Keen at every arch where the baseline softens; deaths price the trade)
- [x] Machine-readable journey report for a sweep / CI (D-083: `journey --json`, one object
  with headline, economy, arc, faction peaks, and full per-crossing bestiary; prose untouched)
- [x] Worldgen evaluation harness (D-137/D-168: `aegis worldgen`, batch generation across seeds
  and tiers on a pure WorldEval core; per-tier story mixes, fact and storylet spreads, site
  tenancy, name variety, the family-aware prose audit, a double-generate purity gate wired
  to the exit code, `--json` for CI, `--dump` for grouped curation, and `--dump --json` for
  one structured surface per line)
- [~] Release journey and Windows x64 package (V1-09 design Approved D-165; built D-174):
  the machine-gated twelve-world route, campaign-scoped generator 1, repeatable Native
  AOT packaging, hashes, clean-extraction smokes, defect audit, and roadmap
  classification passed for the terminal candidate. D-175 supersedes that package; the
  SadConsole package, repeated automated gates, fresh manual playthrough, and explicit
  signoff remain

---

## Open design questions (mirror of the `decisions.md` parking lot)

All questions below are classified post-1.0 under D-165 unless a later decision promotes
one into a release.

- ◇ Folk cultures: how worldgen recultures the five folk per world, and whether factions read folk (D-017, D-092)
- ◇ Spell list growth past the seven V1-07 workings / school-shaped content, if any
  (D-022, D-091, D-163)
- ◇ Storylet external data-file format + condition/effect vocabulary (D-030)
- ◇ Catalogs to grow: more oaths beyond D-164's nine, the dragging step and scars beyond
  the launch three, Legend rungs past 5, patron deeds past 3, and hostility bands past the
  fen-leaguer (D-164, D-009, D-048, D-054, D-033+)
- ◇ Story open items: bottle-episode playability, Unbinder guise tells, reveal-tier sharing across characters, template 7+ candidates (aegis-arc.md §11, world-story-templates.md §11)
- ◇ Named stead folk (plan 2026-07 A5): adopt after A1-A3 are live, and at what depth (D-131)

---

## Changelog / newly tracked

Newest first. Log when a feature is checked off, or when new work is added to this file.

- 2026-07-23: **D-177 implements the SadConsole client and focus-free host.**
  `Aegis.Host` now owns saves, one serialized input queue, current-user pilot pipes,
  bounded requests, and text plus structured RGB observations. `Aegis.Client` owns the
  SadConsole and MonoGame window, embedded font, palette, resizing, input aliases,
  settings, and first-run help. `Aegis.Cli` becomes `aegis-tools.exe`. Release and Native
  AOT client smokes pass, all 999 tests pass, both five-seed journey sets, both seed-1
  replays, and worldgen are byte-identical to D-174. V1-10 is Implemented. The clean
  replacement package, visible physical-keyboard review, guided campaign, and explicit
  signoff remain.
- 2026-07-23: **D-176 approves the complete V1-10 migration contract.** The project
  boundary, rendering and input contracts, focus-free pilot, save and determinism
  boundaries, Native AOT package shape, accessibility requirements, migration sequence,
  acceptance criteria, and exclusions are locked for implementation. V1-10 moves from
  Draft to Approved. V1-09 remains Implemented, and neither card becomes Verified before
  the replacement package, complete automated sweep, restarted guided campaign, and
  explicit user signoff.
- 2026-07-23: **D-175 locks the SadConsole direction and focus-free pilot contract.**
  The guided candidate exposed terminal-owned presentation as a release risk. A bounded
  SadConsole 10.10.1 and MonoGame DesktopGL spike rendered the real Aegis frame with an
  owned tiled font and RGB palette, survived `Fit` resizing, launched under Windows x64
  Native AOT, and accepted canonical named-pipe keys without taking focus from Brave.
  The repository remains at 980 passing tests. V1-10 is added as a Draft tenth tranche,
  the D-174 terminal candidate is superseded, and the complete migration contract lives
  in `design/sadconsole-client-migration.md`. No tracked product code has changed.
- 2026-07-23: **D-174 the Salt Fen and the automated 1.0 candidate are built.**
  V1-09 opens the fourth named country with its independent climate, roofed hamlet,
  exact four-site mix, finite salt work, ordinary fen adder, bounded compact account,
  delayed capped peddler restock, and three promoted readers. Save v98 advances to v99
  with campaign-scoped generator 1. `journey --release` carries a nine-row machine gate,
  and the repeatable Windows x64 release script publishes Native AOT, hashes, packages,
  extracts, verifies, and smokes the candidate. Release builds are clean and all 980
  tests pass. Five default and five release twin pairs are byte-identical, both seed 1
  journals replay exactly, and generator 1 is pure across 240 worlds and 93,002 prose
  surfaces. The roadmap, important-fact, conflict, and defect audits are complete.
  V1-09 remains Implemented and partial until the user signs off the fresh packaged
  campaign.
- 2026-07-23: **D-173 companion, faction, and consequence depth is built and
  Verified.** V1-08 closes companion combat parity, the Crofter grain road, bounded
  success and loss memories, live watch and levy readers, beast warmth and recognition,
  scar aftermath, the fitted brace, tier-scaled Toll, and the final two launch oaths.
  `journey --companion` proves the complete live route, and `sim --keys-file` keeps long
  Windows journals replayable. Save v97 advances to v98. Release builds with zero
  warnings and all 968 tests pass, including 28 focused checks. Five v102 twin pairs are
  byte-identical and every seed reaches cycle 13. Default seed 1 replays 26,891 keys
  exactly to turn 25,825; companion seed 6 replays 38,588 keys exactly to turn 33,481
  with all three beast recognitions, both memories and oaths, a cured scar, 312 brace
  parries, and the capped tier Toll contribution. The 240-world purity gate has zero
  digest mismatches and zero hard failures across 89,402 prose surfaces. Tranche 8 is
  checked off. V1-09 is next.
- 2026-07-23: **D-172 combat and magic depth is built and Verified.** V1-07 closes
  launch movesets through exact flanking, an awareness-aware alarm, the boardless
  warder phase, the Severed sweep, and five level-6 martial questions. The rune-tongue
  introduces readable hostile workings and Will resistance. Severing and Mending bring
  the player catalog to seven, and Spellcraft gains its level-2 and level-4 questions.
  The default pilot answers every new threat and uses both new words; `journey --caster`
  demonstrates all seven workings and takes the recommended Spellcraft answers. Save
  v96 advances to v97. Release builds with zero warnings and all 940 tests pass,
  including 20 focused checks. Five v101 twin pairs are byte-identical, seed 1 replays
  26950 keys exactly to cycle 13 and turn 25885, the caster journal also replays exactly,
  all six JSON reports match their prose runs, and the 240-world purity gate has zero
  digest mismatches across 87722 surfaces. Tranche 7 is checked off. V1-08 is next.
- 2026-07-23: **D-171 character and activity breadth is built and Verified.** V1-06
  end-appends Alchemy, Athletics, Stealth, and Larceny to close the roster at eighteen.
  Self-brewing now feeds Alchemy, uppercase local directions perform costed live-pressure
  rushes, and hostile sites support deterministic two-turn soft tread with explicit
  awareness. Sleight keeps pockets and locks while Larceny owns pilfering, burglary, and
  fencing. Three pasts receive their approved hooks and all five level-2 questions ship.
  The default pilot proves the three lawful activity lanes and stays crime-free, while
  `journey --rogue` proves both criminal ledgers and their consequences. Save v95 advances
  to v96. Release builds with zero warnings and all 920 tests pass. Five v100 twin pairs
  are byte-identical, seed 1 replays 32,587 keys exactly to cycle 13 and turn 30,609, all
  five JSON reports match their prose runs, and the 240-world purity gate has zero digest
  mismatches across 87,722 surfaces. Tranche 6 is checked off. V1-07 is next.
- 2026-07-22: **D-170 town and economy depth is built and Verified.** V1-05 adds the
  world-scoped guild loft, settled bed, reading desk, safe strongbox, fitted repair
  workshop, one three-bout nonlethal lists entry per eligible world, and the one-bout
  judicial challenge. The scrivener now holds all six books behind one stable shelf,
  including the two final launch titles and their durable gates. Journey prose and JSON
  expose every approved counter; the crime-free pilot correctly records zero judicial
  results while focused tests prove both outcomes. Save v94 advances to v95. Release
  builds with zero warnings and all 900 tests pass, including 14 focused property tests.
  Five v99 twin pairs are byte-identical, seed 1 replays 26,386 keys exactly to cycle 13
  and turn 25,172 with all six books read, all five JSON reports match their prose runs,
  and the 240-world purity gate has zero digest mismatches across 87,722 surfaces. Tranche
  5 is checked off. V1-06 is next.
- 2026-07-22: **D-169 bounded pacing authority is built and Verified.** V1-04 gives the
  teller authority only over seven explicitly elastic season-deck cards. Press promotes
  a missed cadence roll when an eligible hand exists; Space suppresses one successful
  opportunity per continuous episode without selecting or carrying a card. Every tick
  still consumes one ordinary cadence roll, and every scheduled future, faction clock,
  weather step, duration, player-triggered consequence, fight, site, and worldgen path
  remains protected. Journey prose and JSON expose full call, outcome, gap, and per-card
  diagnostics. Save v93 advances to v94. Release builds cleanly and all 886 tests pass.
  Five v98 twin pairs are byte-identical, every key journal holds exactly against v97,
  seed 1 replays 25,260 keys to cycle 13 and turn 24,066, and the 240-world purity gate
  has zero digest mismatches. Tranche 4 is checked off. V1-05 is next.
- 2026-07-22: **D-168 prose variety infrastructure is built and Verified.** V1-03 makes
  fact details, storylet lines, scene lines, and ask-about answers enumerable with stable
  source, family, variant, skeleton, reuse, and origin metadata. Five structured fact
  families prove pure per-surface authored selection and the Fixed, Rare, Standard, and
  Frequent budgets, while legacy wording remains Fixed and visible. WorldEval now audits
  gated topics without pilot visits, groups the human dump, emits compact JSON-line
  records, reports per-kind and family measures, hard-fails invalid catalogs or missing
  variable content, and leaves distribution findings advisory. Save v93 holds. Release
  builds cleanly and all 877 tests pass. The final 240-world run inventories 86,282
  surfaces, including 20,272 topics, with zero digest mismatches or hard failures. Five
  v97 journey twins are byte-identical to their mates and to v96, and seed 1 replays all
  25,260 keys exactly to cycle 13 and turn 24,066. Tranche 3 is checked off. V1-04 pacing
  steering is next.
- 2026-07-22: **D-167 weather and seasons v1 is built and Verified.** V1-02 puts every
  world under one autumn-first calendar, preserves the seed-drawn hard-winter arrival,
  and gives lowlands, road, and high fells independent deterministic three-card hands
  across Calm, Wet, Wind, and Cold. One-tick forecasts, local narration, sidebar and
  snapshot reads, exposed walking and camp rules, existing shelter answers, season-gated
  stead cards, Haying days, Late frost, and the one-tick season's bargain are live. The
  pilot and journey reports exercise the system, and WorldEval charts every band-family
  cell. Save v92 advances to v93. Release builds with zero warnings and errors; all 862
  tests pass; five v96 twelve-crossing twin pairs are byte-identical; seed 1 replays
  exactly at 25,260 keys and turn 24,066; and worldgen reports zero digest mismatches
  across 240 worlds with nonzero coverage in all twelve weather cells. Tranche 2 is
  checked off. V1-03 prose-variety infrastructure is next.
- 2026-07-22: **D-166 the black tarn is built and Verified.** V1-01 closes the current
  high-fells density tranche with a deterministic fourth site, three reachable finite
  fishing reaches, permanent tackle, Survival-scaled trout, fixed-fire and camp cooking,
  town sale through the established economy rules, complete presentation, persistence,
  pilot policy, journey metrics, snapshots, and WorldEval coverage. Save v91 advances to
  v92. Release builds cleanly and all 848 tests pass. Five v95 twelve-crossing journey
  pairs are byte-identical, seed 1 replays exactly at 25,135 keys and turn 24,009, and
  worldgen reports zero digest mismatches across 240 worlds with exactly three reaches in
  every qualifying site. Tranche 1 is checked off. V1-02 weather and seasons is next.
- 2026-07-22: **D-165 the Salt Fen and the 1.0 release gate are designed.** V1-09 is
  Approved but not implemented. The fourth named country opens from the town end of the
  road with a roofed hamlet, four full sites, three finite weather-gated salt pans, one
  ordinary fen-adder family, the salters' compact and carriers' edge, and a bounded local
  arc with two equal-tier conclusions. The housebreaker and two Calling follow-ons become
  launch readers, and the compact's outcome completes the capped peddler restock. Generator
  1 is pinned per campaign in the save header. A machine-gated release journey, exact
  WorldEval coverage, zero-blocker defect rule, line-by-line post-1.0 roadmap audit,
  reproducible Windows x64 Native AOT package, hashes, clean-extraction smokes, and a fresh
  signed-off manual packaged campaign make D-155's finish line executable. All nine cards
  are now Approved; implementation may begin at V1-01. Expected final save v99 and product
  1.0.0 remain implementation results, not changes made here. No engine or save-format
  change yet.
- 2026-07-22: **D-164 companions, factions, and consequences depth is designed.** V1-08
  is Approved but not implemented. Physical threats judge fellows honestly, following
  fellows escape visible marked ground while a held one keeps the ordered risk, and a
  mortal body safely refuses a shot through it. The Crofter's grain road adds the second
  guest arc and the Stead-to-Town edge through the bond, levy, scheduled cart, Stores,
  Regard, and facts; two bounded Aegis memories carry outcomes without giving strangers
  false knowledge. Beasts gain camp warmth and recognition. Scar and scar-mended facts,
  the fitted brace, tier-scaled Toll fill, sheet presentation, the closed door, and the
  long count close the consequence catalog. Default and opt-in companion pilot policies,
  expected v98 bump, focused acceptance, exclusions, and the full sweep contract live in
  `design/plan-1.0.md`. Tranche 8 remains unchecked until built and verified. No engine or
  save-format change yet.
- 2026-07-22: **D-163 combat and magic depth is designed.** V1-07 is Approved but not
  implemented. Exact opposite-cell flanking runs both ways, the current family verbs
  close the launch movesets, three enemy follow-ons pay the alarm, warder, and sweep
  gaps, and all five martial skills gain level-6 questions. The rune-tongue brings two
  readable hostile workings and deterministic Will resistance; Severing and Mending
  bring the player catalog to seven; Spellcraft gains level-2 and level-4 questions.
  Default and opt-in caster pilot policies, expected v97 bump, focused acceptance,
  exclusions, and the full sweep contract live in `design/plan-1.0.md`. Tranche 7
  remains unchecked until built and verified. No engine or save-format change yet.
- 2026-07-22: **D-162 character and activity breadth is designed.** V1-06 is Approved
  but not implemented. Four end-appended skills close the roster at eighteen: Alchemy
  grows through self-brewing, Athletics spends stamina on a two-cell rush, Stealth spends
  two honest turns on deterministic soft tread, and Larceny takes household crime and
  fencing while Sleight keeps pockets and locks. Three existing pasts gain the approved
  creation hooks; the four new skills and Sleight receive level-2 knack questions; the
  default pilot stays crime-free while an opt-in rogue route proves the criminal lane.
  Exact costs, awareness rules, causal-clock protection, expected v96 bump, exclusions,
  and the full sweep contract live in `design/plan-1.0.md`. Tranche 6 remains unchecked
  until built and verified. No engine or save-format change yet.
- 2026-07-22: **D-161 town and economy depth is designed.** V1-05 is Approved but not
  implemented. One per-world guild loft costs 80 coin after the primer, bond, and even
  town book, providing a bed, reading desk, and crossing-safe strongbox; a 120-coin
  fitted workshop adds the launch masterwork commission without combat-stat gear. The
  law-day lists are one 15-coin, three-bout nonlethal tournament per world, paying 45
  and a champion fact on a clean sweep; the primer also unlocks one judicial challenge
  per world. The town-law primer and folk-tales complete the six-book launch shelf behind
  a scalable submenu. Exact gates, pilot evidence, expected v95 bump, exclusions, and the
  full sweep contract live in `design/plan-1.0.md`. Tranche 5 remains unchecked until
  built and verified. No engine or save-format change yet.
- 2026-07-22: **D-160 pacing steering is designed.** V1-04 is Approved but not
  implemented. Authority is limited to explicitly elastic random-deck cards: Press
  guarantees an eligible draw after three quiet nights, and each continuous Space episode
  may suppress one otherwise-successful opportunity without reserving a card. Scheduled
  futures, faction clocks, seasons and weather, durations, player-triggered content,
  combat, and world state remain protected. The five-seed evidence, failure behavior,
  diagnostics, expected v94 bump, and full sweep requirements live in `design/plan-1.0.md`.
  Tranche 4 remains unchecked until built and verified. No engine or save-format change yet.
- 2026-07-22: **D-159 prose variety infrastructure is designed.** V1-03 is Approved but
  not implemented. Fact details, storylets, scenes, and ask-about topics become an
  enumerable surface inventory; fact families gain compatible authored variant bundles,
  validated contexts, pure deterministic selection, and explicit variation budgets. The
  WorldEval audit becomes metadata-aware, with structural failures gating the command and
  distribution findings remaining advisory. Five representative fact families form the
  first composed slice, while existing narrative prose enters as visible Fixed content.
  Tranche 3 remains unchecked until built and verified. No engine or save-format change yet.
- 2026-07-22: **D-158 weather and seasons v1 is designed.** V1-02 is Approved but not
  implemented: one shared seasonal calendar, three climate bands, deterministic
  three-card weather hands, four narrow exposure families, one-tick forecasts, existing
  shelter counterplay, season-gated stead events, two weather cards, and a bargain that
  reads Regard and Shame. The card fixes persistence, presentation, pilot, evaluation,
  and full sweep requirements in `design/plan-1.0.md`. Tranche 2 remains unchecked until
  built and verified. No engine or save-format change yet.
- 2026-07-22: **D-157 design the whole road before building it.** The nine 1.0
  tranches now have stable V1 cards in `design/plan-1.0.md`, with design status,
  decisions, dependencies, approved behavior, acceptance, roadmap associations, and
  exclusions kept together. All nine cards will reach Approved before implementation
  resumes, after which they move through Implemented to Verified in queue order. The
  roadmap remains the status truth; the new plan owns ordered release scope. No engine
  or save-format change.
- 2026-07-22: **D-156 the black tarn is designed.** V1-01 is Approved but not yet
  implemented: a fourth high-fells site with three finite fishing reaches, a permanent
  hook and line, Survival-scaled tarn trout, cooking and town-sale choices, no resident
  monster, and full pilot, snapshot, worldgen, and sweep requirements. The detailed
  contract lives in `design/plan-1.0.md`. Tranche 1 remains unchecked until the feature
  is built and verified. No engine or save-format change yet.
- 2026-07-22: **D-155 the road to 1.0 has a finish line.** The completed sixteen-step
  July sequence now continues through nine ordered tranches: the high-fells capstone,
  weather and seasons, prose variety, pacing steering, town and economy depth,
  character and activity breadth, combat and magic depth, companions/factions/
  consequences depth, then the next full-density region and a release audit. The
  1.0-ready gate requires every tranche complete, the full engine sweep and a fresh
  manual playthrough review green, no known release-blocking defects, current save/help/
  design documentation, and an explicit post-1.0 classification for every remaining
  roadmap line. Open-ended catalog growth no longer makes "done" unreachable. No engine
  or save-format change.
- 2026-07-21: **D-154 the red book and the dark temper: bloom becomes craft.** The
  scrivener's fourth title asks 14 coin, Lore 2, and seven shrine sittings, then
  teaches the bloom-temper for good. The town forge's stable recipe digit opens a
  bounded bench listing every eligible carried piece: one bloom gives one weapon or
  mail piece 10 more maximum wear once, feeds Smithing, changes no combat number, and
  travels with the gear. Bows and cloth refuse. The snapshot marks tempered gear; the
  pilot tempers one highest-value piece, then sells all surplus blooms; journey prose
  and JSON separate crafted blooms from sold ones. Save v90 -> v91. 838 tests green.
  All five v95 journey twins are byte-identical and reach cycle 13 with twelve
  crossings, each tempering one piece and selling 95-115 blooms. Seed 1 replay is
  exact at 24,546 keys, turn 23,137, and nine deaths; worldgen JSON exits 0. The v95
  sweeps are the new baselines. Deferred: more bloom recipes and commissions, a
  broader crafting board when its catalog earns one, the town-law primer and folk-tales,
  and masterwork commissions.
- 2026-07-21: **D-153 the dark iron under wet stone: the fells' own trade.** B4's
  country-goods debt closes. Four finite, visible tarn-iron seams generate on a new
  fells stream, reachable and mostly against wet scree or tarn. `g` works one only
  with an unworn hafted weapon, paying eight exposed turns and one wear for a
  Survival-scaled yield capped at three, plus one under wolf-winter. The town forge
  smelts the whole raw lot for 2 coin at 1:1 and feeds Smithing once; the carriers'
  guild buys blooms at 4 coin each under the ordinary haggle, law, bond, and Held Road
  rules, feeding Commerce once. Raw ore and blooms survive death and crossings. The
  waykeeper, glyph, sidebar, facts, snapshots, journey prose/JSON, pilot, and worldgen
  metric all carry the loop. Save v89 -> v90. 835 tests green. All five v94 journey
  twins byte-identical and all reached cycle 13 with twelve crossings; seed 1 replay
  exact at 24,283 keys, turn 22,932, and seven deaths; worldgen JSON exit 0. The v94
  sweeps are the new baselines. Deferred: blooms as inputs to recipes or commissions,
  further regional goods, a possible fourth fells site, and the shared re-tenanting or
  renewal decision.
- 2026-07-21: **D-152 the laws past the seventh gate stand in the world.** Plan step 14
  closes and D2 flips to [x]. `WorldTwistCatalog` deals exactly one law from tier 7
  onward through the master seed's independent three-item shuffle bag, all three before
  refill and no boundary repeat. The Held Road generates its holder and three sheltering
  waystones, then lays the visible one-coin tithe on completed official road and town
  business only. The Grave Market puts a common truce and a tally at both eligible sites:
  each ground may be settled for half its living yield without the ordinary kill rewards,
  while violence or unbought goods closes both books. The Horned Law protects harts,
  separates and reads their hides through official refusal, the town gate, and the
  peddler's fence, while the town pays its wolf bounty. Facts, shrine and local readers,
  snapshots, crossing reports, and worldgen metrics carry the law. The pilot obeys and
  exploits each one. Save v88 -> v89. Two sweep-found seams closed: cairn and gill now
  route their own clear responses instead of falling through another site's fallback,
  and each tally stands in a generated side alcove so a narrow approach cannot be sealed.
  829 tests green. All five journey seeds reached cycle 13 with twelve crossings, every
  emitted-key twin byte-identical; seed 1 replay exact at 22,843 keys and turn 21,223;
  worldgen JSON exit 0. The v93 sweeps are the new baselines. Deferred: catalog growth,
  law-specific storylet depth, additional readers, and the later coinless flagship law.

- 2026-07-21: **D-151 the laws past the seventh gate: step 14's design gate.** Tier 7+
  takes one twist per world as its recurring freshness structure, independently drawn
  from the world story through a deterministic three-law shuffle bag with no boundary
  repeat. The opening library is the Held Road (one standing faith, waystone shelter,
  one-coin official tithe), the Grave Market (a common truce at the barrow and cairn,
  peaceful settlement bought for half the tenants' possible Essence, violence closing
  both markets), and the Horned Law (harts protected and fenceable, wolves bountied).
  Twists are mandatory mixed-valence world identity, never covenants, and must change a
  player rule, a world response, facts, and multiple surfaces. The D-058 tier-7+ and
  D-131 governance questions close. D2 remains unchecked until D-152 builds and verifies
  the library. Newly found while scoping the build: cairn and gill clearing currently
  fall through another site's fallback response; D-152 will give both explicit routing
  and regression tests before the Grave Market depends on the seam. Next: D-152.

- 2026-07-21: **D-150 the wolf-gill: the fells' third site, the she-wolf, and the great pelt.** B4's density line continued. A scree-walled ravine strewn with the drove-years' bones: one carved gully (connected by construction), denned pockets, the bone-hollow at the deep end, all on the fells' own three-terrain palette with scree stopping feet never eyes. The great she-wolf is the first elite beast: her own kind (GreatWolf, own reads and glyph) but deliberately the pack's own behavior, heavier only in weight (hp 16+tier, jaw +2, pounce +2, posture 6, all additive after the dice). Her yield is the great pelt, once ever, carried like a keepsake: a cold fells camp under it mends whole (the D-146 exposure answered by the country's own apex), plus two extra hides; no coin, no essence, game-honest. The gill's coin is a lost drover's pack among the bones. The D-149 fang and the D-146 draw-off both cover her kind. Pilot: third errand behind the same armed gate, taken last. Save v87 -> v88. The sweep caught a live latent bug (the D-138 seam's second face): trade-places moves never gathered the traded-onto ground, so the pilot's mule parked on a herb spot spun a world's key budget; both trade branches now gather, with a regression test. 820 tests green (new WolfGillTests; the pack-fall test filtered honestly). Twins byte-identical on all five seeds; the gill cleared in all twelve worlds on every seed; sim replay exact; worldgen purity exit 0; v92 sweeps are the new baselines. Deferred: re-tenanting with the respawn question, the she-wolf in song, more fell sites toward full density.
- 2026-07-21: **D-149 the wolf-winter: the frontier's season, and news that is not about the bearer.** B4's frontier-news deferral paid, and D-143's "news of deeds beyond crime" opened: the calendar's first news with no bearer in it. Every world's hard winter now schedules the tops' turn as it lands (announced: the fells are whitening already); the wolf-winter sits three ticks on the fells, the pack biting one point deeper under it (bite and pounce, additive after the dice so the rng stream never moves) with the climb's narration and the waykeeper's fells topic reading the season; its word walks to town two ticks later at freight speed and the hidemonger pays a coin over the chalk while it stands (scarcity's label saying why); the lifting's word takes the same road home before the ordinary chalk returns. The frontier's bargain restated: most dangerous exactly when hides sell dearest. Save v86 -> v87. 816 tests green (new FellWinterTests: the whole choreography plus the fang proven by twin games on identical dice; ScheduleTests updated: the winter's landing now loads the calendar). Twins byte-identical on all five seeds and byte-identical to v90 outright; a sim prefix-replay probe (the snapshot grown FellWinterStands/WolfWordStands) confirms the season stands mid-journey, the bytes holding only because the pilot absorbed the fang and sold outside the word's window on these seeds; sim replay exact; worldgen purity exit 0; the v91 sweeps are the new baselines. Deferred: the stead hearing it in the keeper's news, frontier goods beyond hides, seasons read by the pacing layer.
- 2026-07-21: **D-148 letters and Lore: the 14th skill, the scrivener, and the first books.** Plan step 13 lands before any town archives deepen, paying D-005 and D-016's "Lore (incl. literacy)". Literacy IS Lore 1 (settled in Q&A): the 14th use-grown skill, mundane script refusing the eye below it, graven script canonized as NOT letters (the stones answer the warmth behind the eyes; nothing shipped regresses). Lettered pasts: scribe's-ward and hedge-healer seed Lore 1 via banked uses; the other five feel the gate. The scrivener joins the market plot as the seventh towner and the town's bookish anchor: 2-coin sittings bank 2 Lore uses each (the D-141 school pattern; four sittings are the letters), and the shelf sells three books to lettered hands only. Books read at the shrine on the new 'v' verb, a sitting a turn and a use, rereads feeding nothing; every book pays a concrete keep: the herbal teaches the wort-cunning (draughts steep from 2 sprigs, first book-taught lesson), the bestiary banks the wight Keen (StudyKind, stamped and dulled per D-061), the lay (Lore 2, the growth text) writes a fact, opens a skald topic keyed on the reading, and pays +2 Legend at the next crossing once ever (D-048 honored). The fallen hall's chest seeds a free lay while unread. Sheet re-laid (attributes fold to one row; 14 skills and a deep question fit 24 rows exactly). Pilot walks it all: scrivener errands on the town leg (letters, shelf, copy-work, shared predicates so nothing shuttles) and a quiet-hour errand reading at the shrine; seed 1's journey ends Lore 3 with all three books read. Save v85 -> v86. 814 tests green (new LoreTests; sheet and deep-knack tests updated for the 14th slot; TownTests grown the scrivener). Twins byte-identical on all five sweep seeds; drift healthy (cycle 13 everywhere, deaths 3-6); sim replay exact (seed 1: 21797 keys, cycle 13, turn 20299); worldgen purity exit 0; the v90 sweeps are the new baselines. Deferred: more titles as lanes grow, Lore knacks, the bookshop/archives proper, illiteracy texture on future script surfaces.
- 2026-07-21: **D-147 the fells fill in: the combe's own stone and the high cairn.** B4's two recorded debts paid as one density step. The combe's authored interior lands: GenerateCombe cuts the wolves' ground from the fells' own palette (a bowl of open heath ringed in scree, a black tarn in every combe's low end, outcrops under the quarry's all-nine-open rule so nothing seals), with rock stopping feet but not eyes (D-057), so the bow's lines and the pack's closing both hold on the new ground. The high cairn rises as the fells' second site and the first new site kind since the town (SiteKind.Cairn, Terrain.CairnEntrance, both end-appended): a mostly authored creep-and-chamber under the kerb, wights at the frontier's price (10+tier hp, two to five by tier), the cist's grave-gold through the standard chest machinery, and a graven word set inline (the D-091 loop predates the fells' growth). All new draws on fresh streams after every D-146 draw, so prior fells hold to the tile and the tops only gain a door. Perceivable at every seam: entrance prose live and cleared, glyph and sidebar hint, a site fact, and the waykeeper's fells topic grown its cairn sentence inside the one topic. Pilot: FellTripWanted is combe-or-cairn behind the same armed gate; the fells loop takes the cairn after the combe; the generic site machinery does the rest without a new branch. Save v84 -> v85. 808 tests green (3 new FrontierTests; CrossingTests hardened onto per-site wights). Twins byte-identical on all five sweep seeds; drift healthy (12 crossings everywhere, the cairn cleared in every world from cycle 1, deaths 2-8, the hunt 540-592 hides); sim replay exact (seed 1: 21698 keys, cycle 13, turn 20217); worldgen purity exit 0; the v89 sweeps are the new baselines. Still open on B4: more fell sites and kinds toward full density, then further regions. Next: plan step 13 (literacy/Lore) or more B4 depth.
- 2026-07-21: **D-146 the high fells: the frontier opens.** B4 begins and flips to [~]. The structural bill paid once: OnRoad's bool becomes Area (Valley/Road/Fells) on bearer, sites, folk, and beasts, with OnRoad kept as a derived legacy read so nothing old moves. The fells: a third overworld off the road's north shoulder behind a drovers' track (deterministic edge scan, everything off one derived seed after every existing draw), treeless heath and way-walling scree (the first unwalkable rock) and rare tarns; the third region drawn third on the regions stream, fact written, crossings narrated by name, the waykeeper's fourth topic pricing the climb. No roof up top: the camp is the only rest, and the fells' cold quarters its mending. The moor-wolf holds the ground: game to the harvest (hide and meat, no purse, no essence: the frontier pays down the hunt ladder into the town's counter) and a foe to the fight, pack-read by three rules (closes from far, commits in company, works prey in shifts with the bite capped) with the pounce as its telegraphed reach-2 tell; a pack that kills draws off to its ground with the kill, so re-entry is never a door ambush. Pilot: climbs when armed (bow, iron, armor), fights the pack bow-first, camps, picks the high herbs, sells the hides in town; two lessons kept: wilds sites dispatch by tenant not kind, and the runner attributes a death on the entering key to the site behind the door or the give-up budget never sees a threshold-killer. Save v83 -> v84. 805 tests green (12 new FrontierTests; RegionTests and BarrowTests hardened to three countries). Twins byte-identical on all five sweep seeds; drift healthy (12 crossings everywhere, deaths 3-7, the hunt doubled to 540-587 hides, 14-17 climbs, all salt sold, the teller's book reading the new heat with more calls for air on every seed); sim replay exact (seed 1: 20882 keys, cycle 13, turn 19478); worldgen purity exit 0; the v88 sweeps are the new baselines. Still open on B4: the combe's authored interior, more fell sites and kinds, then further regions. Next: plan step 13 (literacy/Lore) or B4's depth.
- 2026-07-21: **D-145 the teller's book: the pacing layer opens read-only.** Plan step 12 lands and D1 flips to [~] (the read-only half built; the steering half stays open, gated on this book's evidence). The Storyteller (Pacing.cs) watches every coarse-tick night from the tick block's tail: it makes its editorial call BEFORE the night's events from carried state alone (temperature at 4+ calls Space, the run needs air; three heatless ticks call Press, the run coasts; else Steady), then observes what the causes did (a death since last tick heats 3, a claimed night 2, a raid by its take) and cools a point a night. The audit output is the disagreement counters: cards dealt through a call for air, and pressed nights that stayed quiet. The teller draws no RNG, writes no facts, and narrates nothing, so replay cannot feel it and NO save bump is owed (v83 holds); the book spans the run (the carry resets per world with the World bucket) and the journey report/JSON carry the ledger, the plan's "log what it would have done across the sweep seeds" delivered literally. 794 tests green (9 new PacingTests). Twins byte-identical on all five sweep seeds AND every sweep byte-identical to the v86 baselines except the single added teller line (the strongest read-only proof available: not one key or number moved); sim replay exact (seed 1: 18349 keys, cycle 13, turn 17075); worldgen harness purity exit 0; the v87 sweeps are the new baselines. First finding in numbers: Press 9-23 per run against Space's 1-7, with 6-15 pressed nights unanswered: the run coasts once the camps clear, and the pressure half is where steering will earn its keep. Next: B4 (the frontier region) per the session's marching orders, then step 13 (literacy/Lore).
- 2026-07-21: **D-144 the caravan leg: salt on the cart, and the margin earned by the walk.** Plan step 11 closes and B3 flips to [x]. The first buy-to-resell trade (D-025's productive capital at its smallest, D-124's stock-growth deferral cashed): the cart's fourth digit sells salt at 5 coin from a per-world stock dealt by the tier alone (two and the tier, capped at six, no RNG), and the provisioner's second digit buys it at 8, a lot like the hides (TownHaggle, the guild's mark, the law's teeth, and D-143's road-spoken distrust all riding it), Commerce fed per lot with the margin as the walk. Salt is freight on the body, crossing death and the waygate; the pilot loads the cart before the road trip, carries east, and sells inside the market leg (predicate-matched at walk, talk, and counter). Save v82 -> v83. 785 tests green (7 new CaravanTests; the cart's board grown to four digits in PeddlerTests). Twins byte-identical on all five sweep seeds; all five moved off v85 as predicted, drift healthy (12 crossings everywhere, deaths unchanged, 58-63 sacks loaded and all sold per run, town lots up ~10 with seed 99's Commerce at level 3); sim replay exact (seed 1: 18349 keys, cycle 13, turn 17075); worldgen harness purity exit 0; the v86 sweeps stand as baselines. Next per the plan: step 12 (D1, the pacing layer read-only) or B4 (the frontier region).
- 2026-07-21: **D-143 the named countries and the word that walks.** Plan step 11 opens (B3's first half): the region becomes an entity, and news travels between regions at freight speed. A Region is identity, not a map (id and name, two per world: the home valley and the road's high country), paying the box D-049 left open; NameGen grows its region kind (world openers over the land's own closer pool, disjoint from every other kind's, spoken with "the" in front, the road's rerolled against the valley's), drawn on its own stream after every existing draw so all prior placement holds byte-identical. The naming is perceivable everywhere it touches: region facts at generation, the road's crossings narrated by name both ways, the stead topic pointing east into the named country, the town topic claiming its own. The word that walks rides D-132's calendar: an unwelcome name (shame rung 2) puts word on the road, due two ticks out with the drovers, cancelled by restitution before they roll (narrated both ways); landed, the town's haggle coin dies while home shame stands beside the word, and squaring the stead's book restores the trust the moment it is done (the road carries the mending on the clock it carried the wrong). Westbound the mirror: a name at the town's barred rung is freight, due two ticks, cancelled by pleas back under the rung; at the valley's end it is talk alone (the doors say it is being said; the stead's book never moves for hearsay: D-142's separate books hold even sharing news). The warden names and prices the incoming word. Save v81 -> v82. 778 tests green (5 new RegionTests, 6 new NewsTests). Twins byte-identical on all five sweep seeds AND all five byte-identical to the v84 baselines outright (crime-gated, pilot-unexercised, test-covered); sim replay exact (seed 1: 17988 keys, cycle 13, turn 16801); worldgen harness purity exit 0; the v85 sweeps stand as baselines. B3 flips to [~]. Next: step 11's second half (D-144, the caravan leg: the peddler's stock growing, the buy-to-resell arbitrage).
- 2026-07-21: **D-142 the town's depth, second half: the warden's book and the plea.** Plan step 10 closes, and B2 flips to [x]. The law D-140 surfaced as talk is machinery now: the light hand works inside the wall ('p' beside a towner draws the lift's dice, the clean take writing the town's own secret), and a caught hand goes into the warden's book, not the stead's shame, because a stead's suspicion has no eyes out east: the town is the fourth faction (FactionId.Town) and the book is its infamy count. The teeth are two and legible: one standing mark kills the haggle coin whole (no counter trusts a booked hand's scales, Commerce level and guild mark alike), two marks shut every counter in town, and the moot itself never bars, because a law you cannot answer is a wall. The plea is the answer: one always-honest digit at the warden (listed only while a mark stands: his board stays counter-free when the book is even), one mark answered per fine, and Persuasion, the 13th skill, is seeded there: fed one use per plea that truly moved the book, cost-gated by the fine itself, its level shaving the fine from 6 toward the floor of 2. Making right in the wronged hand stays its own ledger (the repay clears the hand, never the book: the mark is the moot's to rule through). The moot topic reads the book true; the sheet folds its attributes to three columns of three, buying exactly the row the 13th skill needed. The parking-lot question "town law vs stead shame" resolves: one infamy machinery, separate books per faction. Save v80 -> v81. 767 tests green (7 new TownLawTests: both lift outcomes with their whole ledgers held across seeds, the repaid hand leaving the book standing, the dead haggle, the barred counters with the moot still hearing, the plea's mark-by-mark arithmetic going home even, the practiced pleader's shaved fine, the topic reading the book; the snapshot skills string and the deep-knack exclusion grown the established way). Twins byte-identical on all five sweep seeds AND all five byte-identical to the v83 baselines outright (crime-gated, pilot-unexercised, test-covered: the D-135 precedent); sim replay exact (seed 1: 17988 keys, cycle 13, turn 16801); no worldgen change, so no harness run owed; the v83/v84 sweeps stand as baselines. Next per the plan: step 11 (B3 caravan/arbitrage and region machinery) or step 12 (D1 the pacing layer, read-only).
- 2026-07-21: **D-141 the town's depth, first half: the forge's school and the guild's oath.** Plan step 10's first bite. The stitch now deals the town's four working institutions in every town (market, moot, forge, guildhall; the last two plots still draw and shuffle), and the cast grows to six: the forge-smith at his forecourt and the guildmaster at the hall's cut door. The forge is the school D-135 promised: a sitting files the bearer's most worn piece exactly as the home bench does and feeds Smithing the same honest way, but for coin (3c the sitting, no slates), the town's copy of the verb cost-gated by price where the bench is gated by the walk home; the smith also teaches the drawn temper (lesson 5, 14c), gated on Smithing 1 and never waved by the stead's regard, its keep +2 wear off every sitting at any bench in any world. The carriers' guild is the town's first faction with a real ledger on the bearer: the bond sworn once per world (10c down, gated on Commerce 1: the guild bonds no name the market has not learned), written as a guild fact, and worth a flat coin on every town lot beside Commerce's own level; the moot-warden's guild topic now points at the open hall, and both new boards read their state true (D-041). The pilot walks it all live: the road leg detours for worn iron and unsworn proven names, the school and the bond clear their own errands, and the journey report grew its school line. Save v79 -> v80. 760 tests green (6 new TownDepthTests: the school's arithmetic, its two refusals, the lesson's gate and its keep measured at the wheel, the bond's whole ledger, the mark on the lot, the shared nine; TownTests' cast check grown to six; the stead's stock-taking taught to count only the valley's own lessons). Twins byte-identical on all five sweep seeds; drift healthy (cycle 13 and 12 crossings everywhere, 165-192 forge sittings per run with Smithing at level 7-8, the bond sworn in 7-9 of 12 worlds, deaths 2,3,2,1,1); sim replay exact (seed 1: 17988 keys, cycle 13, turn 16801); worldgen harness purity exit 0 with the new folk struck to {person} in the skeleton audit; the v83 sweeps are the new baselines. Next: step 10's second half (the law's book on the bearer, Persuasion).
- 2026-07-21: **D-140 the market town: B2's first cut.** Plan step 9 lands: the far country the wayhouse signposted stands behind its own gate at the east road's end. The town is the world's first peopled site, not a third overworld (Mode.Site gives a walled interior all its rules for free, and B3 is where region machinery gets generalized after B2 forces it into existence): a 46x26 walled map of authored chunks stitched per seed, the Daggerfall lesson at slice scale (market row and moot yard in every town, house-lanes, well square, gardens, and a sealed guildhall drawn and shuffled from a library on the town's own stream, so towns share their parts and never their arrangement). NPCs gained site placement (Npc.SiteId, NpcsHere now area- and site-aware, the bump opening talk inside sites): four townsfolk stand at their stalls. The market is the arbitrage ladder's third rung (hides 5c over the cart's 4c and the bench's 3c; herbs 6c over the stillroom's 5c and the bench's 4c; the market loaf 4c under everyone, with the walk east as the real price), and Commerce, the 12th skill, is seeded there: fed one use per lot sold above the valley's own price (cost-gated by construction: the margin is the walk), its level added flat in coin to every town lot. The moot-warden speaks the law and the guild as topics (the web surfaced in a mouth; its machinery is step 10's). The waykeeper's far-country topic now names the town at its door. The pilot sells the road's yield at the market every world and the journey report grew its market line. The 24-row sheet was re-laid dense to hold 12 skills and a deep question (found by the suite, the honest way). Save v78 -> v79. 754 tests green (8 new TownTests: the stitch deterministic across 30 seeds with every stall reachable from the gate, the gate crossing both ways, the market's arithmetic with Commerce fed and the empty pack counting nothing, the valley benches feeding none, the practiced tongue's coin, the loaf with the no-slates refusal, the warden's lawful board inside the shared nine, the replay; NpcTests and DeepKnackTests and the snapshot skills string hardened onto the new machinery). Twins byte-identical on all five sweep seeds; all five moved off v81 as predicted for a pilot with a new errand, drift healthy (cycle 13 and 12 crossings everywhere, the market walked 10-12 times and 20-24 lots sold per run, Commerce at level 2 in every run, deaths 2,2,1,0,4); sim replay exact (seed 1: 17357 keys, cycle 13, turn 16432); worldgen harness purity exit 0 with the town in the site census and {town} struck in the skeleton audit; the v82 sweeps are the new baselines. B2 flips to [~]. Next: B2/C town depth (plan step 10: guild and law as factions with ledgers, smithing lessons, Persuasion) or B3's region machinery.
- 2026-07-21: **D-139 the audit that found its overflow, and the granary's flood teeth.** The two A3 v1 follow-ons (D-134) cashed as one small decision before B2 leans on the boards they guard. The topic-budget audit ran and its suspicion was right, one door over: a full world fills all nine general villager digits by itself (stead, raids, shrine, arch, mound, ring, wanderer, songs, and D-133's news made nine), and since every villager door is a named one carrying at least one offer digit (steadholder two, woodward and herbwife one each), the news topic could push a board to ten entries on nine keys, the bench digit unreachable in exactly the fullest worlds. The fix follows D-134's own logic to its end: the season's news moves off the villagers' shared nine entirely, to the shrinekeeper's door, the one board with room (three static topics, no offers), where the seasons are watched for a living; a new fullest-world guard test builds the overflow state deliberately (tier 2, carried song, news landed) and walks every talking door, pinning the woodward at exactly nine and everyone at or under. And the granary's teeth: a washout that comes down while the granary stands takes nothing, the staddle stones holding the grain above the brown water, with the flood still claiming its night whole (no raid rides drowned fords) and a new washout_stood fact read at the keeper's door crediting whose boards held. Save v77 -> v78 (talk digits sit differently wherever season news stood; a granaried washout keeps its measure). 746 tests green (2 new: the flood turned away read through the real surfaces, the fullest-world audit; 2 hardened onto the news topic's new door). Twins byte-identical on all five sweep seeds; all five moved off v80 only inside the key stream (equal counts, all 191 report lines byte-identical per seed: the pilot recomputes talk digits live, so the same buys press shifted keys); sim replay exact (seed 1: 16926 keys, cycle 13, turn 16025); the v81 sweeps are the new baselines. No worldgen change, so no harness run owed. Checked off the A3 v1 follow-ons. Next: B2 (the market town, plan step 9).
- 2026-07-21: **D-138 the east road: travel as play.** Plan 2026-07's B1, the multi-region lane opened and the generator's first growth since D-137 charted it (the harness confirms purity across the growth, exactly the sequencing the plan asked for). The world gains a second overworld: the east road (72x16, its own derived seed, the valley layout untouched to the tile) behind a RoadMouth on the valley's east edge, crossed both ways with '>'. All four of B1's named pieces ship: camping ('m', the D-006 box opened: the kill cooked into rations at the fire, the supper's ration buying 6+2/Survival mending with the skill fed only when the night healed, 8 real turns passing, cold camps legs-only), weather exposure (the road's own sky per coarse tick: rain and cold take the step's stamina regen, halve the camp's mending, and the cold wind refuses a supperless camp), an encounter site seeded along the way (the half-way glade, a road-tenanted wilds with its own map id), and supplies (road-verge herbs, and the wayhouse at the far end: keeper, road bread at the cart's price, a 4-coin bed as the full rest without the shrine's essence counting, and the far-country topic signposting B2). The two overworlds never bleed (area-gated sites, people, herbs, pony, ledgers; NearHouse and the crime family stay the stead's; a beast at the side takes the mouth, one left grazing waits; death and the crossing come home). The pilot walks it all live and the journey report/JSON grew road tallies. Bonus: a latent D-100 seam found and closed (the ridden stride never ran ground pickups on its first tile, so a rider could orbit a herb forever; found live on seed 88888, fixed with a regression test). Save v76 -> v77. 744 tests green (9 new RoadTests, 1 stride regression, 3 suites hardened honestly onto the second overworld). Twins byte-identical on all five sweep seeds; all five moved off v79 as predicted, drift healthy (cycle 13 and 12 crossings everywhere, the mouth taken 12 times per run, 1-4 nights camped, deaths equal or fewer on every seed); sim replay exact (seed 1: 16926 keys, cycle 13, turn 16025); the v80 sweeps are the new baselines. Checked off B1; camping moves into the wilderness-living line as shipped. New tracked: road encounters beyond the glade, deeper exposure, wayhouse prices joining B3's spreads. Next: B2 (the market town, plan step 9) or the A3 v1 follow-ons.
- 2026-07-21: **D-137 the worldgen evaluation harness.** Plan 2026-07's D4, built on the plan's own timing rule: before the generator grows again (B1, B2). Two halves: WorldEval in Core (pure reads over a generated World, no I/O, no RNG, no Game: the per-world measure, the tier summary, the name-struck prose skeletons, the repetition audit, and an FNV digest) and `aegis worldgen` in the CLI (batch generation straight through WorldGen.Generate across --seeds and --tiers; the prose report, `--json` for CI, and `--dump` as the generate-then-curate feed D3's repetition audit was corrected onto at adoption, so the two share one harness as planned). Every world generates twice and the digests must match, wired to the exit code, so a generator that grows a hidden input fails loud in the same run that charts it. The first 240-world run (30 seeds, tiers 1-8): purity holds everywhere; the tier bands stand exactly as built; settlement names 30/30 distinct, world names 29/30 (the collision honest: direct generation passes no takenNames); the story mix is visibly uneven at depth (gold-rush 12 of 30 at tier 6+ against war-of-faiths' 3), the harness's first real finding, recorded to watch; and the skeleton ledger fixes the baseline D3's fact-keyed fragments will be measured against (220 distinct skeletons over 10220 surfaces). No save bump (v76 holds: pure new code, Game untouched). 734 tests green (6 new WorldEvalTests). Twins byte-identical on all five sweep seeds and all five byte-identical to the v78 baselines outright; sim replay exact (seed 1: 13395 keys, cycle 13, turn 12550); the v79 sweeps are the new named baselines. Checked off D4. Next: B1 (the road, plan step 8) or the A3 v1 follow-ons.
- 2026-07-21: **D-136 the crossing split audited, and the builder's echo.** Plan 2026-07's A4, the stead layer's closing check, and the parked legacy-echo question resolved with it. The audit found the layer split clean by construction (the D-089 derive-from-facts discipline): the calendar, the deck's stream and card guards, all five works and both derived caps, the raids' clock, the stores, the watch, and the ledgers all reset with the World bucket; the Smithing craft, the gear's wear, and the satchel's vials cross with the bearer (the cap gates drawing, never carrying, so over-cap vials are earned consumables, not a leak). One hygiene fix: the crossing's refill reads StoresMax, not the bare constant. The echo: story only, never mechanical. A stead with works standing presses one legacy/builders_hand fact into the next world by the patronage traces' road, hushed-gated (the building was the bearer's open deed), never compounding, read by one NearHouse storylet as founding talk, with a whole-ladder text variant. Nothing pre-built, no coin, no regard. No save bump (v76 holds, the D-061 precedent: no new digits, no RNG moved). 728 tests green (3 new SteadFacilityTests: the echo heard through the real NearHouse surface with the works gone and both brims bare, the bare stead leaving none, the hushed name stilling it). Twins byte-identical on all five sweep seeds and all five byte-identical to the v77 baselines outright (fund-gated, the pilot never talks); sim replay exact (seed 1: 13395 keys, cycle 13, turn 12550); the v78 sweeps are the new named baselines. Checked off A4; the legacy-echo parking-lot entry closes. Next: D4 (the worldgen evaluation harness, the plan's step 7) or the A3 v1 follow-ons.
- 2026-07-20: **D-135 the ladder's second rung: the stillroom's wing, the smithy bench, and the Smithing seed.** Plan 2026-07's A3 v2, closing the stead half of the sink ladder at its planned five works. Two entries join the steadholder's bench (appended, so the older digits hold, D-041). The stillroom's new wing (35 coin): the satchel's draught cap rises 2 to 3 while it stands, one derived property (DraughtCap off the event fact), so the herbwife's pot, the Stillcraft rest-steep, and the guest's spent vial all read the deeper rack for free. The smithy bench (45 coin): the one work whose standing digit becomes a verb; each sitting files wear off the bearer's most worn piece (4 base, +2 per Smithing level) and each sitting that truly moved iron counts a use of Smithing, the 11th skill, seeded at home exactly as the plan asked (its proper school is B2's town lessons). The smith's wheel sink stays honest: the bench is slow, one piece a sitting, priced in the turns the raids' clock keeps counting, while the wheel stays instant and whole for coin. Effects derive off the funding facts (the D-089 shape: no reset code, the crossing takes both works; the craft is the bearer's and crosses). Regard once per work (D-131's guard). Save v75 -> v76. 725 tests green (3 new SteadFacilityTests: the third vial drawn through the real keys, the filing and the seeding, the crossing taking the bench but not the hands; the nine-digit guard now covers the steadholder's bench; two enum-growth hardenings). Twins byte-identical on all five sweep seeds and all five byte-identical to the v76 baselines outright (talk-gated, pilot-unexercised, test-covered); sim replay exact (seed 1: 13395 keys, cycle 13, turn 12550); the v77 sweeps are the new baselines. Checked off A3 v2; the sink-ladder line's stead half is whole. Next: A4 (the crossing split audit) or the A3 v1 follow-ons.
- 2026-07-20: **D-134 the stead's works: the facility ladder's first rung.** Plan 2026-07's A3 v1, the stead half of the D-025/D-036 aspirational sink ladder begun. The steadholder keeps a bench now, one talk digit ("The stead's works") opening a trade menu on the woodward's D-071 pattern, three works always listed with state-read labels (D-041), each funded once per world and each modifying a system that already runs. The palisade (40 coin): every greedy raiding night, the mustered ones included, is blunted to a plain measure at the timber, its own narration; the plain raid was never its business, so the war still bleeds and the camp stays the exit. The watchtower (30): the watch turns its nights without eating, so it can no longer bare the stead guarding it. The granary (25): the lofts' brim rises two measures; recovery, levy answers, and the far fields fill to the new brim, and the winter lands on a buffer instead of a levy. Effects and StoresMax derive straight off the event facts the funding writes, so replay rebuilds the works and the crossing clears them with the World bucket, no reset code; regard pays exactly once per work (D-131's guard). The shared nine digits held by the steadholder ceding "The season's news" (the steadholder makes that news; the gossip lives at the other doors) and "The wanderer" (the well's talk; one test repointed). Snapshot gains the three works and StoresMax. Save v74 -> v75 (steadholder digits that fell dead now move coin, facts, and the raids' arithmetic). 722 tests green (7 new SteadFacilityTests walking the real key surface). Twins byte-identical on all five sweep seeds and all five byte-identical to the v75 baselines outright (the pilot never talks: the works are talk-gated, pilot-unexercised, test-covered, the D-107/D-124 precedent); sim replay exact (seed 1); the v76 sweeps are the new baselines. New tracked: A3 v1 follow-ons (the topic-budget audit; granary teeth against the washout). Next: A3 v2 (stillroom extension, smithy bench).
- 2026-07-20: **D-133 the season deck: the stead's own news on the tick.** Plan 2026-07's A2, dealt on D-132's calendar. On any tick night no future has claimed, a per-world stream may deal one card (one-in-three): four cards open the deck, each once per world, guarded by the fact it writes. The far fields (fortune: a measure back, bread eased, a levy liftable, war-time only), the drovers (two faces: a measure sold at a hill price, coin in the stead's box, dearer bread on the bearer's board), the fords washout (foreshadowed weather: the river read one tick, the flood the next through the calendar, claiming its night whole, baring lofts by water, calling the levy), and the banns (the calendar's first cancellable promise: the feast spends a measure on purpose, or is put off with its own narration if the season eats the lofts below the line; the wedding deliberately does not claim its night, since the dens do not check the banns). Two new omen readers (the-river-read, the-banns-heard) live in the warning gaps, and "The season's news" talk topic reads the latest season event after the fact, paying the D-132 follow-on for hard_winter/muster_broken readers. Save v73 -> v74. 715 tests green (9 new SteadDeckTests including an organic-deal proof; nine choreographed tick tests hold the deck via a new test hook, staying about the cadence they test). Twins byte-identical on all five sweep seeds; four of five differ only inside the key stream (equal counts), seed 88888 drifts honestly (2 fewer deaths); sim replays exact (seeds 1, 7, 2024). Live: every card deals and every leg fires (washouts land 6-9 of 12 worlds, weddings 1-5 with one put off, drovers 4-6, far fields 1-7, all three readers speak); the news topic is talk-gated, pilot-unexercised, test-covered (the D-109/D-111 precedent). The v75 sweeps are the new baselines. New tracked: A2 follow-ons (deck growth as catalog work; a card reading the bearer's own deeds). Next: A3 v1, the facility ladder's first cut.
- 2026-07-20: **D-132 scheduled future facts: the world learns a calendar.** Plan 2026-07's A1 lands, the stead layer's opening move. ScheduledFact (Schedule.cs): futures pinned to the coarse tick with foreshadow, cancellation, and hold conditions, replay-rebuilt, cleared at the crossing; a night a future claims is claimed whole. Two first uses: the hard winter (every valley, tick 3-5 from the world's own seed, foreshadowed one tick ahead with an omen fact and the-signs-read storylet living in the gap, two measures out of the stores so bread rides the raids' own bump, the levy callable, the lofts bareable with the watch stood down, nothing cancels it) and the dens' muster (the cull that teaches dread sets the answer, announced at once and read from the raids topic, due two ticks out, held while the bearer stands in the camp, fired as a greedy raid, broken by the camp emptied first, the fires going out one by one). Save v72 -> v73. 706 tests green (8 new ScheduleTests; three tick tests hardened onto the new season). Twins byte-identical on all five sweep seeds; all five moved as predicted, drift healthy (12 crossings everywhere, deaths identical on three seeds and +2 on two, seed 7 byte-identical in keys with only narration moved); sim replays exact (seeds 1, 7, 2024); the v74 sweeps are the new baselines. Live: the omen speaks in 10-11 of 12 worlds, the winter lands in 9-10, and the muster is set and broken in all 12 (the pilot clears camps inside two ticks; the mustered raid is pilot-unexercised, test-covered). New tracked: A1 follow-ons (the deck on the calendar, the peddler restock, after-the-fact talk readers, the calendar in the Snapshot). Next: A2.
- 2026-07-20: **D-131 the July 2026 plan adopted and wired in.** Design-only session. The research sweep's plan (`design/plan-2026-07.md`, distilling research/11 Daggerfall, research/12 KDM's settlement phase, research/13 the procgen survey) is adopted with its five user decisions standing: multi-region world scale at valley density, the facilities version of the stead layer, all four lanes with the stead layer first, a market town as the first new region, and NG+ freshness as worlds that differ in kind. New tracked here: the stead-layer section (A1 scheduled future facts, A2 stead event deck, A3 facility ladder in two cuts, A4 crossing split, A5 named folk parked), the multi-region section (B1 the road, B2 the market town, B3 region machinery, B4 later regions), and the pacing/worldgen/NG+ section (D1 pacing layer, D2 twist library, D3 prose variety, D4 evaluation harness); the sink-ladder, weather, region-entities, and camping lines annotated to their lanes; the phase map now points at the plan as the sequencing story. Six questions joined the parking lot (named folk depth, legacy echo scope, town law vs stead shame, twist governance and its reconciliation with the D-058 tier-7+ question, pacing authority, generator-version pinning). Three plan corrections folded in at adoption: camping is an open box that lands as part of B1, not an existing verb; the generator-freeze principle is only half honored today (save bumps re-deal live worlds; true pinning parked for before any save-format freeze); and D3's repetition audit cannot ride the journey sweep (talk-gated prose is pilot-unexercised by design) so it shares a generate-then-curate harness with D4. Plus one guard: a facility pays regard once, never recurring (the D-109 no-profit discipline extended to purchases). Next: A1.
- 2026-07-20: **D-130 the Long Siege: the sixth spine, and the fen-leaguer gets its story.** The parking-lot deferral tracked since D-121 closes: the sixth template binds to the one deep site with a whole identity and no spine, D-057's fen-leaguer, "the siege that outlived its object." Eligible tier 6+ where the leaguer stands. The accepted history is the stead's penned-thing tale told as gratitude (something old and hungry walled under the holm, held by the falling stones: a watch the stead never had to post); the holm's bare turf flips it twice over (nothing was ever penned: the besieged were the stead's own founders, out across one winter's ice, and the tale was theirs, told so the siege's setters would never come asking, so the stead has spent its history thanking the works set to starve its own grandmothers). The fisher's whole-life wanting voices act 1 against the tale (promise lift_the_leaguer, 3 essence settling, truth-indifferent); the lifting with the truth in hand opens "The ice and the tally" (carry the tally down, founding_carried, the gratitude turned inheritance; or leave the founders their fear, fear_stands plus withheld mere_truth, the fifth writer into the D-118/D-120 silence pipe with its own retelling arm); without the truth, plain lines and the tale explains the quiet alone. Full-contract spec added as world-story-templates.md §9. Save v71 -> v72 (tier-6+ worlds draw among six and re-deal; tiers 1-5 untouched by construction). 698 tests green (9 new SiegeTests on master seed 2's sixth world; one LeaguerTests texture test hardened onto a non-siege world, since the evidence outranks the bare-holm line by design). Twins byte-identical on all five sweep seeds; all five moved off v72 as predicted, drift healthy (cycle 13 and 12 crossings everywhere, death counts identical, seed 7 +2839 keys from a longer late-world story); sim replays exact (seeds 1, 7, 2024); the v73 sweeps are the new baselines. Deferred: the seventh template (the dying god's succession); D-025 economy hooks for the opened mere.
- 2026-07-20: **D-129 the guard war's coda: the board-check and the drilled bind.** The two kinds the second bars were built waiting for (named at D-125 and D-126) ship together. The shield-carl's board-check is the field's first pressure verb aimed at the bearer's guard and not the blood: at arm's length, a quarter of its turns, the whole board squares as a telegraphed blow at the bearer's cell, landing as thrown mass at the charge's tier (3) with no dice and no wound, riding D-126's stance-and-commitment reading, dodged by feet, and met cleanest by the parry (a met check hands the carl's mass back for the full 4: verb and answer point at each other). A sundered board has no check, and the board never leaves its line, so the archer's windows stay the seax's. The drilled thegn answers being parried: its met cut gives only half its force to the bind (2, not 4) and shoves a point back through the crossed iron; the parry stays worth its turn (bloodless, still teaching), and the feint interplay is untouched by construction (a lying mark can never be met, so the bind only answers the honest cut). Save v70 -> v71 (the carl's new draw re-deals fort fights). 689 tests green (9 new GuardWarTests; one RingfortTests seax test hardened past the check, the D-121 precedent). Twins byte-identical on all five sweep seeds; all five moved off v70 as predicted, drift healthy (cycle 13 and 12 crossings everywhere, deaths equal or fewer, hauls near-identical, seed 1 wearing D-126's more-keys-fewer-turns signature); sim replays exact (seeds 1, 7, 2024); the v72 sweeps are the new baselines. Combat's open lane narrows to knacks, flanking, duels, and more second moves.
- 2026-07-20: **D-128 the stead reads its own unease: the secret facts' consumer pass.** The deferral D-127 named the day it tripled the fuel, one small session in the D-109 discipline (narrative and facts, no mechanics: the world reading differently IS the payoff). lifted_purse already had its reader (D-109's two ledgers), so this pays the two tracked open: the bolted dark reads the burgled house off the lane (bright iron on grey wood, the dog kept in, talk that stops at one threshold; NearHouse, once per world, priority 6), with the structural idea being the forbid: shame/housebroken kills the beat for the world, because a lane that has seen a face has a name for its trouble and the beat's whole weight was the lack of one. The heirloom missed grieves the fenced goods to the very hand that sent them (the whittled nothing shaped in the air, the floor up twice, "nobody here would take it" said to the taker; villager Talk, once per world, gated on live shame at zero like the tale carried). Every secret fact a deed can write now has a consumer. Pure perception, no new state, no save bump (v70 holds, the D-109 precedent). 680 tests green first run (5 new in FactConsumerTests, seed 1 probed honest: first burgle clean, second caught). Twins byte-identical and all five sweep seeds byte-identical to the v70 baselines outright (fact-gated on crimes no journey commits); sim replays exact (seeds 1, 7, 2024). New tracked: a consumer for shame/housebroken in its turn.
- 2026-07-20: **D-127 the crossed sill: burglary proper.** Crime's last named verb (D-006), deferred twice (D-107, D-122) for interior plumbing it turned out never to need: the coffer taught the lock, the lift the dice, the mantel the heirloom, the fence the buyer, and burglary is their composition aimed the whole distance in. 's' (Command.Burgle, new journaled key) beside a stead door slips the latch: one Sleight roll for the whole entry, on a curve between the pocket's and the coffer's (0.40 green, +0.05 per level, cap 0.85), one try per door per world, the ledger independent of the pilfer ledger (the sill and the kist are two distances in). Clean: 4-9 coin, an heirloom for the peddler's fence (D-124's loop closed from the far end), secret/burgled_house (the third secret fact from a deed), Sleight fed, no shame: the first crime against the stead it cannot see. Caught: two rungs at once on the unified ladder (a loaf off a sill is hunger; a body in the dark of your house is something else), shame/housebroken, and restitution at the crossed sill at twice a door's coin (12), walking both rungs down, repay outranking more wrong. Save v69 -> v70. 675 tests green (10 new BurglaryTests, pinned like the lift's: seed 1 clean, seed 4 caught). Twins byte-identical; baselines byte-identical to v69 on all five sweep seeds (the pilot never presses 's': pilot-unexercised by design, the D-107/D-122/D-124 precedent); sim replays exact (seeds 1, 7, 2024); the v70 sweeps are the new baselines. Crime's box flips to [x]: all four named verbs shipped. New tracked: a consumer for secret/burgled_house (the secret-facts pass is now three strong).
- 2026-07-20: **D-126 the bearer's second bar: the field reads you back.** Combat depth's fifth rung, closing D-004's last asymmetry: the bearer's own posture bar (Will's clause, D-015) and monsters reading the bearer's stance and commitment (deferred at D-058) ship together. The bearer's guard is rocked only by landed telegraphed committed blows (the field's own D-014 mirror: nips, untelegraphed trades, stones, the cry, and the cold rock nothing): light blows 1, heavy blows 2, the charge's mass 3. The brim is Will's: 8 at baseline, +1 per point above. The reading: a pressing bearer's thinner guard is leaned on a point harder, a set guard shrugs a point (a light blow whole), and a body holding its own wind-up is rocked a point deeper, so every kind now punishes commitment. At the brim: the held heave dies in hand, the arms refuse two full turns (turn-free), every blow lands 2 deeper (the thegn's 4: it alone knows the door), and the feet keep working, deliberately half the foes' sentence: retreat is the answer. Quiet ground settles the bar; death and the crossing clear it. Save v68 -> v69. 665 tests green (13 new BearerGuardTests; nothing moved). Twins byte-identical; four of five sweep seeds byte-identical to v68; seed 7 drifts honestly and tells the whole story in three numbers (2545 more keys, 397 fewer turns, same 3 deaths: the pilot's guard broke, its bumps refused turn-free, and it walked out alive); sim replays exact (seeds 1, 7, 2024); the v69 sweeps are the new baselines. New tracked: a kind with a pressure verb of its own (a board-check aimed at the bearer's bar).
- 2026-07-20: **D-125 the second bar and the parry: the guard beaten open.** Combat depth's fourth rung, closing vision §4's two oldest unstarted boxes together because each is the other's payoff: the posture bar (D-004's launch-module clause) and parry as a distinct verb. Every foe carries a guard beside its blood, rocked in D-094's flat register by pressure that is not blood: a paid blow 1 (swing, thrust, heave's landing; a winded tap rocks nothing), the wall 2 (paying D-095's tracked wall-slam cost), the heave 3, a parried blow 4. Brims 4-10 by discipline and mass (goblin 4, carl 8, stone 10). At the brim: the wind-up dies, the body staggers open two turns on D-053's standing-open machinery (extended to the generic kinds), and one paid melee blow through the door lands a flat +4 and spends it; a stagger walked off is a door closed. The parry is 'a' against an adjacent blow whose shown mark sits on the bearer's own cell: 2 wind, the turn committed to the guard not the kill, the blow turned whole with no dice, and the family in hand trained. The feint's lying mark can never be met (the iron goes where the eye says), so D-096's keen-read payoff stands. Save v67 -> v68. 652 tests green (11 new PostureTests; nothing moved). Twins byte-identical; four of five sweep seeds byte-identical to v67 (the pilot never presses 'a', and its fights end before a brim of 4 fills: D-004's trash-dies-in-2-3 promise reading back out of the data); seed 1 an honest 3-key drift (same turns, deaths, chests); sim replays exact (seeds 1, 7, 2024); the v68 sweeps are the new baselines. New tracked: the bearer's own posture bar (Will's clause, D-015); a kind that answers being parried; parry- and stance-riding knacks.
- 2026-07-20: **D-124 the peddler on the road: the fence, and goods with a past.** Crime's oldest named blocker closes: fencing wanted a fence (D-107), and the peddler is it, picked over a second settlement. Two halves: pilfering now pockets a small thing off the mantel beside the loaf (Player.Trinket, crossing worlds on the body like hides), the take a stead knows on sight and will never buy back; and the peddler stands camped with a cart in every world (NpcKind.Peddler, own stream after every draw, open ground 7-14 out from the stead), the road's trader whose whole voice is not asking. Three talk digits: bread at the road's 6c to anyone (the larder's bars are the stead's books, not the cart's: the outcast's grocer), hides at 4c over the bench's 3c (D-025's arbitrage, first stone), and the fence's digit: everything with a past at 7c apiece, no dice, no shame, no witness, writing secret/fenced_goods (the stead's second secret fact from a deed). The peddler is not a mark and not a villager. Save v66 -> v67. 641 tests green (12 new PeddlerTests, diceless so no probing; one found-and-fixed: the talk-menu offer gate lacked the new kind, so the cart's digits fell through to close). Twins byte-identical on all five sweep seeds; sim replays exact (seeds 1, 7, 2024); baselines moved off v64 as predicted for a standing person: four seeds differ only inside the key stream (equal counts, identical outcome lines), seed 7 drifts honestly (36 fewer keys, same crossings, deaths, and hauls); the v67 sweeps are the new baselines. Deferred: a fenced_goods consumer; the cart's stock growing with tiers toward the caravan.
- 2026-07-20: **D-123 the standing round and the light purse read.** Town life thickens by both of D-108's named deferrals. The standing round is carousing's small verb at the same hearth: one always-listed digit after the bones (state-read label), 5 coin once per world, moving no rung and opening no ledger on purpose (D-108's own reason for setting carousing aside): it pays in the evening's warmth, a game/round_stood fact, and the lane's memory (the-round-remembered Talk storylet in the D-088 discipline: the stead remembers who poured). A round stood over a losing board gets its own toast. The light purse read is the light_purse fact's consumer: the-light-purse Talk storylet gated on the live net at nine down exactly as the lucky hand's talk is at nine up, so coin won back ends the reading while the fact stays history: D-108's symmetry completed on both signs. RoundStood rides the BonesNet pattern, reset at the crossing. Save v65 -> v66. 629 tests green (8 new CarouseTests, seed 15 pinned: three losses then a win). Twins byte-identical, sim replay exact, baselines byte-identical on all five sweep seeds (the pilot never gambles or carouses; both storylets sit behind facts no journey writes). Deferred: tournaments, trading, property; a second game deeper in the chain; a round_stood consumer beyond the lane if a template wants a generous bearer.
- 2026-07-20: **D-122 the locked coffer: lockpicking in the fighting deeps.** The crime family's third verb in the exact shape D-107 deferred it: deep-site coffers as the guilt-free Sleight outlet, picked over fencing (still wants a fence) and burglary proper (new furniture and item plumbing). One box of old iron per fighting deep whose makers were the locking kind (camp, quarry, hall, ringfort, leaguer; the barrow left out: the dead lock nothing, they watch, and D-106 owns that ledger), placed on its own derived stream per site so pinned layouts hold. 'g' on the cell, Sleight dice on a harder curve than a pocket (0.35 green, +0.06 per level, capped 0.85), one sitting per lock per world: a lock that gives pays 7-14 coin and feeds the hand, a lock that holds teaches nothing and keeps its lid. No shame, no facts, no witness, and a tier-1 Sleight surface that asks no crime against the stead. Save v64 -> v65. 621 tests green (9 new CofferTests on pinned seeds 1 and 4). Twins byte-identical, sim replay exact, baselines byte-identical to v64 on all five sweep seeds (the pilot never tries a lock: pilot-unexercised by design, the D-107/D-108 precedent). Deferred: fencing once a fence exists; burglary proper; Sleight knacks (the skill now has two feeders).
- 2026-07-20: **D-121 the Gold Rush: the fifth template, bound to the old quarry.** The first new spine since the launch list closed, cashing D-040's template-driven-dressing deferral: eligible tier 3+ where the quarry stands, so the mid-band finally deepens on descent. The accepted history is the greed-tale (the graven figures as the old crew, stone mid-stroke over the seam they would not leave), the prospector asks the watch put down (promise open_the_pit, 3 essence settling, truth-indifferent), and the survey-marks at the working face flip it twice over: the crew struck the seam, read that it runs through the stone that holds the valley's slope, and authored the greed-tale on themselves as a fence, so the wrong story is a kind one and the wealth is real. The hushing with the truth in hand opens "The fence and the seam" (no check: nothing in the pit resists): carry the survey down (pit_fenced_true, the rush dies awake) or leave the founders' fence standing alone (fence_alone plus withheld pit_truth, the fourth writer into the D-118/D-120 silence pipe with its own retelling arm). Without the truth: plain lines, fence_alone, and no one knows what the founders knew. Save v63 -> v64 (tier-3+ worlds draw among five and re-deal). 612 tests green (RushTests new with nine on master seed 2; HallTests/LeaguerTests hardened against the shuttered-window scene the re-dealt NearHouse draw can open). Journey baselines legitimately moved on all five sweep seeds, twins byte-identical, sim replay exact, all reaching cycle 13; the whole lane fired live on seed 2 (greed-tale, plea, survey, scene answered). Deferred: the buyer/factor role and live rush thread with D-025's economy hooks; the sixth template.
- 2026-07-20: **D-120 the count travels: the unsaid crosses as a silence.** The withheld fact's cross-world consumer, the deferral D-118 named with three writers now waiting (the faiths' shrine, the blight's mound, the throne's cairn). The crossing captures every withheld fact and presses it into the next world as a silence fact whose detail is the wrong story told for true, and every later crossing carries the whole count forward, so silences accumulate for the rest of the run. Deliberately not hushed-gated: the hushed name stills the songs about the bearer, and the story a kept truth left standing was never one of those (you cannot hush what was never said; an oath must not be a guilt-eraser). One consumer ships: silence-retold, NearHouse, once per world, a walker off the trade-road telling the story from over the arch while the one person who knows better stands listening ("I keep the count of unsaid things, and the count travels"). Save v62 -> v63 (the retelling shifts the storylet draw). 603 tests green (SilenceTests new with five; BlightTests +1 end to end). The pilot answers scenes first-choice and never keeps a truth, so journey baselines are byte-identical to v62 on all five sweep seeds, twins byte-identical, sim replay exact, all reaching cycle 13. Deferred: a fifth template; deeper silence consumers at the arc's late rungs.
- 2026-07-20: **D-119 the hill and the cairn: the blight's and throne's climaxes staged as scenes.** The D-118 lane finished: every template's truth-in-hand ending is now a held moment. cb-ending-truth opens "What comes down the hill" (carry the paid watch's truth down, truth_published word for word, or leave the telling under the turf: truth_buried plus withheld/mound_truth) and ut-ending-truth opens "The cairn and the ledger" (carry the ledger down, seat_truth_carried preserved, or leave the truth under the cairn: seat_lie_stands plus withheld/seat_truth). Both fire on DeedWritten mid-site with no plumbing change (InScene routes first). No checks, deliberately: nothing on either hill resists, and dice against no resistance would be a roll wearing a choice's clothes. The throne's settling follows the choice, not the evidence: ut-telling-truth forbids the withheld fact, and the ninth throne storylet ut-telling-kept is the game's first withheld consumer (the teller closes the book content while its one reader stands by; same 3 essence). Late-read evidence still reaches the teller (no withheld fact on that road); the blight's settling stays truth-indifferent (the promise was the stilling, not the history). No-truth endings stay plain lines. Save v61 -> v62. 597 tests green (BlightTests and ThroneTests +2 each). The pilot walks these roads (reads the stones, clears the sites, answers first-choice), so journey baselines legitimately moved for the first time since D-116: turns identical to v61 on all five sweep seeds, keys +6 to +12 (exactly the scene answers), twins byte-identical, sim replay exact, all reaching cycle 13. Deferred: a cross-world withheld consumer (three writers now waiting); a fifth template.
- 2026-07-20: **D-118 the claim at dawn: the faiths' climax staged as a scene.** The first plot-beat conversion on the D-117 machinery, cashing the ending D-116 explicitly parked. wf-claim-truth (Rest hook, truth in hand, both accounts heard) now opens "The claim at dawn": the elder says the claim, and the socket's telling is the bearer's to spend or keep, the spec's three-way climax at slice scale. Say it whole (the old best ending, one_grief_shared, word for word); turn the cuts against the harrow's claim behind the game's second visible check (Presence, difficulty 1): carried, the claim breaks on a true stone laid in a crooked course (new coda claim_broken, nothing shared, the whole truth left cut where no one will climb to read it); seen through (the elder asked for a reason to doubt, not one built to order), the truth is spent and the quarrel shelves (claim_shelved); or keep the telling, the deliberate silence (claim_shelved plus the new withheld fact: the graph remembers the truth stayed in its stone by choice). A third settling for the broken claim pays the same 3 essence (endings differ in what the valley believes, never in the pay). Without the truth there is nothing to choose, so wf-claim-cold stays plain lines. Eleven faiths storylets now. Save v60 -> v61. 593 tests green (FaithsTests grown to 13: the scene surface with the check tag, all four leaf nodes, both wielding branches on burned combat rolls, the broken settling, ends-once and the no-truth path scene-free). Twins byte-identical and journals replay exact through sim on all five sweep seeds, all reaching cycle 13, byte-identical to the v60 baselines (no worldgen or selection change; the climax is talk-gated, pilot-unexercised and test-covered per the D-109/D-111 precedent). Deferred: the blight and throne climax conversions; a withheld-truth consumer (a later world's echo reading the silence).
- 2026-07-20: **D-117 dialogue-tree scenes with visible skill checks.** D-021's oldest open line cashed, on the exact seam storylets.md sec 6 promised: gating unchanged, delivery grown. A storylet can carry a Scene; firing opens a modal dialogue tree (nodes of prose over numbered answers, entry effects writing facts), state on Game behind InScene, every key journaled through ApplyKey, prose landing in the log so the log stays the one full transcript. The visible check is the game's first: a checked choice shows its odds on the row before the player commits ("Presence, 40 in 100"), read off the sheet at node entry, rolled at commit on the gameplay stream (the Lifting formula generalized: half base, a twentieth per point of edge, a tenth off per difficulty step, clamped [0.05, 0.95]); skill checks feed the skill on success, and the first shipped check wakes Presence, the last inert attribute (7 of 7 now active). A scene is a moment, not a menu: while choices stand other keys wait, and walking away is an authored answer. First content: grievance-voiced grown into the shuttered window (the checked pressing to counsel or the bolt, the word writing its promise, or leaving). Save v59 -> v60. 590 tests green (7 new ScenesTests). Twins byte-identical and journals replay exact through sim on all five sweep seeds, all reaching cycle 13; the window fired live seven times in the master's twelve worlds, three carries and four fails. New tracked: scene conversions for the standing plot beats (faiths' claim first). Deferred: checks that alter simulation; topics inside scenes; a scene-native witnessed-ending frame.
- 2026-07-20: **D-116 the War of Faiths: the fourth template, cast by office.** D-114's second lane lands and the named launch-template list closes. The template compiles through the D-032/D-035 seam on D-115's institutions, with a casting principle new to the pool: the whole cast holds offices, not lots (the shrinekeeper and the harrow's elder are the believer-champions, the doorward the silent keeper-of-the-founding-site); only the straddler (a villager of harrow kin who prays both ways) and the aggressor side are drawn. The war at slice scale is a feud not yet bled: one side has stopped arguing and started taking (kerb-stones off the ring, or the offerings off the shrine-stone), the two schism accounts are planted against each other and voiced by their own champions, the evidence reads off the mother-stone's empty socket (two keepers of one rite, a burying winter: the truth complicates both books), and the climax cashes D-115's rumor line on the Rest hook: the elder comes down and says the claim at the shrine, dissolving into shared keeping with the truth in hand (the war that never starts) or shelved on the old answers without it. Ten storylets; endings fire once; the settlings pay 3 essence either way and never open unasked. The compile context grew the full cast (Villagers stays the only drawable pool) and the story compile moved below the faiths in worldgen; save v58 -> v59, tier-2+ worlds re-deal, fixture masters remapped (blight 41 held, throne 7, stead 43, faiths 44). 583 tests green (9 new FaithsTests). Twins byte-identical and journals replay exact through sim on all five sweep seeds, all reaching cycle 13; the doors beat fired live three times in the master's twelve worlds, the rest pilot-unexercised and test-covered. Deferred: the war-profiteer optional role; escalation beats on a longer fuse; codas feeding cross-world mythology weight.
- 2026-07-20: **D-115 the two-faiths worldgen lane: the harrow raised, the keeper cast.** D-114's first lane built to spec: the harrow (new SiteKind and terrain, glyph 'A') stands up the valley in every world at every tier, an authored peaceful room in the songhall's mold holding the tended fire and the mother-stone beside its empty socket, the founding fact planted in generated history (the shrine-stone came down off the harrow's ring: lent by their telling, given by ours). Cast: the elder and the doorward at the harrow's door on plain ground, the shrinekeeper at the shrine's diagonal shoulder (never a cardinal, so the rest point's approaches stay clear). Both sides speak: the keeper carries the stead's reading and the rumor line (the villagers' nine digits are full in a deep world, so the stead's side lives where it institutionally belongs), the elder the debt reading and the custody claim, the doorward the shorter answers. War, aggressor, and schism accounts deliberately absent (template-time). Save v57 -> v58, worlds re-deal. 574 tests green (5 new HarrowTests). Twins byte-identical and journals replay exact through sim on all five sweep seeds, all reaching cycle 13. Two live finds fixed: harrow folk could be cast onto another site's mouth (now plain ground only, regression-tested), and a latent D-100/D-104 pilot bug (the ridden stride orbiting a pinned laden mule at Chebyshev 2 forever; the pilot gained a stride-aware sidle, engine untouched). Next: the War of Faiths template through the compiler seam.
- 2026-07-20: **D-114 the valley's two faiths: the War of Faiths scoped.** Design-only session, no code. The template's precondition (two organized faiths) turned out to need more than the roadmap clause said: the Aegis-shrine is a faith-anchor but not an institution, so the lane firms up the first faith while adding the second. Chosen: a new order with its own site whose doctrine differs from the stead's folk practice over what the shrine-power is and what it is owed (same power, read differently), which buys the arc resonance the template doc asks for while keeping the debunking machinery fully template-owned. Rejected: a Severed faith (echo risks becoming dependency) and a mound dead-faith (D-113: condition, not cast). Three calls: same power read differently; a dedicated keeper NPC at the stead's shrine; light ambient coloring only in non-template worlds. Two lanes tracked: the worldgen lane first (the harrow as the elder site with the stead's shrine its daughter, the order's elder and folk, the keeper, the founding fact, topics both sides; save v57 to v58 expected, baselines re-deal), then the template through the D-032/D-035 compiler seam. The war, the aggressor, and the schism accounts stay template-time per the spec.
- 2026-07-20: **D-113 the valley's memories read aloud: the roster follow-ons batch.** Four deferred consumers cashed in one small lane, all perception and stead-voice, no new state, no save bump (v57 holds). The mended page consumes made_right in its turn: the stead remembers the one who made right as a story it keeps on purpose, the mend left where the young can see the stitching; no coin, no regard, the D-109 discipline held. The two memories is the made_right thread meeting the roster's memory: made_right plus nemesis/risen reads the valley's two ledgers side by side, the risen heir named, the villager speaking only what a stead can perceive and the Aegis reading the far book's owner aloud (one book could be paid; the other can only be outlived). The long-mound topic reads the grudge aloud while the mark stands (the pacing lights, the mound's tally, the bearer never named, the dogs growling first), settling back at the stilling. And the chief is told apart on the map: capital G among its lowercase raiders, same colors, rank not a new kind. The conditional named-figure-for-another-faction stays tracked (no current faction has an individual to name). 568 tests green (4 new). Twins identical; master keys bit-identical to the D-112 baseline and the replay exact (the honest expectation: nothing mechanical moved; the two talk beats sit behind shame roads the pilot never walks, pilot-unexercised and test-covered per the D-109/D-111 precedent). Next: the War of Faiths (wants a second faith-bearing institution).
- 2026-07-20: **D-112 the Usurped Throne: the first faction-hungry template.** The roster's unblock cashed: the third compiled world-story, and the throne reads broadly as designed, because at slice scale the only polity with a personal ruler and live succession is the dens themselves. The seat was taken, not given: the world seed names a dead chief from the story's own stream, the dens' official telling hangs the death on a stead arrow (the lie is load-bearing: every raid since is collected as that debt), a lieutenant is cast as the old blood standing under the usurper, and a stead teller carries the account to the doors. The cairn behind the fires is the evidence, and the flip complicates: the old chief was raising one night of fire against the stead, so the usurper's knife bought the stead its walls and the lie that hides it keeps the raids coming. Endings branch on whether the truth came down before the fall (codas seat_truth_carried / seat_lie_stands); the restoration beats ride D-110's live succession (the old blood back on the seat, or passed over, a story that will keep); the settling with the teller pays the same 3 essence truth or quiet (endings never differ in coin), and never opens if the story was never heard. Eligible tier 2+, so the first world keeps its single crafted story; selection now draws among three, so tier-2+ worlds redeal (save v56 -> v57). The pilot surfaced a real hole: a cairn read after the fall re-armed the truth ending on the next deed's hook, both endings firing in one world; endings now fire once, and the same latent hole is closed in the Creeping Blight. 564 tests green (10 new ThroneTests, 1 new blight regression; fixture masters remapped 42/41/40). Twins identical on master and all four sweep seeds; master keys bit-identical to D-111's journal (the pilot's course is story-agnostic on that seed) and its replay exact, with the throne confirmed firing live in all five of its worlds (5 cairns, 3 truth endings, 2 lie endings, no doubles); the talk beats are pilot-unexercised and test-covered. Still tracked: the War of Faiths (needs a second faith-bearing institution); the roster follow-ons.
- 2026-07-20: **D-111 the roster read aloud, and the exits audited.** D-110's deferral paid and D-023's last open box closed in one small lane. The goblin-raids topic reads the dens' order live: a risen chief named at the doors ("a new voice over them since the old one fell"), a camp whose named have all fallen read as leaderless, both gated on the live roster; the scar deliberately gets no stead-side reader (the stead speaks only what it can perceive). The kept boast comes home as a Talk storylet, once per world: the stead heard the name in the howling the night the bearer fell and laughs the boast off to the standing bearer's face, den-talk not being believed, so the stead's epistemology holds and only the Aegis says the joke's other half. The exit-conditions audit walked every live conflict against the tick code and found each holds a designed exit (raids/watch/levy/shame/mound/wrath/boldness/nemesis, plus the crossing reset as doctrine backstop): the D-023 box is checked. No new state, no save bump (v56 holds). 553 tests green (3 new NemesisTests). Twins identical; sim replay exact; master and seeds 1/7/88888 byte-identical, seed 99 moved as designed (10056 -> 9918 turns, deaths held): its pilot dies to a named raider and the boast then fires live at the well, confirmed from the full sim log; the topic branches are pilot-unexercised (the pilot never presses the raids digit) and test-covered. Still tracked: the made_right thread meeting the roster; the chief told apart on the map; a named figure for another faction.
- 2026-07-20: **D-110 the named of the dens: the Nemesis-style roster begun.** D-023's roster clause lands where the fighting is: the camp's world seed names a chief and two lieutenants from their own stream (a new raider weave in NameGen, short and bitten off), the stead's rumor carries the chief's name from the first morning beside a nemesis/chief fact, and rank is worn as hide (+4/+2 Hp over the tier's base). Three memory beats ride the replay, nothing serialized, no save bump (v56 holds): the scar (a named raider bloodied and left alive keeps the wound's author, swept on leaving by ladder or by dying in the camp, nemesis/scarred), the succession (a chief slain over a standing lieutenant hands the camp on, the office coming with the grudge in it, nemesis/risen, the last named falling to the no-heir silence), and the slaying (the very hand that authors the bearer's death keeps the boast, nemesis/slew_bearer). Light teeth on the established rail: a held grudge arms the hand one point after the dice, the dread's mirror, and every grudge is spoken to the bearer's face at the next descent, once per memory. 550 tests green (8 new NemesisTests; one crossing assertion updated for rank-as-hide). Twins identical; sim replay exact. Baselines moved as designed (the tougher camp): master 11846 turns/1 death/26 raids, sweep completes on all four seeds. Pilot-exercise verified from the full sim log: the rumor, the announcement, the named falls, 9 successions and 12 no-heir silences fire live; the scar, the taunts, and the boast are pilot-unexercised (the pilot clears camps whole) and test-covered. New tracked: nemesis fact consumers; the chief told apart on the map; a named figure for another faction if one earns its keep.
- 2026-07-20: **D-109 the facts answered in their turn: made right, the door that held, the two ledgers.** The graph's three produced-but-unread facts get their consumers, three storylets in the D-088 discipline (narrative and facts, no mechanics: the world reading differently is the payoff). The making-right beat at the well consumes `confronted`: both producers feed it (the reckoning D-088, the caught hand D-107, the unified ladder paying off), gated on live shame back at zero, once per world, writing `made_right`; deliberately no coin or regard, so restitution never turns a profit. The door that held consumes `stead_cellar` on the raid seam the tick already writes: the raid's morning read from inside the count, once per world. The two ledgers consumes `lifted_purse` where it collides with trust: the fence opened at the friend rung to a hand that has been inside it unseen; a clean lift has no restitution road, so the weight is the consequence. No new state, no save bump (v56 holds). 542 tests green (7 new FactConsumerTests). Twins identical; sim replay exact; baselines byte-identical on master and all four sweep seeds, verified honestly (the journey log grepped: none of the three beats fires under the pilot; all pilot-unexercised, test-covered). New tracked: the `made_right` consumer (Nemesis-roster memory fuel).
- 2026-07-20: **D-108 knucklebones at the hearth: town life opens.** The last unopened family breaks ground with the vision's first-named town verb, kept at the skald's hearth (the songhall is where men game already, and the skald's digits had room where the villagers' shared nine did not). One always-listed digit after the deeds: 3 coin the throw, stake matched and spoken for, the bearer's cast face-up, one throw back if dared (standing is any key but the throw: a live board never traps a hand and never refunds), the skald standing at eleven or better and sweeping up anything under, announced: a readable house, so the reroll is a real decision. High board takes the pot; ties return the stakes; turn-free. Per-world net ledger, wiped at the crossing; at nine up the lucky_hand fact and a Talk storylet gated on the live net (the streak given back ends the talk, the fact stays history), at nine down the light_purse fact for a future consumer. Save v55 -> v56. 535 tests green (10 new BonesTests on pinned seeds). Twins identical; sim replay exact; baselines byte-identical on master and all four sweep seeds (no digit shifts; the pilot never gambles). New tracked: the light_purse consumer; carousing as a round-standing verb at the hearth.
- 2026-07-20: **D-107 the light hand: pickpocketing and the Sleight skill.** The crime family opens in earnest, the fork won by the lift over lockpicking (furniture for what pilfering half-covers) and fencing (no plausible fence in a three-door stead; both stay tracked). 'p' (Command.Lift, a new journaled key) beside one of the stead's folk brushes the purse: one try per pocket per world. The dice ride the new Sleight skill alone (half green, a twentieth per level, capped at 0.85: no hand ever safe), the tenth skill, fed only by lifts that worked. Clean: 2-4 coin, the stead's first secret fact from a deed (secret/lifted_purse). Caught: the confronted fact with the catcher's name and the same unified shame ladder as pilfering (the user's pick: the stead keeps no separate books on flavors of thief), restitution in the wronged hand with the key that tried it, at the sill's price. The first-shame way-back hint moved from RaiseShame to the deed (a guest's fall no longer names the door road). Save v54 -> v55. 525 tests green (9 new SleightTests on pinned seeds; 2 updated for the tenth skill). Twins identical; sim replay exact; baselines byte-identical on master and all four sweep seeds (the pilot never presses 'p': the honest-bearer path, test-covered). New tracked: fencing wants a fence; Sleight knacks; the confronted/secret consumers now have live fuel.
- 2026-07-20: **D-106 the third faction: the long mound.** The relation matrix gets its second edge, and the mound won the fork over the old watch (every-world presence from tier 2, three systems already leaning on it; the watch waits for the Nemesis roster). The grudge is earned one way: grave-goods carried out of an unstilled barrow. One rung, and the weight is in the dead's hands: riled wights strike a point harder (the dark mirror of the raiders' dread) and the mound raises its own slain again on the tick, capped at three, never under the bearer's eye, each rising spoken up the lane and written (mound_restless). The stead perceives both directions of the edge: the stilling was already the regard's deed, and the riled lights are now feared at its doors (NearHouse storylet on the live grudge). The designed exit is the stilling itself: the one Infamy whose exit is completion, not payment. Save v53 -> v54. 516 tests green (9 new MoundTests). Twins identical; sim replay exact; the D-105 baselines predicted byte-identical and confirmed on master and all four sweep seeds (the pilot stills before it robs: the pious-delver path, the grudge honestly pilot-unexercised, test-covered). New tracked: the mound topic reading the grudge; a raiders-mound edge; a desecration verb.
- 2026-07-20: **D-105 the stead moves: the watch posted and the levy called.** D-089's deferral paid: the home faction acts on the tick. The watch answers greed: posted the morning after a sackful raid, it turns the raiding nights away with nothing (no plunder, so the dens' greed stops compounding) at a measure of upkeep per tick, and can bare the lofts itself; it stands down at the cull (wrath's second faction-scale consequence), the camp-fall, or the empty loft. The levy answers the stores: called at the last measure, it closes the larder and turns the ration digit into the levy's answer (coin against a carted measure, +1 regard: the stores axis' first bearer-side input), lifted by answers or recovery, the ask voiced by a Talk storylet gated on live state. All narrated + written (watch_posted, levy_called, levy_met). Save v52 -> v53. 507 tests green (10 new SteadMovesTests, the bare-out test rewritten). Twins identical; sim replay exact (11750 turns/1 death); the watch shows live on every seed (master raids 33 -> 26, sweep 11/13/15/14 vs 14/23/16/18), deaths 0/3/2/3 held. New tracked: a pilot levy-answer rung if journeys show levies standing; the raids topic reading the watch and levy aloud.
- 2026-07-19: **D-104 the pilot works the beasts.** Fourth and last lane of the pilot-policies batch, closing D-100's follow-on. Courser forward, mule banks: the steadholder's gift claimed the moment the camp breaks, the mule bought with surplus coin at the friend's rung, the purse banked into its bags on the working road (the spook hands it back at uncanny mouths and the bank reloads), the stable turned so the courser's stride leads and the laden mule stands as the raid-proof vault, and the bank brought home before the world-bound bags would forfeit at the arch. Runner grew the roads evidence line. Master: 9 mules, the courser in all 12 worlds, 1244 coin banked, 11886 turns/2 deaths, twins identical, sim replay exact. Sweep: courser in all 8 worlds on every seed, 384-573 banked, deaths 0/3/2/3. The companions pillar is now driven live by every journey; the fell pony's taming stays a tracked follow-on.
- 2026-07-19: **D-103 the pilot says the words.** Third lane of the pilot-policies batch, closing the oldest tracked pilot gap (the D-091 stones-and-words line) and D-099's cast-policy follow-on in one doctrine: the warded delver. Every stone in a held site read before climbing out (chest-shaped rung), the ward said with live steel in the word's reach, and on wight/graven ground the calling said instead, the shade released by the second saying once that ground clears (at the base pool the two workings cannot coexist: D-099's designed trade live). Spark and levin stay unsaid; no word opened on an aimed cell. Runner grew the words/wards/shades evidence line. Master: all 5 words, 139 wards, 30 shades, 11302 turns/1 death, twins identical, sim replay exact. Sweep: every seed learns all five, deaths collapse to 0/0/2/3 (five-seed total 24 -> 6): the magic pillar demonstrably carries its weight.
- 2026-07-19: **D-102 the pilot keeps its feet and its medicine.** Second lane of the pilot-policies batch: stance (D-094) and the steeping (D-090) pressed live for the first time. Footing read off the blood (pressing at two thirds and up, guarded under a third), set free on quiet ground, exactly one bought mid-fight downshift (pressing to guarded, never on an aimed cell, never in the wilds); vials steeped before the herb sale and drunk below a third, the dodge outranking the stopper. The runner's herb ledger untangled (a steeping is not a sale) and the report grew the steeping line. Master seed: two deaths became zero (10105 turns, 11 drawn/9 drunk); twins identical, sim replay exact; sweep re-recorded (6690/5, 6941/7, 7198/6, 6529/6), five-seed death total 27 -> 24.
- 2026-07-19: **D-101 the pilot walks the cure roads.** First lane of the pilot-policies batch (D-101..D-104). The autopilot pays a carried mark off the moment the price is in hand: the brace folded into the smith errand (gear buys first), the eye into the stillroom errand and bench driver (herbs sold first, the sprig-coin counting toward her price), and the haunted look walked to the skald at the hall door, its 8 essence held back from the shrine's raising while the mark is carried (SpendableEssence, D-099's held-Focus shape; a hall keeper stands in every world so the hold is never wasted). Cli-only, 497 tests green. Master never scars so its baseline held to the byte; seeds 7/99 held to the digit; the two haunted seeds diverged exactly at the cure (seed 1: 6952 turns/7 deaths/13 raids, seed 88888: 6592/6/17, both ending scar-free). New sweep baselines recorded.
- 2026-07-19: **D-100 stage 2: the roster gathered.** The other two roads open and the leans split the three: mule the banker (bottomless bags, grass stride), courser the racer (grass/hills/forest stride, bags capped 25), fell pony the delver's beast (alone stands the uncanny mouths). The courser is the raiders' stolen animal, given over by a once-per-world steadholder storylet after the camp deed; the fell pony stands the high ground (own worldgen stream, named when first seen) and three breads win it: the one road the stead has no hand in. Mortal-nerved beasts bolt from uncanny mouths and shed the bags at the bearer's feet (bolting is never free coin-safety); the stable is one cycling wood's-edge digit (put-up/lead-out/swap, digit law kept), keeps bags, and the raid does not reach in: the promised deliberate parking. All world-bound; the crossing clears the stable. Save v51 -> v52. 497 tests green (8 new, first-run); twins identical; sim replay exact; every baseline held to the digit (the pilot's steadholder errands predate the camp deed, so no journey meets the courser: the beast lane is honestly pilot-unexercised, covered by the tracked pilot mule policy). The companions pillar stands complete.
- 2026-07-19: **D-100 stage 1: the stead's mule, the ridden stride, and the saddlebags.** The pack animal (D-024's last niche) opens, with the user's custom roster call setting the design: three beasts on three roads (mule bought / courser storied / fell pony won wild), a per-world stable, one at your side; stage 2 gated. Stage 1 is the mule whole: sold at the wood's-edge bench (the steadholder's nine digits are full: the digit law placed it), 40 coin, only to a friend of the stead (regard rung 2 pays again). It follows on the overworld, waits at site mouths, and open grass passes two strides to a key: half the turns for the distance against every clock the game counts. The saddlebags are one key and a turn: banked coin does not fall with the bearer, and a raid landing while the bearer is below takes the beast whole instead: banking is a choice of risks (grounding finding: no carry caps exist and goods already survive death, so travel + the coin-risk choice are the honest niches). World-bound; never crosses. Save v50 -> v51. 489 tests green (9 new MountTests); twins identical; sim replay exact; master and sweep held to the digit (the pilot neither buys nor rides: pilot mule policy tracked).
- 2026-07-19: **D-099 the calling: the fifth word, and the shade that walks while it is held.** The summon slot (D-024) ships where D-097's guest engine and D-091's craft meet, every Q&A answer the recommendation. A called remnant: SpellId.Calling on every stone leaning (the barrow second), held rather than spent: 2 of the pool stay bound while the shade walks (SpendableFocus the new seam), freed on any ending. The shade IS a Guest (role Shade, 10 HP, blow 1-3 doubled on wight/graven: soul-stuff answering soul-stuff) and the engine generalized to fellows: own slot beside a mortal guest, full physics, follows/holds/place-trades, raiders turn on the nearer body. It refuses the severed (mid-build finding: the laying is the bearer's choice surface, D-038/D-045, so auto-striking would foreclose it). Not mortal, on purpose: unravels on fall (no fact, no shame, "nothing here mourns"), the bearer's death lets the word slip, the waygate keeps only the knowledge; released by saying the word again, anywhere. Never tended; whole again at a rest. Cyan 's' glyph, the rail line, the FO bar and cast menu reading the hold. Save v49 -> v50 (the leanings grew a word). 480 tests green (13 new ShadeTests); twins identical; sim replay exact; master and sweep held to the digit (the pilot never casts: a pilot cast policy joins the tracked follow-ons, with a shade-noticing storylet and the warder's kinship question).
- 2026-07-19: **D-098 stage 2: the cure roads.** Each scar's way back, on the bench it belongs to, always listed with a state-read label (D-041's law), dear on purpose. The eye: the stillroom's longest work (30 coin; the Keen read restored whole). The hand: the smith's brace (24 coin; jointed iron built TO the crookedness; D-009's superior-prosthetic hook in fiction, mechanical edge deferred). The look: sung to rest at the songhall (8 essence, paid in what deeds weigh; the walk out is the pilgrimage; bread and regard warm the same turn). One shared Aegis parity line per mend. Plus the vision's dialogue-hook clause: the marks-they-carry Talk storylet, any villager, once per world. Save v48 -> v49 (three benches grew an entry; a scarred talk draws a new storylet). 467 tests green (5 new); twins identical; sim replay exact; every stage-1 baseline held to the digit (the pilot walks no cure road yet: new tracked follow-on). Also tracked: brace's mechanical edge, scar facts, dragging step, tier fill, sheet display.
- 2026-07-19: **D-098 stage 1: the Death's Toll, and the scars matched to their deaths.** The last untouched pillar (D-009) opens as a deterministic ledger: deaths fill the count (100; a thegn's or hart's hand 160; Will above baseline shaves a tenth per point, floored 40), it drains 1 a turn, and a death at the line (20) or above converts, no roll: the judgment reads the count as it stood, THEN this death's fill lands, so a first death always warns and never scars, and two deaths within the Wounded span (80 turns) is what clustering means. The scar matches the death (uncanny kinds -> the haunted look; thrown/lofted -> the taken eye; iron close in -> the crushed hand; shapeless -> fixed order), replay-clean, zero new rng. Weights on surfaces the game already reads: the eye steps ReadOf down a whole tier, the hand adds 1 stamina per swing, the look docks every regard gain by 1 and dears bread by a coin. TOLL on the sidebar (red at the line), scars named under it, the drain's crossing spoken, one Aegis scar line on the motif. Waygate wipes the count; scars cross until cured (cure roads = gated stage 2). Save v47 -> v48. 462 tests green (9 new TollTests); twins identical + sim replay exact on master AND scarred seed 1; sweep: seeds 1/88888 land the haunted look live and still finish, seed 99's eight spaced deaths land nothing, every v47 baseline held to the digit. Journey report grew a scars stat.
- 2026-07-19: **D-097 stage 2: the huntsman's debt, the bond's ledger, and the full weight.** The woodward, once the stead has bled, sets down the hide-scales off a talk and walks as a 16-HP huntsman until the camp breaks (world scope, once per world; their NPC steps off the map). Loyalty beats bank from all four Q&A sources (shared blood within 3, each tending, each fireside rest: mends whole + one line of who they are, every raider felled). A fallen guest writes guest-fell, guest-beloved at 3+ beats, costs the stead a point of shame, and the bench stands empty ALL WORLD (the woodward never comes home); the memorial storylet cashes the beloved fact in any villager's mouth, once. The paid arc: farewell at the cold fire-pits, portfolio fact, NPC home, +1 regard. Save v46 -> v47. The dividend: the pilot already works the woodward's bench, so EVERY journey now casts the huntsman live: new master baseline 10313 turns/2 deaths/10705 keys, sweep deaths 5/4/8/6 (the huntsman pulls their weight). 453 tests green (4 new GuestArcTests); twins identical; sim replay exact.
- 2026-07-19: **D-097 the one who walks with you: the guest engine (companions stage 1).** The D-024 pillar breaks ground, guests first (Q&A-settled: they build the ally engine the summon and mule reuse). Guest entity with role-derived competence (a huntsman's blow 2-5, a crofter's 1-2: who they are, not a slider), one at a time, world-bound. They follow at the shoulder, hold ground on order, fight the adjacent foe to their measure (kills route through HarvestRemains: wrath and site-clearing stay honest; never a severed one, a hart, or the dormant), and take real blows: a raider the guest stands nearer turns on them, and a guest on any intent's resolved cell takes the roll whole (no stance, no iron, no Aegis). One key 'o', contextual: tend a hurt guest from the satchel (draught 8 / sprig 4 / bread 2, always a turn) or hold-here/with-me (free off the fight, a turn under live steel, D-094's grammar). Place-trading steps; doors, exits, and the death-wake shared; a living guest never crosses. Stage-1 death is a stark line + one Aegis line; the full weight is stage 2. NO save bump (fourth no-bump): no journaled path casts a guest, dice roll only when a body is struck: master baseline and sweep held to the digit. 449 tests green (11 new GuestTests). Stage 2 next: the huntsman's-debt arc, loyalty beats, death weight, farewell/portfolio.
- 2026-07-19: **D-096 the kinds' second moves: cry, chill, feint, and the drag.** Combat depth's third rung closes the Q&A set. Goblin: the rallying cry (marked a turn; on resolve the whole camp takes an extra stride at the bearer; kill the crier or be gone). Wight: the grave-chill (marked cell; kept ground = 4 turns of blows landing 2 softer; stepping off is the whole answer). Thegn: the measured cut, its first declared blow, whose mark LIES to any read short of keen (FeintCell carries the truth: the read tiers finally pay differently at the top). Hound: the landed lunge hauls the bearer a stride toward the pack. Sidebar tells + keen weight notes for all three new intents. Save v45 -> v46. 438 tests green (5 new; the thegn's never-telegraphs test superseded by design). Twins identical; sim replay exact; new master baseline 10404 turns/4 deaths/10801 keys; sweep all finish, honestly deadlier (6/5/12/11). New tracked: stance-reading monsters, more second moves, cry waking dormant bands.
- 2026-07-19: **D-095 the families' verbs: sunder, answered step, and the shove.** Each melee family does one thing only it does. Hafted: the heave now sunders a carl/warder's linden board FOR GOOD (thrust and shaft board-turns check BoardBroken) and its weight staggers any wind-up, knackless. Blades: a paid cut into a body whose wind-up marks YOUR cell carries the feet a half-step off the marked ground (deterministic slip, keeps the reach). Brawling: a paid bare-knuckle blow shoves the body a stride back where the ground gives. Spear: the long thrust (D-053) recorded as its standing identity. No new keys anywhere: every verb rides an existing act. Save v44 -> v45 (fights replay onto different cells). 433 tests green (3 new; stropped-edge parity re-aims per swing, an honest knock-on of the answered step). Twins identical; sim replay exact; master seed held, sweep shifted where fights met boards. New tracked: flanking, wall-slam cost, warder rim post-board.
- 2026-07-19: **D-094 the footing: three stances on one key.** Combat depth opens (fork 4's second lane, Q&A-settled order: stances -> weapon verbs -> enemy moves). 'x' cycles measured/pressing/guarded: pressing +2 to melee blows (swing/thrust/heave, floored 1) and 2 bled through the guard; guarded the mirror; incoming applied on the raw blow before iron so the unarmored are guarded too. Free on quiet ground, costs the turn under live steel (commitment, both ways). Sidebar names a non-measured footing; 'x stance' on the help rail. Save v43 -> v44. 430 tests green (3 new); twins hash-identical; D-093 baseline HELD exactly (pilot never presses 'x'). New tracked: monsters reading the stance, pilot stance policy, stance knacks.
- 2026-07-19: **D-093 the asking's long shadows: burdens, vows, the face, and the keepsake's thread.** Creation stage 2, closing fork 4's first lane. Three new questions between the thing and the name. BURDENS (one, buys a second precious thing, duplicates refused): the old wound (MaxHp -2 always), the hunted past (every world's raider wrath wakes at 1, reseeded per crossing), the marked face (every stead's suspicion wakes at 1, stacks with the oathbreaker). VOWS: vengeance (kept at the first camp cleared: +5 essence, once ever), finding (never dangles: an unnamed face draws from its own stream; a cycle-2+ villager half-memory feeds the search), the road's end (answered at the first crossing). The REMEMBERED FACE: typed name, one villager wears it for a blink, once ever. The KEEPSAKE'S THREAD: the skald names the unassuming thing from cycle 2 and a second visit sings it into the halls (+3 Legend, the reward no chest holds); unpicked, the thing waits down the chain from cycle 3 (priority-tolerant arrival placement) and joins the thread when found. Fate rolls all of it; the pilot's '0' is unchanged. Save v42 -> v43 (longer asking + six storylets in the draws; Snapshot gains BearerBurden/BearerVow, dodging the oath-weight Burden field). 427 tests green (6 new, first-run pass); twins hash-identical; sim replay exact (seed 2024's fated bearer rolls a hunted past: wrath peaks 7); sweep all finish. New tracked: face cast into faction NPCs, name in NPC line banks, folk cultures in worldgen, keepsake content past the song.
- 2026-07-19: **D-092 the asking: character creation as the first wake's own scene.** Fork 4's first lane, Q&A-settled, stage 1 of 2. No menu screen: the Aegis asks who it caught, five journaled questions before the first step (cycle 1 only). Five ORIGINAL world-grown folk (supersedes D-017's example roster, keeps its structure), each a tilt + one trait: Steadfolk (third shaping + 10 coin), Emberwrought (+1 MaxFocus), Cairnborn (reads one tier keener, innate), Heathborn (harvests +1, hide and sprig), Wrightkin (gear wears half as fast, parity clock). Seven pasts, each banking a skill's level 1 as counted uses + one extra + a `past` fact: Soldier/Poacher/Hedge-healer/Smith's-hand (a free mending, once ever)/Scribe's-ward (stone rumor)/Wayfarer/Oathbreaker (twice-skilled, Shame 1). Paired swaps (2, Steadfolk 3; band 3..7). One precious thing, soul-bound: the known word (D-091's hook paid), grave-iron arms, the craft kit (stillcraft + 6 sprigs), the heavy purse, or the UNASSUMING THING (inert until stage 2's keyed storylet thread; NG+ placement when unpicked). Name typed in-fiction ('.' seals, '-' erases; empty draws from the folk's stream); sidebar + sheet carry it. The fate door ('0') rolls the whole bearer from the world's stream; the pilot always takes it, so journeys exercise creation every run. Save v41 -> v42 (journals now open with the asking; plain new Game(seed) keeps the unmade wake for the test suite). 421 tests green (12 new CreationTests); twins hash-identical; emit->sim exact (seed 2024's fated bearer: Dunelmund, Steadfolk hedge-healer); baselines re-recorded by design (2024 x12: cycle 13, 10521 turns, 5 deaths, 31 raids, 10925 keys; sweep all finish). New tracked: stage 2 (burdens/vows/face/keepsake thread), folk cultures in worldgen, NPC line banks adopting the name.
- 2026-07-19: **D-091 the remnant craft: graven stones, the four workings, Focus, and Spellcraft.** Magic v1, designed in a Q&A session with the player and built to their picks (found not taught, rare-and-old, small pool over stamina/essence/risk pricing, weight-split casting, Mind=power / Will=pool+grip, Spellcraft from day one). Graven stones stand at the deepest reach of every fighting deep site (camp/barrow/quarry/hall/ringfort/leaguer, own worldgen streams, pinned layouts hold); 'g' reads the word in for good; each fabric leans toward its own word and gives the first the bearer lacks, decided at the reading so worldgen stays character-blind; stones regenerate per world. Four workings on 'z' + digits: the spark (1 focus, instant line, boards no answer: the caster's lane past shield-carls), the levin (2, the caster's OWN wind-up: marked ground, one visible turn, dodged by feet both ways, a mid-hold wound threatens it and Will+Spellcraft hold the grip), the ward (2, six turns of thickened air, teaches only when it turns a blow), the veilsight (2, names the floor's living, sharpens the D-059 reads at this tier, shows the feigning graven men and warders: adapted from "unveils the layout" because the engine has no fog of war). Focus: Will's pool (3+), regen every 8 turns, full at rest/death-wake/crossing, HIDDEN until the first word so the system unveils as a discovery. Spellcraft: 9th skill, D-014 cost-gated (only workings that did work). Words are knowledge: cross whole, survive death. Sheet overwrite bug fixed (Taught/Legend rows had hidden the newest 3 skills since D-070). Save v40 -> v41 ('z' gains meaning; a death now drops a held heave). 409 tests green (9 new MagicTests, first-run pass; 1 knock-on in the knack catalog test); twins byte-identical; emit->sim exact; sweep identical to baseline to the digit (the pilot never reads a stone). New tracked: more workings, enemy casters + Will resists, Spellcraft knacks, caster social texture, pilot stone policy, the fork-4 starting-word hook.
- 2026-07-19: **D-090 the stillroom's craft: the hale-draught, 'd', and the stillcraft.** Alchemy v1 on the lane D-074/D-081 built: three sprigs steep into a hale-draught at the herbwife's bench (satchel cap 2, priced in sprigs NEVER coin: the herb lane's first sink, forage becoming a choice instead of pure income); 'd' gains meaning (v26 'w' precedent) and drinks it anywhere: +12 blood, -24 wound-weight, a turn for the swallow: the first remedy that walks into a deep site (the niche: shrine rest heals free at home, nothing helped in the dark). The STILLCRAFT lesson (4th lesson, 12c at her bench, D-052 pattern, free to the stead's own via D-087's gate untouched: pays D-087's deferred fourth-lesson slot with the herbwife as honest teacher): a taught shrine rest steeps a draught from carried sprigs, any world (TendedIron rider pattern). Bench entries appended so digits hold (D-041); vials on the HUD rail; Draughts in Snapshot. Save v39 -> v40. 400 tests green (5 new AlchemyTests; 2 knock-ons: the drink's turn ticks the wound one further, and the D-087 taking-stock test must now buy the fourth lesson: the teaching gate absorbed the new lesson unmodified); twins byte-identical; emit->sim exact; sweep identical (the pilot never brews). New tracked: recipes as effects arrive, the pilot learning to brew, the skill question if the lane outgrows know-how.
- 2026-07-19: **D-089 the factions get their state: stores, boldness, and the tick that gives back.** Vision §2's keystone clause lands: two causal axes on the coarse tick. The stead's STORES (Max 6, stored, reset each world): raids drain it, RationPrice rides it via PriceBump (replacing the frozen + Raids term), bared lofts END the raids as their own dark exit (fact lofts_bare, replacing the flat cap of 3), and once the camp falls the stead recovers +1/tick to full (each easing narrated, fact lofts_full): deliberately amends D-079's grain-stays-taken, since the exit now has an aftermath. The dens' BOLDNESS (DERIVED, not stored: Base 3 + Raids - Wrath): below 2 a tick raids nothing (fact dens_cowed, once: wrath's first faction-scale consequence, the cull buying the stead quiet), at 4+ the raid comes greedy and takes double, so an untouched camp bares the lofts in 4 raids. Snapshot carries Stores + Boldness. Save v38 -> v39. 395 tests green (5 new/rewritten RaidsTests; WrathTests camp helpers promoted internal); twins byte-identical; emit->sim exact (final stores 6/boldness 3: a recovered world); sweep turns/deaths IDENTICAL to baseline with raids now varying by seed (the culls showing through). Checked off the state-vectors item; new tracked lines: the stead acting on the tick, a third faction.
- 2026-07-19: **D-088 the facts answered: three storylets consume the faction graph.** The consumers the last three decisions deferred, landed in one stroke: the-steads-reckoning (Talk, prio 12: a named thief told so to their face, once per stead, gated on the live barred rung so restitution stills it, writing a `confronted` fact), the-tale-carried (NearHouse, prio 6: the hearthtale's rumor fact consumed, the tarred door-posts read differently because a story was told, nothing gained and nothing needing to be), and what-the-stead-keeps (Talk, prio 7, one under the hearthtale so the ladder keeps its order: the own rung shown the stead's deep cellar, the graph's first `secret` fact). All three lines-and-facts only: no coin, no grant, so every journey number held to the digit. Save v37 -> v38 by the v35 precedent (new storylets shift the weighted draws). 392 tests green (7 new); byte-identical + emit->sim exact + sweep unchanged. Checked off the fact-consumers item; new tracked line for `confronted`/`secret` consumers in their turn.
- 2026-07-19: **D-087 the stead's teaching: the own rung's boon.** The bright ladder's top rung finally pays: at "the stead's own" (regard 5) every lesson the stead sells is shown freely, the boon paid in the one currency in the stead's gift that crosses the arch (lessons bank on the bearer past death and waygate, D-052), where the friend rung paid in this world's coin: D-077's collision lesson applied at the top. SteadsTeaching gate beside FriendsPrice; crossing narrated with a taking-stock variant when nothing is left to show; bench/smith labels rename ("freely, to the stead's own", label text only, digits hold); the coin's refusal narrated at the showing. Suspicion (unwelcome+) closes it with a narrated withholding; live-shame gating means restitution reopens the craft. Save v36 -> v37 (a v36 journal that bought a showing at the own rung replays richer). 385 tests green (4 new RegardTests); byte-identical + emit->sim exact (baseline held: the pilot buys no lessons) + sweep unchanged. Checked off the regard-boons item (every rung pays); new tracked line for rumor/shame-fact consumers and a rung-3 storylet beat.
- 2026-07-19: **D-086 the stead's suspicion: pilfering, shame, and the coin on the sill.** The home faction's Infamy axis opens on the game's first transgression verb: g beside an overworld house pilfers the door (a ration's worth, once per door per world), thematically casting the bearer as the raiders' twin. Keyed `_factionInfamy` ledger beside the regard (wrath migrated in: each dictionary now means one thing); three houses, thresholds 1/2/3, one rung per door (watched / unwelcome / named a thief), each rung costing in its own currency: watched closes the hearthtale + opens a closed-doors NearHouse storylet (first shame-fact consumer), unwelcome closes the friend's price and purse (withholdings narrated), thief bars the larder. Shame runs beside regard, never against it (both titles on the HUD). Designed exit: 6 coin on the robbed sill walks the ladder down; repay outranks theft at shared corners (a mistaken press never commits a worse deed). Shame facts written as permanent history; live shame gates the reopenable doors. Save v35 -> v36 (the overworld g press changed meaning). 381 tests green (13 new ShameTests); byte-identical + emit->sim exact (10425 turns, matching the D-085 baseline: the pilot never robs) + sweep unchanged. Checked off the Fame/Infamy dual-axis item; new tracked ideas: the stead acting on suspicion beyond commerce, raider-perceivable transgressions for the relation matrix.
- 2026-07-19: **D-085 regard rungs become facts + the friend's hearthtale.** Every rung crossed writes a `regard` fact (known/friend/own) into the world graph, making reputation queryable by storylets, topics, and template casting with one declarative pattern: the structural seam for all reputation-gated content. First passenger: a friend-gated Talk storylet (priority 8, once per world) where a villager tells the stead's own story to a friend and never to a stranger, writing a `rumor` fact for later content. Narrative-only by design (D-077's currency-collision lesson). Closes the last D-077-named boon via the storylet channel D-080 pointed at. Save v34 -> v35 (new Talk storylet shifts the weighted draw). 368 tests green (3 new); byte-identical + emit->sim exact (keys read off the new JSON field) + sweep via --json. Advanced the boons item.
- 2026-07-19: **D-084 --wits: the keen-eyed walk.** The pilot raises Wits to baseline+2 first (D-061's perception-build identity: innate acuity clears the dulling floor), then resumes the survivability rotation. Seed 42 x6 crossings: every mastered kind holds Keen at every arch (baseline softens all to Read), at the honest price of 8 deaths vs 5. Header names the mode; JSON carries witsDemo. Default path proven untouched by hash. Cli-only, 365 tests unchanged. Checked off the last D-062/D-063 tooling deferral: that ledger is clear.
- 2026-07-19: **D-083 journey --json: the report as data.** The whole journey report as one JSON object (headline, every economy counter, arc reach/cycles, Legend + burden share, regard/wrath peaks with titles, raids, and full per-crossing sites + sworn terms + two-sided bestiary), via plain DTO records through the existing source-generated camelCase context (AOT-safe); --emit-keys folds the key string in as a nullable field. Prose path proven untouched by hash. Two JSON runs byte-identical; parsed numbers match prose exactly (10425 turns/4 deaths/12 crossings/380 herb coin). Cli-only, 365 tests unchanged. Checked off the machine-readable-report item; --wits demo is the last tooling deferral standing.
- 2026-07-19: **D-082 the bot takes the stillroom walk.** The autopilot sells its herbs at the herbwife's bench (5c) instead of the wood's edge (4c), proving the D-081 price spread live: master seed sells 76 sprigs for 380 coin, and the run SHORTENS (11101 -> 10425 turns, deaths held 4) because the stillroom sits on the smith road the bot already walks. Woodward's bench errand drops herbs; TradeOpenDigit generalized from WoodEdgeDigit; runner reads sale coin off the key (honest with two prices) and names the stillroom in the report. Cli-only, no engine/save touch, 365 tests unchanged; byte-identical + emit->sim exact + sweep uniform (44 sprigs, 220 coin every seed). Checked off the bot-stillroom item.
- 2026-07-19: **D-081 the herbwife's stillroom: second bench, vendor pattern proven.** The herbwife buys herbs at the apothecary's 5c/sprig behind her own bench (the wood's edge pays 4: the first genuine price-choice, arbitrage in miniature, the walk is the spread). Her wound-dressing moved onto the bench, forced by the 9-digit cap (topics fill 8 in a full world; conditional mend + trade would burst it) and always listed reading "you are whole" when moot (digits never shift, D-041). Two entries + flavor lines, zero new machinery: the D-071 pattern generalizes. Save v33 -> v34 (her menu digits moved; herbs pay differently). Bot untouched (mends at shrine, sells at the wood's edge; the 1-coin walk is a future policy increment). 365 tests green (3 new, 2 moved through the bench path); byte-identical + emit->sim exact + sweep unchanged. New tracked items: bot weighs the stillroom walk; alchemy lane's home is the stillroom.
- 2026-07-19: **D-080 the friend's price + the talk keeps the raid ledger.** At the friend rung the steadholder takes a coin off bread: a new RationPrice term beside the hearth-price, stacking with it, deed-earned so the hushed name silences the hearth-price and not this one (the local-vs-global split proven in one formula). Composes with D-079: a raid prices bread +1, the deed that ends raids prices it -1, so camp-clear moves bread twice over. Offer label names it every open; steadholder says it aloud once per stead. The goblin-raids topic now counts raids suffered since arrival (no new topics: villager menu at its 9-digit cap). Save v32 -> v33. 362 tests green (5 new, 2 honest updates where the discount and the blight story crossed old expectations); byte-identical + emit->sim exact + sweep unchanged. Advanced the boons item; remaining: the rumor boon, rung-3 boons.
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
