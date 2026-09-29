"""The shared pieces of the family tables (paint_families_hard, paint_families_soft): the sample constructor and the
folders the samples' textures live in, under the reference export's Assets."""


def s(texture, **options):
    """One family sample: a texture, and optionally prefab, crop, mask and material (see paint_families)."""
    return dict(texture=texture, **options)


I = "GameElements/Items/"
W = "GameElements/Items/weapons/_res/"
A = "GameElements/Items/armor/_res/"
P = "GameElements/Pieces/_res/"
R = "world/Props/"
C = "Characters/"
PLANKS = "3rd party/ADG_Textures/Plank Textures/"
NATURE = "3rd party/A_piece_of_nature/Textures/"
