"""The trailer's thumbnail: a 4K game still (the giant on the right), graded and darkened at the left, with the title in
the game's own Norse font (title-card renders on black, used as masks) stacked over it with an outline and shadow.

    python thumbnail.py <still.png> <ELITE CREATURES.png> <PACK.png>    writes thumbnail.png (1920) and thumbnail.jpg (1280)
"""
import os
import sys

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter

import director as d

SIZE = (1920, 1080)


def background(path):
    im = Image.open(path).convert("RGB").crop((0, 216, 3072, 1944)).resize(SIZE, Image.LANCZOS)
    im = ImageEnhance.Contrast(im).enhance(1.18)
    im = ImageEnhance.Color(im).enhance(1.15)
    shade = Image.new("L", SIZE)
    draw = ImageDraw.Draw(shade)
    for x in range(SIZE[0]):                       # dark at the left, where the words go
        draw.line([(x, 0), (x, SIZE[1])], fill=int(170 * max(0.0, 1 - x / (SIZE[0] * 0.62)) ** 1.6))
    return Image.composite(Image.new("RGB", SIZE, (4, 8, 14)), im, shade)


def words(path):
    """Each word of a title-card render as its own mask: split at gaps much wider than the gaps between letters."""
    mask = Image.open(path).convert("L").point(lambda v: min(255, int(v * 1.4)))
    mask = mask.crop(mask.getbbox())
    ink = [x for x in range(mask.width) if mask.crop((x, 0, x + 1, mask.height)).getbbox() is not None]
    gaps = [(b - a, a, b) for a, b in zip(ink, ink[1:]) if b - a > 1]
    if not gaps:
        return [mask]
    typical = sorted(g for g, _, _ in gaps)[len(gaps) // 2]
    cuts = [(a, b) for g, a, b in gaps if g > 1.8 * typical and g > 40]
    edges = [0] + [v for a, b in cuts for v in (a + 1, b)] + [mask.width]
    return [mask.crop((edges[i], 0, edges[i + 1], mask.height)) for i in range(0, len(edges), 2)]


def stamp(canvas, mask, at, height, color):
    mask = mask.resize((int(mask.width * height / mask.height), height), Image.LANCZOS)
    shadow = mask.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(10))
    canvas.paste((0, 0, 0), (at[0] + 6, at[1] + 8), shadow)
    canvas.paste((8, 6, 4), at, mask.filter(ImageFilter.MaxFilter(7)))
    canvas.paste(color, at, mask)
    return at[1] + height


def main(still, title, pack):
    canvas = background(still)
    whole = Image.open(pack).convert("L").point(lambda v: min(255, int(v * 1.4)))
    lines = words(title) + [whole.crop(whole.getbbox())]
    y = 70
    for i, (mask, color) in enumerate(zip(lines, [(244, 232, 205), (244, 232, 205), (150, 215, 255)])):
        y = stamp(canvas, mask, (70, y), 172, color) + 30
    out = os.path.join(d.FOOTAGE, "thumbnail.png")
    canvas.save(out)
    canvas.resize((1280, 720), Image.LANCZOS).save(out.replace(".png", ".jpg"), quality=92)
    return out


if __name__ == "__main__":
    print(main(*sys.argv[1:4]))
