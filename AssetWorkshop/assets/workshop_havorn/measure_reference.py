import sys,json,os
sys.path.insert(0,os.path.abspath('AssetWorkshop/blender'))
from workshop import prefab,lineup,scene
import bpy
scene.clear()
r=prefab.load('GameElements/Ships/Karve.prefab')
ms=prefab.meshes(r)
for m in ms:
 lo,hi=prefab.bounds([m]); print('MEASURE',m.name,list(hi-lo),len(m.data.polygons),flush=True)
lo,hi=prefab.bounds(ms)
print('TOTAL',list(lo),list(hi),list(hi-lo),flush=True)
lineup.run([{'root':r,'meshes':ms,'label':'Karve reference','grip':None,'item':False}],os.path.abspath('AssetWorkshop/out/ship_reference'),views=('turn',))
