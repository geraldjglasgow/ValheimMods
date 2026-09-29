"""Local-only vanilla body fitting review. Never exports game geometry."""
import os,sys,bpy
HERE=os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0,os.path.abspath(os.path.join(HERE,'../../blender')))
from workshop import scene,prefab,preview
for asset,base in [('ecp_bog_maw','Blob'),('ecp_fen_crawler','Neck')]:
    scene.clear()
    folder=os.path.join(os.path.dirname(HERE),asset)
    with bpy.data.libraries.load(os.path.join(folder,'out',asset+'.blend'),link=False) as (src,dst):dst.objects=[asset]
    for ob in dst.objects:
        if ob: bpy.context.scene.collection.objects.link(ob)
    body=prefab.load('Characters/'+base+'/'+base+'.prefab')
    target=scene.join(scene.meshes(),asset+'_review_only')
    out=os.path.join(folder,'out','fitting');os.makedirs(out,exist_ok=True)
    preview.render_sheet(target,out)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(out,'fitting.blend'))
