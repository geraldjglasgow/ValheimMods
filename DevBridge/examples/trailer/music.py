"""The trailer's score, written to the film: each take's marks (from its record) placed on the stitched timeline the way
edit.chain joins the takes, so every hit lands on its picture. D minor, 96 bpm.

    python music.py [preview|final]     writes <kind>/score.wav; edit.chain mixes it under the game's sound

The arc: a march (drums, bone rattles, drone, throat-singer) - tension as the crossbow aims - the fight (war drums, horn
theme, strings) - the shatter - cold glass and wind under the sleeping giant - its waking (horns, footstep drums), the
slam, the ice, the fall (into a dazed blur) - the storm at sea at full drive, the grab, the throw in slow motion - under
water into the hold (a heartbeat and a lyre) - the riser into the ballista: a hard stop on black. The ending has no
music; one deep boom under the title.
"""
import json
import os
import sys
import wave

import numpy as np

import director as d
import edit
from music_kit import (SR, Bus, crash, drone, envelope, frame_drum, glass, horn, hz, lowpass_curve, pluck, rattle,
                       reverse_cymbal, staccato, sub_drop, sweep, taiko, throat, tom, tremolo_strings)

BEAT = 60 / 96
THEME = [("D3", 2), ("F3", 1), ("E3", 1), ("D3", 2), ("A2", 2), ("C3", 2), ("D3", 1), ("F3", 1), ("A3", 2), ("G3", 1), ("F3", 1)]
OSTINATO = ["D3", "D3", "F3", "D3", "A3", "D3", "F3", "E3"]


def timeline(kind):
    """Film time of every take's start and of each of its marks, keyed "take.mark"; plus each take's start and end."""
    times, at = {}, 0.0
    for i, (name, _) in enumerate(edit.CHAIN):
        video = os.path.join(d.FOOTAGE, kind, name + ".mp4")
        start = 0.0 if i == 0 else at - (edit.CHAIN[i - 1][1] or 0.04)
        length = edit.duration(video)
        times[name + ".start"], times[name + ".end"] = start, start + length
        record = os.path.join(d.FOOTAGE, kind, name + ".json")
        if os.path.exists(record):
            with open(record, encoding="utf-8") as f:
                for fired in json.load(f).get("fired", []):
                    if fired.get("mark"):
                        times.setdefault(f"{name}.{fired['mark']}", start + fired["t"])
        at = start + length
    return times


def impact(bus, at, size=1.0):
    bus.add(taiko(0.8, 0.9), at, 0.9 * size, verb=0.35)
    bus.add(sub_drop(), at, 0.7 * size, verb=0.1)
    bus.add(crash(1.6), at, 0.35 * size, pan=0.2, verb=0.4)


def roll(bus, start, end, first=0.3, last=0.05, f=110, gain=0.5):
    """Toms speeding up from one hit every `first` s to every `last` s."""
    t, i = start, 0
    while t < end:
        u = (t - start) / max(end - start, 0.01)
        bus.add(tom(f * (1 + 0.15 * u)), t, gain * (0.5 + 0.5 * u), pan=0.3 * (-1) ** i)
        t += first + (last - first) * u
        i += 1


def drums(bus, start, end, gain=0.7, pattern="TtxTTxtx"):
    """War drums in eighths: T a big drum, t a tom, x a frame drum."""
    t, i = start, 0
    while t < end - 0.01:
        hit = pattern[i % len(pattern)]
        if hit == "T":
            bus.add(taiko(1.0 if i % 8 else 0.9), t, gain, verb=0.25)
        elif hit == "t":
            bus.add(tom(120), t, gain * 0.6, pan=-0.3)
        else:
            bus.add(frame_drum(), t, gain * 0.35, pan=0.35)
        t += BEAT / 2
        i += 1


def theme(bus, start, end, gain=0.35, octave_down=True):
    """The horn theme, looped until end, doubled an octave below."""
    t, i = start, 0
    while t < end - 0.05:
        note, beats = THEME[i % len(THEME)]
        length = min(beats * BEAT, end - t)
        f = hz(note)
        bus.add(horn([f, f / 2] if octave_down else [f], length + 0.15, 0.08, 0.15), t, gain, pan=-0.15, verb=0.35)
        t += beats * BEAT
        i += 1


def ostinato(bus, start, end, gain=0.18, sixteenth=False):
    step = BEAT / (4 if sixteenth else 2)
    t, i = start, 0
    while t < end - 0.01:
        bus.add(staccato(hz(OSTINATO[i % len(OSTINATO)])), t, gain, pan=0.25, verb=0.2)
        t += step
        i += 1


# ---------------------------------------------------------------- sections
def crypt(bus, m):
    aim, loosed, hit, shatter = m["crypt_sequence.aim"], m["crypt_sequence.loosed"], m["crypt_sequence.bolt_hit"], m["crypt_sequence.shatter"]
    bus.add(drone([hz("D2"), hz("A2")], shatter + 0.5, 450, 2.5, 0.4), 0.0, 0.22, verb=0.3)
    bus.add(throat(aim + 0.4, hz("D2"), ("u", "o", "a"), whistle=[8, 9, 10, 12, 10, 9]), 0.4, 0.3, verb=0.45)
    t, i = 0.6, 0
    while t < aim - 0.05:                        # the march: a big drum every two beats, bones on every beat
        u = t / aim
        if i % 2 == 0:
            bus.add(taiko(), t, 0.35 + 0.45 * u, verb=0.3)
        if t > 1.2:
            bus.add(rattle(), t, 0.25 + 0.25 * u, pan=float(np.sin(i * 1.7)) * 0.6)
        if t > 2.5 and i % 2 == 1:
            bus.add(frame_drum(), t, 0.15 + 0.15 * u, pan=0.4)
        t += BEAT
        i += 1
    bus.add(taiko(0.9, 0.8), aim, 0.8, verb=0.4)    # it takes aim: the drums stop
    bus.add(tremolo_strings([hz("D4"), hz("A4"), hz("D5")], hit - aim, attack=hit - aim - 0.2), aim, 0.22, verb=0.4)
    bus.add(sub_drop(1.5) * 0.6, loosed, 0.6)
    bus.add(reverse_cymbal(hit - loosed), loosed, 0.8)
    impact(bus, hit, 0.8)
    fight(bus, m, hit, shatter)


def fight(bus, m, start, shatter):
    hurl = m.get("crypt_sequence.hurl", shatter - 1.0)
    bus.add(horn([hz("D2"), hz("A2"), hz("D3")], 2 * BEAT, 0.05, 0.3), start, 0.45, verb=0.35)
    drums(bus, start, hurl, 0.75)
    theme(bus, start + 2 * BEAT, shatter, 0.32)
    ostinato(bus, start, shatter, 0.16)
    for mark, size in (("rear", 0.5), ("spin", 0.8), ("bolt2_hit", 0.5)):
        if f"crypt_sequence.{mark}" in m:
            bus.add(crash(1.0), m[f"crypt_sequence.{mark}"], 0.3 * size, pan=-0.3)
            bus.add(tom(95, 0.3), m[f"crypt_sequence.{mark}"], 0.6 * size)
    roll(bus, hurl, shatter, 0.16, 0.04, 120, 0.55)
    bus.add(sweep(shatter - hurl, 400, 7000) * 0.5, hurl, 0.6)
    impact(bus, shatter, 1.2)


def giant(bus, m):
    shatter, s = m["crypt_sequence.shatter"], m["rime_giant.start"]
    hit, wake, slam, ice, struck = (m[f"rime_giant.{k}"] for k in ("hit", "wake", "slam", "impact", "struck"))
    bus.add(glass([hz("A4"), hz("D5"), hz("E5"), hz("A5")], hit - shatter + 1.5), shatter + 0.1, 0.16, verb=0.6)
    bus.add(drone([hz("D2")], hit - shatter + 1.0, 300, 2.0, 1.0), shatter, 0.12)
    bus.add(throat(hit - s, hz("D2"), ("u", "o", "u")), s + 0.5, 0.16, verb=0.55)
    bus.add(taiko(0.85, 0.7), hit, 0.75)
    bus.add(horn([hz("D2")], 0.9, 0.04, 0.4), hit, 0.35)
    bus.add(horn([hz("D2"), hz("A2"), hz("D3")], slam - wake + 0.4, 0.6, 0.4), wake, 0.45, verb=0.4)
    t = wake
    while t < slam:                                  # its footsteps
        bus.add(taiko(0.75, 0.7), t, 0.7)
        t += 2 * BEAT
    roll(bus, slam, ice, 0.35, 0.06, 95, 0.5)
    bus.add(tremolo_strings([hz("D4"), hz("A4"), hz("D5")], ice - slam, attack=ice - slam - 0.1), slam, 0.25)
    bus.add(sweep(ice - slam, 300, 6000) * 0.5, slam, 0.55)
    impact(bus, ice, 1.2)
    roll(bus, ice + 0.2, struck, 0.16, 0.08, 80, 0.45)    # the ice rolling at us
    bus.add(tremolo_strings([hz("D2"), hz("D3")], struck - ice, attack=struck - ice), ice, 0.3)
    impact(bus, struck, 1.0)
    dazed = m["kraken.start"] + 1.7 - struck
    bus.add(tremolo_strings([hz("D3"), hz("F3"), hz("A3")], dazed + 1.0, attack=1.0, release=1.0, rate=3.0), struck + 0.2, 0.2, verb=0.6)
    bus.add(throat(dazed, hz("D2"), ("o", "u")), struck + 0.4, 0.14, verb=0.6)


def kraken(bus, m):
    k = m["kraken.start"]
    drive, grab, caught, thrown, splash = k + 2.4, m["kraken.grab"], m["kraken.caught"], m["kraken.thrown"], m["kraken.splash"]
    impact(bus, drive, 0.9)
    bus.add(horn([hz("D2"), hz("A2"), hz("D3"), hz("F3")], 2 * BEAT, 0.05, 0.3), drive, 0.5)
    drums(bus, drive, grab, 0.85, "TtTxTTtx")
    theme(bus, drive + 2 * BEAT, grab, 0.36)
    ostinato(bus, drive, grab, 0.16, sixteenth=True)
    for mark in ("slam_bow", "slam_mid"):
        bus.add(crash(1.2), m[f"kraken.{mark}"], 0.35, pan=0.3)
        bus.add(taiko(0.9), m[f"kraken.{mark}"], 0.6)
    t = grab
    while t < caught:                                # the grab: a tense pulse
        bus.add(taiko(1.1, 0.4), t, 0.4)
        t += BEAT
    bus.add(tremolo_strings([hz("A4"), hz("D5"), hz("F5")], caught - grab + 0.2, attack=caught - grab), grab, 0.25)
    impact(bus, caught, 0.6)
    impact(bus, thrown, 1.1)
    held = splash - thrown + 1.0                     # the throw in slow motion: one long chord
    bus.add(horn([hz("D2"), hz("A2"), hz("D3"), hz("F3"), hz("A3")], held, 0.4, 0.8), thrown, 0.4, verb=0.6)
    bus.add(tremolo_strings([hz("D5"), hz("F5"), hz("A5")], held, 0.3, 0.8, rate=7.0), thrown, 0.2, verb=0.6)
    bus.add(throat(held, hz("D2"), ("a", "o")), thrown, 0.2, verb=0.6)


def hold(bus, m):
    h, fire, black = m["gear_montage.start"], m["gear_montage.fire"], m["gear_montage.black"]
    build = fire - 2.0
    bus.add(drone([hz("D2"), hz("A2")], black - h, 350, 1.0, 0.1), h, 0.16)
    t = h + 0.4
    while t < build:                                 # a heartbeat
        bus.add(taiko(0.9, 0.35), t, 0.4, verb=0.2)
        bus.add(taiko(0.95, 0.3), t + 0.22, 0.28, verb=0.2)
        t += 2 * BEAT
    melody = ["D4", "F4", "A4", "G4", "F4", "E4", "D4", "C4", "D4", "F4", "A4", "C5", "A4", "G4", "F4", "E4"]
    t, i = h + 0.6, 0
    while t < build:                                 # the lyre over the weapons
        bus.add(pluck(hz(melody[i % len(melody)])), t, 0.35, pan=-0.3, verb=0.45)
        t += BEAT
        i += 1
    roll(bus, build, fire, 0.3, 0.05, 100, 0.5)
    bus.add(sweep(fire - build, 300, 8000) * 0.5, build, 0.6)
    bus.add(tremolo_strings([hz("D4"), hz("A4"), hz("D5")], fire - build, attack=fire - build), build, 0.28)
    bus.add(reverse_cymbal(fire - build), build, 0.8)
    impact(bus, fire, 1.2)


def title(bus, m):
    """Under the title: a lyre phrase over a quiet throat-singer's hum, folk and not a trailer's boom."""
    at = m["ending_cut.start"] + 9.2
    for i, (note, beat) in enumerate((("D3", 0), ("A3", 1), ("D4", 2), ("F4", 3), ("E4", 4.5), ("D4", 6))):
        bus.add(pluck(hz(note), 3.0), at + beat * BEAT, 0.42, pan=-0.2 + 0.08 * i, verb=0.5)
    bus.add(throat(4.2, hz("D2"), ("u", "o", "u")), at, 0.16, verb=0.55)


def master(bus, m):
    """The mix out of the hall, with the dazed and underwater blurs, normalised, cut dead on the ballista's black and
    silent through the ending until the title."""
    out = bus.render()
    struck, k, splash, h = m["rime_giant.struck"], m["kraken.start"], m["kraken.splash"], m["gear_montage.start"]
    times = [0, struck, struck + 0.6, k + 1.2, k + 2.0, splash, splash + 0.3, h + 0.2, h + 1.8, out.shape[1] / SR]
    cuts = [20000, 20000, 900, 900, 20000, 20000, 350, 350, 6000, 20000]
    out = lowpass_curve(out, times, cuts)
    out *= 0.95 / np.max(np.abs(out))
    black, title_at = int(m["gear_montage.black"] * SR), int((m["ending_cut.start"] + 9.15) * SR)
    out[:, black:title_at] = 0.0
    fade = int(0.012 * SR)
    out[:, black - fade:black] *= np.linspace(1, 0, fade)
    end = int((m["ending_cut.end"] - 0.5) * SR)        # the lyre rings out, then 2 s to silence; the film ends 0.5 s later
    tail = int(2.0 * SR)
    out[:, end - tail:end] *= np.linspace(1, 0, tail) ** 2
    out[:, end:] = 0.0
    return out


def write_wav(path, x):
    pcm = (np.clip(x.T, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


def score(kind="preview"):
    m = timeline(kind)
    bus = Bus(m["ending_cut.end"] + 1.0)
    for part in (crypt, giant, kraken, hold, title):
        part(bus, m)
    path = os.path.join(d.FOOTAGE, kind, "score.wav")
    write_wav(path, master(bus, m))
    return path


if __name__ == "__main__":
    print(score(sys.argv[1] if len(sys.argv) > 1 else "preview"))
