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
Both models are static rest poses, but split into parts at their real joints so they can be animated later.
