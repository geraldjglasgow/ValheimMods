"""Helpers for filming in the running game through DevBridge's director (/shot, /cue, /rec, /cast, /scout).

Shots are plain dicts (see REFERENCE.md, "Filming"); this module sends them, waits for them, moves the player between
sets, and turns takes into contact sheets to look at. DEVBRIDGE_PORT picks the bridge (default 7780).
"""
import json
import os
import subprocess
import time
import urllib.parse
import urllib.request

PORT = int(os.environ.get("DEVBRIDGE_PORT", "7780"))
MAIN = int(os.environ.get("DEVBRIDGE_MAIN", str(PORT)))
FOOTAGE = os.environ.get("TRAILER_FOOTAGE", r"C:\Users\gglasgow\Videos\ValheimTrailers")


def _url(path, port=None, **args):
    query = urllib.parse.urlencode({k: v for k, v in args.items() if v is not None})
    return f"http://127.0.0.1:{port or PORT}{path}" + (f"?{query}" if query else "")


def get(path, port=None, timeout=60, **args):
    with urllib.request.urlopen(_url(path, port, **args), timeout=timeout) as reply:
        return reply.read().decode("utf-8")


def get_json(path, port=None, timeout=60, **args):
    return json.loads(get(path, port, timeout, **args))


def post(path, body, port=None, http_timeout=60, **args):
    data = json.dumps(body).encode("utf-8")
    request = urllib.request.Request(_url(path, port, **args), data=data, headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=http_timeout) as reply:
            return json.loads(reply.read().decode("utf-8"))
    except urllib.error.HTTPError as error:
        raise RuntimeError(f"{path}: {error.code} {error.read().decode('utf-8', 'replace')[:800]}") from None


def ev(expr):
    """One /eval expression; returns the value text after 'Type = '."""
    text = get("/eval", MAIN, expr=expr)
    return text.split(" = ", 1)[1].strip().strip('"') if " = " in text else text


def console(cmd):
    return get("/console", MAIN, cmd=cmd)


def status():
    return get_json("/status", MAIN)


def cue(*cues, origin=None, yaw=0):
    body = {"cues": list(cues)}
    if origin is not None:
        body.update(origin=list(origin), yaw=yaw)
    reply = post("/cue", body)
    if reply.get("errors"):
        raise RuntimeError("cue errors: " + "; ".join(reply["errors"]))
    return reply


def player_pos():
    return status()["player"]["position"]


def ground(x, z):
    return float(ev(f"ZoneSystem.instance.GetGroundHeight($v3({x},0,{z}))"))


def teleport(x, z, y=None, yaw=0, settle=4.0):
    """Moves the player (and the zones loaded around them) to x,z; waits until the world there has loaded."""
    target_y = y if y is not None else max(30.0, float(ev(f"WorldGenerator.instance.GetHeight({x},{z})"))) + 0.5
    ev(f"$player.TeleportTo($v3({x},{target_y},{z}), Quaternion.Euler(0,{yaw},0), true)")
    deadline = time.time() + 60
    while time.time() < deadline:
        time.sleep(1.0)
        s = status()
        px, _, pz = s["player"]["position"]
        if s["state"] == "ingame" and abs(px - x) < 3 and abs(pz - z) < 3 and ev("$player.IsTeleporting()") == "False":
            break
    time.sleep(settle)
    return player_pos()


def shot(spec, mode="preview", timeout=600):
    """Plays a shot. mode: 'rehearse' (no recording), 'preview' (1280x720, quick), 'final' (2560x1440)."""
    args = {"wait": 1, "timeout": timeout}
    if mode == "rehearse":
        args["record"] = 0
    elif mode == "preview":
        args.update(size="1280x720", quality=24, out=take_path(spec["name"], "preview"))
    else:
        args.update(size="2560x1440", quality=16, out=take_path(spec["name"], "final"))
    return post("/shot", spec, http_timeout=timeout + 10, **args)


def take_path(name, kind):
    folder = os.path.join(FOOTAGE, kind)
    os.makedirs(folder, exist_ok=True)
    return os.path.join(folder, name).replace("\\", "/")


def ffmpeg():
    base = os.path.expandvars(r"%LOCALAPPDATA%\Microsoft\WinGet\Packages")
    for root, _, files in os.walk(base):
        if "ffmpeg.exe" in files:
            return os.path.join(root, "ffmpeg.exe")
    return "ffmpeg"


def sheet(video, out_png, fps=2.0, cols=4, width=400):
    """A contact sheet of a take: one frame every 1/fps seconds, cols across."""
    probe = subprocess.run([ffmpeg().replace("ffmpeg.exe", "ffprobe.exe"), "-v", "error", "-show_entries", "format=duration",
                            "-of", "default=nw=1:nk=1", video], capture_output=True, text=True)
    seconds = float(probe.stdout.strip() or 1)
    count = max(1, int(seconds * fps))
    rows = (count + cols - 1) // cols
    label = ("drawtext=fontfile='C\\:/Windows/Fonts/arial.ttf':text='%{pts\\:hms}':x=6:y=6:fontsize=18:fontcolor=white:"
             "box=1:boxcolor=black@0.5")
    vf = f"fps={fps},scale={width}:-1,{label},tile={cols}x{rows}"
    subprocess.run([ffmpeg(), "-y", "-loglevel", "error", "-i", video, "-vf", vf, "-frames:v", "1", out_png], check=True)
    return out_png


def wait_for(path, seconds=30):
    deadline = time.time() + seconds
    while time.time() < deadline and not os.path.exists(path):
        time.sleep(0.5)
    time.sleep(1.0)
    return os.path.exists(path)
