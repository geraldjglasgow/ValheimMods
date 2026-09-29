"""Scree Wing: shale brow, backward sweeping crag horns and a broken stone crest.
Original attachment built in Hatchling root rest coordinates, mounted to Head.
"""
import importlib.util
from pathlib import Path
_spec=importlib.util.spec_from_file_location('cairn_shapes',Path(__file__).resolve().parent.parent/'ecp_cairn_wight/model.py')
_shapes=importlib.util.module_from_spec(_spec);_spec.loader.exec_module(_shapes)
paint,lump,horn=_shapes.paint,_shapes.lump,_shapes.horn
TEXTURE_SIZE=256
NORMAL_MAP=False
AO_STRENGTH=.65

def build():
    slate=paint('weathered mountain shale',(.025,.031,.035),(.145,.17,.18))
    edge=paint('pale chipped shale',(.10,.115,.117),(.29,.31,.285))
    hornmat=paint('old crag horn',(.055,.044,.034),(.25,.225,.17))
    lump('heavy shale brow',(0,-.36,2.21),(.27,.19,.15),slate,2)
    lump('beak shield',(0,-.49,2.06),(.16,.135,.21),slate,1)
    for s in [-1,1]:
        lump('cheek plate',(s*.21,-.27,2.05),(.12,.15,.14),edge)
        horn('swept horn',[(s*.20,-.20,2.26),(s*.36,.04,2.49),(s*.43,.34,2.62),(s*.37,.56,2.60)],[.13,.105,.057,.006],hornmat,7)
        horn('lower horn',[(s*.20,-.16,2.09),(s*.34,.13,2.18),(s*.40,.29,2.23)],[.07,.045,.003],slate)
    # Five asymmetric thick rock vanes produce the serrated silhouette at a distance.
    for i in range(5):
        y=-.20+i*.13
        horn('crest shard',[(0,y,2.27-i*.025),(0,y+.07,2.54-i*.06),(0,y+.19,2.60-i*.065)],[.115,.075,.004],slate,5)

