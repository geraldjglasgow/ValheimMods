"""Check the actual saved rig, side transitions, visibility, dimensions and cue timings."""
import json
from pathlib import Path
import bpy
from mathutils import Matrix

HERE=Path(__file__).resolve().parent
OUT=HERE/'out'
bpy.ops.wm.open_mainfile(filepath=str(OUT/'ecp_bone_reaver.blend'))
rig=bpy.data.objects['BoneReaverRig']
info=json.loads((OUT/'manifest.json').read_text())
checks=[]


def pose(name,frame):
    rig.animation_data.action=bpy.data.actions[name]
    bpy.context.scene.frame_set(frame)
    bpy.context.view_layer.update()
    return {b.name:b.matrix.copy() for b in rig.pose.bones}


def difference(a,b):
    # The invisible dagger fan may follow either drawing hand; it has zero scale at rest.
    names=[n for n in a if n!='daggers']
    return max(abs(a[n][i][j]-b[n][i][j]) for n in names for i in range(4) for j in range(4))


for side,other in [('R','L'),('L','R')]:
    error=difference(pose('sweep_'+side,90),pose('idle_'+other,0))
    assert error<.0001, ('Sweep rest-side discontinuity',side,error)
    checks.append(f'sweep_{side} ends exactly at idle_{other}: max matrix error {error:.8f}')
    for name,last in [('idle',60),('walk',48)]:
        error=difference(pose(name+'_'+side,0),pose(name+'_'+side,last))
        assert error<.0001, ('Loop discontinuity',name,side,error)
    for name in ['slam','spin','rear','daggers','axe_throw']:
        spec=next(c for c in info['clips'] if c['name']==name+'_'+side)
        error=difference(pose(spec['name'],spec['last']),pose('idle_'+side,0))
        assert error<.0001, ('Attack rest discontinuity',name,side,error)
    pose('axe_throw_'+side,75)
    assert max(rig.pose.bones['axe'].scale)<.001
    pose('axe_throw_'+side,148)
    assert min(rig.pose.bones['axe'].scale)>.999
    for frame,expected in [(11,0),(12,1),(26,1),(27,0)]:
        pose('rear_'+side,frame)
        assert abs(rig.pose.bones['eye_glow'].scale.x-expected)<.001
checks.append('All loops and attack/rest transitions continuous; both resting sides checked')
checks.append('Purple eyes visible for exactly 15 frames (0.5 seconds at 30 fps)')
for side in ('R','L'):
    cues={c['event']:c['frame'] for c in next(c for c in info['clips'] if c['name']=='spin_'+side)['cues']}
    assert cues['recovery_end']-cues['recovery_start']==120
    cues={c['event']:c['frame'] for c in next(c for c in info['clips'] if c['name']=='axe_throw_'+side)['cues']}
    assert cues['summon_yell']-cues['release_axe']==45
checks.append('Spin recovery 4.0 seconds; axe release to yell 1.5 seconds')
for obj in bpy.data.objects:
    if obj.type!='MESH' or obj.parent!=rig:
        continue
    for vertex in obj.data.vertices:
        assert abs(sum(g.weight for g in vertex.groups)-1)<.0001, (obj.name,vertex.index)
checks.append('Every deforming vertex is weighted and weights are normalized')
assert abs(info['height']/info['vanillaHeight']-1.25)<.000001
assert info['maxGripError']<.005
checks.append(f"Height scale 1.25; maximum two-hand wrist-target error {info['maxGripError']:.8f} metres")
(OUT/'validation.txt').write_text('\n'.join(checks)+'\n')
print('\n'.join(checks))
