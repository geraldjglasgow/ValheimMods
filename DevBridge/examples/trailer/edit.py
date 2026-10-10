"""Cuts the trailer from its takes with ffmpeg: grade, cuts, black, title cards and the sound laid over them.

    python edit.py ending [preview|final]     the ending: the take, black with the squeal dying away, the title

Each take's .json (written by ecp_trailer.py) gives the film times of its marks, so sound lands on the picture.
"""
import json
import os
import subprocess
import sys

import director as d

GRADE = "eq=contrast=1.16:saturation=1.3:brightness=-0.025:gamma=0.97"


def take(kind, name):
    base = os.path.join(d.FOOTAGE, kind, name)
    with open(base + ".json", encoding="utf-8") as f:
        info = json.load(f)
    return base + ".mp4", base + ".wav", info


def mark(info, label):
    return next(f["t"] for f in info["fired"] if f.get("mark") == label)


def size_of(video):
    out = subprocess.run([d.ffmpeg().replace("ffmpeg.exe", "ffprobe.exe"), "-v", "error", "-select_streams", "v:0",
                          "-show_entries", "stream=width,height", "-of", "csv=p=0", video], capture_output=True, text=True)
    w, h = out.stdout.strip().split(",")
    return int(w), int(h)


def ending(kind="preview", black=2.0, title_hold=7.0, squeal_lead=0.25):
    """The take until the boar takes the lens; black for `black` s while the squeal dies away; the title."""
    video, _, info = take(kind, "ending")
    _, squeal_wav, squeal = take(kind, "squeal_stem")
    title, _, _ = take(kind, "title_ecp")
    cut = info["time"]
    start = mark(info, "charge") - squeal_lead            # the squeal starts a beat before the run
    s0 = mark(squeal, "squeal") + 0.05                      # where the squeal begins in its own take
    total = cut + black + title_hold
    w, h = size_of(video)
    fade_at = cut - start                                   # into the squeal: the picture goes black there
    graph = ";".join([
        f"[0:v]trim=0:{cut},setpts=PTS-STARTPTS,{GRADE},format=yuv420p[v0]",
        f"color=black:s={w}x{h}:r=60:d={black},format=yuv420p[vb]",
        f"[2:v]tpad=stop_mode=clone:stop_duration={title_hold},trim=0:{title_hold},setpts=PTS-STARTPTS,format=yuv420p[vt]",
        "[v0][vb][vt]concat=n=3:v=1:a=0[v]",
        f"[0:a]atrim=0:{cut},asetpts=PTS-STARTPTS,volume=2.8,apad=whole_dur={total}[ag]",
        f"[1:a]atrim={s0}:{s0 + 3.0},asetpts=PTS-STARTPTS,volume=7.0,"
        f"afade=t=out:st={fade_at}:d={black},aecho=0.8:0.6:60|120:0.25|0.15,"
        f"adelay={int(start * 1000)}:all=1,apad=whole_dur={total}[as]",
        "[ag][as]amix=inputs=2:normalize=0:duration=longest,alimiter=limit=0.97[a]",
    ])
    out = os.path.join(d.FOOTAGE, kind, "ending_cut.mp4")
    subprocess.run([d.ffmpeg(), "-y", "-loglevel", "error", "-i", video, "-i", squeal_wav, "-i", title,
                    "-filter_complex", graph, "-map", "[v]", "-map", "[a]", "-t", str(total),
                    "-c:v", "libx264", "-preset", "slow", "-crf", "17", "-pix_fmt", "yuv420p",
                    "-colorspace", "bt709", "-color_primaries", "bt709", "-color_trc", "bt709",
                    "-c:a", "aac", "-b:a", "320k", "-movflags", "+faststart", out], check=True)
    return out


def duration(video):
    out = subprocess.run([d.ffmpeg().replace("ffmpeg.exe", "ffprobe.exe"), "-v", "error", "-show_entries",
                          "format=duration", "-of", "csv=p=0", video], capture_output=True, text=True)
    return float(out.stdout.strip())


# The trailer as one shot: each take with the dissolve that hides its join to the next (0 = straight on: black to black).
GAME_LIFT, MUSIC_LEVEL = 2.2, 0.6                   # the game's sound runs quiet; the score sits just under it
CHAIN = [("crypt_sequence", 0.0), ("rime_giant", 0.0), ("kraken", 0.3), ("gear_montage", 0.0), ("ending_cut", None)]


def chain(kind="preview"):
    """Every take so far joined into one film, each join a short dissolve inside a matched movement."""
    files = [os.path.join(d.FOOTAGE, kind, name + ".mp4") for name, _ in CHAIN]
    files = [f for f in files if os.path.exists(f)]
    norm = "fps=60,scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,setsar=1,format=yuv420p,settb=AVTB"
    graph = [f"[{i}:v]{norm}{',' + GRADE if not f.endswith('_cut.mp4') else ''}[v{i}];[{i}:a]aresample=48000,aformat=channel_layouts=stereo[a{i}]"
             for i, f in enumerate(files)]
    v, a, at = "[v0]", "[a0]", duration(files[0])
    for i in range(1, len(files)):
        fade = CHAIN[i - 1][1] or 0.04
        graph.append(f"{v}[v{i}]xfade=transition=fade:duration={fade}:offset={at - fade:.3f}[vx{i}]")
        graph.append(f"{a}[a{i}]acrossfade=d={fade}:c1=tri:c2=tri[ax{i}]")
        v, a, at = f"[vx{i}]", f"[ax{i}]", at - fade + duration(files[i])
    score = os.path.join(d.FOOTAGE, kind, "score.wav")
    if os.path.exists(score):                      # music.py's score under the game's sound (lifted until the ending)
        ending = at - duration(files[-1]) if files[-1].endswith("_cut.mp4") else at
        graph.append(f"{a}volume={GAME_LIFT}:enable='lt(t,{ending:.2f})'[ag];[{len(files)}:a]volume={MUSIC_LEVEL}[am];"
                     "[ag][am]amix=inputs=2:normalize=0:duration=first,alimiter=limit=0.95[amix]")
        a = "[amix]"
        files = files + [score]
    out = os.path.join(d.FOOTAGE, kind, "trailer_so_far.mp4")
    args = [d.ffmpeg(), "-y", "-loglevel", "error"] + sum((["-i", f] for f in files), [])
    subprocess.run(args + ["-filter_complex", ";".join(graph), "-map", v, "-map", a, "-c:v", "libx264", "-preset", "medium",
                           "-crf", "18", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "256k", "-movflags", "+faststart", out], check=True)
    return out


if __name__ == "__main__":
    what = sys.argv[1] if len(sys.argv) > 1 else "ending"
    kind = sys.argv[2] if len(sys.argv) > 2 else "preview"
    print(globals()[what](kind))
