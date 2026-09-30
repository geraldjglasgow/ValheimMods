"""Open with Blender --python to play the roam performance (or the bite asset)."""
import bpy


def play():
    rig = bpy.data.objects.get('crypt_rat_rig')
    action = bpy.data.actions.get('Crypt Rat - idle, roam, lunge') or bpy.data.actions.get('attack_bite')
    if rig is None or action is None:
        raise RuntimeError('Open the animated Crypt Rat scene before running playback')
    rig.animation_data_create()
    rig.animation_data.action = action
    scene = bpy.context.scene
    scene.render.fps = 30
    scene.frame_start = 0
    if not action.name.startswith('Crypt Rat -'):
        scene.frame_end = 54
    scene.sync_mode = 'FRAME_DROP'
    scene.frame_set(0)
    for window in bpy.context.window_manager.windows:
        area = next((a for a in window.screen.areas if a.type == 'VIEW_3D'), None)
        if area is not None:
            area.spaces.active.shading.type = 'MATERIAL'
            with bpy.context.temp_override(window=window, area=area):
                if not window.screen.is_animation_playing:
                    bpy.ops.screen.animation_play()
            return None
    return .5


bpy.app.timers.register(play, first_interval=1.5)
