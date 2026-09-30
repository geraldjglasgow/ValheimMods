"""Export articulated visual parts and an embedded GLB; check the actual delivery."""
import os
import sys
import json
import math
import bpy
from mathutils import Vector

HERE=os.path.dirname(os.path.abspath(__file__))
OUT=os.path.join(HERE,'out')
NAME='workshop_havorn'
sys.path.insert(0,os.path.abspath(os.path.join(HERE,'../../blender')))
sys.path.insert(0,HERE)
from workshop import scene, export
import interaction_rig


def separate_part(obj, group):
    scene.select_only([obj])
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='DESELECT')
    bpy.ops.object.mode_set(mode='OBJECT')
    idx=obj.vertex_groups[group].index
    for v in obj.data.vertices:
        v.select=any(g.group==idx and g.weight>.5 for g in v.groups)
    before=set(bpy.data.objects)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.separate(type='SELECTED')
    bpy.ops.object.mode_set(mode='OBJECT')
    part=(set(bpy.data.objects)-before).pop()
    part.name=group
    return part


def pivot(obj, point):
    point=Vector(point)
    for v in obj.data.vertices:
        v.co-=point
    obj.location=point


def finish():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,NAME+'.blend'))
    obj=bpy.data.objects[NAME]
    low,high=scene.bounds([obj])
    size=high-low
    expected=tuple(bpy.context.scene['ship_target'])
    assert all(abs(size[i]-expected[i])<.002 for i in range(3)),list(size)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    assert obj.data.uv_layers.active is not None
    assert all(p.area>1e-10 for p in obj.data.polygons)
    mat=obj.data.materials[0]
    for node in mat.node_tree.nodes:
        if node.type=='TEX_IMAGE':
            node.interpolation='Closest'
    parts={name:separate_part(obj,name) for name in
           ('Rudder','Sail','Rigging','UpperDeck','HoldFloor','Stairs','Cargo','DeckRail','Decoration',
            'MastLadder','Lookout','CannonBase','CannonYaw','CannonBarrel',
            'Shields','ShieldMounts','HelmWheel','HelmStand')}
    rudder,sail=parts['Rudder'],parts['Sail']
    f=bpy.context.scene['ship_scale']
    pivot(rudder,[0,10.08*f[1],2.8*f[2]])
    pivot(parts['HelmWheel'],[0,7.35*f[1],3.8*f[2]])
    pivot(sail,[0,-.35*f[1],14.85*f[2]])
    obj.name='Hull'
    visuals=[obj]+list(parts.values())
    yaw=interaction_rig.cannon(parts,f)
    markers=interaction_rig.anchors(parts,f)
    extras={}
    import fit_checks
    fit_checks.check(parts,obj,f,extras)
    fit_checks.check_finished_ends(parts,obj,f,extras)
    interaction_rig.check_nest(parts,f,extras)
    interaction_rig.check_climb_lane(parts,f,extras)
    interaction_rig.check_swivel(parts,extras)
    interaction_rig.animate(yaw)
    interaction_rig.metadata(OUT,f,markers)
    for image in bpy.data.images:
        if image.source=='FILE':
            image.pack()
    interaction_rig.fbx(os.path.join(OUT,'havorn_parts.fbx'),visuals+markers+[o for o in scene.meshes() if scene.is_collider(o)])
    scene.select_only(visuals+markers)
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUT,'havorn.glb'),export_format='GLB',use_selection=True,
                            export_extras=True,export_animations=True)
    for collider in [o for o in scene.meshes() if scene.is_collider(o)]:
        collider.hide_set(True)
        collider.hide_render=True
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'havorn_parts.blend'))
    names={o.name for o in visuals}
    report={'visual_dimensions_blender_xyz_m':list(size),'revision':'fitted bow and stern, full end decks and rails, shield hooks and raised forward cannon',
            'triangles':sum(scene.triangles(o) for o in visuals),'parts':[o.name for o in visuals],
            'finite_vertices':True,'nondegenerate_faces':True,'uvs_present':True,
            'gameplay_tested':False,'copied_game_assets':False}
    report.update(extras)
    report['hold_headroom_m']=(2.53-.37)*f[2]
    report['minimum_headroom_under_beams_m']=(2.31-.37)*f[2]
    report['stair_clear_width_m']=1.62*f[0]
    report['stair_rise_m']=.24*f[2]
    report['stair_tread_depth_m']=.36*f[1]
    report['stair_steps']=10
    # Check actual deck faces: no face bridges the companionway opening.
    deck=parts['UpperDeck']
    for p in deck.data.polygons:
        c=p.center
        assert not (-.97*f[0]<c.x<.97*f[0] and 1.36*f[1]<c.y<5.64*f[1]
                    and 2.3*f[2]<c.z<2.79*f[2]),'Blocked hatch'
    report['upper_deck_opening_clear']=True
    for c in [o for o in scene.meshes() if o.name.startswith('col_box_Upper_deck')]:
        lo,hi=scene.bounds([c])
        overlaps=hi.x>-.97*f[0] and lo.x<.97*f[0] and hi.y>1.36*f[1] and lo.y<5.64*f[1]
        assert not overlaps,'Deck collider covers stairs'
    report['upper_deck_colliders_leave_opening_clear']=True
    scene.clear()
    bpy.ops.import_scene.gltf(filepath=os.path.join(OUT,'havorn.glb'))
    imported=scene.meshes()
    assert {o.name for o in imported}==names
    low,high=scene.bounds(imported)
    assert all(abs((high-low)[i]-expected[i])<.002 for i in range(3))
    assert all(o.data.uv_layers.active and o.data.materials for o in imported)
    report['glb_roundtrip_dimensions_parts_uvs_materials']=True
    imported_yaw=bpy.data.objects['CannonYaw']
    assert imported_yaw.parent.name=='CannonBase'
    assert bpy.data.objects['CannonBarrel'].parent.name=='CannonYaw'
    assert all(bpy.data.objects.get(name) for name in ('Ladder_Enter','Ladder_NestExit','Cannon_Muzzle'))
    for frame,expected_angle in ((1,-90),(31,0),(61,90)):
        bpy.context.scene.frame_set(frame)
        relative=imported_yaw.parent.matrix_world.inverted() @ imported_yaw.matrix_world
        actual=math.degrees(relative.to_euler().z)
        assert abs(actual-expected_angle)<.1,('GLB aim',frame,actual)
    bpy.context.scene.frame_set(31)
    report['glb_swivel_hierarchy_anchors_and_animation']=True
    scene.clear()
    bpy.ops.import_scene.fbx(filepath=os.path.join(OUT,'havorn_parts.fbx'))
    fbx_parts=[o for o in scene.meshes() if not scene.is_collider(o)]
    assert {o.name for o in fbx_parts}==names
    lo,hi=scene.bounds(fbx_parts)
    assert all(abs((hi-lo)[i]-expected[i])<.005 for i in range(3)),('FBX bounds',list(hi-lo))
    assert bpy.data.objects['CannonYaw'].parent.name=='CannonBase'
    assert bpy.data.objects['CannonBarrel'].parent.name=='CannonYaw'
    report['fbx_roundtrip_dimensions_parts_and_swivel_hierarchy']=True
    with open(os.path.join(OUT,'validation.json'),'w') as file:
        json.dump(report,file,indent=2)
    print('HAVORN VALIDATED',json.dumps(report),flush=True)


finish()
