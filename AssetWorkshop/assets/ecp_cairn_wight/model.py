"""Cairn Wight: cracked grave-bone face, cairn stones and ragged funeral hide.

Original geometry/paint. Root-space rest fit to Fenring; attach to Head while
preserving the root-space rest transform. No game content is exported.
"""
import math
import bpy
from mathutils import Vector
from workshop import materials
TEXTURE_SIZE=256
NORMAL_MAP=False
AO_STRENGTH=.7

def paint(name,dark,light):
    mat=materials.flat(name,light)
    tree=mat.node_tree; bs=materials.principled(mat)
    noise=tree.nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=7
    noise.inputs['Detail'].default_value=1.2
    coord=tree.nodes.new('ShaderNodeTexCoord');tree.links.new(coord.outputs['Generated'],noise.inputs['Vector'])
    ramp=tree.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position=.22;ramp.color_ramp.elements[0].color=(*dark,1)
    ramp.color_ramp.elements[1].position=.78;ramp.color_ramp.elements[1].color=(*light,1)
    tree.links.new(noise.outputs['Fac'],ramp.inputs[0]);tree.links.new(ramp.outputs[0],bs.inputs['Base Color'])
    return mat

def lump(name,loc,size,mat,sub=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc)
    o=bpy.context.object;o.name=name;o.scale=size;o.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    return o

def horn(name,points,radii,mat,sides=6):
    verts=[]
    for i,(p,r) in enumerate(zip(points,radii)):
        p=Vector(p); direction=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
        axis=direction.normalized(); a=axis.cross(Vector((0,1,0))).normalized(); b=axis.cross(a)
        verts.extend(p+r*(a*math.cos(j*2*math.pi/sides)+b*math.sin(j*2*math.pi/sides)) for j in range(sides))
    faces=[tuple(range(sides-1,-1,-1))]
    for i in range(len(points)-1):
        for j in range(sides):
            k=i*sides+j;n=i*sides+(j+1)%sides;faces.append((k,n,n+sides,k+sides))
    faces.append(tuple(range((len(points)-1)*sides,len(points)*sides)))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob);mesh.materials.append(mat)
    return ob

def build():
    bone=paint('old ochre bone',(.105,.082,.049),(.48,.405,.275))
    worn=paint('worn bone edges',(.20,.16,.10),(.58,.50,.35))
    stone=paint('cairn slate',(.037,.043,.039),(.16,.175,.155))
    hide=paint('burial pelt',(.024,.019,.015),(.12,.087,.054))
    # Brow and cheeks surround real holes; the eyes and jaw of the living base remain visible.
    lump('brow ridge',(0,-.32,3.27),(.34,.15,.13),bone,2)
    lump('nasal bridge',(0,-.49,3.08),(.10,.12,.24),worn,1)
    for s in [-1,1]:
        lump('temple bone',(s*.29,-.285,3.13),(.115,.16,.22),bone,1)
        horn('cheek tusk',[(s*.29,-.36,3.05),(s*.26,-.45,2.89),(s*.18,-.47,2.80)],[.08,.064,.004],worn)
        horn('broken antler',[(s*.26,-.19,3.30),(s*.40,-.12,3.53),(s*.51,.02,3.66)],[.11,.075,.013],bone)
        horn('antler tine',[(s*.39,-.10,3.51),(s*.60,-.12,3.58)],[.055,.006],bone)
    # Ragged strips form a mantle close to the existing neck fur, avoiding rigid arm geometry.
    for i in range(11):
        a=2*math.pi*i/11;x=.39*math.cos(a);y=.07+.34*math.sin(a)
        horn('ragged hide',[(x,y,2.93),(x*1.30,y,2.72),(x*1.18,y,2.47+(i%3)*.055)],[.125,.11,.008],hide,5)
    for i in range(5):
        x=(i-2)*.16
        lump('grave stone',(x,.21,3.30+(.15 if i==2 else .04)),(.13,.15,.19),stone)

