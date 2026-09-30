"""Undead crypt rat, authored wholly in AssetWorkshop."""
import os
import sys
sys.path.insert(0, os.path.dirname(__file__))
from geometry import ellipsoid as ell, tube
from workshop import paint

CATEGORY = 'creature.quadruped'
TEXTURE_SIZE = 256
AO_STRENGTH = .2
NORMAL_MAP = True
NORMAL_FROM_ALBEDO = 3
TAIL = [(0,.55,.40),(.07,.79,.25),(.22,1.03,.13),(.40,1.23,.08),(.63,1.34,.10),(.81,1.26,.15)]


def build():
    fur = paint.fur('sodden rat hide', preset='wolf', tint='#555448', density=65)
    skin = paint.skin('dead grey skin', preset='draugr', tint='#6a7060', density=65)
    bone = paint.make('bone.bone', 'yellowed bone', density=65)
    flesh = paint.make('skin.flesh', 'dry torn tissue', tint='#563c38', region='secondary', density=65)
    dark = paint.skin('eye sockets', tint='#272820', region='secondary', density=65)
    eyes = paint.skin('pale bog eyes', tint='#a9b75b', region='glow', density=65)
    ell('haunch', (0,.27,.41), (.29,.38,.28), fur, 'Body')
    ell('ribcage', (0,-.12,.40), (.235,.36,.225), flesh, 'Body')
    ell('ragged back', (0,-.06,.54), (.23,.38,.15), fur, 'Body')
    ell('neck', (0,-.39,.43), (.17,.20,.19), skin, 'Head')
    ell('rat skull', (0,-.53,.45), (.185,.22,.17), fur, 'Head')
    tube('long wedge muzzle', [(0,-.56,.44),(0,-.75,.38),(0,-.90,.35)], [.15,.095,.04], skin, 'Head', 10)
    ell('nose', (0,-.91,.36), (.065,.04,.043), dark, 'Head', 8, 5)
    ell('lower jaw', (0,-.70,.285), (.11,.20,.052), flesh, 'Jaw', 10, 5)
    for s in (-1, 1):
        ell('round torn ear', (s*.185,-.405,.59), (.12,.052,.145), skin, 'Head', 9, 5)
        ell('ear hollow', (s*.19,-.449,.60), (.078,.012,.096), flesh, 'Head', 8, 5)
        ell('socket', (s*.151,-.602,.49), (.052,.068,.056), dark, 'Head', 8, 5)
        ell('eye', (s*.178,-.631,.497), (.025,.029,.027), eyes, 'Head', 8, 5)
        tube('upper incisor', [(s*.035,-.859,.345),(s*.037,-.874,.266),(s*.034,-.851,.237)], [.025,.022,.015], bone, 'Head', 5)
        tube('lower incisor', [(s*.035,-.847,.273),(s*.036,-.862,.324)], [.021,.014], bone, 'Jaw', 5)
        for i in range(4):
            y = -.29+i*.115
            tube('exposed rib', [(s*.14,y,.54),(s*.232,y-.02,.47),(s*.245,y,.36),(s*.19,y+.025,.275)], [.023,.025,.023,.012], bone, 'Body', 6)
        legs(s, fur, skin, bone)
    tail(skin)
    for i in range(5):
        ell('exposed vertebra', (0,-.27+i*.14,.665), (.046,.059,.035), bone, 'Body', 7, 4)


def tail(skin):
    from mathutils import Vector
    import math
    points=[]
    for ring in range(21):
        value=ring/4
        index=min(4,int(value))
        t=value-index
        a,b,c,d=[Vector(TAIL[max(0,min(5,j))]) for j in (index-1,index,index+1,index+2)]
        points.append(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t))
    radii=[.071-.066*i/20 for i in range(21)]
    obj=tube('continuous tail',points,radii,skin,'Tail0',8)
    obj.vertex_groups.clear()
    groups=[obj.vertex_groups.new(name='Tail'+str(i)) for i in range(5)]
    for ring in range(21):
        coordinate=max(0,min(4,ring/4-.5))
        left=int(math.floor(coordinate))
        right=min(4,left+1)
        blend=coordinate-left
        vertices=list(range(ring*8,(ring+1)*8))
        groups[left].add(vertices,1-blend,'REPLACE')
        if right!=left and blend>0:
            groups[right].add(vertices,blend,'REPLACE')


def legs(s, fur, skin, bone):
    side = 'L' if s < 0 else 'R'
    for name,y,x in [('Front',-.30,.20),('Back',.35,.245)]:
        upper, lower = name+side, name+side+'Foot'
        knee = (s*(x+.065),y+.06,.18)
        ankle = (s*(x+.06),y-.095,.065)
        muscle_size = (.072,.105,.16) if name == 'Front' else (.10,.145,.18)
        shin_radius = .050 if name == 'Front' else .065
        ell(name+' muscle', (s*x,y,.28), muscle_size, fur, upper, 10, 6)
        tube(name+' shin', [knee,ankle], [shin_radius,.032], skin, lower, 7)
        ell(name+' paw', (ankle[0],y-.16,.052), (.065,.12,.044), skin, lower, 8, 5)
        for toe in (-1,0,1):
            tube('claw', [(ankle[0]+toe*.038,y-.22,.049),(ankle[0]+toe*.04,y-.287,.026)], [.018,.004], bone, lower, 5)
