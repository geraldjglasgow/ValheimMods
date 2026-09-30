"""Havorn: decked cargo ship, based on the original 2x Karve concept."""
import os
import sys
import math
import bpy
import bmesh
from mathutils import Vector
from workshop import paint, shapes, scene
sys.path.insert(0,os.path.dirname(__file__))
from geometry import beam, box, ribbon, hull, hullpoint, width, rail, deck, stem, sail
import ship_fittings
import deckworks
import lookout_cannon

CATEGORY='vehicle.ship'
TEXTURE_SIZE=2048
NORMAL_MAP=True
AO_STRENGTH=.2
NORMAL_FROM_ALBEDO=3.0
TARGET=(15.6116686,22.25,19.5)


def materials():
    return dict(wood=[paint.wood_planks('Oak_%d'%i,axis='Y',density=32,pattern=None,
                    tint=t,hollows=.25) for i,t in enumerate(('#775538','#805d3c','#715136'))],
                dark=paint.make('wood.dark','Tarred_oak',axis='Y',region='trim',pattern=None,hollows=.1,tint='#695139'),
                spar=paint.wood_logs('Spars',axis='Z',density=32,pattern=None),
                cloth=paint.linen('Weathered_linen',dye='undyed',density=32,pattern=None,
                                 hollows=.15,jitter=.008,jitter_scale=.2,grain=None,blotch_m=.24,contrast=.7),
                rope=paint.rope('Hemp',density=32,pattern=None),
                iron=paint.iron('Forged_iron',density=32,region='metal',hollows=.1),
                red=paint.linen('Oxide_red',dye='red',tint='#893e34',region='primary',pattern=None,jitter=.008,grain=None),
                blue=paint.wood_planks('Faded_blue',tint='#466569',region='secondary',pattern=None,hollows=.1),
                gold=paint.wood_planks('Ochre_carving',tint='#ab864c',region='trim',pattern=None,hollows=.1),
                gunmetal=paint.iron('Cannon_iron',tint='#4d5250',region='metal',hollows=.1))


def framing(m):
    for i in range(-4,5):
        t=i*.19
        for s in (-1,1):
            pts=[hullpoint(t,f,s) for f in (.13,.35,.6,.85,1)]
            for a,b in zip(pts,pts[1:]):
                beam('Frame',Vector(a)+Vector((-s*.17,0,0)),Vector(b)+Vector((-s*.17,0,0)),.13,m['spar'],6)


def posts(m):
    # Fitted caps conceal the ends of the clinker courses with one clean joint.
    from geometry import mesh
    for side in (-1,1):
        profile=[]
        for i in range(8):
            f=i/7
            x=width(1)*(.13+.87*math.sin(f*math.pi/2))+.10
            z=.44+2.39*f
            profile.append((-x,z))
        profile += [(-x,z) for x,z in reversed(profile)]
        verts=[(x,side*y,z) for y in (9.73,9.96) for x,z in profile]
        n=len(profile)
        faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
        faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
        mesh('Fitted_bow_cap' if side<0 else 'Fitted_stern_cap',verts,faces,m['dark'])


def spars(m):
    before=set(scene.meshes())
    beam('Mast',(0,.25,.35),(0,.25,16.9),.25,m['spar'],10)
    beam('Yard',(-7.8058,-.25,15.04),(7.8058,-.25,15.04),.17,m['spar'],8)
    box('Mast_step',(1.05,1.25,.5),(0,.25,.53),m['dark'])
    for z in (.9,1.4,14.95):
        beam('Mast_band',(0,.25,z),(0,.25,z+.16),.267,m['iron'],10)
    sail(m['cloth'],m['red'])
    for x in (-7.25,7.25):
        beam('Yard_brace',(0,.25,16.3),(x,-.25,15.04),.035,m['rope'],5)
        beam('Sheet',(x*.94,-.38,6.3),(math.copysign(3.0,x),3.9,3.6),.035,m['rope'],5)
    beam('Forward_stay',(0,.25,16.2),(1.4,-7.8,3.3),.045,m['rope'],5)
    beam('Aft_stay',(1.25,.25,16.2),(1.15,8.0,3.75),.045,m['rope'],5)
    for s in (-1,1):
        for y in (-2.1,2.1):
            beam('Shroud',(0,.25,13.8),(s*3.25,y,3.6),.04,m['rope'],5)
    for obj in set(scene.meshes())-before:
        if obj.vertex_groups.get('Sail') is None:
            obj.vertex_groups.clear()
            obj.vertex_groups.new(name='Rigging').add(list(range(len(obj.data.vertices))),1,'REPLACE')


def fittings(m):
    for y in (-.54,.54):
        beam('Ladder_side',(-3.65,y,-.6),(-3.48,y,2.87),.08,m['spar'],6)
    for z in (-.4,.2,.8,1.4,2,2.6):
        beam('Ladder_rung',(-3.6,-.55,z),(-3.6,.55,z),.07,m['spar'],6)


def normalize():
    visual=scene.meshes()
    bpy.context.view_layer.update()
    baseline=[o for o in visual if not any(g.name in (lookout_cannon.EXTRA_GROUPS | {'Rudder','HelmWheel','HelmStand','Shields','ShieldMounts'}) for g in o.vertex_groups)]
    low,high=scene.bounds(baseline)
    factor=Vector((1.0000044107437134,1.1080677509307861,1.0366826057434082))
    for obj in visual:
        scene.select_only([obj])
        bpy.ops.object.convert(target='MESH')
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        for v in obj.data.vertices:
            v.co=Vector([v.co[i]*factor[i] for i in range(3)])
        bm=bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bmesh.ops.triangulate(bm,faces=[face for face in bm.faces if len(face.verts)>4])
        bm.to_mesh(obj.data)
        bm.free()
    return factor


def unwrap_ship(obj):
    scene.select_only([obj])
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.0001)
    bpy.ops.uv.average_islands_scale()
    bpy.ops.uv.pack_islands(rotate=True,rotate_method='ANY',margin=.002,
                            margin_method='FRACTION',shape_method='CONCAVE')
    bpy.ops.object.mode_set(mode='OBJECT')


def colliders(f):
    for obj in list(scene.meshes()):
        if obj.name.startswith(('Upper_deck','Hold_floor','Stair_tread','Nest_floor')):
            lo,hi=scene.bounds([obj])
            shapes.collider_box(obj.name,hi-lo,(hi+lo)/2)


def build():
    from workshop import bake
    bake.unwrap=unwrap_ship
    m=materials()
    hull(m['wood'])
    rail(m['dark'])
    # Only the original deck function's keel closure is still needed.
    before=set(scene.meshes())
    deck(m['wood'][1])
    for obj in set(scene.meshes())-before:
        if obj.name.startswith('Deck_plank'):
            bpy.data.objects.remove(obj,do_unlink=True)
    deckworks.build(m)
    framing(m)
    posts(m)
    spars(m)
    fittings(m)
    lookout_cannon.build(m)
    ship_fittings.build(m)
    f=normalize()
    bpy.context.scene['ship_scale']=list(f)
    bpy.context.view_layer.update()
    lo,hi=scene.bounds(scene.meshes())
    bpy.context.scene['ship_target']=list(hi-lo)
    colliders(f)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.dirname(__file__),'out','havorn_construction.blend'))
