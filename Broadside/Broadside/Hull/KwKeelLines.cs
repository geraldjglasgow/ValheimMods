using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The keels a ship stands on, as lines in one frame (the world on a dock, a blueprint's frame on a launched ship or a
    /// ghost), which its hull parts and ribs are seated on (<see cref="KwSeat"/>). Each keel's line starts on the ship's
    /// level keel line (its x = 0, its depth over the keel piece's origin) and climbs away from there by its own rise and
    /// curve (<see cref="KwKeelSpec.RiseAt"/>). Read from the keels' saved texts and places only, so every peer seats a
    /// part the same; pure numbers once gathered.
    /// </summary>
    public sealed class KwKeelLines
    {
        // How far (level) from a keel's line a point may lie and stand on it, how far past its ends, and how far above or
        // below the line there a rib's foot or a part's start may be.
        private const float Beside = 0.1f, Past = 0.05f, Off = 0.1f;

        private static KwKeelLines? world;
        private static int worldFrame = -1;
        private readonly List<(Vector3 Start, Vector3 Way, KwKeelSpec Spec)> keels = new List<(Vector3 Start, Vector3 Way, KwKeelSpec Spec)>();

        /// <summary>A keel piece taken in from its place, its turn and its saved text; anything that is not a keel is left out.</summary>
        public void Add(Vector3 position, Quaternion rotation, string saved)
        {
            Vector3 way = rotation * Vector3.forward;
            way.y = 0f;
            if (way.sqrMagnitude < 1e-6f || !KwKeelSpec.TryParse(saved, out KwKeelSpec spec))
                return;
            keels.Add((position + Vector3.up * spec.Body.Depth, way.normalized, spec));
        }

        /// <summary>A blueprint's keels, in its frame.</summary>
        public static KwKeelLines Of(KwBlueprint blueprint)
        {
            KwKeelLines lines = new KwKeelLines();
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                if (piece.Prefab == KwKeel.MadeName)
                    lines.Add(piece.Position, piece.Rotation, KwShipParts.Saved(piece));
            }
            return lines;
        }

        /// <summary>The made keels loaded on this peer (<see cref="KwGenerated.LoadedKeels"/>), gathered once a frame.</summary>
        public static KwKeelLines World()
        {
            if (world != null && worldFrame == Time.frameCount)
                return world;
            world = new KwKeelLines();
            foreach (KwGenerated keel in KwGenerated.LoadedKeels)
            {
                ZNetView? view = keel != null ? keel.GetComponent<ZNetView>() : null;
                if (view != null && view.IsValid())
                    world.Add(keel!.transform.position, keel.transform.rotation, view.GetZDO().GetString(KwGenerated.Key));
            }
            worldFrame = Time.frameCount;
            return world;
        }

        /// <summary>
        /// The keel line under a point (level within 10 cm of it, along its length; of several, the one nearest in height to
        /// near): its height there and the level keel line's (its start's); false with none.
        /// </summary>
        public bool Under(Vector3 point, float near, out float line, out float level)
        {
            (line, level) = (0f, 0f);
            float best = float.MaxValue;
            foreach ((Vector3 start, Vector3 way, KwKeelSpec spec) in keels)
            {
                Vector3 d = point - start;
                d.y = 0f;
                float x = Vector3.Dot(d, way);
                if (x < -Past || x > spec.Length + Past || (d - way * x).sqrMagnitude > Beside * Beside)
                    continue;
                float y = start.y + spec.RiseAt(Mathf.Clamp(x, 0f, spec.Length));
                if (Mathf.Abs(y - near) < best)
                    (best, line, level) = (Mathf.Abs(y - near), y, start.y);
            }
            return best < float.MaxValue;
        }

        /// <summary>How far up its keel a rib's foot stands over the level keel line (0 on a level keel, or on none).</summary>
        public float Climb(Vector3 foot) => Under(foot, foot.y, out float line, out float level) && Mathf.Abs(line - foot.y) <= Off ? line - level : 0f;

        /// <summary>
        /// A hull part's seat, the part starting at origin (its start rib's foot) and running level along forward: the level
        /// keel line under it in its frame, and for a part between two ribs the keel under it; null for a part from keel end
        /// to keel end, or with no keel under its start.
        /// </summary>
        public KwSeat? Seat(Vector3 origin, Vector3 forward, KwSpan span)
        {
            if (span.TwoEnded || !Under(origin, origin.y, out float line, out float level) || Mathf.Abs(line - origin.y) > Off)
                return null;
            forward.y = 0f;
            return new KwSeat(level - origin.y, span.RibEnded ? Profile(origin, forward.normalized, span.Length) : null);
        }

        // The keel's top under a part between two ribs, in its frame (along it, over its start): at its two ends and at every
        // corner the keels are drawn with under it (KwKeel.Segments), so its planking's lower edge runs along the keel's own
        // top edge and closes on it; thinned to where the keel bends (KwSeat.Thin).
        private List<Vector2> Profile(Vector3 origin, Vector3 forward, float length)
        {
            List<float> places = Corners(origin, forward, length);
            places.Add(0f);
            places.Add(length);
            places.Sort();
            List<Vector2> points = new List<Vector2>();
            foreach (float x in places)
            {
                if (Under(origin + forward * x, origin.y, out float line, out _) && (points.Count == 0 || x - points[points.Count - 1].x > 0.005f))
                    points.Add(new Vector2(x, line - origin.y));
            }
            return KwSeat.Thin(points);
        }

        // Where along a part (level, from origin) the keels under its line have the corners they are drawn with, inside it.
        private List<float> Corners(Vector3 origin, Vector3 forward, float length)
        {
            List<float> places = new List<float>();
            foreach ((Vector3 start, Vector3 way, KwKeelSpec spec) in keels)
            {
                int steps = KwKeel.Segments(spec.Length);
                for (int i = 0; i <= steps; i++)
                {
                    Vector3 d = start + way * (spec.Length * i / steps) - origin;
                    d.y = 0f;
                    float x = Vector3.Dot(d, forward);
                    if (x > 0.005f && x < length - 0.005f && (d - forward * x).sqrMagnitude <= Beside * Beside)
                        places.Add(x);
                }
            }
            return places;
        }
    }
}
