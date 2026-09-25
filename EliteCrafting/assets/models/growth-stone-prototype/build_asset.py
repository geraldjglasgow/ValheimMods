"""Original review asset; Python standard library only. Run beside preview.template.html."""
import base64
import json
import math
from pathlib import Path
import random
import struct
import zlib

ROOT = Path(__file__).resolve().parent
W, H = 128, 64


def png(path, pixels):
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data))
    raw = b''.join(b'\0' + bytes(pixels[y * W * 3:(y + 1) * W * 3]) for y in range(H))
    data = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>2I5B', W, H, 8, 2, 0, 0, 0))
    path.write_bytes(data + chunk(b'IDAT', zlib.compress(raw)) + chunk(b'IEND', b''))


def distance(x, y, segment):
    ax, ay, bx, by = segment
    dx, dy = bx - ax, by - ay
    t = max(0, min(1, ((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy)))
    return math.hypot(x - ax - t * dx, y - ay - t * dy)


def textures():
    rng = random.Random(81073)
    glyph = [(32, 19, 32, 45), (32, 29, 22, 22), (32, 29, 42, 22),
             (32, 38, 24, 32), (32, 38, 40, 32), (29, 17, 32, 13),
             (32, 13, 35, 17), (35, 17, 32, 21), (32, 21, 29, 17)]
    cracks = [(12, 8, 19, 16), (19, 16, 17, 22), (52, 45, 44, 42),
              (44, 42, 43, 49), (15, 55, 20, 48), (48, 6, 43, 12)]
    base, emit = [], []
    for y in range(H):
        for x in range(W):
            px = x % 64
            grain = rng.choice([-10, -6, -3, 0, 0, 3, 6, 10])
            strata = round(5 * math.sin(px * .19 + y * .38) + 3 * math.cos(px * .42 - y * .12))
            value = 66 + grain + strata + (6 if y < 25 else 0)
            color, glow = [value - 8, value, value + 2], [0, 0, 0]
            if min(distance(px, y, s) for s in cracks) < .55:
                color = [27, 34, 35]
            d = min(distance(px, y, s) for s in glyph) if x < 64 else 100
            if d < 2.1:
                color = [24, 32, 32]
            if d < .8:
                color, glow = [48, 150 + grain, 137 + grain], [38, 155 + grain, 140 + grain]
            base.extend(max(0, min(255, c)) for c in color)
            emit.extend(glow)
    png(ROOT / 'growth-stone-albedo.png', base)
    png(ROOT / 'growth-stone-emission.png', emit)


def cross(a, b):
    return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]


def triangle(points, front):
    a, b, c = points
    n = cross([b[i] - a[i] for i in range(3)], [c[i] - a[i] for i in range(3)])
    center = [sum(p[i] for p in points) / 3 for i in range(3)]
    if sum(n[i] * center[i] for i in range(3)) < 0:
        points = [a, c, b]
        n = [-v for v in n]
    length = math.sqrt(sum(v * v for v in n))
    normal = [v / length for v in n]
    uv = [[(.5 if not front else 0) + (p[0] / .2 + .5) * .5, .5 - p[1] / .26] for p in points]
    return {'p': points, 'n': [normal] * 3, 'uv': uv}


def mesh():
    outline = [(.62, .92), (-.06, 1), (-.65, .80), (-.88, .40), (-1, -.09),
               (-.79, -.62), (-.33, -.96), (.27, -1), (.77, -.67), (.98, -.21),
               (.90, .33), (.78, .68)]
    rings = []
    for scale, depth in [(.76, .032), (.97, .016), (1, -.009), (.77, -.033)]:
        rings.append([(x * .095 * scale, y * .118 * scale, depth) for x, y in outline])
    result = []
    for r in range(3):
        for i in range(12):
            j = (i + 1) % 12
            result.append(triangle([rings[r][i], rings[r][j], rings[r + 1][i]], r == 0))
            result.append(triangle([rings[r][j], rings[r + 1][j], rings[r + 1][i]], r == 0))
    for i in range(12):
        result.append(triangle([(0, .002, .036), rings[0][i], rings[0][(i + 1) % 12]], True))
        result.append(triangle([(-.004, 0, -.038), rings[3][i], rings[3][(i + 1) % 12]], False))
    return result


def flatten(faces, field):
    return [v for face in faces for vertex in face[field] for v in vertex]


def obj(faces):
    lines = ['# Original EliteCrafting prototype. Metres; Y up; +Z front.', 'mtllib growth-stone.mtl', 'o ECF_GrowthLesser_Prototype']
    for field, prefix in [('p', 'v'), ('uv', 'vt'), ('n', 'vn')]:
        for face in faces:
            for vertex in face[field]:
                values = [vertex[0], 1 - vertex[1]] if field == 'uv' else vertex
                lines.append(prefix + ' ' + ' '.join(f'{v:.7f}' for v in values))
    lines += ['usemtl GrowthStone', 's off']
    for i in range(0, len(faces) * 3, 3):
        lines.append('f ' + ' '.join(f'{j}/{j}/{j}' for j in range(i + 1, i + 4)))
    (ROOT / 'growth-stone.obj').write_text('\n'.join(lines) + '\n')
    (ROOT / 'growth-stone.mtl').write_text('newmtl GrowthStone\nKd 1 1 1\nKs 0 0 0\nNs 1\nmap_Kd growth-stone-albedo.png\nmap_Ke growth-stone-emission.png\n')


def glb(faces):
    binary, views, accessors = bytearray(), [], []
    for field, kind, size in [('p', 'VEC3', 3), ('n', 'VEC3', 3), ('uv', 'VEC2', 2)]:
        values = flatten(faces, field)
        data = struct.pack('<' + 'f' * len(values), *values)
        views.append({'buffer': 0, 'byteOffset': len(binary), 'byteLength': len(data), 'target': 34962})
        binary.extend(data)
        accessor = {'bufferView': len(views) - 1, 'componentType': 5126, 'count': len(values) // size, 'type': kind}
        if field == 'p':
            accessor.update(min=[min(values[i::3]) for i in range(3)], max=[max(values[i::3]) for i in range(3)])
        accessors.append(accessor)
    for name in ['albedo', 'emission']:
        while len(binary) % 4:
            binary.append(0)
        data = (ROOT / f'growth-stone-{name}.png').read_bytes()
        views.append({'buffer': 0, 'byteOffset': len(binary), 'byteLength': len(data)})
        binary.extend(data)
    doc = gltf_document(views, accessors, len(binary))
    js = json.dumps(doc, separators=(',', ':')).encode()
    js += b' ' * (-len(js) % 4)
    binary.extend(b'\0' * (-len(binary) % 4))
    body = struct.pack('<I4s', len(js), b'JSON') + js + struct.pack('<I4s', len(binary), b'BIN\0') + binary
    (ROOT / 'growth-stone.glb').write_bytes(struct.pack('<4sII', b'glTF', 2, len(body) + 12) + body)


def gltf_document(views, accessors, length):
    return {
        'asset': {'version': '2.0', 'generator': 'EliteCrafting original currency prototype'},
        'scene': 0, 'scenes': [{'nodes': [0]}], 'nodes': [{'mesh': 0, 'name': 'ECF_GrowthLesser_Prototype'}],
        'meshes': [{'primitives': [{'attributes': {'POSITION': 0, 'NORMAL': 1, 'TEXCOORD_0': 2}, 'material': 0}]}],
        'materials': [{'name': 'GrowthStone', 'pbrMetallicRoughness': {
            'baseColorTexture': {'index': 0}, 'metallicFactor': 0, 'roughnessFactor': .94},
            'emissiveTexture': {'index': 1}, 'emissiveFactor': [1, 1, 1]}],
        'textures': [{'source': 0, 'sampler': 0}, {'source': 1, 'sampler': 0}],
        'samplers': [{'magFilter': 9728, 'minFilter': 9728, 'wrapS': 33071, 'wrapT': 33071}],
        'images': [{'bufferView': i, 'mimeType': 'image/png'} for i in [3, 4]],
        'buffers': [{'byteLength': length}], 'bufferViews': views, 'accessors': accessors,
    }


def preview(faces):
    data = {key: flatten(faces, key) for key in ['p', 'n', 'uv']}
    for kind in ['albedo', 'emission']:
        data[kind] = 'data:image/png;base64,' + base64.b64encode((ROOT / f'growth-stone-{kind}.png').read_bytes()).decode()
    template = (ROOT / 'preview.template.html').read_text()
    (ROOT / 'preview.html').write_text(template.replace('__ASSET_DATA__', json.dumps(data)))


def validate(faces):
    edges = {}
    for face in faces:
        for i in range(3):
            edge = tuple(sorted((tuple(face['p'][i]), tuple(face['p'][(i + 1) % 3]))))
            edges[edge] = edges.get(edge, 0) + 1
    assert all(count == 2 for count in edges.values()), 'Mesh must be closed.'
    assert all(0 <= v <= 1 for v in flatten(faces, 'uv')), 'UV outside atlas.'
    vertices = {tuple(p) for face in faces for p in face['p']}
    assert len(vertices) - len(edges) + len(faces) == 2
    print(f'Validated closed mesh: {len(vertices)} positions, {len(faces)} triangles; 128 x 64 textures.')


if __name__ == '__main__':
    textures()
    faces = mesh()
    validate(faces)
    obj(faces)
    glb(faces)
    preview(faces)
