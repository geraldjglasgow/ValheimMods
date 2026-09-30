"""Original lookout and fictional bow gun geometry; all distances are art units."""
import math
from geometry import beam,box,mesh,ribbon

EXTRA_GROUPS={'MastLadder','Lookout','CannonBase','CannonYaw','CannonBarrel'}
NEST_Z=16.5
HATCH=(-.47,.47,.68,1.58)
LADDER_Y=.75
CANNON_PIVOT=(0,-7.8,4.07)
BARREL_PIVOT=(0,-7.8,4.57)


def ladder(m):
    for x in (-.35,.35):
        top=16.55
        beam('Mast_ladder_stile',(x,LADDER_Y,2.77),(x,LADDER_Y,top),.055,m['spar'],6,'MastLadder')
    count=46
    for i in range(count):
        z=3.02+i*(16.5-3.02)/(count-1)
        beam('Mast_ladder_rung',(-.35,LADDER_Y,z),(.35,LADDER_Y,z),.038,m['spar'],6,'MastLadder')
    for z in (3.3,6.4,9.5,12.6,15.7):
        for x in (-.35,.35):
            beam('Ladder_standoff',(x*.45,.31,z),(x,LADDER_Y,z),.045,m['iron'],6,'MastLadder')


def nest_width(y):
    return math.sqrt(max(0,1.45**2-(y-.25)**2))


def nest_plank(a,b,cut,mat):
    left0,left1=-nest_width(a),-nest_width(b)
    right0,right1=-left0,-left1
    if cut=='left':
        right0=right1=HATCH[0]
    if cut=='right':
        left0=left1=HATCH[1]
    # The existing mast passes through the central planks; access uses the aft hatch.
    outline=[(left0,a),(right0,a),(right1,b),(left1,b)]
    verts=[(x,y,NEST_Z+dz) for dz in (-.1,.1) for x,y in outline]
    faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    mesh('Nest_floor',verts,faces,mat,'Lookout')


def lookout(m):
    edges=sorted(set([-1.18+i*2.86/14 for i in range(15)]+[HATCH[2],HATCH[3]]))
    for i,(a,b) in enumerate(zip(edges,edges[1:])):
        sides=('left','right') if HATCH[2]<=(a+b)/2<=HATCH[3] else (None,)
        for side in sides:
            nest_plank(a+.006,b-.006,side,m['wood'][i%3])
    for i in range(12):
        a,b=i*math.tau/12,(i+1)*math.tau/12
        p=(1.45*math.cos(a),.25+1.45*math.sin(a))
        q=(1.45*math.cos(b),.25+1.45*math.sin(b))
        if i!=3:
            beam('Nest_post',(*p,16.58),(*p,17.65),.065,m['spar'],6,'Lookout')
        for z,r in ((16.54,.095),(17.08,.05),(17.65,.075)):
            if i in (2,3):
                continue  # Open every railing level at the ladder landing.
            beam('Nest_ring',(*p,z),(*q,z),r,m['dark'] if z==17.65 else m['spar'],6,'Lookout')
    for x in (-.9,.9):
        beam('Nest_bracket',(0,.25,15.3),(x,.25,16.4),.105,m['spar'],6,'Lookout')
    beam('Nest_mast_tip',(0,.25,16.85),(0,.25,18.2),.12,m['spar'],8,'Lookout')
    mesh('Lookout_pennant',[(0,.25,18.12),(0,.25,17.72),(.85,.3,17.92)],[(0,1,2)],m['red'],'Lookout')


def cannon_base(m):
    beam('Cannon_deck_pad',(0,-7,2.77),(0,-7,2.96),.5,m['wood'][1],12,'CannonBase')
    beam('Cannon_pedestal',(0,-7,2.92),(0,-7,3.62),.19,m['dark'],8,'CannonBase')
    beam('Swivel_collar',(0,-7,3.58),(0,-7,3.73),.27,m['iron'],12,'CannonBase')
    for x in (-.3,.3):
        beam('Pedestal_brace',(x,-7,2.92),(0,-7,3.4),.075,m['iron'],6,'CannonBase')
    # Two visible stop pins bound the forward half-turn.
    for x in (-.31,.31):
        beam('Traverse_stop',(x,-7,3.66),(x,-7,3.82),.065,m['iron'],6,'CannonBase')


def cannon_yoke(m):
    box('Swivel_crossbar',(.8,.32,.16),(0,-7,3.8),m['iron'],'CannonYaw')
    for x in (-.34,.34):
        box('Yoke_cheek',(.16,.45,.48),(x,-7,4.04),m['dark'],'CannonYaw')
        beam('Yoke_band',(x-.095,-7,4.22),(x+.095,-7,4.22),.14,m['iron'],8,'CannonYaw')
    beam('Cannon_aiming_handle',(0,-6.52,4.13),(0,-5.98,3.97),.065,m['spar'],8,'CannonYaw')
    beam('Cannon_handgrip',(-.21,-5.98,3.97),(.21,-5.98,3.97),.07,m['dark'],8,'CannonYaw')


def barrel(m):
    # A hollow-looking low-poly prop muzzle, with no mechanical internals.
    rings=[]
    profile=[(-6.42,.14),(-6.51,.25),(-6.86,.275),(-7.3,.22),(-7.94,.205),(-8.1,.25),
             (-8.19,.25),(-8.19,.145),(-7.88,.145)]
    for y,r in profile:
        rings.append([(r*math.cos(i*math.tau/12),y,4.22+r*math.sin(i*math.tau/12)) for i in range(12)])
    ribbon('Mini_cannon_barrel',rings,m['gunmetal'],'CannonBarrel')
    # Dark inset keeps the bore legible without a large flat black muzzle cap.
    beam('Bore_shadow',(0,-7.885,4.22),(0,-7.88,4.22),.144,m['dark'],12,'CannonBarrel')
    for y,r in ((-6.7,.28),(-7.28,.235),(-8.075,.26)):
        hoop(y,r,m['iron'])
    beam('Trunnion',(-.4,-7,4.22),(.4,-7,4.22),.10,m['iron'],8,'CannonBarrel')


def hoop(y,r,mat):
    sections=((y-.045,r),(y+.045,r),(y+.045,r-.025),(y-.045,r-.025))
    verts=[(rad*math.cos(i*math.tau/12),py,4.22+rad*math.sin(i*math.tau/12))
           for py,rad in sections for i in range(12)]
    faces=[(j*12+i,j*12+(i+1)%12,((j+1)%4)*12+(i+1)%12,((j+1)%4)*12+i)
           for j in range(4) for i in range(12)]
    mesh('Cannon_reinforce',verts,faces,mat,'CannonBarrel')


def build(m):
    ladder(m)
    lookout(m)
    import bpy
    from mathutils import Vector
    before=set(bpy.data.objects)
    cannon_base(m)
    cannon_yoke(m)
    barrel(m)
    for obj in set(bpy.data.objects)-before:
        # Apply authored transforms before stretching only the pedestal height.
        bpy.context.view_layer.objects.active=obj
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        is_base=any(g.name=='CannonBase' for g in obj.vertex_groups)
        for v in obj.data.vertices:
            v.co.y-=.8
            if is_base:
                v.co.z+=.35*max(0,min(1,(v.co.z-2.77)/(3.72-2.77)))
            else:
                v.co.z+=.35
