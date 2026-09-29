"""Build the packed 2D icon review board. Does not change or deploy game assets.
Run: blender --background --factory-startup --python <this file>
"""
from pathlib import Path
import bpy
from mathutils import Quaternion, Vector

ROOT = Path(__file__).resolve().parent
W, H, SCALE = 1440, 1600, 100.0
ORDER = ['defense', 'sailing', 'foraging', 'husbandry', 'cooking', 'farming',
         'fishing', 'woodcutting', 'pickaxes']
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)


def linear(v):
    v /= 255.0
    return v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4


def solid(name, rgb):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    emission = nodes.new('ShaderNodeEmission')
    emission.inputs['Color'].default_value = (*[linear(v) for v in rgb], 1)
    mat.node_tree.links.new(emission.outputs[0], out.inputs['Surface'])
    mat.diffuse_color = (*[linear(v) for v in rgb], 1)
    return mat


def picture(path):
    image = bpy.data.images.load(str(path), check_existing=True)
    image.pack()
    mat = bpy.data.materials.new(path.parent.name + '_' + path.stem)
    mat.use_nodes = True
    mat.surface_render_method = 'DITHERED'
    nodes = mat.node_tree.nodes
    nodes.clear()
    out = nodes.new('ShaderNodeOutputMaterial')
    tex = nodes.new('ShaderNodeTexImage')
    tex.image = image
    tex.interpolation = 'Closest' if path.parent.name != 'masters' else 'Linear'
    emission = nodes.new('ShaderNodeEmission')
    transparent = nodes.new('ShaderNodeBsdfTransparent')
    mix = nodes.new('ShaderNodeMixShader')
    links = mat.node_tree.links
    links.new(tex.outputs['Color'], emission.inputs['Color'])
    links.new(tex.outputs['Alpha'], mix.inputs[0])
    links.new(transparent.outputs[0], mix.inputs[1])
    links.new(emission.outputs[0], mix.inputs[2])
    links.new(mix.outputs[0], out.inputs['Surface'])
    return mat


def plane(name, x, y, width, height, z, material):
    left, right = (x-W/2)/SCALE, (x+width-W/2)/SCALE
    top, bottom = (H/2-y)/SCALE, (H/2-y-height)/SCALE
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([(left,bottom,z),(right,bottom,z),(right,top,z),(left,top,z)], [], [(0,1,2,3)])
    uv = mesh.uv_layers.new()
    for index, value in enumerate([(0,0),(1,0),(1,1),(0,1)]):
        uv.data[index].uv = value
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    obj.data.materials.append(material)
    return obj


font_path = Path('C:/Windows/Fonts/segoeui.ttf')
font = bpy.data.fonts.load(str(font_path)) if font_path.exists() else None


def label(text, x, y, size, mat):
    curve = bpy.data.curves.new(text, 'FONT')
    curve.body = text
    curve.size = size/SCALE
    if font:
        curve.font = font
    obj = bpy.data.objects.new(text, curve)
    bpy.context.scene.collection.objects.link(obj)
    obj.location = ((x-W/2)/SCALE, (H/2-y)/SCALE, .035)
    curve.materials.append(mat)
    return obj


background = solid('Board charcoal', (25,27,28))
card = solid('Neutral comparison surface', (49,44,39))
ink = solid('Warm paper labels', (229,217,197))
muted = solid('Secondary labels', (157,151,140))
gold = solid('Quiet ochre accent', (194,154,92))
plane('Review background', 0,0,W,H,-.03,background)
label('GRINDSTONE SKILLS',40,57,34,ink)
label('Vanilla-inspired icon remakes  /  2D review',40,89,18,muted)
label('Matte materials. Clear silhouettes. Original artwork.',820,88,17,gold)

for index, name in enumerate(ORDER):
    x, y = 40+(index%3)*460, 120+(index//3)*470
    plane(name+' review card',x,y,440,450,0,card)
    label(name.upper(),x+22,y+32,22,ink)
    plane(name+' enlarged',x+92,y+54,256,256,.012,picture(ROOT/'masters'/('skill_'+name+'.png')))
    label('OLD',x+35,y+341,14,muted)
    label('NEW 64 PX',x+166,y+341,14,gold)
    label('NEW 32 PX',x+302,y+341,14,gold)
    plane(name+' old 64',x+33,y+351,64,64,.015,picture(ROOT/'previous'/('skill_'+name+'.png')))
    plane(name+' new 64',x+164,y+351,64,64,.015,picture(ROOT/'icons64'/('skill_'+name+'.png')))
    plane(name+' new 32',x+305,y+367,32,32,.015,picture(ROOT/'icons32'/('skill_'+name+'.png')))
    label('Transparent PNG  /  32, 64 and 128 px exports',x+22,y+435,13,muted)

label('REVIEW ONLY  /  Game assets have not been replaced.',40,1569,17,gold)
scene = bpy.context.scene
scene.render.engine = 'BLENDER_EEVEE'
scene.eevee.taa_render_samples = 32
scene.render.resolution_x = W
scene.render.resolution_y = H
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
scene.view_settings.exposure = 0
scene.view_settings.gamma = 1
scene.world.color = (0,0,0)
camera = bpy.data.objects.new('Review board - front',bpy.data.cameras.new('Review board camera'))
scene.collection.objects.link(camera)
camera.location = (0,0,30)
camera.data.type = 'ORTHO'
camera.data.ortho_scale = H/SCALE
camera.data.clip_end = 100
scene.camera = camera
scene.render.filepath = str(ROOT/'review-board.png')

# Ready to inspect immediately upon opening, with the textured front view and no scene clutter.
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type == 'VIEW_3D':
            space = area.spaces.active
            space.shading.type = 'MATERIAL'
            space.overlay.show_overlays = False
            space.show_gizmo = False
            space.region_3d.view_rotation = Quaternion((1,0,0,0))
            space.region_3d.view_location = Vector((0,0,0))
            space.region_3d.view_distance = 18
            space.region_3d.view_perspective = 'CAMERA'
            space.region_3d.view_camera_zoom = 0
bpy.ops.object.select_all(action='DESELECT')
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Grindstone_Skill_Icons_Review.blend'))
bpy.ops.render.render(write_still=True)
print('REVIEW_READY',ROOT/'Grindstone_Skill_Icons_Review.blend')
