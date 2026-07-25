# Aegis Godot UI mockup review

Status: World-screen, Character Creation, Conversation, and reusable world-event
architectures approved under D-183 through D-186; Character comparison is next, no
implementation authorized

Related decisions: D-182, D-183, D-184, D-185, D-186

Mockup set: `artifacts/d182-ui-mockups-v1/`

Style-direction set: `artifacts/d182-style-directions-v1/`

Game-screen architecture set: `artifacts/d182-game-screen-architectures-v1/`

Character-creation architecture set:
`artifacts/d183-character-creation-architectures-v1/`

Conversation and commerce architecture set:
`artifacts/d184-conversation-commerce-architectures-v1/`

World-event and action-sheet architecture set:
`artifacts/d185-world-event-action-sheet-architectures-v1/`

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
5. [~] Character. Controlled architecture comparison is next.
6. [ ] Pack and equipment.
7. [ ] Journal: History, People, Bestiary, and Threads.
8. [ ] Help.
9. [ ] Settings and accessibility.
10. [ ] Campaign entry and system states.
11. [ ] Remaining focused task-surface variants.
12. [ ] Complete responsive and light/dark parity matrix.

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

Chronicle Stage was the recommendation before this refinement. The player approved
`06-map-workspace-sidebar-icons.png` under D-183 as the structural world-screen base
because it preserves more map height while making the complete live log persistent.
The approval covers layout and information hierarchy. The reference map colors are not
approved, and the remaining screens still need review before implementation resumes.

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
Production icons, exact theme tokens, and dark-theme parity remain to be completed
without changing this information hierarchy.

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

1. Settle the exact light-theme surface treatment and canonical map-glyph palette on
   the approved D-183 world geometry.
2. Approve the reusable world-event and action-sheet grammar.
3. Approve Character, Pack and equipment, Journal, Help, and Settings.
4. Approve Campaign entry, pause, save, confirmation, and boundary states.
5. Approve the focused task-surface variants and the complete responsive/theme matrix.
