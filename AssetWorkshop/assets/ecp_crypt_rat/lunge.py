"""One lunge timing source for poses, root travel, tail inertia and preview."""
from mathutils import Vector
from rig import sample

DISTANCE=.95
SPRING_SPEED=1.2
TAKEOFF=14+2/SPRING_SPEED
FRONT_LAND=14+8/SPRING_SPEED
HIND_LAND=14+9/SPRING_SPEED
STOP=14+10/SPRING_SPEED
HIT=20


def pose_time(frame):
    """Keep the anticipation, compress the spring, then allow a weighted recovery."""
    keys=[(0,0),(14,14),(TAKEOFF,17),(FRONT_LAND,27),(STOP,30),(STOP+24,54),(54,54)]
    for (a,x),(b,y) in zip(keys,keys[1:]):
        if a<=frame<=b:
            return x+(y-x)*(frame-a)/(b-a)
    return 0 if frame<0 else 54


def travel(frame):
    """Accelerate off the hind paws, coast in flight, brake after front contact."""
    acceleration=TAKEOFF-14
    flight=FRONT_LAND-TAKEOFF
    braking=STOP-FRONT_LAND
    speed=DISTANCE/(acceleration*.5+flight+braking*.5)
    if frame<=14:
        return 0.0
    if frame<TAKEOFF:
        return speed*(frame-14)**2/(2*acceleration)
    if frame<FRONT_LAND:
        return speed*(acceleration*.5+frame-TAKEOFF)
    if frame<STOP:
        t=frame-FRONT_LAND
        return speed*(acceleration*.5+flight+t-t*t/(2*braking))
    return DISTANCE


def body_height(frame):
    if TAKEOFF<=frame<=FRONT_LAND:
        t=(frame-TAKEOFF)/(FRONT_LAND-TAKEOFF)
        return .015+.44*t*(1-t)
    return sample(pose_time(frame),[(0,0),(10,-.095),(13,-.095),(17,.015),
                         (27,.015),(30,-.060),(35,-.012),(43,0),(54,0)])


def pose(rig,f):
    actual=f
    f=pose_time(f)
    b=rig.pose.bones
    b['Body'].location.y=body_height(actual)
    recoil=sample(actual,[(0,0),(10,.16),(14,.17),(TAKEOFF,.025),(14+4/SPRING_SPEED,0),(54,0)])
    b['Body'].location.z=-recoil
    b['Body'].rotation_euler.x=sample(f,[(0,0),(12,-.075),(17,-.11),(22,-.02),(27,.13),(31,.075),(43,0),(54,0)])
    b['Body'].rotation_euler.x-=recoil*.35
    b['Body'].rotation_euler.z=sample(f,[(0,0),(25,0),(28,.025),(33,-.012),(41,0),(54,0)])
    b['Head'].rotation_euler.x=sample(f,[(0,0),(12,-.04),(18,-.07),(23,-.04),(25,.095),(29,.04),(40,0),(54,0)])
    b['Head'].location.z=sample(f,[(0,0),(12,-.009),(22,.025),(26,.032),(33,.008),(44,0),(54,0)])
    b['Jaw'].rotation_euler.x=sample(f,[(0,0),(11,.07),(18,.32),(23,.48),(25,.012),(27,.035),(34,.055),(43,0),(54,0)])
    for side in ('L','R'):
        b['Ear'+side].rotation_euler.x=sample(f,[(0,0),(13,-.22),(20,-.34),(31,-.12),(43,0),(54,0)])
    targets={}
    for name,y,x,side in [('FrontL',-.30,.20,-1),('FrontR',-.30,.20,1),
                         ('BackL',.35,.245,-1),('BackR',.35,.245,1)]:
        front=name.startswith('Front')
        target=Vector((side*(x+.06),y-.095,.065))
        offset,lift=paw(actual,front)
        target.y+=offset
        target.z+=lift
        targets[name]=target
    return targets


def paw(f,front):
    land=FRONT_LAND if front else HIND_LAND
    reach=-.09 if front else -.02
    if f>=land:
        # Hold the landing point in world space while the body brakes over it.
        planted=reach+travel(f)-travel(land)
        start,end=(29,38) if front else (34,43)
        release=sample(f,[(0,0),(start,0),(end,1),(54,1)])
        lift=sample(f,[(0,0),(start,0),((start+end)/2,.035),(end,0),(54,0)])
        return planted*(1-release),lift
    if front:
        return (sample(pose_time(f),[(0,0),(14,0),(19,-.13),(24,-.12),(27,reach)]),
                sample(pose_time(f),[(0,0),(15,0),(19,.105),(23,.09),(27,0)]))
    if f<=TAKEOFF:
        return travel(f),0
    return (sample(f,[(TAKEOFF,travel(TAKEOFF)),(14+4/SPRING_SPEED,.12),(14+7/SPRING_SPEED,.045),(HIND_LAND,reach)]),
            sample(f,[(TAKEOFF,0),(14+4/SPRING_SPEED,.11),(14+7/SPRING_SPEED,.095),(HIND_LAND,0)]))
