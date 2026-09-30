"""Bake a damped world-space tail chain: inertia, gravity, fixed lengths and floor contact."""
import math
import bpy
from mathutils import Vector, Matrix
from model import TAIL

REST=[Vector(p) for p in TAIL]
LENGTHS=[(b-a).length for a,b in zip(REST,REST[1:])]
RADII=[.071,.058,.045,.032,.019,.007]


class TailChain:
    def __init__(self,points):
        self.points=[p.copy() for p in points]
        self.velocity=[Vector() for p in points]

    def step(self,desired):
        dt=1/120
        for _ in range(4):
            old=[p.copy() for p in self.points]
            self.points[0]=desired[0].copy()
            for i in range(1,6):
                stiffness=100-14*i
                force=(desired[i]-self.points[i])*stiffness+Vector((0,0,-2.8))
                self.velocity[i]=(self.velocity[i]+force*dt)*math.exp(-4.8*dt)
                self.points[i]+=self.velocity[i]*dt
            for _ in range(12):
                self.constrain(desired[0])
            self.limit_bends(desired)
            for i in range(1,6):
                self.velocity[i]=(self.points[i]-old[i])/dt
                if self.points[i].z<=RADII[i]+.009:
                    self.velocity[i].x*=.94
                    self.velocity[i].y*=.94
        return [p.copy() for p in self.points]

    def limit_bends(self,desired):
        """A forward projection prevents hard elbows and preserves exact lengths."""
        previous=(desired[1]-desired[0]).normalized()
        for i,length in enumerate(LENGTHS):
            direction=(self.points[i+1]-self.points[i]).normalized()
            angle=previous.angle(direction,0)
            if angle>.48:
                turn=previous.rotation_difference(direction)
                from mathutils import Quaternion
                direction=Quaternion().slerp(turn,.48/angle) @ previous
            minimum=(RADII[i+1]+.008-self.points[i].z)/length
            if direction.z<minimum:
                direction.z=min(.999,minimum)
                horizontal=Vector((direction.x,direction.y,0)).normalized()
                direction=horizontal*math.sqrt(1-direction.z**2)+Vector((0,0,direction.z))
            self.points[i+1]=self.points[i]+direction*length
            previous=direction

    def constrain(self,anchor):
        self.points[0]=anchor.copy()
        for i,length in enumerate(LENGTHS):
            delta=self.points[i+1]-self.points[i]
            if delta.length<1e-8:
                continue
            correction=delta*(1-length/delta.length)
            if i==0:
                self.points[i+1]-=correction
            else:
                self.points[i]+=correction*.5
                self.points[i+1]-=correction*.5
        for i in range(1,6):
            self.points[i].z=max(RADII[i]+.008,self.points[i].z)


def desired_points(rig,world):
    body=rig.pose.bones['Body'].matrix @ rig.data.bones['Body'].matrix_local.inverted()
    return [world @ body @ p for p in REST]


def apply_points(rig,world,points):
    inverse=world.inverted()
    local=[inverse @ p for p in points]
    for i in range(5):
        name='Tail'+str(i)
        original=REST[i+1]-REST[i]
        direction=local[i+1]-local[i]
        rotation=original.rotation_difference(direction).to_matrix().to_4x4()
        basis=rig.data.bones[name].matrix_local.to_3x3().to_4x4()
        rig.pose.bones[name].matrix=Matrix.Translation(local[i]) @ rotation @ basis
        bpy.context.view_layer.update()


def virtual_world(rig,name,frame,elapsed):
    from rig import sample
    travel=0
    if name in ('walk','run','scurry'):
        last=30 if name=='walk' else 8
        travel=elapsed/last*(.267 if name=='walk' else .4)
    elif name=='attack_bite':
        from lunge import travel as lunge_travel
        travel=lunge_travel(frame)
    return Matrix.Translation((0,-travel,0)) @ rig.matrix_world


def bake(rig,action,last,loop,virtual=False):
    """Warm up before recording so looping locomotion carries settled momentum."""
    rig.animation_data.action=action
    s=bpy.context.scene
    s.frame_set(0)
    world=virtual_world(rig,action.name,0,0) if virtual else rig.matrix_world.copy()
    chain=TailChain(desired_points(rig,world))
    warmup=max(3,math.ceil(120/max(last,1)))*last if loop else 90
    frames=range(-warmup,last+1)
    cached=[]
    metrics={'max_link_error':0.0,'min_surface_height':1e9}
    for elapsed in frames:
        frame=elapsed%last if loop else max(elapsed,0)
        s.frame_set(frame)
        world=virtual_world(rig,action.name,frame,elapsed+warmup) if virtual else rig.matrix_world.copy()
        points=chain.step(desired_points(rig,world))
        if elapsed<0:
            continue
        cached.append((world.copy(),points))
        for i,length in enumerate(LENGTHS):
            metrics['max_link_error']=max(metrics['max_link_error'],abs((points[i+1]-points[i]).length-length))
        metrics['min_surface_height']=min(metrics['min_surface_height'],min(points[i].z-RADII[i] for i in range(1,6)))
    # Match the endpoint exactly; warm-up makes the final correction very small.
    if loop:
        cached[-1]=cached[0]
    for frame,(world,points) in enumerate(cached):
        s.frame_set(frame)
        apply_points(rig,world,points)
        for i in range(5):
            bone=rig.pose.bones['Tail'+str(i)]
            for channel in ('location','rotation_euler','scale'):
                bone.keyframe_insert(channel,frame=frame)
    s.frame_set(0)
    return metrics
