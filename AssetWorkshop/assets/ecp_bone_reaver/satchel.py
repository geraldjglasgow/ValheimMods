"""A soft knife roll suspended from two belt loops, fitted to the outside of the hip."""
import math
from mathutils import Vector
from geometry import mesh, curved_rod, sphere
from body import rigid

DRAW_POINT=Vector((-.35,-.015,1.38))


def build(rig, leather, bone):
    def put(obj, group='Hips'):
        return rigid(obj,rig,group)
    # Cross sections make a flattened, sagging leather pouch with a rounded bottom.
    verts=[]
    sections=[(.95,.018,.045),(1.00,.042,.106),(1.13,.054,.125),(1.24,.043,.116)]
    for z,depth,width in sections:
        for j in range(12):
            a=j*math.tau/12
            verts.append((-.305-depth*math.cos(a)-.028*(1.24-z),.015+width*math.sin(a),z))
    faces=[tuple(reversed(range(12)))]
    for k in range(3):
        for j in range(12):
            faces.append((k*12+j,k*12+(j+1)%12,(k+1)*12+(j+1)%12,(k+1)*12+j))
    obj=mesh('SoftLeatherKnifeRoll',verts,[tuple(reversed(f)) for f in faces],leather)
    for p in obj.data.polygons: p.material_index=2; p.use_smooth=True
    put(obj)
    # Two load-bearing straps visibly loop over the hip belt; no floating flap.
    for y in (-.075,.095):
        points=[(-.235,y,1.34),(-.258,y,1.355),(-.292,y,1.31),(-.345,y,1.22),(-.358,y,1.15)]
        vertices=[(x,y+side*.017,z) for x,y,z in points for side in (-1,1)]
        front=[(i*2,i*2+1,i*2+3,i*2+2) for i in range(4)]
        put(mesh('BeltSuspensionStrap',vertices,front+[tuple(reversed(f)) for f in front],leather))
        put(sphere('BoneStrapStud',(-.362,y,1.17),(.012,.013,.013),bone,10,5))
    # Rolled mouth, stitched side seam and a lacing tie add construction detail.
    rim=[(-.305-.043*math.cos(a),.015+.116*math.sin(a),1.242)
         for a in [j*math.tau/24 for j in range(25)]]
    put(curved_rod('RolledPouchMouth',rim,[.008]*25,leather,6))
    for y in (-.105,.128):
        put(curved_rod('PouchSeam',[(-.316,y*.45,.965),(-.349,y,1.035),(-.358,y,1.16),(-.341,y,1.23)],
            [.005]*4,leather,6))
        for j in range(6):
            z=1.03+j*.029
            put(curved_rod('SaddleStitch',[(-.356,y-.008,z),(-.361,y+.008,z+.008)], [.0028,.0028],bone,5))
    # Handles fan along the pouch opening and lean with its weight.
    for i in range(3):
        y=-.063+i*.076
        a=Vector((-.316,y,1.18)); b=Vector((-.347,y+(i-1)*.018,1.40+(i%2)*.022))
        put(curved_rod('BagKnifeHandle',[a,a.lerp(b,.35),b],[.016,.019,.016],leather,10),'bag_daggers')
        put(sphere('BagKnifePommel',b,(.023,.024,.026),bone,10,5),'bag_daggers')
