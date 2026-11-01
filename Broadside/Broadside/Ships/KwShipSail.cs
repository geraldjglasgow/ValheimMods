using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's mast, its sails raised and lowered by the ship's speed as the game's ships raise theirs, each sail
    /// on its own (<see cref="Share"/>): all furled when stopped or rowing back; at slow the lower sails (the course, the
    /// sail along the ship) a quarter raised; at half sail the lower sails set and the ones above them furled (a mast with
    /// no square sail over its course sets them half, as a lone sail always was); at full sail all of them. Each eases over
    /// a second or so (<see cref="KwSailDress.Pose"/>). Every peer reads the speed the game syncs. The sails are dressed
    /// as the speed sets them when the ship is built (<see cref="Stages"/>, <see cref="Dressed"/>), so the first look at
    /// the speed sets again only a sail whose share changed since (a sail's pose is several milliseconds).
    /// </summary>
    public sealed class KwShipSail : MonoBehaviour
    {
        private const float PerSecond = 0.7f;

        public string Saved = "";

        /// <summary>Each sail's share as it was dressed (<see cref="KwShipFittings"/>); null when it was dressed full.</summary>
        public float[]? Dressed;

        private Ship? ship;
        private KwMastSpec? mast;
        private List<KwSailLayout>? sails;
        private float[] stages = new float[0];
        private bool broken;

        /// <summary>Each of a mast's sails' share at its ship's speed now (all set with no ship, as the dock's masts are).</summary>
        public static float[] Stages(Ship? ship, string saved)
        {
            if (!KwMastSpec.TryParse(saved, out KwMastSpec spec))
                return new float[0];
            float[] stages = new float[spec.Sails.Count];
            for (int i = 0; i < stages.Length; i++)
                stages[i] = ship != null ? Share(spec, i, ship.GetSpeedSetting()) : 1f;
            return stages;
        }

        /// <summary>The hoist a sail is posed at for its share (eased in and out).</summary>
        public static float HoistOf(float stage) => Mathf.SmoothStep(0f, 1f, stage);

        private void Start()
        {
            ship = GetComponentInParent<Ship>();
            if (!KwMastSpec.TryParse(Saved, out KwMastSpec spec))
                return;
            (mast, sails, stages) = (spec, KwSailLayout.Of(spec), new float[spec.Sails.Count]);
            for (int i = 0; i < stages.Length; i++)
                stages[i] = -1f;
        }

        // A throw is reported once and stops this mast, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside ship sail", static s => s.Hoist(), this);
            broken = false;
        }

        // Each sail eased to its share at the speed; the first time set there at once (nothing to do for one dressed there).
        private void Hoist()
        {
            if (ship == null || mast == null || sails == null)
                return;
            Ship.Speed speed = ship.GetSpeedSetting();
            for (int i = 0; i < stages.Length; i++)
            {
                float target = Share(mast, i, speed);
                if (Mathf.Approximately(stages[i], target))
                    continue;
                bool first = stages[i] < 0f;
                stages[i] = first ? target : Mathf.MoveTowards(stages[i], target, PerSecond * Time.deltaTime);
                if (!first || Dressed == null || i >= Dressed.Length || !Mathf.Approximately(Dressed[i], target))
                    KwSailDress.Pose(transform, sails, i, HoistOf(stages[i]));
            }
        }

        /// <summary>How much of a mast's sail i is set at a speed (see the class).</summary>
        public static float Share(KwMastSpec mast, int i, Ship.Speed speed)
        {
            bool upper = mast.Upper(i), stacked = mast.Squares > 1;
            return speed switch
            {
                Ship.Speed.Slow => upper ? 0f : 0.25f,
                Ship.Speed.Half => upper ? 0f : stacked ? 1f : 0.5f,
                Ship.Speed.Full => 1f,
                _ => 0f,
            };
        }
    }
}
