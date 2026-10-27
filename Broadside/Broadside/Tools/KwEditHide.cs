using System;
using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The parts an edit changes hidden on this player's screen while its window is open (the user's request, 2026-10-25:
    /// "it shouldn't show you your current setup ... It should only show the new post spacing. If they cancel, it should
    /// re-show what they had before"), so only the preview of the new values shows (<see cref="KwEditPreview"/>, or the sail
    /// and bowsprit tools' own); the pieces moving with them too (<see cref="Follow"/>: the cabins on an edited cabin's roof and
    /// what stands on them, as the values change). Every renderer under each part, its children too (hung shields, sails, ropes, painted bands,
    /// a hold's floor, openings' leaves), is switched off with <c>Renderer.forceRenderingOff</c>, which leaves its enabled
    /// state, its colliders and the ZDO as they are: nothing is sent and no other screen changes. A renderer something else
    /// already switched off this way is left to it. Children a part adds while hidden (built again, a sail rigged) are found
    /// four times a second. Shown again exactly as they were: when the window ends (OK, Cancel, Esc, the player dying,
    /// teleporting or leaving, another window opening, <see cref="KwForm"/>), when the edit's tool is no longer in hand (the
    /// window is closed then), on any frame no edit is open (a throw that skipped the end), and on a throw here; a part
    /// removed meanwhile is dropped. On OK the parts being replaced stay hidden until they are gone (their owner removes
    /// them), at most <see cref="Grace"/> seconds, so the old and the new never show together; the rest show at once.
    /// </summary>
    public static class KwEditHide
    {
        /// <summary>How long a part being replaced stays hidden at most, waiting for its removal to reach this screen.</summary>
        public const float Grace = 3f;

        private const float Rescan = 0.25f;
        private static readonly List<Piece> parts = new List<Piece>(), followers = new List<Piece>();
        private static readonly HashSet<Renderer> hidden = new HashSet<Renderer>();
        private static readonly List<Renderer> found = new List<Renderer>();
        private static string held = "";
        private static float rescanAt, releaseBy = -1f;
        private static KwEditHideTicker? ticker;

        /// <summary>Whether any part is hidden now.</summary>
        public static bool Hiding => parts.Count > 0 || followers.Count > 0 || hidden.Count > 0;

        /// <summary>An edit's parts hidden while its window is open, the edit's tool in hand (anything hidden before shown first).</summary>
        public static void Hide(string tool, IEnumerable<Piece> pieces)
        {
            ShowAll();
            held = KwToolPieces.ToolPrefix + tool;
            foreach (Piece piece in pieces)
            {
                if (piece != null)
                    parts.Add(piece);
            }
            Scan();
            Wake();
        }

        /// <summary>
        /// The pieces moving with the edit's parts (<see cref="KwEditFollow"/>: a cabin's stack), hidden too while the window is
        /// open, as they change with its values: one no longer moving shown again.
        /// </summary>
        public static void Follow(List<Piece> moving)
        {
            foreach (Piece piece in followers)
            {
                if (piece != null && !moving.Contains(piece))
                    Show(piece);
            }
            followers.Clear();
            followers.AddRange(moving.FindAll(piece => piece != null && !parts.Contains(piece)));
            if (followers.Count > 0)
                Scan();
        }

        /// <summary>
        /// The edit ended: every part shown again, but the ones taken away by OK (gone), kept hidden until their removal
        /// reaches this screen or <see cref="Grace"/> seconds pass.
        /// </summary>
        public static void Ended(ICollection<Piece>? gone)
        {
            if (gone == null || gone.Count == 0)
            {
                ShowAll();
                return;
            }
            foreach (Piece piece in Hidden())
            {
                if (piece != null && !gone.Contains(piece))
                    Show(piece);
            }
            parts.RemoveAll(piece => piece == null || !gone.Contains(piece));
            followers.RemoveAll(piece => piece == null || !gone.Contains(piece));
            releaseBy = Time.time + Grace;
            if (parts.Count == 0 && followers.Count == 0)
                ShowAll();
        }

        // The edit's own parts and the pieces moving with them.
        private static List<Piece> Hidden()
        {
            List<Piece> all = new List<Piece>(parts);
            all.AddRange(followers);
            return all;
        }

        /// <summary>Every hidden part shown again, as it was.</summary>
        public static void ShowAll()
        {
            foreach (Renderer renderer in hidden)
            {
                if (renderer != null)
                    renderer.forceRenderingOff = false;
            }
            hidden.Clear();
            parts.Clear();
            followers.Clear();
            (held, releaseBy) = ("", -1f);
        }

        /// <summary>
        /// Every frame while hiding: after OK, the parts shown once gone or late; while the window is open, everything shown
        /// (and the window closed) when no edit is open or its tool is no longer in hand; new children hidden now and then.
        /// </summary>
        internal static void Tick()
        {
            if (!Hiding)
            {
                Sleep();
                return;
            }
            if (releaseBy >= 0f)
            {
                if (Time.time > releaseBy || Hidden().TrueForAll(piece => piece == null))
                    ShowAll();
                return;
            }
            if (!KwEdit.IsOpen || !KwForm.IsOpen || !Holding())
            {
                Abandon();
                return;
            }
            if (Time.time >= rescanAt)
                Scan();
        }

        // The window closed (its end shows the parts), and whatever the end did not do done: no edit open, no preview, everything shown.
        private static void Abandon()
        {
            if (KwEdit.IsOpen && KwForm.IsOpen)
                KwForm.Close();
            KwEdit.Drop();
            KwEditPreview.Clear();
            ShowAll();
        }

        // The local player, alive, holding the edit's tool in the hammer.
        private static bool Holding()
        {
            Player player = Player.m_localPlayer;
            Piece? piece = player != null && !player.IsDead() && player.InPlaceMode() ? player.GetSelectedPiece() : null;
            return piece != null && piece.name == held;
        }

        // Every renderer under the parts still there switched off, once (the parts gone dropped).
        private static void Scan()
        {
            rescanAt = Time.time + Rescan;
            parts.RemoveAll(piece => piece == null);
            followers.RemoveAll(piece => piece == null);
            foreach (Piece piece in Hidden())
            {
                piece.GetComponentsInChildren(true, found);
                foreach (Renderer renderer in found)
                {
                    if (renderer != null && !renderer.forceRenderingOff && hidden.Add(renderer))
                        renderer.forceRenderingOff = true;
                }
            }
            found.Clear();
        }

        // One part's renderers this hid shown again.
        private static void Show(Piece piece)
        {
            piece.GetComponentsInChildren(true, found);
            foreach (Renderer renderer in found)
            {
                if (renderer != null && hidden.Remove(renderer))
                    renderer.forceRenderingOff = false;
            }
            found.Clear();
        }

        // The object that ticks, made once a world (it goes with the world's scene, showing everything as it goes).
        private static void Wake()
        {
            if (ticker == null)
                ticker = new GameObject("KwEditHide").AddComponent<KwEditHideTicker>();
            ticker.enabled = true;
        }

        private static void Sleep()
        {
            if (ticker != null)
                ticker.enabled = false;
        }

        internal static void Lost(KwEditHideTicker gone)
        {
            if (ticker == gone)
                ticker = null;
            ShowAll();
        }
    }

    /// <summary>Ticks <see cref="KwEditHide"/> every frame while it hides anything; a throw shows everything again.</summary>
    public sealed class KwEditHideTicker : MonoBehaviour
    {
        private void Update()
        {
            try
            {
                KwEditHide.Tick();
            }
            catch (Exception e)
            {
                Log.Warn($"Broadside: hiding the parts being edited failed; every part shown again: {e}");
                KwEditHide.ShowAll();
            }
        }

        private void OnDestroy() => KwEditHide.Lost(this);
    }
}
