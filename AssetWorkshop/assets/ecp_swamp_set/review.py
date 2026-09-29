"""Preview original kits on LOCAL vanilla references; no reference assets are exported."""
import sys, importlib.util
from pathlib import Path
import bpy
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'blender'))
from workshop import prefab, scene, preview
OUT=ROOT/'assets/ecp_swamp_set/out'
OUT.mkdir(parents=True,exist_ok=True)
CREATURES=[('Mire Jarl','characters/Draugr/Draugr_Elite.prefab',['ecp_mire_jarl','ecp_mire_crown']),
 ('Reed Stalker','characters/Draugr/Draugr.prefab',['ecp_reed_stalker']),
 ('Bog Maw','characters/Blob/Blob.prefab',['ecp_bog_maw']),
 ('Fen Crawler','characters/Neck/Neck.prefab',['ecp_fen_crawler']),
 ('Drowned Shade','characters/Wraith/Wraith.prefab',['ecp_drowned_shade'])]
views=[]
for i,(name,path,kits) in enumerate(CREATURES):
    scene.clear()
    if not all((ROOT/'assets'/k/'model.py').exists() for k in kits):
        continue
    for kit in kits:
        with bpy.data.libraries.load(str(ROOT/'assets'/kit/'out'/f'{kit}.blend'),link=False) as (src,dst):
            dst.objects=[kit]
        for ob in dst.objects:
            if ob: bpy.context.scene.collection.objects.link(ob)
    prefab.load(path)
    target=scene.join(scene.meshes(),name)
    preview._stage(target)
    cam=preview._camera()
    bpy.context.scene.render.resolution_x=650
    bpy.context.scene.render.resolution_y=850
    output=str(OUT/(kits[0]+'.png'))
    preview._render(cam,[target],Vector((.35,-1,.18)),True,output)
    views.append(output)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(kits[0]+'_review.blend')))
preview.grid(views,5,str(OUT/'swamp_lineup.png'))
