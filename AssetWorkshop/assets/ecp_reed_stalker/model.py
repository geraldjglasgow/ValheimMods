"""Reed Stalker: sodden reed shoulder mantle and wicker eel trap.
Original accessory geometry only; vanilla Draugr supplies the animated body.
Root-space metres, Z up, front -Y. Torso follows runtime chest attachment.
"""
import math
import random
import bpy
from mathutils import Vector
from workshop import materials, shapes

TEXTURE_SIZE = 256
NORMAL_MAP = False
AO_STRENGTH = 0.45

def rod(name, a, b, radius, mat, sides=5):
    a,b=Vector(a),Vector(b)
    ob=shapes.cylinder(name,radius,(b-a).length,(a+b)/2,material=mat,vertices=sides)
    ob.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return ob

def polygon(name, verts, faces, mat):
    mesh=bpy.data.meshes.new(name)
    mesh.from_pydata(verts,[],faces)
    mesh.update()
    ob=bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(mat)
    return ob

def mottled(name, dark, light):
    mat=materials.flat(name,dark,roughness=0.96)
    nodes=mat.node_tree.nodes
    noise=nodes.new('ShaderNodeTexNoise'); noise.inputs['Scale'].default_value=27
    noise.inputs['Detail'].default_value=1
    ramp=nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.interpolation='CONSTANT'
    ramp.color_ramp.elements[0].color=(*dark,1)
    ramp.color_ramp.elements[1].color=(*light,1)
    ramp.color_ramp.elements[1].position=.54
    mat.node_tree.links.new(noise.outputs['Fac'],ramp.inputs['Fac'])
    mat.node_tree.links.new(ramp.outputs['Color'],materials.principled(mat).inputs['Base Color'])
    return mat

def build():
    rng=random.Random(721)
    reed=mottled('waterlogged_ochre_reed',(.052,.058,.019),(.145,.139,.054))
    moss=mottled('black_olive_moss',(.023,.032,.009),(.081,.092,.029))
    bark=mottled('peat_willow',(.032,.020,.010),(.095,.061,.024))
    cord=mottled('rotted_flax',(.086,.075,.040),(.164,.143,.077))
    # Layered flat leaves open around the neck; deliberately irregular and asymmetric.
    for layer in range(2):
        for i in range(27):
            angle=2*math.pi*i/27 + layer*.11
            c,s=math.cos(angle),math.sin(angle)
            x=.39*c; y=.245*s+.015
            z=1.84-layer*.08+rng.uniform(-.025,.025)
            length=rng.uniform(.24,.43)+(0.13 if s>0 else 0)
            tip=(x*1.38+rng.uniform(-.035,.035),y*1.58,z-length)
            half=.034+rng.random()*.012
            verts=[(x-half*s,y+half*c,z),(x+half*s,y-half*c,z),
                   (tip[0]+half*.35*s,tip[1]-half*.35*c,tip[2]+.10),tip,
                   ((x+tip[0])*.5,(y+tip[1])*.5-.016,z-length*.42)]
            polygon('ragged_reed',verts,[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],reed if i%3 else moss)
    # Conical basket rests vertically on the back, visibly hand-woven.
    for i in range(12):
        t=i*math.tau/12
        rod('trap_rib',(.18*math.cos(t),.36+.14*math.sin(t),.96),
            (.23*math.cos(t),.36+.18*math.sin(t),1.61),.012,bark)
    for j in range(9):
        z=.98+j*.076
        q=(z-.96)/.65
        points=[((.18+.05*q)*math.cos(i*math.tau/12),.36+(.14+.04*q)*math.sin(i*math.tau/12),z) for i in range(12)]
        for i in range(12): rod('wicker_weave',points[i],points[(i+1)%12],.009,reed)
    for side in [-1,1]:
        rod('shoulder_lashing',(side*.23,.34,1.57),(side*.21,-.18,1.63),.023,cord,6)
        rod('chest_lashing',(side*.21,-.18,1.63),(-side*.18,-.235,1.20),.019,cord,6)
    # Broken reeds poke out of the trap to form an identifiable serrated outline.
    for i in range(9):
        x=rng.uniform(-.16,.16)
        rod('bundled_cut_reed',(x,.36,1.36),(x+rng.uniform(-.08,.08),.35,1.86+rng.random()*.17),.013,reed)
