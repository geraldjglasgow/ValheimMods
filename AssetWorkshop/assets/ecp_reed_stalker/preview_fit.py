"""Local-only fitted comparison. Never export the combined reference mesh."""
import os, sys
ROOT=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0,os.path.join(ROOT,'blender'))
from workshop import prefab, scene, preview
import bpy
for asset,reference in [('ecp_reed_stalker','Characters/Draugr/Draugr.prefab'),('ecp_drowned_shade','Characters/Wraith/Wraith.prefab')]:
    scene.clear()
    path=os.path.join(ROOT,'assets',asset,'out',asset+'.blend')
    with bpy.data.libraries.load(path,link=False) as (src,dst):
        dst.objects=[asset]
    for ob in dst.objects:
        bpy.context.collection.objects.link(ob)
    prefab.load(reference)
    target=scene.join(scene.meshes(),'fitted_local_reference_only')
    out=os.path.join(ROOT,'assets',asset,'out','fitted')
    os.makedirs(out,exist_ok=True)
    preview.render_sheet(target,out)
