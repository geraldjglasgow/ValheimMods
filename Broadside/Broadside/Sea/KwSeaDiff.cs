using System.Collections.Generic;

namespace Broadside
{
    /// <summary>
    /// A peer catching up with a launched ship's blueprint as its owner last wrote it: index by index (no index ever
    /// shifts, <see cref="KwSeaApply"/>), every entry that differs is copied into the blueprint the ship was built from, so
    /// unchanged pieces keep their objects (and whatever is keyed by them), and the changes come back as one
    /// <see cref="KwSeaChanges"/> however many edits were missed. Pure.
    /// </summary>
    public static class KwSeaDiff
    {
        public static KwSeaChanges Into(KwBlueprint live, KwBlueprint fresh)
        {
            List<KwSeaChange> changes = new List<KwSeaChange>();
            int count = System.Math.Max(live.Pieces.Count, fresh.Pieces.Count);
            for (int i = 0; i < count; i++)
            {
                KwBpPiece? was = i < live.Pieces.Count ? live.Pieces[i] : null;
                KwBpPiece? now = i < fresh.Pieces.Count ? fresh.Pieces[i] : null;
                if (was != null && now != null && KwSeaPiece.Same(was, now))
                    continue;
                if (now == null)
                    now = KwSeaApply.Tombstone(was!);
                if (i < live.Pieces.Count)
                    live.Pieces[i] = now;
                else
                    live.Pieces.Add(now);
                if (!(KwSeaApply.IsGone(was) && KwSeaApply.IsGone(now)))
                    changes.Add(new KwSeaChange(i, KwSeaApply.IsGone(was) ? null : was, KwSeaApply.IsGone(now) ? null : now));
            }
            return new KwSeaChanges(changes);
        }
    }
}
