"""Prints the markdown tables the codex's sound pages quote (codex/sfx/*.md), straight from codex/data/sfx.json, so
the pages can be refreshed after a game update: run it, paste the tables over the old ones, reread the words.

    AssetWorkshop/out/venv/Scripts/python AssetWorkshop/codex/measure/sfx_tables.py archetypes [prefix]
    AssetWorkshop/out/venv/Scripts/python AssetWorkshop/codex/measure/sfx_tables.py creatures
    AssetWorkshop/out/venv/Scripts/python AssetWorkshop/codex/measure/sfx_tables.py mixer
"""
import json
import os
import sys

import numpy as np

DATA = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "data", "sfx.json")
VOICE = ("idle", "alert", "attack", "hurt", "death")


def load():
    with open(DATA, encoding="utf-8") as handle:
        return json.load(handle)


def mid(stats, key, digits=2, scale=1.0):
    """'median (p25..p75)' of one statistic, or '-'."""
    s = stats.get(key)
    if not s:
        return "-"
    fmt = f"{{:.{digits}f}}"
    return f"{fmt.format(s['median'] * scale)} ({fmt.format(s['p25'] * scale)}..{fmt.format(s['p75'] * scale)})"


def top(counts):
    """The most common value of a {value: count} table."""
    return max(counts, key=counts.get) if counts else "-"


def archetype_row(key, entry):
    s, shared = entry["stats"], entry["shared"]
    pitch = f"{s['pitch_min']['median']:.2f}-{s['pitch_max']['median']:.2f}" if "pitch_min" in s else "-"
    volume = f"{s['vol_min']['median']:.2f}-{s['vol_max']['median']:.2f}" if "vol_min" in s else "-"
    return (f"| {key[4:]} | {entry['sounds']}/{entry['clips']} | {mid(s, 'length_s')} | {mid(s, 'played_lufs', 1)} | "
            f"{mid(s, 'attack_s', 2)} | {mid(s, 'tail_s', 2)} | {mid(s, 'centroid_hz', 0)} | {mid(s, 'flatness')} | "
            f"{s['low']['median']:.2f}/{s['mid']['median']:.2f}/{s['high']['median']:.2f} | "
            f"{s['clips']['median']:.0f} | {pitch} | {volume} | {mid(s, 'max_distance', 0)} | "
            f"{top(shared['rolloff'])} | {top(shared['mixer'])} |")


def archetypes(prefix=""):
    data = load()
    print("| archetype | sounds/clips | length s | played LUFS | attack s | tail s | centroid Hz | flatness | "
          "low/mid/high | variations | pitch | volume | reach m | roll-off | group |")
    print("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |")
    for key, entry in data["categories"].items():
        if key.startswith("sfx." + prefix) and entry["stats"].get("low"):
            print(archetype_row(key, entry))


def references(prefix=""):
    """The reference sounds of each archetype, by name."""
    data = load()
    for key, entry in data["categories"].items():
        if key.startswith("sfx." + prefix):
            names = [r.split("/")[-1].replace(".prefab", "") for r in entry["references"]]
            print(f"- `{key}`: " + ", ".join(f"`{n}`" for n in names))


def voice_numbers(sounds):
    """Median length, played level, centroid, flatness and pitch over a role's own sounds."""
    def median(key):
        values = [s[key] for s in sounds if isinstance(s.get(key), (int, float))]
        return float(np.median(values)) if values else None
    return median("length_s"), median("played_lufs"), median("centroid_hz"), median("flatness"), median("pitch_hz")


def creatures():
    data = load()
    print("| creature | roles | idle / alert / attack / hurt / death: length s, played LUFS, centroid Hz, flatness |")
    print("| --- | --- | --- |")
    for folder, roles in data["creatures"].items():
        cells = []
        for role in VOICE:
            own = [s for s in roles.get(role, []) if s["prefab"].startswith(f"Characters/{folder}/")]
            if own:
                length, level, centroid, flat, _ = voice_numbers(own)
                cells.append(f"{role} {length:.1f}, {level:.0f}, {centroid:.0f}, {flat:.2f}"
                             if None not in (length, level, centroid, flat) else role)
        print(f"| {folder} | {', '.join(sorted(roles))} | {'; '.join(cells) or '-'} |")


def mixer():
    data = load()
    for group in data["mixer"]["groups"]:
        volumes = ", ".join(f"{k} {v:+.1f} dB" for k, v in group["volume_db"].items() if v is not None)
        print(f"- `{group['path']}`: {volumes}")
        for effect in group["effects"]["Default"]:
            params = ", ".join(f"{k} {v:g}" for k, v in effect["params"].items() if v is not None)
            print(f"  - {effect['effect']}: {params}")


if __name__ == "__main__":
    command = sys.argv[1] if len(sys.argv) > 1 else "archetypes"
    rest = sys.argv[2] if len(sys.argv) > 2 else ""
    {"archetypes": archetypes, "references": references}.get(command, lambda _: None)(rest) \
        if command in ("archetypes", "references") else {"creatures": creatures, "mixer": mixer}[command]()
