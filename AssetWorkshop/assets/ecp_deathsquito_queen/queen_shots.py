"""How the preview is shown: a camera per stretch of the fight (framed round where she goes and what she aims at, from
the side of her line of attack, from her front three-quarters, from high or wide), a marker cutting to it, a caption;
her belly and eyes glowing as the game's emission map will make them; EEVEE with motion blur (the buzzing wings are a
blur, as an insect's are); the sounds on the timeline; stills and the video."""
import math
import os

import bpy
from mathutils import Vector

import queen_stage

RESOLUTION, LENS, MARGIN = (1280, 720), 30.0, 1.1
GLOW = {(0.941, 0.196, 0.902): 2.5, (0.502, 0.0, 0.0): 4.0}   # region mask colours (glow, eyes) -> strength
UP = Vector((0, 0, 1))


def direction(kind, queen, target):
    """Where the camera stands from the middle of what it frames."""
    line = Vector(((target - queen).x, (target - queen).y, 0.0)).normalized()
    side = Vector((-line.y, line.x, 0.0))
    if side.dot(Vector((1.0, -0.4, 0.0))) < 0:
        side = -side
    if kind == "side":
        return (side + UP * 0.28 - line * 0.15).normalized()
    if kind == "close":
        front = -line
        return (front * math.cos(math.radians(55)) + side * math.sin(math.radians(55)) + UP * 0.35).normalized()
    if kind == "high":
        return (side * 0.8 - line * 0.3 + UP * 0.9).normalized()
    if kind == "shoulder":            # behind the target, looking at her: what she shoots comes at the camera
        return (line + side * 0.45 + UP * 0.35).normalized()
    if kind in ("eggs", "needles"):
        return Vector((0.6, -1.0, 0.32)).normalized()
    return Vector((0.6, -1.0, 0.45)).normalized()


def fit(name, points, towards, lens=LENS):
    """A camera looking along -towards at the points' middle, as near as keeps them all in frame."""
    cam = queen_stage.camera(name, (0, 0, 0), (0, -1, 0), lens)
    centre = sum(points, Vector()) / len(points)
    turn = (-towards).to_track_quat('-Z', 'Y')
    right, up = turn @ Vector((1, 0, 0)), turn @ Vector((0, 1, 0))
    half_w = cam.data.sensor_width / 2 / lens
    half_h = half_w * RESOLUTION[1] / RESOLUTION[0]
    reach = max(p.dot(towards) - centre.dot(towards) + max(abs((p - centre).dot(right)) / half_w,
                                                            abs((p - centre).dot(up)) / half_h) for p in points)
    cam.location = centre + towards * reach * MARGIN
    cam.rotation_euler = turn.to_euler()
    return cam


def cameras(shots, keys, end):
    """shots: (kind, caption, first frame, last frame, points, queen, target) runs; a camera, a marker and a caption
    each."""
    scene, frames = bpy.context.scene, range(1, end + 1)
    for i, (kind, caption, first, last, points, queen, target) in enumerate(shots):
        cam = fit(f"{i:02d} {kind}", points, direction(kind, queen, target), 50.0 if kind == "shoulder" else LENS)
        marker = scene.timeline_markers.new(caption, frame=first)
        marker.camera = cam
        queen_stage.caption(cam, caption, first, last, keys, frames)
        if i == 0:
            scene.camera = cam


def glow(body, regions_png):
    """Emission on the albedo where the regions mask is the glow's or the eyes' colour."""
    mat = body.material_slots[0].material
    tree = mat.node_tree
    bsdf = next(n for n in tree.nodes if n.type == 'BSDF_PRINCIPLED')
    albedo = next(n for n in tree.nodes if n.type == 'TEX_IMAGE' and 'albedo' in n.image.name)
    mask = tree.nodes.new('ShaderNodeTexImage')
    mask.image = bpy.data.images.load(regions_png, check_existing=True)
    mask.image.colorspace_settings.name, mask.interpolation = 'Non-Color', 'Closest'
    strength = None
    for colour, power in GLOW.items():
        near = tree.nodes.new('ShaderNodeVectorMath')
        near.operation, near.inputs[1].default_value = 'DISTANCE', colour
        tree.links.new(mask.outputs['Color'], near.inputs[0])
        step = tree.nodes.new('ShaderNodeMath')
        step.operation, step.inputs[1].default_value = 'LESS_THAN', 0.12
        tree.links.new(near.outputs['Value'], step.inputs[0])
        weighted = tree.nodes.new('ShaderNodeMath')
        weighted.operation, weighted.inputs[1].default_value = 'MULTIPLY', power
        tree.links.new(step.outputs['Value'], weighted.inputs[0])
        strength = _add(tree, strength, weighted.outputs['Value'])
    tree.links.new(albedo.outputs['Color'], bsdf.inputs['Emission Color'])
    tree.links.new(strength, bsdf.inputs['Emission Strength'])


def _add(tree, a, b):
    if a is None:
        return b
    total = tree.nodes.new('ShaderNodeMath')
    total.operation = 'ADD'
    tree.links.new(a, total.inputs[0])
    tree.links.new(b, total.inputs[1])
    return total.outputs['Value']


def settings(end, fps):
    s = bpy.context.scene
    try:
        s.render.engine = 'BLENDER_EEVEE_NEXT'
    except TypeError:
        s.render.engine = 'BLENDER_EEVEE'
    s.render.resolution_x, s.render.resolution_y, s.render.resolution_percentage = *RESOLUTION, 100
    s.render.fps, s.frame_start, s.frame_end, s.frame_current = fps, 1, end, 1
    s.render.use_motion_blur, s.render.motion_blur_shutter = True, 0.5
    s.eevee.taa_render_samples = 24
    s.view_settings.view_transform = 'Standard'
    for mat in bpy.data.materials:
        for node in (mat.node_tree.nodes if mat.node_tree else []):
            if node.bl_idname == 'ShaderNodeTexImage':
                node.interpolation = 'Closest'


def stills(shots, folder, at=(0.3, 0.6, 0.9)):
    """Frames at these fractions through each shot."""
    os.makedirs(folder, exist_ok=True)
    s = bpy.context.scene
    for kind, caption, first, last, *_ in shots:
        s.camera = next(m.camera for m in s.timeline_markers if m.frame == first)
        for fraction in at:
            frame = round(first + (last - first) * fraction)
            s.frame_set(frame)
            s.render.filepath = os.path.join(folder, f"{frame:04d}.png")
            bpy.ops.render.render(write_still=True)


def render(frames, video):
    """Every frame to PNGs, then the frames and the sound strips into an MP4 through the sequencer."""
    s = bpy.context.scene
    os.makedirs(frames, exist_ok=True)
    s.render.image_settings.file_format = 'PNG'
    s.render.filepath = os.path.join(frames, "")
    bpy.ops.render.render(animation=True)
    editor = s.sequence_editor_create()
    strips = editor.strips if hasattr(editor, "strips") else editor.sequences
    names = sorted(f for f in os.listdir(frames) if f.endswith(".png"))
    image = strips.new_image("frames", os.path.join(frames, names[0]), 30, 1)
    for name in names[1:]:
        image.elements.append(name)
    settings = s.render.image_settings
    settings.media_type, settings.file_format = 'VIDEO', 'FFMPEG'
    s.render.ffmpeg.format, s.render.ffmpeg.codec = 'MPEG4', 'H264'
    s.render.ffmpeg.constant_rate_factor, s.render.ffmpeg.audio_codec = 'HIGH', 'AAC'
    s.render.use_sequencer, s.render.filepath = True, video
    bpy.ops.render.render(animation=True)
    print(f"WORKSHOP video: {video}", flush=True)
