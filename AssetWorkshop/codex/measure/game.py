"""Reads the game's own assets from the local reference export for the codex's measurements. Plain Python (numpy and
Pillow at most), no Blender: every measuring script imports this.

    import game
    p = game.prefab("GameElements/Items/weapons/Battleaxe.prefab")
    for node in p.nodes():                              # every GameObject, parents before children
        for kind, body in p.components(node):          # ('MeshRenderer', yaml) or a game script ('ItemDrop', yaml)
            ...
    game.mesh_stats("…/model.asset")                    # triangles, vertices, size, bones, without decoding
    game.material("…/x.mat")                            # shader, every texture slot, floats, colours, keywords

Game scripts are named from assembly_valheim.dll: Unity gives a script in a DLL the fileID of the first four bytes of
MD4("s\\0\\0\\0" + namespace + name), so the class list (ilspycmd, cached in AssetWorkshop/out/game_classes.json) names
every MonoBehaviour. Nothing read here may be copied into the repository: the codex keeps measurements and words only.
"""
import functools
import glob
import json
import os
import struct
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
WORKSHOP = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(WORKSHOP, "blender"))

from workshop import unity  # noqa: E402  (plain Python, no bpy)
from md4 import md4  # noqa: E402
from game_text import _all, _hex, _nested, _section, _vertex_count  # noqa: E402,F401  (callers use some)
from game_mesh import _FORMATS, _channel, _streams, mesh_arrays, texel_density  # noqa: E402,F401  (re-exported)

ROOT = unity.ROOT
GAME_DLL = r"C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll"
CLASS_CACHE = os.path.join(WORKSHOP, "out", "game_classes.json")
ILSPY = os.path.join(os.environ.get("USERPROFILE", ""), ".dotnet", "tools", "ilspycmd.exe")
NATIVE = {1: "GameObject", 4: "Transform", 23: "MeshRenderer", 33: "MeshFilter", 54: "Rigidbody", 59: "HingeJoint",
          64: "MeshCollider", 65: "BoxCollider", 82: "AudioSource", 95: "Animator", 96: "TrailRenderer", 108: "Light",
          111: "Animation", 114: "MonoBehaviour", 120: "LineRenderer", 135: "SphereCollider", 136: "CapsuleCollider",
          137: "SkinnedMeshRenderer", 143: "CharacterController", 153: "ConfigurableJoint", 154: "TerrainCollider",
          198: "ParticleSystem", 199: "ParticleSystemRenderer", 205: "LODGroup", 212: "SpriteRenderer",
          224: "RectTransform", 1001: "PrefabInstance"}
TRANSFORMS = (4, 224)


def find(pattern):
    """Paths under the export matching a glob ('**/fx_*.prefab'), relative to it, forward slashes."""
    hits = glob.glob(os.path.join(ROOT, pattern), recursive=True)
    return sorted(os.path.relpath(h, ROOT).replace("\\", "/") for h in hits)


def path_of(value):
    """A '{fileID: …, guid: …}' reference to another file -> its path under the export, or None."""
    _, guid = unity.ref(value)
    if not guid or guid in (unity.BUILTIN_ASSETS, unity.BUILTIN_MESHES):
        return None
    return unity.asset_path(guid)


def prefab(path):
    return Prefab(path)


class Prefab:
    """One prefab file's documents: its GameObjects as nodes (transform fileIDs) and their components by class name."""

    def __init__(self, path):
        self.path, self.docs = path, unity.documents(path)

    def nodes(self):
        """Every local transform, parents before children, starting at the root."""
        order, stack = [], [self.root()]
        while stack:
            node = stack.pop()
            order.append(node)
            stack.extend(reversed(self.children(node)))
        return order

    def root(self):
        return next(i for i, (cls, body) in self.docs.items() if cls in TRANSFORMS and self.is_local(i)
                    and unity.ref(unity.field(body, "m_Father"))[0] == "0")

    def is_local(self, node):
        return node in self.docs and "m_GameObject:" in self.docs[node][1]

    def children(self, node):
        kids = [unity.ref(item)[0] for item in unity.items(self.docs[node][1], "m_Children")]
        return [k for k in kids if self.is_local(k)]

    def game_object(self, node):
        return self.docs[unity.ref(unity.field(self.docs[node][1], "m_GameObject"))[0]][1]

    def name(self, node):
        return unity.field(self.game_object(node), "m_Name")

    def active(self, node):
        return unity.field(self.game_object(node), "m_IsActive") != "0"

    def path_to(self, node):
        """'Root/Child/Grandchild' names from the prefab root."""
        names = []
        while node and self.is_local(node):
            names.append(self.name(node))
            node = unity.ref(unity.field(self.docs[node][1], "m_Father"))[0]
        return "/".join(reversed(names))

    def world_scale(self, node):
        """The node's size against its own units in the prefab: the product of the (mean) local scales from it up to
        the root, the root's own included."""
        scale = 1.0
        while node and self.is_local(node):
            body = self.docs[node][1]
            scale *= sum(unity.numbers(unity.field(body, "m_LocalScale"))) / 3
            node = unity.ref(unity.field(body, "m_Father"))[0]
        return scale

    def find_node(self, name):
        """The first node with this GameObject name, or None."""
        return next((n for n in self.nodes() if self.name(n) == name), None)

    def components(self, node):
        """[(class name, body)] of the node's GameObject: native classes by name, MonoBehaviours by game class."""
        found = []
        for item in unity.items(self.game_object(node), "m_Component"):
            file_id = unity.ref(item)[0]
            if file_id in self.docs:
                cls, body = self.docs[file_id]
                found.append((script_class(body) if cls == 114 else NATIVE.get(cls, f"class{cls}"), body))
        return found

    def all_components(self):
        """[(node, class name, body)] over the whole prefab."""
        return [(node, kind, body) for node in self.nodes() for kind, body in self.components(node)]

    def nested(self):
        """Paths of the prefabs nested in this one as instances (their contents are not in this file)."""
        sources = [unity.field(body, "m_SourcePrefab") for cls, body in self.docs.values() if cls == 1001]
        return [p for p in (path_of(s) for s in sources) if p]


def script_class(body):
    """The game class of a MonoBehaviour body ('ZSFX'), 'Script:<file>' for scripts outside the game DLL."""
    value = unity.field(body, "m_Script")
    file_id, _ = unity.ref(value)
    source = path_of(value) or ""
    if source.endswith("assembly_valheim.dll"):
        return _classes().get(int(file_id), f"valheim:{file_id}")
    return f"Script:{os.path.basename(source) or file_id}"


@functools.lru_cache(maxsize=None)
def _classes():
    """{fileID: 'Namespace.Class'} for every class in assembly_valheim.dll, cached across runs."""
    if os.path.exists(CLASS_CACHE):
        with open(CLASS_CACHE, encoding="utf-8") as handle:
            return {int(k): v for k, v in json.load(handle).items()}
    listing = subprocess.run([ILSPY, "-l", "c", GAME_DLL], capture_output=True, text=True, check=True).stdout
    names = [line.split(" ", 1)[1].strip() for line in listing.splitlines() if line.startswith("Class ")]
    table = {file_id(n): n.split(".")[-1] if "." not in n else n for n in names if "<" not in n}
    os.makedirs(os.path.dirname(CLASS_CACHE), exist_ok=True)
    with open(CLASS_CACHE, "w", encoding="utf-8") as handle:
        json.dump(table, handle)
    return table


def file_id(full_name):
    """Unity's fileID for a script class inside a DLL."""
    return struct.unpack("<i", md4(b"s\0\0\0" + full_name.encode("utf-8"))[:4])[0]


@functools.lru_cache(maxsize=None)
def mesh_stats(path):
    """Triangles, vertices, submeshes, bone count and bounding size (Unity axes, metres) of a Mesh .asset, read from
    its header without decoding the vertex data."""
    text = unity.read(path)
    body = text[text.index("m_SubMeshes:"):text.index("m_Shapes:")] if "m_Shapes:" in text else text
    counts = [int(c) for c in _all(body, r"indexCount: (\d+)")]
    topologies = [int(t) for t in _all(body, r"topology: (\d+)")]
    tris = sum(c // 3 for c, t in zip(counts, topologies) if t == 0)
    extent = unity.numbers(_nested(text, "m_LocalAABB", "m_Extent"))
    bones = len(_nested(text, "m_BoneNameHashes").strip("[]")) // 8   # one hex string, 4 bytes a bone
    return {"triangles": tris, "vertices": int(_vertex_count(text)),
            "submeshes": len(counts), "bones": bones, "size": [round(2 * e, 4) for e in extent]}


@functools.lru_cache(maxsize=None)
def material(path):
    """{name, shader, textures: {slot: path|None}, floats: {…}, colors: {…}, keywords: […]} of a .mat."""
    text = unity.read(path)
    shader_ref = unity.field(text, "m_Shader")
    shader = path_of(shader_ref) or f"builtin_{unity.ref(shader_ref)[0]}"
    textures = _all(text, r"^      (_\w+):\n        m_Texture: (\{[^}]*\})")
    return {"name": unity.field(text, "m_Name"),
            "shader": os.path.splitext(os.path.basename(shader))[0],
            "textures": {slot: path_of(ref) for slot, ref in textures},
            "floats": {k: float(v) for k, v in _all(_section(text, "m_Floats"), rf"^      (_\w+): ({unity.NUMBER})$")},
            "colors": {k: unity.numbers(v) for k, v in _all(_section(text, "m_Colors"), r"^      (_\w+): (\{[^}]*\})$")},
            "keywords": _all(_section(text, "m_ValidKeywords") + _section(text, "m_InvalidKeywords"), r"^  - (\w+)$")}


def renderer_materials(body):
    """The .mat paths a renderer lists (None for built-in materials)."""
    return [path_of(item) for item in unity.items(body, "m_Materials")]


def renderer_mesh(prefab_, node, body):
    """The Mesh .asset a MeshRenderer (through its MeshFilter) or SkinnedMeshRenderer draws, or None."""
    if "m_Mesh:" in body:
        return path_of(unity.field(body, "m_Mesh"))
    filters = [b for kind, b in prefab_.components(node) if kind == "MeshFilter"]
    return path_of(unity.field(filters[0], "m_Mesh")) if filters else None


def texture_size(path):
    """(width, height) of an exported texture file, or None."""
    from PIL import Image
    try:
        with Image.open(os.path.join(ROOT, path)) as image:
            return image.size
    except (OSError, TypeError):
        return None
