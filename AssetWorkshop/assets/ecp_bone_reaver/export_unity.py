"""Export a distributable original kit plus a separate local-only full-body preview."""
import json
import math
import sys
from pathlib import Path

HERE=Path(__file__).resolve().parent
sys.path[:0]=[str(HERE),str(HERE.parents[1]/'blender')]
import bpy
from mathutils import Matrix
from workshop import scene, reference

OUT=HERE/'out'
KIT=OUT/'unity'
LOCAL=OUT/'reference_unity'
for path in (KIT,LOCAL): path.mkdir(exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(OUT/'ecp_bone_reaver.blend'))
rig=bpy.data.objects['BoneReaverRig']
rig.animation_data.action=None
for b in rig.pose.bones: b.matrix_basis=Matrix.Identity(4)
bpy.context.view_layer.update()
ours=[o for o in bpy.data.objects if o.type=='MESH' and o.parent==rig and not reference.is_reference(o)]
ref=[o for o in bpy.data.objects if o.type=='MESH' and o.parent==rig and reference.is_reference(o)]
scene.select_only(ours)
bpy.ops.object.join()
kit=bpy.context.object
kit.name='BoneReaverEquipment'
turn=Matrix.Rotation(math.pi,4,'Z')
scene.select_only([rig])
bpy.ops.object.mode_set(mode='EDIT')
for b in rig.data.edit_bones: b.transform(turn)
bpy.ops.object.mode_set(mode='OBJECT')
for o in [kit]+ref: o.data.transform(turn)


def materials(objects,folder):
    result=[]
    used={m.name:m for o in objects for m in o.data.materials if m}
    for mat in used.values():
        bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        tex=next((n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image),None)
        filename=''
        if tex:
            filename=tex.name.split('.')[0]+'.png'
            # save_render loads file-backed reference pixels before writing a new path.
            tex.save_render(str(folder/filename))
        result.append(dict(name=mat.name,color=list(bsdf.inputs['Base Color'].default_value[:3]),
            texture=filename,emission=list(bsdf.inputs['Emission Color'].default_value[:3]),
            emissionStrength=bsdf.inputs['Emission Strength'].default_value))
    return result


def fbx(path,objects):
    scene.select_only([rig]+objects,active=rig)
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'ARMATURE','MESH'},
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
        add_leaf_bones=False,armature_nodetype='NULL',use_armature_deform_only=False,
        bake_anim=True,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True,bake_anim_force_startend_keying=True,bake_anim_step=1,
        bake_anim_simplify_factor=0,path_mode='STRIP')


report=json.loads((OUT/'manifest.json').read_text())
report['materials']=materials([kit],KIT)
(KIT/'ecp_bone_reaver.json').write_text(json.dumps(report,indent=2))
fbx(KIT/'ecp_bone_reaver.fbx',[kit])
report['materials']+=materials(ref,LOCAL)
(LOCAL/'ecp_bone_reaver_preview.json').write_text(json.dumps(report,indent=2))
fbx(LOCAL/'ecp_bone_reaver_preview.fbx',[kit]+ref)
assert not any(reference.is_reference(o) for o in [kit])
print('Exported original kit and separate local reference preview.',flush=True)
