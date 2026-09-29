"""A continuous S-shaped spine, straight at the two grip seats and the blade socket."""
import math


def center(z):
    t=max(0,min(1,(z-.72)/1.38))
    envelope=math.sin(math.pi*t)
    return (.075*math.sin(2*math.pi*t)*envelope+.010*envelope*envelope,
            .025*envelope*envelope)


def bend(rig):
    for obj in rig.children:
        if obj.type!='MESH' or not obj.vertex_groups.get('axe'):
            continue
        for v in obj.data.vertices:
            x,y=center(v.co.z)
            v.co.x+=x; v.co.y+=y
        obj.data.update()
