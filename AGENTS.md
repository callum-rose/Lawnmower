# AGENTS.md

**Before doing any work in this project, you MUST read the Unity CLI skill (`unity-cli`).**

## Unity

When working in a git worktree, prefer running Unity in batchmode (e.g. `unity run`, `unity test`, `unity build`) over connecting to a running Editor, unless I say otherwise. A connected Editor is usually open on a different checkout, so live commands would act on the wrong project.

## Screenshots

Use the project's `capture` command (`Assets/Editor/Capture/GameCapture.cs`) instead of writing a new capture script. It renders a camera to a PNG/JPG at any size, includes Screen Space - Overlay UI, and works the same in batch mode and in a live Editor:

```bash
unity run "$PWD" --command capture -- --scene Assets/Scenes/Game.unity --output Captures/game.png
unity command capture --project-path "$PWD" --camera GameCamera --width 1080 --height 1920
```

Captures default to `Captures/` (gitignored). Don't write to `Temp/`: Unity deletes it when the Editor quits, so batch-mode captures vanish. Pass the project as an absolute path: with `.`, the CLI can mistake another worktree's Editor launched with `-projectPath .` for this project's and refuse to start.

It renders in edit mode (or captures the running game if a live Editor is already in Play Mode). Anything set up at runtime, like a loaded level or the hero bunny's render texture, isn't there in edit mode, so put it in place with a `--setup` method.

Options: `--scene`, `--camera`, `--width`, `--height`, `--ui`, `--warmup`, `--output`, and `--setup Namespace.Type.Method` (with `--setup_arg`) to put the scene in the state you want first. For anything more, add a static setup method rather than another capture script. The parameter descriptions are in `GameCapture.cs`.

## Blender

Prefer running Blender in headless mode (`blender --background --python <script>`) over driving the Blender editor through the Blender MCP, unless I say otherwise.
