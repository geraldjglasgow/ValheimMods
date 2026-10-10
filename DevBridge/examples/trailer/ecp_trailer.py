"""Elite Creatures Pack trailer: plays the scene files (DevBridge Studio's Scenes tab edits the same files).

    python ecp_trailer.py list
    python ecp_trailer.py rehearse <scene> [<scene> ...]   setup, then the shot live, nothing recorded
    python ecp_trailer.py preview <scene> [<scene> ...]    setup, then the shot recorded at 1280x720 (+ contact sheet)
    python ecp_trailer.py final <scene> [<scene> ...]      setup, then the shot recorded at 2560x1440

Scenes live in DevBridge's scenes folder (config Director / Scenes folder; by default Videos/ValheimTrailers/scenes):
one JSON per scene with its notes, setup steps and timed cues. The bridge runs setup and shot itself and writes each
take's record (what fired when) beside the video, which edit.py reads.
"""
import sys
import time

import director as d


def scenes():
    return d.get_json("/studio/scenes")["scenes"]


def play(name, mode):
    print(f"== {name}: {mode}")
    d.get("/studio/scene/play", name=name, mode=mode)
    state = {}
    while True:
        time.sleep(1.0)
        state = d.get_json("/studio/scene/status")
        if not state["busy"]:
            break
    shot = state.get("shot") or {}
    print({k: shot.get(k) for k in ("state", "time", "cuesFired", "errors")}, state.get("problems") or "")
    rec = shot.get("recording") or {}
    for _ in range(120):  # the sound is muxed in after the shot ends
        if d.get_json("/rec").get("mux") in ("done", None) or not rec.get("file"):
            break
        time.sleep(0.5)
    if rec.get("file") and d.wait_for(rec["file"], 60):
        png = rec["file"].replace(".mp4", "_sheet.png")
        d.sheet(rec["file"], png, fps=3, cols=5, width=360)
        print("take:", rec["file"], "sheet:", png)


if __name__ == "__main__":
    if len(sys.argv) < 2 or sys.argv[1] == "list":
        for s in scenes():
            print(f"{s['order']:>3}  {s['name']:<18} {s['status']:<9} {s['length']:>5.1f}s  {s['title']}")
    else:
        for scene in sys.argv[2:]:
            play(scene, sys.argv[1])
