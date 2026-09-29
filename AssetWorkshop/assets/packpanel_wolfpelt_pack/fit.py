"""Fit check: the built Wolfpelt Pack on the game's player body, rendered to out/fit.png. Preview only, never exported.

    blender --background --factory-startup --python assets/packpanel_wolfpelt_pack/fit.py

Loads out/packpanel_wolfpelt_pack.blend (building it first when it is missing), imports the player's body from the
local reference export in its bind pose, dressed in the wolf armour's skin-layer textures (preview only), and hangs
the pack on the Spine2 point (0, 0.0433, 1.4524).
Sheet, top row: back three-quarter | straight back (the follow camera's view) | close side section of the back;
bottom row: side (the near arm cut away) | front | from above and behind.
It also prints how far the pack's front sits from the skin down the back, any strap point inside the body, the
pack's highest point and its width at each height.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.dont_write_bytecode = True
sys.path[:0] = [HERE, os.path.join(HERE, "..", "..", "blender")]

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402
from mathutils.bvhtree import BVHTree  # noqa: E402

from workshop import pipeline, preview, reference, scene  # noqa: E402

NAME = os.path.basename(HERE)
OUT = os.path.join(HERE, "out")
SPINE2 = Vector((0.0, 0.0433, 1.4524))
SKIN = "Characters/Player/model/old_PlayerCharacter2/PlayerCharacter_01.png"
WOLF = "GameElements/Items/armor/_res/SilverArmour/"
TILE = 640


def main():
    pack = _pack()
    body = _body()
    pack.location = SPINE2
    bpy.context.view_layer.update()
    low, high = scene.bounds([body])
    print(f"FIT body bounds {tuple(round(v, 3) for v in low)} to {tuple(round(v, 3) for v in high)}, "
          f"height {high.z - low.z:.3f} m")
    _report(pack, body)
    _extent(pack)
    _stage()
    tiles = _views(pack, body)
    preview.grid(tiles, 3, os.path.join(OUT, "fit.png"))
    print("FIT wrote", os.path.join(OUT, "fit.png"))


def _pack():
    blend = os.path.join(OUT, NAME + ".blend")
    if not os.path.exists(blend):
        pipeline.run(HERE, render_preview=False)
    bpy.ops.wm.open_mainfile(filepath=blend)
    pack = bpy.data.objects[NAME]
    for mat in pack.data.materials:     # fully matte, as the game's non-metallic material is
        bsdf = next(n for n in mat.node_tree.nodes if n.bl_idname == 'ShaderNodeBsdfPrincipled')
        bsdf.inputs['Roughness'].default_value = 1.0
        bsdf.inputs['Specular IOR Level'].default_value = 0.0
    return pack


def _body():
    """The skinned body mesh sits in its node's space: -90 degrees about X, scale 100, under Visual (scale 0.95)."""
    mat = _layered("preview_body", [SKIN, WOLF + "SilverArmour_Skin_d.png", WOLF + "SilverArmour_Skin_Legs_d.png"])
    body = reference.mesh("Characters/Player/model/body.asset", "preview_body", mat)
    body.rotation_euler = (math.radians(-90), 0.0, 0.0)
    body.scale = (95.0, 95.0, 95.0)
    body.data.shade_smooth()
    return body


def _layered(name, paths):
    """The game's textures stacked on the body's UVs, each over the last by its alpha, point-filtered."""
    mat = bpy.data.materials.new(name)
    tree = mat.node_tree
    bsdf = next(n for n in tree.nodes if n.bl_idname == 'ShaderNodeBsdfPrincipled')
    bsdf.inputs['Roughness'].default_value = 0.85
    colour = None
    for path in paths:
        tex = tree.nodes.new('ShaderNodeTexImage')
        tex.image = bpy.data.images.load(os.path.join(reference.ROOT, path), check_existing=True)
        tex.interpolation = 'Closest'
        if colour is None:
            colour = tex.outputs['Color']
            continue
        mix = tree.nodes.new('ShaderNodeMix')
        mix.data_type = 'RGBA'
        sockets = [s for s in mix.inputs if s.type == 'RGBA']
        tree.links.new(tex.outputs['Alpha'], mix.inputs[0])
        tree.links.new(colour, sockets[0])
        tree.links.new(tex.outputs['Color'], sockets[1])
        colour = next(s for s in mix.outputs if s.type == 'RGBA')
    tree.links.new(colour, bsdf.inputs['Base Color'])
    return mat


def _tree(obj):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    mesh = obj.evaluated_get(depsgraph).to_mesh()
    tree = BVHTree.FromPolygons([obj.matrix_world @ v.co for v in mesh.vertices], [p.vertices for p in mesh.polygons])
    obj.evaluated_get(depsgraph).to_mesh_clear()
    return tree


def _report(pack, body):
    """Gap between skin and pack along +Y from inside the body (negative: pressed in), and pack points in the body."""
    skin, bag = _tree(body), _tree(pack)
    for x in (0.0, 0.1, 0.15):
        cells = []
        for z in [1.15 + 0.05 * i for i in range(10)]:
            origin = Vector((x, 0.0, z))
            back = skin.ray_cast(origin, Vector((0, 1, 0)))[0]
            front = bag.ray_cast(origin, Vector((0, 1, 0)))[0]
            cells.append(f"{z:.2f}:{(front.y - back.y) * 1000:+.0f}" if back and front else f"{z:.2f}:  -")
        print(f"FIT gap mm at x={x:.2f}  " + "  ".join(cells))
    deep = {}
    for v in pack.data.vertices:
        point = pack.matrix_world @ v.co
        hit, normal, _, dist = skin.find_nearest(point)
        if hit is not None and normal.dot(point - hit) < 0 and dist > 0.008:
            band = round(point.z, 1)
            deep[band] = deep.get(band, 0) + 1
    print(f"FIT pack points more than 8 mm inside the skin, by height: {dict(sorted(deep.items()))}")


def _extent(pack):
    """The pack's highest point, and its half-width and depth range in bands of height."""
    points = [pack.matrix_world @ v.co for v in pack.data.vertices]
    top = max(points, key=lambda p: p.z)
    print(f"FIT highest point {tuple(round(c, 3) for c in top)}")
    top = max((p for p in points if p.y > 0.165), key=lambda p: p.z)
    print(f"FIT highest point behind the neck (the fur, not the shoulder straps) {tuple(round(c, 3) for c in top)}")
    for z in [1.10 + 0.05 * i for i in range(12)]:
        band = [p for p in points if z <= p.z < z + 0.05]
        if band:
            print(f"FIT z {z:.2f}-{z + 0.05:.2f}: |x| <= {max(abs(p.x) for p in band):.3f}, "
                  f"y {min(p.y for p in band):.3f} to {max(p.y for p in band):.3f}")


def _stage():
    s = bpy.context.scene
    s.render.engine = 'BLENDER_EEVEE'
    s.eevee.taa_render_samples = 32
    s.render.resolution_x = s.render.resolution_y = TILE
    s.render.film_transparent = False
    s.view_settings.view_transform = 'Standard'
    s.world = bpy.data.worlds.new("fit")
    background = next(n for n in s.world.node_tree.nodes if n.bl_idname == 'ShaderNodeBackground')
    background.inputs['Color'].default_value = (0.45, 0.52, 0.62, 1.0)
    background.inputs['Strength'].default_value = 0.7
    sun = bpy.data.objects.new("fit_sun", bpy.data.lights.new("fit_sun", 'SUN'))
    sun.data.energy = 2.5
    s.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(50), 0.0, math.radians(150))
    bpy.ops.mesh.primitive_plane_add(size=20, location=(0, 0, 0))
    ground = bpy.data.materials.new("fit_ground")
    next(n for n in ground.node_tree.nodes if n.bl_idname == 'ShaderNodeBsdfPrincipled').inputs[
        'Base Color'].default_value = (0.16, 0.18, 0.13, 1.0)
    bpy.context.active_object.data.materials.append(ground)


def _views(pack, body):
    camera = bpy.data.objects.new("fit_camera", bpy.data.cameras.new("fit_camera"))
    bpy.context.scene.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    whole = scene.bounds([body])
    upper = (Vector((-0.45, -0.2, 0.8)), Vector((0.45, 0.45, 1.95)))
    back = (Vector((0.0, 0.0, 0.95)), Vector((0.0, 0.45, 1.75)))
    above = (Vector((-0.3, -0.25, 1.3)), Vector((0.3, 0.45, 1.75)))
    return [
        _shot(camera, "back_three_quarter", Vector((1.0, 1.5, 0.45)), upper, ortho=False),
        _shot(camera, "back", Vector((0.0, 1.0, 0.12)), upper, ortho=False),
        _shot(camera, "side_close", Vector((-1.0, 0.0, 0.0)), back, clip=0.235),
        _shot(camera, "side", Vector((-1.0, 0.0, 0.0)), whole, clip=0.235),
        _shot(camera, "front", Vector((0.0, -1.0, 0.0)), whole),
        _shot(camera, "above", Vector((0.25, 0.45, 1.0)), above, ortho=False),
    ]


def _shot(camera, name, direction, box, ortho=True, clip=None):
    """One view of `box`; with `clip`, everything nearer the camera than that distance from the centre plane is cut
    away (the near arm, which the bind pose holds out to the side)."""
    low, high = box
    centre, radius = (low + high) / 2, (high - low).length / 2
    direction = direction.normalized()
    camera.data.type = 'ORTHO' if ortho else 'PERSP'
    camera.data.lens = 50
    camera.data.ortho_scale = max(high.z - low.z, 0.3) * 1.08
    distance = 4.0 if ortho else radius / math.sin(camera.data.angle / 2) * 0.9
    camera.location = centre + direction * distance
    camera.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
    camera.data.clip_start = distance - clip if clip else 0.05
    camera.data.clip_end = 100.0
    path = os.path.join(OUT, f"fit_{name}.png")
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


main()
