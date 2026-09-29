"""Blender side of paint_check: opens each swatch .blend it is given and writes the visual mesh's UV triangles and
world area to one JSON file, so the check can find exactly which texels the swatch covers and its texel density.

    blender --background --factory-startup --python paint_check_uv.py -- out.json a.blend b.blend ...
"""
import json
import sys

import bpy


def triangles(obj):
    """[(u0, v0, u1, v1, u2, v2)] of the object's UV triangles and its world surface area in square metres."""
    mesh = obj.data
    mesh.calc_loop_triangles()
    uv = mesh.uv_layers.active.data
    found = [[c for index in tri.loops for c in uv[index].uv] for tri in mesh.loop_triangles]
    area = sum(tri.area for tri in mesh.loop_triangles) * (obj.matrix_world.to_scale().x ** 2)
    return found, area


def main():
    argv = sys.argv[sys.argv.index("--") + 1:]
    out, blends = argv[0], argv[1:]
    result = {}
    for path in blends:
        bpy.ops.wm.open_mainfile(filepath=path)
        meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH' and not o.name.startswith("col_")]
        visual = next((o for o in meshes if o.name == "paint_swatches"), meshes[0])
        tris, area = triangles(visual)
        result[path] = {"triangles": tris, "area": area}
    with open(out, "w", encoding="utf-8") as handle:
        json.dump(result, handle)


main()
