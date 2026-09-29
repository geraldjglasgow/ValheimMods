"""Fen Crawler: a peat plated back with broken reed crest, fitted to vanilla Neck."""
import os,sys
sys.path.insert(0,os.path.dirname(__file__))
from geometry import paint,root,plate
TEXTURE_SIZE=128
NORMAL_MAP=False
AO_STRENGTH=.4

def build():
    bark=paint('wet rotting bark',(.035,.038,.018),(.135,.12,.051))
    peat=paint('fen peat carapace',(.055,.07,.026),(.215,.21,.085))
    straw=paint('dead swamp reed',(.115,.105,.041),(.31,.27,.115))
    # Shingled back, deliberately broad and asymmetrical, clear of neck and limbs.
    for i,(y,z,w) in enumerate([(-.02,.90,.28),(.17,.77,.34),(.37,.64,.32),(.52,.51,.23)]):
        for s in (-1,1):
            plate('split bark carapace',(s*w*.53,y,z),(w*.70,.205,.085),peat,i*2+(s+1)//2)
        root('raised woody keel',[(0,y-.14,z+.02),(.02,y,z+.18),(0,y+.16,z+.04)],[.053,.071,.035],bark)
    for s in (-1,1):
        root('curving flank rim',[(s*.22,-.12,.91),(s*.37,.11,.72),(s*.34,.36,.58),(s*.19,.55,.45)],[.046,.052,.049,.018],bark)
    for i,(x,y,z,h) in enumerate([(-.16,.0,.96,.35),(.14,.11,.90,.24),(-.11,.23,.82,.37),(.16,.35,.72,.27),(-.07,.47,.62,.22)]):
        root('broken reed ridge',[(x,y,z),(x-.03,y+.05,z+h*.65),(x+.015,y+.13,z+h)],[.038,.021,.004],straw,5)
        if i in (0,2):root('reed splinter',[(x-.02,y+.045,z+h*.60),(x-.13,y+.04,z+h*.84)],[.02,.001],bark,4)
    # Rest-pose fitting: follow the Neck's sloping back closely, clear of shoulders.
    import bpy
    for o in bpy.context.scene.objects:
        if o.type=='MESH':
            for v in o.data.vertices:
                v.co.x*=.82
                v.co.z-=.13
