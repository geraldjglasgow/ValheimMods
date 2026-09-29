"""Four original tacklebox silhouettes, progressively larger. Front -Y, origin on the ground."""
import math
from workshop import shapes
from geometry import mesh,rod,loft,octagon,ellipse,band,handle,clasp,bobber,stitch_row
from palette import make
import decorations


def driftwood(p):
    w,d,h=.36,.24,.185
    loft('tapered_wood_core',[octagon(w*.84,d*.83,.012),octagon(w,d,h)],p['drift'])
    # A creel-like flared body, horizontal rough boards, bound rather than metal-capped.
    for k in range(3):
        z=.025+k*.052; f=.86+.045*k
        for side in (-1,1):
            shapes.box('rough_long_plank',(w*f,.016,.046),(0,side*d*(.415+.023*k),z+.021),
                       material=p['drift'],bevel=.003,rotation=(0,0,side*.008*(k-1)))
    loft('low_lid',[octagon(w+.016,d+.012,h+.002),octagon(w+.010,d+.008,h+.020)],p['drift'])
    # Fur is a thick irregular blanket, including a visible jagged edge.
    points=[]
    for j,(y,z) in enumerate([(d*.49,h+.007),(d*.44,h+.025),(-d*.45,h+.025),(-d*.54,h-.027)]):
        for i in range(13):
            x=(i/12-.5)*w*.79
            points.append((x,y,z+(.006*math.sin(i*2.2) if j==3 else 0)))
    mesh('deerhide_blanket',points,[(j*13+i,j*13+i+1,(j+1)*13+i+1,(j+1)*13+i) for j in range(3) for i in range(12)],p['fur'])
    for x in (-w*.28,w*.28):
        path=[(x,-d*.46,.026),(x,-d*.52,h+.027),(x,d*.52,h+.027),(x,d*.46,.026)]
        for i in range(3): rod('leather_lashing',path[i],path[i+1],.005,p['rope'],5)
    handle(w*.82,h+.029,p['rope'],p['drift'])
    clasp(-d*.55,h-.008,p['bone']); bobber(w/2,h*.8,p)
    decorations.driftwood(p)
    shapes.collider_box('body',(w,d,h+.024),(0,0,(h+.024)/2))


def finewood(p):
    w,d,h=.45,.28,.210
    shapes.box('finewood_body',(w,d,h-.016),(0,0,h/2),material=p['fine'],bevel=.009)
    # Real board seams and a low, five-sided crown distinguish it from the driftwood creel.
    for z in (.078,.140):
        for side in (-1,1): shapes.box('board_joint',(w-.020,.0015,.0025),(0,side*(d/2+.0005),z),material=p['seam'])
    rings=[]
    for x in (-w/2-.006,w/2+.006):
        rings.append([(x,-d/2,h),(x,-d/2,h+.025),(x,-d*.27,h+.052),
                      (x,d*.27,h+.052),(x,d/2,h+.025),(x,d/2,h)])
    loft('crowned_finewood_lid',rings,p['fine'])
    for x in (-w*.26,w*.26):
        band('trollhide_binding',[(x,-d/2-.005,.026),(x,-d/2-.005,h+.027),
             (x,-d*.27,h+.057),(x,d*.27,h+.057),(x,d/2+.005,h+.027),(x,d/2+.005,.026)],.052,p['troll'])
        for y in (-d/2-.008,d/2+.008):
            shapes.box('bronze_binding_end',(.060,.005,.027),(x,y,.035),material=p['bronze'],bevel=.002)
        for yy,zz in [(-d/2-.01,.175),(-d/2-.01,.080)]:
            rod('strap_rivet',(x,yy,zz),(x,yy-.003,zz),.0035,p['bronze'])
    for side in (-1,1):
        shapes.box('bronze_corner',(.047,.006,.05),(side*(w/2-.024),-d/2-.004,.045),material=p['bronze'],bevel=.003)
        shapes.box('hinge',(.022,.008,.060),(side*w*.27,d/2+.005,h-.002),material=p['bronze'],bevel=.002)
    clasp(-d/2-.012,h-.008,p['bronze'])
    handle(w,h+.060,p['bronze'],p['troll'])
    decorations.finewood(p)
    shapes.collider_box('body',(w,d,h+.052),(0,0,(h+.052)/2))


def shell_plate(name,cx,w,d,z,p):
    # Hexagonal raised shield, large planar facets like seeker carapace.
    ring=[(cx-w/2,-d*.25,z),(cx-w*.25,-d/2,z),(cx+w*.28,-d/2,z),
          (cx+w/2,0,z),(cx+w*.28,d/2,z),(cx-w*.25,d/2,z),(cx-w/2,d*.25,z)]
    inner=[(cx+(x-cx)*.90,y*.88,zz+.012) for x,y,zz in ring]
    loft(name+'_edge',[ring,inner],p['shell_edge'],False)
    peak=(cx,0,z+.052)
    mesh(name,inner+[peak],[(i,(i+1)%7,7) for i in range(7)],p['shell'])


def carapace(p):
    w,d,h=.54,.32,.232
    loft('oval_yggdrasil_case',[ellipse(w*.82,d*.85,.014),ellipse(w,d,h*.72),ellipse(w*.96,d*.94,h)],p['ygg'])
    loft('dark_lid_seam',[ellipse(w*.96,d*.94,h),ellipse(w*.98,d*.96,h+.009)],p['seam'])
    for side in (-1,1):
        for i in range(4):
            x=(i-1.5)*.108
            # Sloped shields on the flanks, not a reskinned rectangular chest.
            y=side*(d/2)*math.sqrt(max(.15,1-(x/(w*.57))**2))
            verts=[(x-.057,y*.80,.045),(x+.057,y*.80,.045),(x+.066,y,.184),
                   (x,y*1.07,.211),(x-.066,y,.184),(x,y*1.09,.131)]
            mesh('flank_carapace',verts,[(0,1,5),(1,2,5),(2,3,5),(3,4,5),(4,0,5)],p['shell'])
    for i in range(3): shell_plate('overlapping_lid_plate', (i-1)*.154,.212,d*.93,h+.013+i*.003,p)
    handle(w*.8,h+.072,p['shell_edge'],p['ygg'])
    # A bone-colored locking fang in place of a square hasp.
    loft('locking_fang',[[(-.02,-d*.49,h+.018),(.02,-d*.49,h+.018),(.017,-d*.54,h+.005),(-.017,-d*.54,h+.005)],
                         [(-.003,-d*.55,h-.052),(.003,-d*.55,h-.052),(.003,-d*.565,h-.054),(-.003,-d*.565,h-.054)]],p['shell_edge'])
    decorations.carapace(p)
    shapes.collider_box('body',(w*.87,d*.86,h+.065),(0,0,(h+.065)/2))


def flametal(p):
    w,d,h=.62,.36,.245
    # Chamfered strongbox: broad shoulders and an angular, sloped lid.
    loft('forged_octagonal_body',[octagon(w*.90,d*.91,.016,.23),octagon(w,d,h,.23)],p['flametal'])
    loft('base_rim',[octagon(w*.91,d*.93,.014,.23),octagon(w*.95,d*.96,.039,.23)],p['flame_edge'])
    loft('lid_joint',[octagon(w*1.01,d*1.01,h,.23),octagon(w*1.01,d*1.01,h+.010,.23)],p['seam'])
    loft('sloped_armored_lid',[octagon(w*1.02,d*1.02,h+.011,.23),octagon(w*.81,d*.78,h+.070,.23)],p['flametal'])
    for x in (-w*.24,w*.24):
        band('asksvin_hide_wrap',[(x,-d*.46,.042),(x,-d*.51,h+.018),(x,-d*.39,h+.074),
             (x,d*.39,h+.074),(x,d*.51,h+.018),(x,d*.46,.042)],.078,p['ask'])
        shapes.box('flametal_strap_clamp',(.085,.007,.025),(x,-d*.51-.005,h*.48),material=p['flame_edge'],bevel=.003)
    # Reinforcing chevrons cut across the front; warm exposed edges, no neon or molten cracks.
    for side in (-1,1):
        for z in (.065,.164):
            rod('forged_chevron',(side*.10,-d*.49,z+.022),(side*.25,-d*.47,z),.005,p['flame_edge'],4)
    clasp(-d*.53,h-.02,p['flame_edge'])
    handle(w*.88,h+.079,p['flame_edge'],p['ask'])
    decorations.flametal(p)
    shapes.collider_box('body',(w*.90,d*.9,h+.07),(0,0,(h+.07)/2))


def build(tier):
    p=make()
    globals()[tier](p)
