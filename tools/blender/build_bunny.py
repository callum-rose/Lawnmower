"""Builds the Regular Bunny mower in the VoxelStyle.md style and renders preview views.

blender --background --factory-startup --python build_bunny.py -- [variant=classic|toy|lop|sitting|cube]
    [colours=original|snow|midnight|forest|grape] [pattern=dutch|spots|stripes|harlequin]
    [soften=1..4] [rounded=1] [out=<prefix>]

(or, with Blender's `bpy` module installed from pip: python build_bunny.py -- variant=... out=...)

Four takes on the same character, all from Assets/Models/Mower/Blender/regular_bunny.blend as the
reference: an orange egg of a body with a butter face panel carrying the game's own face texture, two
tall ears with butter insides, a white tail and four dark feet.

- classic: the reference's egg, heavily bevelled, ears upright and splayed.
- toy:     a box with short bevels only (the Taxi's lesson, R7a), chunky upright ears.
- lop:     the classic body with ears that flop down its sides.
- sitting: a separate head on a low body, so the head can nod and turn; ears swept back.
- cube:    the reference's profile on a cube: one body part with a big bevel down the back and
           2-voxel bevels elsewhere, thick upright ears.

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
assert VARIANT in ("classic", "toy", "lop", "sitting", "cube"), VARIANT

# The reference's coral, gold, white and dark red. The face is not voxels: it is the reference's own
# face texture, a decal on a second UV channel (see FACE_TEX below).
# colours=<name> swaps the four roles (coat, face panel and inner ears, tail, feet) for a colourway.
COLOURWAYS = {
    "original": ("P44", "P41", "P00", "P51"),   # Orange, Butter, White, Rust
    "snow":     ("P00", "P53", "P46", "P03"),   # White, Blush, Pink, Grey
    "midnight": ("P40", "P37", "P00", "P56"),   # Deep blue, Sky, White, Navy
    "forest":   ("P21", "P59", "P09", "P23"),   # Grass, Mint, Cream, Pine
    "grape":    ("P47", "P55", "P41", "P48"),   # Purple, Lavender, Butter, Plum
}
# pattern=<name> (cube only) adds markings (R12) in a MARK colour, with its own colourway.
PATTERNS = {   # coat, face, tail, feet, mark
    "plain":     None,
    "dutch":     ("P00", "P01", "P00", "P05", "P05"),   # White front, Charcoal rear and ears
    "spots":     ("P09", "P10", "P00", "P14", "P14"),   # Cream with Brown patches
    "stripes":   ("P44", "P09", "P00", "P06", "P06"),   # Orange tiger, Graphite stripes, Cream face
    "harlequin": ("P43", "P09", "P00", "P05", "P05"),   # Amber and Charcoal halves, swapped on the ears
}
PATTERN = kv.get("pattern", "plain")
SOFT = int(kv.get("soften", "0"))
ROUNDED = kv.get("rounded", "0") == "1"   # cube: fillet every edge; colour patches as round-cornered plates
FILLET = 0.35                             # voxels: the fillet's width
PLATE_PROUD = 0.05                        # voxels: how far a colour plate stands off its face
COLOURS = kv.get("colours", "original")
COAT, FACE, WHITE, DARK = COLOURWAYS[COLOURS]
MARK = None
if PATTERNS[PATTERN]:
    COAT, FACE, WHITE, DARK, MARK = PATTERNS[PATTERN]
PALETTE = {"P00": "f6f6f2", "P03": "8a8b90", "P09": "ece0c0", "P21": "5a9440", "P23": "23452a", "P37": "8ab4d8",
           "P40": "3a4d8c", "P41": "ffd070", "P44": "f08236", "P46": "e8899a", "P47": "b868c0", "P48": "7a3e86",
           "P51": "b4521e", "P53": "f6c4cc", "P55": "d4b0e0", "P56": "23305a", "P59": "b8e6b0",
           "P01": "dcdcd8", "P05": "4a4a50", "P06": "2b2b30", "P10": "dcc08e", "P14": "6e3d22", "P43": "eea62a"}

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

def round_edge(part, box, axes, signs, r, facets, where=None):
    """Soften the box edge between faces axes[0] (outward sign signs[0]) and axes[1] by `facets` flat
    planes through points on a quarter circle of radius r tangent to both faces. facets=1 is the R7b
    45 deg bevel of depth r; more facets go beyond the style's 45 deg wedges to read as a rounded edge."""
    far = [(box[2 * a + 1] if sg > 0 else box[2 * a]) for a, sg in zip(axes, signs)]
    pts = [(-r + r * math.sin(t), -r + r * math.cos(t)) for t in
           (math.pi / 2 * i / facets for i in range(facets + 1))]           # (0,-r) ... (-r,0) in (a, b)
    for (pa, pb), (qa, qb) in zip(pts, pts[1:]):
        na, nb = (qb - pb), -(qa - pa)                                       # outward normal of the chord
        if na * 1 + nb * 1 < 0:
            na, nb = -na, -nb
        n = [0, 0, 0]
        n[axes[0]], n[axes[1]] = na * signs[0], nb * signs[1]
        d = na * pa + nb * pb + n[axes[0]] * far[0] + n[axes[1]] * far[1]
        part.bevel(tuple(n), d, where)

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
            uv0.data[l].uv = (STRIP_U.get(cols[f.material_index], 0.125), 0.5)   # markings: no strip
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

def plate(name, parent, pivot, x0, x1, y0, y1, z, radius, col, segments=4):
    """A flat round-cornered rectangle of colour on the plane z (a front face), a hair proud of it, in the
    parent's own space. Painted voxels can only make square patches; this rounds their corners."""
    px, py, pz = pivot
    pts = []
    for cx, cy, a0 in ((x1 - radius, y0 + radius, -90), (x1 - radius, y1 - radius, 0),
                       (x0 + radius, y1 - radius, 90), (x0 + radius, y0 + radius, 180)):
        for i in range(segments + 1):
            a = math.radians(a0 + 90 * i / segments)
            pts.append((cx + radius * math.cos(a), cy + radius * math.sin(a)))
    me = bpy.data.meshes.new(name)
    me.from_pydata([B((x - px, y - py, z + PLATE_PROUD - pz)) for x, y in pts], [], [list(range(len(pts)))])
    idx = mat(col)
    for m in MATS.values():
        me.materials.append(m)
    me.polygons[0].material_index = idx
    if me.polygons[0].normal.y > 0:                                              # face the front (-Y in Blender)
        me.flip_normals()
    ob = bpy.data.objects.new(name, me)
    sc.collection.objects.link(ob)
    ob.parent = parent
    return ob

def fillet_all(width=FILLET):
    """Round every edge of every part (not the plates) with a three-segment bevel, smooth-shaded across
    the bevel, with hardened normals so the flat faces stay flat."""
    dg = bpy.context.evaluated_depsgraph_get()
    for o in [o for o in bpy.data.objects if o.type == "MESH" and not o.name.startswith("Plate")]:
        o.data.shade_smooth()
        m = o.modifiers.new("fillet", "BEVEL")
        m.width, m.segments, m.limit_method, m.angle_limit = width, 3, "ANGLE", math.radians(20)
        m.harden_normals = True                       # flat faces keep their own normal; only the fillet curves
        dg.update()
        me = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
        o.modifiers.clear()
        o.data = me

def ear(name, sx, x_c, base_y, z0, w, d, h, bury, pitch, splay, pivot_y=None, tip=1, col=None, tip_col=None, tip_rows=0,
        tip_round=None, plate_inner=False):
    """An ear: w x d x h with `bury` rows inside its parent, butter on the inner (front) face."""
    e = Part(name)
    x0, x1 = x_c - w / 2, x_c + w / 2
    y0, y1 = base_y - bury, base_y + h - bury
    e.box(x0, x1, y0, y1, z0, z0 + d, col or COAT)
    if not plate_inner:
        e.paint(lambda x, y, z: z == z0 + d - 1 and base_y + 1 <= y < y1 - 2 and x0 + 1 <= x < x1 - 1, FACE)
    if tip_col:
        e.paint(lambda x, y, z: y >= y1 - tip_rows, tip_col)
    if tip_round:                                                                # (radius, facets)
        for s in (1, -1):
            round_edge(e, (x0, x1, y0, y1, z0, z0 + d), (0, 1), (s, 1), *tip_round)
    if tip:
        for s in (1, -1):
            bevel_edge(e, (x0, x1, y0, y1, z0, z0 + d), (s, 1, 0), tip)
    pivot = (x_c, base_y if pivot_y is None else pivot_y, z0 + d / 2)
    ob = e.build(pivot=pivot)
    if plate_inner:
        plate("Plate" + name, ob, pivot, x0 + 1, x1 - 1, base_y + 1, y1 - 2, z0 + d, 0.7, FACE)
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

elif VARIANT == "cube":
    # The reference's profile on a cube, in one part: a tall front and a back sloping away through one
    # big 45 deg bevel (a 7 x 7 wedge, R7b) along the top rear edge; every other edge bevelled by two
    # voxels. Upright ears, 4 x 3 thick.
    # soften=1..4 trials, each building on the last: 1 bigger bevels and cut corners; 2 every small edge
    # as two facets on a curve; 3 the back as a three-facet curve too; 4 that, smooth-shaded.
    box = (-6, 6, 2, 15, -7, 8)
    body = Part("Body")
    body.box(*box, COAT)
    X, Y, Z = 0, 1, 2
    small = [((X, Y), (s1, s2)) for s1 in (1, -1) for s2 in (1, -1)] + \
            [((X, Z), (s1, s2)) for s1 in (1, -1) for s2 in (1, -1)] + \
            [((Y, Z), (1, 1)), ((Y, Z), (-1, 1)), ((Y, Z), (-1, -1))]
    r, facets = {0: (2, 1), 1: (3, 1)}.get(SOFT, (3, 2))
    for axes, signs in small:
        round_edge(body, box, axes, signs, r, facets)
    if SOFT >= 3:
        round_edge(body, box, (Y, Z), (1, -1), 9, 3)                             # the back as a curve
    else:
        bevel_edge(body, box, (0, 1, -1), 7)                                     # the sloping back
    if SOFT == 1:                                                                # cut the eight corners
        for sx in (1, -1):
            for sy in (1, -1):
                for sz in (1, -1):
                    bevel_edge(body, box, (sx, sy, sz), 5)
    flat = r                                                                     # the front's flat margin
    if not ROUNDED:
        body.paint(lambda x, y, z: z == 7 and -6 + flat <= x < 6 - flat and 2 + flat <= y < 15 - flat, FACE)
    mark = lambda pred: body.paint(lambda x, y, z: body.vox[(x, y, z)] == COAT and pred(x, y, z), MARK)
    ear_cols = {1: {}, -1: {}}
    if PATTERN == "dutch":            # the rear half dark, a clean line across the middle
        mark(lambda x, y, z: z < 0)
        ear_cols = {1: dict(col=MARK), -1: dict(col=MARK)}
    elif PATTERN == "spots":          # 3 x 3 patches, one voxel in from every edge (the giraffe's taste call)
        spots = [(-6, 9, -3), (-6, 5, 3), (5, 10, 2), (5, 5, -4), (-3, 13, 1), (2, 13, -1), (-1, 6, -6), (3, 9, -5)]
        mark(lambda x, y, z: any(abs(x - a) <= 1 and abs(y - b) <= 1 and abs(z - c) <= 1 for a, b, c in spots))
    elif PATTERN == "stripes":        # 2-voxel bands around the body, dark ear tips
        mark(lambda x, y, z: (z + 7) % 4 in (1, 2) and z < 6)
        ear_cols = {1: dict(tip_col=MARK, tip_rows=3), -1: dict(tip_col=MARK, tip_rows=3)}
    elif PATTERN == "harlequin":      # split down the middle, the ears the other way round
        mark(lambda x, y, z: x < 0)
        ear_cols = {1: dict(col=MARK), -1: {}}
    face_size = min(11, (12 - 2 * flat) / 0.66)                                  # the texture's face spans 0.66
    body_ob = body.build()
    face_uvs(body_ob, 8, 10.5 if flat <= 2 else 10, face_size)
    if ROUNDED:                       # the panel as a plate, inset to clear the fillet, the face decal on it
        inset = flat + 0.4
        face_uvs(plate("PlateFace", body_ob, (0, 0, 0), -6 + inset, 6 - inset, 2 + inset, 15 - inset, 8, 1.5, FACE),
                 8 + PLATE_PROUD, 10.5, face_size)
    for sx in (1, -1):
        ear("Ear" + ("R" if sx > 0 else "L"), sx, sx * 2.5, 15, 2, 4, 3, 11, 2, pitch=10, splay=15,
            tip=0 if SOFT >= 2 else 1, tip_round=(2, 2) if SOFT >= 2 else None, plate_inner=ROUNDED, **ear_cols[sx])
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

if ROUNDED:
    fillet_all()

if SOFT >= 4:                                         # smooth shading across the facets, hard at 90 deg
    for o in bpy.data.objects:
        if o.type == "MESH":
            o.data.shade_smooth()
            o.data.set_sharp_from_angle(angle=math.radians(50))

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
