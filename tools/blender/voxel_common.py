"""Shared part builder for the voxel-style model scripts in this folder (build_carp.py, build_frog.py).

Ported from Assembler's Tools/blender/build_human.py, which builds to the house style in Assembler's
Assets/docs/VoxelStyle.md. A build script does `from voxel_common import *`, builds its parts, then calls
`make_preview(...)` and ends in the standard render loop, so it also satisfies the bake_shade.py contract:

    blender --background --factory-startup --python bake_shade.py -- build_carp.py <fbx_out> Body

Voxel coordinates are Unity's (x right, y up, z forward); `B` maps them into Blender (front faces -Y).
Colours are VoxelStyle.md palette indices.
"""
import bpy, bmesh, math
from mathutils import Vector, Matrix, Euler

PALETTE = {
    "P00": "f6f6f2", "P01": "dcdcd8", "P02": "b2b2b2", "P03": "8a8b90", "P04": "66676c", "P05": "4a4a50",
    "P06": "2b2b30", "P07": "3a2630", "P08": "141414", "P09": "ece0c0", "P10": "dcc08e", "P11": "c49470",
    "P12": "b07a50", "P13": "8e5634", "P14": "6e3d22", "P15": "4a2818", "P16": "2e1810", "P17": "a6ee28",
    "P18": "bcc584", "P19": "8fc23a", "P20": "9ea26a", "P21": "5a9440", "P22": "3b7038", "P23": "23452a",
    "P24": "c89a60", "P25": "9a6a3a", "P26": "6b4528", "P27": "4a3020", "P41": "ffd070", "P42": "f6d021",
    "P43": "eea62a", "P44": "f08236", "P45": "c83a2a", "P46": "e8899a", "P50": "f6b48a", "P51": "b4521e",
    "P52": "c8961a", "P53": "f6c4cc", "P57": "2a8a8a", "P58": "7ad0c8", "P59": "b8e6b0",
}
INK, MOUTH = "P08", "P07"

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
        MATS[key] = m
    return list(MATS).index(key)


def B(p):
    return (p[0], -p[2], p[1])


_M = Matrix(((1, 0, 0), (0, 0, -1), (0, 1, 0)))     # Unity axes -> Blender axes


def unity_rotation(rx=0, ry=0, rz=0, compat=None):
    """A rotation given in Unity degrees (applied Z, then X, then Y, as Unity does), as a Blender euler,
    kept continuous with `compat` when animating. Every rest angle should be a multiple of 5 (R6)."""
    r = lambda ax, a: Matrix.Rotation(math.radians(a), 3, ax)
    ru = r("Y", ry) @ r("X", rx) @ r("Z", rz)
    m = _M @ ru @ _M.transposed()
    return m.to_euler("XYZ", compat) if compat is not None else m.to_euler()


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

    def build(s, pivot=(0, 0, 0), rot=(0, 0, 0), parent=None):
        """Voxels are authored at rest on the part's own grid, in model coordinates; `rot` (Unity degrees)
        then turns the part about `pivot`, which becomes its origin."""
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
        if parent is None:
            ob.location = B(pivot)
            ob.parent = root
        else:   # pivot is in model coordinates; place relative to the parent's (unrotated) origin
            bpy.context.view_layer.update()
            ob.parent = parent
            ob.location = Vector(B(pivot)) - parent.matrix_world.translation
        ob.rotation_euler = unity_rotation(*rot)
        s.ob = ob
        return ob


root = bpy.data.objects.new("root", None)
sc.collection.objects.link(root)


# ---- animation: parts only rotate about their pivots; only the root part also moves (R7, R16) ----------
REST = {}       # rest transforms by object name; bake_shade.py bakes and measures at rest


def begin_animation(frames, fps=24):
    """Snapshot the rest pose and set a seamless loop over frames 0..frames (the last key equals the first)."""
    for o in bpy.data.objects:
        REST[o.name] = (o.location.copy(), o.rotation_euler.copy())
    sc.frame_start, sc.frame_end = 0, frames
    sc.render.fps = fps
    bpy.context.preferences.edit.keyframe_new_interpolation_type = "LINEAR"


def pose(ob, rot=None, offset=None):
    """Set a part's rotation (Unity degrees, rest angle included) and, for the root part only, an offset
    from its rest position (Unity voxels)."""
    if rot is not None:
        ob.rotation_euler = unity_rotation(*rot, compat=ob.rotation_euler)
    if offset is not None:
        ob.location = REST[ob.name][0] + Vector(B(offset))


def key(ob, frame):
    ob.keyframe_insert("rotation_euler", frame=frame)
    ob.keyframe_insert("location", frame=frame)


def flatten():
    """Bake chained motion into flat parts: every part a direct child of `root`, keyed every frame with its
    full transform (R18 - nested parts come back misplaced through the FBX export)."""
    obs = [o for o in bpy.data.objects if o.type == 'MESH' and o.parent is not None]
    frames = range(sc.frame_start, sc.frame_end + 1)
    mats = {o.name: [] for o in obs}
    for f in frames:
        sc.frame_set(f)
        for o in obs:
            mats[o.name].append(root.matrix_world.inverted() @ o.matrix_world)
    for o in obs:
        if o.animation_data:
            o.animation_data.action = None
    rest_pose()
    bpy.context.view_layer.update()
    rest = {o.name: root.matrix_world.inverted() @ o.matrix_world for o in obs}
    for o in obs:
        o.parent = root
        o.matrix_parent_inverse.identity()
        o.matrix_basis = rest[o.name]
        REST[o.name] = (o.location.copy(), o.rotation_euler.copy())
    for o in obs:
        for i, f in enumerate(frames):
            loc, q, _ = mats[o.name][i].decompose()
            o.location = loc
            o.rotation_euler = q.to_euler("XYZ", o.rotation_euler)
            key(o, f)
    rest_pose()
    sc.frame_set(sc.frame_start)


def rest_pose():
    for name, (loc, rot) in REST.items():
        o = bpy.data.objects[name]
        o.location, o.rotation_euler = loc, rot


# ---- preview: ground, light, orthographic camera ------------------------------------------------------
BG = lin("e3e6eb")


def make_preview(ground_y=0.0, samples=96, res=800):
    """Ground plane at `ground_y` voxels, sun, ortho camera. Returns (ground, cam)."""
    root.scale = (0.1, 0.1, 0.1)                                                      # R1: authored in voxels
    sc.render.engine = "CYCLES"
    sc.cycles.samples = samples
    sc.cycles.use_denoising = True
    sc.view_settings.view_transform = "Standard"
    sc.render.resolution_x = sc.render.resolution_y = res
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = BG
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.75
    gz = ground_y * 0.1
    gme = bpy.data.meshes.new("ground")
    gme.from_pydata([(-20, -20, gz), (20, -20, gz), (20, 20, gz), (-20, 20, gz)], [], [(0, 1, 2, 3)])
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
    return ground, cam
