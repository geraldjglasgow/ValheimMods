"""Animation review and lunge video frames; preview travel never enters the FBX."""
import os
import sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path[:0] = [HERE, os.path.join(HERE,'..','..','blender')]
import bpy
from mathutils import Vector
from workshop import preview
from rig import sample
from lunge import travel

OUT = os.path.join(HERE,'out')
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'ecp_crypt_rat_animated.blend'))
rat = bpy.data.objects['ecp_crypt_rat']
rig = bpy.data.objects['crypt_rat_rig']
preview._stage(rat)
camera = preview._camera()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 3.7
camera.location = (3,-3,1.65)
camera.rotation_euler = (Vector((0,-.1,.34))-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.context.scene.render.resolution_x = 960
bpy.context.scene.render.resolution_y = 640
paths=[]
for name,frame in [('idle',36),('walk',8),('scurry',5),('attack_bite',20),('stagger',9),('death',36)]:
    rig.animation_data.action = bpy.data.actions[name]
    bpy.context.scene.frame_set(frame)
    path = os.path.join(OUT,'pose_'+name+'.png')
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    paths.append(path)
preview.grid(paths,3,os.path.join(OUT,'animation_sheet.png'))

frames = os.path.join(OUT,'lunge_frames')
os.makedirs(frames,exist_ok=True)
for frame in range(120):
    if frame < 24:
        action, local = 'idle', frame
    elif frame < 79:
        action, local = 'attack_bite', frame-24
    else:
        action, local = 'idle', frame-79
    rig.animation_data.action = bpy.data.actions[action]
    bpy.context.scene.frame_set(local)
    rig.location.y = -travel(frame-24)
    bpy.context.scene.render.filepath = os.path.join(frames,f'{frame:04}.png')
    bpy.ops.render.render(write_still=True)
print('WORKSHOP reviewed six representative actions and rendered 120 lunge frames')
