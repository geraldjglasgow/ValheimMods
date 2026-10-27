using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Doorway and Window tools' ghost (the user's words, 2026-10-25: "The tile that you're going to attach it to should
    /// not be blue, and it should actually show the object that you're trying to place, exactly how it would look if you did
    /// place it"): every part a plan changes stood in for by the part as it would build, in its own materials: its body made
    /// again from its new saved text with the openings cut (<see cref="KwGenerated.MeshFor"/>, a cabin's looks,
    /// <see cref="KwHouses.Layers"/>), planking's painted bands cut round them (<see cref="KwBandsShown"/>), and the frames
    /// with the leaves shut (<see cref="KwOpeningLooks.Of"/>) in the tier's wood and the game's iron. Where the plan only
    /// adds openings the part's own frames and leaves stay as they are and the ghost draws the new ones; where it changes
    /// or fills one in, the part's own are hidden and the ghost draws all. The part's own model is hidden meanwhile and
    /// shown again when the ghost goes. Made again only when the plan's openings change (<see cref="Key"/>), worked out once
    /// a plan. An edit window's ghost hangs the leaves part open (<see cref="KwOpeningGhostLooks.Frames"/>), so a cover's
    /// or a door's style shows; a placement's swings them fully open, as E would open them (they are built shut). Only drawn,
    /// local to this player.
    /// </summary>
    public static class KwOpeningGhost
    {
        private const float Again = 0.5f;
        private static readonly List<GameObject> stood = new List<GameObject>();
        private static readonly List<(Piece Piece, bool All)> standing = new List<(Piece, bool)>();
        private static readonly List<Renderer> hidden = new List<Renderer>();
        private static string key = "", planKey = "";
        private static KwOpeningPlan? planned;
        private static float ajar;
        private static float rehide;

        public static bool Shown => key.Length > 0;

        /// <summary>What the ghost shows: each part changed, by the piece, and its openings as they would be.</summary>
        public static string Key(KwOpeningPlan plan)
        {
            List<string> parts = new List<string>();
            foreach (KwPartCut cut in plan.Parts)
            {
                if (cut.Changed && Standable(cut.Site))
                    parts.Add(PartKey(cut.Site) + ":" + cut.Filled + ":" + KwOpeningText.Write(cut.Openings));
            }
            return string.Join(";", parts);
        }

        // Whether a site's part can be stood in for: a dock's piece, or a launched ship's part.
        private static bool Standable(KwOpeningSite site) => site.Piece != null || KwSeaOpeningPart.Of(site) != null;

        // A part's key in the ghost's: its piece, or its launched ship's index there and the ship's redraws.
        private static string PartKey(KwOpeningSite site) =>
            site.Piece != null ? site.Piece.GetInstanceID().ToString() : "sea" + KwSeaOpeningPart.Of(site)!.Index + "@" + (KwOpeningReads.Sea?.Version ?? 0);

        /// <summary>The plan's parts stood in for (none: the ghost gone), their leaves part open when asked; made again only when what it shows changes.</summary>
        public static void Show(KwOpeningPlan? plan, float open = 0f)
        {
            string next = plan != null && plan.Any ? KeyOf(plan) : "";
            if (next == key && (open == ajar || next.Length == 0))
            {
                Keep();
                return;
            }
            Hide();
            (key, ajar) = (next, open);
            if (next.Length == 0)
                return;
            foreach (KwPartCut cut in plan!.Parts)
            {
                if (cut.Changed && Standable(cut.Site))
                    TryStand(cut);
            }
        }

        /// <summary>The ghost gone, the parts' own models shown again.</summary>
        public static void Hide()
        {
            foreach (Renderer renderer in hidden)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
            hidden.Clear();
            foreach (GameObject made in stood)
            {
                if (made != null)
                    Object.Destroy(made);
            }
            stood.Clear();
            standing.Clear();
            KwSeaOpeningGhost.Forget();
            KwSeaOpeningPrepared.Sweep();
            key = "";
        }

        // One part's stand-in; a part that cannot be drawn so is left as it is.
        private static void TryStand(KwPartCut cut)
        {
            try
            {
                Stand(cut);
            }
            catch (System.Exception e)
            {
                Log.Warn($"Broadside openings: no ghost for a part ({e.Message})");
            }
        }

        private static void Stand(KwPartCut cut)
        {
            if (KwSeaOpeningPart.Of(cut.Site) is KwSeaOpeningPart part)
            {
                KwSeaOpeningGhost.Stand(cut, part, !Adds(cut), ajar, stood, Off);
                return;
            }
            Piece piece = cut.Site.Piece!;
            string? text = cut.Site.Text(cut.Openings);
            if (text == null)
                return;
            bool all = !Adds(cut);
            Transform at = piece.transform;
            GameObject root = new GameObject("KW_opening_ghost") { layer = piece.gameObject.layer };
            root.transform.SetPositionAndRotation(at.position, at.rotation);
            stood.Add(root);
            KwOpeningGhostLooks.Body(root.transform, piece, cut.Site, text);
            string? framed = all ? text : cut.Site.Text(cut.Shown.ConvertAll(s => s.Opening));
            if (framed != null)
                KwOpeningGhostLooks.Frames(root.transform, cut.Site, framed, ajar);
            standing.Add((piece, all));
            Conceal(piece, all);
        }

        // A plan's key, worked out once for the plan (the hover and an edit window hand the same plan every frame).
        private static string KeyOf(KwOpeningPlan plan)
        {
            if (!ReferenceEquals(plan, planned))
                (planned, planKey) = (plan, Key(plan));
            return planKey;
        }

        // Whether the plan only adds openings to the part (none of its own changed or filled in).
        private static bool Adds(KwPartCut cut) =>
            cut.Filled == 0 && cut.Shown.TrueForAll(s => !cut.Site.Openings.Exists(o => o.Id == s.Opening.Id && o.Wall == s.Opening.Wall));

        // The part's own model and painted bands hidden (and its frames and leaves, when the ghost draws them all).
        private static void Conceal(Piece piece, bool all)
        {
            Transform? model = piece.transform.Find("model");
            if (model != null)
            {
                Off(model.GetComponent<MeshRenderer>());
                foreach (KwBandsShown bands in model.GetComponentsInChildren<KwBandsShown>())
                    foreach (Renderer renderer in bands.GetComponentsInChildren<Renderer>())
                        Off(renderer);
            }
            Transform? holder = all ? piece.transform.Find(KwOpeningPart.Holder) : null;
            if (holder != null)
            {
                foreach (Renderer renderer in holder.GetComponentsInChildren<Renderer>())
                    Off(renderer);
            }
        }

        private static void Off(Renderer? renderer)
        {
            if (renderer == null || !renderer.enabled)
                return;
            renderer.enabled = false;
            hidden.Add(renderer);
        }

        // Twice a second the parts stood in for are hidden again (a part built again meanwhile brings new bands); a part gone takes its ghost.
        private static void Keep()
        {
            if (key.Length == 0 || Time.time < rehide)
                return;
            rehide = Time.time + Again;
            if (standing.Exists(s => s.Piece == null) || !KwSeaOpeningGhost.Keep(Off))
            {
                Hide();
                return;
            }
            foreach ((Piece piece, bool all) in standing)
                Conceal(piece, all);
        }
    }
}
