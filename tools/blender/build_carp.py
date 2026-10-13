"""Builds a common carp in the voxel style (Assembler Assets/docs/VoxelStyle.md) and renders preview views.

python build_carp.py -- [out=<render prefix>]          (with the `bpy` module, or `blender --background --python`)
blender --background --factory-startup --python bake_shade.py -- build_carp.py <fbx_out> Body

Design note (R3a), no reference image:
- Keep: a deep, humped body with a sloping head; bronze back, gold flanks, cream belly; one long dorsal
  fin, tall at the front and falling to the rear; a thick tail stalk and a deeply forked tail; reddish
  fins; the two mouth barbels, which are what make it a carp rather than a generic fish.
- Drop: scales (noise at thumbnail size, R7c), the anal and pelvic fins (hidden from the thumbnail
  camera, and from the front they read as legs), the second pair of barbels, a mouth row (no jaw, R14):
  pale puckered lips stand in for it.
- Proportions: a ~0.6 m fish scaled up toy-style to 17 voxels (R3); body length : depth 11 : 7,
  tail stalk 2 long, tail 7 tall with a 2-voxel fork.
Origin: the centre of the root part (it swims, R2). Forward +Z.
"""
import sys
from voxel_common import *

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
kv = dict(a.split("=", 1) for a in argv if "=" in a)
OUT = kv.pop("out", "/tmp/carp")

BACK, FLANK, BELLY, FIN = "P25", "P52", "P09", "P51"     # Oak, Gold, Cream, Rust (+ Ink eyes): 5 colours (R11)

# ---- Body (root): 5 x 11 x 7 on a half-voxel offset (R5), a sloping forehead into a humped back -------
body = Part("Body")
body.box(-2.5, 2.5, -3.5, 3.5, -5.5, 5.5, FLANK)
body.paint(lambda x, y, z: y >= 1.5, BACK)
body.paint(lambda x, y, z: y < -1.5, BELLY)
on_body = lambda x, y, z: -3.5 <= y < 3.5 and -5.5 <= z < 5.5          # keeps the lips and barbels unbevelled
body.bevel((0, 1, 1), 3.5 + 5.5 - 3, on_body)                             # forehead, 3 x 3
body.bevel((0, -1, 1), 3.5 + 5.5 - 1, on_body)                            # chin, 1 x 1
body.bevel((0, 1, -1), 3.5 + 5.5 - 3, on_body)                            # back falls to the stalk, 3 x 3
body.bevel((0, -1, -1), 3.5 + 5.5 - 1, on_body)                           # belly to the stalk, 1 x 1
for sx in (1, -1):
    body.bevel((sx, 1, 0), 2.5 + 3.5 - 1, on_body)                        # shoulders
    body.bevel((sx, -1, 0), 2.5 + 3.5 - 1, on_body)                       # belly
    body.bevel((sx, 0, 1), 2.5 + 5.5 - 1, on_body)                        # snout
    body.bevel((sx, 0, -1), 2.5 + 5.5 - 1, on_body)                       # rear
eye_x = (-2.5, 1.5)                                                       # min corners of the two side columns
body.paint(lambda x, y, z: x in eye_x and y == 0.5 and z == 3.5, INK)     # eyes, 1 x 1 (head 7 tall, R13)
body.paint(lambda x, y, z: x in eye_x and -1.5 <= y < 1.5 and z == 1.5, BACK)   # gill line
body.box(-1.5, 1.5, -2.5, -1.5, 5.5, 6.5, BELLY)                          # pale puckered lip
body.box(-1.5, -0.5, -3.5, -2.5, 5.5, 6.5, FIN)                           # barbels under the lip's ends
body.box(0.5, 1.5, -3.5, -2.5, 5.5, 6.5, FIN)
body.build()

# ---- Tail stalk: 3 x 3, exactly the body's rear face, 1 buried (R15, R17) -----------------------------
stalk = Part("Stalk")
stalk.box(-1.5, 1.5, -2.5, 0.5, -7.5, -4.5, FLANK)
stalk.paint(lambda x, y, z: y >= -0.5, BACK)
stalk.build(pivot=(0, -1, -5), parent=body.ob)

# ---- Tail: 1 wide, 7 tall, leading edges rising at 45 deg from the stalk, forked 2 deep ---------------
tail = Part("Tail")
tail.box(-0.5, 0.5, -4.5, 2.5, -10.5, -6.5, FIN)
tail.bevel((0, 1, 1), -6)                                                 # upper leading edge
tail.bevel((0, -1, 1), -4)                                                # lower leading edge
tail.bevel((0, -1, -1), 9.5, lambda x, y, z: y >= -1)                     # fork, upper lobe
tail.bevel((0, 1, -1), 7.5, lambda x, y, z: y < -1)                       # fork, lower lobe
tail.build(pivot=(0, -1, -7), parent=stalk.ob)

# ---- Dorsal fin: tall at the front, falling to the rear; 1 buried in the back --------------------------
dorsal = Part("Dorsal")
dorsal.box(-0.5, 0.5, 2.5, 6.5, -3.5, 2.5, FIN)
dorsal.bevel((0, 1, 1), 6.5 + 2.5 - 1)                                    # rounded leading tip
dorsal.bevel((0, 1, -1), 6.5)                                             # long fall, half a voxel proud of the back's bevel (R8)
dorsal.build(pivot=(0, 3, 0), parent=body.ob)

# ---- Pectoral fins: thin plates low behind the head, swept back and drooped on their own grids (R6, R7) -
pecs = []
for sx in (1, -1):
    pec = Part("Pectoral" + ("R" if sx > 0 else "L"))
    x0 = 1.5 if sx > 0 else -4.5                                          # 1 buried in the body
    pec.box(x0, x0 + 3, -3, -2, 1, 3, FIN)
    pec.build(pivot=(sx * 2, -2.5, 2.5), rot=(0, sx * 45, -sx * 35), parent=body.ob)
    pecs.append((pec.ob, sx))

# ---- Swim: a gentle wave from head to tail through the seam pivots ---------------------------------------
# A real carp is 0.6 m, so its loop would be round(24 x sqrt(0.6)) = 19 frames; this one cruises, so it is
# slowed to 32 (character override). Every channel is a whole number of cycles, so frame 32 equals frame 0.
FRAMES = 32
begin_animation(FRAMES)
for f in range(FRAMES + 1):
    w = 2 * math.pi * f / FRAMES
    pose(body.ob, rot=(0, 3 * math.sin(w), 1.5 * math.sin(w + 0.5)), offset=(0, 0.3 * math.sin(2 * w), 0))
    pose(stalk.ob, rot=(0, -9 * math.sin(w - 0.9), 0))                      # the stalk follows, later
    pose(tail.ob, rot=(0, -16 * math.sin(w - 1.8), 0))                      # and the tail, later still
    for ob, sx in pecs:                                                     # the pectorals paddle
        pose(ob, rot=(0, sx * (45 + 6 * math.sin(w)), -sx * (35 + 8 * math.sin(2 * w))))
    for ob in (body.ob, stalk.ob, tail.ob) + tuple(ob for ob, _ in pecs):
        key(ob, f)
flatten()

# ---- preview ---------------------------------------------------------------------------------------------
ground, cam = make_preview(ground_y=-5.5)
target = Vector((0, -0.05, 0))
VIEWS = {"three": (-45, 30, 2.4), "side": (-90, 0, 2.4), "front": (0, 10, 2.4), "top": (0, 89, 2.4)}

for name, (az, el, scale) in VIEWS.items():
    a, e = math.radians(az), math.radians(el)
    d = Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
    cam.location = target + d * 10
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = scale
    sc.render.filepath = f"{OUT}_{name}.png"
    bpy.ops.render.render(write_still=True)
