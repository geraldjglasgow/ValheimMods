"""Connected vertebrae with broad endplates, an open neural canal and rearward processes."""
import math
import bpy
from mathutils import Matrix
from geometry import mesh, curved_rod
from body import rigid


def centrum(name,z,width,height,mats):
    verts=[]
    # Flat bearing faces and raised rims around a pinched, kidney-shaped body.
    rings=[(-.50,.79),(-.43,1),(-.27,.92),(0,.84),(.27,.92),(.43,1),(.50,.79)]
    for dz,scale in rings:
        for j in range(24):
            a=j*math.tau/24
            x=math.cos(a)*width*scale
            y=math.sin(a)*width*.72*scale-.025
            if y>-.025:
                y-=.012*math.sin(a)**4
            ripple=1+.025*math.sin(j*2.3+z*17)
            verts.append((x*ripple,y,z+dz*height))
    faces=[tuple(reversed(range(24)))]
    for i in range(len(rings)-1):
        for j in range(24):
            faces.append((i*24+j,i*24+(j+1)%24,(i+1)*24+(j+1)%24,(i+1)*24+j))
    faces.append(tuple(range(144,168)))
    return mesh(name,verts,faces,mats)


def make(index,z,mats):
    lumbar=math.sin(index/17*math.pi)
    width=.054+.014*lumbar
    height=.087+.009*lumbar
    pieces=[centrum('Centrum',z,width,height,mats)]
    # Two pedicles join the posterior arch around a clearly open canal.
    for side in (-1,1):
        pieces.append(curved_rod('Pedicle',[(side*.037,.005,z),(side*.043,.035,z+.002),
            (side*.032,.070,z-.008),(0,.088,z-.012)], [.021,.018,.016,.018],mats,10))
        wing=.090+.016*lumbar
        pieces.append(curved_rod('TransverseProcess',[(side*.040,.027,z),(side*.065,.042,z-.006),
            (side*wing,.045,z-.018),(side*(wing+.010),.050,z-.014)],
            [.021,.019,.012,.007],mats,10))
        for direction in (-1,1):
            pieces.append(curved_rod('ArticularJoint',[(side*.032,.053,z),
                (side*.032,.061,z+direction*.033),(side*.026,.062,z+direction*.050)],
                [.016,.020,.013],mats,10))
    # The dorsal process lies along the column, not curled up like a pair of horns.
    projection=.135+.035*math.sin(index/17*math.pi)
    pieces.append(curved_rod('SpinousProcess',[(0,.075,z-.009),(0,.110,z-.021),
        (0,projection,z-.047),(0,projection+.009,z-.055)], [.019,.021,.014,.006],mats,10))
    bpy.ops.object.select_all(action='DESELECT')
    for obj in pieces: obj.select_set(True)
    bpy.context.view_layer.objects.active=pieces[0]
    bpy.ops.object.join()
    obj=bpy.context.object; obj.name='Vertebra_%02d'%index
    # Merge the sculpted forms into a continuous bone surface; retain the canal opening.
    remesh=obj.modifiers.new('Connected bone surface','REMESH')
    remesh.mode='VOXEL'; remesh.voxel_size=.0055; remesh.use_smooth_shade=True
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    smooth=obj.modifiers.new('Bone wear','SMOOTH'); smooth.factor=.55; smooth.iterations=2
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    decimate=obj.modifiers.new('Game mesh','DECIMATE'); decimate.ratio=.40
    bpy.ops.object.modifier_apply(modifier=decimate.name)
    for p in obj.data.polygons: p.use_smooth=True; p.material_index=2
    # Small regional rotation, not identical stamped pieces.
    obj.data.transform(Matrix.Translation((0,0,z)) @ Matrix.Rotation(.09*math.sin(index*.63),4,'Z')
                       @ Matrix.Translation((0,0,-z)))
    # Voxel remeshing removes UVs; restore simple local bone UVs for the pixel texture.
    uv=obj.data.uv_layers.new(name='BoneUV')
    for loop in obj.data.loops:
        p=obj.data.vertices[loop.vertex_index].co
        uv.data[loop.index].uv=(p.x*3,p.z*3+p.y)
    return obj


def build(rig,mats):
    # Hidden thin connective axis; the visible shaft is formed by the vertebrae themselves.
    core=[(0,-.025,.025+i*2.055/40) for i in range(41)]
    rigid(curved_rod('InnerSpine',core,[.021]*41,mats,10),rig,'axe')
    for i in range(18):
        rigid(make(i,.071+i*.108,mats),rig,'axe')
    # The last bones widen into a forked socket carrying the blade.
    for side in (-1,1):
        rigid(curved_rod('AxeHeadSocket',[(side*.027,0,1.91),(side*.061,0,2.00),
            (side*.093,0,2.08),(side*.10,0,2.17)], [.029,.041,.035,.017],mats,12),rig,'axe')
