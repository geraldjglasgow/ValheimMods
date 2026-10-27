using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A launched ship's railings as shots break them (the user, 2026-10-26: "Can it also do damage to railings?"), on every
    /// peer. Each stretch between two posts (<see cref="KwRailStretches"/>) is a box on the ship's body, as tall as the
    /// railing, tagged for the router (<see cref="KwShipHitRouter.Rail"/>), so the crew leans on it and a shot strikes it. A
    /// heavy shot striking one breaks it on the owner (<see cref="Take"/>; within <see cref="NearPost"/> of a post the
    /// stretch past that post too), kept in the ship's ZDO (<see cref="Key"/>: "piece:part" a broken stretch); every peer
    /// takes the broken stretches' boxes away and draws them broken (<see cref="KwShipRailLooks"/>). The hammer's repair
    /// mends them a stretch a click (<see cref="KwShipRepair"/>, asked of the owner, <see cref="MendRpc"/>), as holes are
    /// mended. Its boxes come and go with the body's mass kept (<see cref="KwBodyMass"/>).
    /// </summary>
    public sealed class KwShipRails : MonoBehaviour
    {
        public const string Key = "kw_rails_broken", MendRpc = "KW_RailMend";
        public static readonly int Hash = Key.GetStableHashCode();
        private const float Poll = 0.5f, NearPost = 0.3f, Takes = 12f, EntryShare = 0.15f, InnerShare = 0.1f;

        private readonly List<KwRailStretch> stretches = new List<KwRailStretch>();
        private readonly List<GameObject?> boxes = new List<GameObject?>();
        private ZNetView? view;
        private Rigidbody? body;
        private string text = "";
        private float polled;

        /// <summary>The broken stretches as the ZDO last held them, by piece and part.</summary>
        public HashSet<(int Piece, int Part)> Broken { get; private set; } = new HashSet<(int Piece, int Part)>();

        /// <summary>Counts up whenever the broken stretches change.</summary>
        public int Version { get; private set; }

        public bool AnyBroken => Broken.Count > 0;

        public List<KwRailStretch> Stretches => stretches;

        /// <summary>The ship's blueprint in its own frame, and the keels its parts are seated on.</summary>
        public KwBlueprint? Blueprint { get; private set; }
        public KwKeelLines? Keels { get; private set; }

        public static KwShipRails? Add(Transform ship, KwBlueprint blueprint)
        {
            Transform? colliders = ship.Find(KwShipPrefab.Colliders);
            KwKeelLines keels = KwKeelLines.Of(blueprint);
            List<KwRailStretch> found = KwRailStretches.Of(blueprint, keels);
            if (colliders == null || found.Count == 0)
                return null;
            KwShipRails rails = ship.gameObject.AddComponent<KwShipRails>();
            (rails.view, rails.body, rails.Blueprint, rails.Keels) = (ship.GetComponent<ZNetView>(), ship.GetComponent<Rigidbody>(), blueprint, keels);
            rails.stretches.AddRange(found);
            rails.Boxes(colliders);
            if (rails.view == null || !rails.view.IsValid())
                return rails;
            rails.view.Register<int, int>(MendRpc, (sender, piece, part) => Guard.Run("Broadside railing mend", () => rails.Mend(piece, part)));
            rails.Read();
            return rails;
        }

        private void OnDestroy()
        {
            if (view != null && view.IsValid())
                view.Unregister(MendRpc);
        }

        // A box a stretch under a holder of their own (the holes' boxes are the colliders holder's own children).
        private void Boxes(Transform colliders)
        {
            GameObject holder = new GameObject("rails") { layer = colliders.gameObject.layer };
            holder.transform.SetParent(colliders, false);
            KwBodyMass kept = KwBodyMass.Of(body);
            foreach (KwRailStretch stretch in stretches)
                boxes.Add(Box(holder.transform, stretch));
            kept.Put(body);
        }

        /// <summary>
        /// A sea edit's railings boxed again (<see cref="KwSeaRails"/>): these pieces' stretches taken away and laid anew from
        /// the blueprint as it is now (none for a piece removed), the broken ones read again, the body's mass kept.
        /// </summary>
        public void Refit(ICollection<int> indices)
        {
            Transform? holder = transform.Find(KwShipPrefab.Colliders)?.Find("rails");
            KwBodyMass kept = KwBodyMass.Of(body);
            for (int i = stretches.Count - 1; i >= 0; i--)
            {
                if (!indices.Contains(stretches[i].Piece))
                    continue;
                if (boxes[i] != null)
                {
                    boxes[i]!.SetActive(false);
                    Destroy(boxes[i]);
                }
                stretches.RemoveAt(i);
                boxes.RemoveAt(i);
            }
            foreach (int index in indices)
                Lay(holder, KwRailStretches.For(index, Blueprint!.Pieces[index], Keels!));
            kept.Put(body);
            (text, polled) = ("\u0001", 0f);
            Read();
        }

        // New stretches and their boxes.
        private void Lay(Transform? holder, List<KwRailStretch> found)
        {
            foreach (KwRailStretch stretch in found)
            {
                stretches.Add(stretch);
                boxes.Add(holder != null ? Box(holder, stretch) : null);
            }
        }

        private static GameObject? Box(Transform holder, KwRailStretch s)
        {
            Vector3 run = s.B - s.A;
            if (run.magnitude < 0.05f)
                return null;
            GameObject box = new GameObject("rail") { layer = holder.gameObject.layer };
            box.transform.SetParent(holder, false);
            box.transform.SetLocalPositionAndRotation((s.A + s.B) / 2f + s.Up * (KwRailStretches.Height / 2f), Quaternion.LookRotation(run, s.Up));
            box.AddComponent<BoxCollider>().size = new Vector3(2f * KwRailing.Half, KwRailStretches.Height, run.magnitude);
            KwShipHitRouter.Tag(box, KwShipHitRouter.Rail, s.Piece, s.Part);
            return box;
        }

        private void Update()
        {
            if (Time.time - polled < Poll)
                return;
            polled = Time.time;
            Read();
        }

        // The ZDO's broken stretches, read again only when their text changed; their boxes off, the others on.
        private void Read()
        {
            string now = view != null && view.IsValid() ? view.GetZDO().GetString(Hash) : "";
            if (now == text)
                return;
            (text, Broken) = (now, Parse(now));
            KwBodyMass kept = KwBodyMass.Of(body);
            for (int i = 0; i < stretches.Count; i++)
            {
                if (boxes[i] != null)
                    boxes[i]!.SetActive(!Broken.Contains((stretches[i].Piece, stretches[i].Part)));
            }
            kept.Put(body);
            Version++;
        }

        /// <summary>The router's taker for a railing: the stretch struck broken (and its neighbour past a post it was struck by).</summary>
        public static float Take(KwShipHitRouter ship, KwShipHit hit)
        {
            KwShipRails? rails = ship.GetComponent<KwShipRails>();
            if (rails != null)
                rails.Break(hit.Piece, hit.Part, rails.Local(hit.Point));
            (float takes, float share) = Blow(hit.Energy, hit.Entry);
            ship.Health(hit, share);
            return takes;
        }

        /// <summary>What a railing struck takes from a shot's energy, and the ship's health share (more when struck first).</summary>
        public static (float Takes, float Share) Blow(float energy, bool entry) => (Mathf.Min(energy, Takes + energy / 10f), entry ? EntryShare : InnerShare);

        // On the owner: a stretch broken, with the one past the post the point is by.
        private void Break(int piece, int part, Vector3 local)
        {
            if (view == null || !view.IsValid() || !view.IsOwner())
                return;
            HashSet<(int, int)> broken = Parse(view.GetZDO().GetString(Hash));
            List<int> parts = Breaks(stretches, piece, part, local);
            if (parts.Count == 0)
                return;
            parts.ForEach(p => broken.Add((piece, p)));
            view.GetZDO().Set(Hash, Write(broken));
            Read();
        }

        /// <summary>
        /// The parts a hit at a point (the ship's frame) on a stretch breaks: that stretch, and the one past the post the
        /// point is within <see cref="NearPost"/> of along the railing; none for a stretch not there.
        /// </summary>
        public static List<int> Breaks(List<KwRailStretch> stretches, int piece, int part, Vector3 local)
        {
            int i = stretches.FindIndex(s => s.Piece == piece && s.Part == part);
            if (i < 0)
                return new List<int>();
            KwRailStretch s = stretches[i];
            List<int> parts = new List<int> { part };
            float length = (s.B - s.A).magnitude, along = length > 0.01f ? Vector3.Dot(local - s.A, (s.B - s.A) / length) : 0f;
            bool nearA = along < NearPost, nearB = along > length - NearPost;
            int past = KwRailStretches.Part(s.Run, s.Index + (nearB ? 1 : -1));
            if ((nearA || nearB) && stretches.Exists(o => o.Piece == piece && o.Part == past))
                parts.Add(past);
            return parts;
        }

        // A point of the world in the ship's frame as its body stands (where its colliders are).
        private Vector3 Local(Vector3 world) =>
            body != null ? Quaternion.Inverse(body.rotation) * (world - body.position) : transform.InverseTransformPoint(world);

        // On the owner: a stretch mended.
        private void Mend(int piece, int part)
        {
            if (view == null || !view.IsValid() || !view.IsOwner())
                return;
            HashSet<(int, int)> broken = Parse(view.GetZDO().GetString(Hash));
            if (!broken.Remove((piece, part)))
                return;
            view.GetZDO().Set(Hash, Write(broken));
            Read();
        }

        public static HashSet<(int Piece, int Part)> Parse(string? text)
        {
            HashSet<(int, int)> broken = new HashSet<(int, int)>();
            foreach (string entry in (text ?? "").Split(';'))
            {
                string[] f = entry.Split(':');
                if (f.Length == 2 && int.TryParse(f[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int piece)
                    && int.TryParse(f[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int part))
                    broken.Add((piece, part));
            }
            return broken;
        }

        public static string Write(HashSet<(int Piece, int Part)> broken)
        {
            List<(int Piece, int Part)> sorted = new List<(int, int)>(broken);
            sorted.Sort();
            StringBuilder text = new StringBuilder();
            foreach ((int piece, int part) in sorted)
                text.Append(text.Length > 0 ? ";" : "").Append(piece.ToString(CultureInfo.InvariantCulture)).Append(':').Append(part.ToString(CultureInfo.InvariantCulture));
            return text.ToString();
        }
    }
}
