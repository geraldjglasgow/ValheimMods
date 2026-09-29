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

def build():
    bark=paint('waterlogged root',(.036,.041,.023),(.145,.13,.067))
    peat=paint('crusted peat',(.048,.052,.023),(.19,.18,.075))
    bone=paint('bog stained ivory',(.21,.18,.10),(.55,.49,.29),5)
    # Broad overlapping root crust, leaving the animated slime visible between pieces.
    for i,(x,y,z,sx,sy) in enumerate([(-.39,.0,.58,.29,.37),(.39,.05,.58,.30,.37),(-.18,.32,.74,.30,.30),(.20,.36,.73,.31,.32),(0,-.14,.85,.29,.29)]):
        plate('peat shield', (x,y,z),(sx,sy,.12),peat,i)
    for s in (-1,1):
        root('root cheek',[(s*.53,-.50,.18),(s*.65,-.52,.42),(s*.44,-.38,.68),(s*.18,-.12,.85)],[.115,.12,.11,.06],bark,6)
        root('curved corpse tusk',[(s*.54,-.60,.24),(s*.61,-.78,.30),(s*.65,-.91,.48),(s*.58,-.96,.66)],[.105,.088,.047,.002],bone,6)
        root('old crown root',[(s*.25,.16,.78),(s*.45,.19,1.03),(s*.55,.32,1.23),(s*.49,.43,1.40)],[.115,.095,.055,.002],bark)
        root('broken fork',[(s*.44,.20,1.0),(s*.68,.19,1.17),(s*.77,.25,1.24)],[.055,.027,.002],bark)
    for i in range(7):
        x=(i-3)*.128;z=.54-.11*abs(x)/.4
        root('uneven upper teeth',[(x,-.72,z),(x*.96,-.855,z-.07),(x*.90,-.86,z-.19-(i%2)*.025)],[.052,.036,.001],bone)
    root('root upper lip',[(-.45,-.64,.49),(-.22,-.73,.62),(0,-.73,.65),(.22,-.73,.60),(.44,-.64,.49)],[.075,.075,.085,.072,.07],bark,6)
