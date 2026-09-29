"""Drowned Shade: corroded burial collar, hanging chains and cracked bronze bell.
Accessory kit in vanilla Wraith root coordinates; no borrowed game mesh ships.
"""
import math
import bpy
from mathutils import Vector
from workshop import shapes, materials

TEXTURE_SIZE=256
NORMAL_MAP=False
AO_STRENGTH=.45

def rod(name,a,b,radius,mat):
    a,b=Vector(a),Vector(b)
    ob=shapes.cylinder(name,radius,(b-a).length,(a+b)/2,material=mat,vertices=6)
    ob.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()

def ring(name,position,radius,mat,rotation=(math.pi/2,0,0),scale=(1,1,1)):
    bpy.ops.mesh.primitive_torus_add(major_segments=8,minor_segments=4,location=position,
        rotation=rotation,major_radius=radius,minor_radius=.013)
    ob=bpy.context.object; ob.name=name; ob.scale=scale; ob.data.materials.append(mat)

def build():
    iron=materials.iron('bog_iron',color=(.045,.054,.052),rust=(.080,.039,.017))
    bronze=materials.iron('pitted_bell_bronze',color=(.095,.098,.049),rust=(.042,.077,.061))
    for mat in [iron,bronze]:
        materials.principled(mat).inputs['Metallic'].default_value=.08
        materials.principled(mat).inputs['Roughness'].default_value=.95
    # Heavy open polygonal yoke around the shoulders, with chain draped forward.
    for i in range(10):
        a=i*math.tau/10;b=(i+1)*math.tau/10
        rod('burial_yoke',(.36*math.cos(a),.25*math.sin(a),1.87),
            (.36*math.cos(b),.25*math.sin(b),1.87),.038,iron)
    for side in [-1,1]:
        for i in range(9):
            ring('chest_chain',(side*(.30-.017*i),-.27-.006*i,1.83-i*.065),.039,iron,
                rotation=(math.pi/2,0,math.pi/2*(i%2)),scale=(.80,1.25,1))
        for i in range(13 if side<0 else 10):
            ring('trailing_shackle',(side*(.43+.018*math.sin(i)),.04,1.84-i*.069),.044,iron,
                rotation=(math.pi/2,0,math.pi/2*(i%2)),scale=(.82,1.20,1))
        ring('broken_wrist_shackle',(side*.43,.04,.85 if side<0 else 1.05),.083,iron)
    # Bell has a hollow mouth, an angular shoulder and two missing rim wedges.
    verts=[]; faces=[]; count=12
    levels=[(1.40,.075),(1.37,.105),(1.27,.14),(1.09,.20),(1.055,.24),(1.025,.24),(1.055,.21),(1.12,.17)]
    for z,r in levels:
        for i in range(count):
            t=i*math.tau/count
            verts.append((r*math.cos(t),-.34+r*.72*math.sin(t),z))
    for j in range(len(levels)-1):
        for i in range(count):
            if j>=3 and i==8: continue
            faces.append((j*count+i,j*count+(i+1)%count,(j+1)*count+(i+1)%count,(j+1)*count+i))
    faces.append(tuple(range(count-1,-1,-1)))
    mesh=bpy.data.meshes.new('cracked_hollow_bell');mesh.from_pydata(verts,[],faces);mesh.update()
    ob=bpy.data.objects.new('cracked_hollow_bell',mesh);bpy.context.collection.objects.link(ob)
    mesh.materials.append(bronze)
    ring('bell_handle',(0,-.34,1.435),.061,iron)
    rod('bell_clapper',(0,-.34,1.27),(0,-.34,1.02),.025,iron)
    # Riveted plates produce a broad broken-shoulder silhouette against the shroud.
    for side in [-1,1]:
        ob=shapes.box('grave_plate',(.25,.28,.07),(side*.38,.02,1.85),material=iron,bevel=.018)
        ob.rotation_euler[1]=side*.27
        for y in [-.08,.11]: shapes.sphere('rivet',.025,(side*.39,y,1.90),material=bronze,segments=6,rings=4)
