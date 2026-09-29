"""The workshop's sound synthesis kit: plain numpy, no audio libraries beyond soundfile for writing OGG.

    from kit import core, osc, noise, filters, env, shape, grain, reverb, resample, level, io

Everything works on float64 numpy arrays of samples at core.RATE (44.1 kHz, the rate of 94 % of the game's clips);
mono arrays are 1-D, stereo arrays (frames, 2). The codex's measuring modules (codex/measure/sfx_*.py) are put on the
path here, so a new sound is measured exactly as the game's were.
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
WORKSHOP = os.path.normpath(os.path.join(HERE, "..", ".."))
MEASURE = os.path.join(WORKSHOP, "codex", "measure")
if MEASURE not in sys.path:
    sys.path.insert(0, MEASURE)
