# Voxel model build scripts

Models built to the house voxel style in Assembler's `Assets/docs/VoxelStyle.md`.

| Script | Output |
|---|---|
| `build_carp.py` | `Assets/Models/Creatures/carp.fbx` |
| `build_frog.py` | `Assets/Models/Creatures/frog.fbx` |
| `build_toad.py` | `Assets/Models/Creatures/toad.fbx` and `toad.blend` |

`voxel_common.py` is the shared part builder (ported from Assembler's `Tools/blender/build_human.py`);
`bake_shade.py` is copied from Assembler's `Tools/blender/` unchanged.

Preview renders (Cycles, four views). If Blender can't find `voxel_common`, run from this folder with
`--python-expr "import sys; sys.path.insert(0, '.'); exec(open('build_toad.py').read())"` in place of `--python`.
The toad also takes `frames=0,14,44` (stills from the video camera), `video=<frame prefix>` (every frame of the loop) and
`blend=<path>` (saves the built scene). Pass `blend=` through `bake_shade.py` to write the `.blend` alongside the FBX:
`... bake_shade.py -- build_toad.py ../../Assets/Models/Creatures/toad.fbx Body blend=../../Assets/Models/Creatures/toad.blend`.

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
| Toad | A heavy in-place hop (0..30), then it rears its head, gapes, and its tongue snaps out, wiggles and snaps back (30..64) | 64 |

Parts are posed through a parent chain while building, then flattened (`voxel_common.flatten`) so every
part is a direct child of `root` with its full motion baked per frame: nested parts come back misplaced
through the FBX export (VoxelStyle.md R18). The frog's body height is solved per frame so the lowest point
stays on the ground except while airborne.

For an animated model `bake_shade.py` averages the AO over 8 poses spread across the loop with every part
present (a simplification of R24: it does not bake each part with only its parent and children, or leave the
frog's legs out of the body's bake per R25). Tone is still measured at rest.
