"""Uneven burial cloth: gravity folds, worn-through holes, and torn hems."""
import math
import random
import bpy
from geometry import mesh
from body import rigid


def patch(name, coords, columns, rows, mats, seed, holes=()):
    rng=random.Random(seed)
    faces=[]
    for row in range(rows-1):
        for col in range(columns-1):
            if (row,col) in holes:
                continue
            a=row*columns+col
            faces.append((a,a+1,a+columns+1,a+columns))
    obj=mesh(name,coords,faces,mats)
    for face in obj.data.polygons:
        face.material_index=rng.choices((0,1,2,3),(1,3,10,1))[0]
        face.use_smooth=True
    # Give both sides real separation: coincident reversed faces corrupt smooth normals.
    bpy.context.view_layer.objects.active=obj
    shell=obj.modifiers.new('Thin cloth thickness','SOLIDIFY')
    shell.thickness=.002; shell.offset=0
    bpy.ops.object.modifier_apply(modifier=shell.name)
    return obj


def build(rig,mats,waist):
    for i in range(7):
        rng=random.Random(113+i)
        angle=i*math.tau/7-.17
        width=.91+rng.uniform(-.13,.13)
        length=rng.uniform(.29,.51)
        columns,rows=9,6
        hems=[rng.uniform(-.065,.035) for _ in range(columns)]
        coords=[]
        for row in range(rows):
            t=row/(rows-1)
            for col in range(columns):
                u=col/(columns-1)
                a=angle+(u-.5)*width+.04*math.sin(t*3+i)
                fold=.016*math.sin(u*math.pi*4+i*.7)*(t+.3)
                rx=.253+.047*t+fold
                ry=.173+.045*t+fold
                z=waist-length*t+hems[col]*t*t
                coords.append((rx*math.cos(a),ry*math.sin(a),z))
        holes=[(4,0),(4,7)] if i%2==0 else [(3,5)]
        obj=patch('WornBrownWaistcloth_%02d'%i,coords,columns,rows,mats,i,holes)
        rigid(obj,rig,'Hips')
    # The same piece crosses the shoulder, shorter across the chest and torn down the back.
    for back in (False,True):
        rng=random.Random(812+back)
        columns,rows=9,7
        hems=[rng.uniform(-.07,.04) for _ in range(columns)]
        coords=[]
        for row in range(rows):
            t=row/(rows-1)
            for col in range(columns):
                u=col/(columns-1)
                x=.035+u*(.30-.08*t)+.045*t
                y=(.095+.12*t) if back else (.025-.27*math.sin(t*math.pi/2)**.55)
                y+=.017*math.sin(u*math.pi*4+.7)*(.3+t)
                z=2.055-.05*u-(.53 if back else .30)*t+hems[col]*t*t
                coords.append((x,y,z))
        holes=[(5,0),(5,6),(4,3)] if back else [(5,7)]
        obj=patch('BrownShoulderDrape_'+('Back' if back else 'Front'),coords,columns,rows,mats,91+back,holes)
        rigid(obj,rig,'Spine2')
