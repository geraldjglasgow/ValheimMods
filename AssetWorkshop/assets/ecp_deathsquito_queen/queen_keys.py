"""Keyframes in bulk: every value of every frame is collected first, then each curve is made with one key through the
API and filled at once (as the Headsman's scene does), far faster than a key at a time."""
import bpy

LINEAR, CONSTANT = 1, 0


class Keys:
    def __init__(self):
        self.table = {}           # owner -> {(path, index): ([frame, value, ...], interpolation)}

    def add(self, owner, path, frame, values, constant=False):
        """Values (a number or a sequence) of `path` on `owner` at `frame`."""
        values = values if hasattr(values, "__len__") else (values,)
        curves = self.table.setdefault(owner, {})
        for index, value in enumerate(values):
            points, _ = curves.setdefault((path, index), ([], CONSTANT if constant else LINEAR))
            points += [frame, float(value)]

    def flush(self):
        for owner, curves in self.table.items():
            for path in sorted({path for path, _ in curves}):
                first = next(points[0] for (p, _), (points, _) in curves.items() if p == path)
                owner.keyframe_insert(path, frame=first)
            for curve in _curves(owner):
                entry = curves.get((curve.data_path, curve.array_index))
                if entry:
                    _fill(curve, *entry)
        self.table = {}


def _fill(curve, points, interpolation):
    keys = curve.keyframe_points
    keys.clear()
    keys.add(len(points) // 2)
    keys.foreach_set('co', points)
    keys.foreach_set('interpolation', [interpolation] * (len(points) // 2))
    curve.update()


def _curves(owner):
    action = owner.animation_data.action
    if hasattr(action, 'layers') and action.layers:
        slot = owner.animation_data.action_slot
        for layer in action.layers:
            for strip in layer.strips:
                bag = strip.channelbag(slot)
                if bag is not None:
                    return list(bag.fcurves)
    return list(action.fcurves)


def shown(keys, obj, frames, first, last):
    """The object shown (its scale kept) from `first` to `last` and scaled to nothing outside, over `frames`."""
    scale = tuple(obj.scale)
    for f in frames:
        keys.add(obj, 'scale', f, scale if first <= f <= last else (0.0, 0.0, 0.0), constant=True)


def fps():
    return bpy.context.scene.render.fps
