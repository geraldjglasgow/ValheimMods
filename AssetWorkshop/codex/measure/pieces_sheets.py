"""Contact sheets for looking at how the pieces are painted, written to codex/out/pieces/ (never committed):

- atlas/<texture>.png: a texture shared by several pieces, point-upscaled, with the UV islands of up to eight of
  them drawn over it in their own colours (the material's tiling and offset applied, wrapped into 0-1), so what
  lives where in the atlas can be read off.
- textures/<set>.png: each material set's albedo, normal and worn albedo side by side, point-upscaled, and a
  4x4 tiling of the albedo to see how it repeats.

    python codex/measure/pieces_sheets.py          (after measure/pieces.py)
"""
import json
import os
import re
import sys

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

import game  # noqa: E402
import pieces_mesh  # noqa: E402
import pieces_scan  # noqa: E402
from workshop import unity  # noqa: E402

DATA = os.path.join(game.WORKSHOP, "codex", "data", "pieces.json")
OUT = os.path.join(game.WORKSHOP, "codex", "out", "pieces")
COLOURS = [(255, 64, 64), (64, 220, 255), (255, 220, 40), (120, 255, 90), (255, 110, 230), (255, 150, 40),
           (170, 120, 255), (255, 255, 255)]
SIZE = 1024


def _uv_transform(material):
    """(scale, offset) the material applies to _MainTex."""
    text = unity.read(material)
    found = re.search(r"^      _MainTex:\n        m_Texture: .*\n        m_Scale: (\{[^}]*\})\n        m_Offset: "
                      r"(\{[^}]*\})", text, re.MULTILINE)
    return (unity.numbers(found.group(1)), unity.numbers(found.group(2))) if found else ((1, 1), (0, 0))


def islands(prefab, texture):
    """[(uv triangles (m, 3, 2))] a piece draws with this texture as its main texture, in texture space."""
    scan, found = pieces_scan.Scan(prefab), []
    for row in scan.visual():
        parts = pieces_mesh.parts(row["mesh"])
        if not parts or parts[1] is None:
            continue
        for n, material in enumerate(row["materials"]):
            if material and game.material(material)["textures"].get("_MainTex") == texture:
                scale, offset = _uv_transform(material)
                tris = [t for i, t in parts[2] if i == n]
                if tris:
                    uv = parts[1] * np.array(scale[:2]) + np.array(offset[:2])
                    found.append(uv[np.concatenate(tris)])
    return found


def atlas_sheet(texture, prefabs, path):
    """The texture at SIZE px with each prefab's islands outlined in its colour and a legend."""
    base = Image.open(os.path.join(unity.ROOT, texture)).convert("RGB").resize((SIZE, SIZE), Image.NEAREST)
    image = Image.new("RGB", (SIZE + 300, SIZE), (30, 30, 30))
    image.paste(base, (0, 0))
    draw = ImageDraw.Draw(image)
    for n, prefab in enumerate(prefabs[:len(COLOURS)]):
        colour = COLOURS[n]
        for tris in islands(prefab, texture):
            for tri in tris:
                shift = np.floor(tri.mean(axis=0))
                points = [((u - shift[0]) * SIZE, (1 - (v - shift[1])) * SIZE) for u, v in tri]
                draw.polygon(points, outline=colour)
        draw.text((SIZE + 12, 12 + n * 18), os.path.basename(prefab)[:-7], fill=colour)
    draw.text((SIZE + 12, SIZE - 40), os.path.basename(texture), fill=(200, 200, 200))
    draw.text((SIZE + 12, SIZE - 22), f"{game.texture_size(texture)} px", fill=(200, 200, 200))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    image.save(path)


def texture_sheet(name, materials, path):
    """Albedo, normal and worn albedo of a set side by side at 384 px, then the albedo tiled 4x4 at 768 px."""
    tiles = []
    for label, material, slot in materials:
        texture = game.material(material)["textures"].get(slot) if material else None
        if texture:
            tiles.append((f"{label}: {os.path.basename(texture)} {game.texture_size(texture)}", texture))
    image = Image.new("RGB", (max(len(tiles), 1) * 396 + 12 + 780, 800), (30, 30, 30))
    draw = ImageDraw.Draw(image)
    for n, (label, texture) in enumerate(tiles):
        tile = Image.open(os.path.join(unity.ROOT, texture)).convert("RGB").resize((384, 384), Image.NEAREST)
        image.paste(tile, (12 + n * 396, 30))
        draw.text((12 + n * 396, 420), label[:60], fill=(220, 220, 220))
    if tiles:
        albedo = Image.open(os.path.join(unity.ROOT, tiles[0][1])).convert("RGB").resize((192, 192), Image.NEAREST)
        for i in range(16):
            image.paste(albedo, (len(tiles) * 396 + 12 + (i % 4) * 192, 12 + (i // 4) * 192))
    draw.text((12, 8), name, fill=(255, 230, 180))
    os.makedirs(os.path.dirname(path), exist_ok=True)
    image.save(path)


# material set -> (label, .mat path, slot) to lay side by side
SETS = {
    "wood planks": ("woodwall", "woodwall_worn"), "core wood logs": ("logwall", "logwall_broken"),
    "darkwood": ("DarkWood_mat", "DarkWood_worn_mat"), "thatch": ("straw_roof", "straw_roof_worn"),
    "darkwood shingles": ("RoofShingles", "RoofShingles_worn"), "stone": ("stone_mat", "stone_mat_worn"),
    "iron cage": ("metalwall", "metalwall_worn"), "iron beam": ("Ironbeam_mat", "Ironbeam_Worn_mat"),
    "black marble": ("blackmarble", "blackmarble_worn"), "grausten": ("Grausten_mat", "Grausten_Broken_mat"),
    "ashwood": ("AshWood_mat", "AshWood_worn_mat"), "stave scales": ("Scaledwall_mat", "Scaledwall_worn_mat"),
    "stake wall": ("stake", "stake_worn"), "banner cloth": ("Banner_Border_BlackWhite_mat", None),
    "dvergr metal": ("dvergr_metal_furnmiture", None), "workbench": ("Workbench_mat", "WorkbenchWorn_mat"),
    "forge": ("Forge_mat", "ForgeWorn"), "grausten roof": ("Grausten_RoofSlab_mat", None),
    "wood chest": ("woodchest", None), "reinforced chest": ("ironchest", None), "table": ("Table_Mat", None),
    "bed": ("BedSimple_Mat", "BedSimple_worn_Mat"), "raven throne": ("RavenThrone_Mat", None),
    "torch": ("torch_wood", None), "brazier": ("Brazier_Mat", None), "hearth": ("HearthNew_mat", "HearthBroken_mat"),
    "stonecutter": ("StoneCutterBench_mat", None), "smelter": ("smeltermat", None),
    "charcoal kiln": ("new_charcoalkilnmat", None), "fermenter": ("fermenter", None), "portal": ("portal_small", None),
    "spinning wheel": ("SpinningWheel_mat", None), "windmill": ("WindmillBase", None),
}


def material_path(name):
    """The .mat path of a material by name among the pieces' materials, or None."""
    if not name:
        return None
    hits = game.find(f"**/{name}.mat")
    pieces = [h for h in hits if "Pieces" in h] or hits
    return pieces[0] if pieces else None


def spread(samples, texture, count=len(COLOURS)):
    """Up to count prefabs drawing this texture, one per category in turn so the sheet shows every kind of use."""
    by_kind = {}
    for sample in sorted(samples, key=lambda s: s["name"]):
        if texture in sample["textures"]:
            by_kind.setdefault(sample["category"], []).append(sample["prefab"])
    picked, queues = [], [list(v) for _, v in sorted(by_kind.items())]
    while len(picked) < count and any(queues):
        for queue in queues:
            if queue and len(picked) < count:
                picked.append(queue.pop(0))
    return picked


def main():
    with open(DATA, encoding="utf-8") as handle:
        data = json.load(handle)
    samples = [s for c in data["categories"].values() for s in c["samples"]]
    for texture, info in data["textures"].items():
        if info["pieces"] >= 3:
            atlas_sheet(texture, spread(samples, texture),
                        os.path.join(OUT, "atlas", os.path.basename(texture)[:-4] + ".png"))
    for name, (new, worn) in SETS.items():
        new_path, worn_path = material_path(new), material_path(worn)
        texture_sheet(name, [("albedo", new_path, "_MainTex"), ("normal", new_path, "_BumpMap"),
                             ("worn", worn_path, "_MainTex"), ("metal mask", new_path, "_MetallicTex")],
                      os.path.join(OUT, "textures", name.replace(" ", "_") + ".png"))
    print("sheets written to", OUT)


if __name__ == "__main__":
    main()
