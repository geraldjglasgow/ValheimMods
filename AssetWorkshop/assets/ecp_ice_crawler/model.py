"""Ice Crawler: chunky icy shale crest on the vanilla Neck root frame."""
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'ecp_frostfang'))
from geometry import paint,root,plate
TEXTURE_SIZE=128
NORMAL_MAP=False
AO_STRENGTH=.35


def build():
    shale=paint('cold shale shell',(.047,.071,.076),(.17,.23,.23),9)
    ice=paint('opaque old glacial ice',(.15,.23,.25),(.39,.49,.48),8)
    dark=paint('wet shale edges',(.025,.041,.044),(.10,.15,.15),11)
    for i,(y,z,w) in enumerate([(-.02,.95,.23),(.16,.80,.30),(.34,.66,.33),(.51,.53,.25),(.67,.40,.15)]):
        for s in [-1,1]:
            plate('overlapping shale scute',(s*w*.53,y,z),(w*.72,.18,.077),shale,12+i*2+(s+1)//2)
        root('broad broken ice sail',[(0,y-.09,z),(.025,y+.025,z+.27-i*.025),(-.015,y+.14,z+.12)],[.070,.067,.004],ice,4)
    for s in [-1,1]:
        root('rough dark lateral ridge',[(s*.21,-.07,.92),(s*.34,.16,.72),(s*.33,.35,.56),(s*.19,.58,.43)],[.033,.045,.048,.008],dark,5)
