"""The player body's uv layout, the canvas every chest and leg armour texture is painted on (the Player shader lays
_ChestTex and _LegsTex over the skin where their alpha is opaque). Each body vertex is given the region of the bone
that moves it most (torso, upper arm, hand, thigh ...); the region's uv box says where on the texture it is painted.

    items_body.regions()                   # {region: {"box_px": [x0, y0, x1, y1] on a 256 px texture, ...}}
    items_body.coverage(texture_path)      # which regions an armour texture paints (share of opaque pixels)
    items_body.overlay(texture_path, out)  # the texture scaled up with the regions' uv triangles drawn over it
"""
import functools
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import game
from workshop import unity

PLAYER = "Characters/Player/Player.prefab"
BODY = "Characters/Player/model/body.asset"
REGIONS = {"head": ("Neck", "Head", "Jaw"), "torso": ("Spine", "Spine1", "Spine2"),
           "shoulder": ("LeftShoulder", "RightShoulder"), "upper arm": ("LeftArm", "RightArm"),
           "forearm": ("LeftForeArm", "RightForeArm"), "hand": ("LeftHand", "RightHand"), "hips": ("Hips",),
           "thigh": ("LeftUpLeg", "RightUpLeg"), "shin": ("LeftLeg", "RightLeg"),
           "foot": ("LeftFoot", "RightFoot", "LeftToeBase", "RightToeBase")}
COLOURS = {"head": (255, 255, 255), "torso": (255, 60, 60), "shoulder": (255, 160, 0), "upper arm": (255, 240, 0),
           "forearm": (120, 255, 0), "hand": (0, 255, 200), "hips": (0, 160, 255), "thigh": (120, 80, 255),
           "shin": (255, 0, 255), "foot": (255, 128, 190)}
TEXTURE = 256


@functools.lru_cache(maxsize=None)
def layout():
    """(uv (n, 2), triangles of the body submesh, region name per vertex)."""
    _, uv, tris = game.mesh_arrays(BODY)
    text = unity.read(BODY)
    first = int(game._all(text, r"indexCount: (\d+)")[0]) // 3
    names = bone_names()
    dominant = _dominant_bones(text, len(uv))
    lookup = {bone: region for region, bones in REGIONS.items() for bone in bones}
    region = [lookup.get(names[b] if b < len(names) else "", "hand") for b in dominant]
    return uv, tris[:first], region


def bone_names():
    """The body renderer's bones in order (the order the mesh's bone indices count in), by GameObject name; finger
    bones are named as their hand."""
    prefab = game.prefab(PLAYER)
    node = prefab.find_node("body")
    body = next(b for kind, b in prefab.components(node) if kind == "SkinnedMeshRenderer")
    names = [prefab.name(unity.ref(item)[0]) if prefab.is_local(unity.ref(item)[0]) else "" for item in
             unity.items(body, "m_Bones")]
    return [("LeftHand" if n.startswith("LeftHand") else "RightHand" if n.startswith("RightHand") else n)
            for n in names]


def _dominant_bones(text, count):
    """The index of the bone with the largest weight on each vertex (blend weights: channel 12, float; blend
    indices: channel 13, 32-bit)."""
    raw = game._hex(text, "_typelessdata")
    block = text[text.index("m_Channels:"):text.index("m_DataSize:")]
    channels = [(int(s), int(o), int(f), int(d) & 0xF) for s, o, f, d in
                game._all(block, r"stream: (\d+)\s+offset: (\d+)\s+format: (\d+)\s+dimension: (\d+)")]
    starts, strides = game._streams(channels, count)
    weights = game._channel(raw, channels[12], starts, strides, count)
    stream, offset, _, dimension = channels[13]
    rows = np.frombuffer(raw, dtype=np.uint8, count=strides[stream] * count, offset=starts[stream])
    rows = rows.reshape(count, strides[stream])[:, offset:offset + 4 * dimension]
    indices = np.ascontiguousarray(rows).view("<u4").reshape(count, dimension)
    return indices[np.arange(count), np.argmax(weights, axis=1)]


def regions():
    """Each region's uv box in pixels of a 256 px texture (x right, y down from the top) on the island that holds
    most of it, its share of the body's texture area and its triangle count."""
    uv, tris, region = layout()
    island = islands(tris)
    found = {}
    for name in REGIONS:
        mine = [n for n, t in enumerate(tris) if region[t[0]] == name]
        if not mine:
            continue
        main = max(set(island[mine]), key=lambda i: int((island[mine] == i).sum()))
        points = uv[tris[[n for n in mine if island[n] == main]].ravel()]
        box = [points[:, 0].min(), 1 - points[:, 1].max(), points[:, 0].max(), 1 - points[:, 1].min()]
        found[name] = {"box_px": [round(float(v) * TEXTURE) for v in box], "triangles": len(mine),
                       "uv_area_share": round(_area(uv, tris[mine]) / _area(uv, tris), 3)}
    return found


def islands(tris):
    """The uv island of every triangle: triangles sharing a vertex index belong together (a uv seam splits
    vertices, so indices only join within an island)."""
    parent = list(range(int(tris.max()) + 1))

    def root(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    for a, b, c in tris:
        parent[root(b)] = root(a)
        parent[root(c)] = root(a)
    return np.array([root(int(t[0])) for t in tris])


def _area(uv, tris):
    t = np.asarray(tris)
    a, b = uv[t[:, 1]] - uv[t[:, 0]], uv[t[:, 2]] - uv[t[:, 0]]
    return float(np.abs(a[:, 0] * b[:, 1] - a[:, 1] * b[:, 0]).sum() / 2)


def mask(size, name=None):
    """A size x size mask of the body's uv triangles (one region's when named)."""
    uv, tris, region = layout()
    image = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(image)
    for a, b, c in tris:
        if name is None or region[a] == name:
            draw.polygon([(uv[i][0] * size, (1 - uv[i][1]) * size) for i in (a, b, c)], fill=255)
    return np.asarray(image) > 0


def coverage(texture_path):
    """{region: share of its texture area the texture paints opaque (alpha over half)}."""
    with Image.open(os.path.join(game.ROOT, texture_path)) as image:
        alpha = np.asarray(image.convert("RGBA"))[..., 3] > 127
    size = alpha.shape[0]
    found = {}
    for name in REGIONS:
        inside = mask(size, name)
        if inside.any():
            found[name] = round(float(alpha[inside].mean()), 2)
    return found


def overlay(texture_path, out, scale=None):
    """The texture scaled up nearest-neighbour, every body triangle outlined in its region's colour, labelled."""
    uv, tris, region = layout()
    with Image.open(os.path.join(game.ROOT, texture_path)) as image:
        size = image.width * (scale or max(1, 1024 // image.width))
        big = image.convert("RGB").resize((size, size), Image.NEAREST)
    draw = ImageDraw.Draw(big)
    for a, b, c in tris:
        draw.polygon([(uv[i][0] * size, (1 - uv[i][1]) * size) for i in (a, b, c)], outline=COLOURS[region[a]])
    font = ImageFont.load_default()
    for name, info in regions().items():
        x0, y0, _, _ = info["box_px"]
        draw.text((x0 * size / TEXTURE + 2, y0 * size / TEXTURE + 2), name, fill=COLOURS[name], font=font)
    draw.text((4, size - 14), os.path.basename(texture_path), fill=(230, 230, 230), font=font)
    big.save(out)
