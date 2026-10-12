# Generates thin, tapered grass blade tiles (Pixar's Soul style) and exports them as FBX.
# Run headless:  blender -b --factory-startup -P generate_thin_grass.py -- <output_dir>
#
# UV conventions the Grass shaders rely on:
#   uv0.x  texture colour column, constant per blade
#   uv0.y  0 at the blade base -> 1 at the tip (drives colour gradient, wind falloff, alpha and glow)
#   uv1    blade base position in tile space, constant per blade (bend noise, AO, per-blade seed)

import math
import random
import sys

import bpy
from mathutils import Vector

TILES = [
	# name, height, xy offset (matches the original grass_n meshes), seed
	("grass_thin_1", 0.40, 0.0, 1),
	("grass_thin_2", 0.50, -0.1, 2),
	("grass_thin_3", 0.60, 0.0, 3),
]

BLADES_PER_SIDE = 14
EDGE_MARGIN = 0.0
SEGMENTS = 4
TIP_WIDTH_RATIO = 0.75
CAP_POINTS = 5
HEIGHT_RANGE = (0.55, 1.0)
BASE_WIDTH_RANGE = (0.03, 0.045)
LEAN_RANGE = (0.05, 0.22)
COLUMN_CENTRES = (0.125, 0.375, 0.625)
NORMAL_UP_BIAS = 2.5


def build_tile(name, tile_height, offset, seed):
	rng = random.Random(seed)
	verts, faces, uv0s, uv1s, normals = [], [], [], [], []
	cell = (1 - 2 * EDGE_MARGIN) / BLADES_PER_SIDE

	for ix in range(BLADES_PER_SIDE):
		for iy in range(BLADES_PER_SIDE):
			bx = EDGE_MARGIN + (ix + rng.random()) * cell
			by = EDGE_MARGIN + (iy + rng.random()) * cell
			height = tile_height * rng.uniform(*HEIGHT_RANGE)
			width = rng.uniform(*BASE_WIDTH_RANGE)
			yaw = rng.uniform(0, math.tau)
			side = Vector((math.cos(yaw), math.sin(yaw), 0))
			lean_yaw = yaw + math.pi / 2 + rng.uniform(-0.8, 0.8)
			lean = Vector((math.cos(lean_yaw), math.sin(lean_yaw), 0)) * height * rng.uniform(*LEAN_RANGE)
			u = rng.choice(COLUMN_CENTRES) + rng.uniform(-0.04, 0.04)
			base = Vector((bx + offset, by + offset, 0))

			face_normal = side.cross(Vector((0, 0, 1))).normalized()
			normal = (face_normal + Vector((0, 0, NORMAL_UP_BIAS))).normalized()

			# Body tapers slightly up to the shoulder, then a semicircular cap rounds off the tip
			cap_radius = width * 0.5 * TIP_WIDTH_RATIO
			shoulder_t = 1 - cap_radius / height

			start = len(verts)
			for s in range(SEGMENTS + 1):
				t = shoulder_t * s / SEGMENTS
				centre = base + Vector((0, 0, height * t)) + lean * t * t
				half = side * width * 0.5 * (1 + (TIP_WIDTH_RATIO - 1) * s / SEGMENTS)
				verts += [centre - half, centre + half]
				uv0s += [(u, t), (u, t)]

			shoulder = base + Vector((0, 0, height * shoulder_t)) + lean * shoulder_t * shoulder_t
			tangent = (Vector((0, 0, height)) + lean * 2 * shoulder_t).normalized()
			for i in range(1, CAP_POINTS + 1):
				angle = math.pi * i / (CAP_POINTS + 1)
				verts.append(shoulder + side * cap_radius * math.cos(angle) + tangent * cap_radius * math.sin(angle))
				uv0s.append((u, shoulder_t + cap_radius * math.sin(angle) / height))

			count = (SEGMENTS + 1) * 2 + CAP_POINTS
			uv1s += [(bx, by)] * count
			normals += [normal] * count

			for s in range(SEGMENTS):
				a = start + s * 2
				faces.append((a, a + 1, a + 3, a + 2))
			left = start + SEGMENTS * 2
			faces.append((left, left + 1, *range(left + 2, left + 2 + CAP_POINTS)))

	mesh = bpy.data.meshes.new(name)
	mesh.from_pydata([tuple(v) for v in verts], [], faces)
	mesh.update()

	uv0_layer = mesh.uv_layers.new(name="UVMap")
	uv1_layer = mesh.uv_layers.new(name="BladeUV")
	for loop in mesh.loops:
		uv0_layer.data[loop.index].uv = uv0s[loop.vertex_index]
		uv1_layer.data[loop.index].uv = uv1s[loop.vertex_index]

	mesh.normals_split_custom_set_from_vertices([tuple(n) for n in normals])

	obj = bpy.data.objects.new(name, mesh)
	bpy.context.scene.collection.objects.link(obj)
	return obj


def main():
	argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
	output_dir = argv[0] if argv else "."

	bpy.ops.wm.read_factory_settings(use_empty=True)

	for name, height, offset, seed in TILES:
		obj = build_tile(name, height, offset, seed)
		bpy.ops.object.select_all(action="DESELECT")
		obj.select_set(True)
		bpy.context.view_layer.objects.active = obj
		bpy.ops.export_scene.fbx(
			filepath=f"{output_dir}/{name}.fbx",
			use_selection=True,
			object_types={"MESH"},
			mesh_smooth_type="OFF",
			use_custom_props=False,
			add_leaf_bones=False,
			bake_anim=False,
			axis_forward="-Z",
			axis_up="Y",
			bake_space_transform=True,
		)
		print(f"Exported {name}: {len(obj.data.vertices)} verts, {len(obj.data.polygons)} faces")


main()
