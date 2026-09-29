"""Frostfang: a weathered elder wolf's charcoal ruff; unscaled Wolf root frame."""
import os,sys,math
sys.path.insert(0,os.path.dirname(__file__))
from geometry import paint,root,plate
TEXTURE_SIZE=128
NORMAL_MAP=False
AO_STRENGTH=.35


def build():
    fur=paint('charcoal coarse guard hair',(.046,.044,.040),(.165,.154,.130),14)
    tips=paint('weathered grey fur tips',(.115,.119,.111),(.32,.33,.30),11)
    bone=paint('old shoulder fang trophies',(.22,.185,.12),(.57,.51,.36),9)
    frost=paint('sparse rime on upper guard hair',(.23,.27,.28),(.49,.53,.52),12)
    # Radial shag ends point backwards, making one rough mane instead of a smooth collar.
    for row,(y,rz,rx) in enumerate([(-.52,.28,.29),(-.30,.32,.34),(-.06,.26,.33)]):
        for j in range(11):
            a=(j+.3*row)*math.tau/11
            x=math.sin(a)*rx;z=1.02+math.cos(a)*rz
            reach=.15 if math.cos(a)>0 else .09
            root('long ragged ruff',[(x*.69,y-.08,1.02+(z-1.02)*.69),(x,y,z),(x*1.13,y+.27,z+reach)],[.11,.105,.004],fur if j%3 else tips,5)
    for i in range(4):
        y=-.4+i*.18;z=1.25-i*.058
        root('silver dorsal hackle',[(0,y,z),(.025,y+.10,z+.12),(0,y+.25,z+.06)],[.083,.074,.002],tips,5)
        if i<2:plate('frost caught in mane',(.015,y+.08,z+.12),(.065,.12,.014),frost,i)
    for s in [-1,1]:
        root('old ivory hooked shoulder spur',[(s*.30,-.36,1.04),(s*.43,-.38,.93),(s*.46,-.47,.76)],[.065,.052,.004],bone,6)

