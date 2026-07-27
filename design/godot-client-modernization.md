# Aegis Godot client modernization

Status: Approved under D-182, Phases 1 through 3 implemented and verified through
D-200; corrected Character Creation, world rail, and Character parity review is next

## Implementation status

Phases 1 through 3 plus the D-196 through D-200 review remediation are complete. The persistent screen foundation, typed creation projection,
Atkinson font system, five discrete UI scales, dark and light palettes, custom
square-cell map control, native creation fields, complete descriptions, resize probes,
responsive world shell, structured colored History, repaired conversation behavior,
map-only zoom, searchable Help, and complete world input ownership are built and
verified. The default world shell no longer carries the iron rose or permanent control
text. Character and Pack now use typed semantic projections and faithful D-187 and
D-188 parity candidates. The warning-free Release build, 72 focused Host tests, and
all 1,057 tests pass.

The Phase 2 packaged review found creation spacing and focus defects, incomplete runtime
theme refresh, a remaining legacy interaction surface, global rather than map-only
zoom, and shell-level information design questions. D-183 now approves the Map as
Workspace world geometry with its fixed condition, Activity, and currency sidebar,
floating launcher, and map-only context footer. It does not approve the reference map
colors. D-184 approves the full-window Focused Question creation architecture and its
non-repeating explained-selection band. D-185 approves Conversation Desk as the shared
Talk, Trade, and Services architecture, including its action list, bottom-following
transcript, persistent resource context, and selected-action explanation band. D-186
approves the centered reusable world-event sheet
with independently scrolling Event and Your Choices panes plus a fixed explanation and
confirmation band. D-187 approves Character Ledger as the dedicated Character
architecture: a section navigator, complete browse list, and deep inspector with
independent desktop scrolling. D-200 supersedes its stacked high-scale fallback with a
section selector and one-at-a-time browse-to-inspect flow. D-188 approves Pack and
equipment as Outfitter's Bench, with an unmistakable
under-requirement warning that does not block Equip. D-189 approves Journal as
Chronicle Desk, with complete current-session History, shared Activity state,
  bottom-follow ownership, and learned-only People, Bestiary, and Threads. D-190 approves
  Help Center, with search-first lookup, contextual return, canonical metadata projection,
  and a fixed quick-reference rail. D-191 approves Settings as the Live Preview Workshop,
  with grouped categories, provisional live preview, explicit save or revert actions,
  and separate semantic UI scale and map-only zoom. D-192 approves Campaign Shelf and
  the shared system-state grammar. D-193 approves Field Drawer System for the remaining
  focused task surfaces. D-194 approves the complete responsive and theme parity matrix
  under `artifacts/d193-responsive-theme-parity-review-v1/`, including the Field Drawer
  operational defaults. D-195 implements the Phase 2 review remediation without
  changing Core behavior, save v100, generator 1, or canonical keys. Dedicated
  Character, Inventory, and Equipment surfaces are next. D-196 repairs the first
  packaged-review findings: selection is click or grid-aware keyboard focus rather than
  hover, creation cards and shaping expose a clear information hierarchy and projected
  values, map zoom keeps the player visible, every Activity filter state has explicit
  contrast, and D-186 structured scenes now use the approved split event sheet instead
  of fixed-frame scraping. The reference map-glyph
  colors remain provisional pending the separate implementation-token review.

## Purpose

Replace the bounded D-181 presentation spike with a complete Godot client that keeps
the map terminal-like while making every surrounding surface modern, crisp,
responsive, accessible, and stable under keyboard and pointer input.

This is a presentation migration. `Aegis.Core`, canonical gameplay keys, journal
meaning, saves, generator 1, deterministic tools, `Frame`, and `Presenter` remain
authoritative unless an explicitly identified knowledge-ledger addition is approved
and sweep-verified.

## Player findings that define the work

The implementation is not complete until all of these findings are resolved:

1. Fonts and controls stay crisp at windowed, maximized, fullscreen, and supported UI
   scale sizes.
2. Fullscreen and resize changes cannot strand any surface outside the viewport.
3. Character creation restores every choice description from the canonical catalog.
4. Native text fields remain visible and focused while each letter is journaled.
5. Conversations open at the newest entry and return there after every interaction.
6. Map glyphs render inside true square cells without cutting off descenders.
7. Character, inventory, and equipment use dedicated modern layouts.
8. Recent words retain message colors and open into the complete current-session log.
9. The journal provides History, People, Bestiary, and Threads views without revealing
   anything the character has not learned.
10. Conversation navigation stays inside its own action list.
11. The default world shell has no permanent movement panel. Any later movement aid is
    optional accessibility UI owned by Settings.
12. Ctrl+Left/Right moves northwest/northeast, while Alt+Left/Right moves
    southwest/southeast.
13. Combat and world activity remain pinned to the newest message.
14. Closing Activity or another overlay and returning to the map leaves arrow keys owned by movement,
    never by header controls.

## Research basis

The technical recommendations follow current Godot and game accessibility guidance:

- Godot dynamic TTF/OTF fonts are vector sources. Font oversampling is enabled by
  default and re-rasterizes vector text as the viewport scale changes:
  <https://docs.godotengine.org/en/latest/tutorials/rendering/multiple_resolutions.html>
- Godot warns that MSDF removes hinting and can be less crisp at small sizes. A
  hinted dynamic font at native integer sizes is the right default for this
  text-heavy desktop client:
  <https://docs.godotengine.org/en/latest/classes/class_fontfile.html>
- Godot `RichTextLabel.scroll_following` keeps appended content visible, and
  `ScrollContainer.ensure_control_visible` supports deterministic focused-item
  scrolling:
  <https://docs.godotengine.org/en/4.4/classes/class_richtextlabel.html>
  <https://docs.godotengine.org/en/4.4/classes/class_scrollcontainer.html>
- Godot GUI controls consume arrow navigation before `_UnhandledKeyInput`. World
  movement that must outrank header focus belongs in a gated `_Input` handler:
  <https://docs.godotengine.org/en/latest/tutorials/inputs/inputevent.html>
- Explicit focus neighbors avoid Godot's geometric "best guess" focus escape:
  <https://docs.godotengine.org/en/4.5/tutorials/ui/gui_navigation.html>
- A custom `Control._Draw` is the documented efficient path for a grid or board and
  can draw text with measured ascent, descent, and width:
  <https://docs.godotengine.org/en/latest/tutorials/2d/custom_drawing_in_2d.html>
- Microsoft XAG 101 recommends configurable text up to 200 percent, at least 18-pixel
  PC text at 1080p, a sans-serif option, adequate line spacing, and one-direction
  scrolling after scaling:
  <https://learn.microsoft.com/gaming/accessibility/xbox-accessibility-guidelines/101>
- Atkinson Hyperlegible Next and Mono are designed for character distinction and are
  free for personal and commercial use:
  <https://www.brailleinstitute.org/freefont/>

Two local Godot projects supplied implementation evidence without becoming visual
templates:

- `C:\repos\personal\sandbox\tarpg` proves the square-cell invariant with a 16 by 16
  source atlas, nearest-filtered square quads, an orthographic camera, and explicit
  zoom bounds. Aegis keeps the invariant but uses measured vector glyphs at integer
  sizes so its terminal-like map remains crisp without inheriting pixel-art limits.
- `C:\repos\mud\godot` proves persistent UI state, viewport-aware panel reflow,
  draggable controls, custom-drawn maps, and a structured 2,000-entry log whose tabs,
  filters, detail level, and paused-follow state survive updates. Aegis keeps those
  state-management lessons but uses a purpose-built responsive shell rather than its
  freeform widget desktop.

The production distinction is deliberate: the references solve mechanisms, while
Aegis owns its visual system, information architecture, accessibility scale, semantic
projections, and verification contract.

## Design direction

### Subject and job

The subject is a solitary traveler reading a dangerous world through a precise
instrument. The interface's single job is to make the current situation, available
choices, and earned knowledge immediately legible without flattening the map into a
conventional graphical RPG.

### Visual identity: the field ledger

The client should feel like a modern field instrument whose records happen to belong
to Aegis. It should not imitate parchment, a terminal window, a generic fantasy HUD,
or a dashboard made from identical cards.

The signature element is the **charted margin**: a thin line with short compass ticks
that marks the active screen, selected row, and current log position. The iron rose is
the most expressive use of the same geometry. Everything else stays quiet.

Large filled buttons are reserved for primary actions. Lists use clean rows, strong
type hierarchy, a charted-margin focus mark, and restrained surface changes.

### Color tokens

Dark iron:

| Token | Value | Use |
|---|---:|---|
| Coal | `#0E1518` | Window background |
| Iron | `#151E22` | Main surface |
| Raised iron | `#1D2A30` | Selected and floating surfaces |
| Bone | `#F1EBDD` | Primary text |
| Ash | `#B3BDBC` | Secondary text |
| River glass | `#78CED0` | Focus, navigation, Aegis tone |
| Hearth | `#E2A54D` | Reward and conversation emphasis |
| Warning | `#E06F68` | Danger |

Light field:

| Token | Value | Use |
|---|---:|---|
| Field paper | `#F3EEE4` | Window background |
| Fold | `#E8E0D3` | Main surface |
| Clean sheet | `#FFFFFF` | Selected and floating surfaces |
| Ink | `#182326` | Primary text |
| Graphite | `#536166` | Secondary text |
| Deep river | `#14676E` | Focus, navigation, Aegis tone |
| Ochre | `#8A5B16` | Reward and conversation emphasis |
| Red ink | `#9F3434` | Danger |

Every listed text color clears a 4.5:1 contrast ratio against its theme background.
Color never carries state alone. Tone also receives a label, icon shape, border, or
typographic treatment.

### Typography

- Interface and data: Atkinson Hyperlegible Next, regular and semibold.
- Map, shortcuts, counts, and compact tables: Atkinson Hyperlegible Mono.
- Long conversation and authored prose: Literata, retained from D-181.
- Sentence case for navigation and controls. Short locator labels may use uppercase.
- Default body size: 20 pixels at 100 percent UI scale.
- Small metadata floor: 18 pixels at 100 percent UI scale.
- User scale choices: 100, 125, 150, 175, and 200 percent.
- Each scale generates rounded integer font sizes, spacing, radii, and margins. The
  root canvas is never fractionally scaled.

The current Azeret Mono is removed from general UI. The map remains monospaced, but
the rest of the client stops looking like a terminal.

### Layout sketches

World:

```text
┌ AEGIS   Place and time          Character  Pack  Journal  Move  Display ┐
├───────────────────────────────────────────────┬─────────────────────────┤
│                                               │ Current condition       │
│          square-cell terminal map             │ Resources and weather   │
│                                               │ Contextual actions      │
│                                               │                         │
├───────────────────────────────────────────────┴─────────────────────────┤
│ Activity, newest message visible                         Open history   │
└─────────────────────────────────────────────────────────────────────────┘
```

Creation:

```text
┌ AEGIS                                                    Display       ┐
│ Becoming   Folk  Past  Shape  Things  Burden  Vow  Memory  Name  Review│
├─────────────────────────────────────────────────────────────────────────┤
│ 01                                                                      │
│ What folk bore you?                                                     │
│                                                                         │
│ ◇ Choice name                                                           │
│   Complete canonical description                                       │
│                                                                         │
│ ◇ Choice name                                                           │
│   Complete canonical description                                       │
│                                                                         │
│ Back                                                     Continue       │
└─────────────────────────────────────────────────────────────────────────┘
```

Conversation:

```text
┌ Person and role                                           Leave        ┐
├──────────────────────────────┬──────────────────────────────────────────┤
│ Topics and actions           │ Conversation                             │
│                              │                                          │
│ > focused action             │ complete colored transcript              │
│   wrapped description        │ starts and returns at newest entry       │
│                              │                                          │
│ independently scrollable     │ independently scrollable                 │
└──────────────────────────────┴──────────────────────────────────────────┘
```

At narrow supported widths, the world status rail becomes a drawer. Conversation
stacks actions above transcript. No horizontal scrolling is required for prose.

## Client architecture

### Persistent screen tree

`Main` becomes a small coordinator. The client creates these screens once:

- `CreationScreen`
- `WorldScreen`
- `ConversationScreen`
- `CharacterScreen`
- `InventoryScreen`
- `JournalScreen`
- `MenuScreen` for remaining semantic menus and prose scenes

Changing a game state updates the active screen in place. It never frees and rebuilds
the complete surface for a normal key. A screen switch changes visibility only.

Persistent nodes own their presentation state:

- focused action identity
- scroll offsets
- transcript follow state
- selected journal tab and row
- iron rose open state and normalized position
- world rail open state
- UI theme and scale

Theme, scale, and iron rose position live in a local presentation settings file. They
never enter the game save or canonical journal.

### Semantic host projections

`Aegis.Host` exposes typed, read-only presentation records instead of asking Godot to
scrape the fixed `Frame`:

- creation choice name, description, cost, enabled state, and reason
- character attributes, skill progress, knacks, lessons, scars, and standing
- equipped slots, carried gear, requirements, durability, and resources
- log entries with turn and `LogTone`
- conversation topics and offers
- known people
- bestiary knowledge
- active and completed threads that are already player-visible

The terminal clients keep using `Frame` and `Presenter`. Godot uses `Frame` only for
the map cell layer and as a compatibility fallback for an unmodernized menu during
migration.

### Native creation input

Creation text stages use `LineEdit`, not a label pretending to be a field. The line
edit:

- keeps focus while the authoritative game value updates;
- translates edits, paste, and Backspace into the existing canonical character stream;
- prevents a host refresh from recursively re-emitting unchanged text;
- submits the existing canonical confirmation key on Enter;
- keeps the entire stage visible through fullscreen and resize changes.

### Square-cell map

`MapGridControl` draws the `Frame` directly:

1. Allocate a true square for every visible cell.
2. Fill its background from the owned Aegis palette.
3. Measure the glyph with the font's ascent, descent, and advance.
4. Center the glyph on both axes inside the square.
5. Draw at an integer origin and integer font size.
6. Request a frame size based on the actual map viewport and the selected cell size.

No map glyph is laid out by `RichTextLabel`, no BBCode is built, and no line-height
heuristic can clip the bottom of a glyph.

### Input and focus ownership

- World movement is handled in `_Input` only while `ClientSurface.World` is active.
- Text fields and non-world screens receive normal GUI input first.
- Arrow keys always move on the world, even if a header control was the last thing
  clicked.
- Ctrl+Left/Right emit `y` and `u`.
- Alt+Left/Right emit `b` and `n`.
- A map click grabs a non-button map focus target.
- Each modal or dedicated action surface has an explicit focus scope and explicit
  neighbor loop.
- Conversation Up/Down moves only through conversation actions.
- Tab may reach screen-level controls in a documented order.
- Escape closes the current surface before it can reach the world.

### Logs and transcript

The world activity ribbon always shows the newest colored entry. Opening History
shows the complete current reconstructed session.

History follows new entries while the player is at the bottom. If the player scrolls
up, it pauses and shows a "New entries" control that returns to the bottom. Combat in
the compact activity ribbon always remains at the newest entry.

Conversation always opens at the bottom and returns to the bottom after an interaction,
as requested. Its entries preserve `LogTone` colors.

### Iron rose

The iron rose is a floating `Control` with:

- eight direction actions and Wait;
- a drag handle that does not trigger movement;
- viewport clamping after drag, resize, maximize, fullscreen, or scale changes;
- normalized position persisted in local presentation settings;
- a reset-position action;
- persistent open state across conversation and other temporary surfaces.

## Modern information screens

### Character

A dedicated full-window screen replaces the centered terminal sheet:

- identity and current condition summary;
- seven attributes with plain-language effects;
- all eighteen skills with level and progress;
- earned knacks and lessons;
- scars, burden, standing, and other durable character state;
- pending knack choices as a focused decision panel.

### Inventory and equipment

A dedicated full-window screen replaces the combined terminal list:

- three clear equipped slots at the top;
- carried gear below as a sortable keyboard-friendly list;
- benefit, requirements, durability, and equipped state in separate columns;
- resource inventory in a secondary section;
- pointer click or Enter equips through the same canonical digit path;
- disabled or under-met state includes text, not color alone.
- all ten launch gear entries remain selectable: 1-9 select the first nine and 0
  selects the tenth. This replaces the current inaccessible `:` tenth-item path and
  is included in the save v101 engine sweep.

D-188 approves Outfitter's Bench as the visual and interaction base. At ordinary
desktop widths, a fixed three-slot shelf sits above a sortable carried-gear list and
selected-versus-equipped inspector, with resources in a secondary band. An unmet
requirement uses a warning icon, explicit `Reduced benefit` state, required and current
values, plain-language penalty, and confirmation copy. Equip remains available.
At narrow widths or high text scale, the shelf wraps or stacks and the list moves above
the inspector without losing the warning or confirmation path.

### Journal

One `Journal` destination contains four tabs:

1. **History**: the complete current reconstructed session, colored and filterable by
   tone.
2. **People**: people the character has actually met, with only learned role and
   relationship information.
3. **Bestiary**: existing character knowledge tiers and earned observations.
4. **Threads**: active and completed obligations, leads, and unresolved matters that
   the game already exposes to the character.

`Threads` is recommended instead of `Quests`. It supports the same usability goal
without turning the game's organic world state into a checklist of hidden objectives.

Save v101 carries the corrected tenth-item inventory key. People and Threads also
receive a small deterministic knowledge ledger if existing facts cannot prove that the
character learned an entry. That ledger is end-appended, journal-derived, and covered
by the same full engine sweep. It records discovery only and cannot affect gameplay
eligibility.

Three controlled Journal architectures are ready under
`artifacts/d188-journal-architectures-v1/`. Chronicle Desk is recommended because it
gives complete current-session History the strongest chronological reading and filtering
surface while keeping People, Bestiary, and Threads adjacent. Indexed Cabinet prioritizes
reference browsing, while Open Folio prioritizes long-form selected-record reading.
No architecture may show locks or counts for undiscovered records.

D-189 approves Chronicle Desk as the canonical Journal base. History opens by default
and preserves the complete reconstructed current session. World Activity and Journal
History share filters and reading position. Follow-tail continues only while the player
is already at the latest entry; deliberate scroll-away reveals `Return to latest`, and
`Load earlier` prepends without moving the record being read. People, Bestiary, and
Threads expose learned information only. At narrow widths or high text scale, section
tabs remain reachable and session context becomes a drawer or stacks after the
chronology without introducing horizontal prose scroll.

### Help

Help is a full-window searchable reference under the shared launcher. It owns exact
controls, task guidance, map and message legends, readability guidance, and contextual
entry from other screens. It does not own live appearance controls, which remain in
Settings. Opening Help records the invoking screen so `Return` restores the player's
prior task and meaningful focus.

D-190 approves Help Center as the canonical Help base. Global search leads into a
category rail, topic workspace, complete selected article, and contextual quick-reference
rail. Direct entry selects a relevant topic without hiding search, and Return restores
the invoking screen and meaningful focus. Production Help projects canonical controls
and presentation meanings from shared metadata wherever practical. At narrow widths or
high text scale, categories become a drawer and quick reference stacks after the article.

### Settings and accessibility

Settings owns every live appearance and accessibility control. Theme, UI scale, text
spacing, map zoom, window mode, motion, input guidance, and any optional movement aid
remain separate, inspectable settings. Map zoom changes square map cells only. UI scale
reflows semantic screens. Runtime theme changes must refresh every visible surface from
shared tokens.

Three controlled Settings architectures are ready under
`artifacts/d190-settings-accessibility-architectures-v1/`. Live Preview Workshop is
recommended because its category rail, grouped controls, fixed map, prose, Activity and
focus preview, and stable action bar keep frequent changes direct and discoverable.
Comfort Profiles prioritizes transparent before-and-after setup, while Guided
Calibration prioritizes a one-decision-at-a-time accessibility path. Generated values
and sample content are illustrative only.

D-191 approves Live Preview Workshop as the canonical Settings base. Its category rail
selects Display, Text, Map, Input, or Motion; its broad center workspace owns the active
controls; and its fixed right preview keeps map cells, prose, Activity colors, focus, and
selection visible together. Changes preview immediately but remain provisional until
Save changes. Revert restores saved values, Reset section affects only the active
category, unsaved exit offers Save, Discard, and Cancel, and window-mode changes receive
a keep-or-revert confirmation. At narrow widths or high UI scale, categories become a
drawer and the preview stacks or becomes a reachable drawer while the action bar remains
available.

### Campaign entry and system states

Three controlled campaign-entry architectures and one shared system-state board are
preserved under `artifacts/d191-campaign-entry-system-states-architectures-v1/`. D-192
approves Campaign Shelf because its welcome and primary-action column, broad campaign
shelf, and selected-record inspector balance first use, repeat entry, compatibility
review, and safe management. Campaign Gallery prioritizes a spatial personal collection.
Campaign Ledger prioritizes dense campaign and version administration. The shared state
board defines one transient grammar for Pause, non-blocking Saving, guarded deletion,
compatibility, load failure, and empty entry. Generated records and values are
illustrative only, and no save or replay semantics change through this review.

Continue resumes the most recent safely resumable campaign or explains why it cannot.
New campaign and utilities stay reachable in empty and populated states. Status uses
icon plus text, focus stays distinct from selection, guarded deletion defaults to Cancel,
and transient states restore their invoking focus. Narrow and high-scale layouts move
welcome actions into a top band before the shelf and inspector stack in reading order.

### Focused task surfaces

Three controlled reusable systems are ready under
`artifacts/d192-focused-task-surface-architectures-v1/`. Each covers Rest and shaping,
Progression choice, Action and target, Services and activities, Transition and terms,
and Fall and recovery while holding cost, requirements, projected results, disabled
reasons, cancellation, permanent-choice warning, and input parity constant.

D-193 approves Field Drawer System. A right drawer temporarily owns Activity and may
widen left over the map, while a bottom commitment tray owns cost, requirements,
projected result, confirmation, and cancellation. Activity state returns intact when the
task closes. Map input belongs only to explicit spatial targeting. Permanent choices
confirm separately, unmet requirements state why, and projected values come from
canonical semantic projections. Narrow or high-scale non-target tasks become a
full-width stack, while targeting preserves map priority. Guided Decision Path and
Contextual Workbench remain as rejected comparison references. Generated values and
sample content are illustrative only, and no engine semantics change through this
review.

The final four-board review set is ready under
`artifacts/d193-responsive-theme-parity-review-v1/`. It tests identical light and dark
geometry, the 1100 by 700 minimum, 150 and 200 percent reflow, keyboard and pointer
states, follow-tail, full-window destinations, Campaign entry, and shared boundary
states. It also presents proposed defaults for contextual selection memory, reversible
zero-cost confirmation, stable target cycling, Rest projections, and exact focus return.
These proposals do not become implementation requirements until player approval.

## Responsive contract

Supported minimum window: 1100 by 700 at 100 percent UI scale.

- At 1400 pixels and wider, world and conversation use split layouts.
- Below 1400 pixels, the status rail becomes a drawer and conversation stacks.
- At 150 percent UI scale and above, secondary columns collapse before text shrinks.
- Every screen remains reachable by one-axis scrolling at 200 percent scale.
- Resize, maximize, fullscreen, and restore each trigger one deferred layout pass after
  the new viewport size settles.
- Floating controls are clamped after that deferred pass.
- No screen stores absolute viewport coordinates except during the current layout pass.

## Implementation sequence

### Phase 1: foundation and creation

- Split `Main.cs` into coordinator, theme, input, settings, screen, and control classes.
- Add persistent screens and typed Host projections.
- Add the new font system and discrete UI scale.
- Add the square-cell map control.
- Rebuild creation with complete descriptions and native text fields.
- Add resize/fullscreen regression probes.

Checkpoint: packaged creation plus empty-world shell review.

### Phase 2: world, conversation, logs, and movement

- Build the world shell and responsive status rail.
- Restore colored logs and full History.
- Rebuild conversation with bottom-follow and scoped focus.
- Build the draggable persistent iron rose.
- Add diagonal modifier movement and world input ownership.

Checkpoint passed: packaged keyboard, pointer, resize, conversation, and combat-log
review. The exact review archive is
`artifacts/aegis-d182-phase2-win-x64-final.zip`, SHA-256
`D100ED3C364AC9246D938C42E27CE5E24CE67763FFBC031AA407395C8BCE5269`.

Review remediation passed under D-195: the approved D-183 world geometry, complete
creation route and explained selection, shared Activity and History state, Conversation
follow-tail, map-only zoom, live theme parity, Help, and narrow or high-scale
reachability are verified. The fresh Windows checkpoint is
`artifacts/aegis-d195-phase2-remediation-win-x64.zip`, built from `c9f4cdc`, SHA-256
`0259F45839FC175AF6F5184D267F79EDEC428DE6A788774C2DB5FC9B7BBF8B4D`.
Its clean extraction reports save v100 and generator 1, serves the structured frame,
completes creation, exits cleanly, and reloads the created save exactly.

D-196 follow-up remediation passes with click-only pointer selection, explicit
two-column navigation, hierarchical creation cards, projected shaping values,
player-following map zoom, complete Activity filter contrast, and direct structured
scene projection into the approved D-186 split event sheet. Both themes are verified at
1920 by 1040, with additional 1296 by 839 creation and shaping probes. The Release build
is warning-free, 66 focused Host tests and all 1,051 tests pass, and no Core change
triggers an engine sweep. The fresh Windows checkpoint is
`artifacts/aegis-d196-player-review-remediation-win-x64.zip`, built from `203e405`,
SHA-256
`DCE9C84C610AF2594A5B55ED2E4DF5AEC1BFF4AB96C7656878ABD1361392F6B6`.
Its clean extraction completes creation, reports save v100 and generator 1, serves the
structured 120 by 40 frame, exits without stderr, and reloads the created save exactly.

### Phase 3: character and inventory

- [x] Build dedicated Character (D-197).
- [x] Build dedicated Inventory and Equipment (D-197).
- [x] Preserve every canonical menu action (D-197).
- [x] Add empty, under-met, pending-choice, and long-content states (D-197).

Semantic screens landed under D-197. D-199 brought Pack to its current player-review
candidate but missed the Character reference. D-200 corrects Character with the
identity and section rail, dense active-record list, composed Overview, and deep
inspector, plus a one-at-a-time compact flow. Pack continues to use the three-slot shelf,
sortable and filterable gear table, selected-versus-equipped inspector, explicit
warning, and secondary resource band. Light, dark, desktop, narrow, and 150 percent
background probes pass. The Release build is warning-free, 72 focused Host tests and
all 1,057 tests pass, and no Core change triggers an engine sweep. The earlier packaged
checkpoint is
`artifacts/aegis-d197-phase3-information-screens-win-x64.zip`, built from `2dff334`,
SHA-256
`C4CFB2360922DCBB98F09434D2658F6F726E488F291A1DA740688F0614D9CCF0`.
Its clean extraction completes creation, reports save v100 and generator 1, serves the
structured 120 by 40 frame, exits cleanly, and reloads the exact snapshot. A new D-199
parity package is the player gate before Conversation Desk proceeds. That checkpoint is
`artifacts/aegis-d199-character-pack-parity-win-x64.zip`, built from `6e12f2e`,
SHA-256
`E690534CBBA7F1CB09980B9AEA26FB03B88CCCFF6BBCF237494197F8671C21E5`.
Its clean extraction completes creation, reports save v100 and generator 1, serves the
structured 120 by 40 frame, exits cleanly, and reloads the exact snapshot.

### Phase 4: journal knowledge

- Build People, Bestiary, and Threads projections.
- Add the bounded discovery ledger only where existing state is insufficient.
- Bump to save v101 for the corrected tenth-item inventory key and any required
  discovery ledger.
- Prove that projections reveal only earned knowledge.

Checkpoint: spoiler-safe journal review.

### Phase 5: release candidate

- Remove spike-only stress UI and compatibility shortcuts.
- Complete keyboard and pointer accessibility passes.
- Run the full Release build, focused tests, all tests, format, dash, and diff gates.
- If Core behavior or save state moved, run the complete five-seed twin, replay, and
  worldgen sweep from `HANDOFF.md`.
- Export and smoke a clean Windows x64 package.
- Validate windowed, maximized, fullscreen, all UI scales, both themes, creation,
  world, conversation, combat, Character, Inventory, Journal, save, reload, and exit.
- Start a new guided release campaign only after the client review is approved.

## Acceptance evidence

Automated:

- Host projection tests cover every screen and every creation description.
- View-model tests cover focus loops, input routing, modifier mappings, transcript
  follow rules, tone-to-style mapping, and responsive breakpoints.
- Map-control tests cover square cells, integer layout, glyph baseline bounds, and
  palette mapping.
- Resize tests cover windowed, maximized, fullscreen, restore, and every UI scale.
- Save tests cover the tenth-item key, any approved knowledge ledger, and rejection of
  unsupported versions.
- Existing engine and release suites stay green.

Packaged visual and interaction evidence:

- Background screenshots at 1100x700, 1280x800, 1920x1080, and the review display's
  native fullscreen size.
- Both themes and 100, 150, and 200 percent UI scale.
- Background pointer control without focus theft.
- Creation typing without disappearing nodes.
- Conversation and combat logs at the newest entry after repeated interactions.
- Focus cannot escape a conversation with Up/Down.
- Every map cell is square and no glyph crosses its cell bounds.
- The default world contains no permanent movement panel, and opening or closing the
  responsive Activity drawer does not transfer movement ownership to the launcher.
- Exact save and canonical snapshot parity before the release campaign begins.

## Approval package

The recommended package is:

1. Full Godot migration under this architecture.
2. Field-ledger visual system with the charted margin as its signature.
3. Atkinson Hyperlegible Next for interface text, Atkinson Hyperlegible Mono for the
   map and data, and Literata for long prose.
4. Dedicated full-window Character, Inventory, Conversation, and Journal surfaces.
5. Journal tabs named History, People, Bestiary, and Threads.
6. Ship save v101 for the corrected tenth-item inventory key. Include a deterministic
   discovery ledger only if existing knowledge cannot support People and Threads
   honestly.
7. Implement in the five phases above, with packaged review checkpoints after phases
   2 and 4 before the final candidate.
