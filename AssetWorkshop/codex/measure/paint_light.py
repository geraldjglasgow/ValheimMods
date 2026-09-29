"""How much light the game's painters put into an albedo, measured against its normal map and its mesh.

- Against the normal map (tangent space, x along u, y along v): the painted value regressed on the normal's x and y
  gives the direction and strength of light painted in texture space; the correlation with the normal field's
  curvature says whether bumps are lit and hollows dark; the correlation with the height the normals integrate to says
  whether raised parts are painted lighter.
- Against the mesh (static meshes, in the prefab's axes): the value of texels on faces looking up against those
  looking down, which is light from above baked into the paint.
- How much the normal map carries: how far its normals tilt, and how much of its detail is also in the albedo.
"""
import numpy as np
from PIL import Image, ImageDraw

import game
import paint_stats
import paint_uv


def normal_relation(value, normal, mask):
    """Painted light against the normal map, over the masked texels."""
    nx, ny, nz = normal[..., 0], normal[..., 1], normal[..., 2]
    tilt = np.degrees(np.arccos(np.clip(nz, -1.0, 1.0)))
    curvature = _curvature(nx, ny)
    height = _height(nx, ny)
    slope_x, slope_y = _fit_light(value, nx, ny, mask)
    fine_albedo = value - paint_stats.blur(value, mask, 1.5)
    fine_normal = tilt - paint_stats.blur(tilt, mask, 1.5)
    corr, strength = _from_albedo(value, nx, ny, mask)
    return {"from_albedo_corr": corr, "from_albedo_strength": strength,
            "tilt_mean_deg": round(float(tilt[mask].mean()), 1),
            "tilt_p90_deg": round(float(np.percentile(tilt[mask], 90)), 1),
            "flat_fraction": round(float((tilt[mask] < 5.0).mean()), 3),
            "light_direction_deg": round(float(np.degrees(np.arctan2(slope_y, slope_x))), 0),
            "light_strength": round(float(np.hypot(slope_x, slope_y)), 3),
            "value_vs_curvature": _corr(value, curvature, mask),
            "value_vs_height": _corr(value, height, mask),
            "detail_shared": _corr(np.abs(fine_albedo), np.abs(fine_normal), mask)}


def _from_albedo(value, nx, ny, mask):
    """How well the normal map is the albedo's own relief: the correlation of the normal's x and y with minus the
    value's slope along u and v (bright read as raised), and the normal tilt per unit of value change per texel."""
    gx = (np.roll(value, -1, axis=1) - np.roll(value, 1, axis=1)) / 2
    gy = -(np.roll(value, -1, axis=0) - np.roll(value, 1, axis=0)) / 2
    inner = mask & np.roll(mask, 1, 0) & np.roll(mask, -1, 0) & np.roll(mask, 1, 1) & np.roll(mask, -1, 1)
    if inner.sum() < 16 or gx[inner].std() < 1e-6:
        return None, None
    slopes = np.concatenate([-gx[inner], -gy[inner]])
    tilts = np.concatenate([nx[inner], ny[inner]])
    corr = float(np.corrcoef(slopes, tilts)[0, 1])
    strength = float(np.dot(slopes, tilts) / max(np.dot(slopes, slopes), 1e-12))
    return round(corr, 3), round(strength, 2)


def _curvature(nx, ny):
    """Divergence of the normals' tilt: positive on bumps and ridges, negative in hollows and cracks."""
    slope_x = (np.roll(nx, -1, axis=1) - np.roll(nx, 1, axis=1)) / 2
    return slope_x - (np.roll(ny, -1, axis=0) - np.roll(ny, 1, axis=0)) / 2


def _height(nx, ny):
    """The height field the normals integrate to (Frankot-Chellappa), rows running down the texture."""
    nz = np.sqrt(np.clip(1.0 - nx ** 2 - ny ** 2, 0.05, 1.0))
    p, q = -nx / nz, ny / nz            # dh/dx along columns, dh/dy along rows (v runs up, rows run down)
    fy = np.fft.fftfreq(nx.shape[0])[:, None] * 2 * np.pi
    fx = np.fft.fftfreq(nx.shape[1])[None, :] * 2 * np.pi
    denominator = fx ** 2 + fy ** 2
    denominator[0, 0] = 1.0
    spectrum = (-1j * fx * np.fft.fft2(p) - 1j * fy * np.fft.fft2(q)) / denominator
    spectrum[0, 0] = 0.0
    return np.real(np.fft.ifft2(spectrum))


def _fit_light(value, nx, ny, mask):
    """Least squares value = a + b*nx + c*ny over the mask: (b, c), the painted light's gradient in texture space."""
    rows = np.stack([np.ones(mask.sum()), nx[mask], ny[mask]], axis=1)
    solution, *_ = np.linalg.lstsq(rows, value[mask], rcond=None)
    return float(solution[1]), float(solution[2])


def _corr(a, b, mask):
    x, y = a[mask], b[mask]
    if x.std() < 1e-9 or y.std() < 1e-9:
        return None
    return round(float(np.corrcoef(x, y)[0, 1]), 3)


def facing(texture, uses, size, value, mask):
    """Light from above in the paint: the median value of texels on faces looking up (normal y > 0.5), down
    (< -0.5) and sideways, over static meshes of pieces and world objects (their prefab's up is the world's)."""
    up = _up_map(texture, uses, size)
    if up is None:
        return None
    groups = {"up": up > 0.5, "side": np.abs(up) <= 0.5, "down": up < -0.5}
    found = {name: round(float(np.median(value[mask & g])), 3) if (mask & g).sum() >= 20 else None
             for name, g in groups.items()}
    found["value_vs_up"] = _corr(value, np.nan_to_num(up), mask & ~np.isnan(up))
    return found


def _up_map(texture, uses, size):
    """(h, w) of the world-up component of the face normal under each texel; nan where no static face lies."""
    width, height = size
    canvas = Image.new("F", (width, height), float("nan"))
    draw = ImageDraw.Draw(canvas)
    drawn = 0
    for use in uses:
        if not set(use["kind"]) & {"piece", "env", "location", "creature"}:
            continue
        drawn += _draw_up(draw, use, width, height)
    return np.asarray(canvas) if drawn else None


def _draw_up(draw, use, width, height):
    """Each face of one static use drawn into UV space with its normal's world y as the value."""
    positions, uv, tris = paint_uv.submesh(use["mesh"], use["submesh"])
    rotation = _rotation(use)
    if uv is None or rotation is None or not len(tris):
        return 0
    edges = positions[tris[:, 1:]] - positions[tris[:, :1]]
    normals = np.cross(edges[:, 0], edges[:, 1]) @ rotation.T
    up = normals[:, 1] / np.maximum(np.linalg.norm(normals, axis=1), 1e-12)
    for triangle, y in zip(uv[tris], up):
        local = triangle - np.floor(triangle.min(axis=0))
        draw.polygon([(u * width, (1.0 - v) * height) for u, v in local], fill=float(y))
    return 1


def _rotation(use):
    """The renderer's rotation to the prefab root (scale left out), found by its mesh in the use's prefab."""
    prefab = game.prefab(use["prefab"])
    for node, kind, body in prefab.all_components():
        if kind in ("MeshRenderer", "SkinnedMeshRenderer") and game.renderer_mesh(prefab, node, body) == use["mesh"]:
            return _world_rotation(prefab, node)
    return None


def _world_rotation(prefab, node):
    """3x3 rotation from node space to the prefab root, the product of the local rotations up the chain."""
    matrix = np.eye(3)
    while node and prefab.is_local(node):
        body = prefab.docs[node][1]
        x, y, z, w = game.unity.numbers(game.unity.field(body, "m_LocalRotation")) or (0, 0, 0, 1)
        local = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                          [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                          [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
        matrix = local @ matrix
        node = game.unity.ref(game.unity.field(body, "m_Father"))[0]
    return matrix
