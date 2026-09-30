"""The eggs hatching, for the preview (MOVES.md): the last seconds' throb (faster and faster, the blood glow growing),
then the shell bursts into its petals, which fly out, tumble and crumble away in dust; the cup crumbles last; a game
Deathsquito rises out of it. The burst model is the workshop's ecp_queen_egg_burst, split into its loose pieces once
(the lowest is the cup); each piece turns about its own middle."""
import math

import bpy
from mathutils import Vector
from workshop import prefab

import queen_props

THROB, PETAL_FLY, CRUMBLE, CUP_WAIT, RISE = 2.0, 0.45, 0.4, 0.5, 0.8     # seconds
GAME_SQUITO = "Characters/Deathsquito/Deathsquito.prefab"
_pieces = None


def pieces():
    """The burst egg's pieces as hidden templates, each with its origin at its own middle; the cup first."""
    global _pieces
    if _pieces is None:
        whole = queen_props.template("ecp_queen_egg_burst").copy()
        whole.data = whole.data.copy()
        bpy.context.scene.collection.objects.link(whole)
        bpy.ops.object.select_all(action='DESELECT')
        whole.hide_viewport = False
        whole.select_set(True)
        bpy.context.view_layer.objects.active = whole
        bpy.ops.mesh.separate(type='LOOSE')
        parts = [o for o in bpy.context.selected_objects]
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')
        parts.sort(key=lambda o: o.location.z)
        for part in parts:
            part.hide_render = part.hide_viewport = True
        _pieces = parts
    return _pieces


def beats(start, fps):
    """Frames of the throb's beats: one a second rising to three over THROB seconds."""
    out, phase, f = [], 0.0, 0
    while f < THROB * fps:
        tau = f / fps
        phase += (1.0 + tau) / fps
        if phase >= 1.0:
            phase -= 1.0
            out.append(start + f)
        f += 1
    return out


def throb(keys, egg, rest, hatch, fps):
    """The egg swelling on each beat before it bursts; a red light in it growing. Returns the beat frames."""
    spot, rotation = rest
    light = bpy.data.objects.new(f"glow {egg.name}", bpy.data.lights.new(f"glow {egg.name}", 'POINT'))
    light.data.color, light.data.shadow_soft_size = (1.0, 0.25, 0.1), 0.2
    light.location = spot + Vector((0, 0, 0.25))
    bpy.context.scene.collection.objects.link(light)
    first = hatch - round(THROB * fps)
    marks = beats(first, fps)
    keys.add(light.data, 'energy', 1, 0.0)
    keys.add(light.data, 'energy', first - 2, 0.0)          # dark until the throb begins
    for f in range(first - 1, hatch + 1):
        swell = max(math.exp(-((f - b) / 2.5) ** 2) for b in marks) if marks else 0.0
        keys.add(egg, 'scale', f, (1 + 0.07 * swell,) * 3)
        keys.add(light.data, 'energy', f, (15 + 80 * (f - first) / (hatch - first)) * (0.5 + swell))
    keys.add(egg, 'scale', hatch + 1, (0, 0, 0), constant=True)
    keys.add(light.data, 'energy', hatch + 6, 0.0)
    return marks


def burst(keys, rest, hatch, fps, end, dust, seed):
    """The pieces at the egg's place from the hatch on: petals thrown out and up, tumbling, crumbling in dust; the cup
    crumbling last."""
    spot, rotation = rest
    turn = rotation.to_matrix().to_4x4()
    for k, template in enumerate(pieces()):
        piece = template.copy()
        piece.name, piece.hide_render, piece.hide_viewport = f"shell {seed} {k}", False, False
        bpy.context.scene.collection.objects.link(piece)
        home = spot + (turn @ template.location)
        piece.rotation_mode = 'ZXY'
        if k == 0:
            _cup(keys, piece, home, rotation, hatch, fps, end, dust)
        else:
            _petal(keys, piece, home, spot, rotation, hatch, fps, dust, seed + k)


def _cup(keys, piece, home, rotation, hatch, fps, end, dust):
    gone = hatch + round((CUP_WAIT + CRUMBLE) * fps)
    for f in range(hatch - 1, min(gone, end) + 2):
        crumble = min(max((f - hatch - CUP_WAIT * fps) / (CRUMBLE * fps), 0.0), 1.0)
        keys.add(piece, 'location', f, home - Vector((0, 0, 0.1 * crumble)))
        keys.add(piece, 'rotation_euler', f, rotation)
        keys.add(piece, 'scale', f, (0, 0, 0) if f < hatch or f > gone else (1 - crumble,) * 3)
    dust(home, hatch + round(CUP_WAIT * fps), 0.6)


def _petal(keys, piece, home, spot, rotation, hatch, fps, dust, seed):
    out = (home - spot)
    out = Vector((out.x, out.y, 0)).normalized() if out.xy.length > 1e-3 else Vector((1, 0, 0))
    fly, gone = round(PETAL_FLY * fps), round((PETAL_FLY + CRUMBLE) * fps)
    spin = Vector(((seed * 1.7) % 3 - 1.5, (seed * 2.3) % 3 - 1.5, (seed * 0.9) % 2 - 1))
    for f in range(hatch - 1, hatch + gone + 2):
        u = min(max((f - hatch) / fly, 0.0), 1.0)
        crumble = min(max((f - hatch - fly) / (gone - fly), 0.0), 1.0)
        rise = 0.9 * u - 1.1 * u * u
        keys.add(piece, 'location', f, home + out * 0.9 * u + Vector((0, 0, rise)))
        keys.add(piece, 'rotation_euler', f, Vector(rotation) + spin * (2.2 * u + 0.6 * crumble))
        keys.add(piece, 'scale', f, (0, 0, 0) if f < hatch or f > hatch + gone else (1 - crumble,) * 3)
    dust(home + out * 0.9 + Vector((0, 0, -0.2)), hatch + fly, 0.3)


def hatchling(keys, spot, hatch, fps, end, toward, index):
    """A game Deathsquito rising out of the cup, then turning to a player and flying at it."""
    root = prefab.load(GAME_SQUITO, f"hatchling {index}")
    root.rotation_mode = 'XYZ'
    appear, risen = hatch + 3, hatch + 3 + round(RISE * fps)
    face = math.atan2((toward - spot).x, -(toward - spot).y)
    for f in range(appear - 1, end + 1):
        u = min(max((f - appear) / (risen - appear), 0.0), 1.0)
        onward = max(0, f - risen) / fps
        at = spot + Vector((0, 0, -0.35 + 1.4 * (1 - (1 - u) ** 2) + 0.05 * math.sin(9 * f / fps)))
        at += (toward - spot).normalized() * min(2.5, 2.0 * onward)
        keys.add(root, 'location', f, at)
        keys.add(root, 'rotation_euler', f, (0.0, 0.0, face + (1 - u) * 2.5))
        keys.add(root, 'scale', f, (0, 0, 0) if f < appear else (0.5 + 0.5 * u,) * 3)
    return root
