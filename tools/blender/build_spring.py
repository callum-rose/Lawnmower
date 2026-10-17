"""Builds the Spring tile prop in the voxel style (Assets/Docs/VoxelStyle.md) and renders preview views.

python build_spring.py -- variant=coil|mushroom|trampoline|board [piece=spring|landing|pair] [out=<render prefix>]
blender --background --factory-startup --python bake_shade.py -- build_spring.py <fbx_out> <Stem|Marker0> variant=mushroom piece=<spring|landing> tiles=0

Design note (R3a), no reference image. A spring tile launches the mower to another tile, so it must read as
"bouncy" from the game camera and sit inside one 1 m tile (10 x 10 voxels) without filling it, so the
mower visibly drives onto it. Every variant stays within 9 x 9 so neighbouring tiles never share a face plane.
- coil: the classic launch pad. A slate base, a three-ring steel coil, a red pad with a yellow target.
  Rings alternate +-10 deg so the stack zigzags like a helix seen from the side.
- mushroom: a garden toadstool trampoline. A cream stem under a red octagon cap with white spots.
- trampoline: a kids' trampoline. Blue/yellow octagon padding on four legs, a recessed graphite mat.
- board: a directional springboard. A plank hinged at the back, propped 15 deg up by a coil, with red
  chevrons pointing the launch direction (+Z); the tile turns it to face its Offset.
Proportions: every variant 5-7 voxels tall against the 10-voxel tile (a squat prop the mower rolls onto).
Origin: tile centre, at ground level. Forward +Z.
"""
import sys
from voxel_common import *

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
kv = dict(a.split("=", 1) for a in argv if "=" in a)
OUT = kv.pop("out", "/tmp/spring")
VARIANT = kv.pop("variant", "coil")
PIECE = kv.pop("piece", "spring")            # spring | landing | pair (the landing two tiles ahead)
TILES = () if kv.pop("tiles", "1") == "0" else None   # tiles=0 leaves the preview tiles out (bake_shade.py export)

PALETTE.update({"P04": "66676c", "P06": "2b2b30", "P29": "8a837a", "P39": "4a66b4", "P63": "8896a8"})


def ring(part, half, y0, y1, col, wall=1):
    """A square ring `half` voxels out from the centre, `wall` thick."""
    part.box(-half, half, y0, y1, -half, half, col)
    inner = half - wall
    for k in list(part.vox):
        if -inner <= k[0] < inner and -inner <= k[2] < inner and y0 <= k[1] < y1:
            del part.vox[k]


def coil(name, half, centres, rx_first, parent, x=0, z=0):
    """Rings on alternating +-10 deg grids (R6, R7), each pivoting about its centre on the half-voxel lattice."""
    for i, c in enumerate(centres):
        r = Part(f"{name}{i}")
        ring(r, half, c - 0.5, c + 0.5, STEEL)
        r.vox ={(k[0] + x, k[1], k[2] + z): STEEL for k in list(r.vox)}
        r.build(pivot=(x, c, z), rot=(rx_first * (1 if i % 2 == 0 else -1), 0, 0), parent=parent)


STEEL, SLATE, RED, YELLOW, WHITE = "P63", "P04", "P45", "P42", "P00"

if PIECE in ("spring", "pair"):
    if VARIANT == "coil":
        # Base (root): 8 x 8 x 2 slate plate, top edges bevelled 1 x 1.
        base = Part("Base")
        base.box(-4, 4, 0, 2, -4, 4, SLATE)
        for n in ((1, 1, 0), (-1, 1, 0), (0, 1, 1), (0, 1, -1)):
            base.bevel(n, 4 + 2 - 1)
        base.build()
        # Coil: three 6 x 6 rings, the bottom one 1 voxel into the base, the top one into the pad (R15).
        coil("Coil", 3, (2.5, 4, 5.5, 7), 10, base.ob)
        # Pad: 9 x 9 x 2 red, half a voxel proud of the base on every side (R5, R8); yellow 3 x 3 target.
        pad = Part("Pad")
        pad.box(-4.5, 4.5, 7.5, 9.5, -4.5, 4.5, RED)
        for n in ((1, 1, 0), (-1, 1, 0), (0, 1, 1), (0, 1, -1)):
            pad.bevel(n, 4.5 + 9.5 - 1)
        pad.paint(lambda x, y, z: y == 8.5 and -1.5 <= x < 1.5 and -1.5 <= z < 1.5, YELLOW)
        pad.build(pivot=(0, 7.5, 0), parent=base.ob)

    elif VARIANT == "mushroom":
        CAP, STEM, GILL = RED, "P09", "P10"
        # Stem (root): 4 x 4 cream, 1 buried in the cap; a sand foot one voxel wider.
        stem = Part("Stem")
        stem.box(-2, 2, 0, 6, -2, 2, STEM)
        stem.box(-2.5, 2.5, 0, 1, -2.5, 2.5, GILL)
        stem.build()
        # Cap: 9-wide octagon (R7b round part, >= 8 across), 5 tall, a 2 x 2 bevel round the crest.
        cap = Part("Cap")
        cap.box(-4.5, 4.5, 5, 10, -4.5, 4.5, CAP)
        for sx in (1, -1):
            for sz in (1, -1):
                cap.bevel((sx, 0, sz), 4.5 + 4.5 - 2)
        for n in ((1, 1, 0), (-1, 1, 0), (0, 1, 1), (0, 1, -1)):
            cap.bevel(n, 4.5 + 10 - 2)
        cap.paint(lambda x, y, z: y == 5, GILL)                                       # underside rim
        cap.paint(lambda x, y, z: y == 9 and -1.5 <= x < 1.5 and -1.5 <= z < 1.5, WHITE)   # crest spot, 3 x 3
        side = lambda a, b, y: abs(a + 0.5) == 4 and -1.5 <= b < 1.5 and 6 <= y < 8
        cap.paint(lambda x, y, z: side(x, z, y) or side(z, x, y), WHITE)            # a 3 x 2 spot per flat side
        cap.build(pivot=(0, 6, 0))                                                   # flat under root: nested parts export misplaced (R18)

    elif VARIANT == "trampoline":
        BLUE, MAT = "P39", "P06"
        # Frame (root): 9-wide octagon of padding, 2 tall, round a 5 x 5 opening; legs merged in.
        frame = Part("Frame")
        frame.box(-4.5, 4.5, 3, 5, -4.5, 4.5, BLUE)
        for k in list(frame.vox):
            if -2.5 <= k[0] < 2.5 and -2.5 <= k[2] < 2.5:
                del frame.vox[k]
        on_pad = lambda x, y, z: y >= 3
        for sx in (1, -1):
            for sz in (1, -1):
                frame.bevel((sx, 0, sz), 4.5 + 4.5 - 2, on_pad)
                frame.box(sx * 3 - 0.5, sx * 3 + 0.5, 0, 3, sz * 3 - 0.5, sz * 3 + 0.5, MAT)   # legs, 1 x 1
        frame.bevel((0, 1, 0), 5)
        frame.paint(lambda x, y, z: y >= 3 and abs(x + 0.5) > 1.5 and abs(z + 0.5) > 1.5, YELLOW)   # diagonal pads
        frame.build()
        # Mat: 6 x 6, half a voxel below the padding's top, its edges buried in the frame (R8).
        mat_ = Part("Mat")
        mat_.box(-3, 3, 3.5, 4.5, -3, 3, MAT)
        mat_.build(pivot=(0, 4, 0), parent=frame.ob)

    elif VARIANT == "board":
        WOOD, HINGE = "P24", "P26"
        # Base (root): 7 x 9 slate plate with a 6 x 2 x 2 walnut hinge block at the back.
        base = Part("Base")
        base.box(-3.5, 3.5, 0, 1, -4.5, 4.5, SLATE)
        base.box(-3, 3, 1, 3, -4.5, -2.5, HINGE)
        base.build()
        # Coil under the front of the plank: two 4 x 4 rings on the base, the third fixed under the plank.
        coil("Coil", 2, (1.5, 3), 10, base.ob, z=2)
        # Plank: 7 x 9 x 1, hinged on its back-bottom edge at the top of the hinge block, raised 15 deg (R6).
        plank = Part("Plank")
        plank.box(-3.5, 3.5, 3, 4, -4.5, 4.5, WOOD)
        ring(plank, 2, 2, 3, STEEL)
        plank.vox = {((k[0], k[1], k[2] + 2) if k[1] < 3 else k): c for k, c in plank.vox.items()}
        chev = lambda x, z: (z + 0.5) - (3 - abs(x + 0.5)) in (0, 1)
        plank.paint(lambda x, y, z: y == 3 and chev(x, z), RED)                     # one bold chevron, pointing +Z
        plank.build(pivot=(0, 3, -4.5), rot=(-15, 0, 0), parent=base.ob)

    else:
        raise SystemExit(f"unknown variant {VARIANT}")

# ---- Landing marker: four corner posts on the target tile, echoing the spring ---------------------------
# Posts, not a flat decal: the landing tile is usually grass, which would hide anything under ~4 voxels.
# They stand in the outer 3 voxels of the tile, leaving the middle clear for the mower to land in.
CORNERS = [(sx, sz) for sx in (1, -1) for sz in (1, -1)]


def top_bevel(part, x0, x1, y1, z0, z1, k=1):
    """Bevel the four top edges of the box x0..x1, z0..z1 topping out at y1 by k x k wedges (R7b)."""
    inside = lambda x, y, z: x0 <= x < x1 and z0 <= z < z1
    part.bevel((1, 1, 0), x1 + y1 - k, inside); part.bevel((-1, 1, 0), -x0 + y1 - k, inside)
    part.bevel((0, 1, 1), z1 + y1 - k, inside); part.bevel((0, 1, -1), -z0 + y1 - k, inside)


if PIECE in ("landing", "pair"):
    posts = []
    if VARIANT == "coil":
        # Mini launch pads: a 2 x 2 steel post under a 3 x 3 red cap with a yellow top, on a slate foot.
        for i, (sx, sz) in enumerate(CORNERS):
            cx, cz = sx * 3, sz * 3
            post = Part(f"Marker{i}")
            post.box(cx - 1.5, cx + 1.5, 0, 1, cz - 1.5, cz + 1.5, SLATE)
            post.box(cx - 1, cx + 1, 1, 5, cz - 1, cz + 1, STEEL)
            post.box(cx - 1.5, cx + 1.5, 5, 7, cz - 1.5, cz + 1.5, RED)
            top_bevel(post, cx - 1.5, cx + 1.5, 7, cz - 1.5, cz + 1.5)
            post.paint(lambda x, y, z: y == 6 and x == cx - 0.5 and z == cz - 0.5, YELLOW)
            posts.append(post)
    elif VARIANT == "mushroom":
        # A fairy ring: four little toadstools of different heights, 1 x 1 stems under 3 x 3 x 3 caps.
        # Spots (1 x 1, White) sit on the flat crest or the flat side row under the bevel (R7b: never on a
        # wedge), arranged differently on each so the ring doesn't read as copies.
        # Each spot: "top", or (side, along) with side in -x/+x/-z/+z and along -1..1 across that face.
        # The game camera sees the -x and +z sides; a few spots go round the back for other angles.
        SPOTS = (["top", ("+z", -1)],                       # right
                 [("-x", 1), ("-x", -1), ("+x", 0)],        # far
                 [("+z", 1), ("-x", 0), ("-z", 0)],         # near
                 ["top", ("-x", 1)])                        # left
        for i, ((sx, sz), h, spots) in enumerate(zip(CORNERS, (5, 4, 4, 6), SPOTS)):
            cx, cz = sx * 3, sz * 3
            post = Part(f"Marker{i}")
            post.box(cx - 0.5, cx + 0.5, 0, h - 2, cz - 0.5, cz + 0.5, "P09")
            post.box(cx - 1.5, cx + 1.5, h - 2, h + 1, cz - 1.5, cz + 1.5, RED)
            top_bevel(post, cx - 1.5, cx + 1.5, h + 1, cz - 1.5, cz + 1.5)
            post.paint(lambda x, y, z: y == h - 2, "P10")
            for sp in spots:
                if sp == "top":
                    k = (cx - 0.5, h, cz - 0.5)
                else:
                    side, along = sp
                    edge = 1.5 if side[0] == "+" else -1.5
                    k = ((cx + edge - (side[0] == "+"), h - 1, cz - 0.5 + along) if side[1] == "x"
                         else (cx - 0.5 + along, h - 1, cz + edge - (side[0] == "+")))
                post.vox[k] = WHITE
            posts.append(post)
    elif VARIANT == "trampoline":
        # Padded bollards: 2 x 2, 6 tall, in the trampoline's blue and yellow bands.
        for i, (sx, sz) in enumerate(CORNERS):
            cx, cz = sx * 3.5, sz * 3.5
            post = Part(f"Marker{i}")
            post.box(cx - 1, cx + 1, 0, 6, cz - 1, cz + 1, "P39")
            top_bevel(post, cx - 1, cx + 1, 6, cz - 1, cz + 1)
            post.paint(lambda x, y, z: y in (2, 3), YELLOW)
            posts.append(post)
    elif VARIANT == "board":
        # Wooden stakes, 8 tall, each with a red pennant pointing in toward the landing spot (tapers 3-1).
        for i, (sx, sz) in enumerate(CORNERS):
            cx, cz = sx * 3.5, sz * 3.5
            post = Part(f"Marker{i}")
            post.box(cx - 1, cx + 1, 0, 8, cz - 1, cz + 1, "P24")
            top_bevel(post, cx - 1, cx + 1, 8, cz - 1, cz + 1)
            for j, (y0, y1) in enumerate(((4, 7), (5, 6))):
                z0 = cz - sz * (2 + j) if sz > 0 else cz + 1 + j
                post.box(cx - 0.5, cx + 0.5, y0, y1, z0, z0 + 1, RED)
            posts.append(post)
    for post in posts:
        post.build()                                                                # flat under root (R18)
    landing_parts = [post.ob for post in posts]

# ---- preview: grass tiles underneath for scale (not part of the model) ----------------------------------
if TILES is None:
    TILES = (0, 10, 20) if PIECE == "pair" else (0,)
if PIECE == "pair":
    for ob in landing_parts:
        ob.location += Vector(B((0, 0, 20)))
for t in TILES:
    tile = Part(f"Tile{t}")
    tile.box(-5, 5, -3, 0, t - 5, t + 5, "P14")
    tile.paint(lambda x, y, z: y == -1, "P21" if t % 20 == 0 else "P19")    # alternate shades so tiles read
    tile.build()

ground, cam = make_preview(ground_y=-3)
if PIECE == "pair":
    target = Vector((0, -1.0, 0.3))
    VIEWS = {"three": (-45, 30, 3.6), "side": (-90, 10, 3.6)}
else:
    target = Vector((0, 0, 0.3))
    VIEWS = {"three": (-45, 30, 1.7), "front": (0, 10, 1.7), "side": (-90, 10, 1.7)}
for name, (az, el, scale) in VIEWS.items():
    a, e = math.radians(az), math.radians(el)
    d = Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e)))
    cam.location = target + d * 10
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.ortho_scale = scale
    sc.render.filepath = f"{OUT}_{name}.png"
    bpy.ops.render.render(write_still=True)
