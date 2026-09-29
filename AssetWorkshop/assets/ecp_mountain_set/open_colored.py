"""Show the local mountain review scene with its complete textured materials."""
import bpy


def show_colors():
    windows = bpy.context.window_manager.windows
    if not windows:
        return .5
    for window in windows:
        for area in window.screen.areas:
            if area.type != 'VIEW_3D':
                continue
            space = area.spaces.active
            space.shading.type = 'MATERIAL'
            space.shading.use_scene_lights = True
            space.shading.use_scene_world = True
            space.overlay.show_overlays = False
            space.region_3d.view_perspective = 'CAMERA'
            space.region_3d.view_camera_zoom = 0
    return None


bpy.app.timers.register(show_colors, first_interval=1.0)
