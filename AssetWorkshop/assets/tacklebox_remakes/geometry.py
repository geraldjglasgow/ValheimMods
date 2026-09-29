"""Shared construction tools only; each tier has its own body and lid geometry."""
import math
import bpy
from mathutils import Vector
from workshop import shapes


def mesh(name, verts, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces); data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(mat)
    return obj


def rod(name, a, b, r, mat, sides=6):
    a, b = Vector(a), Vector(b)
    obj = shapes.cylinder(name,r,(b-a).length,(a+b)/2,material=mat,vertices=sides)
    obj.rotation_euler = (b-a).to_track_quat('Z','Y').to_euler()
    return obj


def loft(name, rings, mat, caps=True):
    count = len(rings[0]); verts = [v for ring in rings for v in ring]
    faces = [(j*count+i,j*count+(i+1)%count,(j+1)*count+(i+1)%count,(j+1)*count+i)
             for j in range(len(rings)-1) for i in range(count)]
    if caps: faces += [tuple(reversed(range(count))),tuple((len(rings)-1)*count+i for i in range(count))]
    return mesh(name,verts,faces,mat)


def octagon(w,d,z,cut=.15):
    x,y=w/2,d/2; c=min(w,d)*cut
    return [(-x+c,-y,z),(x-c,-y,z),(x,-y+c,z),(x,y-c,z),
            (x-c,y,z),(-x+c,y,z),(-x,y-c,z),(-x,-y+c,z)]


def ellipse(w,d,z,n=12):
    return [(w/2*math.cos(i*2*math.pi/n),d/2*math.sin(i*2*math.pi/n),z) for i in range(n)]


def band(name, points, width, mat):
    # Runs in the Y/Z plane, with its width along X.
    verts = [(x-width/2,y,z) for x,y,z in points]+[(x+width/2,y,z) for x,y,z in points]
    n=len(points); faces=[(i,i+1,n+i+1,n+i) for i in range(n-1)]
    obj=mesh(name,verts,faces,mat)
    mod=obj.modifiers.new('leather_thickness','SOLIDIFY'); mod.thickness=.003
    return obj


def handle(w,z,mat,grip):
    for side in (-1,1):
        x=side*w*.2
        shapes.box('handle_anchor',(.033,.037,.006),(x,0,z),material=mat,bevel=.002)
        rod('handle_arch',(x,0,z),(x*.83,0,z+.065),.008,mat)
    rod('carrying_grip',(-w*.17,0,z+.065),(w*.17,0,z+.065),.014,grip,8)


def clasp(y,z,mat):
    shapes.box('clasp_shadow',(.036,.004,.065),(0,y+.002,z),material=mat,bevel=.002)
    rod('locking_toggle',(-.021,y-.006,z-.007),(.021,y-.006,z-.007),.004,mat)


def bobber(x,z,p):
    rod('fishing_line',(x-.03,0,z+.034),(x+.022,-.035,z-.018),.0018,p['rope'],4)
    shapes.cylinder('float_red',.010,.022,(x+.022,-.035,z-.029),material=p['red'],vertices=6)
    shapes.cylinder('float_white',.008,.020,(x+.022,-.035,z-.050),material=p['bone'],vertices=6)
    pts=[(x+.022,-.035,z-.06),(x+.022,-.035,z-.086),(x+.014,-.035,z-.092),(x+.006,-.035,z-.083)]
    for i in range(3): rod('hook',pts[i],pts[i+1],.0014,p['bronze'],5)


def stitch_row(y,z,half,mat):
    count=int(half*2/.019)
    for i in range(count):
        x=-half+i*.019
        rod('hand_seam',(x,y,z),(x+.008,y,z+.004),.0012,mat,4)
