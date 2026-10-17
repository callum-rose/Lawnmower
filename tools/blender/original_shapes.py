"""Shape variations on the in-game regular_bunny.blend (base|chubby|tall|lop|swept), rendered from four sides.

python original_shapes.py -- Assets/Archive/RegularBunny/regular_bunny.blend <variant> <out prefix>
"""
import bpy, sys, math, os
from mathutils import Vector, Matrix
argv = sys.argv[sys.argv.index("--") + 1:]
SRC, VARIANT, OUT = argv
bpy.ops.wm.open_mainfile(filepath=SRC)
for n in ["Body.001", "Sphere.005", "Sphere.006", "Sphere.007", "Sphere.008"]:
    bpy.data.objects[n].hide_render = True
for o in bpy.data.objects:
    o.animation_data_clear()
ASSETS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "../../Assets/")
for im in bpy.data.images:
    if "Mowers/" in im.filepath:
        name = im.filepath.split("Mowers/")[1]
        for d in ("Textures/Game/Mowers/", "Archive/RegularBunny/"):
            if os.path.exists(ASSETS + d + name):
                im.filepath = ASSETS + d + name
                break
bpy.context.scene.frame_set(0)
O = bpy.data.objects
body = O["Body"]; centre = body.matrix_world.translation.copy()
ears = [O["ear_left"], O["ear_right"]]
others = ears + [O["tail"]] + [O[n] for n in ("foot_front_left", "foot_front_right", "foot_back_left", "foot_back_right")]

def world(o, m):                    # apply m about o's own origin, in world space
    t = o.matrix_world.translation.copy()
    o.matrix_world = Matrix.Translation(t) @ m @ Matrix.Translation(-t) @ o.matrix_world

def body_scale(sx, sy, sz):         # x forward, y across, z up; attached parts follow the surface
    S = Matrix.Diagonal((sx, sy, sz, 1))
    world(body, S)
    for o in others:
        p = o.matrix_world.translation
        o.matrix_world.translation = centre + Vector(((p - centre).x * sx, (p - centre).y * sy, (p - centre).z * sz))

def ears_do(scale=1, lean_back=0, droop=0):
    for e in ears:
        side = 1 if e.matrix_world.translation.y > 0 else -1
        m = Matrix.Rotation(math.radians(-lean_back), 4, "Y") @ Matrix.Rotation(math.radians(-side * droop), 4, "X")
        world(e, m @ Matrix.Diagonal((scale, scale, scale, 1)))

if VARIANT == "chubby":             # wider and lower, short ears
    body_scale(1.0, 1.2, 0.88); ears_do(scale=0.8)
elif VARIANT == "tall":             # an upright egg with long ears
    body_scale(0.9, 0.9, 1.2); ears_do(scale=1.2)
elif VARIANT == "lop":              # ears flopped down the sides
    ears_do(droop=115)
elif VARIANT == "swept":            # big ears swept back
    ears_do(scale=1.35, lean_back=30)
bpy.context.view_layer.update()

ground_z = min((o.matrix_world @ v.co).z for o in others[3:] for v in o.data.vertices)
sc = bpy.context.scene
sc.render.engine = "CYCLES"; sc.cycles.samples = 48; sc.cycles.device = "CPU"
sc.render.resolution_x = sc.render.resolution_y = 600
cam = O["Camera"]
for name, (az, el) in {"three": (40, 25), "front": (0, 5), "side": (-90, 5), "back": (135, 20)}.items():
    a, e = math.radians(az), math.radians(el); d = 8
    tgt = Vector((0, 0, ground_z + 1.6))
    cam.location = tgt + Vector((d * math.cos(e) * math.cos(a), d * math.cos(e) * math.sin(a), d * math.sin(e)))
    cam.rotation_euler = (tgt - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = f"{OUT}_{name}.png"
    bpy.ops.render.render(write_still=True)
