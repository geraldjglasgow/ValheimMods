using System.Collections.Generic;
using Hotkeys;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Post cap tool (the user's request, 2026-10-25: "The post cap should have its own tool, with easy selection
    /// between the different styles of post cap, and also let them adjust the size of the post cap"). Aiming at a stem or
    /// stern post's top (a keel's "Post top", as the bowsprit tool finds it) shows the cap there as it would be built, in
    /// the keel's tier (<see cref="KwCapGhost"/>); a click builds it (<see cref="KwCapSpec"/>, a generated part, free, one undo
    /// step), its base the post's own section, looking out along the keel (or inboard, as the last edit left the tool). The
    /// tool never turns: the wheel steps through the styles (<see cref="KwCaps.Styles"/>: the carved heads first), Shift +
    /// wheel makes the neck longer or shorter and Ctrl + wheel leans it forward or back, 5° a notch (the styles that lean,
    /// <see cref="KwCapStyle.Leans"/>; <see cref="KwToolNotches"/>), the placement message naming them. A click on a cap, or
    /// on the top of a post that has one, edits it (<see cref="KwCapEdit"/>, Shift: every cap on the dock; its window also
    /// turns a cap to look inboard), so a post never takes a second cap. The style, length, lean and facing stay the tool's
    /// for the session, as the last edit leaves them. Runs on the clicking player's game; the cap is a networked piece whose
    /// text and tier travel in its ZDO and build it on every peer (<see cref="KwGenerated"/>).
    /// </summary>
    public static class KwCapTool
    {
        public const string Tool = KwCapSpec.Tool;
        private const float LengthStep = 0.1f, LeanStep = 5f, ReachSquared = 4f, CapNear = 0.2f, Recheck = 0.5f;

        private static int style;
        private static float length = 1f, lean;
        private static bool inboard;
        private static Vector3 checkedTop;
        private static float checkedAt = -10f;
        private static bool checkedCapped, inHand;

        /// <summary>The tool in the hammer: its placeholder a dragon head on the standard post; the game turns it, so the wheel's notches reach the tool.</summary>
        public static KwDef Def() => new KwDef(KwToolPieces.ToolPrefix + Tool, KwToolWords.Name(Tool), "tool")
        {
            Description = KwToolWords.Of(Tool),
            Wood = true,
            Shape = b => KwCaps.Draw(b, Placeholder),
            Model = (scene, piece) => KwCapHeads.Model(piece, Placeholder),
            Setup = prefab => prefab.GetComponent<Piece>().m_canRotate = true,
        };

        // The placeholder: the carved dragon on the standard post (drawn as a scroll where the head is not loaded).
        private static KwCapSpec Placeholder => new KwCapSpec("dragon", KwCapForm.PostWidth, KwCapForm.PostDepth, 1f);

        /// <summary>The generated cap's prefab: dressed in its keel's tier, with its hover.</summary>
        public static void Part(GameObject prefab) => prefab.AddComponent<KwTierCap>();

        /// <summary>Whether the local player has the tool in hand.</summary>
        public static bool Held(Player player)
        {
            Piece? piece = player.InPlaceMode() ? player.GetSelectedPiece() : null;
            return piece != null && piece.name == KwToolPieces.ToolPrefix + Tool;
        }

        /// <summary>"Dragon head, length 1.0×, leaning 10° forward (1/9)": the tool's style, length, lean and facing now.</summary>
        public static string Now => $"{Current(KwCapForm.PostWidth, KwCapForm.PostDepth).Words} ({style + 1}/{KwCaps.Styles.Length})";

        /// <summary>The tool's cap on a base.</summary>
        public static KwCapSpec Current(float width, float depth) => new KwCapSpec(KwCaps.Styles[style].Id, width, depth, length, lean, inboard);

        /// <summary>Each frame: the style and length said when the tool is taken in hand, the wheel's notches taken while it is.</summary>
        public static void Frame(Player player, bool held)
        {
            if (held && !inHand)
                Say(player, Now);
            inHand = held;
            if (held)
                Wheel(player, KwToolNotches.Take(Tool));
        }

        // The wheel's notches: each a style along (Shift: a tenth longer or shorter; Ctrl: 5° more lean), the player told.
        private static void Wheel(Player player, int notches)
        {
            if (notches == 0)
                return;
            if (KwTools.Slant())
                length = Mathf.Clamp(Mathf.Round((length + notches * LengthStep) * 10f) / 10f, KwCapSpec.MinLength, KwCapSpec.MaxLength);
            else if (Leaning())
                lean = Mathf.Clamp(Mathf.Round((lean + notches * LeanStep) / LeanStep) * LeanStep, -KwCapSpec.MaxLean, KwCapSpec.MaxLean);
            else
                style = ((style + notches) % KwCaps.Styles.Length + KwCaps.Styles.Length) % KwCaps.Styles.Length;
            Say(player, Leaning() && !KwCaps.Styles[style].Leans ? Now + "\nOnly the long necks lean: the heads, the scroll and the tail curl" : Now);
        }

        // Ctrl held: the wheel leans the neck.
        private static bool Leaning() => Hotkey.KeyHeld(KeyCode.LeftControl) || Hotkey.KeyHeld(KeyCode.RightControl);

        /// <summary>An edited cap's style, length, lean and facing become the tool's.</summary>
        public static void Keep(KwCapSpec spec)
        {
            style = Mathf.Max(0, KwCaps.IndexOf(spec.Style));
            (length, lean, inboard) = (spec.Length, spec.Lean, spec.Inboard);
        }

        public static void Click(Player player)
        {
            if (KwForm.IsOpen)
                return;
            KwClick? aim = KwTools.Aim(player);
            checkedAt = -Recheck;
            KwCapSite? site = aim != null ? Site(aim.Value) : null;
            if (site == null || site.Capped)
            {
                Say(player, site == null ? "Aim at the top of a stem or stern post" : "This post has a cap already");
                return;
            }
            KwCapSpec spec = Current(site.Width, site.Depth);
            KwTools.Spawn(player, Tool, site.Top, site.Turn, spec.ToString(), site.Tier);
            checkedCapped = true;
            Say(player, $"{spec.Words}: post cap built");
        }

        /// <summary>The post top aimed at, with no cap on it; null for anything else.</summary>
        public static KwCapSite? Hover(Player player)
        {
            KwClick? aim = KwTools.Aim(player);
            KwCapSite? site = aim != null ? Site(aim.Value) : null;
            return site != null && !site.Capped ? site : null;
        }

        // The clicked keel's post top nearest the click (within 2 m): the cap faces out along the keel, past its post.
        private static KwCapSite? Site(KwClick aim)
        {
            if (!KwKeel.IsKeel(aim.Piece))
                return null;
            Transform? top = null;
            float best = ReachSquared;
            foreach (Transform child in aim.Piece.transform)
            {
                float sq = (child.position - aim.Point).sqrMagnitude;
                if (child.name == "Post top" && child.CompareTag("snappoint") && sq < best)
                    (top, best) = (child, sq);
            }
            Vector3 axis = aim.Piece.transform.forward;
            if (top == null || new Vector2(axis.x, axis.z).sqrMagnitude < 1e-4f)
                return null;
            KwBiomeTier tier = KwBiomeTiers.Of(aim.Piece.GetComponent<ZNetView>()?.GetZDO());
            return new KwCapSite(top.position, KwShapeSites.Facing(axis, axis), KwKeel.BodyOf(aim.Piece), tier, Capped(top.position));
        }

        /// <summary>The cap on the post top a click on a keel aims at (an edit of it, <see cref="KwCapEdit.Over"/>); null for none.</summary>
        public static Piece? CapOn(KwClick aim)
        {
            KwCapSite? site = Site(aim);
            return site != null && site.Capped ? CapAt(site.Top) : null;
        }

        // Whether a cap already stands on a post's top, looked up again only when the top changes or half a second passes.
        private static bool Capped(Vector3 top)
        {
            if ((top - checkedTop).sqrMagnitude < 1e-6f && Time.time - checkedAt < Recheck)
                return checkedCapped;
            (checkedTop, checkedAt, checkedCapped) = (top, Time.time, CapAt(top) != null);
            return checkedCapped;
        }

        // The cap standing on a post's top (its origin within 20 cm); null for none.
        private static Piece? CapAt(Vector3 top)
        {
            List<Piece> near = new List<Piece>();
            Piece.GetAllPiecesInRadius(top, 1f, near);
            return near.Find(p => p != null && KwCaps.Is(Utils.GetPrefabName(p.gameObject)) && (p.transform.position - top).sqrMagnitude < CapNear * CapNear);
        }

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }

    /// <summary>A post's top a cap may go on: the place and the way out, the keel's thickness (its post's section) and tier, and whether a cap is on it.</summary>
    public sealed class KwCapSite
    {
        public KwCapSite(Vector3 top, Quaternion turn, KwKeelBody body, KwBiomeTier tier, bool capped)
        {
            Top = top;
            Turn = turn;
            Body = body;
            Tier = tier;
            Capped = capped;
        }

        public Vector3 Top { get; }
        public Quaternion Turn { get; }
        public KwKeelBody Body { get; }
        public KwBiomeTier Tier { get; }
        public bool Capped { get; }

        /// <summary>The post's section across the ship and along the keel.</summary>
        public float Width => Body.PostHalf * 2f;
        public float Depth => Body.PostDepth;
    }
}
