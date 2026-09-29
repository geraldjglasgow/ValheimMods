"""Compares a built sound with the game: its clips measured exactly as the codex measured the game's (sfx_features),
a table against the archetype's ranges, and a sheet with the new clips' spectrograms and envelopes in the top row and
the archetype's nearest game clips below (the game's clips are read from the local reference export and drawn into
the gitignored out folder only).

    AssetWorkshop/out/venv/Scripts/python AssetWorkshop/sfx/compare.py rootling     (after build.py)
"""
import json
import os
import sys

import numpy as np

import kit  # noqa: F401  (puts codex/measure on the path)
import sfx_audio
import sfx_clips
import sfx_draw
import sfx_features
from kit import targets

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "out")


def measure(path):
    samples, rate = sfx_audio.read(os.path.abspath(path))
    return sfx_features.measure(samples, rate)


def summarise(numbers):
    """The medians of a sound's variations."""
    keys = [k for k, v in numbers[0].items() if isinstance(v, (int, float)) and not isinstance(v, bool)]
    found = {}
    for key in keys:
        values = [n[key] for n in numbers if isinstance(n.get(key), (int, float))]
        if values:
            found[key] = float(np.median(values))
    found["pitch_hz"] = next((n["pitch_hz"] for n in numbers if n.get("pitch_hz")), None)
    return found


def table(name, archetype, numbers):
    """Markdown rows: each compared feature, our median, the archetype's middle half and range, and a verdict."""
    lines = [f"### {name} against {archetype}", "", "| feature | new | game p25..p75 | game min..max | verdict |",
             "| --- | --- | --- | --- | --- |"]
    for feature, value, middle, whole, verdict in targets.report(numbers, archetype):
        shown = "-" if value is None else f"{value:.3g}"
        lines.append(f"| {feature} | {shown} | {middle[0]:.3g}..{middle[1]:.3g} | {whole[0]:.3g}..{whole[1]:.3g} | "
                     f"{verdict} |")
    return lines


def sheet(sound, files, numbers, clips):
    """The comparison picture: our variations, then the nearest game clips of the archetype."""
    median = summarise(numbers)
    nearest = targets.nearest(median, sound["archetype"], 6, clips)
    span = sfx_draw.span_for([n["length_s"] for n in numbers] + [clips[f]["length_s"] for f in nearest])
    ours = [sfx_draw.clip_tile(os.path.abspath(f), span, n, "new: " + os.path.basename(f)[:-4])
            for f, n in zip(files, numbers)][:6]
    game = [sfx_draw.clip_tile(f, span, clips.get(f), "game: " + os.path.basename(f)[:-4]) for f in nearest]
    blank = sfx_draw.Image.new("RGB", ours[0].size, (0, 0, 0))
    tiles = ours + [blank] * (6 - len(ours)) + game
    title = f"{sound['name']} ({sound['archetype']}): top row new, bottom row the nearest game clips; span {span:.2f} s"
    return sfx_draw.sheet(tiles, title, 6), nearest


def compare(sound, files, folder):
    """Measures the files of one sound, writes its sheet, returns its report lines and numbers."""
    clips = sfx_clips.load_cache()
    numbers = [measure(f) for f in files]
    picture, nearest = sheet(sound, files, numbers, clips)
    picture.save(os.path.join(folder, f"{sound['name']}_compare.png"))
    median = summarise(numbers)
    median["played_lufs"] = median["momentary_max_lufs"] + targets.copy_gain_db(sound["prefab"], sound.get("volume"))
    lines = table(sound["name"], sound["archetype"], median)
    lines += ["", "Nearest game clips: " + ", ".join(os.path.basename(f) for f in nearest), ""]
    if sound.get("loop"):
        lines += [f"Loop seam: {seam(files[0]):+.1f} dB (the step from the last sample to the first against the "
                  "clip's typical step between neighbouring samples; near 0 is seamless, over +12 clicks)", ""]
    return lines, numbers


def seam(path):
    """How big the step is where a loop wraps, against the median step between neighbouring samples, in dB."""
    samples, _ = sfx_audio.read(os.path.abspath(path))
    mono = sfx_audio.mid(samples)
    typical = max(float(np.median(np.abs(np.diff(mono)))), 1e-9)
    return float(20 * np.log10(max(abs(mono[-1] - mono[0]), 1e-9) / typical))


def main(name):
    folder = os.path.join(OUT, name)
    with open(os.path.join(folder, "manifest.json"), encoding="utf-8") as handle:
        manifest = json.load(handle)
    report = [f"# {manifest['set']}: comparison with the game", ""]
    for sound in manifest["sounds"]:
        files = [os.path.join(folder, f"{clip}.wav") for clip in sound["clips"]]
        report += compare(sound, files, folder)[0]
    with open(os.path.join(folder, "report.md"), "w", encoding="utf-8") as handle:
        handle.write("\n".join(report) + "\n")
    print(os.path.join(folder, "report.md"))


if __name__ == "__main__":
    main(sys.argv[1])
