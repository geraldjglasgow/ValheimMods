"""Original mounted shields, captain's wheel and centreline stern rudder."""
import math
from mathutils import Vector
from geometry import mesh, beam, box, width
from deckworks import inside, UPPER

WHEEL=(0,7.35,3.8)
RUDDER=(0,10.08,2.8)


def shields(m):
    for side in (-1,1):
        for k,y in enumerate((-5.7,-3.75,-1.8,2.9,5.7)):
            slope=(width((y+.01)/9.75)-width((y-.01)/9.75))/.02
            normal=Vector((side,-slope,0)).normalized()
            tangent=Vector((-normal.y,normal.x,0))
            center=Vector((side*width(y/9.75),y,3.18))+normal*.36
            def point(depth,u,v):
                return center+normal*depth+tangent*u+Vector((0,0,v))
            verts=[]
            for depth,r in ((-.08,.5),(.08,.5),(.08,.44)):
                verts.extend(point(depth,r*math.cos(j*math.tau/12),r*math.sin(j*math.tau/12)) for j in range(12))
            verts.extend((point(.11,0,0),point(-.08,0,0)))
            faces=[]
            slots=[]
            for j in range(12):
                n=(j+1)%12
                faces.extend(((37,n,j),(j,n,n+12,j+12),(j+12,n+12,n+24,j+24),(36,j+24,n+24)))
                slots.extend((0,1,1,2+(j//3+k)%2))
            obj=mesh('Mounted_shield',verts,faces,m['wood'][1],'Shields')
            for material in (m['iron'],m['red'],m['blue']):
                obj.data.materials.append(material)
            for face,index in zip(obj.data.polygons,slots):
                face.material_index=index
            beam('Shield_boss',point(.105,0,0),point(.245,0,0),.125,m['iron'],8,'Shields')
            for u in (-.22,.22):
                q=center+tangent*u
                rail=Vector((side*inside(q.y,UPPER),q.y,0))
                depth=(rail-q).dot(normal)
                top=3.65+.25*(abs(q.y)/9.55)**3
                # One closed, bent flat strap: outside leg, top bridge, downward return.
                outer=-.08
                inner=depth-.15
                outline=[(outer,3.43),(outer,top+.15),(inner,top+.15),
                         (inner,top-.28),(inner+.055,top-.28),
                         (inner+.055,top+.095),(outer-.055,top+.095),(outer-.055,3.43)]
                verts=[q+normal*d+tangent*w+Vector((0,0,z-q.z))
                       for w in (-.045,.045) for d,z in outline]
                n=len(outline)
                faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
                faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
                mesh('Downward_shield_hook',verts,faces,m['iron'],'ShieldMounts')


def helm(m):
    c=Vector(WHEEL)
    for i in range(16):
        a,b=i*math.tau/16,(i+1)*math.tau/16
        p=c+Vector((.65*math.cos(a),0,.65*math.sin(a)))
        q=c+Vector((.65*math.cos(b),0,.65*math.sin(b)))
        beam('Wheel_rim',p,q,.075,m['dark'],6,'HelmWheel')
    for i in range(8):
        a=i*math.tau/8
        direction=Vector((math.cos(a),0,math.sin(a)))
        beam('Wheel_spoke',c,c+direction*.83,.045,m['spar'],6,'HelmWheel')
        beam('Wheel_grip',c+direction*.69,c+direction*.86,.068,m['wood'][1],6,'HelmWheel')
    beam('Wheel_hub',(0,7.22,3.8),(0,7.48,3.8),.16,m['iron'],10,'HelmWheel')
    box('Helm_foot',(.7,.7,.15),(0,6.97,2.845),m['dark'],'HelmStand')
    box('Helm_pedestal',(.25,.3,.96),(0,6.97,3.32),m['spar'],'HelmStand')
    beam('Helm_axle',(0,6.88,3.8),(0,7.34,3.8),.09,m['iron'],8,'HelmStand')
    # Visible paired steering ropes lead around the operator space to the stern.
    for side in (-1,1):
        points=[(side*.12,6.97,3.65),(side*.92,7.1,2.91),(side*.65,8.45,3.0),(side*.2,9.7,3.15),(side*.3,10.08,3.12)]
        for a,b in zip(points,points[1:]):
            beam('Steering_rope',a,b,.028,m['rope'],5,'HelmStand')
    beam('Stern_rudder_stock',(0,10.08,-1.3),(0,10.08,3.22),.115,m['spar'],8,'Rudder')
    beam('Rudder_quadrant',(-.32,10.08,3.12),(.32,10.08,3.12),.07,m['iron'],8,'Rudder')
    outline=[(10.04,1.4),(10.9,1.12),(11.45,-.95),(11.22,-1.55),(10.04,-1.55)]
    n=len(outline)
    verts=[(x,y,z) for x in (-.12,.12) for y,z in outline]
    faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh('Stern_rudder_blade',verts,faces,m['wood'][1],'Rudder')
    for z in (.45,1.6,2.6):
        beam('Rudder_hinge',(0,9.63,z),(0,10.08,z),.085,m['iron'],8,'HelmStand')
        beam('Rudder_gudgeon',(0,10.08,z-.09),(0,10.08,z+.09),.15,m['iron'],8,'HelmStand')


def build(m):
    shields(m)
    helm(m)
