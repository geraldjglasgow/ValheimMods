"""Local reference review only. Never export this scene into a shipping bundle."""
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'blender'))
from workshop import prefab, preview, scene

OUT = Path(__file__).resolve().parent / 'out'
KINDS = [
    ('Frostfang', 'ecp_frostfang', 'characters/Wolf/Wolf.prefab', 1.65),
    ('Rimeback', 'ecp_rimeback', 'characters/Lox/Lox.prefab', .65),
    ('Scree Wing', 'ecp_scree_wing', 'characters/Hatchling/Hatchling.prefab', 1.1),
    ('Cairn Wight', 'ecp_cairn_wight', 'characters/Fenring/Fenring.prefab', .9),
    ('Ice Crawler', 'ecp_ice_crawler', 'characters/Neck/Neck.prefab', 2.0),
]


def attachment(asset, root):
    path = ROOT / 'assets' / asset / 'out' / (asset + '.blend')
    with bpy.data.libraries.load(str(path), link=False) as (source, target):
        target.objects = [n for n in source.objects if n == asset]
    obj = target.objects[0]
    bpy.context.collection.objects.link(obj)
    obj.parent = root
    for material in obj.data.materials:
        for node in material.node_tree.nodes:
            if node.type == 'TEX_IMAGE':
                node.interpolation = 'Closest'
                if node.image:
                    node.image.filepath = str(path.parent / (asset + '_albedo.png'))
                    node.image.reload()
    return obj


def run():
    OUT.mkdir(parents=True, exist_ok=True)
    scene.clear()
    creatures, measurements = [], {}
    cursor = 0.0
    for title, asset, source, scale in KINDS:
        root = prefab.load(source, root_name=title)
        attachment(asset, root)
        root.scale *= scale
        bpy.context.view_layer.update()
        low, high = prefab.bounds(root)
        root.location.x += cursor - low.x
        root.location.z -= low.z
        if asset == 'ecp_scree_wing':
            root.location.z += .35
        cursor += high.x - low.x + 1.1
        creatures.extend(prefab.meshes(root))
        measurements[title] = {'scale': scale, 'size': list(high-low)}
    board = scene.join(creatures, 'Mountain creatures - local reference review')
    preview._stage(board)
    camera = preview._camera()
    bpy.context.scene.render.resolution_x = 2400
    bpy.context.scene.render.resolution_y = 900
    preview._render(camera, [board], Vector((.1, -1, .18)), True,
                    str(OUT / 'mountain_lineup.png'))
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'mountain_review.blend'))
    (OUT / 'measurements.json').write_text(json.dumps(measurements, indent=2))


if __name__ == '__main__':
    run()
