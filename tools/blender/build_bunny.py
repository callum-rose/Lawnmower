"""Builds the Regular Bunny mower in the VoxelStyle.md style and renders preview views.

blender --background --factory-startup --python build_bunny.py -- [variant=classic|toy|lop|sitting|loplong] [out=<prefix>]

(or, with Blender's `bpy` module installed from pip: python build_bunny.py -- variant=... out=...)

Four takes on the same character, all from Assets/Models/Mower/Blender/regular_bunny.blend as the
reference: an orange egg of a body with a butter face panel carrying the game's own face texture, two
tall ears with butter insides, a white tail and four dark feet.

- classic: the reference's egg, heavily bevelled, ears upright and splayed.
- toy:     a box with short bevels only (the Taxi's lesson, R7a), chunky upright ears.
- lop:     the classic body with ears that flop down its sides.
- sitting: a separate head on a low body, so the head can nod and turn; ears swept back.
- loplong: the reference's profile on a cube: a big bevel down the back, a separate head slab at the
           front a voxel taller than the body, and upright ears.

The script follows the bake_shade.py contract: parts are authored in voxels under an empty called
`root`, the root part is `Body`, there is a shadow-catching `ground`, and it ends in the standard
render loop, so

blender --background --factory-startup --python bake_shade.py -- build_bunny.py <fbx_out> Body variant=<v>

bakes and exports the same model. Voxel coordinates are Unity's (x right, y up, z forward); `B` maps
them into Blender (front faces -Y). Colours are VoxelStyle.md palette indices.
"""
import bpy, bmesh, math, sys
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
kv = dict(a.split("=", 1) for a in argv if "=" in a)
VARIANT = kv.get("variant", "classic")
OUT = kv.get("out", "/tmp/bunny_" + VARIANT)
assert VARIANT in ("classic", "toy", "lop", "sitting", "loplong"), VARIANT

# The reference's coral, gold, white and dark red. The face is not voxels: it is the reference's own
# face texture, a decal on a second UV channel (see FACE_TEX below).
COAT, FACE, WHITE, DARK = "P44", "P41", "P00", "P51"
PALETTE = {"P00": "f6f6f2", "P41": "ffd070", "P44": "f08236", "P51": "b4521e"}

# ---- the face decal ----------------------------------------------------------------------------------
# The game's bunny material ("Toon Test With UV2 Decal") draws _MainTex on UV0 and alpha-blends
# _DecalTex over it on UV1, and FaceAnimationComponent swaps the decal between the bunny_face textures.
# So the mesh carries the same two channels as regular_bunny.blend:
# - UVMap (UV0): every face at the centre of its colour's strip in bunny_tex.png;
# - FaceMap (UV1): a planar projection of a FACE_SIZE-voxel square onto the face's front plane, with
#   every other face parked outside 0..1, where the clamped decal is transparent.
# The material's decal offset (0, -0.05) is applied in the preview too, so it matches the game.
import os
HERE = os.path.dirname(os.path.abspath(__file__)) if "__file__" in globals() else os.getcwd()
FACE_TEX = kv.get("face", os.path.join(HERE, "../../Assets/Textures/Game/Mowers/bunny_face.png"))
DECAL_OFFSET = (0.0, -0.05)
EYE_V = 0.426                                   # bunny_face.png: the eyes' centre, from the bottom
STRIP_U = {COAT: 0.125, FACE: 0.375, WHITE: 0.625, DARK: 0.875}

# ---- scene and materials -----------------------------------------------------------------------------
for o in list(bpy.data.objects):
    bpy.data.objects.remove(o)
sc = bpy.context.scene
world = bpy.data.worlds.new("world")
sc.world = world

def lin(h):
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c] + [1]

MATS = {}
def mat(key):
    if key not in MATS:
        m = bpy.data.materials.new("Palette_" + PALETTE[key])
        m.use_nodes = True
        b = m.node_tree.nodes["Principled BSDF"]
        b.inputs["Base Color"].default_value = lin(PALETTE[key])
        b.inputs["Roughness"].default_value = 0.9
        nt = m.node_tree
        uv = nt.nodes.new("ShaderNodeUVMap"); uv.uv_map = "FaceMap"
        off = nt.nodes.new("ShaderNodeVectorMath"); off.operation = "ADD"
        off.inputs[1].default_value = (DECAL_OFFSET[0], DECAL_OFFSET[1], 0)
        tex = nt.nodes.new("ShaderNodeTexImage"); tex.extension = "CLIP"
        tex.image = bpy.data.images.load(os.path.abspath(FACE_TEX), check_existing=True)
        mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = "RGBA"
        mix.inputs["A"].default_value = lin(PALETTE[key])
        nt.links.new(uv.outputs["UV"], off.inputs[0]); nt.links.new(off.outputs["Vector"], tex.inputs["Vector"])
        nt.links.new(tex.outputs["Alpha"], mix.inputs["Factor"]); nt.links.new(tex.outputs["Color"], mix.inputs["B"])
        nt.links.new(mix.outputs["Result"], b.inputs["Base Color"])
        MATS[key] = m
    return list(MATS).index(key)

def B(p):
    return (p[0], -p[2], p[1])

# ---- parts: voxels on the half-voxel lattice, cut by 45 deg bevel planes ------------------------------
EPS = 1e-6
def _dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def _area(poly):
    n = Vector((0, 0, 0))
    for i in range(len(poly)):
        n += Vector(poly[i]).cross(Vector(poly[(i + 1) % len(poly)]))
    return n.length / 2

def clip_voxel(k, planes):
    """Clip the unit voxel at min corner k by half-spaces n.p <= d (R7b bevels). None if no plane cuts
    it, [] if nothing is left, else [(polygon, tag)]: tag (axis, sign) for what remains of a cube face."""
    lo, hi = k, [c + 1 for c in k]
    corners = [(a, b, c) for a in (lo[0], hi[0]) for b in (lo[1], hi[1]) for c in (lo[2], hi[2])]
    act = [(n, d) for n, d in planes if any(_dot(n, p) > d + EPS for p in corners)]
    if not act:
        return None
    faces = []
    for ax in range(3):
        u, v = [a for a in range(3) if a != ax]
        for sg in (1, -1):
            pts = []
            for a, b in ((0, 0), (1, 0), (1, 1), (0, 1)):
                p = [0, 0, 0]
                p[ax] = hi[ax] if sg > 0 else lo[ax]; p[u] = (lo[u], hi[u])[a]; p[v] = (lo[v], hi[v])[b]
                pts.append(tuple(p))
            faces.append((pts if sg > 0 else pts[::-1], (ax, sg)))
    for n, d in act:
        out, cap = [], []
        for poly, tag in faces:
            res = []
            for i in range(len(poly)):
                p, q = poly[i], poly[(i + 1) % len(poly)]
                dp, dq = _dot(n, p) - d, _dot(n, q) - d
                if dp <= EPS:
                    res.append(p)
                if (dp < -EPS and dq > EPS) or (dp > EPS and dq < -EPS):
                    t = dp / (dp - dq)
                    res.append(tuple(p[j] + t * (q[j] - p[j]) for j in range(3)))
            cap += [p for p in res if abs(_dot(n, p) - d) <= 1e-5]
            if len(res) >= 3 and _area(res) > EPS:
                out.append((res, tag))
        uniq = []
        for p in cap:
            if all(sum((p[j] - q[j]) ** 2 for j in range(3)) > 1e-10 for q in uniq):
                uniq.append(p)
        if len(uniq) >= 3:
            c = sum((Vector(p) for p in uniq), Vector()) / len(uniq)
            e1 = (Vector(uniq[0]) - c).normalized(); e2 = Vector(n).normalized().cross(e1)
            uniq.sort(key=lambda p: math.atan2((Vector(p) - c).dot(e2), (Vector(p) - c).dot(e1)))
            if _area(uniq) > EPS:
                out.append((uniq, None))
        faces = out
    return faces

class Part:
    """A rigid block of voxels on one grid (R4), keyed by min corner on the half-voxel lattice (R5)."""
    def __init__(s, name):
        s.name, s.vox, s.planes = name, {}, []
    def box(s, x0, x1, y0, y1, z0, z1, col):
        for i in range(round(x1 - x0)):
            for j in range(round(y1 - y0)):
                for k in range(round(z1 - z0)):
                    s.vox[(x0 + i, y0 + j, z0 + k)] = col
    def paint(s, pred, col):
        for k in s.vox:
            if pred(*k):
                s.vox[k] = col
    def bevel(s, n, d, where=None):
        """A 45 deg bevel as the half-space n.p <= d, applied to voxels whose min corner passes `where`."""
        s.planes.append((n, d, where))
    def build(s, pivot=(0, 0, 0)):
        bm = bmesh.new()
        px, py, pz = pivot
        def face(pts, col):
            vs = [bm.verts.new(B((p[0] - px, p[1] - py, p[2] - pz))) for p in pts]
            for i in range(1, len(vs) - 1) if len(vs) > 4 else [None]:   # quads and triangles only
                f = bm.faces.new(vs if i is None else [vs[0], vs[i], vs[i + 1]])
                f.material_index = mat(col)
        clipped = {}
        for k in list(s.vox):
            pl = [(n, d) for n, d, w in s.planes if w is None or w(*k)]
            faces = clip_voxel(k, pl) if pl else None
            if faces == []:
                del s.vox[k]
            elif faces:
                clipped[k] = faces
        dirs = [((1, 0, 0), 0), ((-1, 0, 0), 0), ((0, 1, 0), 1), ((0, -1, 0), 1), ((0, 0, 1), 2), ((0, 0, -1), 2)]
        for k, col in s.vox.items():
            if k in clipped:
                for poly, tag in clipped[k]:
                    if tag is not None:
                        nb = list(k); nb[tag[0]] += tag[1]
                        if tuple(nb) in s.vox:
                            continue
                    face(poly, col)
                continue
            for d, ax in dirs:
                if (k[0] + d[0], k[1] + d[1], k[2] + d[2]) in s.vox:
                    continue
                lo, hi = list(k), [c + 1 for c in k]
                u, v = [a for a in range(3) if a != ax]
                pts = []
                for a, b in ((0, 0), (1, 0), (1, 1), (0, 1)):
                    p = [0, 0, 0]
                    p[ax] = hi[ax] if sum(d) > 0 else lo[ax]; p[u] = (lo[u], hi[u])[a]; p[v] = (lo[v], hi[v])[b]
                    pts.append(p)
                face(pts if sum(d) > 0 else pts[::-1], col)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        me = bpy.data.meshes.new(s.name)
        bm.to_mesh(me)
        for m in MATS.values():
            me.materials.append(m)
        ob = bpy.data.objects.new(s.name, me)
        sc.collection.objects.link(ob)
        ob.location = B(pivot)
        ob.parent = root
        return ob

root = bpy.data.objects.new("root", None)
sc.collection.objects.link(root)


# ---- helpers -----------------------------------------------------------------------------------------
def bevel_edge(part, box, n, k, where=None):
    """R7b: bevel the box edge facing n (two non-zero components of +-1) by a k x k wedge."""
    x0, x1, y0, y1, z0, z1 = box
    far = [(x1 if n[0] > 0 else x0), (y1 if n[1] > 0 else y0), (z1 if n[2] > 0 else z0)]
    part.bevel(n, sum(n[i] * far[i] for i in range(3)) - k, where)

def round_box(part, box, top=0, side=0, bottom=0, front_back=True, where=None):
    """Bevel a box's top edges by `top`, its vertical edges by `side`, its bottom edges by `bottom`."""
    for s in (1, -1):
        if top:
            bevel_edge(part, box, (s, 1, 0), top, where)
            if front_back:
                bevel_edge(part, box, (0, 1, s), top, where)
        if bottom:
            bevel_edge(part, box, (s, -1, 0), bottom, where)
            if front_back:
                bevel_edge(part, box, (0, -1, s), bottom, where)
        if side:
            for t in (1, -1):
                bevel_edge(part, box, (s, 0, t), side, where)

def inside(box):
    x0, x1, y0, y1, z0, z1 = box
    return lambda x, y, z: x0 <= x < x1 and y0 <= y < y1 and z0 <= z < z1

def face_uvs(ob, front_z, eye_y, size):
    """UV0 into bunny_tex's colour strips; UV1 projects the face texture onto the plane z = front_z, a
    size x size square centred on x = 0 with the texture's eyes at height eye_y (after the decal offset)."""
    me = ob.data
    cols = list(MATS)
    uv0, uv1 = me.uv_layers.new(name="UVMap"), me.uv_layers.new(name="FaceMap")
    y0 = eye_y - (EYE_V - DECAL_OFFSET[1]) * size
    px, py, pz = ob.location.x, ob.location.z, -ob.location.y      # the part's pivot, back in voxels
    for f in me.polygons:
        n = f.normal
        on_front = front_z is not None and n.y < -0.99 and all(abs(-me.vertices[me.loops[l].vertex_index].co.y + pz - front_z) < 1e-4
                                       for l in f.loop_indices)
        for l in f.loop_indices:
            uv0.data[l].uv = (STRIP_U[cols[f.material_index]], 0.5)
            co = me.vertices[me.loops[l].vertex_index].co
            x, y = co.x + px, co.z + py
            uv1.data[l].uv = ((x + size / 2) / size, (y - y0) / size) if on_front else (-1.0, -1.0)
    me.uv_layers.active = uv0

def rot(ob, pitch=0, splay=0, yaw=0):
    """Rest rotation in degrees, 5 deg steps (R6). pitch > 0 leans the top back, splay > 0 leans it
    towards +x, yaw > 0 turns the front towards +x."""
    for a in (pitch, splay, yaw):
        assert a % 5 == 0, a
    ob.rotation_euler = (math.radians(-pitch), math.radians(splay), math.radians(yaw))

def ear(name, sx, x_c, base_y, z0, w, d, h, bury, pitch, splay, pivot_y=None, tip=1):
    """An ear: w x d x h with `bury` rows inside its parent, butter on the inner (front) face."""
    e = Part(name)
    x0, x1 = x_c - w / 2, x_c + w / 2
    y0, y1 = base_y - bury, base_y + h - bury
    e.box(x0, x1, y0, y1, z0, z0 + d, COAT)
    e.paint(lambda x, y, z: z == z0 + d - 1 and base_y + 1 <= y < y1 - 2 and x0 + 1 <= x < x1 - 1, FACE)
    if tip:
        for s in (1, -1):
            bevel_edge(e, (x0, x1, y0, y1, z0, z0 + d), (s, 1, 0), tip)
    ob = e.build(pivot=(x_c, base_y if pivot_y is None else pivot_y, z0 + d / 2))
    rot(ob, pitch=pitch, splay=sx * splay)
    return ob

def tail(y_c, back, size=4, proud=2.5):
    t = Part("Tail")
    t.box(-size / 2, size / 2, y_c - size / 2, y_c + size / 2, back - proud, back - proud + size, WHITE)
    t.build(pivot=(0, y_c, back))

def feet(xs, zs, w, l, h, bury, body_bottom):
    for i, x_c in enumerate(xs):
        for j, z_c in enumerate(zs):
            f = Part("Foot" + ("R" if x_c > 0 else "L") + ("F" if z_c > 0 else "B"))
            f.box(x_c - w / 2, x_c + w / 2, 0, body_bottom + bury, z_c - l / 2, z_c + l / 2, DARK)
            f.build(pivot=(x_c, body_bottom, z_c))

# ---- the variants ------------------------------------------------------------------------------------
if VARIANT in ("classic", "lop"):
    # 12 x 14 x 13 egg on 2-voxel feet: the reference's 1.7 : 2.1 : 2.0 width : length : height.
    box = (-6, 6, 2, 15, -7, 7)
    body = Part("Body")
    body.box(*box, COAT)
    round_box(body, box, top=3, side=2, bottom=2)
    body.paint(lambda x, y, z: z == 6 and -4 <= x < 4 and 4 <= y < 12, FACE)    # the flat front, bevels stay coat
    face_uvs(body.build(), 7, 9.5, 11)
    if VARIANT == "classic":
        for sx in (1, -1):
            ear("Ear" + ("R" if sx > 0 else "L"), sx, sx * 2.5, 15, -2, 3, 2, 11, 2, pitch=10, splay=15)
    else:
        for sx in (1, -1):   # pivot on the top bevel, flopped out and down past the cheeks
            ear("Ear" + ("R" if sx > 0 else "L"), sx, sx * 4, 14, -2, 3, 2, 10, 2, pitch=5, splay=135)
    tail(9, -7)
    feet((-3, 3), (-4, 4), 3, 4, 3, 1, 2)
    HEIGHT = 26 if VARIANT == "classic" else 16

elif VARIANT == "toy":
    # A box with short bevels only: the silhouette stays a stubby toy (Taxi taste call).
    box = (-6, 6, 2, 14, -7, 7)
    body = Part("Body")
    body.box(*box, COAT)
    round_box(body, box, top=2, side=1, front_back=True)
    body.paint(lambda x, y, z: z == 6 and -4 <= x < 4 and 3 <= y < 12, FACE)    # an inset panel with a coat rim
    face_uvs(body.build(), 7, 9, 11)
    for sx in (1, -1):
        ear("Ear" + ("R" if sx > 0 else "L"), sx, sx * 3, 14, -3, 4, 2, 10, 2, pitch=0, splay=0, tip=0)
    tail(8, -7, size=4, proud=2.5)
    feet((-3.5, 3.5), (-4, 4), 3, 5, 3, 1, 2)
    HEIGHT = 22

elif VARIANT == "loplong":
    # The reference's profile on a cube: a tall front and a back sloping away through one big 45 deg
    # bevel (a 7 x 7 wedge, R7b) along the top rear edge; every other edge bevelled by one voxel. The
    # face is a separate head slab at the front, a voxel taller and narrower than the body (R8), so it
    # can nod and turn; the ears ride on it, upright.
    box = (-6, 6, 2, 14, -7, 7)
    body = Part("Body")
    body.box(*box, COAT)
    round_box(body, box, top=1, side=1, bottom=1, front_back=False)
    bevel_edge(body, box, (0, 1, -1), 7)                                         # the sloping back
    bevel_edge(body, box, (0, -1, -1), 1); bevel_edge(body, box, (0, -1, 1), 1)
    body.build()
    hbox = (-5.5, 5.5, 4, 15, 1.5, 8.5)
    head = Part("Head")
    head.box(*hbox, COAT)
    round_box(head, hbox, top=1, side=1, bottom=1)
    head.paint(lambda x, y, z: z == 7.5 and -4.5 <= x < 4.5 and 5 <= y < 14, FACE)
    head_ob = head.build(pivot=(0, 10, 3))                                       # neck, inside the body
    face_uvs(head_ob, 8.5, 11, 11)
    for sx in (1, -1):                                                           # upright ears on the head
        e = ear("Ear" + ("R" if sx > 0 else "L"), sx, sx * 2.5, 15, 2.5, 3, 2, 11, 2, pitch=10, splay=15)
        e.parent = head_ob
        e.location = Vector(e.location) - Vector(head_ob.location)
    tail(5, -7)
    feet((-3, 3), (-4, 4), 3, 4, 3, 1, 2)
    HEIGHT = 26

else:  # sitting
    # A low body and a separate head that can nod: the classic proportions split at the neck.
    box = (-6, 6, 2, 11, -7, 7)
    body = Part("Body")
    body.box(*box, COAT)
    round_box(body, box, top=2, side=2, bottom=1)
    body.paint(lambda x, y, z: y < 6 and z >= 4 and -3 <= x < 3, FACE)          # a butter chest bib
    body.build()
    hbox = (-5.5, 5.5, 9, 19, -1.5, 8.5)
    head = Part("Head")
    head.box(*hbox, COAT)
    round_box(head, hbox, top=2, side=2, bottom=1)
    head.paint(lambda x, y, z: z == 7.5 and -3.5 <= x < 3.5 and 10 <= y < 17, FACE)
    head_ob = head.build(pivot=(0, 11, 3))                                       # neck, on the body's top
    face_uvs(head_ob, 8.5, 14.5, 10)
    for sx in (1, -1):                                                           # the ears ride on the head
        e = ear("Ear" + ("R" if sx > 0 else "L"), sx, sx * 2.5, 19, 0.5, 3, 2, 11, 2, pitch=25, splay=10)
        e.parent = head_ob
        e.location = Vector(e.location) - Vector(head_ob.location)
    tail(6, -7)
    feet((-3, 3), (-4, 4), 3, 4, 3, 1, 2)
    HEIGHT = 29

for o in list(bpy.data.objects):                     # every other part: colour strips, no face
    if o.type == "MESH" and not o.data.uv_layers:
        face_uvs(o, None, 0, 1)

# ---- preview: ground, light, orthographic camera ------------------------------------------------------
root.scale = (0.1, 0.1, 0.1)                                                      # R1: authored in voxels
BG = lin("e3e6eb")
sc.render.engine = "CYCLES"
sc.cycles.samples = int(__import__("os").environ.get("SAMPLES", "96"))
sc.cycles.use_denoising = True
sc.view_settings.view_transform = "Standard"
sc.render.resolution_x = sc.render.resolution_y = 600
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = BG
world.node_tree.nodes["Background"].inputs[1].default_value = 0.75
gme = bpy.data.meshes.new("ground")
gme.from_pydata([(-20, -20, 0), (20, -20, 0), (20, 20, 0), (-20, 20, 0)], [], [(0, 1, 2, 3)])
ground = bpy.data.objects.new("ground", gme)
sc.collection.objects.link(ground)
gm = bpy.data.materials.new("ground")
gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = BG
gme.materials.append(gm)
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
sun.data.energy = 2.6
sun.data.angle = math.radians(12)
sun.rotation_euler = (math.radians(40), 0, math.radians(-35))
sc.collection.objects.link(sun)
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
cam.data.type = "ORTHO"
sc.collection.objects.link(cam)
sc.camera = cam
target = Vector((0, 0, HEIGHT * 0.05))
S = HEIGHT * 0.12 + 0.8
VIEWS = {"three": (-45, 30, S), "front": (0, 10, S), "side": (-90, 10, S), "back": (135, 20, S)}

for name, (az, el, scale) in VIEWS.items():
    a, e = math.radians(az), math.radians(el)
    d = Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
    cam.location = target + d * 10
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = scale
    sc.render.filepath = f"{OUT}_{name}.png"
    bpy.ops.render.render(write_still=True)
