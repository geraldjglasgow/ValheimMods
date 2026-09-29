r"""Demo asset for the codex tools: a small iron-banded round shield of painted planks, after the game's ShieldWood and
ShieldBanded (round, 0.8 to 1 m, a painted face, an iron rim, iron bands, a domed boss, a batten and grip behind).

Built to its category's numbers (shield.round in codex/data/items.json: 128 px, 108 to 2,288 triangles, 50 to 102 px
per metre, 0.74 to 1.44 m), painted with the codex's recipes (blender/workshop/paint.py), each of which sets its paint
region and family (primary: the face, red-dyed planks; wood: the bare back and edge, wood.planks; metal: rim, bands
and boss, metal.iron; leather: grip and arm strap, leather.leather), so one build proves the regions mask, the
recolour variants (variants.json), the style check (CATEGORY) and the lineup:

    .\build.ps1 -Asset codex\tools\demo_asset -Lineup

Z up, the face towards -Y, the origin at the grip behind the boss. Plank seams, grain and rivets are paint, not
geometry: at 128 px a rivet is one texel.
"""
import math

import bmesh
import bpy

from workshop import paint, shapes

CATEGORY = "shield.round"
TEXTURE_SIZE = 128
AO_STRENGTH = 0.2        # the paint recipes paint their own hollows (codex/paint.md)
NORMAL_FROM_ALBEDO = 3.0  # the game's normal maps are the albedo's relief; wood is 3 to 4
DENSITY = 56.0           # px per metre: the category's median (shield.round)

RADIUS = 0.43            # the face's radius, metres
THICK = 0.03             # plank thickness
FACE_Y = -0.045          # the planks' middle; the grip (origin) is behind them
RIM = 0.02               # how far the iron rim stands out round the edge
BANDS = (-0.19, 0.19)    # heights of the two iron bands across the face
PLANK = RADIUS * 2 / 6   # six planks across
SIDES = 24


def build():
    planks = dict(axis="Z", pattern=("seams", PLANK), density=DENSITY)
    # a dyed face: the style check judges it against bare planks, so its saturation reads HIGH by design
    face = paint.make("wood.planks", "demo_face", region="primary", tint="#5c2c20", **planks)
    wood = paint.make("wood.planks", "demo_wood", **planks)
    iron = paint.make("metal.iron", "demo_iron", density=DENSITY)
    leather = paint.make("leather.leather", "demo_leather", density=DENSITY)
    _face(face, wood)
    _rim(iron)
    _bands(iron)
    _boss(iron)
    _back(wood, leather)


def _face(painted, bare):
    """The plank disc: its front cap painted, the back and edge bare wood."""
    disc = shapes.cylinder("face", RADIUS, THICK, (0, FACE_Y, 0), (math.pi / 2, 0, 0), painted, vertices=SIDES)
    disc.data.materials.append(bare)
    turn = disc.rotation_euler.to_matrix()
    for polygon in disc.data.polygons:
        polygon.material_index = 0 if (turn @ polygon.normal).y < -0.9 else 1


def _rim(material):
    """An iron band round the edge, a rounded U lapping 2 cm over the face and the back, its lips down on the wood."""
    face_front, face_back = FACE_Y - THICK / 2, FACE_Y + THICK / 2
    front, back, inner = face_front - 0.006, face_back + 0.006, RADIUS - 0.02
    section = [(inner, face_front + 0.001), (inner, front), (RADIUS + RIM * 0.5, front - 0.003),
               (RADIUS + RIM, FACE_Y), (RADIUS + RIM * 0.5, back + 0.003), (inner, back), (inner, face_back - 0.001)]
    _lathe("rim", section, material)


def _lathe(name, section, material):
    """Turns a (radius, y) section, front to back over the outside, round the Y axis into a ring of SIDES segments;
    that order winds every face outward."""
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    rings = [[bm.verts.new((r * math.cos(a), y, r * math.sin(a))) for r, y in section]
             for a in (2 * math.pi * i / SIDES for i in range(SIDES))]
    for i in range(SIDES):
        ring, following = rings[i], rings[(i + 1) % SIDES]
        for j in range(len(section) - 1):
            bm.faces.new((ring[j], ring[j + 1], following[j + 1], following[j]))
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    mesh.materials.append(material)
    return obj


def _bands(material):
    """Two flat iron straps across the face, ending under the rim."""
    front = FACE_Y - THICK / 2 - 0.003
    for i, z in enumerate(BANDS):
        chord = 2 * math.sqrt(RADIUS ** 2 - z ** 2) - 0.02
        shapes.box(f"band{i}", (chord, 0.008, 0.06), (0, front, z), material=material, bevel=0.002)


def _boss(material):
    """A low iron dome on a round plate in the middle of the face."""
    front = FACE_Y - THICK / 2
    shapes.cylinder("boss_plate", 0.11, 0.008, (0, front - 0.004, 0), (math.pi / 2, 0, 0), material, vertices=12)
    dome = shapes.sphere("boss", 0.085, (0, front - 0.006, 0), material, segments=12, rings=6)
    dome.scale = (1.0, 0.6, 1.0)


def _back(wood, leather):
    """A batten across the back, the grip bar on two blocks (the origin is the grip's middle), an arm strap."""
    back = FACE_Y + THICK / 2
    shapes.box("batten", (0.66, 0.025, 0.08), (0, back + 0.0125, 0), material=wood, bevel=0.004)
    for x in (-0.085, 0.085):
        shapes.box(f"grip_block{x}", (0.03, 0.035, 0.05), (x, back + 0.04, 0), material=wood)
    shapes.cylinder("grip", 0.017, 0.15, (0, 0, 0), (0, math.pi / 2, 0), leather, vertices=8)
    shapes.box("arm_strap", (0.035, 0.01, 0.26), (-0.2, back + 0.03, 0), material=leather)
