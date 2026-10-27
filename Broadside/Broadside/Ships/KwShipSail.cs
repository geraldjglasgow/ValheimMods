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
    /// a second or so (<see cref="KwSailDress.Pose"/>). Every peer reads the speed the game syncs.
    /// </summary>
    public sealed class KwShipSail : MonoBehaviour
    {
        private const float PerSecond = 0.7f;

        public string Saved = "";

        private Ship? ship;
        private KwMastSpec? mast;
        private List<KwSailLayout>? sails;
        private float[] stages = new float[0];
        private bool broken;

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
                stages[i] = stages[i] < 0f ? target : Mathf.MoveTowards(stages[i], target, PerSecond * Time.deltaTime);
                KwSailDress.Pose(transform, sails, i, Mathf.SmoothStep(0f, 1f, stages[i]));
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
