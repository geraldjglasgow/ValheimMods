"""Blender side of the style check: opens a built asset's .blend and writes its visual mesh as triangles to an .npz,
so the style check (plain Python) can measure surface, UV area, texel density and which texels the UVs cover.

    blender --background --factory-startup <asset>/out/<name>.blend --python codex/tools/meshdump.py --
        --name <name> --out <file.npz>

The .npz holds `positions` (triangles x 3 corners x xyz, world metres) and `uvs` (triangles x 3 corners x uv).
"""
import argparse
import sys

import bpy
import numpy as np


def parse():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(prog="meshdump.py")
    parser.add_argument("--name", required=True, help="the visual mesh object (the asset's name)")
    parser.add_argument("--out", required=True, help="the .npz to write")
    return parser.parse_args(argv)


def dump(obj, out):
    """Writes the object's triangles in world space with their UVs (zeros when it has no UV map)."""
    mesh = obj.data
    mesh.calc_loop_triangles()
    count = len(mesh.loop_triangles)
    corners, loops = np.empty(count * 3, np.int64), np.empty(count * 3, np.int64)
    mesh.loop_triangles.foreach_get("vertices", corners)
    mesh.loop_triangles.foreach_get("loops", loops)
    points = np.empty(len(mesh.vertices) * 3, np.float64)
    mesh.vertices.foreach_get("co", points)
    matrix = np.array(obj.matrix_world)
    points = points.reshape(-1, 3) @ matrix[:3, :3].T + matrix[:3, 3]
    uvs = np.zeros((len(mesh.loops), 2), np.float64)
    if mesh.uv_layers.active is not None:
        flat = np.empty(len(mesh.loops) * 2, np.float64)
        mesh.uv_layers.active.data.foreach_get("uv", flat)
        uvs = flat.reshape(-1, 2)
    np.savez(out, positions=points[corners].reshape(count, 3, 3), uvs=uvs[loops].reshape(count, 3, 2))
    print(f"WORKSHOP meshdump: {count} triangles -> {out}", flush=True)


args = parse()
target = bpy.data.objects.get(args.name)
if target is None or target.type != 'MESH':
    raise SystemExit(f"meshdump: no mesh object named {args.name} in {bpy.data.filepath}")
dump(target, args.out)
