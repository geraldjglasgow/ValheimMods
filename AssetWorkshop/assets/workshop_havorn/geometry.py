"""Original ship geometry, in metres. No imported geometry."""
import math
import bpy
from mathutils import Vector
from workshop import shapes


def mesh(name, verts, faces, mat, group='Hull'):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    obj.vertex_groups.new(name=group).add(list(range(len(verts))), 1, 'REPLACE')
    return obj


def beam(name, a, b, radius, mat, sides=8, group='Hull'):
    a, b = Vector(a), Vector(b)
    obj = shapes.cylinder(name, radius, (b-a).length, (a+b)/2, material=mat, vertices=sides)
    obj.rotation_euler = (b-a).to_track_quat('Z', 'Y').to_euler()
    obj.vertex_groups.new(name=group).add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    return obj


def box(name, size, loc, mat, group='Hull'):
    obj = shapes.box(name, size, loc, material=mat)
    obj.vertex_groups.new(name=group).add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    return obj


def ribbon(name, rings, mat, group='Hull'):
    verts = [v for ring in rings for v in ring]
    n = len(rings[0])
    faces = [tuple(reversed(range(n)))]
    for j in range(len(rings)-1):
        for k in range(n):
            faces.append((j*n+k, j*n+(k+1)%n, (j+1)*n+(k+1)%n, (j+1)*n+k))
    faces.append(tuple((len(rings)-1)*n+k for k in range(n)))
    return mesh(name, verts, faces, mat, group)


def width(t):
    half=3.45
    return half * max(0.018, math.cos(t*math.pi/2))**0.72


def hullpoint(t, f, side):
    base=-1.75+1.7*abs(t)**3+.65*abs(t)**4
    top=2.65
    return (side*width(t)*(0.13+0.87*math.sin(f*math.pi/2)),
            t*9.75, base+(top-base)*f)


def hull(mats):
    for side in (-1, 1):
        for row in range(7):
            rings = []
            for i in range(25):
                t = -1+2*i/24
                a, b = hullpoint(t,row/7,side), hullpoint(t,(row+1)/7+0.004,side)
                a=(a[0]+side*.095,a[1],a[2])
                rings.append([a,b,(b[0]-side*0.12,b[1],b[2]),(a[0]-side*0.12,a[1],a[2])])
            ribbon('Clinker_%s_%02d'%(side,row),rings,mats[row%len(mats)])


def rail(mat):
    for side in (-1,1):
        rings=[]
        for i in range(25):
            p=Vector(hullpoint(-1+2*i/24,1.02,side))
            rings.append([p+Vector((x,0,z)) for x,z in [(-.14,-.13),(.14,-.13),(.14,.13),(-.14,.13)]])
        ribbon('Tarred_gunwale',rings,mat)


def deck(mat):
    for i in range(-6,7):
        x=i*.325
        extent=min(8.15,9.75*2/math.pi*math.acos(((abs(x)+.17)/2.16)**(1/.72)))
        box('Deck_plank_%02d'%i,(.305,extent*2,.16),(x,0,.25),mat)
    rings=[]
    for i in range(25):
        t=-1+2*i/24
        a,b=hullpoint(t,0,-1),hullpoint(t,0,1)
        rings.append([a,b,(b[0],b[1],b[2]-.16),(a[0],a[1],a[2]-.16)])
    ribbon('Keel_bottom',rings,mat)


def stem(name, points, mat):
    rings=[]
    for y,z,w in points:
        rings.append([(-w,y-.15,z),(w,y-.15,z),(w,y+.15,z),(-w,y+.15,z)])
    return ribbon(name,rings,mat)


def sail(mat,red=None):
    verts=[]
    for j in range(9):
        v=j/8
        for i in range(17):
            u=i/16
            x=(u-.5)*(14.72-.9*v)
            y=-.35-1.35*math.sin(math.pi*u)*math.sin(math.pi*v*.9)
            y+=.11*math.sin(u*math.pi*12+v*3)*math.sin(math.pi*v)
            z=14.85-8.65*v+.35*math.sin(math.pi*u)*v
            verts.append((x,y,z))
    faces=[]
    for j in range(8):
        for i in range(16):
            a=j*17+i
            faces.append((a,a+1,a+18,a+17))
    obj=mesh('Square_sail',verts,faces,mat,'Sail')
    if red:
        obj.data.materials.append(red)
        for p in obj.data.polygons:
            i=p.index%16
            p.material_index=int(i<3 or i>=13 or i in (7,8))
    mod=obj.modifiers.new('Two_sided_cloth','SOLIDIFY')
    mod.thickness=.018
    return obj
