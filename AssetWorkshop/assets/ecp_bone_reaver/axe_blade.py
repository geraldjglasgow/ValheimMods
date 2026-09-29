"""One solid blade with an exposed cutting bevel; blood conforms to both faces."""
import math
import bpy
from mathutils import Vector
from geometry import mesh
from body import rigid
import bone_surface


def outline():
    rear=[(-.075,2.13),(.17,2.25),(.43,2.35),(.69,2.405)]
    # Convex cutting edge. Keep its two terminal points sharp, without spline overshoot.
    edge=[]
    a,b,c=Vector((.855,2.46)),Vector((1.105,2.045)),Vector((.825,1.615))
    for i in range(33):
        t=i/32
        p=(1-t)**2*a+2*t*(1-t)*b+t*t*c
        if i in (7,19,26): p.x-=.014 if i==7 else .022
        edge.append(tuple(p))
    return rear+edge+[(.56,1.71),(.29,1.86),(.10,1.98),(-.075,1.99)]


def blade(bone,edge):
    points=outline(); n=len(points)
    center=Vector((.43,2.065))
    scales=(.04,.10,.16,.22,.28,.34,.40,.46,.52,.58,.64,.70,.76,.81,.85,.89)
    rings=[(s,-1) for s in scales]+[(1,0)]+[(s,1) for s in reversed(scales)]
    vertices=[]
    for scale,side in rings:
        for x,z in points:
            p=center+(Vector((x,z))-center)*scale
            vertices.append((p.x,side*relief(p.x,p.y,scale),p.y))
    faces=[tuple(reversed(range(n)))]
    polished=[]
    for band in range(len(rings)-1):
        for j in range(n):
            faces.append((band*n+j,band*n+(j+1)%n,(band+1)*n+(j+1)%n,(band+1)*n+j))
            if band in (15,16) and 4<=j<36:
                polished.append(len(faces)-1)
    faces.append(tuple(range((len(rings)-1)*n,len(rings)*n)))
    obj=mesh('ScapulaBlade_HonedEdge',vertices,faces,bone+edge)
    obj.data.materials.append(bone_surface.material())
    for p in obj.data.polygons:
        p.material_index=8; p.use_smooth=True
    for index in polished:
        obj.data.polygons[index].material_index=6
        obj.data.polygons[index].use_smooth=False
    bpy.context.view_layer.update()
    bone_surface.uv(obj)
    return obj


def relief(x,z,radius):
    """Sculpted bone plate: thick root, forked buttress ridges, and a recessed central fossa."""
    envelope=min(1,max(0,(1-radius)/.13))
    base=.115-.048*max(0,min(1,x/.95))
    upper=.075*math.exp(-((z-(2.075+.43*(x-.05)))/.042)**2)
    lower=.055*math.exp(-((z-(2.035-.43*(x-.05)))/.038)**2)
    socket=.065*math.exp(-((x-.12)/.18)**2-((z-2.07)/.18)**2)
    hollow=.055*math.exp(-((x-.56)/.22)**2-((z-2.075)/.12)**2)
    small=.012*math.sin(x*27+z*13)*math.sin(z*31-x*7)
    pits=sum(.012*math.exp(-((x-cx)/.018)**2-((z-cz)/.025)**2)
             for cx,cz in ((.22,2.13),(.26,2.16),(.29,2.20),(.39,2.27),(.57,1.89),(.62,1.90),(.68,2.25)))
    return max(.025,base+upper+lower+socket-hollow+small-pits-bone_surface.groove(x,z))*envelope


def build(rig,bone,edge):
    obj=blade(bone,edge)
    rigid(obj,rig,'axe')
    return obj
