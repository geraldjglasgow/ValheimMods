"""Package only Havorn's own outputs, never vanilla reference assets."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from PIL import Image,ImageDraw,ImageFont

ROOT=Path(__file__).resolve().parent
OUT=ROOT/'out'
FILES=['Havorn_Finished_Ends.blend','Havorn_Hold_Inspection.blend',
       'havorn_parts.blend','havorn_parts.fbx','havorn.glb','havorn_construction.blend',
       'workshop_havorn_albedo.png','workshop_havorn_normal.png','workshop_havorn_regions.png',
       'colored_ship.png','colored_boarding.png','colored_helm.png','colored_deck.png','colored_hold_cutaway.png',
       'colored_lookout.png','colored_ladder.png','colored_cannon.png','cannon_180_aim.png',
       'validation.json','style_report.txt','interactions.json']
SOURCES=['ship_fittings.py','fit_checks.py','model.py','geometry.py','deckworks.py','lookout_cannon.py','interaction_rig.py',
         'finish.py','showcase.py','package.py','build.ps1']

sheet=Image.new('RGB',(1920,590),(26,34,39))
draw=ImageDraw.Draw(sheet)
font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',27)
draw.text((32,14),'BOW SWIVEL  |  180 DEGREE FORWARD AIMING ARC',font=font,fill=(237,220,189))
for i,(file,label) in enumerate((('colored_cannon_port','PORT 90'),('colored_cannon','BOW 0'),
                                ('colored_cannon_starboard','STARBOARD 90'))):
    with Image.open(OUT/(file+'.png')) as tile:
        sheet.paste(tile.convert('RGB').resize((640,480),Image.Resampling.LANCZOS),(i*640,60))
    draw.text((i*640+24,549),label,font=font,fill=(232,235,236))
sheet.save(OUT/'cannon_180_aim.png')

with ZipFile(OUT/'Havorn.zip','w',ZIP_DEFLATED) as archive:
    for name in FILES:
        archive.write(OUT/name,name)
    for name in SOURCES:
        archive.write(ROOT/name,'source/'+name)
    for name in ('README.md','BRIEF.md'):
        archive.write(ROOT/name,name)
with ZipFile(OUT/'Havorn.zip') as archive:
    assert archive.testzip() is None
    print('Havorn delivery verified:',len(archive.namelist()),'files')
