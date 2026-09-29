"""Build the eight backpacks, their packed material previews and the review scenes. Inventory icons come from each
asset's own icon.py (framed to the model, as every PackPanel icon is)."""
import os,sys,json
import bpy
from mathutils import Vector
HERE=os.path.dirname(os.path.abspath(__file__)); ASSETS=os.path.dirname(HERE)
sys.path[:0]=[HERE,os.path.join(os.path.dirname(ASSETS),'blender')]
from workshop import pipeline,scene,materials
KINDS={'deerhide':'satchel','trollhide':'backpack','rootbound':'pack','wolfpelt':'pack','lox':'hauler','carapace':'pack','asksvin':'pack','moosehide':'pack'}
def asset(t): return 'packpanel_'+t+'_'+KINDS[t]
OUT=os.path.join(HERE,'out'); os.makedirs(OUT,exist_ok=True)

def viewport(center,distance):
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                s=area.spaces.active; s.shading.type='MATERIAL'; s.overlay.show_overlays=False
                s.region_3d.view_rotation=Vector((.25,1,.32)).to_track_quat('Z','Y')
                s.region_3d.view_location=Vector(center); s.region_3d.view_distance=distance
                s.region_3d.view_perspective='ORTHO'; s.clip_start=.001

def stage(target,scale):
    s=bpy.context.scene; s.render.engine='BLENDER_EEVEE'; s.view_settings.view_transform='Standard'
    s.render.resolution_percentage=100; s.render.image_settings.file_format='PNG'; s.render.image_settings.color_mode='RGBA'
    world=bpy.data.worlds.new('Neutral workshop'); world.use_nodes=True
    world.node_tree.nodes['Background'].inputs['Color'].default_value=(.095,.115,.13,1)
    world.node_tree.nodes['Background'].inputs['Strength'].default_value=.6; s.world=world
    for name,position,power,size in [('Key',(-2,3,4),400,3),('Fill',(3,1,2),220,3),('Rim',(-1,-2,3),220,2)]:
        data=bpy.data.lights.new(name,'AREA'); data.energy=power; data.size=size
        obj=bpy.data.objects.new(name,data); s.collection.objects.link(obj); obj.location=position
        obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    data=bpy.data.cameras.new('Camera'); data.type='ORTHO'; data.ortho_scale=scale; data.clip_start=.001
    obj=bpy.data.objects.new('Camera',data); s.collection.objects.link(obj)
    obj.location=Vector(target)+Vector((.65,2,.55)); obj.rotation_euler=(Vector(target)-obj.location).to_track_quat('-Z','Y').to_euler()
    s.camera=obj; return s

if '--gallery-only' not in sys.argv:
    for tier in KINDS:
        name=asset(tier); folder=os.path.join(ASSETS,name)
        if '--finish-only' in sys.argv:
            bpy.ops.wm.open_mainfile(filepath=os.path.join(folder,'out',name+'.blend'))
        else:
            # Original recipes use local modules named forms/looks; isolate each recipe's imports.
            for key,mod in list(sys.modules.items()):
                file=getattr(mod,'__file__','') or ''
                if 'packpanel_' in file and os.path.abspath(file).startswith(os.path.abspath(ASSETS)):
                    del sys.modules[key]
            pipeline.run(folder,render_preview=False)
        obj=bpy.data.objects[name]
        mat=obj.data.materials[0]
        for node in mat.node_tree.nodes:
            if node.type=='TEX_IMAGE': node.interpolation='Closest'
            if node.type=='NORMAL_MAP': node.inputs['Strength'].default_value=.45
        materials.principled(mat).inputs['Roughness'].default_value=.84
        for other in bpy.context.scene.objects:
            if other.name.startswith('col_'): other.hide_set(True); other.hide_render=True
        scene.select_only([obj]); viewport((0,.23,-.08),1.05); bpy.ops.file.pack_all()
        bpy.ops.wm.save_as_mainfile(filepath=os.path.join(folder,'out',name+'.blend'))
        s=stage((0,.23,-.08),.91); s.render.film_transparent=True
        for size,file in [(768,'preview.png')]:
            s.render.resolution_x=size; s.render.resolution_y=size; s.render.filepath=os.path.join(folder,'out',file)
            bpy.ops.render.render(write_still=True)
        print('FINISHED '+tier,flush=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
s=stage((0,.22,.12),3.12); models=[]
# Straight-on comparison preserves relative sizes and label readability.
s.camera.location=(.50,4,1.30); s.camera.rotation_euler=(Vector((0,.22,.12))-s.camera.location).to_track_quat('-Z','Y').to_euler()
for i,tier in enumerate(KINDS):
    name=asset(tier)
    with bpy.data.libraries.load(os.path.join(ASSETS,name,'out',name+'.blend'),link=False) as (src,dst): dst.objects=[name]
    obj=dst.objects[0]; s.collection.objects.link(obj)
    x=(1.5-i%4)*.76; z=.61 if i<4 else -.27
    obj.location=(x,0,z); models.append(obj)
    data=bpy.data.curves.new(tier,'FONT'); data.body=tier.upper(); data.align_x='CENTER'; data.size=.043
    label=bpy.data.objects.new(tier+' label',data); s.collection.objects.link(label)
    label.location=(x,.38,z-.46); label.rotation_euler=s.camera.rotation_euler
    data.materials.append(materials.flat('label',(.65,.62,.52)))
s.render.resolution_x=2000; s.render.resolution_y=1250
s.render.filepath=os.path.join(OUT,'backpacks_lineup.png')
scene.select_only(models); viewport((0,.22,.12),3.3); bpy.ops.file.pack_all()
bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Backpacks_Review.blend'))
# A separate rear review opens directly on the wearer-facing construction.
for obj in models: obj.rotation_euler.z=3.141592653589793
rear_target=Vector((0,-.22,.12))
s.camera.location=(.50,4,.95); s.camera.rotation_euler=(rear_target-s.camera.location).to_track_quat('-Z','Y').to_euler()
for obj in s.objects:
    if obj.type=='FONT': obj.rotation_euler=s.camera.rotation_euler
viewport((0,-.22,.12),3.3)
s.render.filepath=os.path.join(OUT,'backpacks_rear_lineup.png')
bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'Backpacks_Rear_Review.blend'))
print('ALL EIGHT COMPLETE',flush=True)
