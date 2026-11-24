"""Read a DevBridge /perf/record recording: frames, hitches, and which mod methods cost what, when.

Usage: python perfrec.py [--rec FOLDER|NAME] COMMAND [options]

  list                          recordings, newest first
  summary                       frames, fps, frame ms percentiles, hitch counts, mods and methods by total time
  hitches [--over 40] [--top 20] [--show 6]
                                the worst frames, each with its timed share and costliest methods beside their usual cost
  spikes [--over 40] [--top 25] methods by time above their usual cost inside frames over --over ms
  method PATTERN [--top 10]     matching methods (regex on the full name): per frame avg/p50/p99/max, calls, slowest call
  mod PATTERN [--top 15]        a mod's time per frame and its costliest methods
  timeline [--bucket 10]        per bucket of seconds: fps, frame ms, hitches, gc, objects, peers, top mods
  frames --start S --end S      every frame between two times (seconds from the start), with its top methods
  where [--over 40] [--cell 32] hitches by where the local player stood

The recording defaults to the newest folder in %LOCALAPPDATA%\\DevBridge\\perf. Frame ms is main-thread step to step;
"timed" is the time inside mods' timed methods (outermost calls per mod, summed over mods), the rest is the game,
Unity, rendering and the garbage collector. Needs numpy; the binary files are memory-mapped, so size does not matter.
"""
import argparse
import json
import os
import re
import sys

import numpy as np

ROOT = os.path.join(os.environ.get("LOCALAPPDATA", ""), "DevBridge", "perf")
MAIN, OTHER, MOD = 0, 1, 2


class Recording:
    def __init__(self, folder):
        self.folder = folder
        with open(os.path.join(folder, "index.json"), encoding="utf-8") as f:
            self.index = json.load(f)
        self.hz = float(self.index["stopwatch_hz"])
        self.frames = self._read("frames", self.index["frame_fields"])
        self.entries = self._read("entries", self.index["entry_fields"])
        self._trim()
        self.methods = self.index.get("methods", [])
        self.mods = [m["name"] for m in self.index.get("mods", [])]

    def _read(self, which, fields):
        dtype = np.dtype([(name, "<" + kind) for name, kind in fields])
        path = os.path.join(self.folder, self.index["files"][which])
        count = os.path.getsize(path) // dtype.itemsize if os.path.exists(path) else 0
        if count == 0:
            return np.zeros(0, dtype=dtype)
        return np.memmap(path, dtype=dtype, mode="r", shape=(count,))

    def _trim(self):
        """While recording, one file can run a chunk ahead of the other: keep the frames both hold."""
        if len(self.frames) == 0 or len(self.entries) == 0:
            return
        last = min(int(self.frames["seq"][-1]), int(self.entries["seq"][-1]))
        self.frames = self.frames[self.frames["seq"] <= last]
        self.entries = self.entries[: np.searchsorted(self.entries["seq"], last, "right")]

    def ms(self, ticks):
        return np.asarray(ticks, dtype=np.float64) * 1000.0 / self.hz

    def kind(self, kind):
        return self.entries[self.entries["kind"] == kind]

    def in_frame(self, seq):
        lo = np.searchsorted(self.entries["seq"], seq, "left")
        hi = np.searchsorted(self.entries["seq"], seq, "right")
        return self.entries[lo:hi]

    def name(self, entry_id, kind):
        if kind == MOD:
            return self.mods[entry_id] if entry_id < len(self.mods) else f"mod {entry_id}"
        return self.methods[entry_id]["name"] if entry_id < len(self.methods) else f"method {entry_id}"

    def mod_of(self, method_id):
        return self.mods[self.methods[method_id]["mod"]]

    def totals(self, kind):
        rows = self.kind(kind)
        size = len(self.mods) if kind == MOD else len(self.methods)
        ticks = np.bincount(rows["id"], weights=rows["ticks"], minlength=size)
        calls = np.bincount(rows["id"], weights=rows["calls"], minlength=size)
        return ticks, calls

    def timed_ms(self):
        """Per frame row: ms inside mods' outermost timed calls."""
        rows = self.kind(MOD)
        pos = self.row_of(rows["seq"])
        timed = np.zeros(len(self.frames))
        keep = pos >= 0
        np.add.at(timed, pos[keep], self.ms(rows["ticks"][keep]))
        return timed

    def row_of(self, seqs):
        lookup = np.full(int(self.frames["seq"].max()) + 1 if len(self.frames) else 1, -1, dtype=np.int64)
        lookup[self.frames["seq"]] = np.arange(len(self.frames))
        seqs = np.asarray(seqs, dtype=np.int64)
        out = np.full(len(seqs), -1, dtype=np.int64)
        inside = seqs < len(lookup)
        out[inside] = lookup[seqs[inside]]
        return out


def pct(values, q):
    return float(np.percentile(values, q)) if len(values) else 0.0


def newest():
    if not os.path.isdir(ROOT):
        sys.exit(f"no recordings in {ROOT}")
    names = sorted(n for n in os.listdir(ROOT) if os.path.exists(os.path.join(ROOT, n, "index.json")))
    if not names:
        sys.exit(f"no recordings in {ROOT}")
    return os.path.join(ROOT, names[-1])


def resolve(rec):
    if rec is None:
        return newest()
    if os.path.isdir(rec):
        return rec
    matches = sorted(n for n in os.listdir(ROOT) if rec in n)
    if not matches:
        sys.exit(f"no recording matching {rec} in {ROOT}")
    return os.path.join(ROOT, matches[-1])


def cmd_list(_args):
    if not os.path.isdir(ROOT):
        sys.exit(f"no recordings in {ROOT}")
    for name in sorted(os.listdir(ROOT), reverse=True):
        folder = os.path.join(ROOT, name)
        try:
            with open(os.path.join(folder, "index.json"), encoding="utf-8") as f:
                index = json.load(f)
        except (OSError, ValueError):
            continue
        s = index.get("summary", {})
        size = sum(os.path.getsize(os.path.join(folder, n)) for n in os.listdir(folder)) / 1048576
        print(f"{name:32} {size:8.1f} MB  {s.get('seconds', 0):7.0f} s  {s.get('fps', 0):6.1f} fps  {index['status']}")


def cmd_summary(r, _args):
    f = r.frames
    print(f"{r.folder}\nstatus: {r.index['status']}   options: {r.index['options']}")
    if len(f) == 0:
        sys.exit("no frames recorded")
    ms = np.asarray(f["ms"], dtype=np.float64)
    secs = ms.sum() / 1000
    print(f"{len(f)} frames over {secs:.1f} s, {len(f) / secs:.1f} fps; dropped: {int((f['flags'] & 1).sum())}")
    print("frame ms: avg {:.2f}  p50 {:.2f}  p95 {:.2f}  p99 {:.2f}  p99.9 {:.2f}  max {:.1f}".format(
        ms.mean(), pct(ms, 50), pct(ms, 95), pct(ms, 99), pct(ms, 99.9), ms.max()))
    print("frames over: " + "  ".join(f">{t} ms {int((ms > t).sum())}" for t in (25, 40, 50, 100, 250)))
    print_world(f)
    timed = r.timed_ms()
    print(f"timed (mods) ms/frame: avg {timed.mean():.3f}  p99 {pct(timed, 99):.2f}  max {timed.max():.1f}"
          f"   untimed (game) avg {ms.mean() - timed.mean():.2f}")
    print_calibration(r)
    print_mods(r, timed_frames=len(f))
    print_methods(r, len(f), 15)


def print_world(f):
    gc = int(f["gc"].sum())
    kb = f["mono_kb"]
    print(f"collections: {gc}   mono heap MB: {kb.min() / 1024:.0f}-{kb.max() / 1024:.0f}   "
          f"objects: {f['instances'].min()}-{f['instances'].max()}   peers: {f['peers'].min()}-{f['peers'].max()}")


def print_calibration(r):
    probing = r.index.get("probing", {})
    extra = probing.get("extra_us_per_call")
    _, calls = r.totals(MAIN)
    if extra:
        cost = calls.sum() * extra / 1000 / max(len(r.frames), 1)
        print(f"timing itself: {extra} us per call, about {cost:.3f} ms/frame; probes on {probing.get('on')} of "
              f"{probing.get('methods')}, failed {len(probing.get('failed', []))}")


def print_mods(r, timed_frames):
    ticks, _ = r.totals(MOD)
    rows = r.kind(MOD)
    print("\nmods (ms/frame avg, worst frame):")
    for mod_id in np.argsort(-ticks)[:12]:
        if ticks[mod_id] == 0:
            break
        worst = r.ms(rows["ticks"][rows["id"] == mod_id].max())
        print(f"  {r.ms(ticks[mod_id]) / timed_frames:8.3f}  {worst:8.2f}  {r.mods[mod_id]}")


def print_methods(r, frames, top):
    ticks, calls = r.totals(MAIN)
    print(f"\nmethods by total (ms/frame, calls/frame, us/call):")
    for mid in np.argsort(-ticks)[:top]:
        if ticks[mid] == 0:
            break
        us = r.ms(ticks[mid]) * 1000 / max(calls[mid], 1)
        print(f"  {r.ms(ticks[mid]) / frames:8.3f}  {calls[mid] / frames:9.1f}  {us:8.1f}  {r.name(mid, MAIN)}")


def cmd_hitches(r, args):
    ms = np.asarray(r.frames["ms"], dtype=np.float64)
    timed = r.timed_ms()
    ticks, _ = r.totals(MAIN)
    usual = r.ms(ticks) / max(len(r.frames), 1)
    worst = [i for i in np.argsort(-ms) if ms[i] > args.over][: args.top]
    print(f"{int((ms > args.over).sum())} frames over {args.over} ms; worst {len(worst)}:")
    for row in worst:
        print_hitch(r, row, ms[row], timed[row], usual, args.show)


def print_hitch(r, row, ms, timed, usual, show):
    fr = r.frames[row]
    at = "" if np.isnan(fr["x"]) else f" at {fr['x']:.0f},{fr['z']:.0f}"
    print(f"\nt={fr['t']:.2f}s  {ms:.1f} ms  timed {timed:.2f} ms  gc {fr['gc']}  objects {fr['instances']}"
          f"  peers {fr['peers']}{at}")
    rows = r.in_frame(fr["seq"])
    main = rows[rows["kind"] == MAIN]
    for e in main[descending(main["ticks"])][:show]:
        print(f"    {r.ms(e['ticks']):8.2f} ms  x{e['calls']:<6} slowest {r.ms(e['max']):7.2f}  usual {usual[e['id']]:.3f}"
              f"  {r.name(e['id'], MAIN)}")


def cmd_spikes(r, args):
    ms = np.asarray(r.frames["ms"], dtype=np.float64)
    hitch_seqs = r.frames["seq"][ms > args.over]
    ticks, _ = r.totals(MAIN)
    usual = ticks / max(len(r.frames), 1)
    main = r.kind(MAIN)
    inside = main[np.isin(main["seq"], hitch_seqs)]
    excess = np.bincount(inside["id"], weights=inside["ticks"] - usual[inside["id"]], minlength=len(r.methods))
    seen = np.bincount(inside["id"], minlength=len(r.methods))
    peak = np.zeros(len(r.methods))
    np.maximum.at(peak, inside["id"], inside["ticks"])
    print(f"{len(hitch_seqs)} frames over {args.over} ms; methods by time above usual in them "
          f"(excess ms total, frames seen, worst ms, usual ms/frame):")
    for mid in np.argsort(-excess)[: args.top]:
        if excess[mid] <= 0:
            break
        print(f"  {r.ms(excess[mid]):9.2f}  {seen[mid]:6}  {r.ms(peak[mid]):8.2f}  {r.ms(usual[mid]):8.3f}  {r.name(mid, MAIN)}")


def descending(values):
    """Indices from largest to smallest; the files' counts are unsigned, where -x wraps round."""
    return np.argsort(np.asarray(values, dtype=np.int64), kind="stable")[::-1]


def short(name):
    return ".".join(name.split(".")[-2:])


def matching(names, pattern):
    rx = re.compile(pattern, re.IGNORECASE)
    return [i for i, n in enumerate(names) if rx.search(n)]


def cmd_method(r, args):
    names = [m["name"] for m in r.methods]
    ticks, _ = r.totals(MAIN)
    ids = sorted(matching(names, args.pattern), key=lambda i: -ticks[i])[: args.top]
    if not ids:
        sys.exit(f"no method matches {args.pattern}")
    main, other = r.kind(MAIN), r.kind(OTHER)
    for mid in ids:
        print_method(r, mid, main[main["id"] == mid], other[other["id"] == mid])


def print_method(r, mid, rows, other):
    frames = max(len(r.frames), 1)
    m = r.methods[mid]
    per = r.ms(rows["ticks"])
    print(f"\n{m['name']}  [{r.mod_of(mid)}; {m['kind']}; on {', '.join(m['on']) or '-'}]")
    if len(rows) == 0:
        print("  never ran on the main thread")
    else:
        print(f"  ms/frame avg {per.sum() / frames:.3f}; in the {len(rows)} frames it ran: p50 {pct(per, 50):.3f}"
              f"  p99 {pct(per, 99):.3f}  max {per.max():.2f}; calls/frame {rows['calls'].sum() / frames:.1f};"
              f" slowest call {r.ms(rows['max'].max()):.2f} ms; frames over 1 ms {int((per > 1).sum())}")
    if len(other):
        print(f"  other threads: {other['calls'].sum()} calls, {r.ms(other['ticks'].sum()):.1f} ms in all")


def cmd_mod(r, args):
    ids = matching(r.mods, args.pattern)
    if not ids:
        sys.exit(f"no mod matches {args.pattern}; mods: {', '.join(r.mods)}")
    frames = max(len(r.frames), 1)
    mods, ticks = r.kind(MOD), r.totals(MAIN)[0]
    for mod_id in ids:
        per = r.ms(mods["ticks"][mods["id"] == mod_id])
        print(f"\n{r.mods[mod_id]}: ms/frame avg {per.sum() / frames:.3f}  p99 {pct(per, 99):.3f}  max "
              f"{per.max() if len(per) else 0:.2f}  frames over 1 ms {int((per > 1).sum())}")
        own = [i for i, m in enumerate(r.methods) if m["mod"] == mod_id and ticks[i] > 0]
        for mid in sorted(own, key=lambda i: -ticks[i])[: args.top]:
            print(f"  {r.ms(ticks[mid]) / frames:8.3f}  {r.name(mid, MAIN)}")


def cmd_timeline(r, args):
    f = r.frames
    bucket = (np.asarray(f["t"]) // args.bucket).astype(np.int64)
    ms = np.asarray(f["ms"], dtype=np.float64)
    mod_rows = r.kind(MOD)
    mod_bucket = bucket[np.maximum(r.row_of(mod_rows["seq"]), 0)]
    print(f"{'t':>6} {'fps':>6} {'avg':>6} {'p99':>6} {'max':>7} {'>40':>4} {'gc':>3} {'objects':>8} {'peers':>5}  top mods ms/frame")
    for b in np.unique(bucket):
        sel = bucket == b
        print_bucket(r, b * args.bucket, ms[sel], f[sel], mod_rows[mod_bucket == b])


def print_bucket(r, t, ms, f, mods):
    ticks = np.bincount(mods["id"], weights=mods["ticks"], minlength=len(r.mods))
    top = "  ".join(f"{r.mods[i]} {r.ms(ticks[i]) / len(ms):.2f}" for i in np.argsort(-ticks)[:3] if ticks[i] > 0)
    print(f"{t:6.0f} {len(ms) / (ms.sum() / 1000):6.1f} {ms.mean():6.2f} {pct(ms, 99):6.2f} {ms.max():7.1f} "
          f"{int((ms > 40).sum()):4} {int(f['gc'].sum()):3} {int(f['instances'].mean()):8} {int(f['peers'].max()):5}  {top}")


def cmd_frames(r, args):
    f = r.frames
    sel = np.nonzero((f["t"] >= args.start) & (f["t"] <= args.end))[0]
    timed = r.timed_ms()
    for row in sel[:2000]:
        fr = f[row]
        main = r.in_frame(fr["seq"])
        main = main[main["kind"] == MAIN]
        top = "  ".join(f"{short(r.name(e['id'], MAIN))} {r.ms(e['ticks']):.2f}" for e in main[descending(main["ticks"])][:3])
        print(f"{fr['t']:9.3f} {fr['ms']:7.2f} timed {timed[row]:6.2f} gc {fr['gc']}  {top}")


def cmd_where(r, args):
    f = r.frames
    ok = ~np.isnan(f["x"])
    cells = np.stack([np.floor(f["x"][ok] / args.cell), np.floor(f["z"][ok] / args.cell)], axis=1).astype(np.int64)
    hitch = np.asarray(f["ms"])[ok] > args.over
    keys, inverse, counts = np.unique(cells, axis=0, return_inverse=True, return_counts=True)
    hitches = np.bincount(inverse.ravel(), weights=hitch, minlength=len(keys))
    print(f"cells of {args.cell} m (x, z of the corner): frames, frames over {args.over} ms, per 1000 frames")
    for k in np.argsort(-hitches)[:20]:
        if hitches[k] == 0:
            break
        x, z = keys[k] * args.cell
        print(f"  {x:6},{z:6}  {counts[k]:8}  {int(hitches[k]):5}  {1000 * hitches[k] / counts[k]:7.1f}")


def parser():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--rec", help="a recording folder, or part of its name (default: the newest)")
    sub = p.add_subparsers(dest="cmd", required=True)
    sub.add_parser("list")
    sub.add_parser("summary")
    h = sub.add_parser("hitches")
    h.add_argument("--over", type=float, default=40)
    h.add_argument("--top", type=int, default=20)
    h.add_argument("--show", type=int, default=6)
    s = sub.add_parser("spikes")
    s.add_argument("--over", type=float, default=40)
    s.add_argument("--top", type=int, default=25)
    for name, top in (("method", 10), ("mod", 15)):
        m = sub.add_parser(name)
        m.add_argument("pattern")
        m.add_argument("--top", type=int, default=top)
    sub.add_parser("timeline").add_argument("--bucket", type=float, default=10)
    fr = sub.add_parser("frames")
    fr.add_argument("--start", type=float, required=True)
    fr.add_argument("--end", type=float, required=True)
    w = sub.add_parser("where")
    w.add_argument("--over", type=float, default=40)
    w.add_argument("--cell", type=float, default=32)
    return p


def main():
    args = parser().parse_args()
    if args.cmd == "list":
        return cmd_list(args)
    r = Recording(resolve(args.rec))
    if len(r.frames) == 0:
        sys.exit(f"{r.folder}: no frames yet (status: {r.index['status']})")
    globals()["cmd_" + args.cmd](r, args)


if __name__ == "__main__":
    main()
