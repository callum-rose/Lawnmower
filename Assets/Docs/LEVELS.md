# Bunny Heaven levels

The eleven levels in `LevelDataManager.asset`, in play order. Every one is verified by `Lawnmower.Solver`
(a BFS over `LevelSimulation`, the same rules the game runs). Par is the solver's shortest solution, and
that solution is recorded on the asset and replays to Completed in `LevelAssetSimulationTests` and
`LevelSolverAssetTests`.

Difficulty = par × (1 + dead-end share) × log10(10 + reachable states). The dead-end share is the share
of first moves after which the level can no longer be won. See `SolveResult.Difficulty`.

| # | Asset | Size | Mechanics | Par | States | Dead-end first moves | Difficulty | Idea it teaches |
|---|---|---|---|---|---|---|---|---|
| 1 | `Level01` (new) | 3×3 | grass | 7 | 13 | 0/2 | 9.5 | Mow every tile once. A ring round a flower bed, and either way round works. |
| 2 | `Level02` (new) | 3×4 | grass | 10 | 128 | 2/3 | 35.7 | Mowed grass can't be driven over again, so the first move matters. Two of the three openings strand a tile. |
| 3 | `Level03` (was `Level1`) | 4×4 | grass | 15 | 1,669 | 0/2 | 48.4 | Sweep a whole lawn in rows. A bigger board, solved by snaking back and forth. |
| 4 | `Level04` (new) | 4×5 | grass, stone | 17 | 556 | 2/4 | 70.2 | Stones block the mower, so plan a route around them. |
| 5 | `Level05` (was `Level4`) | 4×4 | grass 2–3 | 20 | 37,618 | 0/3 | 91.5 | Tall grass (height 3) needs two passes. You have to come back through the middle. |
| 6 | `Level06` (was `Level9`) | 5×5 | grass 2–3, wood | 23 | 134,812 | 0/2 | 118.0 | Wooden decking never gets mowed, so you can cross it as often as you like. Use it as a hub. |
| 7 | `Level07` (new) | 5×5 | grass, stone, spring | 21 | 2,417 | 1/4 | 88.9 | A spring jumps you over the stone, onto the one tile you'd otherwise leave stranded. |
| 8 | `Level08` (new) | 5×6 | grass 2–3, water, spring | 24 | 15,837 | 2/3 | 168.0 | Water splits the lawn and the spring only goes one way. Finish this bank before you jump. |
| 9 | `Level09` (new) | 5×6 | grass 2–3, water, wood, spring | 27 | 59,814 | 2/3 | 215.0 | A wooden bridge can be crossed both ways, again and again, but the spring can't. Plan your crossings. |
| 10 | `Level10` (new) | 6×6 | grass 2–3, water, spring ×2 | 31 | 41,921 | 2/3 | 238.8 | Two springs, one each way. Cross the river and come back, with tall grass on both banks. |
| 11 | `Level11` (new) | 5×5 | grass 2–3, water, rabbit holes | 22 | 8,828 | 2/3 | 144.7 | A pair of rabbit holes: in one, out of the other, still heading the same way. Which way you go in decides where you come out. |

## The curve

- **Levels 1–3:** grass only, up to 4×4. Par 7, 10, 15.
- **Levels 4–6:** stone (L4), tall grass (L5) and wood (L6). Par 17, 20, 23.
- **Levels 7–10:** springs (L7), water (L8), then both together with bridges, up to 6×6. Par 21, 24, 27, 31.
- **Level 11:** rabbit holes, on a 5×5 lawn. Par 22.
- **Par** only drops at levels that introduce a tile kind: L7 (23 → 21, springs) and L11 (31 → 22, rabbit holes).
  Difficulty dips at both (118 → 89, 239 → 145) and climbs between them.
- `LevelSolverAssetTests.LevelList_FollowsTheDifficultyCurve` pins these rules:
  - grass only and at most 4×4 for levels 1–3;
  - no springs or water in levels 4–6;
  - everything fits in 6×6;
  - par never drops unless the level introduces a tile kind.

## Rules recap (from `LevelSimulation`)

- **Moving:** each move steps one tile and mows the tile it lands on, taking 1 off its height.
- **Grass:** a level is complete when every grass tile is at height 1. A tile mowed to 0 fails the level.
  So height-2 grass is entered exactly once, height-3 grass exactly twice, and the start tile (height 1)
  never again.
- **Blocking tiles:** stone, water and empty tiles can't be entered.
- **Wood:** never mowed, so it can be crossed any number of times.
- **Spring:** stepping on one launches you to its offset and mows the tile you land on. It works one way only.
- **Rabbit hole:** holes come in pairs (`hole:N`). Entering one takes you out of the other, one tile further on in the
  direction you were going, and mows the tile you land on; the holes themselves are never mowed. If that tile is off the
  level, blocked or another hole, the move bounces: nothing changes and it isn't a move. A pair needs exactly two holes;
  otherwise its holes are plain tiles.

## Retired levels (`Data/Archive/`)

Retired levels were moved, not deleted, so their guids, and any progress keyed by them, survive.
`Bunny Heaven/Recompute Par` still scores them.

| Asset | Size | Par | Why retired |
|---|---|---|---|
| `Level Spring Test` | 3×3 | 6 | A test fixture, not a level. The spring golden in `LevelAssetSimulationTests` and the PlayMode spring play-through still use it. |
| `Level3` | 7×7 | 37 | Too big for the 6×6 portrait board, and a long grind for a grass-only level. |
| `Level5` | 7×3 | 18 | 7 tiles wide doesn't fit portrait, and level 5 already teaches tall grass. |
| `Level6` | 13×9 | unknown | Too big for the board and for the solver (over 1,000,000 states). |
| `Level7` | 9×6 | 36 | Too wide, and par 36 is past the end of the curve. |
| `Level8` | 14×9 | unknown | Too big for the board and for the solver (over 1,000,000 states). |

## Authoring

- **New levels live in code.** They're defined in `Assets/Editor/Levels/LevelAuthoring.cs`, with fixed
  guids. **Bunny Heaven/Author Levels** creates or updates those assets and writes the level list in
  order. Then run **Bunny Heaven/Recompute Par** to record par and a shortest solution.
- **Changing a layout:** edit its rows in `LevelAuthoring`, then run both menu items again. Changing a
  layout clears that level's recorded solution and par until Recompute Par runs. Never change a
  shipped guid, because guids key player progress.
- **How the candidates were found:** generate-and-test.
  1. A throwaway console harness compiled `Lawnmower.Model` and `Lawnmower.Solver` outside Unity.
  2. It generated random boards for each slot: sizes, holes, interior stones, tall grass, and rivers
     with springs or wooden bridges across them.
  3. It kept boards that solved inside the slot's par window.
  4. It kept only boards that use the slot's mechanic. The shortest solution has to jump a spring or
     cross a wood tile twice. For spring levels, turning the springs into wood also has to make the
     level unsolvable.
  5. It ranked the survivors by difficulty.

  The final picks were then re-verified in the editor by Recompute Par and the EditMode tests.
- **Preview:** the Main Menu preview of Level 1 is `Assets/Art/Previews/Level01_Preview.png`. It was
  rendered with **Bunny Heaven/Render Level Preview**.
