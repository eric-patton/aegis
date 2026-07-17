# Storylets and the Fact Graph: Format Spec (v1)

The content unit for all authored narrative (D-018 architecture, D-021 storylet decision).
This spec defines the storylet anatomy, the trigger surfaces, the fact-graph schema
conventions, and the selection rules the engine implements. World-story templates
(`design/story/world-story-templates.md`) and the Aegis arc (`design/story/aegis-arc.md`)
compile down to storylets in this format.

Decision record: D-030 in `../design/decisions.md`.

## 1. Principles

- **Atomic.** A storylet is one complete beat: it fires, it lands, it is done. No storylet
  requires another to have meaning (iron rule: complete micro-reveal per touch).
- **Precondition-gated, never scheduled.** A storylet states what must be true for it to
  make sense; the engine decides when. Authors write conditions, not timelines.
- **Facts in, facts out.** Preconditions read the fact graph and game state; effects write
  facts back. Content that only reads is flavor; content that writes moves the world.
  Lore-only storylets that neither gate on nor write anything specific are banned
  (iron rule 12).
- **Deterministic.** Selection draws from a dedicated seed-tree stream per world. Same
  seed, same keys, same stories. A storylet firing is replayable from the input journal
  because it is a pure consequence of state.
- **Additive to simulation v1.** Storylets emit lines and write facts and small grants;
  they do not alter combat, movement, or worldgen outcomes. This keeps every pre-storylet
  save journal replaying bit-identically (D-028) without a version bump. When a future
  storylet must alter simulation, that lands with a save version bump.

## 2. Storylet anatomy

| Field | Meaning |
|---|---|
| `Id` | Stable string, kebab-case, globally unique (e.g. `grievance-voiced`). |
| `Trigger` | The surface this storylet can fire on (section 3). |
| `Tile` | For tile-keyed triggers: which terrain. |
| `Scope` | `World` (eligible again in the next world) or `Character` (once per character, ever). |
| `Once` | If true, fires at most once per scope. |
| `CooldownTurns` | Minimum turns between firings (repeatable storylets only). |
| `Weight` | Relative selection weight when several storylets are eligible on the same trigger. |
| `Priority` | Selection tier: only the highest-priority eligible candidates enter the weighted draw. Template-emitted story beats use 10; asides and flavor use the default 0, so a plot moment is never lost to an ambient line. |
| `Requires` | Declarative fact patterns that must all match (section 4). |
| `Forbids` | Declarative fact patterns that must all be absent. |
| `When` | Optional compiled predicate over game state for what facts cannot express (cycle, position, flags). Kept small; anything reusable should become a fact or a named condition. |
| `Lines` | The beat itself: (text template, tone) pairs. Templates may reference `{settlement}`, `{world}`, and captures from matched facts (section 4). |
| `Effect` | Optional deterministic mutation: write facts, grant coin/essence. Runs after lines. |

Authoring format today is a C# catalog (`StoryletCatalog.cs`): AOT-safe, type-checked,
zero parser. Every field above is plain data except `When` and `Effect`. The format is
designed so a future external data file (JSON per storylet pack) maps 1:1, with `When`
replaced by a small named-condition vocabulary and `Effect` by named-effect entries.
That migration happens when content volume or modding demands it, not before.

## 3. Trigger surfaces

Storylets only ever fire at these hook points, checked in the engine:

| Trigger | Fires when | Notes |
|---|---|---|
| `Arrival` | The player wakes in a world (game start or crossing). | The cold-open surface. |
| `EnterTile` | The player steps onto a tile of terrain `Tile`. | Shrine, Waygate, CampEntrance. |
| `NearHouse` | The player steps onto a walkable tile adjacent (8-way) to a House. | Houses are not walkable; this is "visiting the settlement". |
| `Rest` | The player rests at the shrine. | Fires after the rest heal, before the menu closes. |
| `DeedWritten` | A `deed` fact is written to the graph. | The consequence surface. A storylet answering a SPECIFIC deed must `Require` it: worlds hold several deeds (camp, barrow), and the trigger fires for each. Learned live in D-033. |
| `AmbientTurn` | A rolled chance each turn that advances while on the overworld. | Low-stakes flavor; the pacing-director seam (D-021.5 grows here). |
| `Talk` | A conversation opens (bump-to-talk, D-031). | `When` narrows to a specific speaker (e.g. the plaintiff). |

One trigger event fires at most one storylet: all eligible candidates are collected,
one is picked by weight. Nothing queues; a beat that misses its moment stays gated
and waits for the next moment.

## 4. Fact-graph schema conventions

`Fact(Id, Type, Subject, Object, Detail)` is unchanged. Conventions:

- `Type` is a lower_snake noun naming the relation. Reserved types so far:
  `world_name`, `settlement`, `rest_point`, `site`, `grievance`, `deed`, `echo`,
  `person`, `wanderer`; from storylets: `met`, `boon`, `noticed`; from templates
  (D-032): `role`, `promise`, `story_complete`.
- `Subject` is the thing the fact is about (a site id, a settlement name, a storylet id).
- `Object` is the other party or a coordinate pair, `""` when unary.
- `Detail` is prose for surfacing in content; never parsed, never load-bearing.

A **fact pattern** in `Requires`/`Forbids` is `(Type, Subject?, Object?)` where null
means wildcard. The first fact matching a `Requires` pattern is captured and its
fields are available to line templates as `{r0.subject}`, `{r0.object}`, `{r0.detail}`
(index = position in the Requires list).

Storylets record their own footprint in the graph when it matters to other content:
a storylet that introduces a character writes `met`, one that grants something lasting
writes `boon`. `Once` handles repetition; facts handle cross-content knowledge.

## 5. Selection and pacing

1. Hook fires with context (tile, deed, etc.).
2. Candidates: trigger matches, tile matches, `Once`/scope not spent, cooldown elapsed,
   all `Requires` match, no `Forbids` match, `When` passes.
3. If no candidates: nothing happens (silence is always acceptable).
4. Candidates below the highest present `Priority` drop out; one winner is picked
   from the rest by weighted draw from the world's storylet RNG stream
   (`Derive(worldSeed, "storylets")`, re-derived each crossing).
5. Winner's lines land in the log with their tones; effect runs; firing is recorded
   (fired-set by scope, cooldown timestamp).

`AmbientTurn` additionally rolls one chance die per eligible turn before collecting
candidates, so ambient content stays sparse regardless of how much of it exists.
This roll happens every eligible turn whether or not content exists, keeping the
draw count, and therefore every later draw, deterministic as the catalog grows.

## 6. What this deliberately defers

- External data files and a condition/effect vocabulary (needs content volume first).
- The full pacing director (D-021.5): act awareness, drought/glut balancing, hostility
  tier tuning. `AmbientTurn`'s chance roll is its seam.
- ~~Role casting and template compilation~~ Landed as v0 in D-032: a template compiles
  at worldgen into role facts plus cast-bound storylets (`WorldStory.cs`), merged with
  the global catalog per world. The storylet format did not change, as predicted.
- Dialogue trees: storylets deliver beats as log lines today. When a scene UI exists,
  `Lines` grows scene directions without changing gating.
