# Generates the smooth, mossy shore rocks that ring the SDF terrain, and exports them as one FBX with one mesh object
# per rock, for TerrainEdging to scatter along the shoreline (it reads each mesh like the voxel cobbles it replaces).
# Run headless:  blender -b --factory-startup -P generate_shore_rocks.py -- <output.fbx> [preview.png] [set]
#   preview.png  optional: also renders the rocks laid out in a row, as they sit along the shore ("-" to skip)
#   set          rocks (default; shore_rocks.fbx), slabs (shore_slabs.fbx) or pebbles (shore_pebbles.fbx)
#
# Design note (reference: a Japanese-garden island ringed by rounded river stones with soft moss caps):
#   Chunky, rounded, slightly flat-topped stones, two or three to a tile edge, sunk into the shoreline. Some carry a
#   small pebble tucked beside them so the ring doesn't read as an even row.
#   Shape: a superellipsoid (rounder than a box, boxier than an egg) with a squashed underside, lumped by low-frequency
#     noise so no two match.
#   Stone colour is baked per vertex: a warm or cool grey per stone, mottled and darkening towards the base. Moss is
#     grown over the upward faces through a noise mask and pushed out a touch so the patches sit proud, but it isn't
#     painted here: the mask goes out smooth in the Shade UV's V (0.5 is the patch edge), and the Game/ShoreRock shader
#     thresholds it per pixel and textures the moss, so its edges stay crisp however coarse the mesh. Cycles AO (with
#     the terrain top present as a ground plane) goes in the Shade UV's U.
#   Slabs (the same reference's other take): long, low, oval stones in two tiers, a smaller raised top sitting
#     off-centre on a wide base, weathered with shallow dents and a crack or two scored across the top (grooved and
#     darkened). Hardly any moss; small pebbles and moss tufts sit at their ends to fill the joints between neighbours.
#     TerrainEdging only lays them along long straight runs, never round corners.
#   Pebbles: small round stones, some in pairs, and loose moss tufts, which TerrainEdging scatters over the earth
#     between the grass and the rocks and into the joints between rocks.
#   Vertices are in metres with the origin at the centre of the base; each rock's length runs along X.

import math
import random
import sys

import bmesh
import bpy
from mathutils import Vector, noise

SUBDIVISIONS = 4
SLAB_TIER_SUBDIVISIONS = 5  # the upper tier carries the cracks, so it needs the finer mesh
CRACK_WIDTH = 0.016
CRACK_DEPTH = 0.003
CRACK_DARKEN = 0.22
GROUND = 0.12  # where the terrain top cuts the rock, matching TerrainEdging's sink
AO_DISTANCE = 0.12
AO_SAMPLES = 128

# sRGB stone greys
STONES = {
	"Warm": "b3aa9c",
	"Pale": "c4beb4",
	"Mid": "999389",
	"Cool": "8d9196",
	"Slate": "73767c",
	"Slab": "a6a29a",
	"SlabCool": "979ba0",
	"SlabWarm": "aea594",
}
# Where the moss coverage field crosses a patch's edge, and the span of it the mask carries either side
MOSS_EDGE = 0.56
MOSS_MASK_RANGE = 0.3
# Preview only: the game's moss colours live on the ShoreRock material
MOSS_DARK = "4f6f26"
MOSS_LIGHT = "8aa543"

# Plan and profile superellipse exponents (higher is boxier / flatter) and how lumpy the surface is
SHAPES = {
	"round": (2.8, 2.8, 0.09),
	"slab": (2.3, 3.6, 0.045),
	"tuft": (2.2, 2.2, 0.2),
	"pebble": (2.6, 2.6, 0.08),
}

# (name, length, depth, height, stone, moss amount 0..1, seed, extras), in metres. Extras sit at the rock's ends:
# ("pebble", length, stone) towards the water, ("tuft", length) at the joint with the next rock.
ROCKS = [
	("RockA", 0.62, 0.45, 0.28, "Warm", 0.9, 1, []),
	("RockB", 0.53, 0.41, 0.26, "Cool", 0.5, 2, []),
	("RockC", 0.58, 0.40, 0.24, "Pale", 0.0, 3, [("pebble", 0.19, "Mid")]),
	("RockD", 0.47, 0.39, 0.28, "Slate", 0.7, 4, []),
	("RockE", 0.65, 0.46, 0.25, "Mid", 0.3, 5, []),
	("RockF", 0.50, 0.40, 0.25, "Warm", 0.0, 6, [("pebble", 0.16, "Slate")]),
	("RockG", 0.55, 0.43, 0.29, "Cool", 1.0, 7, []),
	("RockH", 0.45, 0.36, 0.22, "Pale", 0.6, 8, [("pebble", 0.18, "Warm")]),
]

SLABS = [
	("SlabA", 0.80, 0.44, 0.17, "Slab", 0.0, 11, [("tuft", 0.2)]),
	("SlabB", 0.72, 0.42, 0.16, "SlabCool", 0.0, 12, [("pebble", 0.16, "Mid")]),
	("SlabC", 0.86, 0.46, 0.18, "SlabWarm", 0.0, 13, [("tuft", 0.18)]),
	("SlabD", 0.70, 0.41, 0.17, "Slab", 0.0, 14, [("pebble", 0.13, "Pale"), ("tuft", 0.17)]),
	("SlabE", 0.78, 0.45, 0.17, "SlabCool", 0.0, 15, []),
	("SlabF", 0.74, 0.43, 0.17, "SlabWarm", 0.0, 16, [("pebble", 0.15, "Slate"), ("tuft", 0.19)]),
]

# A stone of None is a loose moss tuft
PEBBLES = [
	("PebbleA", 0.17, 0.14, 0.09, "Pale", 0.0, 21, []),
	("PebbleB", 0.13, 0.11, 0.07, "Warm", 0.0, 22, []),
	("PebbleC", 0.19, 0.15, 0.10, "Mid", 0.5, 23, []),
	("PebbleD", 0.14, 0.12, 0.08, "Pale", 0.0, 24, [("pebble", 0.09, "Cool")]),
	("PebbleE", 0.11, 0.10, 0.06, "Pale", 0.0, 25, []),
	("PebbleF", 0.18, 0.14, 0.09, "Warm", 0.6, 26, [("pebble", 0.08, "Pale")]),
	("PebbleG", 0.15, 0.12, 0.08, "Slate", 0.0, 27, []),
	("TuftA", 0.15, 0.13, 0.08, None, 1.0, 28, []),
	("TuftB", 0.12, 0.11, 0.07, None, 1.0, 29, []),
]

SETS = {"rocks": (ROCKS, "round"), "slabs": (SLABS, "slab"), "pebbles": (PEBBLES, "pebble")}


def hex_to_srgb(hex_colour):
	return Vector(int(hex_colour[i:i + 2], 16) / 255 for i in (0, 2, 4))


def smoothstep(a, b, x):
	t = min(max((x - a) / (b - a), 0.0), 1.0)
	return t * t * (3 - 2 * t)


def stone_shape(bm, length, depth, height, seed, shape):
	"""Projects a unit icosphere onto a superellipsoid (separate plan and profile exponents), squashes the underside
	and lumps it with noise."""
	plan, profile, lumpiness = SHAPES[shape]
	offset = Vector((seed * 13.7, seed * 5.3, seed * 9.1))
	half = Vector((length / 2, depth / 2, height))
	for v in bm.verts:
		d = v.co.normalized()
		radial = (abs(d.x) ** plan + abs(d.y) ** plan) ** (profile / plan)
		r = (radial + abs(d.z) ** profile) ** (-1 / profile)
		p = d * r
		# The lower half is buried, so squash it to keep the vertex count above ground
		z = p.z if p.z > 0 else p.z * 0.35
		co = Vector((p.x * half.x, p.y * half.y, z * half.z))
		lump = noise.fractal(d * 1.3 + offset, 0.6, 2.0, 3) * lumpiness
		lump += noise.noise(d * 3.1 + offset) * 0.012
		if shape == "tuft":
			lump += noise.noise(d * 6 + offset) * 0.06
		scale = min(half) * 2
		co += d * lump * scale
		v.co = co
	# Lift so the widest point sits a little above the ground line
	lowest = min(v.co.z for v in bm.verts)
	for v in bm.verts:
		v.co.z -= lowest


def build_rock(name, length, depth, height, stone, moss, seed, extras, shape):
	rng = random.Random(seed)
	mesh = bpy.data.meshes.new(name)
	# (length, depth, height, stone, moss, seed, offset, shape, subdivisions, weathered)
	if stone is None:
		shape = "tuft"
	# Slabs and pebbles are too low to bury their widest line, so lift them to just above the ground
	lift = {
		"slab": max(0.0, GROUND + 0.02 - 0.35 * height),
		"pebble": GROUND + 0.005 - 0.35 * height,
		"tuft": GROUND - 0.3 * height,
	}.get(shape, 0.0)
	subdivisions = SUBDIVISIONS if length > 0.2 else SUBDIVISIONS - 1
	parts = [(length, depth, height, stone, moss, seed, Vector((0, 0, lift)), shape, subdivisions, shape == "slab")]
	if shape == "slab":
		# The upper tier: a smaller oval off-centre on the base, its widest line just under the base's top
		tier_length, tier_depth, tier_height = length * rng.uniform(0.6, 0.72), depth * rng.uniform(0.58, 0.68), height * 0.45
		at = Vector((rng.uniform(-0.12, 0.12) * length, rng.uniform(-0.1, 0.06) * depth,
			lift + 1.35 * height - 0.35 * tier_height - 0.012))
		parts.append((tier_length, tier_depth, tier_height, stone, moss, seed + 50, at, shape, SLAB_TIER_SUBDIVISIONS, True))
	side = rng.choice((-1, 1))
	for i, extra in enumerate(extras):
		kind, size = extra[0], extra[1]
		if kind == "pebble":
			# At one end, towards the water, resting on the ground line
			at = Vector((side * (length * 0.5 + size * 0.05), -depth * 0.42, GROUND - size * 0.12))
			parts.append((size, size * 0.85, size * 0.55, extra[2], moss * 0.5, seed + 100 + i, at, "round",
				SUBDIVISIONS - 1, False))
		else:
			# In the joint at the other end, half under the next rock
			at = Vector((-side * (length * 0.5 + size * 0.3), rng.uniform(-0.3, 0.2) * depth, GROUND - size * 0.2))
			parts.append((size, size * 0.9, size * 0.6, None, 1.0, seed + 200 + i, at, "tuft", SUBDIVISIONS - 1, False))

	verts, faces, colours, masks = [], [], [], []
	for part_length, part_depth, part_height, part_stone, part_moss, part_seed, at, part_shape, subdivisions, weathered in parts:
		part = bmesh.new()
		bmesh.ops.create_icosphere(part, subdivisions=subdivisions, radius=1)
		stone_shape(part, part_length, part_depth, part_height, part_seed, part_shape)
		part.normal_update()
		cracks = weather(part, part_length, part_depth, part_seed) if weathered else {}
		colour_part(part, part_stone, part_moss, part_seed, part_height, cracks)
		part.verts.index_update()
		colour_layer = part.verts.layers.float_color["Col"]
		mask_layer = part.verts.layers.float["Moss"]
		base = len(verts)
		for v in part.verts:
			verts.append(v.co + at)
			colours.append(tuple(v[colour_layer]))
			masks.append(v[mask_layer])
		faces += [tuple(base + v.index for v in face.verts) for face in part.faces]
		part.free()

	mesh.from_pydata(verts, [], faces)
	mesh.attributes.new("Col", "FLOAT_COLOR", "POINT").data.foreach_set("color", [c for colour in colours for c in colour])
	mesh.attributes.new("Moss", "FLOAT", "POINT").data.foreach_set("value", masks)
	for polygon in mesh.polygons:
		polygon.use_smooth = True

	obj = bpy.data.objects.new(name, mesh)
	bpy.context.scene.collection.objects.link(obj)
	return obj


def weather(bm, length, depth, seed):
	"""Knocks dents into the stone and scores cracks across its top. Returns each cracked vertex's depth into its crack,
	0..1, for colouring."""
	rng = random.Random(seed * 31 + 7)
	bm.verts.index_update()
	verts = list(bm.verts)
	size = min(length, depth)

	# Dents: shallow round scoops, most on the top and upper sides where they catch the light
	candidates = [v for v in verts if v.normal.z > -0.1 and v.co.z > 0.02]
	for _ in range(rng.randint(4, 7)):
		centre = rng.choice(candidates)
		c, n = centre.co.copy(), centre.normal.copy()
		radius = size * rng.uniform(0.1, 0.2)
		dent = radius * rng.uniform(0.12, 0.22)
		for v in verts:
			d = (v.co - c).length
			if d < radius:
				f = 1 - (d / radius) ** 2
				v.co -= n * dent * f * f

	# Cracks: a jagged line wandering in from the rim across the top
	top = max(v.co.z for v in verts)
	cracks = {}
	for _ in range(1 if rng.random() < 0.6 else 2):
		angle = rng.uniform(0, math.tau)
		point = Vector((math.cos(angle) * length * 0.55, math.sin(angle) * depth * 0.55))
		heading = angle + math.pi + rng.uniform(-0.6, 0.6)
		line = [point.copy()]
		for _ in range(rng.randint(3, 5)):
			heading += rng.uniform(-0.7, 0.7)
			point += Vector((math.cos(heading), math.sin(heading))) * size * rng.uniform(0.15, 0.28)
			line.append(point.copy())
		width = CRACK_WIDTH
		for v in verts:
			if v.normal.z < 0.15 or v.co.z < top * 0.5:
				continue
			p = v.co.xy
			distance = min(segment_distance(p, a, b) for a, b in zip(line, line[1:]))
			if distance < width:
				f = 1 - distance / width
				f = f * f * (3 - 2 * f)
				v.co.z -= CRACK_DEPTH * f
				cracks[v.index] = max(cracks.get(v.index, 0), f)
	bm.normal_update()
	return cracks


def segment_distance(p, a, b):
	ab = b - a
	t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
	return (p - (a + ab * t)).length


def colour_part(bm, stone, moss, seed, height, cracks):
	"""Stone grey, mottled and darker towards the base, and the mask of the moss grown on the upward faces (0.5 at a
	patch's edge, written to the Moss layer). Moss verts are pushed out along their normal so the patches sit proud.
	With no stone it's a tuft: moss all over."""
	layer = bm.verts.layers.float_color.new("Col")
	mask_layer = bm.verts.layers.float.new("Moss")
	offset = Vector((seed * 3.3, seed * 7.9, seed * 1.7))
	base = hex_to_srgb(STONES[stone or "Mid"])
	for v in bm.verts:
		p = v.co
		mottle = 1 + noise.fractal(p * 9 + offset, 0.5, 2.0, 3) * 0.07
		speck = 1 + noise.noise(p * 40 + offset) * 0.03
		t = min(max(p.z / height, 0), 1)
		shade = 0.72 + 0.33 * smoothstep(0.0, 0.8, t)
		colour = base * (mottle * speck * shade)
		colour *= 1 - CRACK_DARKEN * cracks.get(v.index, 0)

		mask = 0.0
		if not stone:
			mask = 1.0
		elif moss > 0:
			up = v.normal.z
			patch = noise.fractal(p * 6 + offset + Vector((50, 0, 0)), 0.55, 2.0, 4)
			coverage = up * 0.7 + patch * 1.1 + (moss - 0.5) * 0.6 + (t - 0.7) * 0.6
			mask = min(max(0.5 + (coverage - MOSS_EDGE) / MOSS_MASK_RANGE, 0.0), 1.0)
			v.co += v.normal * smoothstep(0.4, 0.6, mask) * 0.006

		v[layer] = (colour.x, colour.y, colour.z, 1.0)
		v[mask_layer] = mask


def bake_ao(rocks):
	"""Bakes AO for each rock alone on a ground plane at the terrain top, then packs it with the moss mask into the
	Shade UV map and leaves only the stone colour set."""
	scene = bpy.context.scene
	scene.render.engine = "CYCLES"
	scene.cycles.device = "CPU"
	scene.cycles.samples = AO_SAMPLES
	if scene.world is None:
		scene.world = bpy.data.worlds.new("World")
	scene.world.light_settings.distance = AO_DISTANCE

	bpy.ops.mesh.primitive_plane_add(size=4, location=(0, 0, GROUND))
	ground = bpy.context.object

	for rock in rocks:
		for other in rocks:
			other.hide_render = other is not rock
		mesh = rock.data
		ao = mesh.color_attributes.new("AO", "BYTE_COLOR", "CORNER")
		mesh.color_attributes.active_color = ao
		bpy.ops.object.select_all(action="DESELECT")
		rock.select_set(True)
		bpy.context.view_layer.objects.active = rock
		bpy.ops.object.bake(type="AO", target="VERTEX_COLORS")

		# Adding layers reallocates the mesh's data, so look every layer up again after the last one is added
		mesh.color_attributes.new("Colour", "BYTE_COLOR", "CORNER")
		mesh.uv_layers.new(name="Shade")
		col = mesh.color_attributes["Col"]
		ao = mesh.color_attributes["AO"]
		mask = mesh.attributes["Moss"]
		final = mesh.color_attributes["Colour"]
		shade = mesh.uv_layers["Shade"]
		for loop in mesh.loops:
			c = col.data[loop.vertex_index].color
			final.data[loop.index].color_srgb = (c[0], c[1], c[2], 1.0)
			shade.data[loop.index].uv = (ao.data[loop.index].color[0], mask.data[loop.vertex_index].value)
		mesh.attributes.remove(mesh.attributes["Moss"])
		mesh.color_attributes.remove(mesh.color_attributes["AO"])
		mesh.color_attributes.remove(mesh.color_attributes["Col"])
		mesh.color_attributes.active_color = mesh.color_attributes["Colour"]
		mesh.color_attributes.render_color_index = mesh.color_attributes.active_color_index

	for rock in rocks:
		rock.hide_render = False
	bpy.data.objects.remove(ground)


def assign_material(rocks):
	material = bpy.data.materials.new("ShoreRock")
	material.use_nodes = True
	nodes, links = material.node_tree.nodes, material.node_tree.links
	attribute = nodes.new("ShaderNodeVertexColor")
	attribute.layer_name = "Colour"
	links.new(attribute.outputs["Color"], nodes["Principled BSDF"].inputs["Base Color"])
	nodes["Principled BSDF"].inputs["Roughness"].default_value = 1.0
	for rock in rocks:
		rock.data.materials.append(material)


def export(rocks, path):
	bpy.ops.object.select_all(action="DESELECT")
	for rock in rocks:
		rock.select_set(True)
	bpy.context.view_layer.objects.active = rocks[0]
	bpy.ops.export_scene.fbx(
		filepath=path,
		use_selection=True,
		object_types={"MESH"},
		mesh_smooth_type="FACE",
		colors_type="SRGB",
		prioritize_active_color=True,
		use_custom_props=False,
		add_leaf_bones=False,
		bake_anim=False,
		axis_forward="-Z",
		axis_up="Y",
		bake_space_transform=True,
	)


def render_preview(rocks, specs, path):
	"""The rocks in a row on a lawn-green ground, sunk to the terrain top, from a three-quarter view. Moss and AO are
	applied from the Shade UV roughly as Game/ShoreRock does."""
	for rock in rocks:
		material = bpy.data.materials.new(f"{rock.name}_Preview")
		material.use_nodes = True
		nodes, links = material.node_tree.nodes, material.node_tree.links
		nodes.clear()
		colour = nodes.new("ShaderNodeVertexColor")
		colour.layer_name = "Colour"
		uv = nodes.new("ShaderNodeUVMap")
		uv.uv_map = "Shade"
		split = nodes.new("ShaderNodeSeparateXYZ")
		links.new(uv.outputs["UV"], split.inputs["Vector"])
		edge = nodes.new("ShaderNodeMapRange")
		edge.inputs["From Min"].default_value = 0.48
		edge.inputs["From Max"].default_value = 0.52
		links.new(split.outputs["Y"], edge.inputs["Value"])
		texture = nodes.new("ShaderNodeTexNoise")
		texture.inputs["Scale"].default_value = 60
		moss = nodes.new("ShaderNodeMix")
		moss.data_type = "RGBA"
		moss.inputs["A"].default_value = hex_to_srgb(MOSS_DARK).to_4d()
		moss.inputs["B"].default_value = hex_to_srgb(MOSS_LIGHT).to_4d()
		links.new(texture.outputs["Fac"], moss.inputs["Factor"])
		albedo = nodes.new("ShaderNodeMix")
		albedo.data_type = "RGBA"
		links.new(edge.outputs["Result"], albedo.inputs["Factor"])
		links.new(colour.outputs["Color"], albedo.inputs["A"])
		links.new(moss.outputs["Result"], albedo.inputs["B"])
		ao = nodes.new("ShaderNodeMapRange")
		ao.inputs["To Min"].default_value = 0.45
		links.new(split.outputs["X"], ao.inputs["Value"])
		shaded = nodes.new("ShaderNodeMix")
		shaded.data_type = "RGBA"
		shaded.blend_type = "MULTIPLY"
		shaded.inputs["Factor"].default_value = 1
		links.new(albedo.outputs["Result"], shaded.inputs["A"])
		links.new(ao.outputs["Result"], shaded.inputs["B"])
		diffuse = nodes.new("ShaderNodeBsdfDiffuse")
		links.new(shaded.outputs["Result"], diffuse.inputs["Color"])
		output = nodes.new("ShaderNodeOutputMaterial")
		links.new(diffuse.outputs["BSDF"], output.inputs["Surface"])
		rock.data.materials[0] = material

	x = 0
	for i, (rock, spec) in enumerate(zip(rocks, specs)):
		half = spec[1] / 2
		rock.location = (x + half, 0, -GROUND)
		rock.rotation_euler = (0, 0, math.radians((-1) ** i * 6))
		x += half * 2 - 0.02

	scene = bpy.context.scene
	bpy.ops.mesh.primitive_plane_add(size=20, location=(0, 0, 0))
	ground = bpy.context.object
	ground_material = bpy.data.materials.new("PreviewGround")
	ground_material.use_nodes = True
	ground_material.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.1, 0.2, 0.05, 1)
	ground.data.materials.append(ground_material)

	centre = Vector((x / 2, 0, 0.08))
	camera_data = bpy.data.cameras.new("Preview")
	camera_data.type = "ORTHO"
	camera_data.ortho_scale = x * 1.05
	camera = bpy.data.objects.new("Preview", camera_data)
	scene.collection.objects.link(camera)
	elevation, azimuth = math.radians(35), math.radians(-115)
	direction = Vector((math.cos(elevation) * math.cos(azimuth), math.cos(elevation) * math.sin(azimuth), math.sin(elevation)))
	camera.location = centre + direction * 6
	camera.rotation_euler = (-direction).to_track_quat("-Z", "Y").to_euler()
	scene.camera = camera

	scene.world.use_nodes = True
	scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.8, 0.9, 1, 1)
	scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.6
	sun_data = bpy.data.lights.new("Key", "SUN")
	sun_data.energy = 2.2
	sun_data.angle = math.radians(10)
	sun = bpy.data.objects.new("Key", sun_data)
	scene.collection.objects.link(sun)
	sun.rotation_euler = (math.radians(40), 0, math.radians(-40))

	scene.cycles.samples = 48
	scene.view_settings.view_transform = "Standard"
	scene.render.resolution_x = 1600
	scene.render.resolution_y = 520
	scene.render.filepath = path
	bpy.ops.render.render(write_still=True)


def main():
	argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
	output = argv[0] if argv else "shore_rocks.fbx"
	preview = argv[1] if len(argv) > 1 and argv[1] != "-" else None
	specs, shape = SETS[argv[2] if len(argv) > 2 else "rocks"]

	bpy.ops.wm.read_factory_settings(use_empty=True)
	rocks = [build_rock(*spec, shape) for spec in specs]
	bpy.context.view_layer.update()
	bake_ao(rocks)
	assign_material(rocks)
	export(rocks, output)
	print(f"Exported {len(rocks)} rocks, {sum(len(r.data.vertices) for r in rocks)} vertices, "
		f"{sum(len(r.data.polygons) for r in rocks)} faces")
	if preview:
		render_preview(rocks, specs, preview)


main()
