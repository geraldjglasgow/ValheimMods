"""Small tier-specific fishing details and hand-finished ornament, using each box's own materials."""
import math
from workshop import shapes
from geometry import mesh, rod, loft


def driftwood(p):
    # Pale hand stitching across the hide flap and a small tied bone charm.
    for i in range(13):
        x=-.125+i*.020
        rod('hide_edge_stitch',(x,-.132,.171),(x+.008,-.132,.176),.0012,p['rope'],4)
    rod('bone_charm_tie',(-.095,-.129,.180),(-.095,-.132,.151),.0016,p['rope'],4)
    rod('small_bone_charm',(-.111,-.134,.143),(-.079,-.134,.148),.004,p['bone'],6)
    # A restrained carved wave below the original toggle.
    points=[(-.039,-.115,.117),(-.020,-.117,.124),(0,-.117,.117),(.020,-.117,.124),(.039,-.115,.117)]
    for a,b in zip(points,points[1:]): rod('wood_wave_cut',a,b,.0016,p['seam'],4)


def face_badge(name, outline, y, mat, depth=.002):
    rings=[[(x,y,z) for x,z in outline],[(x,y-depth,z) for x,z in outline]]
    return loft(name,rings,mat)


def finewood(p):
    # A small bronze fish plaque below the clasp, with a punched eye.
    outline=[(-.045,.090),(-.028,.102),(-.010,.116),(.019,.112),(.037,.101),
             (.020,.088),(-.009,.085),(-.028,.097),(-.045,.108)]
    face_badge('bronze_fish_badge',outline,-.143,p['bronze'])
    rod('fish_eye',(.022,-.146,.102),(.022,-.148,.102),.0024,p['seam'])
    # Compact spool secured on the lid beside the handle, instead of a hanging side float.
    x,y,z=.026,-.045,.282
    rod('wound_line_spool',(x-.026,y,z),(x+.026,y,z),.018,p['rope'],10)
    for end in (-1,1):
        rod('spool_wood_end',(x+end*.027,y,z),(x+end*.032,y,z),.022,p['fine'],8)
    for i in range(6):
        sx=x-.023+i*.009
        # Narrow dark seams read as wound line at inventory scale.
        rod('spool_wrap_seam',(sx,y,z),(sx+.0018,y,z),.0185,p['seam'],10)
    rod('spool_tie',(x,-.068,.264),(x,-.065,.299),.0022,p['troll'],5)
    finewood_trim(p)


def finewood_trim(p):
    # Small bronze waves frame the fish without covering the wood grain.
    for side in (-1,1):
        pts=[(side*.033,-.145,.053),(side*.045,-.145,.061),
             (side*.060,-.145,.055),(side*.074,-.145,.062)]
        for a,b in zip(pts,pts[1:]): rod('bronze_wave_inlay',a,b,.0017,p['bronze'],4)
    for x in (-.117,.117):
        for z in (.102,.122,.142):
            rod('trollhide_cross_stitch',(x-.019,-.149,z),(x-.011,-.149,z+.005),.0012,p['rope'],4)
            rod('trollhide_cross_stitch',(x+.011,-.149,z),(x+.019,-.149,z+.005),.0012,p['rope'],4)


def carapace(p):
    # Collar around the existing fang clasp; two small natural shell studs.
    face_badge('clasp_shell_collar',[(-.025,.235),(-.023,.251),(0,.260),(.023,.251),(.025,.235)],-.172,p['shell'])
    for x in (-.016,.016):
        rod('shell_clasp_stud',(x,-.175,.245),(x,-.179,.245),.0035,p['shell_edge'])
    # An ivory-and-shell spoon lure hangs against the FRONT of the right-hand lid shield.
    x=.166
    rod('lure_tie',(x,-.127,.281),(x,-.157,.254),.0017,p['rope'],4)
    face_badge('shell_lure_rim',[(x,.257),(x+.012,.245),(x+.013,.223),(x,.207),
                               (x-.012,.223),(x-.009,.245)],-.157,p['shell_edge'])
    face_badge('shell_lure_inlay',[(x,.250),(x+.007,.239),(x+.008,.224),(x,.214),
                                 (x-.007,.225),(x-.005,.239)],-.160,p['shell'])
    rod('lure_hook_shank',(x,-.160,.210),(x,-.160,.193),.0014,p['bone'],5)
    rod('lure_hook_bend',(x,-.160,.193),(x+.007,-.160,.190),.0014,p['bone'],5)
    rod('lure_hook_point',(x+.007,-.160,.190),(x+.010,-.160,.198),.0014,p['bone'],5)
    carapace_ridges(p)


def carapace_ridges(p):
    # Short pale veins sit directly on the raised facets of the front shell plates.
    for i in range(4):
        x=(i-1.5)*.108
        y=-.16*math.sqrt(max(.15,1-(x/(.54*.57))**2))
        peak=(x,y*1.09-.0015,.131)
        upper=(x,y*1.07-.0015,.195)
        rod('shell_growth_ridge',peak,upper,.0018,p['shell_edge'],4)
        for side in (-1,1):
            tip=(x+side*.032,y*1.045-.002,.157)
            rod('shell_growth_branch',peak,tip,.0013,p['shell_edge'],4)


def flametal(p):
    # A restrained angular forge mark in exposed warm metal, centered below the latch.
    face_badge('forged_flame_emblem',[(-.030,.108),(-.017,.133),(-.020,.114),(0,.151),
               (.013,.128),(.020,.137),(.030,.111),(.015,.093),(-.013,.093)],-.183,p['flame_edge'])
    face_badge('flame_emblem_cutout',[(-.008,.110),(0,.131),(.010,.110),(0,.101)],-.186,p['flametal'])
    # Three hooks laid flat under a keeper on the lid: no hanging side ornament.
    for x in (-.034,0,.034):
        pts=[(x,-.025,.320),(x,-.069,.320)]
        for i in range(1,6):
            a=math.pi*i/5
            pts.append((x+.008-.008*math.cos(a),-.069-.008*math.sin(a),.320))
        pts.append((x+.016,-.061,.320))
        for a,b in zip(pts,pts[1:]): rod('forged_fishing_hook',a,b,.0018,p['flame_edge'],5)
    shapes.box('hide_hook_keeper',(.096,.014,.004),(0,-.042,.324),material=p['ask'],bevel=.001)
    for x in (-.044,.044):
        rod('keeper_rivet',(x,-.042,.326),(x,-.042,.330),.003,p['flame_edge'])
    flametal_trim(p)


def flametal_trim(p):
    # Forged corner diamonds, with rivets rather than a repeated hanging ornament.
    for side in (-1,1):
        x=side*.236
        face_badge('forged_corner_diamond',[(x-.012,.206),(x,.220),(x+.012,.206),(x,.192)],-.181,p['flame_edge'])
        rod('corner_dark_rivet',(x,-.184,.206),(x,-.187,.206),.003,p['flametal'],6)
    # Paired stitch rows on the broad Asksvin hide bindings.
    for x in (-.149,.149):
        for z in (.146,.169,.192):
            for side in (-1,1):
                sx=x+side*.027
                rod('askhide_stitch',(sx-.004,-.188,z),(sx+.004,-.188,z+.005),.0013,p['rope'],4)
