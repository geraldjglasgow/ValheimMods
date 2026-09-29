"""Reference study for the bone battleaxe: the game's own greataxes, bone gear and skeletons, each rendered alone
(silhouette view along its thinnest axis, and a three-quarter view) on its own point-filtered textures, with its size,
triangle count and textures logged. Local reference only: nothing of the game's is saved into the asset.

    blender --background --factory-startup --python assets/ecp_battleaxe_bone/reference_study.py -- [group ...]

Writes out/reference/<group>.png (one row per prefab: silhouette, three-quarter) and out/reference/study.txt.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', '..', 'blender'))

import bpy  # noqa: E402
from mathutils import Vector  # noqa: E402
from workshop import prefab, preview, scene  # noqa: E402

OUT = os.path.join(HERE, 'out', 'reference')
ITEMS = 'GameElements/Items/'
GROUPS = {
    'greataxes': [ITEMS + f'weapons/{n}.prefab' for n in (
        'Battleaxe', 'BattleaxeBlackmetal', 'BattleaxeCrystal', 'BattleaxeSkullSplittur', 'BattleaxeWood',
        'AxeBerzerkr', 'AxeJotunBane', 'AxeFlint')] + ['Characters/Jotnar/model/weapons/Axe2h_JotunWarrior.prefab'],
    'bonegear': [ITEMS + p for p in (
        'weapons/BowSpineSnap.prefab', 'weapons/BowDraugrFang.prefab', 'shields/ShieldBoneTower.prefab',
        'weapons/PickaxeAntler.prefab', 'misc/LargeBone.prefab', 'materials/BoneFragments.prefab',
        'materials/CharredBone.prefab', 'materials/Charredskull.prefab', 'materials/AsksvinCarrionSkull.prefab',
        'materials/BonemawSerpentTooth.prefab', 'materials/HardAntler.prefab', 'trophies/TrophySkeleton.prefab',
        'materials/WolfFang.prefab')],
    'skeletons': ['Characters/Skeleton/Skeleton.prefab', 'Characters/Skeleton/Skeleton_Poison.prefab',
                  'Characters/TheCharred/Charred_Melee.prefab', 'Characters/TrollSkeleton/model/TrollUndead.prefab'],
}
TILE = 512


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    os.makedirs(OUT, exist_ok=True)
    log = open(os.path.join(OUT, 'study.txt'), 'a', encoding='utf-8')
    for group in argv or list(GROUPS):
        tiles = []
        for path in GROUPS[group]:
            tiles += study(path, group, log)
        preview.grid(tiles, 2, os.path.join(OUT, group + '.png'))
    log.close()


def study(path, group, log):
    scene.clear()
    stage()
    root = prefab.load(path, include_inactive=False)
    parts = prefab.meshes(root)
    if not parts:
        log.write(f'{path}: nothing drawn\n')
        return []
    low, high = prefab.bounds(parts)
    size = high - low
    tris = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in parts)
    textures = sorted({m.get('reference_albedo', m.name) for o in parts for m in o.data.materials if m})
    log.write(f'{path}: size {fmt(size)} tris {tris} parts {len(parts)} textures {textures}\n')
    thin = min(range(3), key=lambda i: size[i])
    along = Vector([1.0 if i == thin else 0.0 for i in range(3)])
    name = os.path.splitext(os.path.basename(path))[0]
    flat = shot(parts, along if thin != 1 else -along, True, os.path.join(OUT, f'{group}_{name}_flat.png'))
    turn = shot(parts, Vector((0.9, -1.2, 0.7)), False, os.path.join(OUT, f'{group}_{name}_3q.png'))
    return [flat, turn]


def stage():
    s = bpy.context.scene
    s.render.engine = 'BLENDER_EEVEE'
    s.eevee.taa_render_samples = 16
    s.render.resolution_x = s.render.resolution_y = TILE
    s.view_settings.view_transform = 'Standard'
    s.world = bpy.data.worlds.new('study')
    bg = next(n for n in s.world.node_tree.nodes if n.bl_idname == 'ShaderNodeBackground')
    bg.inputs['Color'].default_value = (0.42, 0.46, 0.44, 1.0)
    bg.inputs['Strength'].default_value = 0.9
    sun = bpy.data.objects.new('study_sun', bpy.data.lights.new('study_sun', 'SUN'))
    sun.data.energy = 2.2
    sun.rotation_euler = (math.radians(45), 0.0, math.radians(30))
    s.collection.objects.link(sun)
    camera = bpy.data.objects.new('study_camera', bpy.data.cameras.new('study_camera'))
    s.collection.objects.link(camera)
    s.camera = camera


def shot(parts, direction, ortho, path):
    camera = bpy.context.scene.camera
    low, high = prefab.bounds(parts)
    center, radius = (low + high) / 2, max((high - low).length / 2, 0.02)
    direction = direction.normalized()
    camera.data.type = 'ORTHO' if ortho else 'PERSP'
    camera.data.ortho_scale = radius * 2.1
    distance = radius * 4 if ortho else radius / math.sin(camera.data.angle / 2) * 1.05
    camera.data.clip_start, camera.data.clip_end = distance * 0.01, distance * 10
    camera.location = center + direction * distance
    camera.rotation_euler = (-direction).to_track_quat('-Z', 'Y').to_euler()
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    return path


def fmt(v):
    return '(' + ', '.join(f'{c:.3f}' for c in v) + ')'


main()
