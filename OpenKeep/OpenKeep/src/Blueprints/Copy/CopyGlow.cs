using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// The Copy tool's glow on the real pieces, on this machine only, with the game's own piece tint: the colour and
    /// emission WearNTear.Highlight sets through MaterialMan when the hammer hovers a piece, but held until taken back.
    /// The selection glows gold, what a click would add pale blue, what it would let go red. What changed is worked out
    /// only when the selection, the hover or the visibility changed, and at most <see cref="PerFrame"/> pieces are set
    /// or cleared a frame, so a selection of thousands costs nothing while it stands still and never drops frames when
    /// it changes. While the Copy entry is selected the game's own hover tint is held back, and a piece whose tint the
    /// game resets anyway is set again (<see cref="CopyHighlightPatch"/>, <see cref="CopyHighlightResetPatch"/>).
    /// </summary>
    public static class CopyGlow
    {
        private enum Kind
        {
            None,
            Selected,
            Add,
            Drop,
        }

        /// <summary>Pieces set or cleared in one frame at most (MaterialMan redraws each one in its next update).</summary>
        private const int PerFrame = 200;

        /// <summary>The emission is the colour times this, as the game's own hover tint.</summary>
        private const float Emission = 0.4f;

        private static readonly Color SelectedColour = new Color(1f, 0.8f, 0.25f);
        private static readonly Color AddColour = new Color(0.45f, 0.8f, 1f);
        private static readonly Color DropColour = new Color(1f, 0.4f, 0.3f);
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        /// <summary>Every piece this tool has tinted, with the tint it has now.</summary>
        private static readonly Dictionary<Piece, Kind> lit = new Dictionary<Piece, Kind>();
        private static readonly Queue<Piece> todo = new Queue<Piece>();
        private static readonly HashSet<Piece> queued = new HashSet<Piece>();
        private static HashSet<Piece> hover = new HashSet<Piece>();
        private static bool hoverDrops;
        private static bool showing;
        private static int selectionVersion = -1;
        private static int hoverVersion = -1;

        /// <summary>Per frame: works out what changed when the selection, the hover or <paramref name="shown"/> did, then sets the next pieces.</summary>
        public static void Tick(bool shown)
        {
            if (shown != showing || selectionVersion != CopySelection.Version || hoverVersion != CopyHover.Version)
                Plan(shown);
            int budget = PerFrame;
            while (budget-- > 0 && todo.Count > 0)
            {
                Piece piece = todo.Dequeue();
                queued.Remove(piece);
                Apply(piece, Wanted(piece));
            }
        }

        /// <summary>WearNTear.ResetHighlight postfix: the game took the tint off a piece this tool lights; it is set again.</summary>
        public static void Restore(WearNTear wear)
        {
            Piece piece = wear != null ? wear.GetComponent<Piece>() : null;
            if (piece == null || !lit.ContainsKey(piece))
                return;
            lit.Remove(piece);
            Enqueue(piece);
        }

        /// <summary>
        /// Queues every piece whose tint may be wrong now: after a hover change only the old and the new hover; after a
        /// selection or visibility change also every lit piece and the selection. Each piece is set to what it should be
        /// when its turn comes, so a piece queued twice over is still set once.
        /// </summary>
        private static void Plan(bool shown)
        {
            bool everything = selectionVersion != CopySelection.Version || shown != showing;
            showing = shown;
            selectionVersion = CopySelection.Version;
            hoverVersion = CopyHover.Version;
            HashSet<Piece> before = hover;
            hover = shown ? new HashSet<Piece>(CopyHover.Pieces.Select(p => p.Piece)) : new HashSet<Piece>();
            hoverDrops = CopyHover.Drops;
            Check(everything ? lit.Keys : (IEnumerable<Piece>)before);
            Check(hover);
            if (shown && everything)
                Check(CopySelection.Pieces);
        }

        private static void Check(IEnumerable<Piece> pieces)
        {
            foreach (Piece piece in pieces)
            {
                if (Wanted(piece) != Current(piece))
                    Enqueue(piece);
            }
        }

        private static void Enqueue(Piece piece)
        {
            if (!ReferenceEquals(piece, null) && queued.Add(piece))
                todo.Enqueue(piece);
        }

        /// <summary>The tint a piece should have: what the hover would do to it, else selected or nothing.</summary>
        private static Kind Wanted(Piece piece)
        {
            if (!showing || piece == null)
                return Kind.None;
            bool selected = CopySelection.Contains(piece);
            if (hover.Contains(piece))
                return hoverDrops ? Kind.Drop : selected ? Kind.Selected : Kind.Add;
            return selected ? Kind.Selected : Kind.None;
        }

        /// <summary>The tint set now (a piece reference that was never anything has none).</summary>
        private static Kind Current(Piece piece) => !ReferenceEquals(piece, null) && lit.TryGetValue(piece, out Kind kind) ? kind : Kind.None;

        /// <summary>Sets or takes back the tint through MaterialMan; a piece that is gone is only forgotten (its tint went with it).</summary>
        private static void Apply(Piece piece, Kind kind)
        {
            if (kind == Current(piece))
                return;
            MaterialMan man = MaterialMan.instance;
            if (piece == null || man == null)
            {
                lit.Remove(piece);
                return;
            }
            GameObject go = piece.gameObject;
            if (kind == Kind.None)
            {
                man.ResetValue(go, ColorId);
                man.ResetValue(go, EmissionId);
                lit.Remove(piece);
                return;
            }
            Color colour = kind == Kind.Selected ? SelectedColour : kind == Kind.Add ? AddColour : DropColour;
            man.SetValue(go, ColorId, colour);
            man.SetValue(go, EmissionId, colour * Emission);
            lit[piece] = kind;
        }
    }
}
