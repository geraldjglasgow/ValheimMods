"""Full upper deck, real companionway and furnished cargo hold."""
import math
from geometry import mesh, beam, box, width, ribbon

LOWER=.25
UPPER=2.65
OPENING=(-.98,.98,1.35,5.65)


def inside(y,z):
    t=y/9.75
    base=-1.75+1.7*abs(t)**3+.65*abs(t)**4
    top=2.65
    f=min(1,max(0,(z-base)/(top-base)))
    return max(.1,width(t)*(.13+.87*math.sin(f*math.pi/2))-.18)


def plank(name,y0,y1,z,mat,group,cut=None):
    w0,w1=inside(y0,z),inside(y1,z)
    left0,left1,right0,right1=-w0,-w1,w0,w1
    if cut=='port':
        right0=right1=OPENING[0]
    if cut=='starboard':
        left0=left1=OPENING[1]
    if right0<=left0 or right1<=left1:
        return
    outline=[(left0,y0),(right0,y0),(right1,y1),(left1,y1)]
    verts=[(x,y,z+dz) for dz in (-.12,.12) for x,y in outline]
    faces=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    mesh(name,verts,faces,mat,group)


def decks(m):
    for i in range(36):
        y=-6.7+i*13.4/36
        plank('Hold_floor',y,y+13.4/36-.015,LOWER,m['wood'][i%3],'HoldFloor')
    boundaries=sorted(set([-9.73+i*19.46/54 for i in range(55)]+[OPENING[2],OPENING[3]]))
    for i,(a,b) in enumerate(zip(boundaries,boundaries[1:])):
        cut=OPENING[2]<=(a+b)/2<=OPENING[3]
        for side in ('port','starboard') if cut else (None,):
            plank('Upper_deck',a+.007,b-.007,UPPER,m['wood'][i%3],'UpperDeck',side)
    for y in (-6,-4,-2,.85,5.9,7.5):
        box('Deck_beam',(inside(y,UPPER)*2,.19,.2),(0,y,2.41),m['dark'],'UpperDeck')


def stairs(m):
    rise=(UPPER+.12-(LOWER+.12))/10
    for i in range(10):
        y=1.35+.18+i*.36
        top=UPPER+.12-(i+1)*rise
        box('Stair_tread_%02d'%i,(1.62,.36,.12),(0,y,top-.06),m['wood'][1],'Stairs')
        box('Stair_riser_%02d'%i,(1.62,.07,rise),(0,y-.15,top+rise/2),m['wood'][0],'Stairs')
    for x in (-.87,.87):
        beam('Stair_stringer',(x,1.35,2.56),(x,4.95,.16),.1,m['dark'],6,'Stairs')
        beam('Stair_handrail',(x,1.25,3.6),(x,5.0,1.18),.075,m['spar'],6,'Stairs')
        for y,z in ((1.45,2.48),(3.15,1.35),(4.85,.4)):
            beam('Stair_baluster',(x,y,z),(x,y,z+.85),.055,m['spar'],6,'Stairs')
    for x in (-1.07,1.07):
        box('Hatch_coaming',(.16,4.4,.22),(x,3.5,2.82),m['dark'],'UpperDeck')
        beam('Hatch_guard',(x,1.55,3.55),(x,5.65,3.55),.07,m['spar'],6,'UpperDeck')
        for y in (1.65,3.6,5.65):
            beam('Hatch_post',(x,y,2.76),(x,y,3.58),.075,m['spar'],6,'UpperDeck')
    beam('Hatch_end_guard',(-1.07,5.65,3.55),(1.07,5.65,3.55),.07,m['spar'],6,'UpperDeck')


def perimeter(m):
    for side in (-1,1):
        points=[]
        stations=sorted(set([-9.55+i*19.1/22 for i in range(23)]+([-.78,.78] if side==-1 else [])))
        for y in stations:
            if side==-1 and -.78<y<.78:
                continue
            x=side*inside(y,UPPER)
            top=3.65+.25*(abs(y)/9.55)**3
            points.append((x,y,top))
            beam('Deck_stanchion',(x,y,2.76),(x,y,top),.085,m['spar'],6,'DeckRail')
        for a,b in zip(points,points[1:]):
            if side==-1 and a[1]<0<b[1]:
                continue
            beam('Deck_handrail',a,b,.095,m['dark'],6,'DeckRail')
            beam('Deck_middle_rail',(a[0],a[1],a[2]-.42),(b[0],b[1],b[2]-.42),.055,m['spar'],6,'DeckRail')

    for y in (-9.55,9.55):
        w=inside(y,UPPER)
        beam('End_handrail',(-w,y,3.90),(w,y,3.90),.095,m['dark'],6,'DeckRail')
        beam('End_middle_rail',(-w,y,3.48),(w,y,3.48),.055,m['spar'],6,'DeckRail')


def barrel(x,y,m):
    rings=[]
    for z,r in ((.38,.34),(.48,.41),(.96,.46),(1.42,.4),(1.5,.33)):
        rings.append([(x+r*math.cos(i*math.tau/12),y+r*math.sin(i*math.tau/12),z) for i in range(12)])
    ribbon('Cargo_barrel',rings,m['wood'][1],'Cargo')
    for z,r in ((.58,.43),(1.29,.43)):
        band=[]
        for h in (z-.065,z+.065):
            band.append([(x+r*math.cos(i*math.tau/12),y+r*math.sin(i*math.tau/12),h) for i in range(12)])
        ribbon('Barrel_hoop',band,m['iron'],'Cargo')


def crate(x,y,z,m):
    box('Cargo_crate',(.85,.85,.7),(x,y,z),m['wood'][0],'Cargo')
    for dx in (-.33,.33):
        box('Crate_batten',(.12,.92,.12),(x+dx,y,z+.4),m['wood'][1],'Cargo')
    beam('Crate_crossbrace',(x-.37,y-.445,z-.27),(x+.37,y-.445,z+.27),.075,m['spar'],4,'Cargo')


def cargo(m):
    for x,y in ((-1.3,-3.8),(-1.3,-2.7),(1.3,-3.5),(1.65,-.9),(-1.65,.4),(-1.6,1.5)):
        barrel(x,y,m)
    for x,y,z in ((-1.0,-5,.74),(1.05,-5,.74),(1.45,1,.74),(-1.55,3,.74),(1.6,3,.74),(-1.55,3,1.55)):
        crate(x,y,z,m)
    for x in (-1.45,1.45):
        box('Hold_storage_chest',(1.05,1.1,.66),(x,-.0,.7),m['wood'][0],'Cargo')
        box('Chest_lid',(1.12,1.16,.12),(x,0,1.08),m['wood'][1],'Cargo')
        for dx in (-.34,.34):
            box('Chest_band',(.12,1.18,.06),(x+dx,0,1.16),m['iron'],'Cargo')


def decorate(m):
    for y in (-6.8,):
        for x in (-.85,.85):
            box('Deck_bench',(.55,1.5,.18),(x,y,3.16),m['wood'][1],'Decoration')
            for dy in (-.5,.5):
                box('Bench_leg',(.18,.18,.43),(x,y+dy,2.97),m['dark'],'Decoration')


def build(m):
    decks(m)
    stairs(m)
    perimeter(m)
    cargo(m)
    decorate(m)
