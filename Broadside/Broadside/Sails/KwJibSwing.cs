using System.Collections.Generic;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's jibs sheeted to leeward of the apparent wind (<see cref="KwShipWind"/>, <see cref="KwSailTrim"/>):
    /// each turned about its own stay, so its luff stays on the stay and its club's end swings out to the angle its trim
    /// asks, easing there and going across when the wind crosses the bow or the stern, amidships with the sails furled;
    /// the leeward sheet taut from the club's end to where it is made fast, the windward one slack. Every jib on a bowsprit
    /// at the same angle. Only drawn, on every screen from the game's wind and the ship's speed as seen there; nothing is
    /// sent. On the dock the jibs stay as they are drawn.
    /// </summary>
    public sealed class KwJibSwing : MonoBehaviour
    {
        private const float Ease = 0.8f, Fastest = 80f, Least = 0.05f, Seen = 150f, Slack = 0.07f;

        private readonly List<KwJibHung> hung = new List<KwJibHung>();
        private Ship? ship;
        private KwShipWind? wind;
        private int side = 1;
        private float foot, shown, turning;
        private bool broken;

        /// <summary>A bowsprit's jibs on a launched ship, swung from now on; nothing with none.</summary>
        public static void Add(Transform bowsprit, List<KwJib> jibs)
        {
            if (jibs.Count == 0)
                return;
            KwJibSwing swing = bowsprit.gameObject.AddComponent<KwJibSwing>();
            foreach (KwJib jib in jibs)
                swing.hung.Add(new KwJibHung(jib));
        }

        private void Start() => (ship, wind) = (GetComponentInParent<Ship>(), GetComponentInParent<KwShipWind>());

        // A throw is reported once and stops this swing, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside jib swing", static j => j.Swing(), this);
            broken = false;
        }

        private void Swing()
        {
            Camera? eye = Utils.GetMainCamera();
            if (wind == null || eye == null || (eye.transform.position - transform.position).sqrMagnitude > Seen * Seen)
                return;
            KwApparentWind air = wind.Now(transform.forward);
            side = KwSailTrim.Side(side, air.Across);
            bool set = ship == null || ship.GetSpeedSetting() is Ship.Speed.Slow or Ship.Speed.Half or Ship.Speed.Full;
            foot = Mathf.SmoothDamp(foot, set ? side * KwSailTrim.Foot(air.Angle) : 0f, ref turning, Ease, Fastest, Time.deltaTime);
            if (Mathf.Abs(foot - shown) < Least)
                return;
            shown = foot;
            foreach (KwJibHung jib in hung)
                jib.Sheeted(foot, Slack);
        }
    }

    /// <summary>
    /// One jib as a launched ship hangs it, for its swing (<see cref="KwJibSwing"/>): its cloth's root and its club as they
    /// were made, its stay's foot and way, its club's end, and its two sheets with where each is made fast.
    /// </summary>
    public sealed class KwJibHung
    {
        private readonly KwJib jib;
        private readonly Transform? root, club;
        private readonly (Vector3 At, Quaternion Turn) rootWas, clubWas;
        private readonly LineRenderer?[] sheets = new LineRenderer?[2];
        private readonly Vector3[] feet = new Vector3[2];

        public KwJibHung(KwJib jib)
        {
            this.jib = jib;
            (root, club) = (jib.Holder.Find("jib" + jib.Index), jib.Holder.Find("club" + jib.Index));
            rootWas = root != null ? (root.localPosition, root.localRotation) : (Vector3.zero, Quaternion.identity);
            clubWas = club != null ? (club.localPosition, club.localRotation) : (Vector3.zero, Quaternion.identity);
            for (int i = 0; i < 2; i++)
            {
                sheets[i] = jib.Holder.Find($"sheet{jib.Index}_{(i == 0 ? -1 : 1)}")?.GetComponent<LineRenderer>();
                LineRenderer? line = sheets[i];
                feet[i] = line != null && line.positionCount > 1 ? line.GetPosition(line.positionCount - 1) : Vector3.zero;
            }
        }

        /// <summary>The jib turned about its stay so its foot lies that far off the line aft (degrees, plus to port), its sheets after it.</summary>
        public void Sheeted(float foot, float slack)
        {
            KwJibCut cut = jib.Cut;
            Quaternion by = KwSailTrim.Sheeted(cut, foot);
            Vector3 tack = KwMesh.ToUnity(cut.Tack);
            if (root != null)
                root.SetLocalPositionAndRotation(tack + by * (rootWas.At - tack), by * rootWas.Turn);
            if (club != null)
                club.SetLocalPositionAndRotation(tack + by * (clubWas.At - tack), by * clubWas.Turn);
            Vector3 end = tack + by * (KwMesh.ToUnity(cut.Tack + cut.FootDir * (cut.Foot + 0.08f)) - tack);
            for (int i = 0; i < 2; i++)
                Sheet(sheets[i], end, feet[i], (i == 1) == (foot > 0f) ? KwSailRigging.SagOf(1f) : slack);
        }

        // A sheet from the club's end to its foot, sagging that share of its length (taut to leeward, slack to windward).
        private static void Sheet(LineRenderer? line, Vector3 end, Vector3 foot, float share)
        {
            if (line == null)
                return;
            Vector3 a = KwMesh.ToKit(end), c = KwMesh.ToKit(foot);
            float sag = share * (c - a).magnitude;
            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i <= 8; i++)
                points.Add(KwSailRigging.Hanging(a, c, i / 8f, sag));
            KwSailRigging.Set(line, points);
        }
    }
}
