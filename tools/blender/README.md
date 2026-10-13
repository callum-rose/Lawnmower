# Blender tools

Scripts for models in the [voxel style](../../Assets/Docs/VoxelStyle.md). They run in Blender
(`blender --background --factory-startup --python <script> -- ...`) or with Blender's Python module
(`pip install bpy` into a Python 3.13 venv, then `python <script> -- ...`).

- **`build_bunny.py`** — the Regular Bunny in four takes (`variant=classic|toy|lop|sitting`); renders
  three-quarter, front, side and back previews to `out=<prefix>`. Previews are in
  [`docs/images/bunny-voxel`](../../docs/images/bunny-voxel).
- **`bake_shade.py`** — bakes AO and the shading channels into a built model and exports FBX for Unity:
  `python bake_shade.py -- build_bunny.py RegularBunny.fbx Body variant=classic`. Ported from Assembler;
  its Tone and edge-distance channels feed Assembler's Voxel Shade shader, which this project doesn't
  have yet — the AO channel is usable on its own as vertex colour R.
