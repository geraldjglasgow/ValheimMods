"""Quick modelling review without re-baking the action library."""
import sys
from pathlib import Path
HERE=Path(__file__).resolve().parent
sys.path[:0]=[str(HERE),str(HERE.parents[1]/'blender')]
import bpy
from workshop import scene
import body, equipment, stage
scene.clear()
rig,_=body.build()
equipment.build(rig)
for obj in rig.children:
    if obj.type=='MESH' and not obj.vertex_groups.get('axe'): obj.hide_render=True
cam=stage.setup()
out=HERE/'out'
stage.shot(cam,out/'weapon_review_front.png',(2.5,-7,3.2),target=(.16,0,1.2),scale=2.85)
stage.shot(cam,out/'weapon_review_back.png',(-3,7,3.0),target=(.10,0,1.2),scale=2.85)
stage.shot(cam,out/'vertebrae_detail.png',(2,3,2.0),target=(0,.025,1.15),scale=.70)
stage.shot(cam,out/'axehead_detail.png',(3,-4,2.8),target=(.36,0,2.04),scale=1.50)
stage.shot(cam,out/'axehead_profile.png',(4,-1,2.5),target=(.40,0,2.04),scale=1.50)
# Keep a dedicated, editable weapon inspection scene with the rear arches visible.
for obj in rig.children:
    if obj.type=='MESH' and not obj.vertex_groups.get('axe'): obj.hide_set(True)
rig.show_in_front=False
bpy.ops.object.select_all(action='DESELECT')
from mathutils import Vector
eye=Vector((-2.5,6,2.7)); target=Vector((.1,0,1.2))
rotation=(target-eye).to_track_quat('-Z','Y')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            space=area.spaces.active
            space.shading.type='MATERIAL'; space.overlay.show_overlays=False
            space.region_3d.view_rotation=rotation
            space.region_3d.view_location=target
            space.region_3d.view_distance=3.3
bpy.ops.wm.save_as_mainfile(filepath=str(out/'weapon_study.blend'))
target=Vector((.36,0,2.04))
rotation=(target-Vector((3,-4,2.8))).to_track_quat('-Z','Y')
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type=='VIEW_3D':
            region=area.spaces.active.region_3d
            region.view_location=target; region.view_rotation=rotation; region.view_distance=1.9
bpy.ops.wm.save_as_mainfile(filepath=str(out/'axehead_study.blend'))
