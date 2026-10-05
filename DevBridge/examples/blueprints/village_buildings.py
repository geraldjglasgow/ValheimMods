# The village's buildings (see village_plan.py for where they stand), written with kit.py. Each returns a Plan in
# its own frame: door side -z, y from the levelled ground. Furniture comes later from furnish.py (measured pieces).
from kit import Plan, Line, O, spans, roof, gable, building, fence_rect, _bay, _gable_frame
import furnish

H2 = 3                     # storey height of the two-storey houses


# ---------------------------------------------------------------- ornate two-storey house
def ornate_house(roof_kind="wood"):
    """10 x 10 m, two storeys, a 2 m porch on log columns with a railed balcony over it, braced timber frame,
    stairs up the west wall to the bedroom floor; the roof runs out over the balcony."""
    p = Plan()
    _house_floors(p)
    sides = {"F": Line(-5, -5, "+x", "-z"), "K": Line(-5, 5, "+x", "+z"),
             "Wst": Line(-5, -5, "+z", "-x"), "E": Line(5, -5, "+z", "+x")}
    ground = {"F": "SWDWS", "K": "SWSWS", "Wst": "WSSSS", "E": "SWSWS"}
    upper = {"F": "SWdWS", "K": "SWSWS", "Wst": "SWSWS", "E": "SWSWS"}
    for bays, y0, ph in ((ground, 0, 10), (upper, H2, 30)):
        for key, kinds in bays.items():
            for i, kind in enumerate(kinds):
                _bay(p, sides[key], 2 * i + 1, kind, H2, y0)
    _house_frame(p, sides, ground, upper)
    _house_stairs(p)
    _porch(p)
    for zs in (-1, 1):
        gable(p, 45, 10, 2 * H2, zs * 5)
        _gable_frame(p, 10, 2 * H2, zs * 5, zs)
    p.extend(Plan.KEEP, roof(10, 10, 2 * H2, 1, (3, 1), horns="back", kind=roof_kind).pieces())
    for n, x, y, z, yaw in furnish.house() + furnish.house_outside():
        p.add(Plan.KEEP + 10, n, x, y, z, yaw)
    return p


def _house_floors(p):
    for x in spans(-5, 5):
        for z in spans(-7, 5):
            p.add(0, "wood_floor", x, 0.05, z)                            # ground floor and porch
        for z in spans(-7, 5):
            if not (x == -4 and -3 < z < 3):                              # stairwell
                p.add(25, "wood_floor", x, H2, z)                         # bedroom floor and balcony
        for z in (-5, -3, -1, 1, 3):
            if not (x == -4 and z in (-1, 1, 3)):
                p.add(24, "wood_beam", x, H2 - 0.34, z)                   # joists


def _house_frame(p, sides, ground, upper):
    """Posts at every bay edge (full height under the gables, below the thatch on the eave sides), sill beams,
    the girt between the storeys, plates under the gables, braces on the solid corner bays of both storeys."""
    tall = [("wood_pole2", 1), ("wood_pole2", 3), ("wood_pole2", 5)]       # 0..6, up to the gable plate
    low = [("wood_pole2", 1), ("wood_pole2", 3), ("wood_pole", 4.5)]        # 0..5, under the thatch
    for key, line in sides.items():
        full = key in ("F", "K")
        for u in (range(0, 11, 2) if full else range(2, 9, 2)):            # corners belong to the F/K faces
            for n, y in (tall if full and u not in (0, 10) else low):
                line.put(p, 10, n, u, y, O)
        for i, (g, up) in enumerate(zip(ground[key], upper[key])):
            a = 2 * i + 1
            if g not in "DdO":
                line.put(p, 0, "wood_beam", a, 0.2, O)
            line.put(p, 26, "wood_beam", a, H2, O)                         # girt
            if full:
                line.put(p, 46, "wood_beam", a, 2 * H2, O)                 # plate under the gable
            for kind, y0 in ((g, 0), (up, H2)):
                if kind == "S" and i in (0, 4):
                    line.put(p, 32, "wood_beam_45", a, y0 + 1, O, 0 if i == 0 else 180)


def _house_stairs(p):
    for k in range(3):
        p.add(12, "wood_stair", -4, 0.08 + k, -2 + 2 * k, 180)
        p.add(26, "wood_beam_26", -3, 1.0 + k, -2 + 2 * k, 270)          # handrail
    p.add(12, "wood_pole", -3, 0.5, -3)
    for z in (-3, -1, 1):
        p.add(33, "wood_pole", -3, H2 + 0.5, z)
    for x, z, yaw in ((-3, -2, 90), (-3, 0, 90), (-4, -3, 0)):
        p.add(34, "wood_beam", x, H2 + 0.9, z, yaw)                       # rail round the stairwell


def _porch(p):
    """Log columns from the ground up to the balcony rail, a log beam under the balcony edge, the rail."""
    for x in (-5, -1, 1, 5):
        p.add(11, "wood_pole_log_4", x, 1.78, -7)                         # 4.44 m log: ground to the rail top
    for x0 in (-5, 1):
        p.add(20, "wood_wall_log_4x0.5", x0 + 2, H2 - 0.33, -7)          # beam under the balcony edge
    p.add(20, "wood_wall_log", 0, H2 - 0.33, -7)
    for x in (-4, -2, 0, 2, 4):
        p.add(35, "wood_beam", x, H2 + 0.95, -7)                          # front rail, two bars
        p.add(35, "wood_beam", x, H2 + 0.45, -7)
    for x in (-5, 5):
        p.add(35, "wood_beam", x, H2 + 0.95, -6, 90)                      # side rails
        p.add(35, "wood_beam", x, H2 + 0.45, -6, 90)


# ---------------------------------------------------------------- the working buildings and the hall
def mead_hall():
    """14 x 30 m hall, 4 m walls, a 6 m triple gate in the middle of the long side (local +x), two hearths under
    raised ridge vents, two rows of log posts; brewing at the -z end, the high seat at the +z end (furnish.py)."""
    posts = [("wood_pole_log_4", sx, 2.0, z, 0) for sx in (-4, 4) for z in (-11, -3, 3, 11)]   # the upper logs never held
    return building(14, 30, 4, {"F": "SWSWSWS", "K": "SWSWSWS", "Wst": "SWSWSWSSSWSWSWS", "E": "SWSWSWDDDWSWSWS"},
                    vents=(-13, -7, -5, 5, 7), inside=posts + furnish.mead_hall())


def kitchen():
    """10 x 14 m open cooking hall on a stone floor: solid back, open front and sides, a ridge vent over the fires."""
    return building(10, 14, 3, {"F": "OOOOO", "K": "SSSSS", "Wst": "SLLLLLS", "E": "SLLLLLS"}, floor="stone",
                    vents=(-5, -1, 1, 5), inside=furnish.kitchen())


def food_store():
    """10 x 12 m closed storehouse with a door in the front gable."""
    return building(10, 12, 3, {"F": "SSDSS", "K": "SSSSS", "Wst": "SWSSWS", "E": "SWSSWS"},
                    inside=furnish.food_store())


def blacksmith():
    """14 x 18 m forge hall on stone, open front, low side walls, vents over the forge."""
    return building(14, 18, 4, {"F": "OOOOOOO", "K": "SSSSSSS", "Wst": "SLLLLLLLS", "E": "SLLLLLLLS"}, floor="stone",
                    vents=(-1, 1, 5, 7), inside=furnish.blacksmith())


def smelting_yard():
    """10 x 18 m roofed yard on stone, open on three sides, vents over the smelters and kilns."""
    return building(10, 18, 4, {"F": "OOOOO", "K": "SSSSS", "Wst": "OOOOOOOOO", "E": "OOOOOOOOO"}, floor="stone",
                    vents=(-7, -5, -1, 1, 5, 7), inside=furnish.smelting_yard())


def barn():
    """14 x 22 m barn kept as the storage hall: a 6 m triple gate in the front gable, side doors, a loft over the back
    half (stepladder), chests everywhere (furnish.barn)."""
    p = building(14, 22, 4, {"F": "SSDDDSS", "K": "SSSSSSS", "Wst": "SWSWSDSWSWS", "E": "SWSWSDSWSWS"})
    for x in spans(-7, 7):
        for z in spans(1, 11):
            p.add(25, "wood_floor", x, 4.0, z)                            # loft floor
        p.add(24, "wood_beam", x, 3.66, 1)                                # its front beam
    for x in (-3, 3):
        p.add(24, "wood_pole_log_4", x, 1.78, 1)                          # loft posts
    p.add(26, "wood_stepladder", 0, 0.05, -2.0, 180)                      # up the centre aisle to the loft edge at z 1
    p.add(26, "wood_stepladder", 0, 2.05, 0.0, 180)
    for n, x, y, z, yaw in furnish.barn():
        p.add(Plan.KEEP + 10, n, x, y, z, yaw)
    return p


def pen():
    """12 x 12 m animal pen: roundpole fence all round, a gate in the front, a lean-to shelter in the back corner."""
    p = Plan()
    fence_rect(p, -6, -6, 6, 6, gate_at=-1)
    for x in (1, 5):
        p.add(20, "wood_pole_log", x, 1.0, 1)                             # front posts, top 2.1
        p.add(20, "wood_pole_log_4", x, 1.78, 5)                          # back posts, top 4.0
    for x in (2, 4):
        p.add(30, "wood_roof", x, 2.1, 2, 180)                            # lean-to rising to the back fence
        p.add(31, "wood_roof", x, 3.1, 4, 180)
    return p
