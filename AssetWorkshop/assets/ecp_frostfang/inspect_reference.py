import sys
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'blender'))
from workshop import prefab,scene,preview
for name in ['Wolf','Lox','Neck']:
    scene.clear()
    base=prefab.load(f'characters/{name}/{name}.prefab')
    print('REF',name,'bounds',prefab.bounds(base),flush=True)
    for o in base.children_recursive:
        if any(x in o.name.lower() for x in ['spine','head','chest','neck','pelvis']):
            print('BONE',o.name,tuple(o.matrix_world.translation),flush=True)
    body=scene.join(prefab.meshes(base),name)
    out=Path(__file__).parent/'out'/'reference'/name
    out.mkdir(parents=True,exist_ok=True)
    preview.render_sheet(body,str(out))
