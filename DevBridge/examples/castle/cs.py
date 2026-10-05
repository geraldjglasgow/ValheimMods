# shared helpers for the castle scripts
import json, math, time, urllib.request, urllib.parse, urllib.error
B = "http://127.0.0.1:7780"
CX, CZ, GY = 572.0, -1265.0, 55.0
ME = "1591727155"
def get(path, **q): return urllib.request.urlopen(B + path + ("?" + urllib.parse.urlencode(q) if q else ""), timeout=120).read().decode()
def ev(e):
    try: return get("/eval", expr=e)
    except urllib.error.HTTPError as x: return "ERR " + x.read().decode()[:160]
def val(e):
    r = ev(e).strip(); return r.split(" = ", 1)[1].strip('"') if " = " in r else "ERR " + r
def status(): return json.loads(get("/status"))
def console(c): return get("/console", cmd=c).strip()
def go(x, y, z, settle=4):
    ev(f"$player.TeleportTo($v3({x},{y},{z}), Quaternion.Euler(0,0,0), true)")
    for _ in range(90):
        time.sleep(1); s = status(); p = (s.get("player") or {}).get("position") or [0, 0, 0]
        if s["state"] == "ingame" and math.hypot(p[0] - x, p[2] - z) < 3: break
    time.sleep(settle)
def place(n, x, y, z, yaw=0):
    return ev(f'$player.PlacePiece($prefab("{n}").Piece, $v3({x:.3f},{y:.3f},{z:.3f}), Quaternion.Euler(0,{yaw},0), false)')
def nearby(r, x=CX, z=CZ, y=GY, limit=6000): return json.loads(get("/nearby", radius=str(r), at=f"{x},{y},{z}", limit=str(limit)))
def destroy(o): ev(f'ZNetScene.instance.Destroy(ZNetScene.instance.FindInstance($zdo("{o["id"]}")).gameObject)')
