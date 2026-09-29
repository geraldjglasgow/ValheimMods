"""Small faceted meshes, with deterministic face colours rather than smooth plastic."""
import math
import random
import bpy
from mathutils import Vector
from workshop import materials


def palette(name, color):
    mats=[materials.flat(name + str(i), tuple(c * factor for c in color))
          for i, factor in enumerate((.68,.84,1,1.1))]
    size=128 if 'bone' in name or 'edge' in name else 64
    image=bpy.data.images.new(name+'grain',width=size,height=size)
    rng=random.Random(name)
    pixels=[]
    contrast=.84 if 'rag' in name else (.79 if size==128 else .66)
    cells=[[rng.uniform(contrast,1.0) for _ in range(size//4)] for _ in range(size//4)]
    for y in range(size):
        for x in range(size):
            value=cells[y//4][x//4]
            if 'rag' in name and (x%4==0 or y%4==0):
                value*=.94
            pixels.extend((value,value,value,1))
    image.colorspace_settings.name='Non-Color'
    image.pixels.foreach_set(pixels)
    image.pack()
    for mat in mats:
        tree=mat.node_tree
        bsdf=next(n for n in tree.nodes if n.type=='BSDF_PRINCIPLED')
        tex=tree.nodes.new('ShaderNodeTexImage'); tex.image=image; tex.interpolation='Closest'
        multiply=tree.nodes.new('ShaderNodeMixRGB'); multiply.blend_type='MULTIPLY'
        multiply.inputs[0].default_value=1
        multiply.inputs[1].default_value=bsdf.inputs['Base Color'].default_value
        tree.links.new(tex.outputs['Color'],multiply.inputs[2])
        tree.links.new(multiply.outputs[0],bsdf.inputs['Base Color'])
    return mats


def finish(obj, mats):
    for m in mats:
        obj.data.materials.append(m)
    rng = random.Random(obj.name)
    for p in obj.data.polygons:
        p.material_index = rng.choices(range(len(mats)), [1,2,6,1][:len(mats)])[0] if len(mats)==4 else 0
        p.use_smooth = False
    return obj


def mesh(name, verts, faces, mats):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    uv=data.uv_layers.new(name='UVMap')
    for face in data.polygons:
        axis=max(range(3),key=lambda i:abs(face.normal[i]))
        axes=[i for i in range(3) if i!=axis]
        for loop in face.loop_indices:
            p=data.vertices[data.loops[loop].vertex_index].co
            uv.data[loop].uv=(p[axes[0]]*2,p[axes[1]]*2)
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    return finish(obj,mats)


def sphere(name, pos, size, mats, segments=8, rings=4):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=1, location=pos)
    obj = bpy.context.object
    obj.name = name
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj,mats)


def rod(name, a, b, r1, r2, mats, sides=7):
    a,b = Vector(a),Vector(b)
    bpy.ops.mesh.primitive_cone_add(vertices=sides, radius1=r1, radius2=r2,
        depth=(b-a).length, location=(a+b)/2)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = (b-a).to_track_quat('Z','Y')
    return finish(obj,mats)


def slab(name, outline, depth, mats):
    # outline in XZ; triangulated broad faces give the blade a visible central bevel.
    n = len(outline)
    verts = [(x,-depth/2,z) for x,z in outline] + [(x,depth/2,z) for x,z in outline]
    cx,cz = sum(x for x,z in outline)/n, sum(z for x,z in outline)/n
    verts += [(cx,-depth,cz),(cx,depth,cz)]
    faces = []
    for i in range(n):
        j=(i+1)%n
        faces += [(2*n,j,i),(2*n+1,n+i,n+j),(i,j,n+j,n+i)]
    return mesh(name,verts,faces,mats)


def curved_rod(name, points, radii, mats, sides=10):
    points=[Vector(p) for p in points]
    verts=[]
    for i,(p,r) in enumerate(zip(points,radii)):
        tangent=points[min(i+1,len(points)-1)]-points[max(i-1,0)]
        q=tangent.to_track_quat('Z','Y')
        for j in range(sides):
            angle=j*math.tau/sides
            verts.append(p+q @ Vector((r*math.cos(angle),r*math.sin(angle),0)))
    faces=[tuple(reversed(range(sides)))]
    for i in range(len(points)-1):
        for j in range(sides):
            k=i*sides+j; n=i*sides+(j+1)%sides
            faces.append((k,n,n+sides,k+sides))
    faces.append(tuple(range((len(points)-1)*sides,len(points)*sides)))
    obj=mesh(name,verts,faces,mats)
    for p in obj.data.polygons:
        p.material_index=2; p.use_smooth=True
    return obj


def sculpted_blade(name, controls, depth, mats):
    # Catmull-Rom outline and three bevel bands, rather than a triangle fan wedge.
    outline=[]
    points=[Vector(p) for p in controls]
    for i,p1 in enumerate(points):
        p0,p2,p3=points[i-1],points[(i+1)%len(points)],points[(i+2)%len(points)]
        for step in range(5):
            t=step/5
            outline.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
    center=sum(outline,Vector((0,0)))/len(outline)
    verts=[]
    rings=[(.20,-depth),(.70,-depth*.92),(.92,-depth*.48),(1,0),(.92,depth*.48),(.70,depth*.92),(.20,depth)]
    for scale,y in rings:
        for p in outline:
            v=center+(p-center)*scale
            verts.append((v.x,y,v.y))
    n=len(outline)
    faces=[tuple(reversed(range(n)))]
    for i in range(len(rings)-1):
        for j in range(n):
            faces.append((i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j))
    faces.append(tuple(range((len(rings)-1)*n,len(rings)*n)))
    obj=mesh(name,verts,faces,mats)
    for p in obj.data.polygons:
        p.material_index=2
        p.use_smooth=True
    return obj
