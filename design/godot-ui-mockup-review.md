# Aegis Godot UI mockup review

Status: Review draft, not approved, no implementation authorized

Related decision: D-182

Mockup set: `artifacts/d182-ui-mockups-v1/`

## Purpose

The D-182 Phase 2 packaged review showed that individual UI repairs are not enough to
settle the complete player experience. This review draft pauses feature implementation
and proposes one coherent shell across creation, world play, events, conversation,
Character, Inventory, Journal, History, Help, and both themes.

The generated images are layout and visual-language references only. Their names,
numbers, item catalogs, icons, map content, and prose are not canonical game content.

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

1. Approve, revise, or reject the integrated Activity dock and expanded History model.
2. Approve removal of the iron rose, or retain it only as an optional accessibility
   control.
3. Approve full-window focused information screens and the centered event sheet.
4. Approve separate text-size and map-zoom controls.
5. Select which mockups require another visual pass before the contract is recorded.
