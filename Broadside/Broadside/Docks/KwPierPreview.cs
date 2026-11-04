using HarmonyLib;
using Hotkeys;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// Every frame the Dock tool is in the local player's hammer, after the game places its ghost: the wheel read
    /// (<see cref="KwToolWheel"/>), the game's ghost and marker hidden, and the dock a click would make worked out and shown
    /// (<see cref="KwPierView"/>): with no first click, a short stub from the start the aim gives (its way fixed by a dock
    /// aimed at), with one, the whole dock to the aim's end, both always at the width and in the material the wheel last
    /// set (a dock started on a dock too), so a turn of the wheel shows at once. The tool's panel shows the
    /// material, width and keys and what the click would do (<see cref="KwPierWords.Live"/>), and while a first click
    /// waits the tool's requirements are the planned dock's cost, so the build panel shows it as the game shows any
    /// piece's and the game checks it before the click (only for an item the player already knows, so the tool never
    /// leaves the hammer's list). The tool put away drops a first click, hides the preview and gives back the wheel. Local
    /// to this player; the plan is made again only when the aim, width, material or seed change.
    /// </summary>
    [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
    public static class KwPierPreview
    {
        private static string asked = "";
        private static KwPierPlan? planned;
        private static string note = "";

        [HarmonyPostfix]
        private static void Postfix(Player __instance, bool flashGuardStone)
        {
            if (__instance == Player.m_localPlayer && !flashGuardStone)
                Guard.Run("Broadside dock preview", Refresh, __instance);
        }

        private static void Refresh(Player player)
        {
            Piece? tool = KwPierTool.Held(player);
            bool free = tool != null && player.TakeInput() && !Hud.IsPieceSelectionVisible() && !Typing.Active;
            KwToolWheel.Own(free);
            if (tool == null)
            {
                Away();
                return;
            }
            if (free)
                KwPierTool.Steer(KwToolWheel.Notches(), KwToolWheel.Alt);
            Unghost(player);
            KwPierPlan? plan = Plan(player);
            KwPierView.Show(plan);
            Panel(player, tool, plan);
        }

        // The tool out of hand: no first click, no preview, the tool back as it rests in the hammer.
        private static void Away()
        {
            if (KwPierTool.Pending != null)
                KwPierTool.Cancel(null);
            KwPierView.Hide();
            (asked, planned) = ("", null);
            KwPierPrefabs.Rest();
        }

        // The game's ghost of the tool (a small dock) and its marker hidden: the preview stands for it.
        private static void Unghost(Player player)
        {
            if (player.m_placementGhost != null && player.m_placementGhost.activeSelf)
                player.m_placementGhost.SetActive(false);
            if (player.m_placementMarkerInstance != null && player.m_placementMarkerInstance.activeSelf)
                player.m_placementMarkerInstance.SetActive(false);
        }

        // The dock the next click would make, worked out again only when what it is made from changed.
        private static KwPierPlan? Plan(Player player)
        {
            KwPierStart? start = KwPierTool.Pending ?? KwPierAim.Start(player);
            if (start == null)
                return Keep("none", null, KwPierWords.AimStart);
            if (KwPierTool.Pending != null && start.OnDock && start.Joins == null)
            {
                KwPierTool.Cancel("The dock it started on is gone; dock cancelled");
                return Keep("gone", null, KwPierWords.AimStart);
            }
            Vector3? end = KwPierTool.Pending != null ? KwPierAim.End(player, start, out string? why) : Stub(start, out why);
            end = end != null && KwPierTool.Pending != null ? KwPierPlan.Snap(start, end.Value) : end;
            if (end == null)
                return Keep("noend", null, why ?? KwPierWords.AimEnd);
            (float width, KwPierKind kind) = (KwPierTool.Width, KwPierTool.Kind);
            string key = string.Join("|", start.At.ToString("F2"), (start.Way ?? Vector3.zero).ToString("F3"), Round(end.Value), width, kind.Id, KwPierTool.Seed, start.Why);
            if (key == asked && planned != null)
                return planned;
            bool stub = KwPierTool.Pending == null;
            KwPierPlan plan = KwPierPlan.Make(start, end.Value, width, kind, KwPierTool.Seed, false, stub);
            return Keep(key, plan, stub ? start.Why ?? $"Click to start it {start.Says}" : "");
        }

        // With no first click: a stub the shortest dock long, its way fixed by a dock aimed at, else the way the camera looks.
        private static Vector3? Stub(KwPierStart start, out string? why)
        {
            why = null;
            Vector3 look = GameCamera.instance.transform.forward;
            Vector3 way = start.Way ?? new Vector3(look.x, 0f, look.z).normalized;
            return start.At + (way.sqrMagnitude > 1e-6f ? way : Vector3.forward) * KwPierPlan.MinLength;
        }

        private static KwPierPlan? Keep(string key, KwPierPlan? plan, string words)
        {
            (asked, planned, note) = (key, plan, words);
            return plan;
        }

        // An end to the decimetre, so a still aim keeps its plan.
        private static string Round(Vector3 end) => $"{Mathf.Round(end.x * 10f)},{Mathf.Round(end.y * 10f)},{Mathf.Round(end.z * 10f)}";

        // The panel's lines, and the cost shown while a first click waits on a dock that may be built.
        private static void Panel(Player player, Piece tool, KwPierPlan? plan)
        {
            bool waiting = KwPierTool.Pending != null;
            string text = KwPierWords.Live(plan, note, waiting);
            if (tool.m_description != text)
                tool.m_description = text;
            bool shows = waiting && plan != null && plan.Why == null && Known(player, plan.Kind);
            tool.m_resources = shows ? KwPierPrefabs.Shown(plan!) : KwPierPrefabs.None;
        }

        // The player has met the material's item (the hammer lists a piece only when every item it needs is known).
        private static bool Known(Player player, KwPierKind kind)
        {
            ItemDrop? drop = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(kind.Item)?.GetComponent<ItemDrop>() : null;
            return drop != null && (player.NoCostCheat() || player.m_knownMaterial.Contains(drop.m_itemData.m_shared.m_name));
        }
    }
}
