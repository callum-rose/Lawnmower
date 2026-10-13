# Voxel model build scripts

Models built to the house voxel style in Assembler's `Assets/docs/VoxelStyle.md`.

| Script | Output |
|---|---|
| `build_carp.py` | `Assets/Models/Creatures/carp.fbx` |
| `build_frog.py` | `Assets/Models/Creatures/frog.fbx` |

`voxel_common.py` is the shared part builder (ported from Assembler's `Tools/blender/build_human.py`);
`bake_shade.py` is copied from Assembler's `Tools/blender/` unchanged.

Preview renders (Cycles, four views):

```bash
blender --background --factory-startup --python build_carp.py -- out=/tmp/carp
```

Bake AO and export the FBX (run from this folder):

```bash
blender --background --factory-startup --python bake_shade.py -- build_carp.py ../../Assets/Models/Creatures/carp.fbx Body
```

The `bpy` wheel from PyPI (`pip install bpy`, Python 3.13) works in place of `blender`:
`python build_carp.py -- out=/tmp/carp`, `python bake_shade.py -- build_carp.py <fbx> Body`.

Each FBX is one object per part under `root`, palette colours as materials (named by hex), and a `Shade`
vertex colour: R is the baked AO (multiply it into the base colour), G is height over the body.
Both FBXs carry one looping animation (frames 0..N, the last frame equal to the first, 24 fps):

| Model | Animation | Frames |
|---|---|---|
| Carp | Gentle swim: the body sways and bobs, a lagged wave runs through stalk and tail, the pectorals paddle | 32 |
| Frog | In-place hop: lean, launch with the hind legs kicked back, jaw opens at the top, land nose-first, settle | 24 |

Parts are posed through a parent chain while building, then flattened (`voxel_common.flatten`) so every
part is a direct child of `root` with its full motion baked per frame: nested parts come back misplaced
through the FBX export (VoxelStyle.md R18). The frog's body height is solved per frame so the lowest point
stays on the ground except while airborne.

For an animated model `bake_shade.py` averages the AO over 8 poses spread across the loop with every part
present (a simplification of R24: it does not bake each part with only its parent and children, or leave the
frog's legs out of the body's bake per R25). Tone is still measured at rest.

## Bunny scripts

The Regular Bunny's redesign in the [voxel style](../../Assets/Docs/VoxelStyle.md), ending in the
in-game Egg Bunny. Run them the same way as the scripts above.

- **`build_bunny.py`** — the Regular Bunny in six takes (`variant=classic|toy|lop|sitting|cube|egg`, recoloured with `colours=original|snow|midnight|forest|grape`; the cube also takes `pattern=dutch|spots|stripes|harlequin`, `soften=1..4` and `rounded=1` (filleted edges, round-cornered colour plates) and `golden=1` (rounded, with golden-ratio proportions)); renders
  three-quarter, front, side and back previews to `out=<prefix>`. Previews are in
  [`docs/images/bunny-voxel`](../../docs/images/bunny-voxel). The face is the game's own
  `bunny_face.png`, not voxels: the mesh carries the old model's two UV channels — `UVMap` into
  `bunny_tex.png`'s colour strips and `FaceMap` projecting the face decal onto the front — so it works
  with the existing RegularBunny material and FaceAnimationComponent. `face=<png>` previews another face.
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
