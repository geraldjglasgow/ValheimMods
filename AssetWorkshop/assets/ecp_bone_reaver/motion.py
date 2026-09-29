"""Baked two-hand IK clips; side-specific starts/ends preserve the resting axe side."""
import math
import bpy
from mathutils import Matrix, Quaternion, Vector
from satchel import DRAW_POINT

FPS=30
CLIPS=[]
ERRORS=[]
GRIP_ERRORS=[]


def mix(a,b,t):
    return a*(1-t)+b*t


def sample(keys, frame):
    for (a,va),(b,vb) in zip(keys,keys[1:]):
        if frame <= b:
            t=max(0,(frame-a)/(b-a)); t=t*t*(3-2*t)
            return {k: mix(va[k],vb[k],t) for k in va}
    return keys[-1][1]


def state(pos=(-.30,-.24,1.8), direction=(-.20,.61,-.766), yaw=0, head=0,
          duck=-.08, glow=0, knife=0, axe=1, reach=0, jaw=0):
    return dict(pos=Vector(pos),direction=Vector(direction).normalized(),yaw=yaw,head=head,
                duck=duck,glow=glow,knife=knife,axe=axe,reach=reach,jaw=jaw)


def timelines():
    idle=state()
    overhead=state((0,-.15,1.98),(0,.25,.968),duck=-.13)
    slam=state((0,-.48,1.67),(0,-.68,-.733),duck=-.19)
    wind=state((-.12,-.22,1.66),(-.95,.3,.04),yaw=-.7)
    front=state((0,-.22,1.60),(0,-1,0),yaw=0)
    across=state((.12,-.22,1.66),(.95,.3,.04),yaw=.7)
    other=state((.30,-.24,1.8),(.20,.61,-.766))
    rear=state((0,-.05,1.96),(0,.70,-.714),head=math.pi,duck=-.13)
    rear_overhead=dict(overhead,head=math.pi)
    unarmed=state((-.30,-.24,1.8),(-.20,.61,-.766),axe=0)
    specs=[
        ('idle',60,True,[(0,idle),(30,state(duck=-.095)),(60,idle)],[]),
        ('walk',48,True,[(0,idle),(24,idle),(48,idle)],[]),
        ('slam',108,False,[(0,idle),(30,overhead),(43,overhead),(53,slam),(69,slam),(108,idle)],
         [(53,'slam_impact')]),
        ('spin',192,False,[(0,idle),(25,wind),(38,front),(49,across),
            (61,state((0,.12,1.7),(0,1,0),yaw=math.pi)),
            (72,state(yaw=math.tau)),(192,state(yaw=math.tau))],
         [(34,'spin_hit_start'),(68,'spin_hit_end'),(72,'recovery_start'),(192,'recovery_end')]),
        ('sweep',90,False,[(0,idle),(24,wind),(37,front),(48,across),(66,other),(90,other)],
         [(30,'sweep_hit_start'),(51,'sweep_hit_end'),(66,'rest_side_flip')]),
        ('rear',108,False,[(0,idle),(12,state(head=math.pi,glow=1)),
            (27,state(head=math.pi,glow=1)),(38,rear_overhead),(49,rear),(65,rear),(108,idle)],
         [(12,'purple_eyes_on'),(27,'purple_eyes_off'),(49,'rear_impact')]),
        ('daggers',96,False,[(0,idle),(20,state(reach=1)),(28,state(reach=1,knife=1)),
            (38,state(reach=2,knife=1)),(48,state(reach=3,knife=1)),
            (49,state(reach=3)),(66,state(reach=2)),(96,idle)],
         [(26,'draw_three_daggers'),(49,'release_three_daggers')]),
        ('axe_throw',192,False,[(0,idle),(24,wind),(38,front),(50,across),
            (62,state((0,.12,1.7),(0,1,0),yaw=math.pi)),
            (74,dict(front,yaw=math.tau)),(75,dict(front,axe=0,yaw=math.tau)),
            (96,dict(unarmed,yaw=math.tau)),(120,dict(unarmed,yaw=math.tau)),
            (135,state(axe=0,jaw=1,yaw=math.tau)),(147,state(axe=0,jaw=1,yaw=math.tau)),
            (148,state(axe=1,jaw=1,yaw=math.tau)),(170,state(yaw=math.tau)),(192,state(yaw=math.tau))],
         [(75,'release_axe'),(120,'summon_yell'),(148,'materialize_axe')]),
    ]
    return specs


def rotate_world(pb, axis, angle):
    mat=pb.matrix.copy()
    pb.matrix=Matrix.Translation(mat.translation) @ Matrix.Rotation(angle,4,axis) @ mat.to_3x3().to_4x4()
    bpy.context.view_layer.update()


def aim(pb, child, target):
    start=pb.matrix.translation.copy()
    current=child.matrix.translation-start
    desired=Vector(target)-start
    if min(current.length,desired.length)<.00001:
        return
    q=current.rotation_difference(desired)
    pb.matrix=Matrix.Translation(start) @ q.to_matrix().to_4x4() @ pb.matrix.to_3x3().to_4x4()
    bpy.context.view_layer.update()


def limb(rig, upper, lower, end, target, pole):
    bones=rig.pose.bones
    a,b,c=bones[upper],bones[lower],bones[end]
    p=a.matrix.translation.copy()
    l1=(b.matrix.translation-p).length
    l2=(c.matrix.translation-b.matrix.translation).length
    delta=Vector(target)-p
    dist=max(.005,min(delta.length,l1+l2-.002))
    direction=delta.normalized()
    bend=Vector(pole)-p
    bend=(bend-direction*bend.dot(direction)).normalized()
    along=(l1*l1-l2*l2+dist*dist)/(2*dist)
    elbow=p+direction*along+bend*math.sqrt(max(0,l1*l1-along*along))
    aim(a,b,elbow)
    aim(b,c,p+direction*dist)
    ERRORS.append((Vector(target)-c.matrix.translation).length)


def pose(rig, values, frame, side, walk=False, move='idle'):
    b=rig.pose.bones
    for pb in b:
        pb.matrix_basis=Matrix.Identity(4)
    bpy.context.view_layer.update()
    sign=1 if side=='R' else -1
    hip=b['Hips'].matrix.copy()
    hip.translation.z+=values['duck']
    b['Hips'].matrix=hip
    bpy.context.view_layer.update()
    rotate_world(b['Spine'],'X',.10)
    rotate_world(b['Hips'],'Z',values['yaw']*sign)
    rotate_world(b['Head'],'Z',values['head']*sign)
    rotate_world(b['Jaw'],'X',values['jaw']*.45)
    # Feet stay planted during attacks; walk loops have lift and stride on the spot.
    for prefix,phase in [('Left',0),('Right',math.pi)]:
        p=rig.data.bones[prefix+'Foot'].head_local.copy()
        if move in ('spin','axe_throw'):
            p=Matrix.Rotation(values['yaw']*sign,3,'Z') @ p
        if walk:
            t=frame*math.tau/48+phase
            p.y+=.24*math.sin(t)
            p.z+=.11*max(0,math.cos(t))
        limb(rig,prefix+'UpLeg',prefix+'Leg',prefix+'Foot',p,(p.x,-1,.65))
    pos=values['pos'].copy(); pos.x*=sign
    direction=values['direction'].copy(); direction.x*=sign
    q=direction.to_track_quat('Z','Y')
    wm=Matrix.LocRotScale(pos,q,Vector((1,1,1)))
    primary='Left' if side=='R' else 'Right'
    secondary='Right' if side=='R' else 'Left'
    swap=max(0,min(1,(frame-60)/24)) if move=='sweep' else 0
    grips=((primary,mix(.28,.66,swap)),(secondary,mix(.66,.28,swap)))
    # Project the shared weapon into both arm reach spheres, preserving its angle.
    wrist_offset=(q @ Vector((0,-1,0)))*.105
    for _ in range(20):
        for prefix,z in grips:
            if values['reach'] and prefix==primary:
                continue
            shoulder=b[prefix+'Arm'].matrix.translation
            target=wm @ Vector((0,0,z))-wrist_offset
            length=(rig.data.bones[prefix+'ForeArm'].head_local-rig.data.bones[prefix+'Arm'].head_local).length
            length+=(rig.data.bones[prefix+'Hand'].head_local-rig.data.bones[prefix+'ForeArm'].head_local).length
            delta=target-shoulder
            if delta.length>length-.006:
                wm.translation-=delta.normalized()*(delta.length-length+.006)
    b['axe'].matrix=wm @ Matrix.Diagonal((values['axe'],)*3+(1,))
    bpy.context.view_layer.update()
    for prefix,z in grips:
        target=wm @ Vector((0,0,z))-wrist_offset
        reach=values['reach'] if prefix==primary else 0
        if reach:
            bag=b['Hips'].matrix @ rig.data.bones['Hips'].matrix_local.inverted() @ DRAW_POINT
            poses=[target,bag,Vector((-.36*sign,.03,2.17)),Vector((-.16*sign,-.70,1.91))]
            lower=min(2,int(reach)); target=poses[lower].lerp(poses[lower+1],reach-lower)
        shoulder_sign=1 if prefix=='Left' else -1
        limb(rig,prefix+'Arm',prefix+'ForeArm',prefix+'Hand',target,(shoulder_sign*1.1,.15,1.25))
        if not reach:
            GRIP_ERRORS.append((target-b[prefix+'Hand'].matrix.translation).length)
        hand=b[prefix+'Hand']
        current=b[prefix+'HandMiddle1'].matrix.translation-hand.matrix.translation
        wanted=q @ Vector((0,-1,0))
        rot=current.rotation_difference(wanted)
        hand.matrix=Matrix.Translation(hand.matrix.translation) @ rot.to_matrix().to_4x4() @ hand.matrix.to_3x3().to_4x4()
        bpy.context.view_layer.update()
        for finger in ('Index','Middle','Ring','Pinky'):
            for segment in (1,2,3):
                pb=b[prefix+'Hand'+finger+str(segment)]
                rotate_world(pb,q @ Vector((0,0,1)),.55 if segment==1 else .85)
    hand=b[primary+'Hand'].matrix
    b['daggers'].matrix=Matrix.LocRotScale(hand.translation,q,Vector((values['knife'],)*3))
    b['eye_glow'].scale=(float(move=='rear' and 12<=frame<27),)*3
    b['bag_daggers'].scale=(1-values['knife'],)*3
    bpy.context.view_layer.update()


def build(rig):
    rig.animation_data_create()
    for name,last,loop,keys,events in timelines():
        for side in ('R','L'):
            action=bpy.data.actions.new(name+'_'+side)
            action.use_fake_user=True
            rig.animation_data.action=action
            for frame in range(last+1):
                pose(rig,sample(keys,frame),frame,side,name=='walk',name)
                for pb in rig.pose.bones:
                    pb.keyframe_insert('location',frame=frame,group=pb.name)
                    pb.keyframe_insert('rotation_quaternion',frame=frame,group=pb.name)
                    pb.keyframe_insert('scale',frame=frame,group=pb.name)
            for f,event in events:
                action.pose_markers.new(event).frame=f
            CLIPS.append(dict(name=action.name,last=last,loop=loop,tag='attack' if not loop else '',
                              events=[f for f,e in events if e.endswith('impact')],
                              cues=[dict(frame=f,event=e) for f,e in events],
                              startSide=side,endSide=('L' if side=='R' else 'R') if name=='sweep' else side))
    rig.animation_data.action=bpy.data.actions['idle_R']
    bpy.context.scene.frame_set(0)
