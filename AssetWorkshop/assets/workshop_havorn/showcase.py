"""Colored Blender presentation plus exterior, deck and cargo cutaway renders."""
import os
import sys
import math
import bpy
import bmesh
from mathutils import Vector

HERE=os.path.dirname(os.path.abspath(__file__))
OUT=os.path.join(HERE,'out')
sys.path.insert(0,os.path.abspath(os.path.join(HERE,'../../blender')))
from workshop import scene,materials,shapes


def light(name,location,power,size,color):
    data=bpy.data.lights.new(name,'AREA')
    data.energy=power
    data.shape='DISK'
    data.size=size
    data.color=color
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=location
    obj.rotation_euler=(-Vector(location)).to_track_quat('-Z','Y').to_euler()


def stage():
    s=bpy.context.scene
    s.render.engine='BLENDER_EEVEE'
    s.eevee.taa_render_samples=64
    s.render.resolution_x=1600
    s.render.resolution_y=1200
    s.render.resolution_percentage=100
    s.view_settings.view_transform='AgX'
    s.world=bpy.data.worlds.new('Harbor_studio')
    bg=s.world.node_tree.nodes.get('Background')
    bg.inputs['Color'].default_value=(.26,.32,.39,1)
    bg.inputs['Strength'].default_value=.65
    light('Warm_key',(4,-8,24),4200,16,(1,.83,.64))
    light('Cool_fill',(-12,5,14),3600,14,(.66,.82,1))
    light('Stern_fill',(3,17,17),5000,12,(1,.94,.82))
    sun=bpy.data.lights.new('Soft_sun','SUN')
    sun.energy=2
    sun.angle=.2
    obj=bpy.data.objects.new('Soft_sun',sun)
    bpy.context.collection.objects.link(obj)
    obj.rotation_euler=(.5,-.5,-.4)
    ground=shapes.box('Studio_ground',(200,200,.1),(0,0,-2.1),material=materials.flat('Slate_stage',(.045,.064,.075)))
    ground.hide_select=True


def camera(name,pos,target,span):
    data=bpy.data.cameras.new(name)
    data.type='ORTHO'
    data.ortho_scale=span
    obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj)
    obj.location=pos
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    data.clip_end=500
    return obj


def shot(cam,name):
    bpy.context.scene.camera=cam
    bpy.context.scene.render.filepath=os.path.join(OUT,name+'.png')
    bpy.ops.render.render(write_still=True)


def hidden(names,value):
    for name in names:
        bpy.data.objects[name].hide_render=value
        bpy.data.objects[name].hide_set(value)


def colored_view(cam):
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type!='VIEW_3D':
                continue
            view=area.spaces.active
            view.shading.type='MATERIAL'
            view.shading.use_scene_lights=True
            view.shading.use_scene_world=True
            view.overlay.show_overlays=False
            view.clip_end=500
            view.region_3d.view_rotation=cam.rotation_euler.to_quaternion()
            view.region_3d.view_distance=32
            view.region_3d.view_location=(0,0,7)
            view.region_3d.view_perspective='CAMERA'
            view.region_3d.view_camera_zoom=0


def additions():
    f=bpy.context.scene['ship_scale']
    def point(p):
        return tuple(p[i]*f[i] for i in range(3))
    nest=camera('04_Lookout',point((5,7,21)),point((0,.4,16)),7.8)
    ladder=camera('05_Mast_ladder',point((7,16,13)),point((0,.8,10)),24)
    gun=camera('06_Bow_cannon',point((4,-12.3,7.5)),point((0,-7.8,3.9)),6.8)
    boarding=camera('07_Boarding_access',point((-9,3,6.5)),point((-3.3,0,3)),6.5)
    stern=camera('08_Captains_helm',point((7,15,9)),point((0,8.5,2.4)),10.5)
    shot(boarding,'colored_boarding')
    shot(stern,'colored_helm')
    shot(nest,'colored_lookout')
    shot(ladder,'colored_ladder')
    for frame,name in ((1,'colored_cannon_port'),(31,'colored_cannon'),(61,'colored_cannon_starboard')):
        bpy.context.scene.frame_set(frame)
        shot(gun,name)
    bpy.context.scene.frame_set(31)


def run():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'havorn_parts.blend'))
    stage()
    hero=camera('01_Colored_ship',(36,12,31),(0,0,7.6),35)
    top=camera('02_Upper_deck',(13,18,24),(0,0,2),28)
    hold=camera('03_Cargo_cutaway',(14,15,15),(0,0,1),25)
    shot(hero,'colored_ship')
    additions()
    hidden(('Sail','Rigging'),True)
    shot(top,'colored_deck')
    hidden(('Sail','Rigging'),False)
    colored_view(hero)
    bpy.context.scene.camera=hero
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Havorn_Colored.blend'))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Havorn_Lookout_Cannon.blend'))
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Havorn_Finished_Ends.blend'))
    # Inspection copy only: actual delivery retains both hull sides and full deck.
    hull=bpy.data.objects['Hull']
    cut=hull.copy()
    cut.data=hull.data.copy()
    cut.name='Hull_cutaway_INSPECTION_ONLY'
    bpy.context.collection.objects.link(cut)
    bm=bmesh.new()
    bm.from_mesh(cut.data)
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.x>0 and v.co.z>-.9],context='VERTS')
    bm.to_mesh(cut.data)
    bm.free()
    hidden(('Hull','UpperDeck','DeckRail','Decoration','Sail','Rigging','Rudder',
            'MastLadder','Lookout','CannonBase','CannonYaw','CannonBarrel',
            'Shields','ShieldMounts','HelmWheel','HelmStand'),True)
    light('Hold_inspection_fill',(4,2,6),850,5,(1,.88,.68))
    shot(hold,'colored_hold_cutaway')
    bpy.context.scene.camera=hold
    colored_view(hold)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Havorn_Hold_Inspection.blend'))


run()
