"""Original generic rig with ear/paw articulation and baked in-place actions, 30 fps."""
import bpy
from model import TAIL

CLIPS = [('idle',150,True,'idle'), ('walk',30,True,'idle'),
         ('run',8,True,'idle'), ('scurry',8,True,'idle'), ('attack_bite',54,False,'attack'),
         ('stagger',18,False,'stagger'), ('death',36,False,'freeze')]


def build(mesh):
    data = bpy.data.armatures.new('crypt_rat_skeleton')
    rig = bpy.data.objects.new('crypt_rat_rig', data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    add(data,'Root',(0,0,0),None)
    add(data,'Body',(0,.12,.40),'Root')
    add(data,'Head',(0,-.37,.43),'Body')
    add(data,'Jaw',(0,-.53,.30),'Head')
    add(data,'BiteOrigin',(0,-.89,.32),'Head')
    add(data,'EyePos',(0,-.61,.50),'Head')
    add(data,'EarL',(-.185,-.405,.52),'Head')
    add(data,'EarR',(.185,-.405,.52),'Head')
    limbs(data)
    for i in range(5):
        add(data,'Tail'+str(i),TAIL[i],'Body' if i==0 else 'Tail'+str(i-1))
    bpy.ops.object.mode_set(mode='OBJECT')
    refine_weights(mesh)
    mesh.parent = rig
    mesh.modifiers.new('Rat skin', 'ARMATURE').object = rig
    for p in rig.pose.bones:
        p.rotation_mode = 'XYZ'
    return rig


def add(data, name, pivot, parent):
    bone = data.edit_bones.new(name)
    bone.head = pivot
    bone.tail = (pivot[0],pivot[1],pivot[2]+.09)
    if parent:
        bone.parent = data.edit_bones[parent]
    bone.use_deform = name not in ('Root','BiteOrigin','EyePos')


def limbs(data):
    for s,side in [(-1,'L'),(1,'R')]:
        for name,y,x in [('Front',-.30,.20),('Back',.35,.245)]:
            add(data,name+side,(s*x,y,.35),'Body')
            add(data,name+side+'Foot',(s*(x+.065),y+.06,.18),name+side)
            add(data,name+side+'Paw',(s*(x+.06),y-.095,.065),name+side+'Foot')


def refine_weights(mesh):
    """Separate whole ear and paw islands without changing the model or its UVs."""
    from collections import defaultdict
    edges = defaultdict(list)
    for edge in mesh.data.edges:
        a,b = edge.vertices
        edges[a].append(b)
        edges[b].append(a)
    pending = set(range(len(mesh.data.vertices)))
    while pending:
        island, todo = [], [pending.pop()]
        while todo:
            vertex = todo.pop()
            island.append(vertex)
            for neighbor in edges[vertex]:
                if neighbor in pending:
                    pending.remove(neighbor)
                    todo.append(neighbor)
        reweight_island(mesh,island)


def reweight_island(mesh, island):
    from mathutils import Vector
    center = sum((mesh.data.vertices[i].co for i in island),Vector())/len(island)
    source = mesh.vertex_groups[mesh.data.vertices[island[0]].groups[0].group]
    name = None
    if source.name=='Head' and center.z>.56 and center.y>-.50 and abs(center.x)>.13:
        name = 'EarL' if center.x<0 else 'EarR'
    elif source.name.endswith('Foot') and center.z<.10:
        name = source.name[:-4]+'Paw'
    if name:
        group = mesh.vertex_groups.get(name) or mesh.vertex_groups.new(name=name)
        source.remove(island)
        group.add(island,1,'REPLACE')


def reset(rig):
    for bone in rig.pose.bones:
        bone.location = (0,0,0)
        bone.rotation_euler = (0,0,0)
        bone.scale = (1,1,1)


def actions(rig):
    rig.animation_data_create()
    for name,last,loop,tag in CLIPS:
        action = bpy.data.actions.new(name)
        action.use_fake_user = True
        rig.animation_data.action = action
        for frame in range(last+1):
            reset(rig)
            pose(rig,name,frame,last)
            for bone in rig.pose.bones:
                bone.keyframe_insert('location',frame=frame)
                bone.keyframe_insert('rotation_euler',frame=frame)
                bone.keyframe_insert('scale',frame=frame)
        action['loop'] = loop
        action['state_tag'] = tag
    rig.animation_data.action = None
    reset(rig)


def pose(rig,name,frame,last):
    from motion import perform
    perform(rig,name,frame,last)


def sample(frame,keys):
    if frame<=keys[0][0]:
        return keys[0][1]
    for (a,x),(b,y) in zip(keys,keys[1:]):
        if a <= frame <= b:
            t = (frame-a)/(b-a)
            t = t*t*(3-2*t)
            return x+(y-x)*t
    return keys[-1][1]
