# Generates thin, tapered grass blade tiles with candidate glowing flowers (Pixar's Soul style) and exports them as FBX.
# Run headless:  blender -b --factory-startup -P generate_thin_grass.py -- <output_dir> [density]
#   density  fraction of REFERENCE_BLADES_PER_SIDE^2 blades (default DEFAULT_DENSITY); blade width scales inversely to keep coverage
#
# UV conventions the Grass shaders rely on:
#   uv0.x  texture colour column, constant per blade
#   uv0.y  0 at the blade base -> 1 at the tip (drives colour gradient, wind falloff, alpha and glow)
#   uv1    blade base position in tile space, constant per blade (bend noise, AO, per-blade seed)
#   uv2.x  -1 on a blade's left edge -> 1 on its right edge, following the rounded tip (rim light); 0 on flowers
#   Flowers reuse uv0.x as a marker: 2 + index on the stem, 10 + index on the bulb, 20 + index on the halo quad
#   (the shader shows each flower per tile at random and expands the halo quad to face the camera, using its normals as corners)

import math
import random
import sys

import bpy
from mathutils import Vector

TILES = [
	# name, height, xy offset (matches the original grass_n meshes), seed, blade width scale (shorter grass is thinner,
	# and gets proportionally more blades so coverage stays the same)
	("grass_thin_1", 0.40, 0.0, 1, 0.5),
	("grass_thin_2", 0.50, -0.1, 2, 0.75),
	("grass_thin_3", 0.60, 0.0, 3, 1.0),
]

REFERENCE_BLADES_PER_SIDE = 14
DEFAULT_DENSITY = 0.25
EDGE_MARGIN = 0.0
SEGMENTS = 4
TIP_WIDTH_RATIO = 0.75
CAP_POINTS = 5
HEIGHT_RANGE = (0.55, 1.0)
BASE_WIDTH_RANGE = (0.03, 0.045)
LEAN_RANGE = (0.05, 0.22)
COLUMN_CENTRES = (0.125, 0.375, 0.625)
NORMAL_UP_BIAS = 2.5

FLOWER_CANDIDATES = 4
FLOWER_AREA = (0.15, 0.85)
# Same seed and absolute heights for every grass height, so a flower stays put when its tile is mowed
FLOWER_SEED = 100
FLOWER_HEIGHT_RANGE = (0.55, 0.68)
FLOWER_STEM_WIDTH = 0.018
FLOWER_STEM_SEGMENTS = 3
FLOWER_LEAN_RANGE = (0.02, 0.08)
FLOWER_RADIUS_RANGE = (0.04, 0.055)
FLOWER_BULB_SEGMENTS = 8
FLOWER_BULB_RINGS = 5
HALO_CORNER_SPREAD = 0.002


def build_tile(name, tile_height, offset, seed, blades_per_side, width_scale):
	rng = random.Random(seed)
	verts, faces, uv0s, uv1s, uv2s, normals = [], [], [], [], [], []
	cell = (1 - 2 * EDGE_MARGIN) / blades_per_side

	for ix in range(blades_per_side):
		for iy in range(blades_per_side):
			bx = EDGE_MARGIN + (ix + rng.random()) * cell
			by = EDGE_MARGIN + (iy + rng.random()) * cell
			height = tile_height * rng.uniform(*HEIGHT_RANGE)
			width = rng.uniform(*BASE_WIDTH_RANGE) * width_scale
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
				uv2s += [(-1, 0), (1, 0)]

			shoulder = base + Vector((0, 0, height * shoulder_t)) + lean * shoulder_t * shoulder_t
			tangent = (Vector((0, 0, height)) + lean * 2 * shoulder_t).normalized()
			for i in range(1, CAP_POINTS + 1):
				angle = math.pi * i / (CAP_POINTS + 1)
				verts.append(shoulder + side * cap_radius * math.cos(angle) + tangent * cap_radius * math.sin(angle))
				uv0s.append((u, shoulder_t + cap_radius * math.sin(angle) / height))
				# The cap's outline is still the blade's edge, so it stays at the rim value
				uv2s.append((-1, 0) if math.cos(angle) < 0 else (1, 0))

			count = (SEGMENTS + 1) * 2 + CAP_POINTS
			uv1s += [(bx, by)] * count
			normals += [normal] * count

			for s in range(SEGMENTS):
				a = start + s * 2
				faces.append((a, a + 1, a + 3, a + 2))
			left = start + SEGMENTS * 2
			faces.append((left, left + 1, *range(left + 2, left + 2 + CAP_POINTS)))

	flowers_start = len(verts)
	add_flowers(random.Random(FLOWER_SEED), offset, verts, faces, uv0s, uv1s, normals)
	uv2s += [(0, 0)] * (len(verts) - flowers_start)

	mesh = bpy.data.meshes.new(name)
	mesh.from_pydata([tuple(v) for v in verts], [], faces)
	mesh.update()

	uv0_layer = mesh.uv_layers.new(name="UVMap")
	uv1_layer = mesh.uv_layers.new(name="BladeUV")
	uv2_layer = mesh.uv_layers.new(name="BladeSide")
	for loop in mesh.loops:
		uv0_layer.data[loop.index].uv = uv0s[loop.vertex_index]
		uv1_layer.data[loop.index].uv = uv1s[loop.vertex_index]
		uv2_layer.data[loop.index].uv = uv2s[loop.vertex_index]

	mesh.normals_split_custom_set_from_vertices([tuple(n) for n in normals])

	obj = bpy.data.objects.new(name, mesh)
	bpy.context.scene.collection.objects.link(obj)
	return obj


def add_flowers(rng, offset, verts, faces, uv0s, uv1s, normals):
	for index in range(FLOWER_CANDIDATES):
		bx = rng.uniform(*FLOWER_AREA)
		by = rng.uniform(*FLOWER_AREA)
		base = Vector((bx + offset, by + offset, 0))
		height = rng.uniform(*FLOWER_HEIGHT_RANGE)
		yaw = rng.uniform(0, math.tau)
		side = Vector((math.cos(yaw), math.sin(yaw), 0))
		lean_yaw = rng.uniform(0, math.tau)
		lean = Vector((math.cos(lean_yaw), math.sin(lean_yaw), 0)) * height * rng.uniform(*FLOWER_LEAN_RANGE)
		stem_normal = (side.cross(Vector((0, 0, 1))).normalized() + Vector((0, 0, NORMAL_UP_BIAS))).normalized()

		start = len(verts)
		for s in range(FLOWER_STEM_SEGMENTS + 1):
			t = s / FLOWER_STEM_SEGMENTS
			centre = base + Vector((0, 0, height * t)) + lean * t * t
			half = side * FLOWER_STEM_WIDTH * 0.5
			verts += [centre - half, centre + half]
			uv0s += [(2 + index, t), (2 + index, t)]
			uv1s += [(bx, by)] * 2
			normals += [stem_normal] * 2
		for s in range(FLOWER_STEM_SEGMENTS):
			a = start + s * 2
			faces.append((a, a + 1, a + 3, a + 2))

		# Low-poly UV sphere bulb sitting on top of the stem, with radial normals for the glow falloff
		radius = rng.uniform(*FLOWER_RADIUS_RANGE)
		centre = base + Vector((0, 0, height + radius * 0.6)) + lean
		start = len(verts)
		ring_starts = []
		verts.append(centre + Vector((0, 0, radius)))
		normals.append(Vector((0, 0, 1)))
		for r in range(1, FLOWER_BULB_RINGS):
			polar = math.pi * r / FLOWER_BULB_RINGS
			ring_starts.append(len(verts))
			for k in range(FLOWER_BULB_SEGMENTS):
				azimuth = math.tau * k / FLOWER_BULB_SEGMENTS
				direction = Vector((math.sin(polar) * math.cos(azimuth), math.sin(polar) * math.sin(azimuth), math.cos(polar)))
				verts.append(centre + direction * radius)
				normals.append(direction)
		bottom = len(verts)
		verts.append(centre - Vector((0, 0, radius)))
		normals.append(Vector((0, 0, -1)))
		count = len(verts) - start
		uv0s += [(10 + index, 1.0)] * count
		uv1s += [(bx, by)] * count

		n = FLOWER_BULB_SEGMENTS
		for k in range(n):
			faces.append((start, ring_starts[0] + k, ring_starts[0] + (k + 1) % n))
		for r in range(len(ring_starts) - 1):
			a, b = ring_starts[r], ring_starts[r + 1]
			for k in range(n):
				faces.append((a + k, b + k, b + (k + 1) % n, a + (k + 1) % n))
		last = ring_starts[-1]
		for k in range(n):
			faces.append((last + k, bottom, last + (k + 1) % n))

		start = len(verts)
		for cx, cy in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
			verts.append(centre + Vector((cx, cy, 0)) * HALO_CORNER_SPREAD)
			normals.append(Vector((cx, cy, 0)).normalized())
		uv0s += [(20 + index, 1.0)] * 4
		uv1s += [(bx, by)] * 4
		faces.append((start, start + 1, start + 2, start + 3))


def main():
	argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
	output_dir = argv[0] if argv else "."
	density = float(argv[1]) if len(argv) > 1 else DEFAULT_DENSITY

	bpy.ops.wm.read_factory_settings(use_empty=True)

	for name, height, offset, seed, tile_width_scale in TILES:
		# Thinner blades get proportionally more blades; width is then derived from the rounded blade count so
		# blade count x width (coverage) matches REFERENCE_BLADES_PER_SIDE^2 blades at the reference width
		blades_per_side = max(1, round(REFERENCE_BLADES_PER_SIDE * math.sqrt(density / tile_width_scale)))
		width_scale = REFERENCE_BLADES_PER_SIDE ** 2 / blades_per_side ** 2
		print(f"{name}: {blades_per_side ** 2} blades, width x{width_scale:.2f}")
		obj = build_tile(name, height, offset, seed, blades_per_side, width_scale)
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
