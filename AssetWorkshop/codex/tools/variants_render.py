"""Blender side of recolour.py: the asset once per albedo, side by side on the lineup's stage (sky, sun, dark earth,
point filtered), rendered from the front three-quarters and from behind.

    blender --background --factory-startup --python codex/tools/variants_render.py -- --blend <out/name.blend>
        --name <name> --albedos <png>,<png>,... --labels <text>,<text>,... --out <folder>

Writes <folder>/turn.png and <folder>/back.png.
"""
import argparse
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.normpath(os.path.join(HERE, "..", "..", "blender")))
sys.dont_write_bytecode = True

import bpy  # noqa: E402
from workshop import lineup  # noqa: E402


def parse():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(prog="variants_render.py")
    for name in ("--blend", "--name", "--albedos", "--labels", "--out"):
        parser.add_argument(name, required=True)
    return parser.parse_args(argv)


def dressed(base, albedo, label, first):
    """The base object (first) or a copy of it, wearing a copy of its material with this albedo."""
    obj = base if first else base.copy()
    if not first:
        obj.data = base.data.copy()
        bpy.context.scene.collection.objects.link(obj)
    mat = base.data.materials[0].copy()
    for node in mat.node_tree.nodes:
        if node.bl_idname == 'ShaderNodeTexImage' and node.image and node.image.name.endswith("_albedo"):
            node.image = bpy.data.images.load(albedo, check_existing=True)
    obj.data.materials[0] = mat
    obj.name = "variant_" + label
    return {"root": obj, "meshes": [obj], "label": label, "grip": None, "item": False}


def main(args):
    bpy.ops.wm.open_mainfile(filepath=args.blend)
    for obj in [o for o in bpy.data.objects if o.name != args.name]:
        bpy.data.objects.remove(obj, do_unlink=True)
    base = bpy.data.objects[args.name]
    pairs = list(zip(args.albedos.split(","), args.labels.split(",")))
    entries = [dressed(base, albedo, label, i == 0) for i, (albedo, label) in enumerate(pairs)]
    low, high = lineup.layout(entries)
    lineup.point_filter()
    lineup.stage(high.x - low.x)
    lineup.labels(entries, high.z - low.z)
    os.makedirs(args.out, exist_ok=True)
    meshes = [e["root"] for e in entries]
    names = [o for o in bpy.data.objects if o.name.startswith("label_")]
    lineup.shot("turn", meshes + names, os.path.join(args.out, "turn.png"))
    for label in names:
        label.hide_render = True
    lineup.shot("back", meshes, os.path.join(args.out, "back.png"))


main(parse())
