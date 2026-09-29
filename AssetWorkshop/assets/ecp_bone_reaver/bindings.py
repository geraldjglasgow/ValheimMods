"""Layered sinew lashings and knots, shaped around the bone joint and grips."""
import math
import bpy
from geometry import curved_rod, mesh
from body import rigid


def build(rig,leather):
    for base,radius,turns,step in ((.235,.077,3,.032),(.595,.080,3,.032),(1.95,.11,4,.036)):
        points=[]
        for j in range(turns*24+1):
            a=j*math.tau/24
            z=base+step*j/24
            r=radius*(1+.045*math.sin(a*3+z*8))
            points.append((r*math.cos(a),.018+r*math.sin(a)*(1.35 if base>1 else 1),z))
        vertices=[]
        for x,y,z in points: vertices.extend(((x,y,z-.008),(x,y,z+.008)))
        faces=[(2*i+2,2*i+3,2*i+1,2*i) for i in range(len(points)-1)]
        # A thin leather strip, visible from either side of the binding.
        obj=mesh('SpiralRawhide',vertices,faces,leather)
        for p in obj.data.polygons: p.material_index=1
        bpy.context.view_layer.objects.active=obj
        shell=obj.modifiers.new('LeatherThickness','SOLIDIFY')
        shell.thickness=.002
        shell.offset=0
        bpy.ops.object.modifier_apply(modifier=shell.name)
        rigid(obj,rig,'axe')
        knot=[(-radius,-.025,base+.04),(-radius-.025,-.055,base+.06),
              (-radius,-.075,base+.04),(-radius+.015,-.05,base+.025),(-radius,-.025,base+.04)]
        rigid(curved_rod('BindingKnot',knot,[.009]*5,leather,7),rig,'axe')
    # A crossed retaining stitch ties the blade's thicker heel into the spine socket.
    for side in (-1,1):
        points=[(-.06,side*.11,1.965),(.075,side*.15,2.025),(-.055,side*.12,2.095)]
        rigid(curved_rod('SocketCrossLacing',points,[.008,.009,.008],leather,7),rig,'axe')
