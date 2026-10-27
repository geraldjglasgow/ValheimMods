using System;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's mast's sail along the ship (its gaff or triangle sail) swung about the mast to the apparent wind
    /// (<see cref="KwShipWind"/>, <see cref="KwSailTrim"/>): to leeward at the angle its trim asks, easing there over a
    /// second or so, tacking and gybing across when the wind crosses the bow or the stern, amidships with the sails
    /// furled. The cloth, its bolt rope, its gaff, its boom (on a ship its own part with its lacing, out of the ship's look
    /// and the lashings, <see cref="Apart"/>), its halyard, peak halyard and topping lift swing as one about the mast's
    /// axis; the boom's sheet runs from the boom's end to where it is made fast on deck. Only drawn, on every screen from
    /// the game's wind and the ship's speed as seen there; nothing is sent. The square sails keep their yards. On the dock
    /// nothing swings.
    /// </summary>
    public sealed class KwBoomSwing : MonoBehaviour
    {
        private const string BoomName = "boom", LacingName = "lacing";
        private const float Ease = 0.9f, Fastest = 90f, Least = 0.05f, Seen = 150f;

        private KwSailLayout? s;
        private int index = -1, side = 1;
        private float angle, shown, turning;
        private Ship? ship;
        private KwShipWind? wind;
        private Vector3? foot;
        private bool broken;

        /// <summary>A mast's parts made while booms are left out of the rig's mesh and the lashings (a launched ship's look and sails).</summary>
        public static void Apart(Action build)
        {
            KwSailShapes.LooseBooms = true;
            try
            {
                build();
            }
            finally
            {
                KwSailShapes.LooseBooms = false;
            }
        }

        /// <summary>A launched ship's mast's sails dressed with their booms apart (<see cref="KwSailDress.Dress"/>), the sail along the ship swung to the wind.</summary>
        public static void Dress(Transform made, string saved)
        {
            Apart(() => KwSailDress.Dress(made, saved));
            if (KwMastSpec.TryParse(saved, out KwMastSpec mast))
                made.gameObject.AddComponent<KwBoomSwing>().Make(made, mast);
        }

        // The sail along the ship and its boom, made as a part of its own about the mast's axis; nothing with none.
        private void Make(Transform made, KwMastSpec mast)
        {
            for (int i = 0; i < mast.Sails.Count && index < 0; i++)
                index = mast.Sails[i].Square ? -1 : i;
            if (index < 0)
            {
                enabled = false;
                return;
            }
            s = new KwSailLayout(mast.Sails[index]);
            Transform boom = Part(made, BoomName + index, KwSailShapes.Boom(s, false), KwPrefabs.Planks);
            Part(boom, LacingName, KwSailShapes.Boom(s, true), KwSailCloth.Rope);
        }

        private void Start() => (ship, wind) = (GetComponentInParent<Ship>(), GetComponentInParent<KwShipWind>());

        // A throw is reported once and stops this swing, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside boom swing", static b => b.Swing(), this);
            broken = false;
        }

        private void Swing()
        {
            if (s == null || wind == null || !Near())
                return;
            KwApparentWind air = wind.Now(transform.forward);
            side = KwSailTrim.Side(side, air.Across);
            float target = Sailing() ? side * KwSailTrim.Boom(air.Angle) : 0f;
            angle = Mathf.SmoothDamp(angle, target, ref turning, Ease, Fastest, Time.deltaTime);
            if (Mathf.Abs(angle - shown) < Least)
                return;
            Turn(KwSailTrim.Boomed(angle - shown), KwSailTrim.Boomed(angle));
            shown = angle;
        }

        // Whether the sails are set at all (furled, the boom is sheeted amidships).
        private bool Sailing() => ship == null || ship.GetSpeedSetting() is Ship.Speed.Slow or Ship.Speed.Half or Ship.Speed.Full;

        // Within sight of this screen's camera.
        private bool Near()
        {
            Camera? eye = Utils.GetMainCamera();
            return eye != null && (eye.transform.position - transform.position).sqrMagnitude < Seen * Seen;
        }

        // Every swinging part turned that much more about the mast's axis, or set to its turn now; the boom's sheet to the
        // boom's new end.
        private void Turn(Quaternion by, Quaternion now)
        {
            foreach (string name in new[] { KwSailDress.Cloth(index), KwSailDress.GaffName + index })
                Swung(transform.Find(name), by);
            foreach (string name in new[] { BoomName + index, "halyard" + index, "peak" + index, KwSailRigging.RopesName + "/lift" })
                Turned(transform.Find(name), now);
            Sheet(transform.Find(KwSailRigging.RopesName + "/sheet_boom" + index), now);
        }

        // A part turned by that much more about the mast's axis.
        private static void Swung(Transform? part, Quaternion by)
        {
            if (part != null)
                part.SetLocalPositionAndRotation(by * part.localPosition, by * part.localRotation);
        }

        // A part on the mast's axis set to the turn (a rope drawn in its own space is drawn turned).
        private static void Turned(Transform? part, Quaternion turn)
        {
            if (part != null)
                part.localRotation = turn;
        }

        // The boom's sheet from the boom's end, turned, down to where its foot is made fast (found as first drawn).
        private void Sheet(Transform? rope, Quaternion turn)
        {
            LineRenderer? line = rope != null ? rope.GetComponent<LineRenderer>() : null;
            if (line == null || line.positionCount < 2)
                return;
            foot ??= line.GetPosition(line.positionCount - 1);
            Vector3 end = turn * KwMesh.ToUnity(new Vector3(-s!.Width - 0.1f, 0f, s.At));
            KwSailRigging.Set(line, KwSailRigging.Sag(KwMesh.ToKit(end), KwMesh.ToKit(foot.Value)));
        }

        // A part of its own mesh under a parent, at the parent's origin.
        private static Transform Part(Transform parent, string name, Mesh mesh, Material? material)
        {
            GameObject made = new GameObject(name) { layer = parent.gameObject.layer };
            made.transform.SetParent(parent, false);
            made.AddComponent<MeshFilter>().sharedMesh = mesh;
            made.AddComponent<MeshRenderer>().sharedMaterial = material;
            made.AddComponent<KwMeshOwner>();
            return made.transform;
        }
    }
}
