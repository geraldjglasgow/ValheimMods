"""Local-only vanilla reference board; never exports game meshes into bundles."""
import os, sys, json
from pathlib import Path
import bpy
from mathutils import Vector
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'blender'))
from workshop import prefab, scene, preview
OUT = ROOT / 'assets/ecp_swamp_set/out'
OUT.mkdir(parents=True, exist_ok=True)
scene.clear()
paths = ['characters/Draugr/Draugr_Elite.prefab', 'characters/Draugr/Draugr.prefab',
         'characters/Blob/Blob.prefab', 'characters/Neck/Neck.prefab', 'characters/Wraith/Wraith.prefab']
all_meshes = []
info = {}
for i, path in enumerate(paths):
    root = prefab.load(path)
    bpy.context.view_layer.update()
    low, high = prefab.bounds(root)
    info[path] = {'bounds': [list(low), list(high)], 'bones': {
        o.name: list(o.matrix_world.translation) for o in root.children_recursive
        if o.type == 'EMPTY' and any(s in o.name.lower() for s in ('head', 'spine', 'hip', 'neck'))}}
    root.location.x += i * 3.2
    all_meshes.extend(prefab.meshes(root))
with open(OUT / 'reference_measurements.json', 'w') as f:
    json.dump(info, f, indent=2)
board = scene.join(all_meshes, 'Vanilla swamp reference')
preview._stage(board)
cam = preview._camera()
bpy.context.scene.render.resolution_x = 1800
bpy.context.scene.render.resolution_y = 650
preview._render(cam, [board], Vector((0,-1,.22)), True, str(OUT/'vanilla_reference.png'))
print('SWAMP REFERENCE', json.dumps(info))
