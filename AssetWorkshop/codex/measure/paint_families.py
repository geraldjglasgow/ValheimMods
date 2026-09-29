"""The material families the codex measures and the game textures that show each one, chosen by looking at them.

A sample is a dict: `texture` (albedo path in the export), and optionally `prefab` (only the texels that prefab's
meshes cover: picks one item out of a shared atlas), `crop` (x0, y0, x1, y1 in fractions from the top left), `mask`
('metal' or 'nonmetal', by the material's metal mask) and `material` (whose tint, normal map and metal mask apply;
by default the material that draws the texture most often). `region` is the paint region a new asset's part of this
family gets by default (see recolour.md).
"""
from paint_families_base import A, C, I, NATURE, P, PLANKS, R, W, s  # noqa: F401  (palette_biomes uses s)
import paint_families_hard
import paint_families_soft

ORDER = [
    "wood.planks", "wood.dark", "wood.logs", "wood.bark", "wood.fine", "metal.iron", "metal.bronze", "metal.copper",
    "metal.tin", "metal.silver", "metal.blackmetal", "metal.gold", "metal.flametal", "bone.bone", "bone.antler",
    "bone.horn", "bone.teeth", "leather.leather", "leather.hide", "leather.fur", "cloth.linen", "cloth.rope",
    "stone.stone", "stone.marble", "stone.grausten", "thatch.straw", "crystal.crystal", "crystal.ice",
    "crystal.obsidian", "chitin.chitin", "skin.skin", "skin.flesh", "veg.mushroom", "veg.leaves", "veg.moss"
]
_ALL = {**paint_families_hard.FAMILIES, **paint_families_soft.FAMILIES}
FAMILIES = {name: _ALL[name] for name in ORDER}
