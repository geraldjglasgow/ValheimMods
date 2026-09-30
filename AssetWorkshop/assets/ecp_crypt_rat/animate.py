"""Run after build.ps1: produces rigged Blend/FBX, clip contract and bite sheet."""
import json
import os
import sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path[:0] = [HERE, os.path.join(HERE,'..','..','blender')]
import bpy
from mathutils import Vector, Matrix
from workshop import scene, preview
import rig as ratrig
from lunge import TAKEOFF, FRONT_LAND, HIND_LAND, STOP, HIT

NAME = 'ecp_crypt_rat'
OUT = os.path.join(HERE,'out')
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,NAME+'.blend'))
mesh = bpy.data.objects[NAME]
rig = ratrig.build(mesh)
ratrig.actions(rig)
import tail_motion
for clip,last,loop,tag in ratrig.CLIPS:
    tail_motion.bake(rig,bpy.data.actions[clip],last,loop,virtual=True)
rig.animation_data.action=None
ratrig.reset(rig)
assert all(abs(sum(g.weight for g in v.groups)-1)<.0001 for v in mesh.data.vertices)
assert all(g.name in rig.data.bones for g in mesh.vertex_groups)
bpy.context.scene.render.fps = 30
bpy.context.scene.frame_start = 0
bpy.context.scene.frame_end = 54
rig.animation_data.action = bpy.data.actions['attack_bite']
bpy.context.scene.frame_set(0)
from colored_view import prepare
prepare()
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,NAME+'_animated.blend'))

# Turn the exported rig and mesh together, matching crypt_mimic's tested axis route.
rig.animation_data.action = None
ratrig.reset(rig)
turn = Matrix.Rotation(3.141592653589793,4,'Z')
rig.data.transform(turn)
mesh.data.transform(turn)
scene.select_only([rig,mesh],active=rig)
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,NAME+'_animated.fbx'),use_selection=True,
    object_types={'ARMATURE','MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
    axis_forward='-Z',axis_up='Y',bake_space_transform=False,add_leaf_bones=False,
    primary_bone_axis='Y',secondary_bone_axis='X',armature_nodetype='NULL',
    use_armature_deform_only=False,mesh_smooth_type='FACE',bake_anim=True,
    bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True,bake_anim_step=1,bake_anim_simplify_factor=0,path_mode='STRIP')
rig.data.transform(turn.inverted())
mesh.data.transform(turn.inverted())

clips = []
for name,last,loop,tag in ratrig.CLIPS:
    clip = dict(name=name,first=0,last=last,loop=loop,tag=tag,events=[])
    if name=='attack_bite':
        clip['events'] = [dict(frame=HIT,function='Hit')]
    if name in ('walk','run','scurry'):
        clip['footstep'] = [[0,0],[last*.25,1],[last*.5,0],[last*.75,1],[last,0]]
    clips.append(clip)
contract = dict(asset=NAME,fbx=NAME+'_animated.fbx',fps=30,rootMotion=False,
    rig=rig.name,head='Head',attackOrigin='BiteOrigin',clips=clips,
    lunge=dict(startFrame=14,endFrame=STOP,takeoffFrame=TAKEOFF,frontContactFrame=FRONT_LAND,hindContactFrame=HIND_LAND,
               distanceMetres=.95,travelCurve='accelerate until takeoff, constant flight until front contact, brake until end',
               authority='runtime character owner'))
with open(os.path.join(OUT,'animation_contract.json'),'w') as f:
    json.dump(contract,f,indent=2)

preview._stage(mesh)
camera = preview._camera()
camera.data.type='ORTHO'
camera.data.ortho_scale=2.8
camera.location=(2.6,-3.0,1.75)
camera.rotation_euler=(Vector((0,.1,.32))-camera.location).to_track_quat('-Z','Y').to_euler()
bpy.context.scene.render.resolution_x=640
bpy.context.scene.render.resolution_y=480
rig.animation_data.action=bpy.data.actions['attack_bite']
paths=[]
for frame in (0,14,18,HIT,24,54):
    bpy.context.scene.frame_set(frame)
    path=os.path.join(OUT,f'bite_{frame:02}.png')
    bpy.context.scene.render.filepath=path
    bpy.ops.render.render(write_still=True)
    paths.append(path)
preview.grid(paths,3,os.path.join(OUT,'bite_sheet.png'))
print('WORKSHOP rig: weighted vertices checked;',len(rig.data.bones),'bones; 7 clips; bite event frame',HIT)
