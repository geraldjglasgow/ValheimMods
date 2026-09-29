"""Mesh .asset vertex and index data as numpy arrays, and texel density, for the codex's measurements (game.py
re-exports these)."""
import functools

from workshop import unity

from game_text import _all, _hex, _nested, _vertex_count


@functools.lru_cache(maxsize=None)
def mesh_arrays(path):
    """(positions (n, 3) in Unity axes, uv0 (n, 2) or None, triangles (m, 3)) of a Mesh .asset, as numpy arrays."""
    import numpy as np
    text = unity.read(path)
    count, raw = int(_vertex_count(text)), _hex(text, "_typelessdata")
    block = text[text.index("m_Channels:"):text.index("m_DataSize:")]
    channels = [(int(s), int(o), int(f), int(d) & 0xF) for s, o, f, d in
                _all(block, r"stream: (\d+)\s+offset: (\d+)\s+format: (\d+)\s+dimension: (\d+)")]
    starts, strides = _streams(channels, count)
    positions = _channel(raw, channels[0], starts, strides, count)
    uv = _channel(raw, channels[4], starts, strides, count) if channels[4][3] else None
    index = _hex(text, "m_IndexBuffer")
    wide = _nested(text, "m_IndexFormat") == "1"
    triangles = np.frombuffer(index, dtype=np.uint32 if wide else np.uint16).astype(np.int64)
    return positions, uv, triangles[: len(triangles) // 3 * 3].reshape(-1, 3)


def texel_density(mesh_path, texture_px, scale=1.0):
    """Texture pixels per metre of surface on a mesh with a square texture of texture_px: sqrt(uv area * px^2 /
    world area) over every triangle. scale is the mesh's size in the game against its own units: the renderer's
    Prefab.world_scale for a static mesh (the Battleaxe's mesh is 5 m long, scaled down), the armature's scale times
    the prefab's for a skinned one (vertices in centimetres under an armature at 100). None when there are no uvs."""
    import numpy as np
    positions, uv, tris = mesh_arrays(mesh_path)
    if uv is None or not len(tris):
        return None
    world = np.linalg.norm(np.cross(positions[tris[:, 1]] - positions[tris[:, 0]],
                                    positions[tris[:, 2]] - positions[tris[:, 0]]), axis=1).sum() / 2
    a, b = uv[tris[:, 1]] - uv[tris[:, 0]], uv[tris[:, 2]] - uv[tris[:, 0]]
    area = np.abs(a[:, 0] * b[:, 1] - a[:, 1] * b[:, 0]).sum() / 2
    return float(texture_px * np.sqrt(area / world) / scale) if world > 0 else None


_FORMATS = {0: ("<f4", 4), 1: ("<f2", 2), 2: ("u1", 1), 3: ("i1", 1), 4: ("<u2", 2), 5: ("<i2", 2)}


def _channel(raw, channel, starts, strides, count):
    """One vertex attribute (stream, offset, format, dimension) as an (n, dimension) float array."""
    import numpy as np
    stream, offset, form, dimension = channel
    dtype, size = _FORMATS.get(form, ("<f4", 4))
    rows = np.frombuffer(raw, dtype=np.uint8, count=strides[stream] * count, offset=starts[stream])
    rows = rows.reshape(count, strides[stream])[:, offset:offset + size * dimension]
    return np.ascontiguousarray(rows).view(dtype).reshape(count, dimension).astype(np.float64)


def _streams(channels, count):
    """Byte offset and stride of every vertex stream (each starts on a 16-byte boundary), as reference.py reads them."""
    strides = {}
    for stream, offset, form, dimension in channels:
        if dimension:
            strides[stream] = max(strides.get(stream, 0), offset + dimension * _FORMATS.get(form, ("", 4))[1])
    starts, at = {}, 0
    for stream in sorted(strides):
        starts[stream] = at
        at = (at + strides[stream] * count + 15) // 16 * 16
    return starts, strides
