"""Bake a repeatable semi-random scurry performance in a textured review scene."""
import os
import sys
import math
import random
import json
HERE=os.path.dirname(os.path.abspath(__file__))
sys.path[:0]=[HERE,os.path.join(HERE,'..','..','blender')]
import bpy
from mathutils import Vector
from workshop import preview,materials,shapes
import rig as ratrig
from colored_view import prepare

OUT=os.path.join(HERE,'out')
# Old choreography frames mapped onto a much snappier performance. Moving bouts
# take one third as long; pauses are brief; the bite retains its authored timing.
TIMING=[(0,0),(150,36),(205,54),(229,61),(285,80),(309,87),(365,106),
        (389,113),(445,132),(495,147),(549,201),(594,213),(690,245),(840,281)]
TOTAL=TIMING[-1][1]


def source_frame(frame):
    for (a,x),(b,y) in zip(TIMING,TIMING[1:]):
        if x<=frame<=y:
            return a+(b-a)*(frame-x)/(y-x)
    return TIMING[-1][0]


def preview_frame(source):
    for (a,x),(b,y) in zip(TIMING,TIMING[1:]):
        if a<=source<=b:
            return round(x+(y-x)*(source-a)/(b-a))
    return TOTAL


def bezier(a,b,heading,t):
    distance=(b-a).length
    control=a+Vector((math.sin(heading),-math.cos(heading),0))*distance*.32
    end=b-(b-a)*.25
    pos=(1-t)**3*a+3*(1-t)**2*t*control+3*(1-t)*t*t*end+t**3*b
    tangent=3*(1-t)**2*(control-a)+6*(1-t)*t*(end-control)+3*t*t*(b-end)
    return pos,math.atan2(tangent.x,-tangent.y)


def timeline():
    rng=random.Random(73)
    plans=[]
    position=Vector((0,0,0))
    heading=0
    for start,end in [(150,205),(229,285),(309,365),(389,445)]:
        angle=heading+rng.choice((-1,1))*rng.uniform(.65,1.65)
        target=position+Vector((math.sin(angle),-math.cos(angle),0))*rng.uniform(.95,1.45)
        if target.length>2.2:
            target=-position*.35+Vector((rng.uniform(-.6,.6),rng.uniform(-.6,.6),0))
        plans.append((start,end,position.copy(),target.copy(),heading))
        heading=bezier(position,target,heading,1)[1]
        position=target
    attack_end=position+Vector((math.sin(heading),-math.cos(heading),0))*.95
    plans.append((594,690,attack_end.copy(),Vector((0,0,0)),heading))
    return plans,position,heading


def state(frame,plans,attack_position,attack_heading):
    position=Vector((0,0,0))
    heading=0
    local=frame
    for start,end,a,b,direction in plans:
        if frame<start:
            break
        if frame<=end:
            t=(frame-start)/(end-start)
            progress=t*t*(3-2*t)
            position,heading=bezier(a,b,direction,progress)
            return 'scurry',0,position,heading
        position=b.copy()
        heading=bezier(a,b,direction,1)[1]
        local=frame-end
    if 495<=frame<=549:
        local=frame-495
        from lunge import travel
        distance=travel(local)
        position=attack_position+Vector((math.sin(attack_heading),-math.cos(attack_heading),0))*distance
        return 'attack_bite',local,position,attack_heading
    if 549<frame<594:
        position=attack_position+Vector((math.sin(attack_heading),-math.cos(attack_heading),0))*.95
        heading=attack_heading
        local=frame-549
    if frame>=690:
        heading*=1-ratrig.sample(frame,[(690,0),(725,1),(840,1)])
    return 'idle',local,position,heading


def snapshot(rig):
    return {b.name:(b.location.copy(),b.rotation_euler.copy(),b.scale.copy()) for b in rig.pose.bones}


def blend(rig,previous,weight):
    for bone in rig.pose.bones:
        location,rotation,scale=previous[bone.name]
        bone.location=location.lerp(bone.location,weight)
        bone.scale=scale.lerp(bone.scale,weight)
        bone.rotation_euler=rotation.to_quaternion().slerp(bone.rotation_euler.to_quaternion(),weight).to_euler('XYZ')


def bake(rig):
    plans,attack_position,attack_heading=timeline()
    action=bpy.data.actions.new('Crypt Rat - idle, roam, lunge')
    action.use_fake_user=True
    rig.animation_data.action=action
    previous_name=None
    previous_position=Vector((0,0,0))
    gait_distance=0
    yaw=0
    switch_frame=0
    source=None
    for frame in range(TOTAL+1):
        name,local,position,heading=state(source_frame(frame),plans,attack_position,attack_heading)
        if name!=previous_name:
            source=snapshot(rig)
            switch_frame=frame
        gait_distance+=(position-previous_position).length
        ratrig.reset(rig)
        if name=='scurry':
            local=(gait_distance/.4*8)%8
        duration=8 if name=='scurry' else 54 if name=='attack_bite' else 150
        ratrig.pose(rig,name,local%duration if name=='idle' else local,duration)
        if frame-switch_frame<3 and frame>0:
            blend(rig,source,(frame-switch_frame+1)/3)
        yaw+=((heading-yaw+math.pi)%math.tau)-math.pi
        rig.location=position
        rig.rotation_euler.z=yaw
        rig.keyframe_insert('location',frame=frame)
        rig.keyframe_insert('rotation_euler',frame=frame)
        for bone in rig.pose.bones:
            for channel in ('location','rotation_euler','scale'):
                bone.keyframe_insert(channel,frame=frame)
        previous_name,previous_position=name,position.copy()
    return action


def stage(rat):
    preview._stage(rat)
    for obj in bpy.context.scene.objects:
        if obj.type=='MESH' and obj!=rat:
            obj.location.z=-.06
    stone=materials.flat('crypt floor',(.10,.115,.10))
    for x in range(-4,5):
        for y in range(-4,5):
            shapes.box('floor slab',(.99,.99,.035),(x,y,-.035),material=stone)
    camera=preview._camera()
    camera.data.type='ORTHO'
    camera.data.ortho_scale=7.7
    camera.location=(6,-7,6)
    camera.rotation_euler=(Vector((0,-.6,.1))-camera.location).to_track_quat('-Z','Y').to_euler()
    s=bpy.context.scene
    s.render.resolution_x=1100
    s.render.resolution_y=800
    s.eevee.taa_render_samples=24
    s.view_settings.view_transform='Standard'
    prepare()
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                area.spaces.active.region_3d.view_perspective='CAMERA'
                area.spaces.active.region_3d.view_camera_zoom=8


def main():
    bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,'ecp_crypt_rat_animated.blend'))
    rig=bpy.data.objects['crypt_rat_rig']
    rig.rotation_mode='XYZ'
    action=bake(rig)
    import tail_motion
    tail_report=tail_motion.bake(rig,action,TOTAL,True)
    with open(os.path.join(OUT,'tail_checks.json'),'w') as handle:
        json.dump(tail_report,handle,indent=2)
    rat=bpy.data.objects['ecp_crypt_rat']
    stage(rat)
    s=bpy.context.scene
    s.frame_start,s.frame_end,s.render.fps=0,TOTAL-1,30
    for frame,name in [(0,'IDLE - sniff and listen'),(150,'SCURRY - explore'),(229,'SCURRY - turn'),
                       (309,'SCURRY - dart'),(389,'SCURRY - investigate'),(445,'IDLE - spot target'),
                       (495,'LUNGE - crouch / leap / bite / land'),(594,'SCURRY - return'),(690,'IDLE - settle')]:
        s.timeline_markers.new(name,frame=preview_frame(frame))
    s.frame_set(0)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'ecp_crypt_rat_performance.blend'))
    paths=[]
    for frame in (35,65,155,185,250,340,508,515,520,526,635,730):
        s.frame_set(preview_frame(frame))
        path=os.path.join(OUT,f'performance_{frame:03}.png')
        s.render.filepath=path
        bpy.ops.render.render(write_still=True)
        paths.append(path)
    preview.grid(paths,4,os.path.join(OUT,'performance_sheet.png'))
    print('WORKSHOP performance:',TOTAL/30,'seconds; 3x scurry travel speed; 25 bones')


main()
