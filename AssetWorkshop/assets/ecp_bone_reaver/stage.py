"""Neutral-lit preview: no cinematic lighting hiding the actual asset."""
import math
import bpy
from mathutils import Vector, Matrix
from workshop import materials, preview


def setup():
    s=bpy.context.scene
    s.render.engine='BLENDER_EEVEE'
    s.eevee.taa_render_samples=24
    s.render.resolution_x=800; s.render.resolution_y=800
    s.render.resolution_percentage=100
    s.world=bpy.data.worlds.new('ReaverPreviewWorld')
    s.world.use_nodes=True
    s.world.node_tree.nodes['Background'].inputs[0].default_value=(.16,.19,.23,1)
    s.world.node_tree.nodes['Background'].inputs[1].default_value=.6
    s.view_settings.view_transform='AgX'
    bpy.ops.mesh.primitive_plane_add(size=200)
    ground=bpy.context.object; ground.name='PREVIEW_Ground'
    ground.data.materials.append(materials.flat('preview_ground',(.065,.077,.068)))
    for name,pos,energy,size in [('Key',(-3,-4,7),1700,5),('Fill',(4,-2,4),800,4),('Rim',(0,4,5),1500,3)]:
        data=bpy.data.lights.new(name,'AREA'); data.energy=energy; data.shape='DISK'; data.size=size
        obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj); obj.location=pos
        obj.rotation_euler=(Vector((0,0,1.5))-obj.location).to_track_quat('-Z','Y').to_euler()
    camera=bpy.data.objects.new('PreviewCamera',bpy.data.cameras.new('PreviewCamera'))
    bpy.context.collection.objects.link(camera); s.camera=camera; camera.data.type='ORTHO'
    return camera


def shot(camera,path,eye,target=(0,0,1.4),scale=3.6):
    camera.location=eye
    camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=scale
    bpy.context.scene.render.filepath=str(path)
    bpy.ops.render.render(write_still=True)


def render(rig,out):
    cam=setup()
    paths=[]
    for name,eye in [('front',(0,-7,3)),('three_quarter',(5,-7,3.8)),('back',(4,7,3.4)),('side',(-7,-1,3))]:
        path=out/('view_'+name+'.png')
        shot(cam,path,eye)
        paths.append(str(path))
    preview.grid(paths,2,str(out/'preview.png'))
    attacks=[('slam_R',35),('slam_R',53),('spin_R',38),('sweep_R',48),
             ('rear_R',20),('rear_R',49),('daggers_R',38),('axe_throw_R',135)]
    paths=[]
    for name,frame in attacks:
        rig.animation_data.action=bpy.data.actions[name]
        bpy.context.scene.frame_set(frame)
        path=out/(name+'_'+str(frame)+'.png')
        shot(cam,path,(5,-7,4),target=(0,0,2.1),scale=5.5)
        paths.append(str(path))
    preview.grid(paths,4,str(out/'attacks.png'))
    rig.animation_data.action=bpy.data.actions['idle_R']
    bpy.context.scene.frame_set(0)
    shot(cam,out/'satchel_detail.png',(-4,-3,2.0),target=(-.23,0,1.12),scale=1.05)
    # Stand the axe in its modelling pose to inspect the curvature and vertebrae.
    rig.animation_data.action=None
    for bone in rig.pose.bones:
        bone.matrix_basis=Matrix.Identity(4)
    hidden=[]
    for obj in rig.children:
        if obj.type=='MESH' and not obj.vertex_groups.get('axe'):
            hidden.append(obj); obj.hide_render=True
    bpy.context.view_layer.update()
    shot(cam,out/'axe_detail.png',(2.5,-7,3.2),target=(.16,0,1.2),scale=2.85)
    for obj in hidden: obj.hide_render=False
    rig.animation_data.action=bpy.data.actions['idle_R']
    bpy.context.scene.frame_set(0)
    shot(cam,out/'view_three_quarter.png',(5,-7,3.8))
    bpy.ops.wm.save_as_mainfile(filepath=str(out/'ecp_bone_reaver.blend'))
