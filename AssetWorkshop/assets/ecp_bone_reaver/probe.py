"""Inspect the installed game's skeleton, never a guessed humanoid stand-in."""
import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[1] / 'blender'))
import bpy
from workshop import prefab, unity, scene

scene.clear()
tree = prefab._Prefab('Characters/Skeleton/Skeleton.prefab')
root = prefab.load('Characters/Skeleton/Skeleton.prefab')
low, high = prefab.bounds(root)
report = {'bounds': [list(low), list(high)], 'bones': [], 'skins': []}
for ident, (kind, body) in tree.docs.items():
    if kind == 4 and tree.is_local(ident):
        report['bones'].append({'id': ident, 'name': tree.name(ident),
            'position': list(tree.world(ident).translation),
            'parent': unity.ref(unity.field(body, 'm_Father'))[0]})
    if kind == 137:
        report['skins'].append({'mesh': unity.asset_path(unity.ref(unity.field(body,'m_Mesh'))[1]),
            'bones': [tree.name(unity.ref(x)[0]) for x in unity.items(body,'m_Bones')]})
out = HERE / 'out'
out.mkdir(parents=True, exist_ok=True)
(out / 'reference.json').write_text(json.dumps(report, indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(out / 'reference.blend'))
print(json.dumps(report, indent=2))
