"""Check baked loops, planted idle paws, scurry ground clearance and performance travel."""
import os
import json
import sys
import bpy
from mathutils import Vector

OUT=os.path.join(os.path.dirname(os.path.abspath(__file__)),'out')
sys.path.insert(0,os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0,os.path.abspath(os.path.join(OUT,'..','..','..','blender')))
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'ecp_crypt_rat_animated.blend'))
rig=bpy.data.objects['crypt_rat_rig']
feet=[n+'Paw' for n in ('FrontL','FrontR','BackL','BackR')]
report={}
for name,last in [('idle',150),('scurry',8),('walk',30),('run',8)]:
    rig.animation_data.action=bpy.data.actions[name]
    bpy.context.scene.frame_set(0)
    first={b.name:b.matrix.copy() for b in rig.pose.bones}
    bpy.context.scene.frame_set(last)
    error=max(abs(b.matrix[r][c]-first[b.name][r][c]) for b in rig.pose.bones for r in range(4) for c in range(4))
    assert error<.002,(name,'loop discontinuity',error)
    report[name+'_loop_matrix_error']=error
rig.animation_data.action=bpy.data.actions['idle']
positions={name:[] for name in feet}
for frame in range(151):
    bpy.context.scene.frame_set(frame)
    for name in feet:
        positions[name].append(rig.pose.bones[name].matrix.translation.copy())
drift=max((point-points[0]).length for points in positions.values() for point in points)
assert drift<.002,('idle feet drift',drift)
report['idle_paw_drift_metres']=drift
rig.animation_data.action=bpy.data.actions['scurry']
heights=[]
for frame in range(9):
    bpy.context.scene.frame_set(frame)
    heights.extend(rig.pose.bones[name].matrix.translation.z for name in feet)
assert min(heights)>.05,('paw sinks',min(heights))
assert max(heights)>.13,('no paw lift',max(heights))
report['scurry_paw_height_range']=[min(heights),max(heights)]
assert len(rig.data.bones)==25
report['bones']=len(rig.data.bones)
report['clips']=[a.name for a in bpy.data.actions]
from lunge import travel,TAKEOFF,FRONT_LAND
rig.animation_data.action=bpy.data.actions['attack_bite']
for phase,names,frames in [('windup',feet,range(0,15)),
                          ('push_off',['BackLPaw','BackRPaw'],range(14,int(TAKEOFF)+1)),
                          ('front_landing',['FrontLPaw','FrontRPaw'],range(int(FRONT_LAND)+1,29))]:
    samples={name:[] for name in names}
    for frame in frames:
        bpy.context.scene.frame_set(frame)
        for name in names:
            position=rig.pose.bones[name].matrix.translation.copy()
            position.y-=travel(frame)
            samples[name].append(position)
    drift=max((p-points[0]).length for points in samples.values() for p in points)
    report['lunge_'+phase+'_paw_drift_metres']=drift
    assert drift<.01,(phase,'paw slides',drift)
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'ecp_crypt_rat_performance.blend'))
rig=bpy.data.objects['crypt_rat_rig']
path=[]
for frame in range(bpy.context.scene.frame_end+2):
    bpy.context.scene.frame_set(frame)
    path.append(rig.location.copy())
assert (path[0]-path[-1]).length<.001
report['roam_distance_metres']=sum((b-a).length for a,b in zip(path,path[1:]))
report['max_frame_travel_metres']=max((b-a).length for a,b in zip(path,path[1:]))
assert report['max_frame_travel_metres']<.30,'teleport in performance'
report['performance_seconds']=(bpy.context.scene.frame_end+1)/30
report['scurry_speed_multiplier']=3
assert report['roam_distance_metres']>5,'missing travel'
with open(os.path.join(OUT,'tail_checks.json')) as handle:
    tail=json.load(handle)
assert tail['max_link_error']<.001,('tail stretches',tail)
assert tail['min_surface_height']>=0,('tail solver floor penetration',tail)
mesh=bpy.data.objects['ecp_crypt_rat']
tail_groups={g.index for g in mesh.vertex_groups if g.name.startswith('Tail')}
tail_vertices=[v.index for v in mesh.data.vertices if any(g.group in tail_groups for g in v.groups)]
lowest=1e9
for frame in range(0,bpy.context.scene.frame_end+1,2):
    bpy.context.scene.frame_set(frame)
    evaluated=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    lowest=min(lowest,min((evaluated.matrix_world @ evaluated.data.vertices[i].co).z for i in tail_vertices))
assert lowest>=-.0175,('tail mesh below floor',lowest)
report['tail_min_mesh_height_metres']=lowest
report['tail_max_link_error_metres']=tail['max_link_error']
with open(os.path.join(OUT,'motion_checks.json'),'w') as handle:
    json.dump(report,handle,indent=2)
print('WORKSHOP motion checks PASS',json.dumps(report))
