using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// While a tool's first click waits, hovering a second rib or keel end shows what clicking it would build: the same
    /// part (every part, from one keel end to the other, <see cref="KwKeelRun"/>), see-through (its texture at a third of its opacity: planking and decks in the biome tier their window last
    /// picked, then in the one it picks while it is open, <see cref="KwSpanForm"/>), at the place it would go; nothing when that span
    /// would not build; with the ladder tool, the ladder at the snapped spot nearest the aim (<see cref="KwLadderTool"/>);
    /// with the railing tool's roof window open, the roof railing (<see cref="KwRoofRailTool"/>). While a tool's window is
    /// open the tool's own placeholder is hidden, so only the part being made shows. Runs after the game updates its placement ghost; the part's mesh is rebuilt only when what it
    /// would be changes. Local to this player, never networked.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwPreview
    {
        private const float Opacity = 0.35f;
        private static GameObject? shown;
        private static Mesh? mesh;
        private static string key = "";
        private static readonly System.Collections.Generic.Dictionary<Material, Material> glass = new System.Collections.Generic.Dictionary<Material, Material>();

        [HarmonyPostfix]
        private static void Postfix(Player __instance) => Guard.Run("Broadside preview", Refresh, __instance);

        private static void Refresh(Player player)
        {
            if (player != Player.m_localPlayer)
                return;
            string? tool = SelectedTool(player);
            KwOpeningHover.Unless(tool);
            if (tool != null && KwForm.IsOpen && player.m_placementGhost != null)
                player.m_placementGhost.SetActive(false);
            if (OwnPreview(player, tool))
                return;
            KwClick? first = tool != null ? KwTools.Pending(tool) : null;
            KwClick? aimed = first != null ? KwTools.Aim(player) : null;
            KwClick? hover = aimed != null ? KwTools.Resolve(aimed.Value, tool!) : null;
            List<KwPlan>? plans = null;
            if (hover != null && hover.Value.Piece != first!.Value.Piece)
                KwSpans.Plans(first.Value, hover.Value, KwTools.Slant(), tool!, out plans, true);
            if (plans == null || plans.Count == 0)
                Hide();
            else
                Show(tool!, KwRailField.Spaced(tool!, plans), LastTier(tool!));
        }

        // The tier the tool's window last picked (the window opens on it); Meadows for a tool the tiers do not dress.
        private static KwBiomeTier LastTier(string tool)
        {
            KwPart? part = tool == KwBiomeCost.Rail ? KwPart.Deck : KwBiomeTiers.PartOf(tool);
            return part != null ? KwBiomeField.Last(part.Value) : KwBiomeTiers.Meadows;
        }

        private static string? SelectedTool(Player player)
        {
            Piece? piece = player.InPlaceMode() ? player.GetSelectedPiece() : null;
            return piece != null && piece.name.StartsWith(KwToolPieces.ToolPrefix) ? piece.name.Substring(KwToolPieces.ToolPrefix.Length) : null;
        }

        // An open edit shows its parts (KwEdit); the ladder, keel, rib, sail and cabin tools show their part their own
        // way, the planking, deck and railing tools while their window is open, the railing tool's roof window its railing.
        private static bool OwnPreview(Player player, string? tool)
        {
            if (KwEdit.Preview())
                return true;
            if (KwSeaTools.Hover(player, tool))
            {
                Hide();
                return true;
            }
            if (KwSpanForm.IsOpenFor(tool))
                KwSpanForm.Preview();
            else if (tool == KwLadderTool.Tool)
                Ladder(player);
            else if (tool == KwShapeTool.Keel || tool == KwShapeTool.Rib)
                KwShapeTool.Preview();
            else if (tool == KwSailTool.Tool)
                KwSailTool.Preview();
            else if (tool == KwBowspritTool.Tool)
                KwBowspritTool.Preview();
            else if (!KwHouseTools.Preview(tool) && !KwRoofRailTool.Preview(tool))
                return false;
            return true;
        }

        // The ladder tool: the ladder at the snapped spot nearest the aim, on a hatch or an open edge, down to what it stands on.
        private static void Ladder(Player player)
        {
            KwLadderPlan? plan = KwLadderTool.Hover(player);
            if (plan == null)
                Hide();
            else
                Show("ladder|" + plan, plan.Top, plan.Turn, _ => KwLadder.Mesh(plan.Length), false);
        }

        /// <summary>
        /// A tool's planned parts (one, or every part of a run between keel ends) where they would go, in one see-through
        /// mesh placed from the first, in a biome tier's look (railings in the plank wood).
        /// </summary>
        public static void Show(string tool, List<KwPlan> plans, KwBiomeTier tier)
        {
            KwPart? part = KwBiomeTiers.PartOf(tool);
            Material? look = part != null ? KwBiomeLooks.For(part.Value, tier) : KwPrefabs.Planks;
            KwPlan first = plans[0];
            float step = plans.Count > 1 ? 0.05f : 0.001f;
            string next = tool + "|" + tier.Id + "|" + string.Join(";", plans.ConvertAll(p => Key(tool, p, step)));
            Show(next, first.Position, Quaternion.LookRotation(first.Direction), _ => plans.Count == 1 ? Part(tool, first, tier) : Parts(tool, plans, tier), look);
        }

        // What a planned part shows, to know when the preview must be made again: its span and place, a deck's heights to a
        // step (a millimetre; 5 cm while many parts show, which are all made again each time), other parts' heights not at
        // all (they draw none, and the clicks' heights change as the aim moves).
        private static string Key(string tool, KwPlan plan, float step)
        {
            KwSpan s = plan.Span;
            string heights = tool == "deck" ? $"|{Mathf.Round(s.DeckA / step)}|{Mathf.Round(s.DeckB / step)}" : "";
            return string.Join("|", s.Family, s.FrameA, s.FrameB, s.Length.ToString("F3"), s.End, s.Rail) + heights + "|" + plan.Position.ToString("F2") + "|" + plan.Direction.ToString("F2");
        }

        // The planned part's mesh, a deck in its tier's boards, planking's planks run on from where it stands, seated on the
        // keels loaded, as the part will build (KwGenerated.MeshFor).
        private static Mesh Part(string tool, KwPlan plan, KwBiomeTier tier)
        {
            KwDef def = new KwSeatAt(KwKeelLines.World(), plan.Position, plan.Direction).Seated(plan.Span).Def(tool);
            if (tool == "deck")
                def.Boards = KwDeckLayout.Of(tier);
            Mesh made = KwMesh.Build(def);
            if (tool == "planking")
                KwPlankShift.Apply(made, plan.Position, plan.Direction);
            return made;
        }

        // Several planned parts in one mesh, each placed from the first.
        private static Mesh Parts(string tool, List<KwPlan> plans, KwBiomeTier tier)
        {
            Vector3 origin = plans[0].Position;
            Quaternion back = Quaternion.Inverse(Quaternion.LookRotation(plans[0].Direction));
            CombineInstance[] parts = plans.ConvertAll(p => new CombineInstance
            {
                mesh = Part(tool, p, tier), transform = Matrix4x4.TRS(back * (p.Position - origin), back * Quaternion.LookRotation(p.Direction), Vector3.one),
            }).ToArray();
            Mesh mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts, true, true);
            foreach (CombineInstance one in parts)
                Object.Destroy(one.mesh);
            return mesh;
        }

        /// <summary>The preview at a place, its mesh made again only when what it shows (next) changes; hull or plank wood.</summary>
        public static void Show(string next, Vector3 position, Quaternion turn, System.Func<Transform, Mesh> build, bool hull) =>
            Show(next, position, turn, build, hull ? KwPrefabs.Hull : KwPrefabs.Planks);

        /// <summary>The same in a material of its own, see-through (a part's tier look).</summary>
        public static void Show(string next, Vector3 position, Quaternion turn, System.Func<Transform, Mesh> build, Material? solid)
        {
            GameObject preview = Preview();
            preview.SetActive(true);
            if (next == key)
                return;
            key = next;
            preview.transform.SetPositionAndRotation(position, turn);
            if (mesh != null)
                Object.Destroy(mesh);
            mesh = build(preview.transform);
            preview.GetComponent<MeshFilter>().sharedMesh = mesh;
            preview.GetComponent<MeshRenderer>().sharedMaterial = Glass(solid);
        }

        public static void Hide()
        {
            if (shown != null && shown.activeSelf)
                shown.SetActive(false);
            key = "";
            KwLookPreview.Hide();
        }

        private static GameObject Preview()
        {
            if (shown != null)
                return shown;
            shown = new GameObject("KwPreview");
            shown.AddComponent<MeshFilter>();
            MeshRenderer renderer = shown.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return shown;
        }

        // The texture on a see-through shader the game ships (the sprite shader, else the UI's); the solid material when
        // neither is there.
        internal static Material? Glass(Material? solid)
        {
            if (solid == null)
                return null;
            if (glass.TryGetValue(solid, out Material cached))
                return cached;
            Shader? shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader == null)
                return solid;
            cached = new Material(shader) { mainTexture = solid.mainTexture, color = new Color(1f, 1f, 1f, Opacity), renderQueue = 3000 };
            glass[solid] = cached;
            return cached;
        }
    }
}
