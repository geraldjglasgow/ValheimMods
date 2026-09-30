"""Authoring pivots, ladder anchors and a portable 180-degree swivel contract."""
import json
import math
import os
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from workshop import scene
from lookout_cannon import CANNON_PIVOT,BARREL_PIVOT,HATCH,LADDER_Y


def scaled(point,factor):
    return Vector([point[i]*factor[i] for i in range(3)])


def origin(obj,point):
    for v in obj.data.vertices:
        v.co-=point
    obj.location=point


def parent(child,root):
    world=child.matrix_world.copy()
    child.parent=root
    child.matrix_world=world


def anchor(name,point):
    obj=bpy.data.objects.new(name,None)
    bpy.context.collection.objects.link(obj)
    obj.location=point
    obj.empty_display_type='ARROWS'
    obj.empty_display_size=.18
    return obj


def cannon(parts,factor):
    base,yaw,barrel=[parts[n] for n in ('CannonBase','CannonYaw','CannonBarrel')]
    origin(base,scaled((0,-7.8,2.77),factor))
    origin(yaw,scaled(CANNON_PIVOT,factor))
    origin(barrel,scaled(BARREL_PIVOT,factor))
    bpy.context.view_layer.update()
    parent(yaw,base)
    parent(barrel,yaw)
    limit=yaw.constraints.new('LIMIT_ROTATION')
    limit.name='Forward_180_degree_traverse'
    limit.owner_space='LOCAL'
    limit.use_limit_x=limit.use_limit_y=limit.use_limit_z=True
    limit.min_x=limit.max_x=limit.min_y=limit.max_y=0
    limit.min_z,limit.max_z=-math.pi/2,math.pi/2
    limit.use_transform_limit=True
    yaw['yaw_min_degrees']=-90.0
    yaw['yaw_max_degrees']=90.0
    yaw['forward_axis']='-Y in Blender; +Z in Unity'
    return yaw


def anchors(parts,factor):
    lane=LADDER_Y+.27
    poses={'Ladder_Enter':(0,lane,2.77),'Ladder_ClimbBottom':(0,lane,3.02),
           'Ladder_ClimbTop':(0,lane,16.6),'Ladder_NestExit':(.84,.67,16.6),
           'Helm_Operator':(0,8.05,2.77),'Boarding_Exit':(-2.85,0,2.77),
           'Cannon_Operator':(0,-6.65,2.77),'Cannon_Muzzle':(0,-9.0,4.57)}
    result=[anchor(name,scaled(pos,factor)) for name,pos in poses.items()]
    bpy.context.view_layer.update()
    parent(next(o for o in result if o.name=='Cannon_Muzzle'),parts['CannonBarrel'])
    return result


def check_nest(parts,factor,report):
    left,right,near,far=HATCH
    for polygon in parts['Lookout'].data.polygons:
        c=polygon.center
        in_hatch=(left+.015)*factor[0]<c.x<(right-.015)*factor[0] and (near+.015)*factor[1]<c.y<(far-.015)*factor[1]
        assert not (in_hatch and 16.39*factor[2]<c.z<16.61*factor[2]),('Nest access blocked',list(c),list(factor))
    for obj in [o for o in scene.meshes() if o.name.startswith('col_box_Nest_floor')]:
        lo,hi=scene.bounds([obj])
        overlap=hi.x>(left+.015)*factor[0] and lo.x<(right-.015)*factor[0]
        overlap=overlap and hi.y>(near+.015)*factor[1] and lo.y<(far-.015)*factor[1]
        assert not overlap,'Nest collider blocks ladder'
    report['lookout_hatch_and_colliders_clear']=True
    report['mast_ladder_stile_clear_width_m']=.59*factor[0]
    report['mast_ladder_rung_spacing_m']=(16.5-3.02)/45*factor[2]
    report['lookout_floor_height_m']=16.6*factor[2]


def check_swivel(parts,report):
    yaw=parts['CannonYaw']
    measured=[]
    for angle,expected in ((-130,-90),(-90,-90),(0,0),(90,90),(130,90)):
        yaw.rotation_euler.z=math.radians(angle)
        bpy.context.view_layer.update()
        graph=bpy.context.evaluated_depsgraph_get()
        evaluated=yaw.evaluated_get(graph)
        relative=yaw.parent.matrix_world.inverted() @ evaluated.matrix_world
        actual=math.degrees(relative.to_euler().z)
        assert abs(actual-expected)<.05,(angle,actual,expected)
        measured.append({'requested_degrees':angle,'evaluated_degrees':round(actual,3)})
    yaw.rotation_euler.z=0
    bpy.context.view_layer.update()
    report['cannon_yaw_limit_checks']=measured


def check_climb_lane(parts,factor,report):
    vertices,faces=[],[]
    for name in ('UpperDeck','Rigging','Lookout'):
        obj=parts[name]
        offset=len(vertices)
        vertices.extend(obj.matrix_world @ v.co for v in obj.data.vertices)
        faces.extend(tuple(offset+i for i in p.vertices) for p in obj.data.polygons)
    tree=BVHTree.FromPolygons(vertices,faces)
    low,high=2.77*factor[2],16.6*factor[2]
    radius,height=.27,1.8
    minimum=100
    for step in range(151):
        foot=low+(high-low)*step/150
        for segment in range(10):
            z=foot+radius+(height-2*radius)*segment/9
            point=Vector((0,(LADDER_Y+.27)*factor[1],z))
            _,_,_,distance=tree.find_nearest(point)
            minimum=min(minimum,distance)
            assert distance>=radius-.003,('Climb lane obstructed',list(point),distance)
    report['sampled_climb_capsule']={'radius_m':radius,'height_m':height,
        'samples':1510,'minimum_surface_distance_m':round(minimum,5),
        'clear_of':'upper deck, mast, sail rigging and lookout; hand-contact ladder excluded',
        'note':'authoring clearance check, not an in-game player-controller test'}


def animate(yaw):
    yaw.rotation_mode='XYZ'
    for frame,angle in ((1,-90),(31,0),(61,90),(91,0),(121,-90)):
        yaw.rotation_euler.z=math.radians(angle)
        yaw.keyframe_insert(data_path='rotation_euler',index=2,frame=frame,group='Forward traverse')
    yaw.animation_data.action.name='Cannon_180_Degree_Sweep'
    bpy.context.scene.render.fps=30
    bpy.context.scene.frame_start=1
    bpy.context.scene.frame_end=121
    bpy.context.scene.frame_set(31)


def metadata(out,factor,objects):
    def unity(v):
        return [round(v.x,5),round(v.z,5),round(-v.y,5)]
    data={'version':1,'status':'authoring rig; Valheim interaction controller not implemented',
          'coordinate_system':'Unity local metres, +Y up, +Z bow',
          'anchors':{o.name:unity(o.matrix_world.translation) for o in objects},
          'ladder':{'visual':'MastLadder','landing':'Lookout','clear_width_m':round(.59*factor[0],4),
                    'enter':'Ladder_Enter','climb_start':'Ladder_ClimbBottom',
                    'climb_end':'Ladder_ClimbTop','exit':'Ladder_NestExit'},
          'cannon':{'fixed_base':'CannonBase','yaw_node':'CannonYaw','barrel_node':'CannonBarrel',
                    'yaw_axis':'local +Y','min_degrees':-90,'max_degrees':90,
                    'total_traverse_degrees':180,'center_direction':'ship forward +Z',
                    'muzzle':'Cannon_Muzzle','operator':'Cannon_Operator',
                    'preview_clip':'Cannon_180_Degree_Sweep'},
          'helm':{'wheel':'HelmWheel','stand':'HelmStand','rudder':'Rudder',
                  'wheel_axis_blender':'local Y','rudder_axis_blender':'local Z',
                  'operator':'Helm_Operator','status':'separate pivoted visual parts; no steering controller'},
          'gameplay_requirements':['owner-authoritative climb/aim state and multiplayer replication',
                                   'player attachment and climb interaction',
                                   'runtime yaw clamp matching this contract',
                                   'separate firing implementation if requested']}
    with open(os.path.join(out,'interactions.json'),'w') as file:
        json.dump(data,file,indent=2)


def fbx(path,objects):
    scene.select_only(objects)
    bpy.ops.export_scene.fbx(filepath=path,use_selection=True,object_types={'MESH','EMPTY'},
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',
        bake_space_transform=False,use_mesh_modifiers=True,mesh_smooth_type='FACE',use_tspace=True,
        add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=0,path_mode='STRIP',embed_textures=False,use_custom_props=True)
