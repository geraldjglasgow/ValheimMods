"""Openable comparison scene: actual vanilla prefabs and our dressed 1.25x rig."""
import sys
from pathlib import Path

HERE=Path(__file__).resolve().parent
sys.path[:0]=[str(HERE),str(HERE.parents[1]/'blender')]
import bpy
from mathutils import Quaternion, Vector
from workshop import prefab
import stage

OUT=HERE/'out'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'ecp_bone_reaver.blend'))
rig=bpy.data.objects['BoneReaverRig']
rig.location.x=1.8
for name in ('PreviewCamera','PREVIEW_Ground','Key','Fill','Rim'):
    obj=bpy.data.objects.get(name)
    if obj:
        bpy.data.objects.remove(obj,do_unlink=True)
cam=stage.setup()
vanilla=prefab.load('Characters/Skeleton/Skeleton.prefab','VANILLA Skeleton — unchanged')
vanilla.location.x=-1.6
poison=prefab.load('Characters/Skeleton/Skeleton_Poison.prefab','VANILLA Rancid Remains — unchanged')
poison.location.x=-4.2
for text,pos in [('VANILLA / RANCID REMAINS',(-4.2,-.35,.02)),
                 ('VANILLA / SKELETON',(-1.6,-.35,.02)),
                 ('BONE REAVER / 1.25x',(1.8,-.8,.02))]:
    curve=bpy.data.curves.new(text,'FONT'); curve.body=text; curve.align_x='CENTER'; curve.size=.13
    obj=bpy.data.objects.new(text,curve); bpy.context.collection.objects.link(obj)
    obj.location=pos
    obj.data.materials.append(bpy.data.materials['reaver_edge_2'])
stage.shot(cam,OUT/'comparison.png',(5,-13,6),target=(-.9,0,1.3),scale=9)
# Start in a useful solid/material view; the camera gives the complete comparison.
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True); bpy.context.view_layer.objects.active=rig
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active
            space.shading.type='MATERIAL'
            space.overlay.show_extras=False
            space.region_3d.view_distance=8.5
            space.region_3d.view_location=(-.8,0,1.25)
            space.region_3d.view_rotation=Quaternion((.88,.46,.05,.10)).normalized()
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'compare_valheim_skeletons.blend'))
