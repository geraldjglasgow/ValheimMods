"""Bog Maw: root crown and curved corpse tusks fitted to vanilla Blob (metres)."""
import bpy, math, random
from mathutils import Vector
from workshop import materials
TEXTURE_SIZE=128
NORMAL_MAP=False
AO_STRENGTH=.45

def paint(name,dark,light,scale=8):
    m=materials.flat(name,dark,.95)
    t=m.node_tree;n=t.nodes.new('ShaderNodeTexNoise');n.inputs['Scale'].default_value=scale;n.inputs['Detail'].default_value=1
    ramp=t.nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.23;ramp.color_ramp.elements[0].color=(*dark,1)
    ramp.color_ramp.elements[1].position=.78;ramp.color_ramp.elements[1].color=(*light,1)
    t.links.new(n.outputs['Fac'],ramp.inputs[0]);t.links.new(ramp.outputs['Color'],materials.principled(m).inputs['Base Color'])
    return m

def mesh(name,verts,faces,mat):
    import bmesh
    m=bpy.data.meshes.new(name);m.from_pydata(verts,[],faces);m.update()
    bm=bmesh.new();bm.from_mesh(m);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(m);bm.free()
    o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);m.materials.append(mat);return o

def root(name,points,radii,mat,sides=5):
    vs=[]
    for i,p in enumerate(points):
        p=Vector(p);t=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)]);t.normalize()
        u=t.cross(Vector((0,1,0)))
        if u.length<.1:u=t.cross(Vector((1,0,0)))
        u.normalize();v=t.cross(u).normalized()
        vs.extend([tuple(p+radii[i]*(math.cos(j*math.tau/sides)*u+math.sin(j*math.tau/sides)*v)) for j in range(sides)])
    fs=[tuple(reversed(range(sides)))]
    for i in range(len(points)-1):
        for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
    fs.append(tuple((len(points)-1)*sides+j for j in range(sides)));return mesh(name,vs,fs,mat)

def plate(name,center,size,mat,seed=0):
    r=random.Random(seed);cx,cy,cz=center;sx,sy,sz=size
    vs=[(cx,cy,cz+sz)]
    for j in range(7):
        a=j*math.tau/7;w=r.uniform(.86,1.1);vs.append((cx+math.cos(a)*sx*w,cy+math.sin(a)*sy*w,cz+r.uniform(-.018,.018)))
    vs.append((cx,cy,cz-.035));fs=[]
    for j in range(7):a=1+j;b=1+(j+1)%7;fs.extend([(0,a,b),(8,b,a)])
    return mesh(name,vs,fs,mat)


