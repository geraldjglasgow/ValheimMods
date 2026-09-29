"""Preview-only 2D direction using the original icon drawing vocabulary.
Writes exclusively beside this script; does not change runtime artwork or code.
"""
import importlib.util
import math
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

HERE=Path(__file__).resolve().parent
source=HERE.parents[1]/'PackPanel'/'assets'/'skin_art.py'
spec=importlib.util.spec_from_file_location('legacy_icon_drawing',source)
art=importlib.util.module_from_spec(spec); spec.loader.exec_module(art)
art.finish=lambda img: img
def outlined(draw,points,fill=None):
    draw.polygon(points,fill=art.BRONZE if fill is None else fill)
    draw.line(points+[points[0]],fill=art.DARK,width=art.LINE,joint='curve')
art.shape=outlined
OUTLINE=(48,34,24,255)
# Restrained flat fills, one shadow and one highlight, with the old bold outlines.
PALETTES={
 'head':('#87918b','#626e69','#b6bca9'),
 'chest':('#9f7447','#715032','#c59c65'),
 'legs':('#977049','#695038','#bb9362'),
 'back':('#587b89','#3a5665','#88a5a8'),
 'backpack':('#587583','#344f5e','#8ba0a2'),
 'utility':('#93683c','#67472c','#c4a16b'),
 'food':('#af7046','#794729','#d49b64'),
 'mead':('#c1ab7d','#8e7954','#e1cea2'),
 'ammo':('#999884','#6f7367','#c6b88c'),
 'key':('#b1955f','#806842','#d8bb82'),
 'tacklebox':('#95714c','#6b5037','#baa077'),
 'tackle':('#8f9c98','#586d6d','#c4cabb'),
}
def rgba(s): return tuple(bytes.fromhex(s[1:]))+(255,)
BRONZE,SHADOW,LIGHT=map(rgba,('#aa7f4b','#775635','#d8b780'))

def hook():
    im,d=art.canvas()
    # One continuous J: straight shank, round bend, tapered inward-facing point.
    points=[(171,75),(171,164)]
    points += [(122+49*math.cos(a),164+49*math.sin(a)) for a in [i*math.pi/40 for i in range(41)]]
    points += [(73,139),(85,111)]
    for width,color in ((28,OUTLINE),(14,BRONZE)):
        d.line(points,fill=color,width=width,joint='curve')
    outlined(d,[(85,101),(95,143),(80,134),(70,147)],BRONZE)
    d.ellipse((145,27,197,79),fill=BRONZE,outline=OUTLINE,width=8)
    d.ellipse((160,42,182,64),fill=(0,0,0,0),outline=OUTLINE,width=4)
    d.line((169,91,169,155),fill=LIGHT,width=4)
    return im

def coin():
    im,d=art.canvas()
    for x,y,n in [(59,144,3),(155,115,4),(112,203,1)]:
        for k in range(n):
            z=y-k*10
            d.ellipse((x-40,z-18,x+40,z+18),fill=BRONZE,outline=OUTLINE,width=8)
        z=y-(n-1)*10
        d.ellipse((x-28,z-11,x+28,z+11),outline=LIGHT,width=5)
        d.line([(x-8,z),(x,z-6),(x+8,z),(x,z+6),(x-8,z)],fill=SHADOW,width=4)
    return im

icons={}
for name,paint in art.ICONS.items():
    art.BRONZE,art.SHADOW,art.LIGHT=BRONZE,SHADOW,LIGHT; art.BONE=LIGHT; art.DARK=OUTLINE; art.LINE=8
    im=hook() if name=='tackle' else paint(); d=ImageDraw.Draw(im)
    if name=='backpack':
        # Leather closure and brass buckle retain the simple original bag shape.
        d.rectangle((113,117,143,159),fill=OUTLINE)
        d.rectangle((119,123,137,152),fill=LIGHT)
        for x in range(90,169,17): d.line((x,201,x+5,207),fill=LIGHT,width=4)
    if name=='chest':
        for y in (95,111,127): d.line((121,y,135,y+5),fill=LIGHT,width=4)
    if name=='tacklebox':
        for x in (54,202):
            d.line((x,139,x,209),fill=SHADOW,width=8)
            d.ellipse((x-3,145,x+3,151),fill=LIGHT)
    icons[name]=im
icons['purse']=coin()
for name,im in icons.items():
    im.save(HERE/f'concept_{name}.png')
    im.resize((64,64),Image.Resampling.LANCZOS).save(HERE/f'concept_{name}_64.png')

order=['head','chest','legs','back','backpack','utility','food','mead','ammo','purse','key','tacklebox','tackle']
sheet=Image.new('RGB',(1280,920),(37,31,25)); d=ImageDraw.Draw(sheet)
try:
    font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',19)
    title=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',30)
except OSError: font=title=ImageFont.load_default()
d.text((38,22),'2D SLOT ICONS — CONCEPT ONLY',font=title,fill=(220,204,173))
d.text((40,65),'Simple silhouettes  /  unified bronze palette  /  bold outlines',font=font,fill=(168,151,126))
for i,name in enumerate(order):
    x=40+(i%5)*244; y=116+(i//5)*260
    d.rounded_rectangle((x,y,x+216,y+202),radius=12,fill=(53,43,32),outline=(82,66,47),width=1)
    sheet.paste(icons[name].resize((164,164),Image.Resampling.LANCZOS),(x+26,y+15),icons[name].resize((164,164),Image.Resampling.LANCZOS))
    label=name.upper(); d.text((x+108,y+215),label,font=font,anchor='mt',fill=(211,192,159))
sheet.save(HERE/'preview.png')
print(HERE/'preview.png')
