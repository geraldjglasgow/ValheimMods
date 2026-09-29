"""What one building piece draws, measured from its prefab: the renderers of the look a new piece shows (the active
LOD 0 renderers, snow and fragment copies left out), their triangles, materials, textures and texel density, the
piece's size in its own frame, and the worn and broken looks beside it.

    scan = pieces_scan.Scan("GameElements/Pieces/woodwall.prefab")
    scan.renderers()               # every mesh renderer with its state, LOD, materials, matrix
    scan.visual()                  # the new look at LOD 0
    scan.look(scan.visual())       # triangles, materials, shaders, maps, texture px, texel density, size
"""
import functools
import os
import re

import numpy as np

import game
import pieces_mesh
from workshop import unity

RENDERERS = ("MeshRenderer", "SkinnedMeshRenderer")
SNOW_SHADERS = ("SnowMesh",)
BUILTIN_SHADERS = {"builtin_46": "Standard"}          # Unity's built-in Standard shader (fileID 46)


class Scan:
    """One piece prefab, read once."""

    def __init__(self, path):
        self.path, self.prefab = path, game.prefab(path)
        self.nodes = self.prefab.nodes()
        self.by_object = {unity.ref(unity.field(self.prefab.docs[n][1], "m_GameObject"))[0]: n for n in self.nodes}

    def components(self, node):
        """[(fileID, class name, body)] of a node's GameObject."""
        found = []
        for item in unity.items(self.prefab.game_object(node), "m_Component"):
            file_id = unity.ref(item)[0]
            if file_id in self.prefab.docs:
                cls, body = self.prefab.docs[file_id]
                found.append((file_id, game.script_class(body) if cls == 114 else game.NATIVE.get(cls, f"class{cls}"),
                              body))
        return found

    def all_components(self):
        return [(node, fid, kind, body) for node in self.nodes for fid, kind, body in self.components(node)]

    def first(self, kind):
        """The body of the first component of this class, or None."""
        return next((body for _, _, k, body in self.all_components() if k == kind), None)

    def node_of(self, value):
        """A '{fileID: …}' GameObject reference inside this prefab -> its transform node, or None."""
        return self.by_object.get(unity.ref(value)[0])

    def active(self, node, stop=None):
        """Whether the node and every ancestor (up to stop, not included) are active as the prefab is saved."""
        while node is not None and node != stop:
            if not self.prefab.active(node):
                return False
            node = pieces_mesh.parent(self.prefab, node)
        return True

    def under(self, node, ancestor):
        while node is not None:
            if node == ancestor:
                return True
            node = pieces_mesh.parent(self.prefab, node)
        return False

    @functools.cached_property
    def lod_of(self):
        """{renderer fileID: LOD index} over every LODGroup in the prefab."""
        found = {}
        for _, _, kind, body in self.all_components():
            if kind == "LODGroup":
                for level, block in enumerate(body.split("- screenRelativeHeight:")[1:]):
                    for file_id in re.findall(r"renderer: \{fileID: (-?\d+)\}", block):
                        found.setdefault(file_id, level)
        return found

    @functools.cached_property
    def states(self):
        """{'new'|'worn'|'broken'|'wet': node} from WearNTear, and 'fragments': [nodes] (its fragment roots)."""
        wear = self.first("WearNTear")
        if wear is None:
            return {"fragments": []}
        found = {key: self.node_of(unity.field(wear, "m_" + key)) for key in ("new", "worn", "broken", "wet")}
        found["fragments"] = [self.node_of(item) for item in unity.items(wear, "m_fragmentRoots")]
        found["fragments"] = [n for n in found["fragments"] if n is not None]
        return found

    @functools.cached_property
    def _renderers(self):
        rows = []
        for node, file_id, kind, body in self.all_components():
            if kind not in RENDERERS:
                continue
            ref = pieces_mesh.mesh_ref(unity.field(body, "m_Mesh")) if kind == "SkinnedMeshRenderer" else \
                self._filter_mesh(node)
            materials = game.renderer_materials(body)
            rows.append({"node": node, "id": file_id, "kind": kind, "mesh": ref, "materials": materials,
                         "enabled": unity.field(body, "m_Enabled") != "0", "active": self.active(node),
                         "lod": self.lod_of.get(file_id), "snow": self._is_snow(materials),
                         "name": self.prefab.name(node), "matrix": pieces_mesh.to_root(self.prefab, node)})
        return rows

    def renderers(self):
        return list(self._renderers)

    def _filter_mesh(self, node):
        filters = [body for _, kind, body in self.components(node) if kind == "MeshFilter"]
        return pieces_mesh.mesh_ref(unity.field(filters[0], "m_Mesh")) if filters else None

    @staticmethod
    def _is_snow(materials):
        return any(m and game.material(m)["shader"] in SNOW_SHADERS for m in materials)

    def visual(self):
        """The renderers a newly built piece shows up close: active, enabled, not snow, LOD 0 or in no LOD group."""
        return [r for r in self._renderers if r["active"] and r["enabled"] and not r["snow"] and not r["lod"]]

    def state(self, key):
        """The LOD 0 renderers of one WearNTear state ('new', 'worn', 'broken'), active or not; [] when absent."""
        root = self.states.get(key)
        if root is None:
            return []
        return [r for r in self._renderers if self.under(r["node"], root) and r["enabled"] and not r["snow"]
                and not r["lod"] and self.active(r["node"], stop=root)]

    def look(self, rows):
        """Triangles, materials, shaders, texture slots in use, main texture px, texel density and size of rows."""
        materials = sorted({m for r in rows for m in r["materials"] if m})
        density, points = _density(rows), [p for p in (_points(r) for r in rows) if p is not None]
        box = pieces_mesh.bounds(np.concatenate(points)) if points else None
        mains = sorted({t for t in (_main(m) for m in materials) if t})
        sizes = [max(game.texture_size(t) or (0, 0)) for t in mains]
        return {"triangles": sum(_triangles(r) for r in rows), "renderers": len(rows),
                "materials": [os.path.basename(m)[:-4] for m in materials],
                "shaders": sorted({shader(m) for m in materials}),
                "maps": sorted({slot for m in materials for slot, t in game.material(m)["textures"].items() if t}),
                "textures": mains, "texture_px": max(sizes) if sizes else None,
                "texel_density": round(density, 1) if density else None,
                "size_m": [round(float(v), 3) for v in box[1] - box[0]] if box else None,
                "bounds_m": [[round(float(v), 3) for v in b] for b in box] if box else None}


def shader(material):
    """A material's shader name, Unity's built-in ones by name."""
    name = game.material(material)["shader"]
    return BUILTIN_SHADERS.get(name, name)


def _triangles(row):
    """Triangles a renderer draws: its mesh's, for each submesh its materials cover."""
    return pieces_mesh.triangles(row["mesh"])


def _points(row):
    parts = pieces_mesh.parts(row["mesh"])
    return pieces_mesh.moved(parts[0], row["matrix"]) if parts else None


def _main(material):
    return game.material(material)["textures"].get("_MainTex")


@functools.lru_cache(maxsize=None)
def tiling(material):
    """The (x, y) tiling a material applies to _MainTex."""
    text = unity.read(material)
    found = re.search(r"^      _MainTex:\n        m_Texture: .*\n        m_Scale: (\{[^}]*\})", text, re.MULTILINE)
    return unity.numbers(found.group(1)) if found else (1.0, 1.0)


def _density(rows):
    """Texture pixels per metre over rows, weighted by world area: sqrt(sum of pixel areas / sum of world areas), each
    submesh with its own material's main texture and tiling. Materials without a main texture are left out."""
    pixels = world = 0.0
    for row in rows:
        parts = pieces_mesh.parts(row["mesh"])
        if not parts:
            continue
        for n, material in enumerate(row["materials"]):
            texture = _main(material) if material else None
            size = game.texture_size(texture) if texture else None
            if not size:
                continue
            uv_area, area = pieces_mesh.surface(parts, row["matrix"], n, tiling(material))
            pixels += uv_area * size[0] * size[1]
            world += area
    return float(np.sqrt(pixels / world)) if world > 0 else None
