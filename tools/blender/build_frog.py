"""Builds a sitting frog in the voxel style (Assembler Assets/docs/VoxelStyle.md) and renders preview views.

python build_frog.py -- [out=<render prefix>]          (with the `bpy` module, or `blender --background --python`)
blender --background --factory-startup --python bake_shade.py -- build_frog.py <fbx_out> Body

Design note (R3a), no reference image:
- Keep: the sitting pose with the back sloping down to the rump; a wide flat head; eye domes bulging from
  the top of the head; the wide mouth line wrapping round the snout; short straight front legs; big
  folded hind legs at the sides with long feet pointing forward; a pale belly and throat; dark spots on
  the back and bands on the thighs.
- Drop: webbed toes, a vocal sac, nostrils, skin texture: none survives the thumbnail (R7c).
- Proportions: a ~0.1 m frog scaled up toy-style to 15 voxels long and 19 wide across the hind feet (R3);
  head : trunk width 8 : 10; eye domes 3 x 3 x 2; the head sits one voxel above the back.
Origin: the centre between the four feet at ground level (R2). Forward +Z.
"""
import sys
from voxel_common import *

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
kv = dict(a.split("=", 1) for a in argv if "=" in a)
OUT = kv.pop("out", "/tmp/frog")

SKIN, SPOT, BELLY = "P19", "P21", "P09"     # Leaf, Grass, Cream (+ Ink eyes, Mouth): 5 colours (R11)
Z0 = -1.5                                   # shifts the model so the origin falls between the feet (R2)

# ---- Body (root): trunk, head and eye domes on the main grid -------------------------------------------
body = Part("Body")
body.box(-5, 5, 2, 7, -5, 3, SKIN)                                        # trunk
body.box(-4, 4, 5, 8, 3, 9, SKIN)                                         # head, above the jaw
for x0 in (1, -4):
    body.box(x0, x0 + 3, 8, 10, 5, 8, SKIN)                               # eye domes
trunk = lambda x, y, z: z < 3
head = lambda x, y, z: z >= 3 and y < 8
dome = lambda x, y, z: y >= 8
body.bevel((0, 1, -1), 7 + 5 - 3, trunk)                                  # back falls to the rump, 3 x 3
body.bevel((0, -1, -1), -2 + 5 - 1, trunk)                                # rump, underside
body.bevel((0, 1, 1), 8 + 9 - 2, head)                                    # snout, 2 x 2
for sx in (1, -1):
    body.bevel((sx, 1, 0), 5 + 7 - 1, trunk)                              # trunk's top edges
    body.bevel((sx, 0, -1), 5 + 5 - 1, trunk)                             # rump corners
    body.bevel((sx, 0, 1), 4 + 9 - 1, head)                               # snout corners
    body.bevel((sx, 1, 0), 4 + 8 - 1, lambda x, y, z: head(x, y, z) and not 5 <= z < 8)   # head's top edges, clear of the domes
    on_dome = lambda x, y, z, sx=sx: dome(x, y, z) and x * sx > 0
    body.bevel((sx, 1, 0), 4 + 10 - 1, on_dome)                           # dome, outer edge
    body.bevel((-sx, 1, 0), -1 + 10 - 1, on_dome)                         # dome, inner edge
body.bevel((0, 1, 1), 10 + 8 - 1, dome)                                   # domes, front edge
body.bevel((0, 1, -1), 10 - 5 - 1, dome)                                  # domes, back edge
body.paint(lambda x, y, z: y == 2, BELLY)
spots = [(-3, -1), (1, 0), (-1, 2)]                                       # min corners (x, z) of 2 x 2 spots on the back
body.paint(lambda x, y, z: y == 6 and any(sx <= x < sx + 2 and sz <= z < sz + 2 for sx, sz in spots), SPOT)
body.paint(lambda x, y, z: y == 8 and z == 7 and x in (2, -3), INK)       # eyes, 1 x 1 (head under 8 tall, R13)
body.build(pivot=(0, 0, -Z0))                                             # origin lands between the feet once shifted

# ---- Jaw: hinged at the mouth corners, resting one 5 deg step open (R14, R19); 2 buried in the trunk ----
jaw = Part("Jaw")
jaw.box(-3.5, 3.5, 3, 5, 1, 9, SKIN)
jaw.bevel((0, -1, 1), -3 + 9 - 1)                                         # chin
for sx in (1, -1):
    jaw.bevel((sx, 0, 1), 3.5 + 9 - 1)                                    # chin corners, under the snout's
jaw.paint(lambda x, y, z: y == 3 and z < 8 and -2.5 <= x < 2.5, BELLY)    # throat, inside the chin and sides
jaw.paint(lambda x, y, z: y == 4 and z >= 3 and (x in (-3.5, 2.5) or z == 8), MOUTH)   # the smile
jaw.build(pivot=(0, 5, 3), rot=(5, 0, 0), parent=body.ob)

fronts, hinds = [], []

# ---- Front legs: short and straight, 1 buried in the trunk ---------------------------------------------
for sx in (1, -1):
    leg = Part("FrontLeg" + ("R" if sx > 0 else "L"))
    x0 = 2.5 if sx > 0 else -5.5
    leg.box(x0, x0 + 3, 0, 3, 0.5, 2.5, SKIN)                             # half a voxel behind the trunk's front face (R8)
    leg.box(x0, x0 + 3, 0, 1, 2.5, 4.5, SKIN)                             # foot, toes forward
    leg.build(pivot=(sx * 4, 2.5, 1.5), parent=body.ob)
    fronts.append((leg.ob, sx))

# ---- Hind legs: folded thigh at the side of the rump, long foot forward along the ground ----------------
for sx in (1, -1):
    leg = Part("HindLeg" + ("R" if sx > 0 else "L"))
    xi, xo = 4.5, 8.5                                                     # thigh, inner face buried half a voxel
    fi, fo = 6.5, 9.5                                                     # foot
    m = lambda a, b: (a, b) if sx > 0 else (-b, -a)
    leg.box(*m(xi, xo), 1, 5, -6, 1, SKIN)
    leg.box(*m(fi, fo), 0, 1, -2, 4, SKIN)
    thigh = lambda x, y, z: y >= 1
    leg.bevel((0, 1, -1), 5 + 6 - 2, thigh)                               # top rear, 2 x 2
    leg.bevel((0, 1, 1), 5 + 1 - 3, thigh)                                # knee slope, 3 x 3
    leg.bevel((sx, 1, 0), xo + 5 - 1, thigh)                              # outer top edge
    leg.paint(lambda x, y, z: y >= 1 and z in (-4, -1), SPOT)             # thigh bands
    leg.build(pivot=(sx * 5, 3, -2), parent=body.ob)
    hinds.append((leg.ob, sx))

body.ob.location = Vector(body.ob.location) + Vector(B((0, 0, Z0)))     # the whole model moves; pivots move with it

# ---- Jump: an in-place hop. 24 frames: a 0.1 m frog's own cycle would be 8, far too quick to read, so the
# hop is slowed (character override). Keys per channel (frame, value), eased between; frame 24 equals 0.
FRAMES = 24
KEYS = {   # body pitch (+ is nose down), hind and front leg swing (+ swings the foot back), jaw, height
    "pitch": [(0, 0), (4, 6), (7, -25), (12, -5), (16, 10), (19, 3), (24, 0)],
    "hind":  [(0, 0), (4, 0), (7, 70), (12, 55), (16, 15), (19, 0), (24, 0)],
    "front": [(0, 0), (4, 5), (7, 20), (12, -35), (16, -20), (19, 0), (24, 0)],
    "jaw":   [(0, 5), (6, 5), (10, 20), (14, 5), (24, 5)],
}
AIR = (6, 11, 16, 5.0)     # take-off frame, peak frame, landing frame, peak height in voxels


def channel(name, f):
    ks = KEYS[name]
    for (f0, v0), (f1, v1) in zip(ks, ks[1:]):
        if f0 <= f <= f1:
            u = (f - f0) / (f1 - f0)
            return v0 + (v1 - v0) * u * u * (3 - 2 * u)                  # smoothstep between keys
    return ks[-1][1]


def air(f):
    t0, tp, t1, h = AIR
    if not t0 <= f <= t1:
        return 0.0
    u = (f - tp) / ((tp - t0) if f < tp else (t1 - tp))
    return h * (1 - u * u)                                                # a parabola, ground to peak to ground


def lowest():
    bpy.context.view_layer.update()
    return min((o.matrix_world @ v.co).z for o in root.children_recursive if o.type == 'MESH' for v in o.data.vertices)


begin_animation(FRAMES)
parts = [body.ob, jaw.ob] + [o for o, _ in fronts + hinds]
for f in range(FRAMES + 1):
    pose(jaw.ob, rot=(channel("jaw", f), 0, 0))
    for ob, _ in hinds:
        pose(ob, rot=(channel("hind", f), 0, 0))
    for ob, _ in fronts:
        pose(ob, rot=(channel("front", f), 0, 0))
    pose(body.ob, rot=(channel("pitch", f), 0, 0), offset=(0, 0, 0))
    pose(body.ob, offset=(0, air(f) - lowest(), 0))                       # the lowest point stays on the ground (or `air` above it)
    for ob in parts:
        key(ob, f)
flatten()

# ---- preview ---------------------------------------------------------------------------------------------
ground, cam = make_preview(ground_y=0)
target = Vector((0, 0, 0.45))
VIEWS = {"three": (-45, 30, 2.6), "side": (-90, 10, 2.6), "front": (0, 10, 2.6), "top": (0, 89, 2.6)}

for name, (az, el, scale) in VIEWS.items():
    a, e = math.radians(az), math.radians(el)
    d = Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
    cam.location = target + d * 10
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = scale
    sc.render.filepath = f"{OUT}_{name}.png"
    bpy.ops.render.render(write_still=True)
