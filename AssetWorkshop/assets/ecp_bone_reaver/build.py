"""Build editable creature, side-aware animations, and local reference previews."""
import json
import sys
from pathlib import Path

HERE=Path(__file__).resolve().parent
sys.path[:0]=[str(HERE),str(HERE.parents[1]/'blender')]
import bpy
from workshop import scene
import body
import equipment
import motion

OUT=HERE/'out'
OUT.mkdir(exist_ok=True)
scene.clear()
bpy.context.scene.render.fps=motion.FPS
rig,skins=body.build()
equipment.build(rig)
motion.build(rig)
bpy.context.scene.frame_end=60
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'ecp_bone_reaver.blend'))
report={'asset':'ecp_bone_reaver','basePrefab':'Skeleton','heightMultiplier':1.25,
        'vanillaHeight':rig['vanilla_height_m'],'height':rig['height_m'],
        'bones':len(rig.data.bones),'clips':motion.CLIPS,
        'maxIkError':max(motion.ERRORS),
        'maxGripError':max(motion.GRIP_ERRORS),
        'meanIkError':sum(motion.ERRORS)/len(motion.ERRORS),
        'referenceMeshes':[o.name for o in skins],
        'triangles':sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in bpy.data.objects if o.type=='MESH')}
(OUT/'manifest.json').write_text(json.dumps(report,indent=2))
assert max(motion.GRIP_ERRORS)<.005, 'Two-handed contact must remain within 5 mm'
print('REAVER',json.dumps({k:v for k,v in report.items() if k!='clips'}),flush=True)
if '--render' in sys.argv:
    import stage
    stage.render(rig,OUT)
