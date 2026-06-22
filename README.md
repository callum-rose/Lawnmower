# Lawnmower

A grid-based puzzle game built in **Unity**. You drive a lawnmower across a lawn and
have to mow every patch of grass to *exactly* the right height — no more, no less — in
a single continuous route.

> ⚠️ **This is a prototype.** It's a working vertical slice with the core mechanic, a level editor, and supporting
> systems in place.

## The game

You control a mower sitting on a grid of tiles. Each turn you move one tile in one of
four directions (up / down / left / right). The goal is to mow the whole lawn perfectly.

- **Grass** has a height of 0–3. Driving onto a grass tile cuts it down by one.
    - A grass tile is **perfect** when its height is exactly **1**.
    - A grass tile is **ruined** if it's cut to **0** (over-mowed).
- A **level is complete** when *every* tile is in its complete state (all grass at
  height 1, with non-grass tiles always counting as complete).
- A **level fails** the moment *any* grass tile is ruined.

So the puzzle is really about routing: find the path that passes over every grass tile
the correct number of times to bring it down to height 1, without ever stepping on a
patch one time too many.

### Tile types

| Tile   | Traversable | Behaviour                                                                           |
|--------|-------------|-------------------------------------------------------------------------------------|
| Grass  | Yes         | Cut down by 1 each time the mower drives onto it. Perfect at height 1, ruined at 0. |
| Wood   | Yes         | A plain walkable path tile; always "complete".                                      |
| Spring | Yes         | Launches the mower to a defined landing position elsewhere on the grid.             |
| Stone  | No          | Solid obstacle — the mower bumps off it.                                            |
| Water  | No          | Impassable.                                                                         |
| Empty  | No          | A hole / gap in the lawn.                                                           |

Moving into a non-traversable tile registers as a **bump** (the mower doesn't move).
Moving off the edge of the grid is simply ignored.

More tile types could be easily added to vary the complexity of the puzzle.

### Controls

- **Touch** (mobile): swipe / on-screen arrows to move the mower.
- **Keyboard** (editor/desktop): directional input for testing.

### Undo

Every move is fully **undoable**. The undo system records each action (move, bump,
grass cut) and can play it backwards, restoring grass heights and mower position. This
lets players experiment and back out of mistakes instead of restarting.

## Project structure

Unity project — main code lives under `Assets/Scripts`:

- `Assets/Scripts/Game/Mowers` — the mower: movement, input handling, models, animation.
- `Assets/Scripts/Game/Tiles` — tile types, their state, appearance, grass shaders/particles.
- `Assets/Scripts/Game/Levels` — level data, the grid (`FlexGrid`), traversal rules,
  win/lose checking, and the **level editor**.
- `Assets/Scripts/Game/UndoSystem` — the undo/redo framework.
- `Assets/Scripts/Game/Cameras`, `Game/UI`, `Game/Tutorial` — camera control, in-game
  UI, and a tutorial system.
- `Assets/Scripts/Core` — shared infrastructure: ScriptableObject **event channels**,
  singletons, persistent save data, pooling, a view manager.
- `Assets/Scripts/MainMenu`, `LevelSelect`, `UI`, `Settings` — front-end menus,
  level selection, and graphics/quality settings.
- `Assets/Scenes/Boids` — a standalone, Jobs-based boids (flocking birds) demo, used
  for background ambience; not part of the core puzzle loop.

### Notable tech / dependencies

- **Odin Inspector** (`Sirenix.OdinInspector`) for editor tooling and data authoring.
- A ScriptableObject-based **event channel** architecture for decoupled systems.
- Unity **Jobs / Burst** for the boids simulation.
- **Newtonsoft.Json** for tile/level serialisation.
- **Graphy** for on-screen performance stats.
- A bespoke in-Unity **level editor window** for authoring levels and tile layouts.

The project targets mobile (Android performance work is present in the history).