using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Which way a ship sails, from its own build, not from the dock it stood on: its rudder's end is its stern; with no
    /// rudder, the way its sails point, a larger mast's sails counting for more (their power: every sail's area × cloth drive). A blueprint is
    /// turned round as it is read or saved, about its middle, when its rudder is forward of the middle or its sails pull
    /// aft, so its bow is always +z (the way the ship sails). The sails' pull along the bow (<see cref="Pull"/>) is what
    /// drives it: sails pointing aft take from those pointing forward, and two the same pointing opposite ways cancel, so
    /// the ship does not sail at all.
    /// </summary>
    public static class KwBpBow
    {
        private static readonly Quaternion Round = Quaternion.Euler(0f, 180f, 0f);

        /// <summary>Turns the blueprint round when its stern is at +z (the rudder there, or with none, the sails pulling to -z).</summary>
        public static void Fix(KwBlueprint blueprint)
        {
            if (blueprint.Pieces.Count == 0 || !Backward(blueprint))
                return;
            float twice = 2f * Middle(blueprint);
            for (int i = 0; i < blueprint.Pieces.Count; i++)
            {
                KwBpPiece p = blueprint.Pieces[i];
                Vector3 at = new Vector3(-p.Position.x, p.Position.y, twice - p.Position.z);
                blueprint.Pieces[i] = new KwBpPiece(p.Prefab, at, Round * p.Rotation, p.Data);
            }
            blueprint.Pieces.Sort((a, b) => a.Position.z.CompareTo(b.Position.z));
        }

        /// <summary>The sails' pull along +z: every sail's power on every mast (<see cref="KwMastSpec.Power"/>) and every
        /// bowsprit's jibs' (<see cref="KwJibPlan.Power"/>), added when the mast or bowsprit faces forward, taken away when it
        /// faces aft.</summary>
        public static float Pull(KwBlueprint blueprint)
        {
            float pull = 0f;
            foreach (KwBpPiece piece in blueprint.Pieces)
            {
                float power = piece.Prefab == KwBowsprit.Made ? KwJibPlan.Power(blueprint, piece)
                    : piece.Prefab == KwSailTool.Made && KwMastSpec.TryParse(KwShipParts.Saved(piece), out KwMastSpec mast) ? mast.Power : 0f;
                float way = Vector3.Dot(piece.Rotation * Vector3.forward, Vector3.forward);
                pull += Mathf.Abs(way) < 0.1f ? 0f : Mathf.Sign(way) * power;
            }
            return pull;
        }

        private static bool Backward(KwBlueprint blueprint)
        {
            KwBpPiece? rudder = blueprint.Pieces.Find(p => KwRudder.Is(p.Prefab));
            return rudder != null ? rudder.Position.z > Middle(blueprint) : Pull(blueprint) < -0.01f;
        }

        // Halfway between the ship's two ends.
        private static float Middle(KwBlueprint blueprint)
        {
            float min = float.MaxValue, max = float.MinValue;
            foreach (KwBpPiece piece in blueprint.Pieces)
                (min, max) = (Mathf.Min(min, piece.Position.z), Mathf.Max(max, piece.Position.z));
            return (min + max) / 2f;
        }
    }
}
