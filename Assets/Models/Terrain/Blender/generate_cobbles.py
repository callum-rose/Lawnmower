# Generates the voxel-style cobblestones (Assets/Docs/VoxelStyle.md) that edge the SDF terrain, and exports them as
# one FBX with one mesh object per cobble.
# Run headless:  blender -b --factory-startup -P generate_cobbles.py -- <output.fbx> [preview.png]
#   preview.png  optional: also renders the cobbles from the thumbnail camera (R7c)
#
# Each cobble is a single static part, so AO is baked once per cobble with only itself present (R26) into a
# face-corner colour attribute named AO (R23), the only vertex colour set exported. Vertices are written in metres
# (one voxel is 0.1, R1) with the origin at the centre of the base (R2), and the export applies its transform, so the
# game can read each Mesh sub-asset directly and scatter it along the shoreline.
#
# Design note (R3a; reference: a Japanese-garden island ringed by rounded, mossy cobbles):
#   A row of chunky cobbles, three or four to a tile edge, sitting half sunk along the top of the shoreline. They are
#   edging, not props of their own, so they sit well under R3's 12-voxel floor: 4 to 6 voxels long and 3 tall,
#   about the size of the reference's stones against a 10-voxel tile.
#   Keep: the rounded silhouette, as cut corners and a 1 x 1 bevel round the top (as the pond's stepping stones,
#     R7b), warm pale greys in two shades, and a moss cap on about half of them in two greens (the upper colour
#     takes the bevel, R7b).
#   Drop: the lumpy, organic shapes and the speckled moss patches (R7c, R12).
#   Variety comes from six sizes and colourings; the game turns each one along the shoreline in 5 deg steps (R6).

import math
import sys

import bmesh
import bpy
from mathutils import Vector

VOXEL = 0.1
AO_DISTANCE = 2 * VOXEL
AO_SAMPLES = 128

PALETTE = {
	"Limestone": "b8b0a4",  # P28
	"Stone": "8a837a",  # P29
	"DarkStone": "5c5650",  # P30
	"Olive": "9ea26a",  # P20
	"Grass": "5a9440",  # P21
}
GROUND = "5a9440"  # P21, preview only

# (name, half length, half depth, height, stone colour, moss colour or None), in voxels
COBBLES = [
	("CobbleA", 2.5, 2, 3, "Limestone", "Grass"),
	("CobbleB", 2, 2, 3, "Stone", None),
	("CobbleC", 3, 2, 2, "Limestone", "Olive"),
	("CobbleD", 2, 2, 2, "Stone", "Grass"),
	("CobbleE", 3, 2, 3, "Limestone", None),
	("CobbleF", 2, 2, 3, "DarkStone", "Olive"),
]
# Every half width is at least CORNER + BEVEL, so the crown keeps its cut corners
CORNER = 1
BEVEL = 1


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


def ring_outline(hx, hy, k):
	"""A rectangle's cross-section with its corners bevelled k x k (R7b), counter-clockwise."""
	return [(-(hx - k), -hy), (hx - k, -hy), (hx, -(hy - k)), (hx, hy - k), (hx - k, hy), (-(hx - k), hy), (-hx, hy - k), (-hx, -(hy - k))]


def cobble(hx, hy, height):
	"""Straight sides with cut corners, then a BEVEL x BEVEL slope round the top to a smaller flat crown."""
	rings = [(0, hx, hy, CORNER), (height - BEVEL, hx, hy, CORNER), (height, hx - BEVEL, hy - BEVEL, CORNER)]
	n = 8
	verts, faces = [], []
	for z, rx, ry, k in rings:
		verts += [(x, y, z) for x, y in ring_outline(rx, ry, k)]
	faces.append(tuple(reversed(range(n))))
	for r in range(len(rings) - 1):
		a, b = r * n, (r + 1) * n
		faces += [(a + i, a + (i + 1) % n, b + (i + 1) % n, b + i) for i in range(n)]
	faces.append(tuple(range((len(rings) - 1) * n, len(rings) * n)))
	return verts, faces


def build_cobble(name, hx, hy, height, stone, moss):
	"""Faces are cut to one segment per voxel on the cobble's own grid (R23) and coloured per voxel: moss over the
	crown and its bevel, stone below."""
	verts, faces = cobble(hx, hy, height)
	grid_offset = (hx % 1, hy % 1, 0)

	mesh = bpy.data.meshes.new(name)
	bm = bmesh.new()
	bm_verts = [bm.verts.new(v) for v in verts]
	for face in faces:
		bm.faces.new([bm_verts[i] for i in face])
	bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)

	for axis in range(3):
		values = [v.co[axis] for v in bm.verts]
		lo, hi = min(values), max(values)
		plane = math.floor(lo - grid_offset[axis]) + grid_offset[axis] + 1
		while plane < hi - 1e-4:
			normal = Vector((0, 0, 0))
			normal[axis] = 1
			co = Vector((0, 0, 0))
			co[axis] = plane
			bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:], plane_co=co, plane_no=normal, dist=1e-5)
			plane += 1

	materials = {}
	for face in bm.faces:
		on_top = face.calc_center_median().z > height - BEVEL
		hex_colour = PALETTE[moss if moss and on_top else stone]
		if hex_colour not in materials:
			materials[hex_colour] = len(materials)
			mesh.materials.append(palette_material(hex_colour))
		face.material_index = materials[hex_colour]

	for v in bm.verts:
		v.co *= VOXEL

	bm.normal_update()
	bm.to_mesh(mesh)
	bm.free()

	obj = bpy.data.objects.new(name, mesh)
	bpy.context.scene.collection.objects.link(obj)
	return obj


def bake_ao(part):
	scene = bpy.context.scene
	scene.render.engine = "CYCLES"
	scene.cycles.device = "CPU"
	scene.cycles.samples = AO_SAMPLES
	if scene.world is None:
		scene.world = bpy.data.worlds.new("World")
	scene.world.light_settings.distance = AO_DISTANCE

	attribute = part.data.color_attributes.new("AO", "BYTE_COLOR", "CORNER")
	part.data.color_attributes.active_color = attribute
	part.data.color_attributes.render_color_index = part.data.color_attributes.active_color_index

	bpy.ops.object.select_all(action="DESELECT")
	part.select_set(True)
	bpy.context.view_layer.objects.active = part
	bpy.ops.object.bake(type="AO", target="VERTEX_COLORS")


def export(parts, path):
	bpy.ops.object.select_all(action="DESELECT")
	for part in parts:
		part.select_set(True)
	bpy.context.view_layer.objects.active = parts[0]
	bpy.ops.export_scene.fbx(
		filepath=path,
		use_selection=True,
		object_types={"MESH"},
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

	# Laid out in a row, as they sit along the shoreline
	x = 0
	for i, part in enumerate(parts):
		half = COBBLES[i][1] * VOXEL
		part.location = (x + half, 0, 0)
		part.rotation_euler = (0, 0, math.radians((-1) ** i * 5))
		x += half * 2 + 0.03

	scene = bpy.context.scene
	bpy.ops.mesh.primitive_plane_add(size=20, location=(0, 0, 0))
	ground = bpy.context.object
	ground_material = bpy.data.materials.new("PreviewGround")
	ground_material.use_nodes = True
	ground_material.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = hex_to_linear(GROUND)
	ground.data.materials.append(ground_material)

	centre = Vector((x / 2, 0, 0.1))
	camera_data = bpy.data.cameras.new("Thumbnail")
	camera_data.type = "ORTHO"
	camera_data.ortho_scale = x * 1.1
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
	scene.render.resolution_x = 960
	scene.render.resolution_y = 480
	scene.render.filepath = path
	bpy.ops.render.render(write_still=True)


def main():
	argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
	output = argv[0] if argv else "cobbles.fbx"
	preview = argv[1] if len(argv) > 1 else None

	bpy.ops.wm.read_factory_settings(use_empty=True)
	parts = [build_cobble(*spec) for spec in COBBLES]
	bpy.context.view_layer.update()
	for part in parts:
		for other in parts:
			other.hide_render = other is not part
		bake_ao(part)
	for part in parts:
		part.hide_render = False
	export(parts, output)
	print(f"Exported {len(parts)} cobbles, {sum(len(p.data.polygons) for p in parts)} faces")
	if preview:
		render_preview(parts, preview)


main()
