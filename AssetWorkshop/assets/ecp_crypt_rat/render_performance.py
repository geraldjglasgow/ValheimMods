"""Render the complete Blender performance to H.264 at its authored frame rate."""
import os
import bpy

out=os.path.join(os.path.dirname(os.path.abspath(__file__)),'out')
bpy.ops.wm.open_mainfile(filepath=os.path.join(out,'ecp_crypt_rat_performance.blend'))
scene=bpy.context.scene
scene.render.resolution_x=960
scene.render.resolution_y=700
scene.eevee.taa_render_samples=16
scene.render.image_settings.media_type='VIDEO'
scene.render.image_settings.file_format='FFMPEG'
scene.render.ffmpeg.format='MPEG4'
scene.render.ffmpeg.codec='H264'
scene.render.ffmpeg.constant_rate_factor='HIGH'
scene.render.filepath=os.path.join(out,'crypt_rat_performance.mp4')
bpy.ops.render.render(animation=True)
print('WORKSHOP video:',scene.render.filepath)
