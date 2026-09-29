"""Broken iron circlet: original mesh, authored in Draugr Elite root coordinates."""
import math
import bpy
from workshop import materials
TEXTURE_SIZE=256
NORMAL_MAP=False
AO_STRENGTH=.3

def build():
    mat=materials.iron('black bog crown',(.085,.092,.058),(.13,.07,.027))
    verts=[]
    n=12
    # A low Norse iron circlet, with broad broken teeth instead of fantasy horns.
    for z in (2.235,2.335):
        for i in range(n):
            a=2*math.pi*i/n
            verts.append((.192*math.cos(a),-.032+.177*math.sin(a),z))
    faces=[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    for i in range(0,n,2):
        a=2*math.pi*(i+.45)/n
        height=(.115,.07,.145,.055,.125,.09)[i//2]
        verts.append((.202*math.cos(a),-.032+.187*math.sin(a),2.335+height))
        faces.append((i+n,(i+1)%n+n,len(verts)-1))
    mesh=bpy.data.meshes.new('jagged circlet')
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    obj=bpy.data.objects.new('jagged circlet',mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    mod=obj.modifiers.new('forged thickness','SOLIDIFY')
    mod.thickness=.028
