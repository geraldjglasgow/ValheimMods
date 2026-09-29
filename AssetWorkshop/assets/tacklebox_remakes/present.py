"""Build four independent assets and a packed, colored Blender review file. Run with Blender --background --python."""
import os
import sys
import math
import bpy
from mathutils import Vector

HERE=os.path.dirname(os.path.abspath(__file__))
ASSETS=os.path.dirname(HERE)
WORKSHOP=os.path.dirname(ASSETS)
sys.path.insert(0,os.path.join(WORKSHOP,'blender'))
from workshop import pipeline,preview,scene,materials

TIERS=('driftwood','finewood','carapace','flametal')
OUT=os.path.join(HERE,'out')
os.makedirs(OUT,exist_ok=True)


def viewport(center,distance):
    direction=Vector((.3,-1,.68))
    rotation=direction.to_track_quat('Z','Y')
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                space=area.spaces.active
                space.shading.type='MATERIAL'
                space.shading.use_scene_world=False
                space.shading.use_scene_lights=False
                space.shading.studiolight_rotate_z=.5
                space.overlay.show_overlays=False
                space.region_3d.view_rotation=rotation
                space.region_3d.view_location=center
                space.region_3d.view_distance=distance
                space.region_3d.view_perspective='ORTHO'
                space.clip_start=.001


def finish_asset(tier):
    name=f'packpanel_{tier}_tacklebox'
    folder=os.path.join(ASSETS,name)
    pipeline.run(folder,render_preview=False)
    obj=bpy.data.objects[name]
    mat=obj.data.materials[0]
    for node in mat.node_tree.nodes:
        if node.type=='TEX_IMAGE': node.interpolation='Closest'
        if node.type=='NORMAL_MAP': node.inputs['Strength'].default_value=.35
    materials.principled(mat).inputs['Roughness'].default_value=.72
    for collider in [o for o in bpy.context.scene.objects if o.name.startswith('col_')]: collider.hide_set(True)
    scene.select_only([obj])
    viewport(Vector((0,0,.14)),.95)
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(folder,'out',name+'.blend'))
    preview.render_sheet(obj,os.path.join(folder,'out'))


def light(name,position,power,size,color):
    data=bpy.data.lights.new(name,'AREA'); data.energy=power; data.shape='DISK'; data.size=size; data.color=color
    obj=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(obj)
    obj.location=position; obj.rotation_euler=(-obj.location+Vector((0,0,.12))).to_track_quat('-Z','Y').to_euler()


def camera(name,at,target,scale):
    data=bpy.data.cameras.new(name); obj=bpy.data.objects.new(name,data)
    bpy.context.collection.objects.link(obj); obj.location=at
    obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    data.type='ORTHO'; data.ortho_scale=scale; data.lens=50
    return obj


def label(text,x,y,size):
    data=bpy.data.curves.new(text,'FONT'); data.body=text; data.align_x='CENTER'; data.size=size; data.extrude=0
    obj=bpy.data.objects.new(text,data); bpy.context.collection.objects.link(obj)
    obj.location=(x,y,.004)
    data.materials.append(materials.flat('label_'+text,(.65,.58,.43)))


def gallery():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene.clear()
    review=bpy.context.scene; review.name='All four tackleboxes'
    review.render.engine='BLENDER_EEVEE'; review.eevee.taa_render_samples=64
    review.render.resolution_x=2000; review.render.resolution_y=900; review.render.resolution_percentage=100
    review.view_settings.view_transform='Standard'
    world=bpy.data.worlds.new('Neutral workshop'); world.use_nodes=True
    world.node_tree.nodes['Background'].inputs['Color'].default_value=(.15,.17,.20,1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value=.5
    review.world=world
    positions=(-1.13,-.48,.26,1.08)
    models=[]
    for index,(tier,x) in enumerate(zip(TIERS,positions)):
        name=f'packpanel_{tier}_tacklebox'
        file=os.path.join(ASSETS,name,'out',name+'.blend')
        with bpy.data.libraries.load(file,link=False) as (src,dest): dest.objects=[name]
        obj=dest.objects[0]
        collection=bpy.data.collections.new(f'{index+1:02d} - {tier.title()}')
        review.collection.children.link(collection); collection.objects.link(obj)
        obj.location.x=x; models.append(obj)
        label(f'{index+1:02d}  {tier.upper()}',x,-.37,.040)
        camera(tier.title()+' close-up',(x+.65,-1,.75),(x,0,.15),.95)
    # Ground for the saved render; hidden in the interactive material-preview view.
    bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012))
    ground=bpy.context.object; ground.name='Render ground'; ground.data.materials.append(materials.flat('ground',(.045,.053,.057)))
    light('Warm key',(-2,-3,4),420,4,(1,.88,.72))
    light('Cool fill',(2,1,3),300,3,(.70,.83,1))
    review.camera=camera('All four - comparison',(.55,-3.8,2.9),(0,0,.12),3.30)
    scene.select_only(models)
    viewport(Vector((0,0,.15)),3.35)
    bpy.ops.file.pack_all()
    review.render.filepath=os.path.join(OUT,'tackleboxes_lineup.png')
    bpy.ops.render.render(write_still=True)
    ground.hide_set(True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Tackleboxes_Review.blend'))
    print('REVIEW '+os.path.join(OUT,'Tackleboxes_Review.blend'),flush=True)


if '--gallery-only' not in sys.argv:
    selected = TIERS[1:] if '--details-only' in sys.argv else TIERS
    for tier in selected: finish_asset(tier)
gallery()
