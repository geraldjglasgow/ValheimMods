"""LOCAL ONLY: assemble our baked kits with game's own unchanged textured bodies."""
import sys
from pathlib import Path
import bpy
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'blender'))
from workshop import prefab,scene,preview
for kit,name in [('ecp_frostfang','Wolf'),('ecp_rimeback','Lox'),('ecp_ice_crawler','Neck')]:
    scene.clear()
    base=prefab.load(f'characters/{name}/{name}.prefab')
    source=ROOT/'assets'/kit/'out'/f'{kit}.blend'
    with bpy.data.libraries.load(str(source),link=False) as (a,b):b.objects=[kit]
    for obj in b.objects:
        bpy.context.collection.objects.link(obj)
        for material in obj.data.materials:
            for node in material.node_tree.nodes:
                if node.type=='TEX_IMAGE':node.interpolation='Closest'
    body=scene.join(prefab.meshes(base)+b.objects,name)
    out=ROOT/'assets'/kit/'out'/'fitted'
    out.mkdir(parents=True,exist_ok=True)
    preview.render_sheet(body,str(out))
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'local_reference.blend'))
