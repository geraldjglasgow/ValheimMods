"""Preview-only gold emblems, using the original procedural 2D silhouettes.
All output stays beside this script; runtime assets and code are untouched.
"""
import importlib.util,math,random
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageFilter
HERE=Path(__file__).resolve().parent
source=HERE.parents[1]/'PackPanel'/'assets'/'skin_art.py'
spec=importlib.util.spec_from_file_location('legacy_shapes',source)
art=importlib.util.module_from_spec(spec); spec.loader.exec_module(art)
GOLD=(239,178,69,255); SHADE=(192,135,43,255); LIGHT=(255,204,104,255); INSET=(151,101,35,255)
art.BRONZE=GOLD; art.SHADOW=SHADE; art.LIGHT=LIGHT; art.DARK=INSET; art.BONE=LIGHT; art.LINE=3
art.finish=lambda im:im
def shape(d,points,fill=None):
    d.polygon(points,fill=GOLD if fill is None else fill)
art.shape=shape

def hook():
    im,d=art.canvas()
    d.ellipse((63,108,179,224),fill=GOLD)
    d.ellipse((83,128,159,204),fill=(0,0,0,0))
    d.rectangle((60,104,182,163),fill=(0,0,0,0))
    d.rectangle((159,68,179,166),fill=GOLD)
    d.polygon([(63,168),(63,139),(86,106),(94,137),(82,129),(83,168)],fill=GOLD)
    d.ellipse((145,24,193,72),fill=GOLD)
    d.ellipse((158,37,180,59),fill=(0,0,0,0))
    d.line((166,84,166,157),fill=LIGHT,width=3)
    return im

def coins():
    im,d=art.canvas()
    for x,y,n in [(64,155,3),(167,124,4),(137,205,1)]:
        for k in range(n):
            z=y-k*10
            d.ellipse((x-40,z-18,x+40,z+18),fill=GOLD,outline=INSET,width=3)
        z=y-(n-1)*10
        d.ellipse((x-29,z-11,x+29,z+11),outline=LIGHT,width=3)
        d.polygon([(x-7,z),(x,z-6),(x+7,z),(x,z+6)],fill=SHADE)
    return im

def field(w,h,seed):
    rng=random.Random(seed); low=Image.new('L',(17,17)); low.putdata([rng.randrange(70,190) for _ in range(289)])
    return low.resize((w,h),Image.Resampling.BICUBIC).filter(ImageFilter.GaussianBlur(3))

def gold_finish(im,seed):
    # Painted broad mottling instead of bevels, black strokes, or 3D lighting.
    broad=field(256,256,seed); fine=random.Random(seed+100)
    pix=im.load(); noise=broad.load()
    for y in range(256):
        for x in range(256):
            r,g,b,a=pix[x,y]
            if a:
                shade=(noise[x,y]-128)*.19+fine.uniform(-2,2)+(128-y)*.025
                pix[x,y]=(int(max(0,min(255,r+shade))),int(max(0,min(255,g+shade))),int(max(0,min(255,b+shade*.65))),a)
    return im

order=['head','chest','legs','back','backpack','utility','food','mead','ammo','purse','key','tacklebox','tackle']
icons={}
for i,name in enumerate(order):
    raw=coins() if name=='purse' else hook() if name=='tackle' else art.ICONS[name]()
    icons[name]=gold_finish(raw,23+i)
    icons[name].save(HERE/f'concept_{name}.png')
    icons[name].resize((64,64),Image.Resampling.LANCZOS).save(HERE/f'concept_{name}_64.png')

w,h=1200,830
sheet=Image.new('RGB',(w,h)); bg=field(w,h,99); px=sheet.load(); bp=bg.load()
for y in range(h):
    for x in range(w):
        v=(bp[x,y]-128)*.19
        px[x,y]=(int(74+v),int(51+v*.72),int(34+v*.5))
d=ImageDraw.Draw(sheet)
try:
    title=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',26)
    font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
except OSError: title=font=ImageFont.load_default()
d.text((35,22),'GOLD EMBLEMS / CONCEPT ONLY',font=title,fill=(237,193,119))
for i,name in enumerate(order):
    x=26+(i%5)*236; y=83+(i//5)*240
    icon=icons[name].resize((166,166),Image.Resampling.LANCZOS)
    sheet.paste(icon,(x+23,y),icon)
    d.text((x+106,y+180),name.upper(),anchor='mt',font=font,fill=(212,175,119))
sheet.save(HERE/'preview.png')
print(HERE/'preview.png')
