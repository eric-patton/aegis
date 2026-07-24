# Aegis 1.0 guided-playtest remediation

Status: Draft backlog, classification and design approval pending

Source: first guided 1.0 candidate playtest, 2026-07-23

This document preserves the usability and behavior findings from the rejected terminal
candidate so they are not lost during the SadConsole replacement. The replacement fixes
terminal-owned presentation, but it does not by itself close the findings below.

No item in this document is approved for implementation merely because it is listed.
The player decides substantive behavior and interface changes after reviewing options.
The next design pass must classify each item as a 1.0 release blocker, an important 1.0
repair, a post-1.0 enhancement, or a finding that first needs reproduction.

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

This recommendation remains unapproved until the player reviews the classification and
the interaction design.
