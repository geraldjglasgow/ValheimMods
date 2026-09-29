"""Pictures of sounds for eyes that cannot hear them: a tile per clip with its spectrogram (log frequency, 40 Hz to
16 kHz, 80 dB range under the clip's own peak) over its envelope (RMS in dBFS, -60 to 0, the same scale on every tile,
so loudness compares across tiles), and contact sheets of tiles. Plain numpy and Pillow.

Reading a tile: time runs left to right over the sheet's common span (written at the top of the sheet), so lengths
compare; bright means loud; the white ticks at the left edge mark 100 Hz, 1 kHz and 10 kHz; the envelope's grey lines
are -40 and -20 dBFS.
"""
import numpy as np
from PIL import Image, ImageDraw

import sfx_audio

WIDTH, SPEC_H, ENV_H, TEXT_H = 220, 110, 36, 26
LOW_HZ, HIGH_HZ, RANGE_DB = 40.0, 16000.0, 80.0
COLOURS = np.array([[0, 0, 4], [40, 11, 84], [101, 21, 110], [159, 42, 99], [212, 72, 66], [245, 125, 21],
                    [250, 193, 39], [252, 255, 164]], dtype=float)


def colour(level):
    """0..1 -> RGB on a dark-to-bright heat scale (black, purple, red, orange, pale yellow)."""
    stops = np.linspace(0, 1, len(COLOURS))
    return np.stack([np.interp(level, stops, COLOURS[:, c]) for c in range(3)], axis=-1).astype(np.uint8)


def spectrogram(mono, rate, span_s, width=WIDTH, height=SPEC_H):
    """An RGB array (height, width) of the clip's spectrogram over span_s seconds."""
    hop = max(64, int(span_s * rate / width))
    cut = sfx_audio.frames(mono.astype(np.float32), 1024, hop) * np.hanning(1024).astype(np.float32)
    power = np.abs(np.fft.rfft(cut, axis=1)) ** 2
    freqs = np.fft.rfftfreq(1024, 1 / rate)
    edges = np.geomspace(LOW_HZ, min(HIGH_HZ, rate / 2), height + 1)
    index = np.clip(np.searchsorted(freqs, edges), 1, len(freqs) - 1)
    rows = np.stack([power[:, a:max(b, a + 1)].mean(axis=1) for a, b in zip(index[:-1], index[1:])], axis=1)
    level = 10 * np.log10(rows + 1e-20)
    level = np.clip((level - level.max() + RANGE_DB) / RANGE_DB, 0, 1)
    image = np.zeros((height, width, 3), np.uint8)
    columns = min(width, len(level))
    image[:, :columns] = colour(level[:columns].T[::-1])
    return image


def envelope_strip(mono, rate, span_s, width=WIDTH, height=ENV_H):
    """A small image of the RMS envelope in dBFS (-60 at the bottom, 0 at the top)."""
    image = Image.new("RGB", (width, height), (18, 18, 22))
    draw = ImageDraw.Draw(image)
    for level in (-40, -20):
        y = int(height * (-level) / 60)
        draw.line([(0, y), (width, y)], fill=(60, 60, 70))
    step = max(1, int(span_s * rate / width))
    cut = sfx_audio.frames(mono.astype(np.float32), step, step)[:width]
    rms = sfx_audio.power_db((cut.astype(np.float64) ** 2).mean(axis=1))
    points = [(x, int(height * min(1.0, -v / 60))) for x, v in enumerate(np.clip(rms, -60, 0))]
    draw.polygon([(0, height)] + points + [(len(points) - 1, height)], fill=(90, 170, 220))
    return image


def tile(samples, rate, span_s, lines):
    """One clip: spectrogram, envelope and up to two lines of text under them."""
    mono = sfx_audio.mid(samples)
    image = Image.new("RGB", (WIDTH, SPEC_H + ENV_H + TEXT_H), (10, 10, 12))
    image.paste(Image.fromarray(spectrogram(mono, rate, span_s)), (0, 0))
    image.paste(envelope_strip(mono, rate, span_s), (0, SPEC_H))
    draw = ImageDraw.Draw(image)
    for hz in (100, 1000, 10000):
        y = SPEC_H - int(SPEC_H * np.log(hz / LOW_HZ) / np.log(HIGH_HZ / LOW_HZ))
        draw.line([(0, y), (5, y)], fill=(255, 255, 255))
    for row, text in enumerate(lines[:2]):
        draw.text((3, SPEC_H + ENV_H + 1 + row * 12), text[:40], fill=(225, 225, 225))
    return image


def sheet(tiles, title, columns=6):
    """A grid of tiles under a title line."""
    rows = max(1, -(-len(tiles) // columns))
    width, height = WIDTH + 4, SPEC_H + ENV_H + TEXT_H + 4
    image = Image.new("RGB", (columns * width + 4, rows * height + 22), (0, 0, 0))
    ImageDraw.Draw(image).text((6, 5), title, fill=(255, 255, 255))
    for number, picture in enumerate(tiles):
        image.paste(picture, (4 + (number % columns) * width, 22 + (number // columns) * height))
    return image


def clip_tile(path, span_s, numbers=None, name=None):
    """The tile of a clip file (the export's or the workshop's), labelled with its name and key numbers."""
    samples, rate = sfx_audio.read(path)
    label = name or path.replace("\\", "/").rsplit("/", 1)[-1].rsplit(".", 1)[0]
    facts = ""
    if numbers:
        facts = f"{numbers.get('length_s', 0):.2f}s {numbers.get('momentary_max_lufs', 0):.0f}LU " \
                f"{numbers.get('centroid_hz', 0):.0f}Hz f{numbers.get('flatness', 0):.2f}"
    return tile(samples, rate, span_s, [label, facts])


def span_for(lengths, cap=6.0):
    """The common time span of a sheet: the 90th percentile length, at least 0.5 s, at most cap seconds."""
    return float(min(cap, max(0.5, np.percentile(lengths, 90)))) if len(lengths) else 1.0
