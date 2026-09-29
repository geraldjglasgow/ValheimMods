"""Local-only reference fit check; no game meshes are exported."""
import sys
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'blender'))
from workshop import prefab,scene,preview
for kit,base in [('ecp_cairn_wight','Fenring'),('ecp_scree_wing','Hatchling')]:
    scene.clear()
    out=ROOT/'assets'/kit/'out'
    with bpy.data.libraries.load(str(out/f'{kit}.blend')) as (src,dst):
        dst.objects=[n for n in src.objects if n==kit]
    for ob in dst.objects:
        if ob:bpy.context.collection.objects.link(ob)
    root=prefab.load(f'Characters/{base}/{base}.prefab')
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    preview._stage(prefab.meshes(root)[0]);cam=preview._camera()
    bpy.context.scene.render.resolution_x=1000;bpy.context.scene.render.resolution_y=1000
    preview._render(cam,objects,Vector((.55,-1,.22)),True,str(out/'fitted.png'))
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'fitted.blend'))
