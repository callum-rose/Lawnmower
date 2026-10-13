"""Builds the in-game Egg Bunny: egg_bunny.blend, egg_bunny.fbx and egg_bunny_tex.png.

python build_egg_bunny_game.py -- <regular_bunny.blend> <out dir for the .fbx> <out .blend> <out tex .png>

(or `blender --background --factory-startup --python build_egg_bunny_game.py -- ...`)

It builds `build_bunny.py variant=egg`, then re-frames it to drop into everything the Regular Bunny already
has in Unity, so the Regular Bunny's animation clips and controller play on it unchanged:

- The same parts with the same names: Body, ear_left, ear_right, foot_{front,back}_{left,right}, tail
  (plus ear_*_patch, the inner-ear colour, as children of the ears). The clips address them by name.
- The same frame: facing +X, Z up, the feet's soles on z = -1.27 like the old ones, exported with the same
  FBX settings (forward Y, up Z, no Root node), so Unity reads the transforms the same way.
- The ears keep the old ears' rest rotation and scale (the left one mirrored) and the tail its rest
  rotation, because the clips key their rotations absolutely.
- The feet's pivots sit exactly where the Jump clip keys them (the old feet were moved in the prefab), so
  their hop moves the egg's feet from their own rest.
- One material slot, "EggBunny": colour comes from UV0 into egg_bunny_tex.png's four strips and the face
  from UV1 (FaceMap), exactly as the Regular Bunny's "Toon Test With UV2 Decal" material expects.

The Regular Bunny's Blender actions are copied into egg_bunny.blend, each channel moved from the old
part's rest to the new one's, so the .blend animates the same way Unity does.
"""
import bpy, bmesh, math, os, sys
from mathutils import Matrix, Vector, Euler

argv = sys.argv[sys.argv.index("--") + 1:]
OLD_BLEND, OUT_DIR, OUT_BLEND, OUT_TEX = argv[:4]
HERE = os.path.dirname(os.path.abspath(__file__))
BUILD = os.path.join(HERE, "build_bunny.py")

# ---- build the egg, unfilleted (the fillet is computed in the final frame) -----------------------------
sys.argv = ["x", "--", "variant=egg", "fillet=0"]
src = open(BUILD).read()
MARK = "for name, (az, el, scale) in VIEWS.items():"
g = {"__file__": BUILD, "__name__": "build_bunny"}
exec(compile(src[:src.index(MARK)], BUILD, "exec"), g)
bpy.context.view_layer.update()

VOXEL_TO_GAME = 1.5             # the preview is 0.1 m a voxel; the game model 0.15, about the old body's size
GROUND = -1.27                  # the old feet's soles
G = Matrix.Translation((0, 0, GROUND)) @ Matrix.Scale(VOXEL_TO_GAME, 4) @ Matrix.Rotation(math.radians(90), 4, "Z")

# Where the Regular Bunny's Jump clip keys each foot (Unity local = 0.5 * (x, z, y) of Blender), in Blender.
FEET = {"foot_back_left": (-0.5130354, 0.4373388), "foot_back_right": (-0.5130354, -0.437339),
        "foot_front_left": (0.5556848, 0.4373388), "foot_front_right": (0.5556848, -0.437339)}
FOOT_Z = -0.9361728

def side(p):
    return "left" if p.y > 0 else "right"

def reframe(src_ob, name, node, parent):
    """A new object `name` with world matrix `node` whose mesh is src_ob's, unchanged in the game frame."""
    w = G @ src_ob.matrix_world
    me = src_ob.data.copy()
    m = node.inverted() @ w
    me.transform(m)
    if m.determinant() < 0:                   # a mirrored frame turns the faces inside out
        me.flip_normals()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    ob.parent = parent
    ob.matrix_world = node
    return ob

src_parts = [o for o in bpy.data.objects if o.type == "MESH" and o.name != "ground"]
root = bpy.data.objects.new("Root", None)
bpy.context.scene.collection.objects.link(root)
made = {}
for o in src_parts:
    if o.name.startswith("Plate"):
        continue
    origin = G @ o.matrix_world.translation
    if o.name == "Body":
        node, final = Matrix.Translation((0.0862, 0, -0.1118)), "Body"
    elif o.name.startswith("Ear"):
        s = side(origin)
        final = "ear_" + s
        rot = Euler((0, math.pi, 0)).to_matrix().to_4x4() @ Matrix.Diagonal((-1, -1, -1, 1)) if s == "left" \
            else Matrix.Identity(4)                                       # the old ears' rest frames
        node = Matrix.Translation(origin) @ rot
    elif o.name.startswith("Foot"):
        final = "foot_" + ("front" if origin.x > 0 else "back") + "_" + side(origin)
        x, y = FEET[final]
        node = Matrix.Translation((x, y, FOOT_Z))
    elif o.name == "Tail":
        node, final = Matrix.Translation(origin), "tail"
    else:
        raise RuntimeError(o.name)
    tmp = ("Ear_" if final.startswith("ear") else "Game_") + final       # "Ear*" picks fillet_all's round-tip path
    made[final] = reframe(o, tmp, node, root)
for o in src_parts:
    if o.name.startswith("Plate"):
        ear = made["ear_" + side(G @ o.matrix_world.translation)]
        made[ear.name.replace("Ear_", "") + "_patch"] = reframe(o, "Plate_" + ear.name, ear.matrix_world.copy(), ear)
for o in src_parts + [g["root"], g["ground"], g["sun"], g["cam"]]:
    bpy.data.objects.remove(o)

g["fillet_all"]()                              # in the final frames: custom normals stay true
for final, ob in made.items():
    ob.name = final
    ob.data.name = final

# ---- one material: UV0 into the colour strips, UV1 the face decal ----------------------------------------
STRIPS = [g["PALETTE"][k] for k in (g["COAT"], g["FACE"], g["WHITE"], g["DARK"])]   # u 0-.25, .25-.5, ...
img = bpy.data.images.new("egg_bunny_tex", 512, 512, alpha=False)
px = []
for _y in range(512):
    for x in range(512):
        h = STRIPS[x * 4 // 512]
        px += [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)] + [1.0]
img.pixels = px
img.filepath_raw = OUT_TEX
img.file_format = "PNG"
img.save()

mat = bpy.data.materials.new("EggBunny")
nt = mat.node_tree
bsdf = nt.nodes["Principled BSDF"]
bsdf.inputs["Roughness"].default_value = 0.9
tex = nt.nodes.new("ShaderNodeTexImage"); tex.image = img
uv0 = nt.nodes.new("ShaderNodeUVMap"); uv0.uv_map = "UVMap"
face = nt.nodes.new("ShaderNodeTexImage"); face.extension = "CLIP"
face.image = bpy.data.images.load(os.path.abspath(g["FACE_TEX"]), check_existing=True)
uv1 = nt.nodes.new("ShaderNodeUVMap"); uv1.uv_map = "FaceMap"
off = nt.nodes.new("ShaderNodeVectorMath"); off.operation = "ADD"; off.inputs[1].default_value = (0, -0.05, 0)
mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = "RGBA"
nt.links.new(uv0.outputs["UV"], tex.inputs["Vector"])
nt.links.new(uv1.outputs["UV"], off.inputs[0]); nt.links.new(off.outputs["Vector"], face.inputs["Vector"])
nt.links.new(tex.outputs["Color"], mix.inputs["A"]); nt.links.new(face.outputs["Color"], mix.inputs["B"])
nt.links.new(face.outputs["Alpha"], mix.inputs["Factor"]); nt.links.new(mix.outputs["Result"], bsdf.inputs["Base Color"])
for ob in made.values():
    ob.data.materials.clear()
    ob.data.materials.append(mat)
    for p in ob.data.polygons:
        p.material_index = 0

# ---- the Regular Bunny's actions, moved onto the new parts' rests ----------------------------------------
with bpy.data.libraries.load(OLD_BLEND, link=False) as (src_lib, dst_lib):
    dst_lib.actions = list(src_lib.actions)

def fcurves(act):
    return [fc for layer in act.layers for strip in layer.strips for bag in strip.channelbags for fc in bag.fcurves]

def owner(act):
    return act.name.split("_", 1)[1]                                      # Idle_ear_left -> ear_left

new_rest = {"Root": (Vector((0, 0, 0)), Vector((0, 0, 0)), Vector((1, 1, 1)))}
for name, ob in made.items():
    if not name.endswith("patch"):
        loc, rotq, sca = ob.matrix_world.decompose()
        e = Euler((0, math.pi, 0)) if name == "ear_left" else Euler((0, 0, 0))
        sca = Vector((-1, -1, -1)) if name == "ear_left" else sca
        new_rest[name] = (loc, Vector(e), sca)
old_rest = {}
for act in bpy.data.actions:                                              # the old rest: Idle's first key, else the action's
    who = owner(act)
    if who in old_rest and not act.name.startswith("Idle"):
        continue
    vals = {}
    for fc in fcurves(act):
        vals[(fc.data_path, fc.array_index)] = fc.keyframe_points[0].co[1]
    old_rest[who] = tuple(Vector([vals[(p, i)] for i in range(3)]) for p in ("location", "rotation_euler", "scale"))

for act in bpy.data.actions:
    who = owner(act)
    o_loc, o_rot, o_sca = old_rest[who]
    n_loc, n_rot, n_sca = new_rest[who]
    for fc in fcurves(act):
        i = fc.array_index
        if fc.data_path == "scale":
            f = lambda v: n_sca[i] * v / o_sca[i]
        else:
            old, new = (o_loc, n_loc) if fc.data_path == "location" else (o_rot, n_rot)
            f = lambda v, d=new[i] - old[i]: v + d
        for k in fc.keyframe_points:
            k.co[1], k.handle_left[1], k.handle_right[1] = f(k.co[1]), f(k.handle_left[1]), f(k.handle_right[1])
        fc.update()
    act.use_fake_user = True

for name in ("Root", "ear_left", "ear_right", "tail"):                     # Idle plays, as in regular_bunny.blend
    ob = root if name == "Root" else made[name]
    ad = ob.animation_data_create()
    act = bpy.data.actions["Idle_" + name]
    ad.action = act
    if act.slots and ad.action_slot is None:
        ad.action_slot = act.slots[0]

sc = bpy.context.scene
sc.render.fps = 24
sc.frame_start, sc.frame_end = 1, 49
sc.frame_set(1)

# ---- export like regular_bunny.fbx: meshes only, forward Y, up Z, no baked animation ---------------------
bpy.ops.object.select_all(action="DESELECT")
for ob in made.values():
    ob.select_set(True)
fbx = os.path.join(OUT_DIR, "egg_bunny.fbx")
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, bake_anim=False, mesh_smooth_type="OFF",
                         axis_forward="Y", axis_up="Z")

for im in bpy.data.images:                                                  # paths relative to the .blend
    if im.filepath:
        im.filepath = bpy.path.relpath(os.path.abspath(bpy.path.abspath(im.filepath)), start=os.path.dirname(os.path.abspath(OUT_BLEND)))
bpy.data.orphans_purge(do_recursive=True)
bpy.ops.wm.save_as_mainfile(filepath=OUT_BLEND, compress=True)
print("EGG_BUNNY", fbx, OUT_BLEND, OUT_TEX, sorted(made))
