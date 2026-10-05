# Blender side of preview.py: blender -b --factory-startup -P preview_blender.py -- <name>-preview.json
# Builds one mesh per colour group, a ground grid (dug squares from the site's terrain shown as water), then renders
# each requested view with the Workbench engine into <out>-<view>.png. The named views frame the whole build however
# tall or wide it is (a camera far enough back that its bounding sphere fills the frame); "upper" frames its top
# third, "cut" shows the cut-away model (east half and roofs left out) when preview.py sent one.
import json, math, sys
import bpy
from mathutils import Vector

data = json.load(open(sys.argv[sys.argv.index("--") + 1]))


def material(name, rgb):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*rgb, 1)
    return m


def add_mesh(name, verts, faces, mat):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    ob.data.materials.append(mat)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def bounds(groups=None, z_from=None):
    xs, ys, zs = [], [], []
    for g in (groups or data["groups"]).values():
        for x, y, z in g["v"]:
            if z_from is None or z >= z_from:
                xs.append(x); ys.append(y); zs.append(z)
    return (min(xs), max(xs)), (min(ys), max(ys)), (min(zs), max(zs))


def fitted(name, direction, b):
    """A camera looking along -direction at the centre of bounds b, far enough back to hold their bounding sphere
    (30 mm lens, 16:10: the narrower, vertical field of view is 41 degrees)."""
    (x0, x1), (y0, y1), (z0, z1) = b
    c = Vector(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2))
    r = max(Vector((x1 - x0, y1 - y0, z1 - z0)).length / 2, 4)
    return camera(name, c + Vector(direction).normalized() * r / math.sin(math.radians(20.5)) * 1.03, c)


def dug(site):
    """Squares the site lowers below the ground, as (x0, x1, y0, y1) in Blender X/Y."""
    return [(s["at"][0] - s["half"], s["at"][0] + s["half"], s["at"][1] - s["half"], s["at"][1] + s["half"])
            for s in site.get("terrain", []) if s["op"] == "level" and s.get("y", 0) < 0]


def ground(bx, by):
    holes = dug(data["site"])
    v, f, wv, wf = [], [], [], []
    m = 20
    for i in range(int(bx[0] - m), int(bx[1] + m)):
        for j in range(int(by[0] - m), int(by[1] + m)):
            cx, cy = i + 0.5, j + 0.5
            wet = any(h[0] <= cx <= h[1] and h[2] <= cy <= h[3] for h in holes)
            tv, tf, z = (wv, wf, -1.5) if wet else (v, f, -0.02)
            b = len(tv)
            tv += [(i, j, z), (i + 1, j, z), (i + 1, j + 1, z), (i, j + 1, z)]
            tf.append((b, b + 1, b + 2, b + 3))
    add_mesh("ground", v, f, material("grass", (0.33, 0.45, 0.22)))
    if wv:
        add_mesh("water", wv, wf, material("water", (0.20, 0.35, 0.50)))


def camera(name, loc, target, ortho=None):
    cam = bpy.data.cameras.new(name)
    ob = bpy.data.objects.new(name, cam)
    bpy.context.scene.collection.objects.link(ob)
    ob.location = loc
    ob.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    if ortho:
        cam.type, cam.ortho_scale = "ORTHO", ortho
    else:
        cam.lens = 30
    cam.clip_end = 2000
    return ob


def views(bx, by, bz):
    b, c = (bx, by, bz), Vector(((bx[0] + bx[1]) / 2, (by[0] + by[1]) / 2, 0))
    upper = bounds(z_from=bz[0] + 0.65 * (bz[1] - bz[0]))
    cams = {
        "oblique": fitted("oblique", (-0.55, -1.25, 0.95), b),
        "top": camera("top", c + Vector((0, 0, 3 * max(bx[1] - bx[0], by[1] - by[0], 12) + bz[1])), c + Vector((0, 0.001, 0)),
                      ortho=max(bx[1] - bx[0], (by[1] - by[0]) * 1.6) * 1.08),     # fits both ways at 16:10
        "upper": fitted("upper", (0.8, -0.9, 0.75), upper),
    }
    for name, d in (("front", (0, -1, 0.25)), ("back", (0, 1, 0.25)), ("east", (1, 0, 0.25)), ("west", (-1, 0, 0.25))):
        cams[name] = fitted(name, d, b)
    if data.get("cut"):
        cams["cut"] = fitted("cut", (1, -0.35, 0.3), b)
    return cams


def close_up(spec):
    """cam=x,y,z:tx,ty,tz in blueprint axes (y up): a camera at the first point looking at the second."""
    a, b = spec[4:].split(":")
    (x, y, z), (tx, ty, tz) = [float(v) for v in a.split(",")], [float(v) for v in b.split(",")]
    return camera(spec, Vector((x, z, y)), Vector((tx, tz, ty)))


def setup():
    sc = bpy.context.scene
    for ob in list(sc.objects):
        bpy.data.objects.remove(ob)
    sc.render.engine = "BLENDER_WORKBENCH"
    sh = sc.display.shading
    sh.light, sh.color_type = "STUDIO", "MATERIAL"
    sh.show_shadows, sh.show_cavity = True, True
    sc.world = sc.world or bpy.data.worlds.new("w")
    sc.world.color = (0.55, 0.68, 0.80)
    sc.render.resolution_x, sc.render.resolution_y = 1600, 1000


def main():
    setup()
    models = {"full": [], "cut": []}
    for model, groups in (("full", data["groups"]), ("cut", data.get("cut") or {})):
        for name, g in groups.items():
            ob = add_mesh(f"{model}-{name}", [tuple(v) for v in g["v"]], [tuple(f) for f in g["f"]],
                          material(name, data["colours"][name]))
            models[model].append(ob)
    bx, by, bz = bounds()
    ground(bx, by)
    cams = views(bx, by, bz)
    for i, view in enumerate(data["views"]):
        for model, obs in models.items():
            for ob in obs:
                ob.hide_render = (model == "cut") != (view == "cut")
        bpy.context.scene.camera = cams[view] if view in cams else close_up(view)
        label = view if view in cams else f"cam{i}"
        bpy.context.scene.render.filepath = f"{data['out']}-{label}.png"
        bpy.ops.render.render(write_still=True)
        print(f"preview {bpy.context.scene.render.filepath}")


main()
