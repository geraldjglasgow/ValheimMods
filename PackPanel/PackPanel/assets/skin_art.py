"""Paints PackPanel's inventory art (Brown Style), all procedural, no source images:

  panel.png            a plain dark brown panel with a clean bronze frame and small rivets; 9-sliced, border 32 px,
                       drawn at 200 pixels per unit (the border is 16 UI units), stretched
  cell.png             a dark recessed inventory cell with a soft inner shadow and a faint lit lower edge, 128 px
  button.png           a dark button with a bronze frame; 9-sliced, border 12 px
  button_hover.png     the same, lit (the pointer is over it)
  button_pressed.png   the same, pressed in
  icon_<slot>.png      bronze icons for the empty slots: head, chest, legs, back, backpack, utility, food, mead, ammo,
                       the key ring's button (key), the Tacklebox slot (tacklebox) and its bait cells (tackle); 64 px,
                       one palette and one outline weight
  ring_panel.png       the key ring's round pop-up: the panel's brown and bronze frame as a disc, 512 px, stretched
  ring_line.png        the ring the key cells hang on: a thin bronze wire circle, 512 px, stretched

Slot icons are now rendered by PackPanel/artwork/slot_icons.py. Existing icon files are preserved.
Usage: python skin_art.py <folder>   (writes panels and preview.png; keeps existing slot icons)
       python skin_art.py <folder> --legacy-icons   (explicitly restores the old bronze icons)
"""
import math
import os
import sys
from PIL import Image, ImageDraw, ImageFilter

BRONZE = (170, 118, 62, 255)
SHADOW = (128, 86, 44, 255)
LIGHT = (218, 168, 102, 255)
DARK = (66, 42, 20, 255)
BONE = (226, 204, 160, 255)
LINE = 9  # outline weight on the 256 px canvas the icons are drawn on


def paint_panel():
    s, n = 128, 127
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, n, n), radius=8, fill=(12, 8, 5, 255))
    d.rounded_rectangle((2, 2, n - 2, n - 2), radius=7, fill=(104, 76, 46, 255))
    d.rounded_rectangle((5, 5, n - 5, n - 5), radius=5, fill=(20, 14, 9, 255))
    d.rounded_rectangle((7, 7, n - 7, n - 7), radius=4, fill=(43, 30, 19, 255))
    shade = Image.new("L", (s, s), 0)
    ImageDraw.Draw(shade).rounded_rectangle((7, 7, n - 7, n - 7), radius=4, outline=255, width=6)
    shade = shade.filter(ImageFilter.GaussianBlur(3)).point(lambda v: int(v * 0.55))
    img = Image.composite(Image.new("RGBA", (s, s), (14, 9, 6, 255)), img, shade)
    d = ImageDraw.Draw(img)
    for x, y in ((3.5, 3.5), (n - 3.5, 3.5), (3.5, n - 3.5), (n - 3.5, n - 3.5)):
        d.ellipse((x - 2, y - 2, x + 2, y + 2), fill=(160, 124, 80, 255))
    return img


def paint_cell():
    s, big = 128, 512
    img = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, big - 1, big - 1), radius=24, fill=(13, 9, 6, 255))
    d.rounded_rectangle((6, 6, big - 7, big - 7), radius=20, fill=(30, 21, 14, 255))
    shade = Image.new("L", (big, big), 0)
    ImageDraw.Draw(shade).rounded_rectangle((6, 6, big - 7, big - 7), radius=20, outline=255, width=40)
    shade = shade.filter(ImageFilter.GaussianBlur(22))
    inside = Image.new("L", (big, big), 0)
    ImageDraw.Draw(inside).rounded_rectangle((6, 6, big - 7, big - 7), radius=20, fill=255)
    shade = Image.composite(shade, Image.new("L", (big, big), 0), inside).point(lambda v: int(v * 0.75))
    img = Image.composite(Image.new("RGBA", (big, big), (10, 7, 4, 255)), img, shade)
    ImageDraw.Draw(img).line([(30, big - 8), (big - 30, big - 8)], fill=(70, 50, 32, 255), width=5)
    return img.resize((s, s), Image.LANCZOS)


def paint_button(frame, top, bottom):
    w, h = 64, 32
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=5, fill=(10, 7, 4, 255))
    d.rounded_rectangle((1, 1, w - 2, h - 2), radius=4, fill=frame)
    d.rounded_rectangle((3, 3, w - 4, h - 4), radius=3, fill=(16, 11, 7, 255))
    for y in range(4, h - 4):
        t = (y - 4) / (h - 8)
        c = int(top + (bottom - top) * t)
        d.line([(4, y), (w - 5, y)], fill=(c, int(c * 0.72), int(c * 0.48), 255))
    return img


def canvas():
    img = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def finish(img):
    return img.resize((64, 64), Image.LANCZOS)


def shape(d, points, fill=BRONZE):
    d.polygon(points, fill=fill, outline=DARK, width=LINE)


def ellipse_points(cx, cy, rx, ry, angle, count=48):
    a = math.radians(angle)
    pts = []
    for i in range(count):
        t = math.tau * i / count
        x, y = rx * math.cos(t), ry * math.sin(t)
        pts.append((cx + x * math.cos(a) - y * math.sin(a), cy + x * math.sin(a) + y * math.cos(a)))
    return pts


def icon_head():
    img, d = canvas()
    shape(d, [(52, 150), (86, 150), (82, 206), (58, 214), (46, 196)], SHADOW)
    shape(d, [(170, 150), (204, 150), (210, 196), (198, 214), (174, 206)], SHADOW)
    d.pieslice((52, 24, 204, 236), 180, 360, fill=BRONZE, outline=DARK, width=LINE)
    d.line([(128, 28), (128, 128)], fill=DARK, width=7)
    d.arc((70, 42, 186, 220), 200, 250, fill=LIGHT, width=12)
    d.rounded_rectangle((42, 124, 214, 156), radius=10, fill=SHADOW, outline=DARK, width=LINE)
    for x in (66, 102, 154, 190):
        d.ellipse((x - 6, 134, x + 6, 146), fill=LIGHT)
    shape(d, [(114, 154), (142, 154), (142, 190), (128, 204), (114, 190)], LIGHT)
    return finish(img)


def icon_chest():
    img, d = canvas()
    shape(d, [(98, 36), (158, 36), (220, 64), (242, 132), (204, 144), (190, 108), (192, 228), (64, 228), (66, 108),
              (52, 144), (14, 132), (36, 64)])
    d.polygon([(98, 36), (128, 84), (158, 36)], fill=DARK)
    d.rectangle((66, 156, 190, 176), fill=SHADOW, outline=DARK, width=5)
    d.rounded_rectangle((114, 150, 142, 182), radius=4, fill=LIGHT, outline=DARK, width=5)
    d.line([(80, 70), (86, 140)], fill=LIGHT, width=8)
    return finish(img)


def icon_legs():
    img, d = canvas()
    shape(d, [(66, 34), (190, 34), (212, 228), (146, 228), (128, 116), (110, 228), (44, 228)])
    d.rectangle((66, 34, 190, 62), fill=LIGHT, outline=DARK, width=LINE)
    d.line([(128, 62), (128, 112)], fill=DARK, width=6)
    d.line([(80, 80), (66, 212)], fill=LIGHT, width=8)
    return finish(img)


def icon_back():
    img, d = canvas()
    shape(d, [(92, 44), (164, 44), (214, 206), (192, 222), (160, 208), (128, 226), (96, 208), (64, 222), (42, 206)])
    for top, bottom in (((110, 70), (86, 206)), ((146, 70), (170, 206)), ((128, 72), (128, 214))):
        d.line([top, bottom], fill=SHADOW, width=7)
    d.line([(96, 62), (66, 196)], fill=LIGHT, width=8)
    d.ellipse((108, 30, 148, 70), fill=LIGHT, outline=DARK, width=LINE)
    return finish(img)


def icon_backpack():
    img, d = canvas()
    d.rounded_rectangle((52, 70, 204, 236), radius=34, fill=BRONZE, outline=DARK, width=LINE)
    d.rounded_rectangle((70, 36, 186, 80), radius=18, fill=None, outline=DARK, width=11)
    d.rounded_rectangle((52, 70, 204, 142), radius=30, fill=LIGHT, outline=DARK, width=LINE)
    d.rectangle((114, 118, 142, 158), fill=DARK)
    d.rectangle((120, 124, 136, 152), fill=LIGHT)
    d.rounded_rectangle((76, 170, 180, 214), radius=12, fill=None, outline=DARK, width=8)
    return finish(img)


def icon_utility():
    img, d = canvas()
    d.ellipse((24, 70, 232, 196), fill=DARK)
    d.ellipse((34, 80, 222, 186), fill=BRONZE)
    d.ellipse((62, 104, 194, 162), fill=DARK)
    d.ellipse((72, 112, 184, 154), fill=(0, 0, 0, 0))
    d.arc((40, 84, 216, 182), 200, 320, fill=LIGHT, width=8)
    d.rounded_rectangle((96, 148, 160, 208), radius=8, fill=LIGHT, outline=DARK, width=LINE)
    d.rounded_rectangle((112, 164, 144, 192), radius=4, fill=DARK)
    d.line([(128, 164), (128, 204)], fill=SHADOW, width=7)
    return finish(img)


def icon_food():
    img, d = canvas()
    d.line([(64, 196), (124, 136)], fill=DARK, width=34)
    d.line([(64, 196), (124, 136)], fill=BONE, width=18)
    for cx, cy in ((44, 190), (70, 216)):
        d.ellipse((cx - 22, cy - 22, cx + 22, cy + 22), fill=BONE, outline=DARK, width=LINE)
    shape(d, ellipse_points(160, 100, 74, 56, -40))
    d.polygon(ellipse_points(142, 84, 30, 16, -40), fill=LIGHT)
    return finish(img)


def icon_mead():
    img, d = canvas()
    left, right = [], []
    for i in range(33):
        t = i / 32
        x = (1 - t) ** 2 * 52 + 2 * (1 - t) * t * 40 + t * t * 196
        y = (1 - t) ** 2 * 214 + 2 * (1 - t) * t * 70 + t * t * 86
        dx = 2 * (1 - t) * (40 - 52) + 2 * t * (196 - 40)
        dy = 2 * (1 - t) * (70 - 214) + 2 * t * (86 - 70)
        length = math.hypot(dx, dy)
        half = 4 + 34 * t
        left.append((x - dy / length * half, y + dx / length * half))
        right.append((x + dy / length * half, y - dx / length * half))
    shape(d, left + right[::-1])
    tip, rim = left[-1], right[-1]
    d.line([tip, rim], fill=LIGHT, width=16)
    for k in (18, 25):
        d.line([left[k], right[k]], fill=DARK, width=7)
    return finish(img)


def icon_ammo():
    img, d = canvas()
    d.line([(64, 194), (178, 80)], fill=DARK, width=30)
    d.line([(64, 194), (178, 80)], fill=LIGHT, width=14)
    shape(d, [(226, 30), (148, 64), (192, 108)])
    shape(d, [(86, 174), (30, 176), (20, 162), (72, 158)])
    shape(d, [(86, 174), (84, 230), (98, 240), (102, 186)])
    return finish(img)


def key_shape(d, bow, tip, hole=13):
    """One key: a round bow at <bow>, a shaft to <tip>, two teeth near the tip on its right side."""
    (bx, by), (tx, ty) = bow, tip
    length = math.hypot(tx - bx, ty - by)
    ux, uy = (tx - bx) / length, (ty - by) / length
    d.line([bow, tip], fill=DARK, width=30)
    d.line([bow, tip], fill=BRONZE, width=14)
    for back in (6, 30):
        x, y = tx - ux * back, ty - uy * back
        shape(d, [(x, y), (x - uy * 34, y + ux * 34), (x - uy * 34 - ux * 14, y + ux * 34 - uy * 14),
                  (x - ux * 14, y - uy * 14)])
    d.ellipse((bx - 34, by - 34, bx + 34, by + 34), fill=BRONZE, outline=DARK, width=LINE)
    d.ellipse((bx - hole, by - hole, bx + hole, by + hole), fill=DARK)
    d.arc((bx - 26, by - 26, bx + 26, by + 26), 200, 290, fill=LIGHT, width=7)


def icon_key():
    img, d = canvas()
    d.ellipse((30, 20, 150, 140), outline=DARK, width=28)
    d.ellipse((39, 29, 141, 131), outline=BRONZE, width=10)
    d.arc((44, 34, 136, 126), 190, 280, fill=LIGHT, width=5)
    key_shape(d, (78, 150), (70, 242))
    key_shape(d, (138, 124), (230, 196))
    return finish(img)


def paint_ring_panel():
    s, big = 512, 1024
    img = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for inset, colour in ((0, (12, 8, 5, 255)), (8, (104, 76, 46, 255)), (22, (20, 14, 9, 255)), (30, (43, 30, 19, 255))):
        d.ellipse((inset, inset, big - 1 - inset, big - 1 - inset), fill=colour)
    shade = Image.new("L", (big, big), 0)
    ImageDraw.Draw(shade).ellipse((30, 30, big - 31, big - 31), outline=255, width=26)
    shade = shade.filter(ImageFilter.GaussianBlur(12)).point(lambda v: int(v * 0.55))
    img = Image.composite(Image.new("RGBA", (big, big), (14, 9, 6, 255)), img, shade)
    d = ImageDraw.Draw(img)
    centre, rim = big / 2, big / 2 - 15
    for k in range(8):
        a = math.tau * k / 8 + math.tau / 16
        x, y = centre + rim * math.cos(a), centre + rim * math.sin(a)
        d.ellipse((x - 7, y - 7, x + 7, y + 7), fill=(160, 124, 80, 255))
    return img.resize((s, s), Image.LANCZOS)


def paint_ring_line():
    s, big = 512, 1024
    img = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((4, 4, big - 5, big - 5), outline=(12, 8, 5, 255), width=30)
    d.ellipse((10, 10, big - 11, big - 11), outline=(150, 104, 56, 255), width=18)
    d.arc((14, 14, big - 15, big - 15), 195, 285, fill=LIGHT, width=6)
    return img.resize((s, s), Image.LANCZOS)


def icon_tacklebox():
    img, d = canvas()
    d.rounded_rectangle((84, 34, 172, 92), radius=20, fill=None, outline=DARK, width=26)
    d.rounded_rectangle((84, 34, 172, 92), radius=20, fill=None, outline=BRONZE, width=10)
    d.rounded_rectangle((34, 118, 222, 226), radius=16, fill=BRONZE, outline=DARK, width=LINE)
    d.line([(46, 178), (210, 178)], fill=SHADOW, width=8)
    d.rounded_rectangle((26, 80, 230, 130), radius=14, fill=LIGHT, outline=DARK, width=LINE)
    d.rounded_rectangle((108, 112, 148, 154), radius=6, fill=DARK)
    d.rounded_rectangle((116, 120, 140, 146), radius=4, fill=LIGHT)
    return finish(img)


def icon_tackle():
    img, d = canvas()
    for width, colour in ((44, DARK), (24, BRONZE)):
        d.line([(170, 70), (170, 150)], fill=colour, width=width)
        d.arc((62, 88, 170, 212), 0, 180, fill=colour, width=width)
        d.line([(62, 150), (62, 104)], fill=colour, width=width)
    shape(d, [(62, 62), (100, 116), (40, 118)])
    d.ellipse((140, 14, 200, 74), fill=BRONZE, outline=DARK, width=LINE)
    d.ellipse((158, 32, 182, 56), fill=DARK)
    d.line([(178, 84), (178, 146)], fill=LIGHT, width=7)
    return finish(img)


ICONS = {"head": icon_head, "chest": icon_chest, "legs": icon_legs, "back": icon_back, "backpack": icon_backpack,
         "utility": icon_utility, "food": icon_food, "mead": icon_mead, "ammo": icon_ammo, "key": icon_key,
         "tacklebox": icon_tacklebox, "tackle": icon_tackle}


def preview(folder, images):
    board = Image.new("RGBA", (1040, 330), (44, 30, 19, 255))
    board.alpha_composite(images["panel"].resize((256, 256)), (10, 10))
    cell = images["cell"].resize((64, 64))
    for i, name in enumerate(ICONS):
        x, y = 290 + (i % 5) * 84, 20 + (i // 5) * 84
        board.alpha_composite(cell, (x, y))
        board.alpha_composite(images["icon_" + name].resize((40, 40)), (x + 12, y + 6))
    for i, name in enumerate(("button", "button_hover", "button_pressed")):
        board.alpha_composite(images[name].resize((128, 44)), (290 + i * 140, 200))
    ring = images["ring_panel"].resize((300, 300))
    ring.alpha_composite(images["ring_line"].resize((180, 180)), (60, 60))
    for k in range(6):
        a = math.tau * k / 6 - math.tau / 4
        ring.alpha_composite(cell.resize((56, 56)), (int(150 + 90 * math.cos(a) - 28), int(150 + 90 * math.sin(a) - 28)))
    board.alpha_composite(ring, (730, 15))
    board.save(f"{folder}/preview.png")


def main():
    folder = sys.argv[1]
    images = {"panel": paint_panel(), "cell": paint_cell(),
              "button": paint_button((120, 88, 52, 255), 40, 26),
              "button_hover": paint_button((176, 132, 78, 255), 60, 40),
              "button_pressed": paint_button((96, 70, 40, 255), 22, 30),
              "ring_panel": paint_ring_panel(), "ring_line": paint_ring_line()}
    for name, paint in ICONS.items():
        path = os.path.join(folder, "icon_" + name + ".png")
        # Slot icons now come from artwork/slot_icons.py; preserve them when rebuilding panels.
        images["icon_" + name] = Image.open(path).convert("RGBA") if os.path.exists(path) and "--legacy-icons" not in sys.argv else paint()
    for name, image in images.items():
        image.save(f"{folder}/{name}.png")
    preview(folder, images)
    print("wrote " + ", ".join(sorted(images)) + " and preview.png")


if __name__ == "__main__":
    main()
