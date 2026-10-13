# Generates voxel-style pond props (Assets/Docs/VoxelStyle.md) and exports each model as FBX.
# Run headless:  blender -b --factory-startup -P generate_pond_props.py -- <output_dir> [preview_dir]
#   preview_dir  optional: also renders a thumbnail-camera preview (R7c) of each model there
#
# Parts are authored in voxels under a root empty scaled 0.1 (R1). The models are static, so AO is baked once with
# every part present (R26) into a face-corner colour attribute named AO (R23); it is the only vertex colour set exported.
#
# Design note (R3a, no reference):
#   Lily pad: a flat round leaf with a notch cut to the centre. Keep the octagon outline (round, faces the camera and
#     defines the subject, R7b) and the notch; drop the veins and upturned rim (R7c). 12 across (R3 floor), 1 thick.
#   Water lily: pointed petals in two rings around a yellow centre. Four flat outer petals (15 deg) and four upright
#     inner petals (40 deg) alternate, each pointed by two 1 x 1 wedges; the centre is a 2 x 2 x 2 block.
#   Lily bud: a closed bud, green at the base and pink above, a teardrop of 45 deg slopes, leaning 10 deg
#     sideways to the thumbnail camera so the lean reads.
#   Mossy rock: big and sturdy. A 1.6 x 1.4 x 1.2 m boulder with straight sides, corners cut 3 x 3 and one 3 x 3
#     bevel round the top, moss over the top and its bevel (the upper colour, R7b); a darker 1 m stone leans 10 deg
#     into it, half buried, and a small stone sits at its foot. No cracks or speckles (R7c).
#   Stepping stones: three chunky stones, 3 thick with cut corners and a 1 x 1 bevel, in two shades, turned in 5 deg
#     steps.
#   Floating log: an 8-across octagon (round, defines the subject, R7c) 2 m long, Walnut bark, end grain in rings,
#     a moss patch on top and one cut branch stub leaning 30 deg. Origin on its axis, the waterline (R2).
#   Stump: a 10-across octagon with ringed end grain on top and four roots angled down off its diagonal faces.

import math
import sys

import bmesh
import bpy
from mathutils import Euler, Vector

VOXEL = 0.1
AO_DISTANCE = 2 * VOXEL
AO_SAMPLES = 128

PALETTE = {
	"Grass": "5a9440",  # P21
	"Leaf": "8fc23a",  # P19
	"Pink": "e8899a",  # P46
	"Blush": "f6c4cc",  # P53
	"Yellow": "f6d021",  # P42
	"Limestone": "b8b0a4",  # P28
	"Stone": "8a837a",  # P29
	"DarkStone": "5c5650",  # P30
	"PaleWood": "c89a60",  # P24
	"Oak": "9a6a3a",  # P25
	"Walnut": "6b4528",  # P26
}
WATER = "2f7fa8"  # P38, preview only

PAD_HALF = 6
PAD_CORNER = 3
PAD_NOTCH_HALF = 1
PAD_NOTCH_END = -2
PAD_THICKNESS = 1

# Petals: (count, first yaw, width, length, pitch, base radius along the yaw direction, colour)
OUTER_PETALS = (4, 45, 3, 5, 15, math.sqrt(0.5), "Pink")
INNER_PETALS = (4, 0, 2, 4, 40, 0.5, "Blush")
PETAL_BASE_Z = 0.5
CENTRE_HALF = 1
CENTRE_Z = (0.5, 2.5)

# Teardrop of 45 deg slopes (R7b): a 2 x 2 neck flaring to 4 x 4, tapering to a point. Rings are (z, half width).
BUD_RINGS = [(0, 1), (1, 2), (6, 2), (8, 0)]
BUD_SEPAL_HEIGHT = 4
BUD_LEAN = 10
BUD_YAW = 45
BUD_BASE = (1, 1, 0.5)

# Stones: (name, loft rings (see ring_outline), location, rotation, colour). The first is the root.
ROCKS = [
	("Boulder", [(0, 8, 7, 3), (9, 8, 7, 3), (12, 5, 4, 2)], (0, 0, 0), (0, 0, 0), "Stone"),
	("LeaningStone", [(0, 5, 5, 2), (7, 5, 5, 2), (9, 3, 3, 1)], (7, -6, 0), (-10, 0, 20), "DarkStone"),
	("Pebble", [(0, 3, 3, 1), (3, 3, 3, 1), (4, 2, 2, 1)], (-8, -7, 0), (0, 0, -15), "Stone"),
]
ROCK_MOSS_ABOVE = 9
STEPPING_STONES = [
	("StoneA", [(0, 5, 4, 1), (2, 5, 4, 1), (3, 4, 3, 1)], (0, 0, 0), (0, 0, 0), "Limestone"),
	("StoneB", [(0, 4, 3.5, 1), (2, 4, 3.5, 1), (3, 3, 2.5, 1)], (-10, 6, 0), (0, 0, 15), "Stone"),
	("StoneC", [(0, 3.5, 3, 1), (2, 3.5, 3, 1), (3, 2.5, 2, 1)], (9, 7, 0), (0, 0, -20), "Limestone"),
]

LOG_HALF = 4
LOG_CORNER = 2
LOG_HALF_LENGTH = 10
LOG_MOSS_X = (-6, -2)
BRANCH_HALF = 1.5
BRANCH_LENGTH = 5
BRANCH_BASE = (4, 0, LOG_HALF - 1)
BRANCH_LEAN = 30

STUMP_HALF = 5
STUMP_CORNER = 3
STUMP_HEIGHT = 7
# Roots: one off each diagonal face, (half width, length, height, pitch down, base radius, base z)
ROOT = (1.5, 5, 2, 30, 3, 2)


def srgb_to_linear(c):
	return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_to_linear(hex_colour):
	return tuple(srgb_to_linear(int(hex_colour[i:i + 2], 16) / 255) for i in (0, 2, 4)) + (1.0,)


def palette_material(hex_colour):
	name = f"Palette_{hex_colour}"
	material = bpy.data.materials.get(name)
	if material is None:
		material = bpy.data.materials.new(name)
		material.diffuse_color = hex_to_linear(hex_colour)
		material.use_nodes = True
		material.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = hex_to_linear(hex_colour)
		material.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
	return material


def prism(polygon, z0, z1):
	"""A polygon in XY (counter-clockwise from +Z) extruded from z0 to z1."""
	n = len(polygon)
	verts = [(x, y, z0) for x, y in polygon] + [(x, y, z1) for x, y in polygon]
	faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
	faces += [(i, (i + 1) % n, n + (i + 1) % n, n + i) for i in range(n)]
	return verts, faces


def octagon(half, corner):
	"""A square of the given half width with its four corners bevelled corner x corner (R7b), counter-clockwise."""
	h, k = half, corner
	return [(-(h - k), -h), (h - k, -h), (h, -(h - k)), (h, h - k), (h - k, h), (-(h - k), h), (-h, h - k), (-h, -(h - k))]


def along_x(verts):
	"""Turns a prism extruded along Z to lie along X (a cyclic swap of axes, so face winding is kept)."""
	return [(z, x, y) for x, y, z in verts]


def ring_outline(ring):
	"""A loft ring's cross-section: (z, half width) square, (z, half x, half y) rectangle, or (z, half x, half y,
	corner) rectangle with its corners bevelled corner x corner (R7b). Always 8 points, counter-clockwise."""
	_, hx, hy, k = (ring + (0,)) if len(ring) == 3 else (ring[0], ring[1], ring[1], 0) if len(ring) == 2 else ring
	return [(-(hx - k), -hy), (hx - k, -hy), (hx, -(hy - k)), (hx, hy - k), (hx - k, hy), (-(hx - k), hy), (-hx, hy - k), (-hx, -(hy - k))]


def loft(rings):
	"""A solid through cross-sections (see ring_outline), bottom to top, capped at the bottom and capped or pointed
	(half width 0) at the top. Coincident points of square rings are merged when the part is built."""
	n = 8
	verts, faces = [], []
	for ring in rings[:-1]:
		verts += [(x, y, ring[0]) for x, y in ring_outline(ring)]
	faces.append(tuple(reversed(range(n))))
	for r in range(len(rings) - 2):
		a, b = r * n, (r + 1) * n
		faces += [(a + i, a + (i + 1) % n, b + (i + 1) % n, b + i) for i in range(n)]
	a = (len(rings) - 2) * n
	top = rings[-1]
	if top[1] == 0:
		verts.append((0, 0, top[0]))
		faces += [(a + i, a + (i + 1) % n, a + n) for i in range(n)]
	else:
		verts += [(x, y, top[0]) for x, y in ring_outline(top)]
		faces += [(a + i, a + (i + 1) % n, a + n + (i + 1) % n, a + n + i) for i in range(n)]
		faces.append(tuple(range(a + n, a + 2 * n)))
	return verts, faces


def build_part(name, parent, verts, faces, grid_offset, colour_of, location=(0, 0, 0), rotation=(0, 0, 0)):
	"""grid_offset: where this part's voxel boundaries fall on each local axis (0 or 0.5). Faces are cut to one
	segment per voxel (R23) and coloured per voxel by colour_of(local face centre, local face normal)."""
	mesh = bpy.data.meshes.new(name)
	bm = bmesh.new()
	bm_verts = [bm.verts.new(v) for v in verts]
	for face in faces:
		bm.faces.new([bm_verts[i] for i in face])
	bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
	bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=1e-5)

	for axis in range(3):
		values = [v.co[axis] for v in bm.verts]
		lo, hi = min(values), max(values)
		plane = math.floor(lo - grid_offset[axis]) + grid_offset[axis] + 1
		while plane < hi - 1e-4:
			normal = Vector((0, 0, 0))
			normal[axis] = 1
			co = Vector((0, 0, 0))
			co[axis] = plane
			geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
			bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=normal, dist=1e-5)
			plane += 1

	materials = {}
	for face in bm.faces:
		hex_colour = PALETTE[colour_of(face.calc_center_median(), face.normal)]
		if hex_colour not in materials:
			materials[hex_colour] = len(materials)
			mesh.materials.append(palette_material(hex_colour))
		face.material_index = materials[hex_colour]

	bm.normal_update()
	bm.to_mesh(mesh)
	bm.free()

	obj = bpy.data.objects.new(name, mesh)
	bpy.context.scene.collection.objects.link(obj)
	obj.parent = parent
	obj.location = location
	obj.rotation_euler = Euler(tuple(math.radians(a) for a in rotation), "XYZ")
	return obj


def build_pad(root):
	h, n = PAD_HALF, PAD_NOTCH_HALF
	# Octagon with the notch cut in from the front edge (-Y)
	outline = [(n, -h)] + octagon(PAD_HALF, PAD_CORNER)[1:] + [octagon(PAD_HALF, PAD_CORNER)[0]]
	outline += [(-n, -h), (-n, PAD_NOTCH_END), (n, PAD_NOTCH_END)]
	verts, faces = prism(outline, 0, PAD_THICKNESS)
	return build_part("Pad", root, verts, faces, (0, 0, 0), lambda c, n: "Grass")


def build_petal(root, name, yaw, width, length, pitch, radius, colour):
	half = width / 2
	# Pointed tip: both tip corners bevelled by 1 x 1 wedges (R7b), merged into the petal
	outline = [(-half, 0), (half, 0), (half, length - 1), (half - 1, length), (-half + 1, length), (-half, length - 1)]
	if width == 2:
		outline = [(-1, 0), (1, 0), (1, length - 1), (0, length), (-1, length - 1)]
	verts, faces = prism(outline, 0, 1)
	direction = Vector((-math.sin(math.radians(yaw)), math.cos(math.radians(yaw))))
	base = direction * radius
	location = (round(base.x * 2) / 2, round(base.y * 2) / 2, PETAL_BASE_Z)
	return build_part(name, root, verts, faces, (half % 1, 0, 0), lambda c, n: colour, location, (pitch, 0, yaw))


def build_flower(root):
	parts = []
	for ring, (count, first_yaw, width, length, pitch, radius, colour) in (("Outer", OUTER_PETALS), ("Inner", INNER_PETALS)):
		for i in range(count):
			yaw = first_yaw + i * 360 / count
			parts.append(build_petal(root, f"Petal{ring}{i + 1}", yaw, width, length, pitch, radius, colour))
	c = CENTRE_HALF
	verts, faces = prism([(-c, -c), (c, -c), (c, c), (-c, c)], *CENTRE_Z)
	parts.append(build_part("Centre", root, verts, faces, (0, 0, CENTRE_Z[0] % 1), lambda c, n: "Yellow"))
	return parts


def build_bud(root):
	verts, faces = loft(BUD_RINGS)
	return build_part(
		"Bud", root, verts, faces, (0, 0, 0),
		lambda c, n: "Leaf" if c.z < BUD_SEPAL_HEIGHT else "Pink", BUD_BASE, (BUD_LEAN, 0, BUD_YAW))


def build_stones(root, stones, colour_of=None):
	parts = []
	for name, rings, location, rotation, colour in stones:
		grid = (rings[0][1] % 1, rings[0][2] % 1, 0)
		verts, faces = loft(rings)
		colour_fn = (lambda c, n, colour=colour: colour) if colour_of is None or parts else colour_of(colour)
		parts.append(build_part(name, root if not parts else parts[0], verts, faces, grid, colour_fn, location, rotation))
	return parts


def build_rock(root):
	return build_stones(root, ROCKS, lambda colour: lambda c, n: "Grass" if c.z > ROCK_MOSS_ABOVE else colour)


def end_grain(ring, half):
	"""Tree rings on a cut face: bark at the rim, alternating Oak and Pale wood inwards from an Oak heart."""
	if ring >= half - 1:
		return "Walnut"
	return "Oak" if int(ring) % 2 == 0 else "PaleWood"


def build_log(root):
	verts, faces = prism(octagon(LOG_HALF, LOG_CORNER), -LOG_HALF_LENGTH, LOG_HALF_LENGTH)

	def log_colour(c, n):
		if abs(n.x) > 0.99:
			return end_grain(max(abs(c.y), abs(c.z)), LOG_HALF)
		if n.z > 0.99 and LOG_MOSS_X[0] < c.x < LOG_MOSS_X[1]:
			return "Grass"
		return "Walnut"

	log = build_part("Log", root, along_x(verts), faces, (0, 0, 0), log_colour)
	b = BRANCH_HALF
	verts, faces = prism([(-b, -b), (b, -b), (b, b), (-b, b)], 0, BRANCH_LENGTH)
	branch = build_part(
		"Branch", log, verts, faces, (b % 1, b % 1, 0),
		lambda c, n: "PaleWood" if n.z > 0.99 and c.z > BRANCH_LENGTH - 0.01 else "Walnut",
		BRANCH_BASE, (0, BRANCH_LEAN, 0))
	return [log, branch]


def build_stump(root):
	verts, faces = prism(octagon(STUMP_HALF, STUMP_CORNER), 0, STUMP_HEIGHT)
	stump = build_part(
		"Stump", root, verts, faces, (0, 0, 0),
		lambda c, n: end_grain(max(abs(c.x), abs(c.y)), STUMP_HALF) if n.z > 0.99 and c.z > STUMP_HEIGHT - 0.01 else "Walnut")
	parts = [stump]
	half, length, height, pitch, radius, base_z = ROOT
	verts, faces = prism([(-half, 0), (half, 0), (half, length), (-half, length)], 0, height)
	for i in range(4):
		yaw = 45 + 90 * i
		direction = Vector((-math.sin(math.radians(yaw)), math.cos(math.radians(yaw))))
		base = direction * radius
		location = (round(base.x * 2) / 2, round(base.y * 2) / 2, base_z)
		parts.append(build_part(
			f"Root{i + 1}", stump, verts, faces, (half % 1, 0, 0), lambda c, n: "Walnut", location, (-pitch, 0, yaw)))
	return parts


MODELS = {
	"LilyPad": lambda root: [build_pad(root)],
	"LilyPadFlower": lambda root: [build_pad(root)] + build_flower(root),
	"LilyPadBud": lambda root: [build_pad(root), build_bud(root)],
	"MossyRock": build_rock,
	"SteppingStones": lambda root: build_stones(root, STEPPING_STONES),
	"FloatingLog": build_log,
	"Stump": build_stump,
}


def bake_ao(parts):
	scene = bpy.context.scene
	scene.render.engine = "CYCLES"
	scene.cycles.device = "CPU"
	scene.cycles.samples = AO_SAMPLES
	if scene.world is None:
		scene.world = bpy.data.worlds.new("World")
	scene.world.light_settings.distance = AO_DISTANCE

	bpy.ops.object.select_all(action="DESELECT")
	for part in parts:
		attribute = part.data.color_attributes.new("AO", "BYTE_COLOR", "CORNER")
		part.data.color_attributes.active_color = attribute
		part.data.color_attributes.render_color_index = part.data.color_attributes.active_color_index
		part.select_set(True)
	bpy.context.view_layer.objects.active = parts[0]
	bpy.ops.object.bake(type="AO", target="VERTEX_COLORS")


def export(root, path):
	bpy.ops.object.select_all(action="DESELECT")
	root.select_set(True)
	for child in root.children_recursive:
		child.select_set(True)
	bpy.context.view_layer.objects.active = root
	bpy.ops.export_scene.fbx(
		filepath=path,
		use_selection=True,
		object_types={"EMPTY", "MESH"},
		mesh_smooth_type="OFF",
		colors_type="SRGB",
		prioritize_active_color=True,
		use_custom_props=False,
		add_leaf_bones=False,
		bake_anim=False,
		axis_forward="-Z",
		axis_up="Y",
		bake_space_transform=True,
	)


def render_preview(parts, path):
	"""Palette colour x baked AO (R23) under a soft ambient and one key light, from the thumbnail camera (R7c)."""
	for part in parts:
		for slot in part.material_slots:
			material = slot.material.copy()
			nodes, links = material.node_tree.nodes, material.node_tree.links
			nodes.clear()
			ao = nodes.new("ShaderNodeVertexColor")
			ao.layer_name = "AO"
			multiply = nodes.new("ShaderNodeMix")
			multiply.data_type = "RGBA"
			multiply.blend_type = "MULTIPLY"
			multiply.inputs["Factor"].default_value = 1
			multiply.inputs["A"].default_value = slot.material.diffuse_color
			links.new(ao.outputs["Color"], multiply.inputs["B"])
			diffuse = nodes.new("ShaderNodeBsdfDiffuse")
			links.new(multiply.outputs["Result"], diffuse.inputs["Color"])
			output = nodes.new("ShaderNodeOutputMaterial")
			links.new(diffuse.outputs["BSDF"], output.inputs["Surface"])
			slot.material = material

	scene = bpy.context.scene
	corners = [part.matrix_world @ Vector(corner) for part in parts for corner in part.bound_box]
	low = Vector(tuple(min(c[i] for c in corners) for i in range(3)))
	high = Vector(tuple(max(c[i] for c in corners) for i in range(3)))
	centre = (low + high) / 2

	bpy.ops.mesh.primitive_plane_add(size=20, location=(0, 0, 0.03))
	water = bpy.context.object
	water_material = bpy.data.materials.new("PreviewWater")
	water_material.use_nodes = True
	nodes = water_material.node_tree.nodes
	nodes.clear()
	emission = nodes.new("ShaderNodeEmission")
	emission.inputs["Color"].default_value = hex_to_linear(WATER)
	output = nodes.new("ShaderNodeOutputMaterial")
	water_material.node_tree.links.new(emission.outputs["Emission"], output.inputs["Surface"])
	water.data.materials.append(water_material)

	camera_data = bpy.data.cameras.new("Thumbnail")
	camera_data.type = "ORTHO"
	camera_data.ortho_scale = max(1.6, (high - low).length * 1.1)
	camera = bpy.data.objects.new("Thumbnail", camera_data)
	scene.collection.objects.link(camera)
	elevation, azimuth = math.radians(30), math.radians(-135)
	direction = Vector((math.cos(elevation) * math.cos(azimuth), math.cos(elevation) * math.sin(azimuth), math.sin(elevation)))
	camera.location = centre + direction * 5
	camera.rotation_euler = (-direction).to_track_quat("-Z", "Y").to_euler()
	scene.camera = camera

	scene.world.use_nodes = True
	scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (1, 1, 1, 1)
	scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.7
	sun_data = bpy.data.lights.new("Key", "SUN")
	sun_data.energy = 2.5
	sun = bpy.data.objects.new("Key", sun_data)
	scene.collection.objects.link(sun)
	sun.rotation_euler = (math.radians(40), 0, math.radians(-60))

	scene.render.engine = "CYCLES"
	scene.cycles.samples = 32
	scene.view_settings.view_transform = "Standard"
	scene.render.resolution_x = scene.render.resolution_y = 480
	scene.render.filepath = path
	bpy.ops.render.render(write_still=True)


def main():
	argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
	output_dir = argv[0] if argv else "."
	preview_dir = argv[1] if len(argv) > 1 else None

	for name, build in MODELS.items():
		bpy.ops.wm.read_factory_settings(use_empty=True)
		root = bpy.data.objects.new(name, None)
		bpy.context.scene.collection.objects.link(root)
		root.scale = (VOXEL, VOXEL, VOXEL)
		parts = build(root)
		bpy.context.view_layer.update()
		bake_ao(parts)
		export(root, f"{output_dir}/{name}.fbx")
		print(f"Exported {name}: {len(parts)} parts, {sum(len(p.data.polygons) for p in parts)} faces")
		if preview_dir:
			render_preview(parts, f"{preview_dir}/{name}.png")


main()
