# Bunny Heaven

*A mowing puzzle*, built in **Unity 6** (`6000.6.5f1`, URP) for iPhone, portrait only.

You steer a bunny-shaped mower across a lawn and have to mow every patch of grass to
*exactly* the right height (no more, no less) in one continuous route.

## The game

The mower sits on a grid of tiles. Each move goes one tile up, down, left or right.

- **Grass** has a height of 0–3. Driving onto a grass tile cuts it down by one.
    - A grass tile is **perfect** when its height is exactly **1**.
    - A grass tile is **ruined** if it's cut to **0** (over-mowed).
- A level is **complete** when every grass tile is at height 1.
- A level **fails** ("SCALPED!") the moment any grass tile is ruined. The card that covers the board offers
  **Undo Last Move**, Restart or Levels.

### Tile types

| Tile   | Traversable | Behaviour                                                                           |
|--------|-------------|-------------------------------------------------------------------------------------|
| Grass  | Yes         | Cut down by 1 each time the mower drives onto it. Perfect at height 1, ruined at 0. |
| Wood   | Yes         | A plain walkable path tile; always "complete".                                      |
| Spring | Yes         | Launches the mower to a landing tile elsewhere on the grid.                         |
| Rabbit hole | Yes    | Comes in colour-coded pairs. Drop in one and pop out of the other, landing one tile further on in the direction you were going. If that tile is blocked, off the grid or another hole, you bounce back to where you started. |
| Stone  | No          | Solid obstacle: the mower bumps off it.                                             |
| Water  | No          | Impassable.                                                                         |
| Empty  | No          | A hole in the lawn.                                                                 |

Moving into a non-traversable tile is a **bump**: the mower doesn't move, and a bump isn't a move (it doesn't count
towards par and isn't an undo step). Bouncing back out of a blocked rabbit hole works the same way. Moving off the edge of the grid is ignored.

### Levels, par and progress

There are 11 levels of increasing difficulty (`Assets/ScriptableObjects/Game/Levels/Data`), described in
[`Assets/Docs/LEVELS.md`](Assets/Docs/LEVELS.md). Every level is checked by the solver, and its **par** is the solver's
shortest solution. Finishing at or under par is "LAWN PERFECT!". Levels unlock in order; holding the Main Menu title for
about 2 seconds unlocks all of them.

### Controls

- **Swipe** on the board (default), or **on-screen arrows** (Settings > Movement).
- **Keyboard** arrows / WASD in the editor.
- **Pinch** to zoom, **twist** to rotate and **two-finger drag** to pan the board (mouse wheel zooms in the editor).
  The rotation settles on the nearest quarter turn when you let go; the view resets for each level.
- Undo / redo / restart from the HUD.

## Building to an iPhone

Requirements: Unity `6000.6.5f1` with **iOS Build Support**, Xcode, the Unity CLI (`unity`), and an iPhone paired with
this Mac (USB or Wi-Fi) with Developer Mode on. Signing uses team `7R68U6FFLK` with automatic signing; the bundle id is
`com.callumrose.bunnyheaven`.

```bash
tools/build_and_install.sh            # Unity iOS build -> xcodebuild -> install -> launch
tools/build_and_install.sh --no-install   # build only (Builds/iOS and the signed .app in Builds/DerivedData)
tools/build_and_install.sh --skip-unity   # reuse the last Unity build; just xcodebuild + install
```

Run it from the root of the checkout or worktree you want to build. If a Unity Editor has that project open, the script
builds through it (via the Unity CLI); otherwise it runs Unity in batch mode. Device commands are retried because the
wireless connection is flaky. Logs go to `Builds/logs/`. Override the target phone with `DEVICE_ID=<udid>`
(`xcrun devicectl list devices` lists them).

If the install step can't reach the phone, install the last built app by hand:

```bash
xcrun devicectl device install app --device <udid> "Builds/DerivedData/Build/Products/Release-iphoneos/BunnyHeaven.app"
```

The first time a build signed by your developer certificate runs, iOS asks you to trust it: Settings > General >
VPN & Device Management.

The iOS build can also be started from the editor (`Lawnmower.Editor.Build.IOSBuild.BuildIOS`, output `Builds/iOS`).

## Tests

EditMode and PlayMode tests run from the Unity Test Runner (or `unity command run_tests` against a live editor).
PlayMode tests include full play-throughs in the real scenes: Main Menu → Level Select → Level 1 solved with the
solver's solution, Level Complete, Level 2 unlocking, and fail → Undo Last Move → finish.

## Project structure

The code is split into assemblies along a Model / Presenter / View line:

| Assembly | Folder | What it holds |
|---|---|---|
| `Lawnmower.Model` | `Assets/Scripts/Model` | Pure C#: levels, tiles, `LevelSimulation` (the rules), undo. No Unity engine code. |
| `Lawnmower.Solver` | `Assets/Scripts/Solver` | Breadth-first solver over `LevelSimulation`: solvability, par, difficulty. |
| `Lawnmower.Presenters` | `Assets/Scripts/Presenters` | Pure C# presenters and services: game session, HUD, menus, progression, settings, input recognisers. |
| `Lawnmower.Infrastructure` | `Assets/Scripts/Infrastructure` | Shared Unity infrastructure: event channels, singletons, save data, pooling. |
| `Lawnmower.Presentation` | `Assets/Scripts/Presentation` | Passive Unity views and scene composition roots (Main Menu, Level Select, Game, Settings). |
| `BunnyHeaven.UI` | `Assets/UI/BunnyHeaven` | The UI kit: theme, fonts, buttons, cards, toggles and other view prefabs. |
| `Lawnmower.Haptics` | `Assets/Plugins/Haptics` | iOS haptics (native plugin in `Assets/Plugins/iOS`), a no-op in the editor. |

Editor tooling lives under `Assets/Editor` (menu items under **Bunny Heaven/**: author levels, recompute par, rebuild
screens, render the icon and level previews, build iOS). Tests live under `Assets/Tests` and next to each assembly.

### Notable tech / dependencies

- Unity **Input System** (swipe and arrow input switchable at runtime).
- **Cinemachine**, **DOTween** and **Timeline** for the board camera and animation.
- **TextMeshPro** with Londrina Solid and Shantell Sans (SIL Open Font License).
- Sound effects from [Kenney](https://kenney.nl) (CC0), mixed through an `AudioMixer`.
