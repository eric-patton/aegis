# Aegis 1.0 guided-playtest remediation

Status: Automated gate passed under D-180; second packaged review found release blockers

Source: first guided 1.0 candidate playtest, 2026-07-23

This document preserves the usability and behavior findings from the rejected terminal
candidate so they are not lost during the SadConsole replacement. The replacement fixes
terminal-owned presentation, but it does not by itself close the findings below.

The player approved the complete bounded remediation package on 2026-07-23. D-180
classifies the findings, locks the interaction design, authorizes implementation, and
records the verified result.

## Release classification

### 1.0 release blockers

- Clipped, unwrapped, low-contrast, or otherwise unreadable required information.
- Character creation without conventional name editing, a safe back path, clear
  progress, understandable prompts, attribute explanations, and a final review.
- Conversations whose responses cannot be wrapped, revisited, or scrolled.
- Missing explanations for map symbols, equipment condition and requirements,
  non-equipment possessions, food and drink, crafting, combat, parrying, and other
  required shipped systems.
- Friendly bodies that can trap or repeatedly obstruct the player without a clear
  deterministic way through.

### Required 1.0 interaction work

- The presentation-only iron rose compass, toggled by `~` or a visible Move control,
  with all eight directions and a center Wait control.
- Visible keyboard focus, arrow navigation, Enter confirmation, Escape back, mouse
  selection, disabled states, and scrolling across interactive menus.
- Redesigned character, inventory, equipment, conversation, field-guide, and creation
  surfaces.

### Reproduction before behavior changes

- The reported blocked-building approach in the named review save.
- The reported ranged-enemy pursuit stall.
- The reported companion obstruction, since the current engine already supports a
  friendly-body position trade and the remaining failure may be follow behavior or
  presentation rather than collision.

### Post-1.0

- Floating damage numbers and ornamental combat animation.
- Controller and touch support.
- Click-to-path world movement.
- A separate authored tutorial location. The approved 1.0 answer is contextual teaching
  plus a permanently available field guide.

## Navigation and direct interaction

- Provide an easier diagonal-movement path in addition to the existing keyboard
  bindings.
- Consider a presentation-only compass overlay toggled by `~` and by a visible button.
  It would expose all eight directions as clickable controls, with a center wait action.
  Each direction would emit an existing canonical movement key, so opening the overlay
  would not enter the save journal or advance game time.
- Keep keyboard and mouse paths at parity. Mouse interaction must not make any required
  action unavailable from the keyboard.
- Preserve number shortcuts for experienced players while adding visible focus,
  arrow-key navigation, Enter to choose, and Escape to go back where those meanings are
  safe and approved.
- Reproduce the reported blocked-building approach and determine whether it is a map,
  collision, or interaction-discovery defect.
- Reproduce companion obstruction during ordinary movement, make each follower legible,
  and decide how the player passes, swaps with, directs, or otherwise avoids being
  trapped by a friendly body.

### Locked interaction contract

- The compass is a compact iron rose laid over the map, not a generic gamepad pad.
- Opening or closing it, moving focus, hovering, and scrolling are local presentation
  state. They do not advance time, consume RNG, or enter the save journal.
- The eight direction controls emit the existing `h/j/k/l/y/u/b/n` characters. The
  center emits the existing `.` wait character.
- A visible Move control and `~` toggle the compass. Mouse and keyboard have equal
  access to every required action.
- Number shortcuts remain live. Arrow keys move focus inside menus, Enter activates the
  focused action, and Escape returns through the current interface where safe.
- Backspace and Enter in creation text fields translate to the existing `-` erase and
  `.` confirm characters. They do not create control characters in the journal.
- Creation backtracking is a new journaled engine action and advances the save format to
  v100. It restores the previous complete creation checkpoint, including provisional
  attributes, belongings, knowledge, facts, ledgers, and log state.
- The creation wizard exposes progress and a final review. Choices stay provisional
  until the final confirmation.
- Entering a friendly follower's cell trades positions when legal. If the reproduced
  obstruction occurs after that trade, the repair may add one stable legal sidestep.
  Failure remains explicit rather than silent.

## Modern interactive screens

- Replace text-only menu interaction with an intentional SadConsole interaction layer
  that supports mouse selection, keyboard focus, clear selected and disabled states,
  wrapping, scrolling, and consistent back and confirm behavior.
- Redesign the character screen so attributes, skills, progression, and current effects
  are aligned and explained in plain language.
- Redesign inventory and equipment as related but distinct views. Show equipped slots,
  carried items, requirements, durability as labeled values rather than unexplained
  fractions, and where non-equipment finds are recorded.
- Redesign conversations with a selectable topic list and a wrapped, scrollable
  transcript area. Long responses must remain readable.
- Keep every gameplay action mapped to the existing deterministic command surface where
  possible. If a proposed interface needs a new journaled command or changes what an
  existing key means, stop for a separate engine decision, save-version ruling, and the
  complete verification sweep.

### Locked screen structure

- Character: overview, seven attributes with plain-language effects, all eighteen skills
  with progress explained, and current conditions.
- Inventory: carried supplies, trade goods, permanent possessions, and non-equipment
  finds in named sections.
- Equipment: equipped slots and carried gear, requirements, mechanical benefit, and
  condition written as a labeled remaining value rather than an unexplained fraction.
- Conversations: selectable topics and actions beside a wrapped, scrollable transcript.
- Field guide: contextual first-time guidance plus a permanent reference for movement,
  interaction, the map, equipment, food and drink, crafting, melee, ranged play, magic,
  telegraphs, and parrying.
- Creation: a responsive wizard with progress, plain explanations, Back, conventional
  text editing, and a final review.

The presentation model may carry semantic action regions, transcript entries, and
screen context beside the canonical cells. Those observations are read-only. Gameplay
actions still resolve through `Game.ApplyKey`.

## Character creation and onboarding

- Add an approved way to revisit earlier creation choices without using attribute swaps
  as an accidental undo mechanism.
- Consider Backspace for deleting name characters and Enter for confirmation, while
  retaining explicit keyboard alternatives and exact replay behavior.
- Make every creation prompt's purpose clear before the player commits.
- Explain what each attribute affects and show the consequence of raising or lowering it.
- Wrap or resize long choices so no text crosses a panel boundary.
- Add a bounded playable onboarding sequence or contextual teaching path for movement,
  interaction, inventory, equipment, food and drink, melee, ranged combat, magic,
  telegraphs, and parrying.
- Add a map legend or inspectable legend surface that distinguishes terrain, people,
  companions, creatures, objects, and entrances without relying on color alone.

## Readability and feedback

- Validate the owned palette against the reported low-contrast secondary text and tune
  it during visible review.
- Keep the entire required frame visible and verify the right-side information area at
  supported display scales and window sizes.
- Fix alignment of long skill names and audit the complete character sheet for stable
  columns.
- Add player-controlled log or transcript scrollback.
- Explain inventory durability, ammunition, charges, or other fractional values at the
  point where they are shown.
- Explain the mechanical purpose of eating and drinking at the point of use.
- Decide whether combat feedback for 1.0 should remain text and cell based or gain a
  bounded SadConsole layer such as impact flashes and brief floating values. Animation
  must never advance turns or obscure required information.

## Behavior and content findings

- Reproduce the reported ranged-enemy pursuit stall and determine whether it is intended
  positioning or a pathfinding defect.
- Add variation to repeated caught-crime responses without moving deterministic RNG
  streams.
- Clarify how crafting is found and performed.
- Clarify parry availability, timing, cost, and result.
- Clarify what a newly acquired non-equipment object is and where it can be reviewed.

## Recommended design boundary

The recommended 1.0 remediation is a bounded usability tranche:

1. Fix inaccessible, clipped, ambiguous, or trapping behavior.
2. Add the presentation-only compass overlay and conventional menu navigation.
3. Add interactive character, inventory, equipment, and conversation surfaces that
   translate to existing canonical commands.
4. Add enough contextual teaching that a first-time player can understand the shipped
   systems without an external guide.
5. Defer ornamental animation and deeper interface expansion unless visible testing
   shows they are required for legibility.

The player approved this boundary as D-180 on 2026-07-23.

## Implementation result

- The semantic interaction layer exposes visible actions, disabled states, focus,
  mouse hit regions, conversation transcript entries, and focus-free local UI pilot
  actions without creating a second gameplay command language.
- The iron rose provides all eight directions and wait. The toolbar opens Move, Pack,
  You, Guide, and Log surfaces.
- The responsive creation wizard supports conventional editing, complete checkpoint
  backtracking, a final review, and v100 journal replay.
- The guide, first-time help, log, conversations, sheet, pack, and equipment views wrap
  and label the shipped information required by this contract.
- Exact reproduction found the approach and companion reports to be interaction
  discovery problems, not collision failures. Existing diagonal movement and friendly
  position trading remain the engine rules and are now taught visibly.
- Exact reproduction confirmed the ranged pursuit stall. A ranged-wounded wolf now
  closes independently. Caught-response variation uses a derived deterministic choice
  without moving an RNG stream.
- Windows DPI ownership prevents operating-system scaling from leaving unused black
  space around the logical frame.

## Visual system

The owned IBM 8x16 font remains the sole face. Hierarchy comes from disciplined
sentence-case labels, spacing, dividers, and the owned palette:

- Clear iron: `#0C1016`
- Raised panel: `#161D27`
- Reading white: `#E8EDF3`
- Aegis cyan: `#60D3E7`
- Worked brass: `#F4C660`
- Danger red: `#F47076`

The iron rose is the one deliberately expressive element. Other surfaces remain quiet,
rectilinear, and information-led.

## Verification boundary

- Presentation-only focus, hover, scroll, and compass visibility must leave snapshots,
  journals, turns, and RNG unchanged.
- Every mouse action that changes the game must prove the same canonical key path as its
  keyboard equivalent.
- Save v100 must reject v99 explicitly unless a migration is separately approved.
- The engine implementation receives the complete HANDOFF sweep: Release build and full
  tests, five twin journeys, drift comparison, seed-1 replay, and worldgen purity.
- Development visual review confirms palette, wrapping, wide layout, DPI behavior,
  guide, sheet, pack, and iron-rose placement. The fresh clean-package campaign remains
  the human gate for first-time legibility and physical interaction.

## Verification evidence

- Release build: zero warnings and zero errors.
- Complete tests: 1,007 passed, zero failed, zero skipped.
- Default and release journeys: five byte-identical twin pairs each, all at cycle 13
  with twelve crossings.
- Seed-1 default and release journals replay exactly.
- Generator 1: 240 worlds, zero digest mismatches, zero hard failures, byte-identical
  report to the prior baseline.

## Second packaged review, 2026-07-24

The automated frame gate did not prove human layout quality. The second packaged review
found the following active blockers:

- The bitmap font does not scale cleanly enough for the supported presentation.
- A light theme is required for players who find the dark palette difficult to read.
- Creation should be a focused full-screen surface rather than a modal over the live map.
- The iron rose should restore its open mode after a conversation ends.
- Long scene and action text still truncates or crosses its intended boundary.
- The fixed conversation split clips action labels and leaves too little responsive room
  for the transcript.

The player has reopened the presentation-host decision. No further remediation is built
until the player chooses between another SadConsole pass and a Godot .NET presentation
migration. Either direction must preserve deterministic canonical commands, v100 save
meaning, and focus-free pilot verification.
