# Generates voxel-style tile models (Assets/Docs/VoxelStyle.md) and exports each model as FBX.
# Run headless:  Blender -b --factory-startup -P generate_voxel_tiles.py -- <tile_models_dir> [preview_dir]
#   tile_models_dir  Assets/Models/Tile; models are written to its Wood/ and Rock/ folders
#   preview_dir      optional: also renders a thumbnail-camera preview (R7c) of each model there, and of the wood
#                    pieces laid out together
#
# Parts are authored in voxels under a root empty scaled 0.1 (R1). The models are static, so AO is baked once with
# every part present (R26) into a face-corner colour attribute named AO (R23); it is the only vertex colour set exported.
#
# Design note (R3a; the references are the previous wood_tile_2 and rock models):
#   Wood tiles: a 1 m wooden block filling the tile, its top level with the ground and the origin at the top's centre.
#     The top is almost all the player sees, so it carries the detail: a lid of planks, 1-voxel gaps between them
#     showing the dark block beneath. Pieces differ only in which edges connect to a neighbouring wood tile - single,
#     end, straight, corner, T and cross - and every connected edge meets its neighbour the same way: planks 3, 2 and
#     3 wide. The 2-wide Oak middle plank runs from the centre to each connected edge; Pale wood planks run along each
#     unconnected edge; any corner left over is a 3 x 3 Pale wood plank. Plank ends are bevelled 1 x 1 where they meet a connected
#     edge only, mitred where two meet (R7b). A Slate strap crosses the planks 2 voxels in from each connected edge and
#     wraps down the sides, with a Light grey nail head on each 3-wide plank; one Walnut knot breaks the symmetry.
#     Straps and knot are markings (R12) and stop short of the bevels. Oak sides; no side grain (R7c).
#   Rock tile: a boulder sitting on a grass tile, origin at ground level. Keep the reference's lopsided cluster - one
#     big rounded stone with smaller stones around its foot - so the tile's rotation shows. A 0.8 x 0.7 m boulder
#     sunk a voxel into the ground with straight sides, corners cut 2 x 2 and a 2 x 2 bevel round the top; a taller
#     darker stone leans 10 deg against its side; a pale pebble at its foot. Everything stays inside the tile so
#     neighbouring rocks never meet. No moss (it would vanish against the grass) and no cracks (R7c).
#   Rabbit hole: a burrow the mower drops into and pops out of its pair, origin at ground level. A 9-wide octagonal
#     mound of dug earth (R7b: 9 clears the 8-voxel floor for a round part), sunk a voxel, 2 voxels proud with a 1 x 1
#     bevel round its top, around a square 5-wide opening (under 8 wide, so square, R7c) that drops 4 voxels below
#     ground to a dark floor. Earth darkens downwards: Chestnut top, Brown sides, Dark brown inside, Umber floor.
#     A carrot is planted point-down in the back corner, behind the opening from the thumbnail camera, leaning 10 deg
#     with two Forest leaves. Its body is its own part, PairMarker, in one colour, so each pair of holes can be told
#     apart by recolouring it: carrots come in orange, purple, yellow and white (CARROT_COLOURS). The terrain leaves a
#     gap for the tile, as it does for wood, so the model brings its own ground: a 1 m earth block, Chestnut on top,
#     with an octagonal opening the mound's base drops into.

import math
import sys

import bmesh
import bpy
from mathutils import Euler, Vector

VOXEL = 0.1
AO_DISTANCE = 2 * VOXEL
AO_SAMPLES = 128

PALETTE = {
	"PaleWood": "c89a60",  # P24
	"Oak": "9a6a3a",  # P25
	"Walnut": "6b4528",  # P26
	"LightGrey": "b2b2b2",  # P02
	"Slate": "66676c",  # P04
	"Limestone": "b8b0a4",  # P28
	"Stone": "8a837a",  # P29
	"DarkStone": "5c5650",  # P30
	"Chestnut": "8e5634",  # P13
	"Brown": "6e3d22",  # P14
	"DarkBrown": "4a2818",  # P15
	"Umber": "2e1810",  # P16
	"Forest": "3b7038",  # P22
	"Orange": "f08236",  # P44
	"Purple": "b868c0",  # P47
	"Yellow": "f6d021",  # P42
	"White": "f6f6f2",  # P00
}
GRASS = "5a9440"  # P21, preview only

WOOD_HALF = 5
WOOD_DEPTH = 10
DIRECTIONS = {"N": (0, 1), "E": (1, 0), "S": (0, -1), "W": (-1, 0)}
# Plank cells are 1 x 1 voxels indexed by their min corner, -5..4 on each axis. Along any connected edge:
OUTER_CELLS = (-5, -4, -3), (2, 3, 4)  # the two 3-wide Pale wood planks
MIDDLE_CELLS = (-1, 0)  # the 2-wide Oak plank; the cells between are the gaps
PLANK_THICKNESS = 2
PLANK_BEVEL = 1
STRAP = (2, 3)  # each strap covers this distance from the centre towards its connected edge
NAILS = (-3.5, 3.5)  # nail heads sit on the strap at these distances across it: the 3-wide planks' centres
KNOT = (0.5, -0.5)  # face centre on the top: the middle plank's centre, never bevelled or strapped

# Stones: (name, loft rings (see ring_outline), location, rotation, colour).
ROCKS = [
	("Boulder", [(-1, 4, 3.5, 2), (4, 4, 3.5, 2), (6, 2, 1.5, 1)], (0, 0.5, 0), (0, 0, 0), "Stone"),
	("LeaningStone", [(0, 2, 2, 1), (6, 2, 2, 1), (7, 1, 1, 0)], (2, -2, -1), (-10, 0, 20), "DarkStone"),
	("Pebble", [(0, 1.5, 1.5, 1), (2, 1.5, 1.5, 1), (3, 0.5, 0.5, 0)], (-3, -3, -1), (0, 0, -15), "Limestone"),
]

# The burrow's profile, outside in (see sweep): up the mound's side, over its bevel, across its top and down the opening.
MOUND_BASE = (-1, 4.5, 4.5, 2)
# The tile's ground: down its sides, across its top and down into the opening, where the mound's base hides the edge.
GROUND_PROFILE = [(-WOOD_DEPTH, WOOD_HALF), (0, WOOD_HALF), (0, 4.5, 4.5, 2), MOUND_BASE]
BURROW_PROFILE = [MOUND_BASE, (1, 4.5, 4.5, 2), (2, 3.5, 3.5, 1), (2, 2.5, 2.5, 0), (-4, 2.5, 2.5, 0), (-4, 0)]
CARROT = (3.5, 3.5, 4.5)  # where the carrot's top sits, in the back corner
CARROT_RINGS = [(0, 1.5), (4, 1), (7, 0)]  # built top-up, then turned point-down
CARROT_ROTATION = (170, 0, 45)
LEAF_TILTS = (-20, 20)
CARROT_COLOURS = ["Orange", "Purple", "Yellow", "White"]  # one per pair; the exported model is the first


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


def sweep(rings):
	"""An open surface through cross-sections (see ring_outline) in order, ending at a point if the last half width is
	0 and open otherwise. Its faces point to the left of the direction of travel, so a profile that climbs outside, crosses the top and
	drops down the inside faces out of the solid all the way."""
	n = 8
	verts, faces = [], []
	for ring in rings[:-1]:
		verts += [(x, y, ring[0]) for x, y in ring_outline(ring)]
	for r in range(len(rings) - 2):
		a, b = r * n, (r + 1) * n
		faces += [(a + i, a + (i + 1) % n, b + (i + 1) % n, b + i) for i in range(n)]
	a = (len(rings) - 2) * n
	if rings[-1][1] == 0:
		verts.append((0, 0, rings[-1][0]))
		faces += [(a + i, a + (i + 1) % n, a + n) for i in range(n)]
	else:
		verts += [(x, y, rings[-1][0]) for x, y in ring_outline(rings[-1])]
		faces += [(a + i, a + (i + 1) % n, a + n + (i + 1) % n, a + n + i) for i in range(n)]
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


def plank_cells(connections):
	"""The plank cells of a piece, as (cells, colour) per plank."""
	outer = set(OUTER_CELLS[0] + OUTER_CELLS[1])
	middle = {(i, j) for i in MIDDLE_CELLS for j in MIDDLE_CELLS}
	pale = {(i, j) for i in outer for j in outer}
	for name, (dx, dy) in DIRECTIONS.items():
		along = range(1, 5) if dx + dy > 0 else range(-5, -1)
		if name in connections:
			middle |= {(i, j) if dx else (j, i) for i in along for j in MIDDLE_CELLS}
		else:
			strip = OUTER_CELLS[1] if dx + dy > 0 else OUTER_CELLS[0]
			pale |= {(i, j) if dx else (j, i) for i in strip for j in range(-5, 5)}

	planks = [(middle, "Oak")]
	if not connections:
		# The pale planks would make a ring, which plank_solid can't outline: a frame of N and S planks the full width
		# and E and W planks between them
		for strip in OUTER_CELLS:
			cells = {(i, j) for i in range(-5, 5) for j in strip}
			pale -= cells
			planks.append((cells, "PaleWood"))
	while pale:
		component, frontier = set(), [pale.pop()]
		while frontier:
			i, j = frontier.pop()
			component.add((i, j))
			for neighbour in ((i + 1, j), (i - 1, j), (i, j + 1), (i, j - 1)):
				if neighbour in pale:
					pale.remove(neighbour)
					frontier.append(neighbour)
		planks.append((component, "PaleWood"))
	return planks


def plank_solid(cells, connections):
	"""A plank's cells extruded PLANK_THICKNESS down from the top, its top edge bevelled where it meets a connected
	edge (R7b). Vertices on two connected edges move in along both, which mitres the bevels where they meet."""
	bm = bmesh.new()
	for i, j in cells:
		bm.faces.new([bm.verts.new(co) for co in ((i, j, 0), (i + 1, j, 0), (i + 1, j + 1, 0), (i, j + 1, 0))])
	bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
	face = bmesh.ops.dissolve_faces(bm, faces=bm.faces[:])["region"][0]
	face.normal_update()
	outline = [v.co.xy.to_tuple() for v in face.verts]
	if face.normal.z < 0:
		outline.reverse()
	bm.free()

	def bevelled(x, y):
		for name, (dx, dy) in DIRECTIONS.items():
			if name in connections and x * dx + y * dy == WOOD_HALF:
				x, y = x - dx * PLANK_BEVEL, y - dy * PLANK_BEVEL
		return x, y

	n = len(outline)
	verts = [(x, y, -PLANK_THICKNESS) for x, y in outline] + [(x, y, -PLANK_BEVEL) for x, y in outline]
	verts += [bevelled(x, y) + (0,) for x, y in outline]
	faces = [tuple(reversed(range(n))), tuple(range(2 * n, 3 * n))]
	for a, b in ((0, n), (n, 2 * n)):
		faces += [(a + i, a + (i + 1) % n, b + (i + 1) % n, b + i) for i in range(n)]
	return verts, faces


def build_wood(root, connections, offset=(0, 0)):
	"""A wood tile whose plank lid connects to neighbouring wood tiles across the edges in `connections` (N E S W)."""
	straps = [DIRECTIONS[name] for name in connections]
	location = offset + (0,)

	def strap_colour(c, n):
		"""Slate where a face lies in a strap's band and runs along it, Light grey on its nail heads, else None."""
		for dx, dy in straps:
			if STRAP[0] < c.x * dx + c.y * dy < STRAP[1] and abs(n.x * dx + n.y * dy) < 0.01:
				across = c.x * dy - c.y * dx
				return "LightGrey" if n.z > 0.99 and any(abs(across - nail) < 0.01 for nail in NAILS) else "Slate"
		return None

	def block_colour(c, n):
		if n.z > 0.5:
			return "Walnut"
		return strap_colour(c, n) or "Oak"

	verts, faces = loft([(-WOOD_DEPTH, WOOD_HALF), (-PLANK_THICKNESS, WOOD_HALF)])
	parts = [build_part("Block", root, verts, faces, (0, 0, 0), block_colour, location)]
	for i, (cells, colour) in enumerate(plank_cells(connections)):
		def plank_colour(c, n, colour=colour):
			if n.z > 0.99 and abs(c.x - KNOT[0]) < 0.01 and abs(c.y - KNOT[1]) < 0.01:
				return "Walnut"
			return strap_colour(c, n) or colour

		verts, faces = plank_solid(cells, connections)
		parts.append(build_part(f"Plank{i + 1}", root, verts, faces, (0, 0, 0), plank_colour, location))
	return parts


# Wood pieces laid out as a window pane, every edge between two tiles connected: (x, y, connections)
WOOD_LAYOUT = [
	(-1, 1, "SE"), (0, 1, "WES"), (1, 1, "WS"),
	(-1, 0, "NSE"), (0, 0, "NESW"), (1, 0, "NSW"),
	(-1, -1, "NE"), (0, -1, "WEN"), (1, -1, "NW"),
]


def build_wood_layout(root):
	parts = []
	for x, y, connections in WOOD_LAYOUT:
		parts += build_wood(root, connections, (x * 2 * WOOD_HALF, y * 2 * WOOD_HALF))
	return parts


def build_rock(root):
	"""Every stone is a direct child of the root: nested parts come back misplaced through the FBX export (R18)."""
	parts = []
	for name, rings, location, rotation, colour in ROCKS:
		grid = (rings[0][1] % 1, rings[0][2] % 1, 0)
		verts, faces = loft(rings)
		parts.append(build_part(
			name, root, verts, faces, grid, lambda c, n, colour=colour: colour, location, rotation))
	return parts


def build_rabbit_hole(root, offset=(0, 0), carrot_colour=CARROT_COLOURS[0]):
	def burrow_colour(c, n):
		if c.z < -0.01:
			return "Umber" if n.z > 0.99 else "DarkBrown"
		return "Chestnut" if n.z > 0.5 else "Brown"

	verts, faces = sweep(BURROW_PROFILE)
	parts = [build_part("Burrow", root, verts, faces, (0.5, 0.5, 0), burrow_colour, offset + (0,))]
	verts, faces = sweep(GROUND_PROFILE)
	parts.append(build_part(
		"Ground", root, verts, faces, (0, 0, 0), lambda c, n: "Chestnut" if n.z > 0.5 else "Brown", offset + (0,)))

	x, y, z = CARROT
	verts, faces = loft(CARROT_RINGS)
	parts.append(build_part(
		"PairMarker", root, verts, faces, (0.5, 0.5, 0), lambda c, n: carrot_colour, (offset[0] + x, offset[1] + y, z),
		CARROT_ROTATION))
	for i, tilt in enumerate(LEAF_TILTS):
		verts, faces = loft([(0, 0.5), (4, 0.5)])
		parts.append(build_part(
			f"Leaf{i + 1}", root, verts, faces, (0.5, 0.5, 0), lambda c, n: "Forest", (offset[0] + x, offset[1] + y, z - 1),
			(tilt, 0, CARROT_ROTATION[2])))
	return parts


# Two pairs of holes, a tile of grass between the holes of each pair, as a level would place them.
HOLE_PAIRS = [(-10, 10, 0), (10, 10, 0), (-10, -10, 1), (10, -10, 1)]  # (x, y, index into CARROT_COLOURS)


def build_hole_pairs(root):
	parts = []
	for x, y, colour in HOLE_PAIRS:
		parts += build_rabbit_hole(root, (x, y), CARROT_COLOURS[colour])
	return parts


MODELS = {
	"Wood/wood_tile_voxel_single": lambda root: build_wood(root, ""),
	"Wood/wood_tile_voxel_end": lambda root: build_wood(root, "S"),
	"Wood/wood_tile_voxel_straight": lambda root: build_wood(root, "NS"),
	"Wood/wood_tile_voxel_corner": lambda root: build_wood(root, "SE"),
	"Wood/wood_tile_voxel_t": lambda root: build_wood(root, "WES"),
	"Wood/wood_tile_voxel_cross": lambda root: build_wood(root, "NESW"),
	"Rock/rock_voxel": build_rock,
	"RabbitHole/rabbit_hole_voxel": build_rabbit_hole,
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


def preview_ground(ground_z, holes):
	"""A 20 m ground plane, opened under each burrow at `holes` (voxel offsets) by the mound's base outline."""
	bm = bmesh.new()
	loops = [[(x, y) for x in (-10, 10) for y in ((-10, 10) if x < 0 else (10, -10))]]
	loops += [[(ox * VOXEL + x * VOXEL, oy * VOXEL + y * VOXEL) for x, y in ring_outline(MOUND_BASE)] for ox, oy in holes]
	edges = []
	for loop in loops:
		loop = [p for i, p in enumerate(loop) if p != loop[i - 1]]  # a square outline repeats its corners
		verts = [bm.verts.new((x, y, ground_z)) for x, y in loop]
		edges += [bm.edges.new((verts[i], verts[(i + 1) % len(verts)])) for i in range(len(verts))]
	bmesh.ops.triangle_fill(bm, use_beauty=True, use_dissolve=True, edges=edges, normal=Vector((0, 0, 1)))
	mesh = bpy.data.meshes.new("PreviewGround")
	bm.to_mesh(mesh)
	bm.free()
	ground = bpy.data.objects.new("PreviewGround", mesh)
	bpy.context.scene.collection.objects.link(ground)
	return ground


def render_preview(parts, path, ground_z, holes=()):
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

	ground = preview_ground(ground_z, holes)
	ground_material = bpy.data.materials.new("PreviewGround")
	ground_material.use_nodes = True
	nodes = ground_material.node_tree.nodes
	nodes.clear()
	emission = nodes.new("ShaderNodeEmission")
	emission.inputs["Color"].default_value = hex_to_linear(GRASS)
	output = nodes.new("ShaderNodeOutputMaterial")
	ground_material.node_tree.links.new(emission.outputs["Emission"], output.inputs["Surface"])
	ground.data.materials.append(ground_material)

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

	def new_model(name, build):
		bpy.ops.wm.read_factory_settings(use_empty=True)
		root = bpy.data.objects.new(name, None)
		bpy.context.scene.collection.objects.link(root)
		root.scale = (VOXEL, VOXEL, VOXEL)
		parts = build(root)
		bpy.context.view_layer.update()
		bake_ao(parts)
		return root, parts

	for name, build in MODELS.items():
		model_name = name.split("/")[-1]
		root, parts = new_model(model_name, build)
		export(root, f"{output_dir}/{name}.fbx")
		print(f"Exported {model_name}: {len(parts)} parts, {sum(len(p.data.polygons) for p in parts)} faces")
		if preview_dir:
			ground_z = -WOOD_DEPTH * VOXEL if name.startswith(("Wood", "RabbitHole")) else 0
			render_preview(parts, f"{preview_dir}/{model_name}.png", ground_z)

	if preview_dir:
		_, parts = new_model("WoodLayout", build_wood_layout)
		render_preview(parts, f"{preview_dir}/wood_layout.png", -WOOD_DEPTH * VOXEL)
		_, parts = new_model("HolePairs", build_hole_pairs)
		render_preview(parts, f"{preview_dir}/rabbit_hole_pairs.png", -WOOD_DEPTH * VOXEL)


main()
