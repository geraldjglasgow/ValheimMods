import sys, json
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'blender'))
from workshop import prefab,scene,preview
OUT=Path(__file__).parent/'out'; OUT.mkdir(exist_ok=True)
for name in ['Fenring','Hatchling']:
    scene.clear()
    path=f'Characters/{name}/{name}.prefab'
    tree=prefab._Prefab(path)
    info={tree.name(i):[list(row) for row in tree.world(i)] for i,(cls,b) in tree.docs.items() if cls==4 and tree.is_local(i) and any(s in tree.name(i).lower() for s in ['head','neck','spine'])}
    (OUT/f'{name}_fit.json').write_text(json.dumps(info,indent=2))
    root=prefab.load(path)
    objects=prefab.meshes(root)
    preview._stage(objects[0]); cam=preview._camera()
    preview._render(cam,objects,Vector((1,-2,.5)),True,str(OUT/f'{name}.png'))
    print(name, json.dumps(info))
