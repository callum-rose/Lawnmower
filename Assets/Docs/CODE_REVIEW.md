# Code Review — Lawnmower (state as of March 2021)

This review covers the game code in `Assets/Scripts`, `Assets/Tests`, and the loose files
outside `Assets/`. Third-party code (Graphy, GUI Kit, Lean Touch, DOTween, Odin, IUnified,
the boids demo) is out of scope except where it affects the architecture.

It is a read-through only: the project was **not** opened in Unity, built, or run, so
behavioural claims are from tracing the code. Where something is a likely bug rather than
a certain one, it says so.

Severity tags: **[bug]** wrong behaviour, **[risk]** works today but fragile,
**[cleanup]** dead / duplicated / misleading code.

---

## 1. Summary

The core idea is sound and much of the groundwork is good: the tile model is plain C#
objects with events, the undo system is clean and tested, and there is already a
headless code path that the level editor window uses to play levels without building any
GameObjects. The "drive the mower backwards to author a level" edit mode is a genuinely
clever design for guaranteeing solvable levels.

The main problems are:

1. **The "headless" layer isn't actually headless.** The game rules live in a set of
   `ScriptableObject` services (`MowerMovementManager`, `LevelTileInteractor`,
   `LevelStateChecker`, `HeadlessLevelManager`, …) that hold per-level runtime state, wire
   themselves up in `OnEnable`, and depend on subscription order. They can only be run
   via `CreateInstance` + `Construct`, and the classes are `internal` in an assembly the
   tests can't see into. As a result there is **not a single test that plays a level**,
   which was the whole point of the separation.
2. **Game rules are scattered** across ~8 types, so there is no single "simulation" you
   can hand a level and a list of moves.
3. **The game and the headless path behave differently**: the in-game undo system
   defers every action to the end of the frame, the editor one runs it immediately.
4. **An unfinished refactor** (`TileData`/`TileType` → `Tile` subclasses) left a lot of
   dead and duplicated code behind.
5. A handful of real bugs, mostly around springs, level unlocking, and undo grouping.
6. **Everything is in one `Scripts` assembly**, so nothing enforces the model/view
   boundary, `Editor` folders inside it aren't treated as editor-only, and namespaces have
   circular dependencies. Section 9 proposes an assembly layout and migration order.

The recommended way forward (section 8) is to extract a pure C# `LevelSimulation` that
owns all rules and state, test it hard, and make everything Unity-side a thin
presenter/view around it.

---

## 2. Architecture as it stands, through an MVP lens

### What you were aiming for

```
 Model (pure C#, no Unity)  ◄── Presenter (wires input → model, model events → view) ──►  View (MonoBehaviours)
```

Headless = run the Model (and maybe the Presenter) with no View.

### What actually exists

```
                    ┌────────────────────── "Headless" layer: ScriptableObject assets ─────────────────────┐
 Input  ─► MowerInputEventChannel ─► MowerMovementManager ──Moved/Bumped──► LevelTileInteractor ─► Tile.TraverseOnto
 (Keyboard/Touch/   (SO event channel)  │  uses ILevelTraversalChecker (SO)      LevelStateChecker ─► win / fail
  UI arrows)                            │  pushes UndoableMove into IUndoSystem   HeadlessLevelManager ─► spring wiring,
                                        ▼                                                                 init order
                                   MowerMover (position)                                                  
                    └──────────────────────────────────────────────────────────────────────────────────────┘
                                        │ EventProperty                         │ Tile events / GrassHeight EventProperty
                                        ▼                                       ▼
 View:                  MowerTransformAnimator, JumpAnimationComponent…   GrassTileObject, StoneTileObject…
                        (bound by MowerManager / MowerObject)             (bound by TileObjectFactory)
 Scene glue:            GameManager ─► MowerManager ─► HeadlessMowerManager
                                   └─► LevelManager  ─► HeadlessLevelManager
```

Mapping to MVP:

| Role | Where it lives | Verdict |
|---|---|---|
| **Model** | `Tile` + subclasses, `LevelData` / `EditableLevelData`, `MowerMover`, `EventProperty<T>` | Mostly good. Plain C#, observable, undo-aware. Some leakage (below). |
| **Presenter / rules** | `MowerMovementManager`, `LevelTileInteractor`, `LevelStateChecker`, `LevelTraversalChecker`, `EditModeLevelTraversalChecker`, `EditModeLevelTileUpgrader`, `HeadlessLevelManager`, `HeadlessMowerManager` | This is where it went wrong: these are really *model logic* (game rules) implemented as Unity `ScriptableObject` services with mutable runtime state. |
| **View** | `*TileObject`, `MowerTransformAnimator`, `JumpAnimationComponent`, particles, `LevelObjectFactory` | Good. Views bind to read-only interfaces (`IReadonlyGrassTile`, `IMowerPosition`) and only listen. |
| **Scene glue** | `GameManager`, `LevelManager`, `MowerManager` | Works, but `LevelManager`/`MowerManager` are "decorators" over the headless SOs rather than presenters. |

So strictly it's closer to **Model–View with observable models (MVVM-ish binding) plus a
service layer of ScriptableObjects** than MVP. That's fine — the important property
(views only observe; nothing in the rules touches a `GameObject`) mostly holds. The
problems are in the middle layer.

### What works well in the separation

- **Views are passive.** `GrassTileObject.Bind` subscribes to `IReadonlyGrassTile`
  events and only renders. `MowerTransformAnimator` only listens to
  `IMowerPosition.CurrentPosition`. No view mutates the model.
- **Read-only interfaces for views** (`IReadonlyTile`, `IReadonlyGrassTile`,
  `IReadonlySpringTile`, `IReadonlyStoneTile`, `IMowerPosition`) are exactly right.
- **`EventProperty<T>` + `Xor` "inverted" flag** threads undo direction all the way to the
  view, so views can animate forwards and snap backwards
  (`MowerTransformAnimator.AnimationPosition`). Nice touch.
- **`LevelEditorWindow_PlayEdit.cs` proves the concept**: it constructs the whole rules
  layer with `CreateInstance` + `Construct(...)` and plays a level inside an editor window
  with no scene objects.

### Where the separation leaks or breaks

1. **[risk] Rules hold runtime state in ScriptableObject assets.**
   `MowerMovementManager._mowerMover`, `LevelTileInteractor._levelData`,
   `LevelStateChecker._levelData/_wasLevelCompleted`, `HeadlessLevelManager.Level` are
   per-play state stored on *project assets*. With domain reload disabled, or between
   scene loads, that state survives and can leak between levels. Assets are singletons by
   nature, so you can't run two simulations at once (e.g. a solver, or a test running in
   parallel with an open editor window).
2. **[risk] Wiring depends on `OnEnable` and subscription order.**
   `LevelTileInteractor` subscribes to `MowerMovementManager.Moved` in `OnEnable`;
   `LevelStateChecker` subscribes in `Init`. The win/fail check is only correct because
   the interactor happens to subscribe first, so tiles are mutated before they are
   checked. Nothing enforces that. The `Construct(...)` methods call `OnEnable()` by hand,
   which double-subscribes if they are ever called on an asset that already ran
   `OnEnable` with its serialized references.
3. **[risk] The tests can't reach the model.** Everything interesting (`GrassTile`,
   `LevelData`, `HeadlessLevelManager`, `LevelTileInteractor`, …) is `internal`, there is
   one `Scripts.asmdef`, and there's no `[assembly: InternalsVisibleTo("Tests")]`. Even
   with that fixed, the tests would still have to go through `ScriptableObject.CreateInstance`.
4. **[risk] The model depends on Unity/third-party code.** `GrassTile` uses
   `UnityEngine.Mathf`, `Tile` uses `Newtonsoft.Json` for cloning, `TileData` uses
   Sirenix attributes, `LevelData` is a `SerializedScriptableObject`. None of this is
   fatal, but it stops the model being a `noEngineReferences` assembly, which is the
   cheapest way to *guarantee* headlessness.
5. **[bug] Game vs headless behave differently.** In-game, `MonoUndoSystem.Do` queues
   the action and runs it at `WaitForEndOfFrame`. The editor/headless path uses
   `UndoSystem`, which runs it immediately. So in game:
   - `MowerMovementManager.MoveToPosition` checks traversability and captures
     `MowerPosition` *before* any previous same-frame move has been applied. Two inputs in
     one frame (very possible with UI arrows + keyboard, or a fast swipe + tap) are both
     computed from the same starting square.
   - The rules are effectively asynchronous in game and synchronous in tests, so a
     headless test passing doesn't prove the game behaves the same.
6. **[risk] Edit-mode is a flag copied into several objects.** `IHasEditMode` is
   implemented by `LevelTileInteractor`, `EditModeLevelTraversalChecker`,
   `HeadlessLevelManager`, `LevelManager` and `LevelEditorManager`, and the editor window
   sets them one by one. `HeadlessLevelManager.IsEditMode` is never read.
   `Tile.IsTraversable(bool editMode)` also takes an edit-mode flag, but every caller
   passes `false`, so `GrassTile`'s edit-mode branch is dead.
7. **[cleanup] Presenter-ish logic in the wrong place.** Spring behaviour is wired in
   `HeadlessLevelManager.Init` rather than in the tile or the movement rules; the
   `GameManager` handles persistence, dialogs and scene loading directly.

---

## 3. What works well

- **Core loop:** move → mow → win/fail is implemented and correct for grass/stone/water/
  wood/empty (`GrassTile`, `LevelTileInteractor`, `LevelStateChecker`).
- **Undo system** (`Game/UndoSystem`). Small, generic, has a limit, redo, and decent
  edit-mode tests (13 cases). The `Xor` inversion type is a neat way to compose
  "undo" with "edit mode" (`Xor.Combine(IsEditMode, isUndo)` in `LevelTileInteractor`).
- **Reverse-play level authoring.** In edit mode, driving the mower *grows* grass and
  `EditModeLevelTileUpgrader` rewrites tiles as you drive (configured in its asset: empty →
  grass 1, grass 3 → wood), ending on a height-1 tile. Any level authored this way is solvable by construction
  (play the recording backwards). This is the best idea in the codebase and worth
  keeping front and centre.
- **Level editor window** (`Levels/Editor/LevelEditorWindow*.cs`): an Odin window with a
  tile palette, undo, play and play-edit modes, all headless.
- **`FlexGrid<T>`** — a grid that can grow in any direction, with tests. Good fit for the
  editor.
- **Views**: grass shader/particles/dirt trails, mower animation components each bound
  through small interfaces (`INeedMowerPosition`, `INeedPositioner`). Easy to add/remove
  visual features.
- **Level progress / metadata** saved per level GUID (`PersistantData.LevelModule`).
- **Event channels** (`Core/EventChannels`) give decoupled cross-scene communication;
  `IUnified` containers let you serialize interface references.

---

## 4. What doesn't work (bugs)

| # | Where | Problem |
|---|---|---|
| B1 | `HeadlessLevelManager.cs:88` | **Spring ignores undo.** The spring handler calls `MoveToPosition(LandingPosition)` regardless of the `inverted` flag. Undoing the move *onto* a spring fires `TraverseOnto(..., inverted: true)` on the spring, which launches the mower again and pushes a **new** action onto the undo stack in the middle of an undo. |
| B2 | `HeadlessLevelManager.cs:88` | Spring launch is recorded as a second, separate undo step (headless: nested `Do`; in-game: re-queued in `MonoUndoSystem`'s loop). One undo puts the mower back *on the spring*, not where it was. The lambdas are also never unsubscribed in `Clear()`. |
| B3 | `MowerTransformAnimator.cs:85-93` | **Spring jump isn't animated.** Non-adjacent moves hit an empty `else {}` so the mower model stays on the spring tile while the model says it's at the landing square. |
| B4 | `LevelStateChecker` + spring | Win/fail is evaluated on the *outer* move's `Moved` event, which (headless) fires after the nested spring move already ran and evaluated. Two evaluations per spring move; with B1 this can raise `LevelCompleted`/`LevelFailed` with the wrong inversion. |
| B5 | `LevelDataManager.SetLevelCompleted` | **Off-by-one / progress regression.** Completing level *i* sets `LevelsCompleted = i`, and `IsLevelLocked(index) => index > LevelsCompleted`. Completing level 0 leaves level 1 locked; replaying an earlier level *lowers* progress. Should be `Max(LevelsCompleted, i + 1)`. |
| B6 | `LevelDataManager.unlockAllLevels` | Serialized but never read. |
| B7 | `MultiUndo.Undo` | Undoes children in forward order. Undo must run in reverse (`for i = Count-1 … 0`) for anything order-dependent, e.g. two moves grouped in one frame. |
| B8 | `MonoUndoSystem` | See §2.5 — actions captured against stale state when more than one is queued per frame. |
| B9 | `LevelData.IEnumerable.GetEnumerator` | `yield return GetEnumerator();` yields **one element, the enumerator itself**. Any non-generic enumeration (e.g. some Odin/inspector paths, `foreach` over `IEnumerable`) gets garbage. Should be `return GetEnumerator();`. |
| B10 | `GrassTile.TraverseOnto` | The "grass height too high" guard checks the *old* value before setting, and the getter clamps anyway, so it can never fire. Harmless but misleading. |
| B11 | `EditableLevelData(Tile[,], GridVector)` | `mowerStartPosition` parameter is ignored. |
| B12 | `GameManager.OnLevelFailed` | Entirely commented out. Failing a level gives no feedback in game except whatever listens to `LevelRuinedChannel` (nothing in code does). |
| B13 | `GameManager.Begin` | Creates a new `LevelDataRecorder` each level and never `Dispose`s the old one, so old recorders stay subscribed to the undo system. |
| B14 | `Editorr/EditorTileUpdator.cs:48` | In-scene editor tile placement throws `NotImplementedException`. `LevelEditorManager` is still in `Game.unity`. |
| B15 | 9 files (listed in §9.2) | `using UnityEditor;` at the top of runtime code, outside `#if UNITY_EDITOR`. Code inside an asmdef is compiled into the player regardless of folder name, so this should fail a player build. Fix before attempting any build. |
| B16 | `/EditorTileUpdator.cs` (repo root) | Not compiled by Unity (outside `Assets/`), and wouldn't compile anyway (`Monobehaviour`). |
| B17 | `SpringTileObject.OnDrawGizmos` | `FindObjectOfType<Positioner>()` every gizmo draw, and NREs before `Bind` is called (`_data` null). |
| B18 | `TileUpgradePair.IsMatch` | Matches tiles by `ToString()` (which includes the full type name and, for grass, the height). Renaming a namespace/class silently breaks every saved upgrade pair. |

---

## 5. What needs consolidating

1. **Game rules → one place.** Traversability (`Tile.IsTraversable`,
   `LevelTraversalChecker`, `EditModeLevelTraversalChecker`), movement
   (`MowerMovementManager`, `MowerMover`), tile interaction (`LevelTileInteractor`),
   tile-specific behaviour (spring wiring in `HeadlessLevelManager`), win/fail
   (`LevelStateChecker`) and edit-mode tile upgrading (`EditModeLevelTileUpgrader`) are all
   one concern: *apply a move to a level*. See §8.
2. **Edit mode → one flag.** Replace the `IHasEditMode` fan-out with a single mode on the
   simulation (or two strategies: `PlayRules` / `AuthorRules`).
3. **Level representations.** There are five ways to hold a grid of tiles:
   `LevelData.newTiles` (Odin), `LevelData.tilesData` (JSON string of the same thing),
   `EditableLevelData` (`FlexGrid`), `ReadOnlyTiles`, and `Serialised2dArray`. Pick one
   runtime type (`Tile[,]` wrapped in a small `Level` class) plus `FlexGrid` for editing.
4. **Serialization.** `LevelData` serializes tiles **twice** — Odin (`[OdinSerialize]`)
   and JSON (`tilesData`) — and `OnAfterDeserialize` overwrites the Odin result with the
   JSON one. Choose one (JSON is the more portable and diffable).
5. **Undo systems.** Make the headless `UndoSystem` the only implementation and drop the
   frame-grouping in `MonoUndoSystem`. If grouping is needed (it is for springs), group
   explicitly: the simulation returns one compound undoable per player move.
6. **Two level editors.** `Levels/Editor` (OdinEditorWindow, works) and
   `Levels/Editorr` (in-scene, half-migrated, throws). Keep the window; delete or rebuild
   the in-scene one on top of the new simulation. The `Editorr` folder name is a typo
   workaround for Unity's special `Editor` folder; rename to `InSceneEditor` if kept.
7. **Manager pairs.** `LevelManager`/`HeadlessLevelManager` and
   `MowerManager`/`HeadlessMowerManager` exist only to let the headless version be an
   asset. Once the simulation is a plain object, each pair collapses into one scene
   presenter.

---

## 6. What is redundant / dead

Safe to delete once the refactor is finished (verify no scene/prefab references first):

- `Game/Levels/FlexGrid_Old.cs`
- `Game/Levels/BiDirectionList.cs` + `Tests/EditMode/BidirectionalListTests.cs` (only used by `FlexGrid_Old`)
- `Game/Levels/TEMP_TileDataToTileeConverter.cs`
- `Game/Tiles/TileData.cs`, `TileType.cs`, `TileDataDrawer.cs`, `GrassTileSetupData.cs`
  (old data model; `TileType` doesn't even include `Spring`)
- `Game/Tiles/ReadOnlyTiles.cs` (unused)
- `Core/Serialised2dArray.cs` (unused)
- `Game/Levels/ILevelData.cs` (fully commented out)
- `Game/Mowers/MowerMoverData.cs` + its asset (acceleration/friction from an older physics mover; no code reads it)
- `Game/Mowers/IMowerManager.cs` (empty marker interface)
- `MowerMover.Bump` (empty)
- `Game/Tiles/GrassDecimator.cs` (not referenced from code — check scenes)
- `Game/Tutorial/TutorialStage.cs` (empty enum) — or fill it in
- `TileObjectFactory` commented-out pooling code
- `GameManager.OnLevelFailed` commented-out body
- Repo root: `EditorTileUpdator.cs`, `Game/` folder, empty `lawnmower/` folder
- `Assets/GUI Kit Mono Round` — imported in the last commit, not wired up

`Assets/AmplifyShaderEditor/` is gitignored but its `.meta` is committed; the generated
shaders still work, but editing them needs ASE reinstalled.

---

## 7. What is conflicting

| Conflict | Details |
|---|---|
| Sync vs async undo | `UndoSystem` (editor/tests) runs immediately; `MonoUndoSystem` (game) defers to end of frame. Same rules, different timing. |
| Two edit-mode switches | `Tile.IsTraversable(editMode)` vs `EditModeLevelTraversalChecker`. Only the latter is used. |
| Two serializers for one field | Odin `newTiles` vs JSON `tilesData` in `LevelData`. |
| Two tile models | `TileData`/`TileType` (struct + enum) vs `Tile` subclasses. `TileType` lacks `Spring`; `TileData` can't represent springs or stone direction. |
| `LevelManager.IsEditMode` | Forwards to `HeadlessLevelManager.IsEditMode`, which nothing reads, while the real flags live on the interactor and traversal checker. |
| Asset-scoped state vs scene lifetime | SO services are global and outlive scenes, but are `Init`/`Clear`-ed by scene objects. Missing a `Clear` (e.g. exceptions, editor stop) leaves stale state for the next level. |
| `IsRunning` vs `_isPlaying` | `MowerMovementManager` has both, set by different callers (`HeadlessLevelManager`, `LevelEditorManager`, start/stop channels). Either one silently drops input. |

---

## 8. Recommended strategy

The goal is: **give a level and a list of moves to a plain C# object in an NUnit test and
assert the outcome**, with the game, the editor window and any solver all using that same
object.

### Phase 0 — make it open and build (half a day)

1. Open in Unity 2020.2.3f1 first (exact version), confirm it runs, commit any
   reserialization. Then decide whether to upgrade to a current LTS; do the upgrade as a
   separate commit.
2. Fix B15 (`using UnityEditor` guards) and try a player build. Then do §9 steps 1–2
   (editor assemblies) so this class of break can't come back.
3. Delete the dead code in §6 in one commit so the rest of the work is against a smaller
   surface.

### Phase 1 — extract a pure model assembly

Create the `Lawnmower.Model` assembly described in §9 (`noEngineReferences: true`, no
Odin). Move into it:

- `GridVector`, `Xor`, `EventProperty`, `IListenableProperty`
- `IUndoable`, `IUndoSystem`, `UndoSystem`, `MultiUndo`, `Undoable`
- `Tile` and all subclasses (replace `Mathf.Clamp` with `Math.Max`/`Math.Min` — `Math.Clamp` needs .NET Standard 2.1; keep JSON cloning or
  give each tile a `Clone()` override)
- A new `Level` class (`Tile[,]` + start position + id) replacing `IReadOnlyLevelData`'s
  implementations at runtime

If something won't compile there, it has a Unity dependency that belongs in a
presenter/view. That's the enforcement mechanism that was missing.

Make the model's API `public` and keep implementation details `internal`; add
`[assembly: InternalsVisibleTo("Lawnmower.Model.Tests")]` only if tests need internals.

### Phase 2 — one `LevelSimulation`

```csharp
public sealed class LevelSimulation
{
    public LevelSimulation(Level level, IUndoSystem undo, SimulationMode mode = SimulationMode.Play);

    public Level Level { get; }
    public IListenableProperty<GridVector> MowerPosition { get; }
    public LevelStatus Status { get; }                // Playing / Completed / Failed
    public event UndoableAction<LevelStatus> StatusChanged;
    public event Action<MoveResult> Moved;            // includes path for spring jumps

    public MoveResult Move(GridVector direction);     // one call == one undo step
}
```

- `Move` does the traversal check, tile interaction, spring/ice/etc. resolution and
  status evaluation **in a fixed order**, and pushes **one** compound undoable. That fixes
  B1, B2, B4, B7, B8 and the subscription-order risk by construction.
- Tile-specific movement (springs now; ice, pushable blocks later) goes through a hook on
  `Tile`, e.g. `virtual GridVector? Redirect(GridVector from, GridVector direction)`, which
  the simulation loops on until the mower comes to rest. Ice = "keep going in the same
  direction"; spring = "jump to landing position". This is the extension point you'll need
  for the planned tiles, and it keeps the mower moving through intermediate tiles in a
  form views can animate (`MoveResult.Path`).
- Edit/author mode is a constructor argument, not a flag on five objects.
- Delete `MowerMovementManager`, `LevelTileInteractor`, `LevelStateChecker`,
  `LevelTraversalChecker`, `EditModeLevelTraversalChecker`, `HeadlessLevelManager`,
  `HeadlessMowerManager`, `MowerMover` — or reduce each to a few lines delegating to the
  simulation while migrating.

### Phase 3 — tests that play levels

With the simulation in place, these are cheap and catch most regressions:

- Unit tests per tile type (mowing, bumping, spring landing, undo of each).
- **Undo round-trip property test**: for random move sequences, `Move…` then `Undo` all
  restores the exact initial state.
- **Solution tests for every shipped level**: store a solution (a string like `"RRUULD"`)
  on each `LevelData` asset and an EditMode test that loads every level via
  `AssetDatabase`, replays its solution through `LevelSimulation`, and asserts
  `Completed`. Reverse-play authoring can record this solution for free.
- Optionally a small BFS/DFS solver over `LevelSimulation` to verify solvability and
  uniqueness of authored levels, and to estimate difficulty. Feasible because state is
  just mower position + grass heights.

### Phase 4 — thin presenters

- `GameManager` becomes a presenter: creates a `LevelSimulation`, forwards input to
  `Move`, maps `StatusChanged` to dialogs/persistence. Move persistence and dialogs into
  their own small classes.
- `LevelManager` builds tile views from `simulation.Level` and disposes them; `MowerManager`
  builds the mower view and binds it to `simulation.MowerPosition`/`Moved`.
- Input components call the presenter rather than going through a global SO event
  channel, or keep the channel but have exactly one subscriber (the presenter).
- Views animate from `MoveResult.Path`, which fixes B3 and gives you the spring arc / ice
  slide animations.
- The level editor window uses exactly the same `LevelSimulation` with
  `SimulationMode.Author`.

Keep ScriptableObjects for what they're good at: **configuration** (prefab tables, game
timing, level lists) and **cross-scene event channels**. Avoid storing per-level runtime
state in them.

### Phase 5 — content and features

Once the above is done, adding tiles is: one `Tile` subclass + tests + one
`BaseTileObject` view + a prefab entry. Fix B5/B6/B12 (progress, unlock-all, fail
feedback), finish the spring visuals, fill in the tutorial stages, then ice and pushable
blocks. Use reverse-play authoring plus the solver to build the level set.

### Suggested order of work

1. Phase 0 (open, build, delete dead code)
2. Phase 1 + 2 together, with the existing 8 levels (+ spring test) as the acceptance test (play each one
   in-game before and after)
3. Phase 3 solution tests as soon as `LevelSimulation` exists
4. Phase 4 presenters, one scene at a time
5. Phase 5 features

---

## 9. Assemblies

### 9.1 Current state

| Assembly | Contents | References |
|---|---|---|
| `Scripts` | **All** game code: model, rules, views, UI, menus, settings, in-scene editor, and the Odin level editor window (`Game/Levels/Editor`) | TextMeshPro, Cinemachine, LeanCommon, LeanTouch, Extensions, Utils, Plugins (+ auto-referenced Odin, Newtonsoft, DOTween DLLs) |
| `Plugins` | IUnified (runtime + `Editor/` folder), DOTween modules | — |
| `Utils`, `Extensions` | Personal utility libraries (`Loops`, `InterfaceHelper`, `Singleton`, mesh tools, coroutine extensions, …) | none declared |
| `BoidsTest` | Boids demo | Extensions, Burst, Mathematics |
| `Tests` (EditMode) | Undo / FlexGrid / BiDirectionList tests | Scripts, NUnit, Moq |
| `PlayMode` | `MonoUndoSystem` tests | Scripts, NUnit, Moq |
| *Assembly-CSharp-Editor* | `Assets/Editor/Editor/*` (enum generators, namespace auto-setter, …) — no asmdef | everything auto-referenced |

### 9.2 Problems

1. **One assembly for the whole game.** Nothing stops the model referencing views, Odin,
   or `UnityEditor`, so the MVP boundary was enforced only by discipline — and it slipped
   (31 of 53 files in `Game/Tiles`, 21 of 31 in `Game/Levels` import Unity/Odin/JSON,
   including model types).
2. **`Editor` folders don't work inside an asmdef.** Unity's special-casing of folders
   named `Editor` only applies to code *not* covered by an asmdef. So
   `Scripts/Game/Levels/Editor/*` and `Plugins/IUnified/Editor/*` compile into runtime
   assemblies; they survive only because every file is wrapped in `#if UNITY_EDITOR`.
   (This is probably also why the in-scene editor folder was named `Editorr`.)
   These runtime files import `UnityEditor` without a guard (B15):
   - `Scripts/Core/EventChannels/BaseBaseEventChannel.cs`
   - `Scripts/Core/DummyInitScriptableObjects.cs`
   - `Scripts/Core/ScriptableObjectReferencer.cs`
   - `Scripts/Core/ViewManager_Odin.cs`
   - `Scripts/Game/Core/GestureForwardingHandler.cs`
   - `Scripts/Game/Tiles/PrefabArranger.cs`
   - `Scripts/Game/Tiles/GrassTileGrassMaterialSetter.cs`
   - `Plugins/Utils/InterfaceHelper.cs`
   - `Plugins/Utils/ScriptableObjectReferencer.cs`
3. **Circular namespace dependencies** that will block a straightforward split:
   - `Game.Tiles` ↔ `Game.Levels` (`WoodTileObject` uses Levels; Levels uses Tiles)
   - `Game.Levels` ↔ `Game.Mowers` (`HeadlessLevelManager`/`LevelStateChecker`/
     `LevelTileInteractor` use `MowerMovementManager`; `MowerMovementManager` uses
     `ILevelTraversalChecker`)
   - `Game.Core` → `Levels`/`Mowers`, while everything → `Game.Core`. `Game.Core` is a
     grab-bag: `GridVector` (model) sits next to `GameManager` (scene glue) and
     `Positioner` (view).
   - Root `Core` (meant to be generic infrastructure) → `Game`
     (`TileObjectEventChannel`, `UndoableEventChannel`, `ViewManager_Odin`), and
     `UI.Buttons.Pulser` / `LevelSelect.SceneManager` → `Game`.
   - `Core` vs `Game.Core` vs `Plugins/Utils` all contain a `ScriptableObjectReferencer`.
4. **Namespaces are organised by feature, not by layer.** `Game.Tiles` holds both
   `GrassTile` (model) and `GrassTileObject` (view); `Game.Mowers` holds
   `MowerMovementManager` (rules), `MowerTransformAnimator` (view) and input handlers. An
   assembly split along MVP lines has to cut through every namespace.
5. **`GridVector` depends on `UnityEngine`** (`Vector2Int` magnitude, `ToXZ()`), so the most
   fundamental model type can't go in an engine-free assembly as-is.
6. **Tests are wired loosely.** `PlayMode` is `autoReferenced: true` (so every
   auto-referencing assembly can see test code), test assemblies reference `Scripts` by
   name, and there's no test assembly per production assembly.
7. **Third-party code isn't isolated.** Odin/DOTween/Newtonsoft are auto-referenced
   precompiled DLLs, so any assembly picks them up silently. Prefer
   `overrideReferences: true` + explicit `precompiledReferences` where you want to keep
   a layer free of them.

### 9.3 Target layout

Dependencies point downwards only. Names are suggestions.

```
Lawnmower.Model            noEngineReferences: true. GridVector, Xor, EventProperty, undo system,
                           Tile + subclasses, Level, LevelSimulation, solver. Only precompiled ref:
                           Newtonsoft.Json (or none, if cloning/serialisation lives elsewhere).
    ▲
Lawnmower.Model.Tests      Editor-only test asmdef. Pure NUnit: per-tile rules, undo round-trips,
                           solutions for every level (loads LevelData via an editor helper).
    ▲
Lawnmower.Infrastructure   Generic Unity plumbing with no game knowledge: event channels,
                           persistent data, pooling, ViewManager, Singleton. (Today's root `Core`
                           minus the Game references, merged with `Utils`/`Extensions` or
                           referencing them.)
    ▲
Lawnmower.Data             ScriptableObject assets: LevelData (wraps Model.Level), level list,
                           tile prefab table, game timing config. Unity + Odin allowed.
    ▲
Lawnmower.Presentation     Views and presenters: tile objects, mower animators, particles,
                           cameras, input, GameManager/LevelPresenter, UI, menus, tutorial.
    ▲
Lawnmower.Presentation.Tests  PlayMode tests for presenters/views (few; most coverage is in Model).

Lawnmower.Editor           includePlatforms: [Editor]. Level editor window, custom drawers,
                           Assets/Editor/Editor tools, Odin editor code. References any of the above.
Lawnmower.Editor.Tests     Editor-only tests for editor tooling (optional).
```

Notes:

- `Lawnmower.Model` with `noEngineReferences: true` is the single most valuable change: the
  compiler then enforces "headless", and the model tests run in milliseconds with no scene.
- Tests that need level *assets* still run headlessly: an editor-only test can
  `AssetDatabase.LoadAssetAtPath<LevelData>` and pass `levelData.ToModel()` into
  `LevelSimulation`. Only that adapter touches Unity.
- Third-party plugins: keep `LeanTouch`/`LeanCommon` as is; give IUnified its runtime part
  in `Plugins` and move `IUnified/Editor` into an editor-only asmdef (or an `.asmref`).
- Boids: leave as its own assembly; nothing references it.
- The `AutoSetScriptNamespacesToFolderName` tool will help once folders match assemblies:
  make folders `Assets/Scripts/Model/...`, `Assets/Scripts/Presentation/...`, etc.
  Then set each asmdef's `rootNamespace` (`Lawnmower.Model`, …).

### 9.4 Migration steps

Each step should leave the project compiling.

1. **Editor assembly first (low risk).** Add `Assets/Editor/Lawnmower.Editor.asmdef`
   (`includePlatforms: ["Editor"]`), move `Scripts/Game/Levels/Editor/*` and
   `Assets/Editor/Editor/*` into it, drop their `#if UNITY_EDITOR` wrappers. Add an
   editor-only asmdef for `Plugins/IUnified/Editor`.
2. **Fix B15.** Guard or remove the nine unguarded `using UnityEditor;` lines; move any
   real editor-only logic (`ViewManager_Odin`, `ScriptableObjectReferencer`,
   `DummyInitScriptableObjects`) into the editor assembly behind an interface if runtime
   code needs it. Do a player build to confirm.
3. **Make `GridVector` engine-free.** Compute magnitude with `Math.Sqrt`; move `ToXZ()` and
   `Vector2Int` conversions to an extension class in Presentation.
4. **Create `Lawnmower.Model`** and move the undo system, `Xor`, `EventProperty`,
   `GridVector`, and the `Tile` hierarchy into it. Expect the compiler to flag every
   Unity/Odin dependency — each one is a model/view leak to fix (e.g. `GrassTile`'s
   `Mathf`, Odin attributes on model types).
5. **Break the cycles** while building `LevelSimulation` (§8 Phase 2): the rules that
   currently tie `Levels` ↔ `Mowers` all move into the model, so the remaining
   Presentation code only depends downward.
6. **Split `Game.Core`**: model types → Model; `Positioner`, `ScreenToWorldPointConverter`,
   gesture handling → Presentation; `GameManager` → Presentation.
7. **Clean root `Core`** of `Game` references (generic `TileObjectEventChannel` →
   `GameObjectEventChannel`, etc.) and turn it into `Lawnmower.Infrastructure`. Dedupe
   the three `ScriptableObjectReferencer`s.
8. **Tests**: rename/split into `Lawnmower.Model.Tests` (EditMode) and
   `Lawnmower.Presentation.Tests` (PlayMode); set both `autoReferenced: false`.
9. Optionally move `LevelData`/config SOs into `Lawnmower.Data` once the rest has settled;
   until then they can live in Presentation.
