using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The launched ship prefab's own numbers that the table's figures need besides a ship's build (the karve's, which
    /// <c>KW_ship</c> copies, as a mod such as ShipConfig may have set them) and the length of the physics step.
    /// </summary>
    public readonly struct KwBpShipRef
    {
        public KwBpShipRef(float angularDamping, float sideways, float rowing, float step)
        {
            AngularDamping = angularDamping;
            Sideways = sideways;
            Rowing = rowing;
            Step = step;
        }

        /// <summary>The game karve's: angular damping 0.05, sideways drag 0.15, oars 0.2, 50 steps a second.</summary>
        public static readonly KwBpShipRef Karve = new KwBpShipRef(0.05f, 0.15f, 0.2f, 0.02f);

        public float AngularDamping { get; }
        public float Sideways { get; }
        public float Rowing { get; }
        public float Step { get; }

        /// <summary>The numbers of the game's <c>KW_ship</c> prefab and its physics step; the karve's while there is none.</summary>
        public static KwBpShipRef Game()
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(KwShipPrefab.Name) : null;
            Ship? ship = prefab != null ? prefab.GetComponent<Ship>() : null;
            float step = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : Karve.Step;
            return ship == null ? Karve : new KwBpShipRef(ship.m_angularDamping, ship.m_dampingSideway, ship.m_backwardForce, step);
        }
    }

    /// <summary>
    /// How a ship from a blueprint would sail and turn, worked out from the launched ship's own step (<see cref="KwShipDrive"/>):
    /// its drag along the ship is the karve's over its speed squared (<see cref="KwShipStats"/>), a share of it while
    /// afloat; under sail each step adds its sails' push (each kind's by the apparent wind, <see cref="KwSailSpeeds"/>:
    /// full wind, full sail), so its top speed is where that push and the drag meet at the best angle to the wind, and the
    /// closest it sails to the wind where it still makes a third of that; a karve (one square sail) is measured the same
    /// way; under oars the oars' push. Its turning circle is the step itself run for a minute in the plane at full helm and full sail: the
    /// game's drags along and across the ship, the speed a turn costs, the rudder's push at the stern and the angular
    /// damping, on a hull turned as a solid block of its length and beam (as <see cref="KwShipHydro"/> sets it).
    /// </summary>
    public sealed class KwBpHandling
    {
        private const int Steps = 3000;

        public static readonly KwBpHandling None = new KwBpHandling();

        // A karve's top speed: its one square sail, its sail force and drag, in full wind.
        private static readonly float Karve = KwSailSpeeds.Best(KwSailMix.Only(KwSailKind.Square), KwShipStats.KarveFactor, KwShipStats.KarveDrag * KwShipDrive.DragShare).Speed;

        /// <summary>Metres a second under full sail at the best angle to the wind; 0 when the sails give no push.</summary>
        public float TopSpeed { get; private set; }

        /// <summary>How far off the bow the true wind is at the top speed, degrees.</summary>
        public float BestAngle { get; private set; } = 180f;

        /// <summary>The closest it sails to the true wind making a third of its top speed or more, degrees (180 when it never does).</summary>
        public float Closest { get; private set; } = 180f;

        /// <summary>The same for a karve's speed and sail push, in the same step.</summary>
        public float KarveSpeed { get; private set; } = 1f;

        /// <summary>Metres a second under oars.</summary>
        public float RowSpeed { get; private set; }

        /// <summary>The turning circle's width at full helm under full sail, metres; 0 when it does not sail.</summary>
        public float Circle { get; private set; }

        public float Relative => KarveSpeed > 0f ? TopSpeed / KarveSpeed : 0f;

        public static KwBpHandling Of(KwShipStats stats, KwBpHull hull, KwBpShipRef ship)
        {
            float drag = KwShipStats.KarveDrag / (stats.Speed * stats.Speed) * KwShipDrive.DragShare;
            (float top, float angle) = KwSailSpeeds.Best(stats.Mix, stats.SailFactor, drag);
            float push = drag * top * top;
            KwBpHandling handling = new KwBpHandling
            {
                TopSpeed = top, BestAngle = angle, Closest = KwSailSpeeds.Closest(stats.Mix, stats.SailFactor, drag, top), KarveSpeed = Karve,
                RowSpeed = Mathf.Sqrt(ship.Rowing * ship.Step / drag),
            };
            float rudder = KwShipStats.KarveSteer * stats.Steering;
            handling.Circle = push > 0f ? Turn(push, drag, rudder, hull, ship) : 0f;
            return handling;
        }

        // A minute of the ship's step in the plane, from its top speed, the helm hard over: the circle's width at the end.
        private static float Turn(float push, float drag, float rudder, KwBpHull hull, KwBpShipRef ship)
        {
            float length = hull.Length, inertia = (length * length + hull.Beam * hull.Beam) / 12f;
            Vector2 v = new Vector2(0f, Mathf.Sqrt(push / drag));
            float heading = 0f, omega = 0f;
            for (int i = 0; i < Steps; i++)
            {
                Vector2 fwd = new Vector2(Mathf.Sin(heading), Mathf.Cos(heading)), right = new Vector2(fwd.y, -fwd.x);
                v = Dragged(v, fwd, right, omega, drag, ship);
                omega -= omega * ship.AngularDamping * KwShipDrive.DragShare;
                float shove = Vector2.Dot(v, fwd) * rudder * ship.Step;
                v += fwd * push - right * shove;
                omega += shove * (length / 2f) / inertia;
                heading += omega * ship.Step;
            }
            return omega > 1e-4f ? 2f * v.magnitude / omega : 0f;
        }

        // KwShipDrive.Drag in the plane, the helm hard over: the speed a turn costs, the drag along and across the ship.
        private static Vector2 Dragged(Vector2 v, Vector2 fwd, Vector2 right, float omega, float drag, KwBpShipRef ship)
        {
            float along = Vector2.Dot(v, fwd), across = Vector2.Dot(v, right);
            float turning = KwShipDrive.HelmDrag + Mathf.Abs(omega) * KwShipDrive.TurnDrag;
            v -= fwd * (along * Mathf.Clamp01(turning * ship.Step));
            v -= fwd * Mathf.Clamp(along * along * Mathf.Sign(along) * drag, -1f, 1f);
            v -= right * Mathf.Clamp(across * across * Mathf.Sign(across) * ship.Sideways * KwShipDrive.DragShare, -1f, 1f);
            return v;
        }
    }
}
