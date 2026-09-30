"""Rat performance: planted paws, asymmetric sniffing, rapid gait and a ballistic bite.

Rest bones point up: local Y is world Z; local Z is world -Y.
Leg transforms use an analytic two-segment solver, baked to ordinary FK keys.
"""
import math
import bpy
from mathutils import Vector, Matrix
from rig import sample

TAU = math.tau
LEGS = [('FrontL',-.30,.20,-1),('FrontR',-.30,.20,1),
        ('BackL',.35,.245,-1),('BackR',.35,.245,1)]


def pulse(t, center, width):
    return math.exp(-((t-center)/width)**2)


def perform(rig,name,frame,last):
    if name=='death':
        death(rig,frame,last)
        return
    if name=='stagger':
        rig.pose.bones['Body'].rotation_euler.x=-.24*math.sin(math.pi*frame/last)
        rig.pose.bones['Body'].location.y=-.035*math.sin(math.pi*frame/last)
        targets = resting_targets()
    elif name=='attack_bite':
        targets = attack(rig,frame)
    elif name in ('run','walk','scurry'):
        targets = scurry(rig,frame/last,name=='walk')
    else:
        targets = idle(rig,frame/last)
    bpy.context.view_layer.update()
    for leg,y,x,side in LEGS:
        solve_leg(rig,leg,targets[leg],side)


def resting_targets():
    return {name:Vector((side*(x+.06),y-.095,.065)) for name,y,x,side in LEGS}


def idle(rig,t):
    b=rig.pose.bones
    breathing=math.sin(TAU*3*t)
    sniff=sum(pulse(t,c,.016) for c in (.19,.245,.30,.73,.785))
    look=sample(t,[(0,0),(.12,0),(.36,.20),(.51,.20),(.64,-.24),(.86,-.24),(1,0)])
    b['Body'].scale=(1+.009*breathing,1+.008*breathing,1)
    b['Body'].location.y=.005*breathing
    b['Head'].rotation_euler.x=.04*breathing+.11*sniff
    b['Head'].rotation_euler.y=look
    b['Head'].location.z=.018*sniff
    b['Jaw'].rotation_euler.x=.035*sniff
    b['EarL'].rotation_euler.z=.38*pulse(t,.43,.018)-.15*pulse(t,.47,.022)
    b['EarR'].rotation_euler.z=-.34*pulse(t,.68,.018)+.13*pulse(t,.72,.022)
    return resting_targets()


def scurry(rig,t,walking=False):
    b=rig.pose.bones
    stride=.16 if walking else .24
    lift=.055 if walking else .095
    b['Body'].location.y=-.035+.012*math.cos(TAU*2*t)
    b['Body'].rotation_euler.x=.035*math.sin(TAU*2*t)
    b['Body'].rotation_euler.z=.025*math.sin(TAU*t)
    b['Head'].rotation_euler.x=-.035*math.sin(TAU*2*t)+.045
    b['Head'].rotation_euler.y=.025*math.sin(TAU*t)
    targets=resting_targets()
    offsets={'FrontL':0,'BackR':.10,'FrontR':.50,'BackL':.60}
    for name in targets:
        phase=(t+offsets[name])%1
        if phase<.60:
            forward=stride*(phase/.60-.5)
            height=0
        else:
            swing=(phase-.60)/.40
            smooth=swing*swing*(3-2*swing)
            forward=stride*(.5-smooth)
            height=lift*math.sin(math.pi*swing)**1.2
        targets[name].y+=forward
        targets[name].z+=height
    return targets


def attack(rig,f):
    from lunge import pose
    return pose(rig,f)


def solve_leg(rig,name,target,side):
    upper=rig.data.bones[name]
    lower=rig.data.bones[name+'Foot']
    paw=rig.data.bones[name+'Paw']
    body=rig.pose.bones['Body'].matrix @ rig.data.bones['Body'].matrix_local.inverted()
    hip=body @ upper.head_local
    a=(lower.head_local-upper.head_local).length
    c=(paw.head_local-lower.head_local).length
    vector=target-hip
    distance=max(.015,min(vector.length,a+c-.001))
    direction=vector.normalized()
    pole=Vector((side*.23,1,0))
    pole=(pole-direction*pole.dot(direction)).normalized()
    along=(a*a-c*c+distance*distance)/(2*distance)
    knee=hip+direction*along+pole*math.sqrt(max(0,a*a-along*along))
    ankle=hip+direction*distance
    orient(rig,name,hip,lower.head_local-upper.head_local,knee-hip)
    bpy.context.view_layer.update()
    orient(rig,name+'Foot',knee,paw.head_local-lower.head_local,ankle-knee)
    bpy.context.view_layer.update()
    rig.pose.bones[name+'Paw'].matrix=Matrix.Translation(ankle) @ paw.matrix_local.to_3x3().to_4x4()
    bpy.context.view_layer.update()


def orient(rig,name,position,original,direction):
    rotation=original.rotation_difference(direction).to_matrix().to_4x4()
    basis=rig.data.bones[name].matrix_local.to_3x3().to_4x4()
    rig.pose.bones[name].matrix=Matrix.Translation(position) @ rotation @ basis


def death(rig,f,last):
    t=sample(f,[(0,0),(8,.25),(19,1),(last,1)])
    b=rig.pose.bones
    b['Body'].rotation_euler.z=-1.55*t
    b['Body'].location.y=-.10*t
    b['Jaw'].rotation_euler.x=.4*t
    b['Head'].rotation_euler.y=.12*t
    for i in range(5):
        b['Tail'+str(i)].rotation_euler.y=.08*t
