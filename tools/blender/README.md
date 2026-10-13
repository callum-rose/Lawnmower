# Blender tools

Scripts for models in the [voxel style](../../Assets/Docs/VoxelStyle.md). They run in Blender
(`blender --background --factory-startup --python <script> -- ...`) or with Blender's Python module
(`pip install bpy` into a Python 3.13 venv, then `python <script> -- ...`).

- **`build_bunny.py`** — the Regular Bunny in five takes (`variant=classic|toy|lop|sitting|cube`, recoloured with `colours=original|snow|midnight|forest|grape`); renders
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
