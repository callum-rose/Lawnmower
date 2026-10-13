# Voxel Export

How a [`VoxelStyle.md`](VoxelStyle.md) model gets from Blender into Unity, shaded and animating.
Every step here was learned the hard way; follow it as written, and verify in Unity (R18).

| Doc | Owns |
|---|---|
| [`VoxelStyle.md`](VoxelStyle.md) | How a model looks and moves |
| **`VoxelExport.md`** (this file) | How a model is exported from Blender and imported into Unity |

---

## Files

Each model keeps three files side by side in `Assets/VoxelStyle/Models/`:

| File | What it is |
|---|---|
| `<Model>.blend` | **The source of truth.** Parts, joints and animation. Every edit is made here. |
| `<Model>.fbx` | Generated from the `.blend` by `bake_shade.py`. Never edited by hand. |
| `<Model>.fbx.meta` | Import settings, written by the scene builder (below), never by hand. |

Unity also imports a `.blend` as a model (it launches Blender in the background). Nothing should
reference that asset; scenes, prefabs and the scene builder use the FBX only.

## What the `.blend` must contain

`bake_shade.py` reads the file by convention, so keep to it:

- **One parentless empty, the model root**, named after the model (`Goose`), scaled **0.1** — parts
  are authored in voxels (R1) and the root brings them to metres. Every part and joint is beneath it.
- **Parts are meshes; joints are empties.** Anything outside the root — a ground plane, camera, lights
  for previewing — is ignored by the bake and the export, so keep it if it helps.
- **A root part** whose height the Tone gradient is normalised to; `Body` unless told otherwise.
- **The scene's frame range is exactly the exported animation.** A loop keys frames 1..N+1 with frame
  N+1 equal to frame 1, and the range ends on N+1 so the clip's length is the whole cycle — otherwise
  Unity loops from frame N straight to frame 1 and hitches. Cycle length comes from the **Cycle
  speed** rule in `VoxelStyle.md` (goose 21 → range 1–22, shark 37 → 1–38).
- **More clips go on the same timeline, after the loop.** FBX export makes one take for the whole
  range — Blender's per-NLA-strip export makes a take per *object*, not per action, so it can't carry
  named clips for a multi-joint model. Unity splits the take by frame (see Import). A second clip
  starts a gap after the loop's seam key and the frame range is extended to its last key. Key every
  channel the loop keys (as well as any new ones), so Unity crossfades like for like, and mark each
  clip's first frame with a timeline marker named after it (`Walk`, `Jump`). Remove the loop's Cycles
  modifiers once anything follows it — they would cycle the whole timeline; the loop keys every frame,
  so this changes nothing it exports. The jump poses count towards the AO/Tone average too (R24).
  Current layout: goose Walk 1–22, Jump 32–53; shark Walk 1–38, Jump 48–78.
- No animation keys → a static model.

The shark and goose `.blend`s were first generated from the worked examples' build scripts; those
scripts are history, not a source. Edit the `.blend`.

## Export — `Tools/blender/bake_shade.py`

```bash
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python Tools/blender/bake_shade.py -- Assets/VoxelStyle/Models/Goose.blend Assets/VoxelStyle/Models/Goose.fbx
```

Arguments: `<model.blend> <fbx_out> [root_part]`. Env: `AO_POSES` (default 8, R24 wants ≥ 6),
`AO_SAMPLES` (default 256; 16 for a quick test). Models can export in parallel. The `.blend` is never
modified — the bake happens in memory and only the FBX is written.

What it does, and why each step matters:

1. **Edge distances** per face corner into UV0/UV1, in voxels, from each part's own mesh — independent
   of pose.
2. **AO and Tone, averaged over the animation (R24).** It bakes Cycles AO (reach 2 voxels, R23) at
   `AO_POSES` poses spread evenly across the frame range, with only the model's parts visible, and
   averages them; Tone (height over the root part's height) is averaged over the same poses. A static
   model bakes once (R26). Averaging also means no rest pose has to be recorded in the file.
3. **Packs channels Unity can read.** Unity imports one vertex-colour set, so AO and Tone share it
   (`Shade`: R = AO, G = Tone).
4. **Exports the root and everything beneath it**, with the animation baked (`bake_anim`, no NLA, no
   all-actions, force start/end keys, no simplification) over the scene's frame range.
5. **Leaves `bake_space_transform` off when the model is animated.** Blender's "Apply Transform" is
   experimental and does not convert animated transforms: with it on, the goose imported lying on its
   side and the shark's parts came back scattered. This was the cause of R18's "nested parts come
   back rotated or misplaced" caution; joints nested to any depth export fine without it. The axis
   conversion then lives on the FBX root node — correct when the imported prefab is instantiated, as
   here, but code that loads the `Mesh` sub-asset directly would get it rotated (see
   `Tools/CLAUDE.md` › `build_primitives.py`). Static models keep it on.

### Checking an export

A static FBX looks identical to an animated one in the Project window. Confirm the animation is there
before going to Unity:

```bash
strings Assets/VoxelStyle/Models/Goose.fbx | grep -c AnimationStack
```

Non-zero means animation is present. (Search for `AnimationStack`, not `AnimStack` — the latter isn't
the node name and always reports 0.)

## Import — `VoxelShadeTestSceneBuilder`

`Assembler > Voxel Style > Build Shade Test Scene` (`Assets/VoxelStyle/Editor/VoxelShadeTestSceneBuilder.cs`)
owns every import setting. Change the builder, not the inspector — its next run overwrites inspector
edits. Its `ConfigureModel` must keep:

| Setting | Value | Why |
|---|---|---|
| `importAnimation` | on | Off silently drops every clip — this happened. |
| `preserveHierarchy` | on | The clip keys the FBX root. Unity normally merges a single root into the prefab root, so the clip overwrites each instance's placed position and rotation and every model snaps to the origin. Preserved, the root is a child and placement survives. |
| looping clips | `loopTime` on | A cycle repeats forever; the seam key makes the wrap seamless. |
| `generateSecondaryUV` | off | It would overwrite UV1, which carries edge distances. |
| `optimizeMeshVertices`, mesh compression | off | Keeps the per-corner channel data exact. |
| material remaps | `Palette_<hex>` | Each embedded material, named by hex, maps to a shared `Assembler/Voxel Shade` material. |
| model search | `.fbx` only | The `.blend` beside each FBX imports as a model too. |

How clips are split and played:

| Setting | Value | Why |
|---|---|---|
| `animationType` | Generic | Mecanim, so clips can crossfade. (Legacy plays one clip on start and can't blend.) |
| clip split | `Splits` table in the builder | The take is cut into `Walk` (frame 1 to the seam key, looping) and `Jump` (its marker to the end, once). Keep the table in step with the `.blend`'s markers whenever a clip moves or changes length. A model not in the table gets its whole take as a looping `Walk`. |
| controller | `Assets/VoxelStyle/Animation/<Model>.controller` | Built by the builder and rewritten in place (its GUID survives). `Walk` is the default state; the `Jump` trigger crossfades in over 0.1 s; `Jump` crossfades back over its last 0.2 s. |
| `JumpOnSpace` | on each instance | Fires `Jump` on space while walking; presses mid-jump are ignored. |

`VoxelShadeTestSceneBuilder.Animate(instance, modelPath)` wires the controller and `JumpOnSpace` onto
an instance, so an existing scene can be updated without a rebuild.

Running the menu item rebuilds the scene from scratch. To re-import models **without** rebuilding it
(e.g. when the scene has uncommitted edits), invoke `ConfigureModel` through the live editor:

```bash
unity command eval --code 'var m = System.Type.GetType("Assembler.VoxelStyle.Editor.VoxelShadeTestSceneBuilder, Assembler.VoxelStyle.Editor").GetMethod("ConfigureModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); var s = UnityEngine.Shader.Find("Assembler/Voxel Shade"); foreach (var p in new[]{"Assets/VoxelStyle/Models/Goose.fbx","Assets/VoxelStyle/Models/Shark.fbx"}) m.Invoke(null, new object[]{p, s}); return "ok";'
```

Then call `Animate` on each instance in the open scene (it is public) and save the scene.

## Verifying in Unity

R18: only the result in Unity counts. After an import:

1. **The clips are the right length.** `length × frameRate` of a loop equals its cycle length (shark 37,
   goose 21), and it is set to loop. A `Jump` is goose 21 and shark 30 frames, and does not loop.
2. **It plays and loops.** Enter Play mode; after several cycles the animation is still playing and a
   joint's (`J_*`) rotation changes between samples.
3. **It looks right.** Capture the game view at two moments
   (`unity command capture_game_view --save_path Temp/x.png` — the path must be inside the project;
   it lands in `Assets/Temp/`, so move it out and delete the folder and its `.meta`). Each model should
   be at its placed position and facing, upright, parts in place, and visibly in a different pose in
   the two frames.
4. **Jump blends in and out.** In Play mode, call `SetTrigger("Jump")` on each `Animator` through
   `unity command eval`, then sample `GetCurrentAnimatorStateInfo(0)`: Walk → Jump → Walk. A key event
   queued via `InputSystem.QueueStateEvent` does *not* reach `JumpOnSpace` from the CLI. The editor
   window isn't focused, and the Input System's default background behaviour resets the keyboard.
   Test the space bar by hand with the Game view focused.

The pipeline server drops for a few seconds while Play mode reloads the domain, and the first command
after can time out — poll `unity command editor_status` until `playMode` is `playing`, then sample.
