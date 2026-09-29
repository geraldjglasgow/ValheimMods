import os
import sys
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'tacklebox_remakes')))
import designs
TEXTURE_SIZE = 256
NORMAL_MAP = True
AO_STRENGTH = 0.4

def build():
    designs.build('flametal')