"""Builds a big fat toad in the voxel style (Assembler Assets/docs/VoxelStyle.md): a hop, then a tongue flick.

python build_toad.py -- [out=<render prefix>] [video=<frame prefix>]   (with `bpy`, or `blender --background --python`)
blender --background --factory-startup --python bake_shade.py -- build_toad.py <fbx_out> Body

Design note (R3a), no reference image:
- Keep: a squat, wide, humped body that reads as fat; a broad flat head set lower than the hump; eye domes;
  parotoid glands behind the eyes; warts as darker patches; a wide mouth line; chunky front arms; big
  folded hind legs; a pale belly and throat; a long pink tongue with a sticky tip.
- Drop: webbed toes, nostrils, skin texture, a vocal sac: none survives the thumbnail (R7c).
- Proportions: a ~0.15 m toad scaled up toy-style to 19 voxels long and 25 wide across the hind feet (R3);
  head : trunk width 10 : 14; hump 11 tall over a 10-tall head; eye domes 3 x 3 x 2 (head 9 tall, 2 x 2 eyes).
- The tongue is hinged at the front of the jaw, as a real toad's is, and lies folded back inside the jaw at
  rest; it flips forward out of the open mouth (R16: it only rotates).
Origin: the centre between the four feet at ground level (R2). Forward +Z.
"""
import sys
from voxel_common import *

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
kv = dict(a.split("=", 1) for a in argv if "=" in a)
OUT = kv.pop("out", "/tmp/toad")
VIDEO = kv.pop("video", None)
STILLS = kv.pop("frames", None)      # comma-separated frames to render from the video camera

SKIN, WART, BELLY, TONGUE = "P20", "P14", "P09", "P46"   # Olive, Brown, Cream, Pink (+ Ink, Mouth): 6 colours (R11)
Z0 = -3                                                  # shifts the model so the origin falls between the feet (R2)

# ---- Body (root): humped trunk, head, eye domes and parotoid glands on the main grid ---------------------
body = Part("Body")
body.box(-7, 7, 2, 11, -7, 4, SKIN)                                       # trunk
body.box(-5, 5, 6, 10, 4, 11, SKIN)                                       # head, above the jaw
for x0 in (2, -5):
    body.box(x0, x0 + 3, 10, 12, 6, 9, SKIN)                              # eye domes
for x0 in (1, -4):
    body.box(x0, x0 + 3, 11, 12, -2, 2, WART)                             # parotoid glands on the shoulders
trunk = lambda x, y, z: z < 4 and y < 11
head = lambda x, y, z: z >= 4 and y < 10
dome = lambda x, y, z: z >= 4 and y >= 10
body.bevel((0, 1, -1), 11 + 7 - 3, trunk)                                 # hump falls to the rump, 3 x 3
body.bevel((0, -1, -1), -2 + 7 - 2, trunk)                                # rump, underside
body.bevel((0, 1, 1), 11 + 4 - 2, trunk)                                  # hump falls to the head
body.bevel((0, 1, 1), 10 + 11 - 2, head)                                  # snout, 2 x 2
for sx in (1, -1):
    body.bevel((sx, 1, 0), 7 + 11 - 3, trunk)                             # trunk's top edges, 3 x 3: the fat
    body.bevel((sx, -1, 0), 7 - 2 - 2, trunk)                             # trunk's bottom edges
    body.bevel((sx, 0, -1), 7 + 7 - 3, trunk)                             # rump corners
    body.bevel((sx, 0, 1), 7 + 4 - 2, trunk)                              # shoulders
    body.bevel((sx, 0, 1), 5 + 11 - 2, head)                              # snout corners
    body.bevel((sx, 1, 0), 5 + 10 - 1, lambda x, y, z: head(x, y, z) and not 6 <= z < 9)   # head's top edges, clear of the domes
    on_dome = lambda x, y, z, sx=sx: dome(x, y, z) and x * sx > 0
    body.bevel((sx, 1, 0), 5 + 12 - 1, on_dome)                           # dome, outer edge
    body.bevel((-sx, 1, 0), -2 + 12 - 1, on_dome)                         # dome, inner edge
body.bevel((0, 1, -1), 12 - 6 - 1, dome)                                  # domes, back edge
body.paint(lambda x, y, z: y == 2, BELLY)
warts = [(-4, -4), (1, -5), (-1, -1), (3, -2), (-6, 0)]                   # min corners (x, z) of 2 x 2 warts on the back
body.paint(lambda x, y, z: y == 10 and z < 4 and any(wx <= x < wx + 2 and wz <= z < wz + 2 for wx, wz in warts), WART)
body.paint(lambda x, y, z: abs(x + 0.5) == 6.5 and (y, z) in ((7, -3), (7, -2), (8, -3), (8, -2), (5, 0), (5, 1), (6, 0), (6, 1)), WART)   # flank warts
body.paint(lambda x, y, z: z == 8 and y in (10, 11) and x in (3, 4, -4, -5), INK)   # eyes, 2 x 2 (head 9 tall, R13)
body.build(pivot=(0, 0, -Z0))                                             # origin lands between the feet once shifted

# ---- Jaw: hinged at the head's back, resting one 5 deg step open (R14, R19); 3 buried in the trunk -------
jaw = Part("Jaw")
jaw.box(-4.5, 4.5, 3, 6, 1, 11, SKIN)
jaw.bevel((0, -1, 1), -3 + 11 - 1)                                        # chin
for sx in (1, -1):
    jaw.bevel((sx, 0, 1), 4.5 + 11 - 1)                                   # chin corners, under the snout's
jaw.paint(lambda x, y, z: y == 3 and z < 10 and -3.5 <= x < 3.5, BELLY)   # throat
jaw.paint(lambda x, y, z: y == 5 and z >= 4 and (x in (-4.5, 3.5) or z == 10), MOUTH)   # the smile
jaw.paint(lambda x, y, z: y == 5 and z < 10 and -3.5 <= x < 3.5, MOUTH)   # inside of the mouth, seen when it gapes
jaw.build(pivot=(0, 6, 4), rot=(5, 0, 0), parent=body.ob)

# ---- Tongue: hinged at the front of the jaw, folded back inside it at rest; a sticky tip on the end -----
tongue = Part("Tongue")
tongue.box(-1, 1, 4.5, 5.5, 3.5, 10.5, TONGUE)                            # band, half a voxel under the jaw's top (R8)
tongue.box(-2, 2, 3.5, 5.5, 1.5, 3.5, TONGUE)                             # sticky tip
tongue.build(pivot=(0, 5, 10), rot=(5, 0, 0), parent=jaw.ob)

fronts, hinds = [], []

# ---- Front arms: chunky and straight, 2 buried in the trunk ---------------------------------------------
for sx in (1, -1):
    leg = Part("FrontLeg" + ("R" if sx > 0 else "L"))
    x0 = 4.5 if sx > 0 else -7.5
    leg.box(x0, x0 + 3, 0, 4, 2.5, 5.5, SKIN)                             # half a voxel off the trunk's side (R8)
    leg.box(x0, x0 + 3, 0, 1, 5.5, 7.5, SKIN)                             # hand, fingers forward
    leg.build(pivot=(sx * 6, 3, 4), parent=body.ob)
    fronts.append((leg.ob, sx))

# ---- Hind legs: fat folded thigh at the side of the rump, long foot forward along the ground -------------
for sx in (1, -1):
    leg = Part("HindLeg" + ("R" if sx > 0 else "L"))
    xi, xo = 6.5, 11.5                                                    # thigh, inner face buried half a voxel
    fi, fo = 8.5, 12.5                                                    # foot
    m = lambda a, b: (a, b) if sx > 0 else (-b, -a)
    leg.box(*m(xi, xo), 1, 7, -8, 1, SKIN)
    leg.box(*m(fi, fo), 0, 1, -3, 5, SKIN)
    thigh = lambda x, y, z: y >= 1
    leg.bevel((0, 1, -1), 7 + 8 - 2, thigh)                               # top rear, 2 x 2
    leg.bevel((0, 1, 1), 7 + 1 - 3, thigh)                                # knee slope, 3 x 3
    leg.bevel((sx, 1, 0), xo + 7 - 2, thigh)                              # outer top edge, 2 x 2
    leg.paint(lambda x, y, z: y >= 1 and (y, z) in ((3, -6), (3, -5), (4, -6), (4, -5), (2, -2), (2, -1), (3, -2), (3, -1)), WART)   # thigh warts
    leg.build(pivot=(sx * 7, 4, -3), parent=body.ob)
    hinds.append((leg.ob, sx))

body.ob.location = Vector(body.ob.location) + Vector(B((0, 0, Z0)))     # the whole model moves; pivots move with it

# ---- Hop, then a tongue flick: one 64-frame loop. A heavy toad hops slower than a frog (frames 0..30), sits,
# then rears its head, gapes, flips the tongue out, wiggles it and reels it back in (frames 30..64).
# Keys per channel (frame, value), eased between; frame 64 equals 0.
FRAMES = 64
KEYS = {   # body pitch (+ is nose down), hind and front leg swing (+ swings the foot back), jaw, tongue
    "pitch":  [(0, 0), (5, 6), (9, -20), (15, -5), (20, 10), (24, 3), (30, 0), (36, 0), (40, -12), (52, -12), (58, 0), (64, 0)],
    "hind":   [(0, 0), (5, 0), (9, 60), (15, 50), (20, 15), (24, 0), (30, 0), (36, 0), (40, 12), (52, 12), (58, 0), (64, 0)],
    "front":  [(0, 0), (5, 5), (9, 20), (15, -30), (20, -20), (24, 0), (30, 0), (36, 0), (40, 12), (52, 12), (58, 0), (64, 0)],
    "jaw":    [(0, 5), (12, 5), (15, 12), (19, 5), (38, 5), (41, 35), (51, 35), (55, 5), (64, 5)],
    "tongue": [(0, 5), (42, 5), (43, 155), (45, 162), (47, 146), (49, 160), (51, 150), (52, 5), (64, 5)],   # flips out and back in a single frame
}
AIR = (8, 14, 20, 6.0)     # take-off frame, peak frame, landing frame, peak height in voxels


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
parts = [body.ob, jaw.ob, tongue.ob] + [o for o, _ in fronts + hinds]
for f in range(FRAMES + 1):
    pose(tongue.ob, rot=(channel("tongue", f), 0, 0))
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
target = Vector((0, 0.1, 0.75))
VIEWS = {"three": (-45, 30, 4.6), "side": (-90, 10, 4.6), "front": (0, 10, 4.6), "top": (0, 89, 4.6)}


def aim(az, el, scale):
    a, e = math.radians(az), math.radians(el)
    d = Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
    cam.location = target + d * 10
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = scale


if STILLS:
    aim(-35, 22, 5.2)
    for f in map(int, STILLS.split(",")):
        sc.frame_set(f)
        sc.render.filepath = f"{OUT}_f{f:02d}.png"
        bpy.ops.render.render(write_still=True)
elif VIDEO:   # every frame of the loop from the three-quarter camera, for ffmpeg
    aim(-35, 22, 5.2)
    sc.cycles.samples = 48
    sc.frame_end = FRAMES - 1                                             # frame 64 repeats frame 0
    sc.render.filepath = VIDEO
    bpy.ops.render.render(animation=True)
else:
    for name, (az, el, scale) in VIEWS.items():
        aim(az, el, scale)
        sc.render.filepath = f"{OUT}_{name}.png"
        bpy.ops.render.render(write_still=True)
