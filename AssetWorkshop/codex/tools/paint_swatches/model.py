"""Test asset for the paint recipes (workshop/paint.py): one simple shape per material family, at the size of a game
thing of that family, painted with its recipe. codex/tools/paint_check.py builds it and compares the bake with the
game's textures.

    PAINT_SWATCH=wood.planks   one family alone, its atlas sized to the family's measured texel density
    (unset)                    every family in a row, for the preview sheet
"""
import math
import os

import bmesh
import bpy

from workshop import paint, paint_specs, scene, shapes

TEXTURE_SIZE = 1024
NORMAL_MAP = True
AO_STRENGTH = float(os.environ.get("PAINT_SWATCH_AO", "0.2"))   # the recipes paint their own hollows
PACKING = 0.55          # share of the atlas smart UV project fills on these shapes (measured by paint_check)
MIN_ATLAS = 128         # pixels: each single-family swatch is grown until its atlas is at least this big


def build():
    global TEXTURE_SIZE
    family = os.environ.get("PAINT_SWATCH")
    if family:
        part = _shape(family, (0.0, 0.0, 0.0))
        spec = paint_specs.spec(family)
        _grow(part, (MIN_ATLAS / spec["density"]) ** 2 * PACKING)
        TEXTURE_SIZE = _atlas(_area(part), spec["density"])
        density = TEXTURE_SIZE * math.sqrt(PACKING / _area(part))
        part.data.materials.append(paint.make(family, family.replace(".", "_"), density=density))
        return
    for index, family in enumerate(paint_specs.FAMILIES):
        part = _shape(family, ((index % 9) * 1.6, (index // 9) * 1.6, 0.0))
        part.data.materials.append(paint.make(family, family.replace(".", "_")))


def _grow(obj, area):
    """Scales the part up until its surface is `area` square metres, so the swatch's atlas holds enough of the
    family's blotches for its statistics to settle (the game's own textures are mostly 128 px and up)."""
    have = _area(obj)
    if have < area:
        obj.scale = (math.sqrt(area / have),) * 3
        scene.select_only([obj])
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)


def _atlas(area, density):
    """The power of two whose atlas gives the family's density on this shape."""
    wanted = density * math.sqrt(area / PACKING)
    return int(2 ** max(5, min(10, round(math.log2(wanted)))))


def _area(obj):
    mesh = obj.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh()
    area = sum(p.area for p in mesh.polygons)
    obj.to_mesh_clear()
    return area


SHAPES = {
    "wood": ("panel", (1.0, 0.1, 1.0)), "wood.logs": ("log", (0.15, 1.2)), "wood.bark": ("log", (0.25, 1.5)),
    "wood.fine": ("panel", (0.7, 0.05, 0.35)), "metal": ("plate", (0.5, 0.06, 0.4)), "metal.copper": ("rock", 0.3),
    "metal.tin": ("rock", 0.3), "bone.bone": ("bone", (0.07, 0.8)), "bone": ("horn", (0.08, 0.5)),
    "leather": ("panel", (0.45, 0.02, 0.35)), "leather.hide": ("panel", (1.2, 0.02, 0.8)),
    "leather.fur": ("rock", 0.4), "cloth.linen": ("panel", (1.0, 0.01, 1.4)), "cloth.rope": ("log", (0.025, 0.8)),
    "stone": ("block", (0.9, 0.6, 0.6)), "stone.stone": ("rock", 0.6), "thatch": ("panel", (1.2, 0.15, 1.0)),
    "crystal.crystal": ("prism", (0.15, 0.6)), "crystal": ("rock", 0.45), "chitin": ("dome", 0.35),
    "skin.skin": ("rock", 0.55), "skin.flesh": ("rock", 0.15), "veg.mushroom": ("dome", 0.15),
    "veg.leaves": ("panel", (1.0, 0.005, 1.0)), "veg.moss": ("dome", 0.5),
}


def _shape(family, at):
    """The family's shape (its own entry, else its group's) at `at`, standing on the ground."""
    kind, size = SHAPES.get(family) or SHAPES[family.split(".")[0]]
    return globals()["_" + kind](family.replace(".", "_"), size, at)


def _panel(name, size, at):
    return shapes.box(name, size, (at[0], at[1], at[2] + size[2] / 2), bevel=0.01)


def _block(name, size, at):
    return shapes.box(name, size, (at[0], at[1], at[2] + size[2] / 2), bevel=0.03)


def _plate(name, size, at):
    return shapes.box(name, size, (at[0], at[1], at[2] + size[2] / 2), bevel=0.012)


def _log(name, size, at):
    radius, length = size
    return shapes.cylinder(name, radius, length, (at[0], at[1], at[2] + length / 2), vertices=10)


def _prism(name, size, at):
    radius, length = size
    obj = shapes.cylinder(name, radius, length, (at[0], at[1], at[2] + length / 2), vertices=6)
    top = max(v.co.z for v in obj.data.vertices)
    for vertex in obj.data.vertices:
        if vertex.co.z >= top - 1e-4:
            vertex.co.x, vertex.co.y, vertex.co.z = vertex.co.x * 0.2, vertex.co.y * 0.2, vertex.co.z + radius
    return obj


def _bone(name, size, at):
    """A long bone: a shaft with a knuckle at each end, joined into one object."""
    radius, length = size
    shaft = shapes.cylinder(name, radius, length, (at[0], at[1], at[2] + length / 2 + radius * 2), vertices=10)
    knobs = [shapes.sphere(name + "_end", radius * 1.9, (at[0], at[1], at[2] + end), segments=10, rings=6)
             for end in (radius * 2, length + radius * 2)]
    scene.select_only([shaft] + knobs, active=shaft)
    bpy.ops.object.join()
    return shaft


def _horn(name, size, at):
    radius, length = size
    bpy.ops.mesh.primitive_cone_add(vertices=10, radius1=radius, radius2=radius * 0.15, depth=length,
                                    location=(at[0], at[1], at[2] + length / 2))
    obj = bpy.context.active_object
    obj.name = name
    return obj


def _rock(name, radius, at):
    """A lumpy boulder: an icosphere pushed in and out by a fixed pattern."""
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=radius, location=(at[0], at[1], at[2] + radius * 0.8))
    obj = bpy.context.active_object
    obj.name = name
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    for vertex in mesh.verts:
        x, y, z = vertex.co
        vertex.co *= 1.0 + 0.12 * math.sin(x * 9.1 + y * 5.3) * math.cos(z * 7.7 - x * 3.1)
    mesh.to_mesh(obj.data)
    mesh.free()
    return obj


def _dome(name, radius, at):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=radius, location=(at[0], at[1], at[2]))
    obj = bpy.context.active_object
    obj.name = name
    obj.scale.z = 0.6
    scene.select_only([obj])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    mesh = bmesh.new()
    mesh.from_mesh(obj.data)
    bmesh.ops.delete(mesh, geom=[v for v in mesh.verts if v.co.z < -1e-4], context='VERTS')
    mesh.to_mesh(obj.data)
    mesh.free()
    return obj
