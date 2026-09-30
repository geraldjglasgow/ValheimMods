"""The preview's sounds on the timeline: each cue as sound strips at its frame, its variants taken in turn from the
newest out/sfx build of queen_sfx.py (the game's own recordings, trimmed, pitched and filtered as a mod can at run
time; SOUNDS.md), cut to the cue's longest length; her buzz looped under everything. As the Headsman's blender_fx."""
import os

import bpy

SFX = os.path.join(os.path.dirname(os.path.abspath(__file__)), "out", "sfx")
OWN = None
BUZZ_EVERY = 4.0          # the buzz loop file is exactly 4 s: back to back
# cue: [(variants, volume, longest seconds)]; OWN: the cue's files from queen_sfx.py (SOUNDS.md has the table)
SOUNDS = {
    'buzz_loop': [(OWN, 0.25, 4.0)],
    'buzz_strain': [(OWN, 0.35, 1.0)],
    'dive_whoosh': [(OWN, 0.45, 0.5)],
    'pierce': [(OWN, 0.5, 0.4)],
    'lunge': [(OWN, 0.45, 0.9)],
    'egg_launch': [(OWN, 0.4, 0.4)],
    'egg_land': [(OWN, 0.4, 0.4)],
    'egg_pulse': [(OWN, 0.25, 0.3)],
    'egg_burst': [(OWN, 0.5, 0.8)],
    'hatch_buzz': [(OWN, 0.3, 1.4)],
    'needle_shot': [(OWN, 0.3, 0.2)],
    'needle_ground': [(OWN, 0.25, 0.25)],
    'needle_flesh': [(OWN, 0.35, 0.3)],
    'zip_whir': [(OWN, 0.45, 1.1)],
    'alert': [(OWN, 0.5, 1.2)],
}


def buzz(end, fps):
    """The buzz loop's cues, back to back, and her alert as the fight opens."""
    step = round(BUZZ_EVERY * fps)
    return [(f, 'buzz_loop') for f in range(1, end, step)] + [(8, 'alert')]


def sounds(cues, fps):
    """Every cue as sound strips at its frame; returns how many were placed (0 before queen_sfx.py has run)."""
    scene = bpy.context.scene
    editor = scene.sequence_editor_create()
    strips = editor.strips if hasattr(editor, 'strips') else editor.sequences
    ends, used, placed = {}, {}, 0
    for frame, cue in sorted(cues):
        for layer, (variants, volume, longest) in enumerate(SOUNDS.get(cue, [])):
            paths = _own(cue) if variants is OWN else variants
            if not paths:
                continue
            n = used.get((cue, layer), 0)
            used[(cue, layer)] = n + 1
            placed += _strip(strips, ends, paths[n % len(paths)], frame, volume, int(longest * fps))
    try:
        scene.sync_mode = 'AUDIO_SYNC'
    except TypeError:
        pass
    print(f"WORKSHOP sounds: {placed} strips", flush=True)
    return placed


def _own(cue):
    """The cue's variants from queen_sfx.py's newest build folder, in order."""
    builds = sorted(d for d in os.listdir(SFX) if d.startswith('build_')) if os.path.isdir(SFX) else []
    if not builds:
        return []
    folder = os.path.join(SFX, builds[-1])
    names = [f for f in os.listdir(folder) if f.startswith(cue + '_') and f[len(cue) + 1:-4].isdigit()]
    return [os.path.join(folder, f) for f in sorted(names, key=lambda f: int(f[len(cue) + 1:-4]))]


def _strip(strips, ends, path, start, volume, longest):
    channel = next(c for c in range(1, 28) if ends.get(c, 0) < start)
    strip = strips.new_sound(os.path.basename(path), path, channel, start)
    strip.volume = volume
    if strip.frame_final_duration > longest:
        strip.frame_final_duration = longest
    ends[channel] = strip.frame_final_end
    return 1
