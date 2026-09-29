"""Rimeback: broad weathered slate armour on the vanilla Lox root frame."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'ecp_frostfang'))
from geometry import paint,root,plate
TEXTURE_SIZE=256
NORMAL_MAP=False
AO_STRENGTH=.4


def build():
    slate=paint('dark weathered mountain slate',(.035,.044,.044),(.15,.18,.17),4)
    edge=paint('broken shale layers',(.075,.089,.087),(.24,.27,.25),6)
    snow=paint('dirty old snow caps',(.30,.33,.31),(.61,.63,.58),5)
    # Five overlapping heavy shields, low and broad rather than a fantasy crystal forest.
    for i,(y,z,w) in enumerate([(-1.75,3.66,1.22),(-.95,3.72,1.53),(-.1,3.48,1.59),(.75,3.22,1.40),(1.48,2.96,1.03)]):
        for s in [-1,1]:
            x=s*w*.51
            plate('split ancient slate mantle',(x,y,z),(.85*w,.67,.25),slate,i*3+(s+1)//2)
            plate('pale fractured rock edge',(x*1.1,y+.10,z+.04),(.60*w,.48,.24),edge,i*4)
            plate('snow gathered on upper ledge',(x*.71,y-.02,z+.25),(.44*w,.35,.035),snow,i*9)
    for s in [-1,1]:
        for i in range(3):
            y=-1.4+i*1.05
            root('broken downward shale flange',[(s*1.30,y,3.5-i*.20),(s*1.67,y+.18,3.16-i*.22),(s*1.55,y+.38,2.88-i*.22)],[.26,.29,.018],slate,5)

