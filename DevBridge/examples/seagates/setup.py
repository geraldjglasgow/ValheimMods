#!/usr/bin/env python3
"""Sets up an in-game test of Wayfare's sea gates ("boat portal anywhere": every gate reaches every other, the
helmsman picks the destination on the map) through DevBridge (http://127.0.0.1:7780).

  python setup.py              build every gate in sites.json that is not built yet, then a Karve at the first gate
  python setup.py --check      read every gate's pillars: paired, both ways, name, usable sides
  python setup.py --remove     take every test gate's pillars and the test boat down (the ground stays dug)

Each site gets a pool 44 x 44 m levelled 3 m under the sea with EarthWright's admin terrain command (game limit 8 m
from the generated ground), two pads 1.7 m deep (a pillar may stand at most 2 m deep) 12 m apart along world x, and
the two Sea Gate Pillars on them; the gate then opens by itself (Wayfare retries every 3 s). Ships pass along world
z. The anchor pillar gets the site's name. The player is moved to each site (teleport, then placed on each pad) and
ends beside the Karve in front of the first gate, facing it. Needs: the LocalTesting profile in a world, devcommands
(single player or an admin), EarthWright and Wayfare loaded, sea gates on. God mode is switched on while it runs and
back off afterwards if it was off.
"""
import json, math, os, sys, time, urllib.error, urllib.parse, urllib.request

BRIDGE = "http://127.0.0.1:7780"
HERE = os.path.dirname(os.path.abspath(__file__))
PILLAR = "WF_SeaGatePillar"
BOAT = "Karve"
POOL_HALF, POOL_Y = 22, 27.0          # 3 m of water over the whole pool
PAD_HALF, PAD_Y = 2.5, 28.3           # 1.7 m: within the pillar's 2 m footing
GAP = 6.0                             # pillars at x - 6 and x + 6: 12 m apart (gate width 10 to 15)
BOAT_OFFSET = -22.0                   # the Karve waits this far south of the gate, bow north


def get(path, **q):
    url = BRIDGE + path + ("?" + urllib.parse.urlencode(q) if q else "")
    return urllib.request.urlopen(url, timeout=120).read().decode()


def ev(expr):
    try:
        return get("/eval", expr=expr).strip()
    except urllib.error.HTTPError as x:
        return "ERR " + x.read().decode()[:300]


def val(expr):
    r = ev(expr)
    if r.startswith("ERR") or " = " not in r:
        raise RuntimeError(f"{expr}: {r}")
    return r.split(" = ", 1)[1].strip('"')


def console(cmd):
    return get("/console", cmd=cmd, wait="0.5").strip()


def status():
    return json.loads(get("/status"))


def nearby(radius, x, y, z, filt=""):
    return json.loads(get("/nearby", radius=str(radius), at=f"{x},{y},{z}", filter=filt, limit="200"))


def zdo(zid, keys):
    return json.loads(get("/zdo", id=zid, keys=",".join(keys)))


def sites():
    with open(os.path.join(HERE, "sites.json")) as f:
        return json.load(f)


# ---------------------------------------------------------------- moving the player
def ground(x, z):
    return float(val(f"ZoneSystem.instance.GetGroundHeight($v3({x:.2f},0,{z:.2f}))"))


def loaded(x, z):
    return val(f"Heightmap.FindHeightmap($v3({x:.2f},0,{z:.2f})) != null") == "True"


def teleport(x, z):
    """A distant teleport (the game's own fade and zone loading), then wait until the ground there is loaded."""
    ev(f"$player.TeleportTo($v3({x:.2f},{40:.2f},{z:.2f}), Quaternion.Euler(0,0,0), true)")
    for _ in range(90):
        time.sleep(1)
        s = status()
        p = s["player"]["position"]
        if s["state"] == "ingame" and math.hypot(p[0] - x, p[2] - z) < 5 and loaded(x, z) and loaded(x + POOL_HALF, z + POOL_HALF):
            time.sleep(3)
            return
    sys.exit(f"the player did not arrive at {x},{z}")


def put_player(x, z):
    """Set the player straight onto a point above the water (terrain commands work around the player's x and z)."""
    y = max(ground(x, z), 31.5) + 2
    for part in ("transform.position", "m_body.position"):
        ev(f"$player.{part} = $v3({x:.3f},{y:.3f},{z:.3f})")
    ev("$player.m_body.linearVelocity = $v3(0,0,0)")
    time.sleep(0.3)


def terrain(cmd, x, z):
    put_player(x, z)
    out = console(cmd)
    if "sent" not in out:
        sys.exit(f"'{cmd}' at {x},{z} was refused: {out}")
    time.sleep(0.5)


# ---------------------------------------------------------------- one gate
def pillars_at(x, z):
    return [o for o in nearby(GAP + 6, x, 30, z, PILLAR) if o["prefab"] == PILLAR]


def dig(site):
    x, z = site["x"], site["z"]
    put_player(x, z)
    print(console("ew forestry 34"))
    print(console("ew debris 34"))
    terrain(f"ew terrain level {POOL_HALF} {POOL_Y} shape=square unlimited", x, z)
    for px in (x - GAP, x + GAP):
        terrain(f"ew terrain level {PAD_HALF} {PAD_Y} shape=square unlimited", px, z)
    time.sleep(4)
    print(f"  ground: pool {ground(x, z + 12):.2f}, pads {ground(x - GAP, z):.2f} / {ground(x + GAP, z):.2f} (sea 30)")


def place_pillars(site):
    x, z = site["x"], site["z"]
    for px in (x - GAP, x + GAP):
        y = ground(px, z)
        r = ev(f'$player.PlacePiece($prefab("{PILLAR}").Piece, $v3({px:.2f},{y:.3f},{z:.2f}), Quaternion.Euler(0,0,0), false)')
        if r.startswith("ERR"):
            sys.exit(f"placing a pillar failed: {r}")


def wait_paired(site, timeout=20):
    deadline = time.time() + timeout
    while time.time() < deadline:
        state = gate_state(site)
        if state["paired"]:
            return state
        time.sleep(1.5)
    return gate_state(site)


def gate_state(site):
    """Both pillars, their ids and partners: paired only when each names the other."""
    found = pillars_at(site["x"], site["z"])
    info = [zdo(o["id"], ["wf_sg_id", "wf_sg_partner", "wf_sg_sides", "wf_sg_name"]) for o in found]
    vals = [i.get("values", i) for i in info]
    ids = [str(v.get("wf_sg_id")) for v in vals]
    partners = [str(v.get("wf_sg_partner")) for v in vals]
    paired = len(found) == 2 and partners[0] == ids[1] and partners[1] == ids[0] and ids[0] not in ("None", "0")
    return {"pillars": [o["id"] for o in found], "values": vals, "paired": paired}


def name_gate(site, state):
    """Both pillars get the name (only the anchor's is read; writing both needs no anchor lookup)."""
    for zid in state["pillars"]:
        ev(f'ZDOMan.instance.GetZDO($zdo("{zid}")).Set("wf_sg_name", "{site["name"]}")')


def build(site):
    print(f"{site['name']} at {site['x']},{site['z']}")
    teleport(site["x"], site["z"])
    if len(pillars_at(site["x"], site["z"])) < 2:
        dig(site)
        place_pillars(site)
    state = wait_paired(site)
    if state["paired"]:
        name_gate(site, state)
    print(f"  pillars {state['pillars']}: {'PAIRED' if state['paired'] else 'NOT paired'} {state['values']}")
    return state["paired"]


# ---------------------------------------------------------------- the boat and the end
def boat(site):
    x, z = site["x"], site["z"] + BOAT_OFFSET
    if [o for o in nearby(8, x, 30, z, BOAT) if o["prefab"] == BOAT]:
        return
    r = ev(f'UnityEngine.Object.Instantiate($prefab("{BOAT}"), $v3({x:.2f},30.4,{z:.2f}), Quaternion.Euler(0,0,0))')
    print(f"  {BOAT}: {r[:80]}")


def finish(site):
    """The player stands on the Karve's deck, facing the gate."""
    x, z = site["x"], site["z"] + BOAT_OFFSET
    teleport(x + 0.5, z)
    for part in ("transform.position", "m_body.position"):
        ev(f"$player.{part} = $v3({x:.3f},32.2,{z:.3f})")
    ev("$player.transform.rotation = Quaternion.Euler(0,0,0)")


def with_god(action):
    was = status()["player"]["god"]
    console("devcommands") if not status()["player"].get("devcommands") else None
    if not was:
        console("god")
    try:
        action()
    finally:
        if not was and status()["player"]["god"]:
            console("god")


def cmd_setup():
    ss = sites()
    results = []
    for site in ss:
        results.append(build(site))
    boat(ss[0])
    finish(ss[0])
    print(f"gates paired: {sum(results)} of {len(ss)}; {BOAT} waiting at {ss[0]['name']}")


def cmd_check():
    for site in sites():
        teleport(site["x"], site["z"])
        state = gate_state(site)
        print(f"{site['name']}: {'PAIRED' if state['paired'] else 'NOT paired'} {state['values']}")


def cmd_remove():
    ss = sites()
    for site in ss:
        teleport(site["x"], site["z"])
        for o in pillars_at(site["x"], site["z"]):
            ev(f'ZNetScene.instance.Destroy(ZNetScene.instance.FindInstance($zdo("{o["id"]}")))')
    for o in nearby(10, ss[0]["x"], 30, ss[0]["z"] + BOAT_OFFSET, BOAT):
        ev(f'ZNetScene.instance.Destroy(ZNetScene.instance.FindInstance($zdo("{o["id"]}")))')
    print("test gates and boat removed (the pools stay dug)")


if __name__ == "__main__":
    mode = sys.argv[1] if len(sys.argv) > 1 else ""
    with_god({"--check": cmd_check, "--remove": cmd_remove}.get(mode, cmd_setup))
