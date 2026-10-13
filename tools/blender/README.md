# Blender tools

Scripts for models in the [voxel style](../../Assets/Docs/VoxelStyle.md). They run in Blender
(`blender --background --factory-startup --python <script> -- ...`) or with Blender's Python module
(`pip install bpy` into a Python 3.13 venv, then `python <script> -- ...`).

- **`build_bunny.py`** — the Regular Bunny in six takes (`variant=classic|toy|lop|sitting|cube|egg`, recoloured with `colours=original|snow|midnight|forest|grape`; the cube also takes `pattern=dutch|spots|stripes|harlequin`, `soften=1..4` and `rounded=1` (filleted edges, round-cornered colour plates) and `golden=1` (rounded, with golden-ratio proportions)); renders
  three-quarter, front, side and back previews to `out=<prefix>`. Previews are in
  [`docs/images/bunny-voxel`](../../docs/images/bunny-voxel). The face is the game's own
  `bunny_face.png`, not voxels: the mesh carries the old model's two UV channels — `UVMap` into
  `bunny_tex.png`'s colour strips and `FaceMap` projecting the face decal onto the front — so it works
  with the existing RegularBunny material and FaceAnimationComponent. `face=<png>` previews another face.
- **`bake_shade.py`** — bakes AO and the shading channels into a built model and exports FBX for Unity:
  `python bake_shade.py -- build_bunny.py RegularBunny.fbx Body variant=classic`. It appends its own
  UV sets after the bunny's, so for the bunny's decal UVs export without it instead. Ported from Assembler;
  its Tone and edge-distance channels feed Assembler's Voxel Shade shader, which this project doesn't
  have yet — the AO channel is usable on its own as vertex colour R.
- **`original_shapes.py`** — shape variations on the in-game, smooth `regular_bunny.blend`
  (`chubby|tall|lop|swept`, or `base` for the model as it is), rendered from four sides. It changes
  only object transforms in memory; the .blend is not saved. Previews are in
  [`docs/images/bunny-original`](../../docs/images/bunny-original).
- **`build_egg_bunny_game.py`** — the in-game **Egg Bunny** (`build_bunny.py variant=egg`, re-framed for Unity):
  `python build_egg_bunny_game.py -- Assets/Models/Mower/Blender/regular_bunny.blend Assets/Models/Mower Assets/Models/Mower/Blender/egg_bunny.blend Assets/Textures/Game/Mowers/egg_bunny_tex.png`.
  It writes `egg_bunny.fbx`, `egg_bunny.blend` (with the Regular Bunny's actions, moved onto the egg's parts)
  and `egg_bunny_tex.png`. The model keeps the Regular Bunny's part names, frame, rest rotations and foot
  pivots, so `Assets/Animations/Game/Mower/EggBunny` holds the Regular Bunny's clips unchanged; the prefab
  names the model instance `regular_bunny` because the clips address its parts by that path. Re-export with
  the script, never by hand: the FBX settings (forward Y, up Z, meshes only) are what Unity's import expects.
