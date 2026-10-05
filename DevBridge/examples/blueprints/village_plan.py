# The village layout (frame metres: x along the canal, z north, main gate on the south wall) and a labelled
# top-down plan of it: python village_plan.py [out.png]. compound.py builds from the same numbers.
import sys
from PIL import Image, ImageDraw, ImageFont

XW, ZW = 64, 53                       # wall lines
CW, QUAY = 7.5, (8.5, 12.5)           # canal half width, quay band
WALK_IN = 4.5

# name, kind, centre x, centre z, width (x), depth (z) - footprints with eaves
LAYOUT = [
    ("Mead hall", "hall", 0, 33, 32, 18),
    ("Plaza", "open", 0, 17, 24, 9),
] + [
    (f"House {i + 1}", "house", x, z, 14, 14)
    for i, (x, z) in enumerate([(-46, 38), (-28, 38), (-46, 21), (-28, 21), (28, 38), (46, 38), (28, 21), (46, 21)])
] + [
    ("Kitchen", "work", -15, -21, 14, 14),
    ("Food store", "store", -15, -38, 12, 14),
    ("Blacksmith", "work", -45, -21, 18, 14),
    ("Smelting yard", "work", -45, -38, 18, 14),
    ("Barn", "barn", 19, -30, 18, 26),
    ("Pen 1", "pen", 37, -21, 12, 12),
    ("Pen 2", "pen", 51, -21, 12, 12),
    ("Pen 3", "pen", 37, -38, 12, 12),
    ("Pen 4", "pen", 51, -38, 12, 12),
    ("Field west", "field", -33, -82, 54, 40),
    ("Field east", "field", 33, -82, 54, 40),
]

COLOURS = {"hall": (170, 120, 60), "house": (150, 105, 60), "work": (120, 90, 70), "store": (135, 100, 70),
           "barn": (140, 95, 55), "pen": (190, 170, 120), "field": (120, 150, 70), "open": (175, 170, 160)}


def draw(out, scale=7, margin=14):
    x0, x1, z0, z1 = -XW - margin, XW + margin, -105, ZW + margin
    img = Image.new("RGB", (int((x1 - x0) * scale), int((z1 - z0) * scale)), (92, 120, 70))
    d = ImageDraw.Draw(img)
    font = ImageFont.load_default(size=13)
    P = lambda x, z: ((x - x0) * scale, (z1 - z) * scale)
    box = lambda cx, cz, w, h, **k: d.rectangle([P(cx - w / 2, cz + h / 2), P(cx + w / 2, cz - h / 2)], **k)
    box(0, 0, 2 * XW + 30, 2 * QUAY[1], fill=(150, 150, 150))                       # quays
    box(0, 0, 2 * XW + 30, 2 * CW, fill=(60, 95, 140))                               # canal
    for sx in (-1, 1):                                                               # walls with their walks
        box(sx * XW, 0, 1, 2 * ZW, fill=(110, 110, 110))
        box(sx * (XW - WALK_IN / 2), 0, WALK_IN, 2 * ZW, fill=(140, 110, 80))
        box(0, sx * ZW, 2 * XW, 1, fill=(110, 110, 110))
        box(0, sx * (ZW - WALK_IN / 2), 2 * XW, WALK_IN, fill=(140, 110, 80))
        box(sx * XW, 0, 4, 16, fill=(60, 95, 140))                                   # water gates
        for sz in (-1, 1):
            box(sx * XW, sz * ZW, 10, 10, fill=(90, 80, 70))                         # corner towers
            box(sx * (XW - 1.5), sz * 11, 6, 6, fill=(90, 80, 70))                   # water-gate towers
        box(sx * 6, -ZW + 1.5, 6, 6, fill=(90, 80, 70))                              # main gate towers
        box(sx * 6, ZW - 1.5, 6, 6, fill=(90, 80, 70))                               # back gate towers
    box(0, -ZW, 6, 3, fill=(200, 180, 120))                                          # main gate
    box(0, ZW, 6, 3, fill=(200, 180, 120))                                           # back gate
    box(0, -ZW - 26, 4, 52, fill=(185, 175, 150))                                    # road out to the fields
    box(0, -30, 4, 35, fill=(185, 175, 150))                                         # road to the drawbridge
    box(0, 0, 4, 2 * CW, fill=(160, 120, 70))                                        # drawbridge
    for name, kind, cx, cz, w, h in LAYOUT:
        box(cx, cz, w, h, fill=COLOURS[kind], outline=(40, 30, 20))
        tw = d.textlength(name, font=font)
        d.text((P(cx, cz)[0] - tw / 2, P(cx, cz)[1] - 7), name, fill=(255, 255, 255), font=font)
    for label, x, z in (("Drawbridge", 9, 0), ("Main gate", 9, -ZW - 3), ("Water gate", -XW + 2, 15),
                        ("Water gate", XW - 14, 15), ("Canal 15 m", -30, -3), ("Back gate", 9, ZW + 5)):
        d.text(P(x, z), label, fill=(255, 255, 230), font=font)
    d.text((10, 10), f"Village {2 * XW} x {2 * ZW} m inside the walls' centre lines; N up, canal W-E", fill=(255, 255, 255), font=font)
    img.save(out)
    print(out)


if __name__ == "__main__":
    draw(sys.argv[1] if len(sys.argv) > 1 else "village_plan.png")
