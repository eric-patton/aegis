# Aegis Godot UI mockup review

Status: World-screen, Character Creation, Conversation, reusable world-event, Character,
Pack, Journal, Help, Settings, Campaign entry, system-state, and focused task-surface
architectures approved under D-183 through D-193; complete responsive and light/dark
parity matrix approved under D-194, implementation authorized

Related decisions: D-182, D-183, D-184, D-185, D-186, D-187, D-188, D-189, D-190, D-191,
D-192, D-193, D-194

Mockup set: `artifacts/d182-ui-mockups-v1/`

Style-direction set: `artifacts/d182-style-directions-v1/`

Game-screen architecture set: `artifacts/d182-game-screen-architectures-v1/`

Character-creation architecture set:
`artifacts/d183-character-creation-architectures-v1/`

Conversation and commerce architecture set:
`artifacts/d184-conversation-commerce-architectures-v1/`

World-event and action-sheet architecture set:
`artifacts/d185-world-event-action-sheet-architectures-v1/`

Character architecture set:
`artifacts/d186-character-architectures-v1/`

Pack and equipment architecture set:
`artifacts/d187-pack-equipment-architectures-v1/`

Journal architecture set:
`artifacts/d188-journal-architectures-v1/`

Help architecture set:
`artifacts/d189-help-architectures-v1/`

Settings and accessibility architecture set:
`artifacts/d190-settings-accessibility-architectures-v1/`

Campaign entry and system-state architecture set:
`artifacts/d191-campaign-entry-system-states-architectures-v1/`

Focused task-surface architecture set:
`artifacts/d192-focused-task-surface-architectures-v1/`

Responsive and theme-parity review set:
`artifacts/d193-responsive-theme-parity-review-v1/`

## Purpose

The D-182 Phase 2 packaged review showed that individual UI repairs are not enough to
settle the complete player experience. This review draft pauses feature implementation
and proposes one coherent shell across creation, world play, events, conversation,
Character, Inventory, Journal, History, Help, and both themes.

The generated images are layout and visual-language references only. Their names,
numbers, item catalogs, icons, map content, and prose are not canonical game content.

## Review progress ledger

Status key: `[x]` approved, `[~]` comparison ready for player review, `[ ]` not yet
reviewed.

1. [x] World-screen structure (D-183).
2. [x] Character Creation (D-184).
3. [x] Conversation and commerce (D-185).
4. [x] Reusable world-event and action sheet (D-186).
5. [x] Character (D-187).
6. [x] Pack and equipment (D-188).
7. [x] Journal: History, People, Bestiary, and Threads (D-189).
8. [x] Help (D-190).
9. [x] Settings and accessibility (D-191).
10. [x] Campaign entry and system states (D-192).
11. [x] Remaining focused task-surface variants (D-193).
12. [x] Complete responsive and light/dark parity matrix (D-194).

## Architecture-first correction

The first screen suite mixed several visual languages. That inconsistency is not an
approved eclectic direction. The next controlled set then kept one shell fixed and
changed palette, typography, borders, focus, density, and signature detail. Player
review correctly found that these were theme and surface-treatment variations on one
design, not meaningfully different product concepts.

The style-direction set is retained as a surface-treatment study:

1. `01-field-instrument.png`: cool mineral daylight, precise survey rules, restrained
   serif prose, and a functional map datum.
2. `02-blackened-brass.png`: soot-dark iron, engraved brass, compact instrument keys,
   and a stronger authored personality.
3. `03-moss-and-ash.png`: bone, charcoal, moss, clipped corners, and compact field tabs.
   It is grounded and daylight-friendly.
4. `04-winter-signal.png`: cloud, slate, fjord blue, and one rare orange waypoint. It is
   the clearest contemporary treatment.

`00-layout-master.png` is that set's neutral geometry reference. No direction from this
set is selected.

The game-screen architecture set instead holds one accessible light theme, the same
information scope, and the same terminal glyph-map premise constant while changing the
layout, hierarchy, navigation model, log prominence, and use of persistent space:

1. `01-map-as-workspace.png`: the glyph map fills the window, with restrained floating
   condition, command, and recent-activity instruments. It gives the map the most room
   and feels most like a game, but overlays can compete with the world and the compact
   activity ribbon is not a reading surface.
2. `02-split-command-deck.png`: a permanent left command spine, central map, and full
   right-side live Chronicle. It makes every primary destination obvious and gives the
   log excellent capacity, but narrows the map and risks feeling like a desktop tool.
3. `03-atlas-frame.png`: the map is the centered plate, with condition readings and a
   chronological timeline living in its margins. It is the most specific to Aegis and
   keeps the map unobstructed, but the narrow timeline is weak for longer wrapped text.
4. `04-chronicle-stage.png`: a thin unified status line, wide map stage, and persistent
   full-width Chronicle below. It gives both world and written record first-class space,
   preserves horizontal map width, and most directly answers the packaged-review request
   for useful live history. Its cost is reduced vertical map height.

Player-directed refinement `05-map-workspace-sidebar.png` combines Map as Workspace with
a fixed right sidebar. The sidebar puts proportional Health, Stamina, and Focus bars at
the top, a filtered bottom-following Activity log in the middle, and Coin plus Essence at
the bottom. A floating map launcher opens Character, Pack, Journal, and Help. A separate
map-width-only footer carries location, cycle, season, weather, and map zoom. Focus is the
canonical transient magic resource. Essence is the canonical attribute-raising currency.

Second refinement `06-map-workspace-sidebar-icons.png` makes the three transient-resource
bars thinner and icon-led, with no resource names inside them. Health remains clay red,
Stamina becomes field green, and Focus becomes blue. Activity rows drop their repeated
category labels. Field, Combat, and Words instead color the complete message text, and
their filter labels use those same green, clay-red, and brass colors.

## Conversation and commerce architecture comparison

The conversation set holds the D-183 light visual language, floating launcher, and
fixed right sidebar constant. Only the task-workspace interaction model changes:

1. `01-conversation-desk.png`: a durable Talk, Trade, and Services workspace. Topics
   and actions remain in a scrollable left list, the bottom-following transcript owns
   the larger right pane, and a full-width selected-action band explains price,
   requirements, affordability, and consequence before confirmation. This is the
   recommendation because one stable grammar handles both short exchanges and large
   catalogs.
2. `02-thread-and-cards.png`: the transcript dominates the workspace, with a horizontal
   rail of large action cards and an expanded confirmation tray below. It gives written
   exchange the calmest reading path, but four-wide cards consume space quickly and
   large commerce catalogs need another browsing state.
3. `03-exchange-table.png`: a narrow conversation rail sits beside a dense offer table
   and selected-item inspector. It is the strongest direct comparison-shopping surface,
   but makes ordinary conversation feel secondary and creates a different hierarchy
   when the player switches away from commerce.

All three preserve the persistent Condition, Activity, Coin, and Essence sidebar. They
remove the old charted connector, show local prices and affordability beside the
selected action, keep focus inside the task workspace, and reserve complete controls for
Help. The final contract must also define bottom-follow behavior, deliberate scroll-away
preservation, disabled reasons, confirmations, keyboard and pointer parity, and the
split-to-stacked responsive state.

## Approved Conversation and commerce base

- `01-conversation-desk.png` is canonical under D-185.
- The focused task workspace replaces the map region while the D-183 launcher and
  complete right sidebar remain visible.
- Talk, Trade, and Services share one mode row and one interaction grammar.
- A scrollable action list sits beside the larger bottom-following transcript.
- Deliberate transcript scroll-away pauses follow-tail and exposes a return-to-newest
  action.
- The full-width selected-action band owns explanation, price, requirements,
  affordability, consequences, disabled reasons, and confirmation.
- Buying, selling, repair, and other services reuse the same hierarchy with suitable
  list and detail content.
- Narrow or high-scale layouts stack actions above transcript and detail without
  horizontal prose scrolling.
- Exact icons, tokens, density, breakpoints, and theme parity remain implementation
  work and may not change the approved hierarchy.

## World-event and action-sheet architecture comparison

The event-sheet set holds the D-183 world shell, terminal glyph map, fixed sidebar,
map-only footer, and D-185 selected-action explanation grammar constant. Only the
sheet's relationship to the map changes:

1. `01-centered-field-sheet.png`: a large sheet floats inside the map workspace with
   visible map context on all four sides. Long prose has the widest readable measure,
   choices stack beneath it, and the selected-action band keeps a visible check,
   consequence, and confirmation together. This is the recommendation because it
   handles both short choices and long authored text while retaining place.
2. `02-docked-side-sheet.png`: a tall sheet docks beside a continuously visible map
   slice. It preserves the strongest direct spatial context and gives choices a stable
   vertical rail, but narrows long prose and leaves less room for high text scale.
3. `03-lower-stage-sheet.png`: a full map-width stage rises from the bottom while the
   upper map remains unobstructed. Prose and choices sit side by side above one
   explanation band. It feels most directly attached to movement, but gives long prose
   the least vertical room and needs the earliest stacked fallback.
4. `04-centered-field-sheet-split-scroll.png`: the player-approved refinement keeps the
   centered sheet and map context from the recommended first architecture, then divides
   its interior into independently scrolling Event and Your Choices panes. The header
   and full-width selected-action band remain fixed. This gives long prose and long
   option lists separate capacity without turning the sheet into a full-window
   destination. It is canonical under D-186.

All candidates preserve the complete right sidebar and map footer, use the custom square-cell
glyph map rather than an illustrated map, keep Close explicit, distinguish checked from
unchecked choices, and show no hidden odds. Exact generated prose, labels, numbers, and
icons are illustrative only. The final contract must cover long-text scrolling,
multi-step events, disabled choices, check disclosure, result acknowledgement, focus
return, keyboard and pointer parity, and narrow or high-scale stacking.

The split-scroll refinement adds one responsive rule to that contract: at ordinary
desktop widths, Event and Your Choices scroll independently side by side. At narrow
widths or high text scale they stack into separately bounded regions, while the header
and confirmation band stay fixed and reachable.

## Approved world-event and action-sheet base

- `04-centered-field-sheet-split-scroll.png` is canonical under D-186.
- The centered sheet keeps map context visible on all four sides and preserves the
  complete D-183 sidebar and map-only footer.
- The header remains fixed and keeps Close explicit.
- Event prose owns the left pane and its scroll position. Your Choices owns the right
  pane and its scroll position.
- The full-width selected-action band remains fixed beneath both panes and carries
  visible check state, known values, requirements, uncertainty, consequence, disabled
  reasons, and confirmation.
- Multi-step events update in place, results require clear acknowledgement, and leaving
  returns focus to the world.
- Narrow or high-scale layouts stack the separately bounded panes without surrendering
  independent scroll ownership.
- Exact icons, tokens, breakpoints, content, map colors, and theme parity remain
  implementation work and may not change the approved hierarchy.

## Character architecture comparison

The Character set holds the approved light visual language, full-window destination,
launcher, canonical resource colors, seven attributes, eighteen skills, and durable
character-state scope constant. The generated names, values, descriptions, icons, and
choice copy are illustrative. Production content must come only from existing semantic
projections. The three images change how the player browses and understands the record:

1. `01-character-atlas.png`: a three-zone overview keeps identity and condition at left,
   all eighteen skills in the broad center, and durable state plus pending choices at
   right. A fixed bottom inspector explains the selected attribute or skill. It offers
   the fastest cross-system comparison and is the recommendation for a screen the player
   will revisit often.
2. `02-character-ledger.png`: a section navigator, complete browse list, and deep
   inspector form a master-detail reference tool. It gives selected mechanics, growth,
   earned choices, and related state the strongest explanation and scales cleanly to
   large text, but only one family is readily comparable at a time. It is canonical
   under D-187.
3. `03-character-record.png`: a sticky section index navigates one continuous scrollable
   document. Attributes, skills, knacks, lessons, burden, scars, standing, and pending
   choices read as one long-lived record, with details expanding inline. It is the most
   coherent and extensible over a long campaign, but requires more vertical travel and
   makes distant systems harder to compare.

All three exclude equipment and inventory, preserve Health, Stamina, and Focus as thin
icon-led bars, expose pending progression instead of hiding it, and require selection and
keyboard focus to remain visually distinct. The final contract must cover zero, short,
and long collections; all eighteen skills; pending and resolved choices; long
descriptions; independent or single-scroll ownership as appropriate; keyboard and
pointer parity; and narrow or high-scale fallbacks.

## Approved Character base

- `02-character-ledger.png` is canonical under D-187.
- Character is a full-window destination under the shared launcher. Pack remains a
  separate destination.
- Identity plus thin Health, Stamina, and Focus bars remain visible while browsing.
- A left section navigator, complete middle list, and deep right inspector share one
  master-detail grammar across every Character section.
- The middle list and inspector scroll independently. Selection and keyboard focus remain
  distinct, and directional navigation stays within the active region.
- All seven attributes, all eighteen skills and progress, knacks, lessons, burden, scars,
  standing, and pending choices come from semantic projections.
- Pending progression stays visible and opens as a focused, fully explained decision in
  the inspector.
- Narrow or high-scale layouts replace the left rail with a compact section control and
  stack the selected list above its inspector.
- Exact icons, tokens, density, breakpoints, and generated content remain implementation
  work and may not change the approved hierarchy.

## Pack and equipment architecture comparison

The Pack set holds the approved light visual language, shared launcher, three canonical
equipment slots, ten launch gear entries, gear facts, carried resources, honest
under-requirement behavior, and keyboard or pointer reach constant. Generated values,
descriptions, icons, comparison text, and any noncanonical attribute names are
illustrative only. Production content must come from semantic projections.

1. `01-outfitters-bench.png`: a fixed three-slot equipped shelf sits above a split
   carried-gear list and comparison inspector. The selected and currently equipped items
   align fact by fact, while carried resources remain a secondary band. This is the
   recommendation because equipping is a decision, and this architecture keeps the item,
   its replacement, requirements, wear, reduced-benefit state, and confirmation in one
   reading path.
2. `02-pack-map.png`: a vertical equipment rail filters a broad field of categorized
   item tiles, with one focused inspector at right. It gives Weapon, Armor, Ranged, and
   Resources the strongest spatial identity and makes the three equipped slots tangible,
   but tiles provide less dense comparison and require earlier stacking at high scale.
3. `03-inventory-ledger.png`: a compact three-slot strip sits above one full-width gear
   table. Selection opens a fixed bottom comparison drawer rather than a side inspector,
   and Resources is a sibling view. It offers the fastest keyboard scan and the strongest
   growth path for long collections, but its dense table is less approachable for a
   first-time player.

All three keep exactly Weapon, Armor, and Ranged slots; make every one of the ten launch
gear entries reachable without the old tenth-item path; separate selection from focus;
show requirements and reduced benefit in text rather than color alone; keep resources
secondary to gear; and omit invented capacity, weight, and derived-stat systems. The
final contract must cover empty slots, no gear, ten or more gear rows, under-requirement
equipment, worn gear, already-equipped state, resource-only inventory, sorting and
filtering, confirmation, keyboard and pointer parity, and narrow or high-scale fallbacks.

## Approved Pack and equipment base

- `01-outfitters-bench.png` is canonical under D-188.
- Pack is a full-window destination under the shared launcher.
- A fixed top shelf shows exactly Weapon, Armor, and Ranged equipped slots.
- A broad sortable carried-gear list sits beside a deep selected-versus-equipped
  inspector. Carried resources remain a secondary band.
- Every one of the ten launch gear entries is reachable by keyboard and pointer.
- Unmet requirements never block Equip, but they must be unmistakable without relying
  on color: warning icon, `Reduced benefit` row state, required-versus-current values,
  inspector warning, plain-language canonical penalty, and confirmation copy.
- The inspector owns comparison, benefit, requirement, wear, applicable family or move,
  replacement consequence, and confirmation.
- Empty slots, no gear, worn gear, already-equipped items, under-requirement items,
  resources-only inventory, and long lists keep direct text and reachable actions.
- Narrow or high-scale layouts wrap or stack the equipped shelf and place the list above
  the inspector without horizontal scrolling.
- Exact icons, warning colors, tokens, density, breakpoints, and illustrative content
  remain implementation work and may not change the approved hierarchy.

## Journal architecture comparison

The Journal set holds the approved light visual language, shared launcher, learned-only
disclosure, and four sections constant: History, People, Bestiary, and Threads. No
candidate shows locks or counts for undiscovered records. Generated names, values,
descriptions, icons, and record prose are illustrative only. Production content must
come from semantic projections.

1. `01-chronicle-desk.png`: History owns a broad chronological workspace with a turn
   scrubber, grouped record stream, compact search and category filters, and a fixed
   session rail. Complete message text carries Field, Combat, or Words color without
   repeated row tags. This is the recommendation because the complete current-session
   record is the Journal's most frequent reading task, while the other three sections
   remain one top-level tab away.
2. `02-indexed-cabinet.png`: four large archival drawers select a broad alphabetized
   record index and deep inspector, with recent additions across sections in a bottom
   strip. It gives People, Bestiary, and Threads the strongest reference-library
   identity, but History becomes one collection among peers rather than the dominant
   chronological tool.
3. `03-open-folio.png`: a screen-native two-leaf workspace places a filtered record list
   on the left and the complete selected record on the right, with section index marks
   at the edge. It offers the calmest long-form reading and strongest game-specific
   character, but the paired leaves stack earlier and create more navigation travel for
   long History sessions.

All three keep History, People, Bestiary, and Threads in one destination; expose only
knowledge the character has earned; provide honest empty states; keep selection and
focus distinct; and avoid objectives, hidden steps, reward checklists, or locked
unknowns. The final contract must cover complete and long current-session History,
bottom-follow and deliberate scroll-away, loading earlier records, preserving world
Activity filters and position, zero and long learned collections, record updates,
search, keyboard and pointer parity, and narrow or high-scale fallbacks.

## Approved Journal base

- `01-chronicle-desk.png` is canonical under D-189.
- Journal is a full-window destination under the shared launcher. History opens by
  default, with People, Bestiary, and Threads one top-level tab away.
- History shows the complete reconstructed current session with turn and time grouping,
  a turn scrubber, search, category filters, and fixed session context.
- World Activity and Journal History share one structured record model. Expanding to
  History preserves filters and reading position.
- New records follow the bottom only while the player is already at the latest entry.
  Deliberate scroll-away pauses follow-tail and reveals `Return to latest`.
- `Load earlier` prepends records without jumping the current reading position.
- Field, Combat, and Words color complete messages and matching filters. Rows do not
  repeat category tags.
- People, Bestiary, and Threads reveal learned information only. They show no locked
  records, unknown counts, hidden steps, rewards, or undiscovered teases.
- Keyboard and pointer access remain equivalent. Narrow or high-scale layouts preserve
  the four tabs, use a drawer or stacked session context, avoid horizontal prose scroll,
  and keep `Return to latest` reachable.
- Exact icons, tokens, density, counts, illustrative content, and breakpoints remain
  implementation work and may not change the approved hierarchy or disclosure rules.

## Help architecture comparison

The Help set holds the approved light visual language, shared launcher, searchable
reference, contextual return, keyboard and pointer parity, map legend, message-color
legend, and a route to Settings constant. Generated controls, icons, key labels,
symbol meanings, and explanatory text are illustrative only. Production Help must be
derived from the canonical input and presentation contracts rather than copied from
the images.

1. `01-help-center.png`: a task-oriented Help Center puts global search first, then uses
   a category rail, topic cards and selected article, plus a fixed contextual quick
   reference. This is the recommendation because it scales from controls to concepts,
   provides the shortest route from a question to an answer, and gives contextual Help
   links from every screen an obvious landing place.
2. `02-command-atlas.png`: a control-first command constellation places the movement
   cluster and surrounding command families at the center, with a live selected-command
   inspector and fixed legend band. It is the strongest at-a-glance control reference,
   but longer concepts, accessibility guidance, and non-command topics become secondary
   appendages.
3. `03-field-manual.png`: a reading-first continuous manual uses sticky chapter anchors,
   a narrow progress rail, editorial two-column chapters, and inline legends. It offers
   the calmest guided learning path and strongest long-form continuity, but known-answer
   retrieval requires more scrolling and contextual links have less precise targets.

All three remove permanent control text from the world shell, keep Help available from
the shared launcher, support search, separate map zoom from UI scale, expose equivalent
keyboard and pointer methods, and route appearance changes to Settings. The final
contract must cover exact canonical controls, context-sensitive entry, search with no
results, map and message legends, long and short topics, reduced-motion and readability
guidance, focus and selection, return to the invoking screen, and narrow or high-scale
fallbacks. Help explains behavior but does not own live settings controls.

## Approved Help base

- `01-help-center.png` is canonical under D-190.
- Help is a full-window destination under the shared launcher. It records the invoking
  screen and meaningful focus so `Return` restores both.
- Global search is first in the reading order. Contextual entry selects the most
  relevant category or topic without bypassing search.
- A category rail, topic workspace, complete selected article, and contextual
  quick-reference rail form one lookup path from short command questions to long
  conceptual guidance.
- Production content derives exact controls and presentation meanings from canonical
  metadata wherever practical. Individual scenes do not own copied command strings.
- Search supports direct and tolerant matching, keyboard navigation, honest no-result
  guidance, and clearing without losing invoking context.
- Map and message legends expose current non-secret presentation meanings. Help explains
  accessibility and appearance, while Settings owns the live controls.
- Keyboard and pointer methods remain equivalent, with focus distinct from selection.
- Narrow or high-scale layouts turn categories into a drawer, stack quick reference
  after the article, keep one-axis article scrolling, and preserve search, Return, and
  the Settings route.
- Exact icons, typography, topic copy, matching thresholds, density, and breakpoints
  remain implementation work.

## Settings and accessibility architecture comparison

The Settings set holds the approved light visual language, shared launcher, crisp
vector-text intent, immediate preview, theme parity, separate UI scale and map zoom,
window mode, text spacing, motion, keyboard and pointer parity, visible focus, reset
actions, and return to Help constant. Generated values, copy, map symbols, and sample
content are illustrative only. Final controls must follow the approved Settings
contract and platform capabilities.

1. `01-live-preview-workshop.png`: a category rail selects Display, Text, Map, Input, or
   Motion. The broad center workspace owns grouped controls, while a fixed right preview
   studio shows map, prose, Activity color, and focus together. A stable action bar owns
   reset, revert, and save. This is the recommendation because it keeps every setting
   discoverable, makes UI scale and map zoom visibly independent, and gives frequent
   revisits the shortest path without hiding what will change.
2. `02-comfort-profiles.png`: transparent profile plates lead into a large before and
   after comparison, an inspectable `What changes` rail, and a compact tuning drawer.
   It offers the fastest accessible starting point and makes every bundled change
   explicit, but profiles add a second mental model above the individual settings.
3. `03-guided-calibration.png`: a six-stage path asks one comfort question at a time,
   compares live samples, preserves completed choices, and ends at a full review. It is
   the strongest first-run accessibility experience, but routine changes require more
   navigation than the workshop.

All three keep map zoom independent from UI scale, preview runtime theme and readability
changes before commitment, expose keyboard and pointer access, preserve a clear route
back to Help, and require full light, dark, and high-contrast token refresh. The final
contract must settle save semantics, restore defaults, unsaved-change handling, window
mode transitions, exact UI scales, text spacing, map zoom limits and shortcuts, reduced
motion, input reference or rebinding scope, optional movement assistance, focus return,
and narrow or high-scale fallbacks.

## Approved Settings and accessibility base

D-191 approves `01-live-preview-workshop.png` as the canonical Settings architecture.
The left rail selects Display, Text, Map, Input, or Motion; the center owns grouped
controls for the active category; and the fixed right preview keeps a square-cell map
sample, prose, Activity colors, and focus treatment visible together. A stable action
bar owns Reset section, Revert, and Save changes.

Changes preview immediately but remain provisional. Save commits, Revert restores the
last saved values, and Reset section affects only the active category. Leaving with
unsaved changes offers Save, Discard, and Cancel. Window-mode changes receive their own
keep-or-revert confirmation. UI scale reflows semantic screens, while map zoom changes
square map cells only and retains Ctrl+minus, Ctrl+plus, and Ctrl+0. Reduced motion never
changes engine pacing. Input guidance does not promise arbitrary v1 rebinding.

At narrow widths or high scale, categories become a drawer and the preview stacks after
the controls or becomes a reachable preview drawer. The action bar and invoking-screen
return remain reachable with one-axis scrolling. Exact tokens, values, zoom limits,
platform window modes, transition timings, and optional movement-aid controls remain
implementation work.

## Approved Campaign entry and system-state base

The campaign-entry set holds the approved light visual language, spoiler-free record
summaries, Continue, New campaign, campaign selection, compatibility, load, guarded
deletion, Settings, Help, Quit, version visibility, keyboard and pointer parity, and
distinct focus and selection constant. Generated dates, times, campaign counts, status
values, icons, and registration marks are illustrative only.

1. `01-campaign-shelf.png`: a narrow welcome and primary-action column leads into a broad
   vertical campaign shelf, while a fixed right inspector owns the selected campaign's
   compatibility, metadata, and Resume, Load, and Delete actions. This is the
   approved base because it welcomes a first campaign without sacrificing efficient
   scanning, clear compatibility, or safe management as the list grows.
2. `02-campaign-gallery.png`: one large selected campaign volume sits between smaller
   previous and next volumes, with Resume and More actions attached directly to it. It
   has the warmest, most game-like entry and strongest sense of a personal collection,
   but browsing many campaigns takes more navigation and comparison is less direct.
3. `03-campaign-ledger.png`: a broad campaign table, inline compatibility notice, and
   deep command panel optimize version review, precise keyboard movement, and a larger
   campaign collection. It is the strongest campaign-management tool, but its registry
   density makes the first launch feel more administrative.

`04-system-state-grammar.png` is the approved shared boundary-state board rather than a
fourth campaign-entry option. It defines one transient-surface grammar for Pause, non-blocking
Saving, guarded Delete campaign, Older version, Could not load, and No campaigns yet.
Destructive confirmation names its target, states permanence, defaults focus to Cancel,
and visually separates the destructive action. Saving does not invent a percentage when
progress is not measurable. Compatibility and load failures state what is known and
offer the next safe action without a technical trace. Empty entry preserves Settings,
Help, and Quit beside New campaign.

D-192 makes Continue resume the most recent safely resumable campaign or explain why it
cannot. New campaign remains distinct. The selected record's compatibility, metadata,
and actions remain visible in the inspector. Status uses icon plus text, focus remains
distinct from selection, and transient surfaces return focus to their invoking record.
At narrow widths or high UI scale, welcome actions become a top band before the shelf and
inspector stack in reading order. Exact campaign naming, manual save selection, supported
compatibility upgrades, save-failure and exit behavior, safe deletion mechanics, and
whether Pause owns a manual Save now action remain implementation work. The architecture
approval does not change save or replay semantics.

## Approved focused task-surface base

The focused task set covers six remaining families together: Rest and shaping,
Progression choice, Action and target, Services and activities, Transition and terms,
and Fall and recovery. Every option holds the approved light visual language, semantic
cost and requirement display, projected results, disabled reasons, explicit cancellation,
keyboard and pointer parity, distinct focus and selection, permanent-choice warning, and
spoiler-free sample content constant. Generated labels, values, icons, maps, and
progression routes are illustrative only.

1. `01-contextual-workbench.png`: a reusable three-zone work surface gives context a
   narrow rail, choices or items the broad center, and selected consequences a fixed
   inspector. A stable action bar owns cost, confirmation, and cancellation. Targeting
   keeps the map readable and uses a compact context rail plus selected-action tray.
   It gives long content, comparison, requirements,
   and projected consequences enough room without forcing routine actions through many
   steps or keeping the map behind tasks that do not need it.
2. `02-guided-decision-path.png`: every task becomes a visible route with one decision
   and one explanation at a time. It provides the calmest learning and strongest
   irreversible-choice pacing, but makes repair, trade, rest, and other repeated work
   slower and adds backtracking between related selections.
3. `03-field-drawer-system.png`: a right-edge drawer and bottom commitment tray keep the
   map present through every task. It gives targeting the strongest spatial continuity
   and makes short actions feel immediate, but long lists, detailed comparison,
   permanent choices, and 150-200 percent UI scale compete with map space even when the
   map has no bearing on the decision.

D-193 approves Field Drawer System. The drawer temporarily owns the Activity region and
may widen left over the map, while the bottom commitment tray owns cost, requirements,
projected result, confirmation, and cancellation. Drawer width follows the task.
Activity returns with its filters, scroll position, and follow-tail state unchanged.
The map accepts input only during explicit spatial targeting. Permanent choices use a
separate irreversible confirmation, unmet requirements use icon plus text and state why,
and projected values come from canonical semantic projections rather than duplicated
client rules.

At narrow widths or high UI scale, non-target drawers become a full-width task stack
beneath the launcher. Targeting prioritizes the map and collapses secondary detail before
shrinking cells or text. Exact selection memory, reversible zero-cost confirmation,
target cycling, Rest summary contents, focus-return anchors, and the complete responsive
and theme matrix remain to be settled. This approval does not change costs,
availability, targeting, recovery, progression, world transition, or deterministic
engine semantics.

## Final responsive and theme-parity review

Four grouped boards are ready under
`artifacts/d193-responsive-theme-parity-review-v1/`. They validate the approved
D-183 through D-193 system rather than propose another architecture:

1. `01-theme-parity.png`: the same world and Field Drawer geometry, content hierarchy,
   semantic states, and focus treatment in complete light and dark themes.
2. `02-responsive-geometry.png`: wide desktop, the supported 1100 by 700 minimum,
   150 percent Character Creation, and 200 percent stacked Conversation. UI scale
   reflows the interface without changing map zoom.
3. `03-interaction-states.png`: distinct focus and selection, unmet requirements,
   reversible zero-cost action, permanent choice, spatial target mode, and follow-tail
   ownership.
4. `04-destinations-boundaries.png`: full-window tasks, narrow Campaign entry, and
   system-state parity across both themes.

The boards carry these approved operational defaults under D-194:

- Selection memory lasts while a drawer remains open, through a nested confirmation,
  and while acting on the same contextual entity. A later reopen starts from the
  current contextual default rather than a stale unrelated selection.
- A zero-cost action needs no second confirmation when it is immediate, fully
  described, and reversible. Spending resources and irreversible choices still require
  explicit confirmation.
- Tab and Shift+Tab cycle valid targets in a stable order: nearest first, then clockwise
  from north, then canonical entity identity as the tie-break. Pointer hover previews,
  click selects, and Confirm commits.
- Rest shows current and projected Health, Stamina, and Focus, elapsed time, resource
  cost, and known immediate consequences. It does not expose unknown future outcomes.
- Closing returns focus to the exact invoking control or map cell. If that anchor no
  longer exists, focus returns to a safe world default.

The validation contract is:

- Light and dark themes preserve geometry, hierarchy, states, semantic colors, and
  readable contrast. Theme changes update every tokenized surface immediately.
- The 1100 by 700 minimum and 100 through 200 percent UI scales keep every action
  reachable through one-axis scrolling. Text and cells do not clip or overlap.
- UI scale and map zoom remain separate. Responsive reflow never changes map position
  or zoom.
- Keyboard and pointer paths expose the same actions. Focus, selection, disabled,
  warning, error, and follow-tail states remain distinguishable without color alone.
- Full-window destinations, campaign entry, system states, conversation, events, and
  Field Drawers share one visual language in both themes.

D-194 approves these defaults and boards as the final visual contract. Phase 3
implementation may resume after the approved Phase 2 remediation checkpoint.

Chronicle Stage was the recommendation before this refinement. The player approved
`06-map-workspace-sidebar-icons.png` under D-183 as the structural world-screen base
because it preserves more map height while making the complete live log persistent.
The approval covers layout and information hierarchy. D-194 approves the final parity
matrix. The production map-glyph palette remains implementation-token work.

## Approved world-screen base

- The map is the dominant workspace.
- A floating upper-left launcher opens Character, Pack, Journal, and Help.
- A fixed right sidebar keeps Health, Stamina, and Focus at the top, Activity in the
  middle, and Coin plus Essence at the bottom.
- Resource bars are thin and icon-led. Values remain visible inside them.
- Activity is scrollable, filtered, and bottom-following. Category color belongs to the
  complete message and matching filter, not a repeated row tag.
- The map-only footer owns place, cycle, season, weather, and map zoom.
- Text size and map zoom remain separate.
- Permanent control text and the default iron rose do not occupy the world shell.
- Map glyph colors, final surface treatment, exact icons, responsive collapse behavior,
  and final spacing remain to be settled.

## Character-creation architecture review

The first remaining screen family now has a controlled five-image architecture set.
Every image holds the D-183 light visual language, stage, option count, and content
scope approximately constant while changing navigation, comparison, progress, and
information density. Generated descriptions, traits, icons, attribute labels, and
stage names are illustrative, not canonical.

1. `01-focused-question.png`: one decision fills the screen. A horizontal ten-stage
   route sits above a two-column choice field, selected detail band, and stable footer.
   It has the calmest focus and best scaling path. It shows enough choice detail without
   making creation feel like character administration.
2. `02-comparison-workbench.png`: option list, selected-choice explanation, and live
   character summary form three permanent panes. It supports precise mechanical
   comparison and later shaping stages well, but exposes too much ledger at the moment
   the player should be making one human-scale choice.
3. `03-scrolling-ledger.png`: all ten stages share one continuous vertical document.
   The current stage expands while future stages remain visible below. It makes
   backtracking and total progress concrete, but feels most like a form and gives future
   stages unnecessary visual weight. The generated launcher and glyph ornament are
   rejected as violations of the focused creation contract.
4. `04-choice-gallery.png`: six tall choice plates make selection feel most like a game
   screen, with a selected-detail shelf beneath. It has the strongest spatial identity,
   but long canonical descriptions and supported narrow or 200-percent layouts would
   force an early fallback from the six-column composition.
5. `05-focused-question-explained-selection.png`: the player-directed hybrid keeps
   Focused Question's calm architecture and concise mechanics in each choice card, then
   turns the selected-detail band into a teaching surface. Each selected consequence is
   named once and explained in plain language before Continue. The reference uses
   canonical folk effects instead of illustrative statistics or starting items.

## Approved Character Creation base

D-184 approves `05-focused-question-explained-selection.png` as the canonical
architecture. It uses `01-focused-question.png` as the structural base, but its bottom
selected-detail band explains what each mechanical consequence does instead of
repeating the terse card summary. Cards summarize; the detail band teaches. A gain,
tradeoff, and qualitative benefit receive separate explanation cells when present.

An optional context-sensitive summary drawer may serve shaping, text entry, and final
review, but never becomes a permanent third pane. The two-column choice field becomes
one column at narrow widths or high text scale. Text entry replaces the choice field
without moving the question, progress route, or footer. Final review uses the same
canvas as a readable summary, so creation remains one coherent screen family.
Production icons and exact theme tokens remain implementation work. Dark-theme parity
is required by D-194 without changing this information hierarchy.

## Findings from the Phase 2 package

1. Creation choice rows, prompt text, and the bottom action bar can touch or cross their
   containing edges. The full creation surface needs a real spacing contract.
2. Starting theme colors are correct, but changing theme at runtime does not refresh
   every existing control and style override.
3. The first text field on creation step 9 does not reliably receive focus when entered
   from step 8.
4. At least one world interaction still falls back to the legacy Frame surface instead
   of using the modern Godot shell.
5. The current scale control changes the complete interface. Player zoom should change
   only the map, with Ctrl+minus, Ctrl+plus, and Ctrl+0 shortcuts.
6. The world footer does not yet behave like a useful scrollable activity record. The
   compact footer and expanded History should be two states of the same log.
7. Conversation and purchase surfaces do not keep relevant resources, especially coin,
   visible beside priced actions.
8. Permanent control instructions consume world space that should belong to the map and
   activity log. A searchable Help surface can own the complete reference.
9. Modifier diagonals may make the iron rose unnecessary. Its removal or accessibility
   role needs an explicit player decision before more work is spent on it.

## Proposed shell contract for review

- Keep text size and map zoom as separate settings. Text size reflows the interface;
  map zoom changes square map cells only.
- Use one persistent Activity model. The world presents its compact dock, and Expand
  opens the complete Journal History view at the same position and filters.
- Give every focused task its own modern semantic surface. Creation, Character,
  Inventory, Journal, Help, and conversation are full-window. Short world events use a
  focused interaction sheet over the still-present modern world shell.
- Put resources beside decisions that consume them. Coin, requirements, affordability,
  and the selected action stay in one visual reading path.
- Remove permanent control legends from gameplay. Keep contextual keycaps on the action
  they activate, and put the complete reference in Help.
- Treat theme parity as a contract: every tokenized surface must update immediately,
  including panels, rows, borders, scrollbars, map colors, focus, and transient sheets.
- Remove the iron rose from the default world shell. If retained, it should be an
  optional accessibility control rather than primary navigation.

## Mockup index

1. `01-character-creation.png`: full-window creation, generous spacing, visible
   step progression, and immediate text focus.
2. `02-world-live-activity.png`: terminal map, condition rail, map-only zoom, Help, and
   a scrollable Activity dock that expands into History.
3. `03-event-interaction.png`: modern focused event sheet and resource visibility.
4. `04-conversation-trade.png`: actions, bottom-follow transcript, coin, selected-action
   context, and affordability in one surface.
5. `05-character.png`: full-window Character overview with tabbed depth.
6. `06-pack-equipment.png`: accessible pack list, item detail, comparison, and equipment.
7. `07-journal-history.png`: complete History plus People, Bestiary, and Threads.
8. `08-help.png`: searchable controls and accessibility reference.
9. `09-theme-parity.png`: identical world geometry in complete light and dark themes.

## Remaining screen and view inventory

These are the remaining design families to settle before Phase 3 implementation. One
approved family may cover several mechanically distinct states when the interaction
shape is the same.

### Full-window destinations

1. **Campaign entry**: title treatment, Continue, New campaign, campaign slots, load,
   safe deletion, version visibility, and quit. This is newly tracked production UI;
   the current checkpoint still relies on launch arguments.
2. **Character creation**: all ten stages, text entry, backtracking, final review,
   keyboard and pointer parity, long descriptions, and narrow or high-scale layouts.
3. **Character**: identity and condition, seven attributes and their effects, all
   eighteen skills and progress, knacks, lessons, burdens, scars, standing, and pending
   progression choices.
4. **Pack and equipment**: equipped slots, carried gear, resources, requirements,
   durability, comparison, sorting, all ten launch gear entries, and empty or
   under-met states.
5. **Journal**: History, People, Bestiary, and Threads as one destination, including
   filters, learned-only disclosure, honest empty states, new-entry behavior, and long
   histories.
6. **Help**: searchable controls, map legend, category legend, accessibility guidance,
   and contextual links from other screens.
7. **Settings and accessibility**: light and dark theme, text size, map zoom, window
   mode, input reference, optional movement assistance, reset actions, and immediate
   live preview.

### Focused task surfaces

8. **Conversation and commerce**: topics, offers, transcript, coin and other relevant
   resources, requirements, affordability, item or service detail, confirmations, and
   the split-to-stacked responsive state.
9. **World event and prose interaction**: a focused modern sheet over the approved
   world shell for authored scenes, choices, visible checks, long wrapping, and
   keyboard or pointer selection.
10. **Rest and character shaping**: rest results, attribute raising, costs, current and
    projected values, disabled reasons, and confirmation.
11. **Progression choice**: pending knack or comparable permanent choice, side-by-side
    consequences, irreversible-choice confirmation, and return to the originating
    screen.
12. **Action and target selection**: casting, aiming, thrusts, heaves, and other
    direction prompts, with the map still readable and cancellation unambiguous.
13. **Trade, craft, service, and activity menus**: buying, selling, repair, reading,
    crafting, games, and other current submenu families through one reusable
    item/action-sheet grammar rather than legacy terminal panels.
14. **World transition and terms**: the crossing flow, selectable terms, summary,
    confirmation, and responsive long-content behavior.
15. **Fall and recovery**: consequence summary, current Toll and recoverable resources,
    the return-to-play action, and later reclaim status without exposing unavailable
    information.

### System and boundary states

16. **Pause and confirmations**: resume, settings, help, save state, return to title,
    quit, destructive confirmations, and controller or keyboard focus return.
17. **Loading, save, empty, and error states**: campaign load progress, unsupported or
    damaged save messaging, no learned entries, no carried items, unavailable actions,
    and recoverable presentation-settings failures.
18. **Responsive and theme parity matrix**: the approved world base and every screen
    family at supported window sizes, fullscreen, 100-200 percent text scale, map zoom
    levels, light and dark themes, keyboard, and pointer input.

## Recommended review order

1. Character creation, because it is the first complete player journey and already has
   known defects.
2. Conversation and commerce, plus the reusable event sheet, because these retire the
   remaining legacy world interactions.
3. Character, then Pack and equipment, because they are Phase 3's core destinations.
4. Journal, including History, People, Bestiary, and Threads.
5. Help and Settings, which settle discoverability, theme, scale, zoom, and any optional
   movement assistance.
6. Campaign entry, pause, save, confirmation, and error states.
7. Rest, progression, targeting, activity, transition, and recovery variants, built
   from the approved destination and task-surface grammar.
8. Final responsive and theme-parity sheet across every approved family.

## First-pass critique

- The creation concept is the strongest layout proposal. Its exact step labels are
  illustrative and must be replaced by the canonical creation sequence.
- The world and theme-parity concepts establish the recommended shell. The live Activity
  dock is the key structural change.
- The event concept's centered sheet is useful, but its illustrated map is rejected.
  The shipped custom square-cell glyph map remains unchanged beneath modern event UI.
- The conversation concept is directionally strong, but the charted connector crosses
  the reading path. Keep the context column and remove that connector.
- Character and Inventory demonstrate information architecture, not final mechanics.
  Every field must come from existing semantic projections, with no invented statistics.
- The Inventory concept still carries a wide shortcut strip. Remove it in the next pass
  and let Help own the reference.
- Journal navigation is sound, but locked icons can imply hidden secrets. Undiscovered
  sections need honest empty states without teasing unknown content.
- Help is the strongest secondary-screen concept. Its command list still needs exact
  canonical input review before approval.
- The theme comparison is the target parity contract, not evidence that the current
  implementation satisfies it.

## Decisions remaining before implementation

The visual contract is approved through D-194. Settle the exact canonical map-glyph
palette during implementation-token review without reopening the approved architecture,
responsive behavior, or interaction rules.
