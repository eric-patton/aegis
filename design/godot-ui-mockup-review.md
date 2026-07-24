# Aegis Godot UI mockup review

Status: World-screen architecture approved under D-183; remaining screens under review,
no implementation authorized

Related decisions: D-182, D-183

Mockup set: `artifacts/d182-ui-mockups-v1/`

Style-direction set: `artifacts/d182-style-directions-v1/`

Game-screen architecture set: `artifacts/d182-game-screen-architectures-v1/`

## Purpose

The D-182 Phase 2 packaged review showed that individual UI repairs are not enough to
settle the complete player experience. This review draft pauses feature implementation
and proposes one coherent shell across creation, world play, events, conversation,
Character, Inventory, Journal, History, Help, and both themes.

The generated images are layout and visual-language references only. Their names,
numbers, item catalogs, icons, map content, and prose are not canonical game content.

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

## Decisions requested before implementation

1. Settle the exact light-theme surface treatment and canonical map-glyph palette on
   the approved D-183 world geometry.
2. Approve the Character Creation screen family.
3. Approve Conversation and commerce plus the reusable event and action-sheet grammar.
4. Approve Character, Pack and equipment, Journal, Help, and Settings.
5. Approve Campaign entry, pause, save, confirmation, and boundary states.
6. Approve the focused task-surface variants and the complete responsive/theme matrix.
