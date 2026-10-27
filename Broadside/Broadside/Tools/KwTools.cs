using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using Splatform;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The planking, deck and railing tools. With one selected in the hammer, "placing" is a click: the first on a rib, the
    /// second on another rib or a keel end (<see cref="KwSpans"/> has the rules; with the deck tool a deck counts too, and
    /// the new deck carries its height on), or the first on a keel end and the second on the keel line's other end, for
    /// every part along it (<see cref="KwKeelRun"/>); the part between them is then built (<see cref="KwGenerated"/>): planking,
    /// a deck or a railing once the player has picked its biome tier (and a railing its posts' spacing) in its window
    /// (<see cref="KwSpanForm"/>); the railing tool's click on a cabin rails that roof (<see cref="KwRoofRailTool"/>). Every part is free.
    /// A deck goes at the heights clicked, level at their middle, or sloping from one to
    /// the other with Shift held on the second click. Clicking the same piece twice cancels. While a first click waits,
    /// <see cref="KwPreview"/> shows what the second would build. The tool itself is never placed. Every tool's click on a
    /// part that tool makes is offered to <see cref="KwEdit"/> first, which edits the part unless the tool still takes that
    /// click its own way; before that, a click on a part the tool does not take switches to that part's tool and edits it
    /// (<see cref="KwToolSwitch"/>). Runs on the clicking player's game; the part it spawns is a networked piece whose span travels in
    /// its ZDO.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class KwTools
    {
        private static KwClick? pending;
        private static string pendingTool = "";

        [HarmonyPrefix]
        private static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (piece == null || !piece.name.StartsWith(KwToolPieces.ToolPrefix))
                return true;
            __result = false;
            Guard.Run("Broadside tool click", Click, __instance, piece.name.Substring(KwToolPieces.ToolPrefix.Length));
            return false;
        }

        private static void Click(Player player, string tool)
        {
            if (KwSeaTools.Click(player, tool) || KwToolSwitch.Click(player, tool) || KwEdit.Click(player, tool) || OwnWay(player, tool))
                return;
            KwClick? aimed = Aim(player);
            KwClick? target = aimed != null ? Resolve(aimed.Value, tool) : null;
            if (target == null)
            {
                Say(player, Missed(tool, aimed));
                return;
            }
            KwClick? first = Pending(tool);
            if (first == null)
            {
                pending = target;
                pendingTool = tool;
                Say(player, $"{KwToolPieces.Label(tool)} from {target.Value.Piece.m_name}: {Next(target.Value)}");
                return;
            }
            pending = null;
            Second(player, tool, first.Value, target.Value);
        }

        // What a click on nothing the tool builds from says.
        private static string Missed(string tool, KwClick? aimed)
        {
            if (tool == KwBiomeCost.Rail)
                return "Click a rib, a keel end piece, the hull's planking or a cabin's roof";
            return tool == "deck" ? "Click a rib, a keel end piece or a deck" : "Click a rib or a keel end piece";
        }

        // What the player clicks next: from a rib, a rib or a keel end; from a keel end, a rib, or the keel line's other end.
        private static string Next(KwClick first) =>
            IsRib(first.Piece) ? "now click another rib or a keel end" : "now click a rib, or the keel line's other end to lay its whole length";

        // The ladder, keel, rib, sail and cabin tools take their clicks their own way; the railing tool its click on a
        // cabin's roof (KwRoofRailTool).
        private static bool OwnWay(Player player, string tool)
        {
            if (tool == KwLadderTool.Tool)
                KwLadderTool.Click(player);
            else if (tool == KwShapeTool.Keel || tool == KwShapeTool.Rib)
                KwShapeTool.Click(player, tool);
            else if (tool == KwSailTool.Tool)
                KwSailTool.Click(player);
            else if (tool == KwBowspritTool.Tool)
                KwBowspritTool.Click(player);
            else if (tool == KwCapTool.Tool)
                KwCapTool.Click(player);
            else if (!KwHouseTools.Click(player, tool) && !KwRoofRailTool.Click(player, tool))
                return false;
            return true;
        }

        private static void Second(Player player, string tool, KwClick first, KwClick second)
        {
            if (first.Piece == second.Piece)
            {
                Say(player, $"{KwToolPieces.Label(tool)} cancelled");
                return;
            }
            string? said = KwSpans.Plans(first, second, Slant(), tool, out System.Collections.Generic.List<KwPlan> plans);
            if (plans.Count > 0 && KwSpanForm.Takes(tool))
            {
                KwSpanForm.Open(player, tool, plans, said);
                return;
            }
            foreach (KwPlan plan in plans)
                Spawn(player, tool, plan.Position, Quaternion.LookRotation(plan.Direction), plan.Span.ToString());
            Say(player, plans.Count == 0 ? said ?? "Nothing to build there" : Built(tool, plans.Count, said));
        }

        // What the player reads once the parts a tool builds at once (railings) are built, with what was left out.
        private static string Built(string tool, int count, string? said)
        {
            string text = count == 1 ? $"{KwToolPieces.Label(tool)} built" : $"{count} {KwToolPieces.Label(tool).ToLowerInvariant()} parts built";
            return said != null ? text + "\n" + said : text;
        }

        /// <summary>
        /// A tool's generated part, networked, with what it builds from saved in its ZDO, its biome tier when it has one
        /// (<see cref="KwBiomeTiers.Key"/>) and the player as creator; a deck gets a seed of its own for its boards in its
        /// saved text (<see cref="KwSpan.Seeded"/>). A part built again by an edit keeps the old one's other values (keep,
        /// <see cref="KwEditWork"/>), its text and tier its own. Recorded for undo (<see cref="KwUndo"/>): the parts spawned
        /// in one frame are one action.
        /// </summary>
        public static GameObject Spawn(Player player, string tool, Vector3 at, Quaternion turn, string saved, KwBiomeTier? tier = null, List<KwBpValue>? keep = null)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(KwToolPieces.PartPrefix + tool);
            GameObject part = Object.Instantiate(prefab, at, turn);
            ZDO zdo = part.GetComponent<ZNetView>().GetZDO();
            if (keep != null)
                KwBpData.Apply(zdo, keep);
            zdo.Set(KwGenerated.Key, tool == "deck" ? KwSpan.Seeded(saved) : saved);
            if (tier != null)
                zdo.Set(KwBiomeTiers.Key, tier.Id);
            part.GetComponent<Piece>().SetCreator(player.GetPlayerID(), PlatformManager.DistributionPlatform.LocalUser.PlatformUserID);
            KwUndo.Made(player, part, tool, true);
            return part;
        }

        /// <summary>A tool's part built in a tier, and the player told so.</summary>
        public static void Build(Player player, string tool, Vector3 at, Quaternion turn, string saved, KwBiomeTier tier)
        {
            Spawn(player, tool, at, turn, saved, tier);
            Say(player, $"{Named(tool, tier)} built");
        }

        /// <summary>"Meadows planking": a tool's part in a tier, as the player reads it.</summary>
        public static string Named(string tool, KwBiomeTier tier) => $"{tier.Name} {KwToolPieces.Label(tool).ToLowerInvariant()}";

        /// <summary>The first click waiting for this tool, if its piece still exists.</summary>
        public static KwClick? Pending(string tool) =>
            pending != null && pendingTool == tool && pending.Value.Piece != null ? pending : null;

        /// <summary>The piece under the crosshair and the point on it, as the game's own placement ray sees it.</summary>
        public static KwClick? Aim(Player player)
        {
            Transform eye = GameCamera.instance.transform;
            if (!Physics.Raycast(eye.position, eye.forward, out RaycastHit hit, 50f, player.m_placeRayMask))
                return null;
            Piece? piece = hit.collider.GetComponentInParent<Piece>();
            return piece != null ? new KwClick(piece, hit.point) : (KwClick?)null;
        }

        /// <summary>
        /// Shift held: a deck's second click lays it sloping, and a click that edits a part edits every one of its kind on the
        /// dock (<see cref="KwEdit"/>); one click is never both (a waiting first click takes the next click as before).
        /// </summary>
        public static bool Slant() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        /// <summary>
        /// What a click means for a tool: a rib or a keel end as it is; for the deck tool, a deck stands for the rib at its
        /// end nearer the click, carrying the deck's height, and for the railing tool the hull's planking does; anything else, nothing.
        /// </summary>
        public static KwClick? Resolve(KwClick click, string tool)
        {
            if (IsRib(click.Piece) || KwKeelEnds.Of(click.Piece) != null)
                return click;
            string name = Utils.GetPrefabName(click.Piece.gameObject);
            bool stands = (tool == "deck" && name == KwToolPieces.PartPrefix + "deck") || (tool == KwBiomeCost.Rail && name == KwToolPieces.PartPrefix + "planking");
            return stands ? EndRib(click, tool == "deck") : null;
        }

        // A click on a deck (the deck tool, carrying the deck's height) or the hull's planking (the railing tool, where ribs
        // are hard to reach among what is built) taken as a click on the rib at the part's end nearer the click, else at its other end.
        private static KwClick? EndRib(KwClick click, bool deck)
        {
            ZDO? zdo = click.Piece.GetComponent<ZNetView>()?.GetZDO();
            if (zdo == null || !KwSpan.TryParse(zdo.GetString(KwGenerated.Key), out KwSpan span))
                return null;
            Vector3 start = click.Piece.transform.position, end = start + click.Piece.transform.forward * span.Length;
            bool endNearer = (click.Point - end).sqrMagnitude < (click.Point - start).sqrMagnitude;
            Piece? rib = RibAt(endNearer ? end : start) ?? RibAt(endNearer ? start : end);
            return rib != null ? new KwClick(rib, click.Point, deck ? click.Point.y - rib.transform.position.y : float.NaN) : (KwClick?)null;
        }

        // The rib standing at a point (its foot within 40 cm).
        private static Piece? RibAt(Vector3 point)
        {
            System.Collections.Generic.List<Piece> near = new System.Collections.Generic.List<Piece>();
            Piece.GetAllPiecesInRadius(point, 0.4f, near);
            return near.Find(IsRib);
        }

        public static bool IsRib(Piece piece) => KwRibs.Of(piece, out _, out _);

        private static void Say(Player player, string text) => player.Message(MessageHud.MessageType.Center, text);
    }
}
