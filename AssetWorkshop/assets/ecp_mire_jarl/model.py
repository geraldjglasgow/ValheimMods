"""Original corroded grave-lamellar and peat mantle fitted to vanilla Draugr Elite."""
import math, random
import bpy
from workshop import materials, shapes
TEXTURE_SIZE = 256
NORMAL_MAP = False
AO_STRENGTH = .42

def mesh(name, verts, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(mat)
    return obj

def plate(name, x, y, z, w, h, mat, lean=0):
    # Broken six-sided plate, folded around the torso rather than a perfect cube.
    verts = [(x-w/2,y,z+h/2),(x+w/2,y,z+h/2),
             (x+w*.48,y-.025,z-h*.28),(x+w*.22,y+.012,z-h*.53),
             (x-w*.4,y+.018,z-h*.48),(x-w*.55,y-.018,z-h*.12)]
    obj = mesh(name, verts, [(0,1,2),(0,2,5),(5,2,3,4)], mat)
    solid = obj.modifiers.new('hammered thickness','SOLIDIFY')
    solid.thickness = .025
    return obj

def build():
    random.seed(56)
    iron = materials.iron('peat black iron', (.058,.065,.047), (.105,.058,.026))
    hide = materials.flat('waterlogged hide', (.067,.056,.029))
    moss = materials.stone('bog lichen', (.12,.139,.054), (.04,.057,.021), 11)
    brass = materials.iron('old rivets', (.15,.119,.05), (.081,.076,.04))
    for row in range(3):
        for col in range(5):
            x = (col-2)*.155
            y = -.285 + abs(col-2)*.024
            z = 1.77-row*.19
            plate('overlapping burial iron',x,y,z,.175,.255,iron)
            for dx in (-.052,.052):
                shapes.sphere('square rivet',.016,(x+dx,y-.014,z+.07),brass,segments=4,rings=3)
    # Unequal shoulder caps leave the original arms free for their attack animation.
    for side, width in ((-1,.35),(1,.27)):
        x = side*.44
        verts = [(x-width/2,-.23,1.93),(x+width/2,-.18,1.91),
                 (x+width*.65,.10,1.85),(x-width*.55,.19,1.87),(x,.02,2.04)]
        mesh('shoulder bog iron',verts,[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],iron)
    # Ragged peat-stiffened mantle, broad at shoulders and torn at waist.
    for i in range(11):
        x = (i-5)*.086
        z = 1.32 + random.uniform(-.15,.12)
        plate('torn moss mantle',x,.245,1.73,.105,(1.73-z)*2,moss if i%3 else hide)
    for i in range(7):
        plate('hanging grave scale',(i-3)*.102,-.255,1.12,.095,.25,hide)

