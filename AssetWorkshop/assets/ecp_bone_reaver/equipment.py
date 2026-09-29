"""Original bone axe, burial rags, belt, throwing knives and attachment markers."""
import math
import bpy
from mathutils import Matrix, Vector
from workshop import materials
from body import rigid
from geometry import palette, mesh, sphere, rod, slab, curved_rod, sculpted_blade
import satchel
import spine_shape
import rags
import vertebrae
import axe_blade
import bindings


def build(rig):
    bone=palette('reaver_bone_',(.26,.235,.18))
    edge=palette('reaver_edge_',(.40,.365,.28))
    cloth=palette('reaver_rag_',(.16,.098,.047))
    leather=palette('reaver_leather_',(.11,.067,.034))
    def put(obj, name):
        return rigid(obj,rig,name)
    vertebrae.build(rig,bone)
    axe_blade.build(rig,bone,edge)
    put(curved_rod('RearTusk',[(0,0,2.10),(-.17,0,2.18),(-.30,0,2.29),(-.36,0,2.37)],
        [.10,.08,.035,.002],edge,12),'axe')
    bindings.build(rig,leather)
    # Hip belt and individually rigged, irregular cloth strips.
    waist=rig.data.bones['Hips'].head_local.z+.10
    for i in range(12):
        a,b=i*math.tau/12,(i+1)*math.tau/12
        verts=[(.25*math.cos(t),.16*math.sin(t),waist+z) for z in (0,.085) for t in (a,b)]
        put(mesh('LeatherBelt',verts,[(0,1,3,2)],leather),'Hips')
    rags.build(rig,cloth,waist)
    satchel.build(rig,leather,bone)
    spine_shape.bend(rig)
    # Three blades fanned between the fingers, controlled independently for release.
    for i in range(3):
        x=(i-1)*.055
        put(rod('ThrownKnifeGrip',(x,0,-.10),(x,0,.05),.019,.019,leather,6),'daggers')
        put(slab('ThrownKnifeBlade',[(x-.027,.04),(x+.027,.04),(x+(i-1)*.065,.36)],.014,edge),'daggers')
    glow=materials.flat('reaver_eye_glow',(.18,.006,.5))
    bsdf=next(n for n in glow.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    bsdf.inputs['Emission Color'].default_value=(.32,.015,.9,1)
    bsdf.inputs['Emission Strength'].default_value=4
    for name in ('eye_l','eye_r'):
        pos=rig.data.bones[name].head_local + Vector((0,-.014,0))
        put(sphere('PurpleEye_'+name,pos,(.018,.015,.012),[glow],8,4),'eye_glow')
    markers={'blood_drip':('axe',(.825,0,1.615)), 'axe_impact':('axe',(.97,0,2.045)),
             'grip_primary':('axe',(0,0,.28)), 'grip_secondary':('axe',(0,0,.66)),
             'dagger_release':('daggers',(0,0,.2)), 'bag_draw':('Hips',satchel.DRAW_POINT)}
    for name,(bone,pos) in markers.items():
        obj=bpy.data.objects.new(name,None)
        bpy.context.collection.objects.link(obj)
        obj.empty_display_type='SPHERE'; obj.empty_display_size=.025
        obj.parent=rig; obj.parent_type='BONE'; obj.parent_bone=bone
        obj.matrix_world=Matrix.Translation(pos)
