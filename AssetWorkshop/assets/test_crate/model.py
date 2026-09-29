"""Test asset: a small plank crate with iron corner brackets. Proves the pipeline from script to asset bundle."""
import random

from workshop import materials, shapes

TEXTURE_SIZE = 512

WIDTH, DEPTH, HEIGHT = 0.8, 0.6, 0.55   # x, y, z in metres
BOARD = 0.03                             # plank thickness
POST = 0.07                              # corner post width
ROWS = 3                                 # planks per side


def build():
    random.seed(7)
    along_x = materials.wood("crate_planks_x", axis='X')
    along_y = materials.wood("crate_planks_y", axis='Y')
    posts = materials.wood("crate_posts", axis='Z', light=(0.36, 0.22, 0.11))
    iron = materials.iron("crate_iron")
    _sides(along_x, along_y)
    _posts(posts)
    _lid(along_x)
    _brackets(iron)
    _latch(iron)
    shapes.collider_box("body", (WIDTH, DEPTH, HEIGHT), (0, 0, HEIGHT / 2))


def _latch(material):
    """On the front only (-Y): the Unity build log shows the front ended up facing +Z."""
    y = -(DEPTH / 2 + 0.02)
    shapes.box("latch_plate", (0.09, 0.012, 0.13), (0, y, HEIGHT - 0.04), material=material)
    shapes.cylinder("latch_ring", 0.025, 0.012, (0, y - 0.012, HEIGHT - 0.12), (1.5708, 0, 0), material, vertices=10)


def _sides(along_x, along_y):
    row = (HEIGHT - 0.02) / ROWS
    for i in range(ROWS):
        z = 0.01 + row * (i + 0.5)
        for sign in (-1, 1):
            shapes.box(f"plank_x{i}{sign}", (WIDTH - 0.02, BOARD, row - 0.012),
                       (0, sign * (DEPTH / 2 - BOARD / 2), z), _jitter(), along_x, bevel=0.006)
            shapes.box(f"plank_y{i}{sign}", (BOARD, DEPTH - 0.02, row - 0.012),
                       (sign * (WIDTH / 2 - BOARD / 2), 0, z), _jitter(), along_y, bevel=0.006)


def _posts(material):
    for sx in (-1, 1):
        for sy in (-1, 1):
            shapes.box(f"post{sx}{sy}", (POST, POST, HEIGHT),
                       (sx * (WIDTH / 2 - POST / 2 + 0.006), sy * (DEPTH / 2 - POST / 2 + 0.006), HEIGHT / 2),
                       material=material, bevel=0.01)


def _lid(material):
    count = 4
    width = DEPTH / count
    for i in range(count):
        y = -DEPTH / 2 + width * (i + 0.5)
        shapes.box(f"lid{i}", (WIDTH + 0.02, width - 0.01, BOARD), (0, y, HEIGHT + BOARD / 2),
                   _jitter(), material, bevel=0.006)


def _brackets(material):
    for sx in (-1, 1):
        for sy in (-1, 1):
            for z in (0.07, HEIGHT - 0.05):
                x, y = sx * (WIDTH / 2 + 0.01), sy * (DEPTH / 2 + 0.01)
                shapes.box(f"band_front{sx}{sy}{z}", (0.16, 0.012, 0.07), (x - sx * 0.07, y, z), material=material)
                shapes.box(f"band_side{sx}{sy}{z}", (0.012, 0.16, 0.07), (x, y - sy * 0.07, z), material=material)


def _jitter(amount=0.012):
    """A slight tilt so the planks look hand fitted."""
    return (random.uniform(-amount, amount), random.uniform(-amount, amount), random.uniform(-amount, amount))
