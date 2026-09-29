import sys
sys.path.insert(0, 'C:/Users/gglasgow/projects/ValheimMods/AssetWorkshop/blender')
import bpy
from workshop import prefab, scene
for path in ['Characters/Draugr/Draugr.prefab','Characters/Wraith/Wraith.prefab']:
    scene.clear()
    root = prefab.load(path)
    print('MEASURE',path,prefab.bounds(root))
    bpy.context.view_layer.update()
    for ob in bpy.data.objects:
        if any(s in ob.name.lower() for s in ['spine','neck','head','hip','shoulder']):
            print('BONE',ob.name,tuple(round(v,3) for v in ob.matrix_world.translation))
