using HarmonyLib;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The Dock tool (<c>BS_tool_pier</c>, DOCKS.md): its "placing" is two clicks, on the placing player's game. The first
    /// sets the start (<see cref="KwPierAim.Start"/>): on the shore or ground, or on a dock, continuing it straight on from
    /// its end or branching square off its side at its deck's height (at the tool's width and material, as the preview
    /// shows, whatever the dock it starts on); the second, on the water or the ground, sets the end and builds the dock (<see cref="KwPierPlan"/>) as
    /// sections placed through the game's own placement (<see cref="KwPierBuild"/>). Before the click the game has checked
    /// the workbench in range and the cost the tool shows (<see cref="KwPierPreview"/> sets it to the dock aimed at); the
    /// click checks the cost again for the dock it builds, sets it as the tool's requirements, and the game takes it as it
    /// takes any piece's (none with the world's free build key). The width (<see cref="MinWidth"/> to
    /// <see cref="MaxWidth"/> in <see cref="WidthStep"/> steps) and the material are kept for the next dock; Esc or a right
    /// click cancels a first click (<see cref="KwPierCancel"/>), and putting the tool away drops it.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
    public static class KwPierTool
    {
        public const float MinWidth = 1.5f, MaxWidth = 6f, WidthStep = 0.5f;
        private static KwPierStart? pending;
        private static int seed = NewSeed();

        /// <summary>The width and material the next dock is built at.</summary>
        public static float Width { get; private set; } = 3f;
        public static KwPierKind Kind { get; private set; } = KwPierKinds.Wood;

        /// <summary>The first click waiting for its second; null while none does.</summary>
        public static KwPierStart? Pending => pending;

        /// <summary>The seed the next dock's boards are laid from (the preview's and the build's, so they match).</summary>
        public static int Seed => seed;

        [HarmonyPrefix]
        private static bool Prefix(Player __instance, Piece piece, ref bool __result)
        {
            if (piece == null || piece.name != KwPierPrefabs.Tool)
                return true;
            __result = Guard.Run<Player, Piece, bool>("Broadside dock tool", Click, __instance, piece);
            return false;
        }

        /// <summary>The tool in this player's hammer, or null.</summary>
        public static Piece? Held(Player player)
        {
            Piece? piece = player.InPlaceMode() ? player.GetSelectedPiece() : null;
            return piece != null && piece.name == KwPierPrefabs.Tool ? piece : null;
        }

        // The first click sets the start; the second builds (true: the game then takes the cost the tool shows).
        private static bool Click(Player player, Piece piece) => pending == null ? First(player) : Second(player, piece, pending);

        private static bool First(Player player)
        {
            KwPierStart? start = KwPierAim.Start(player);
            if (start == null || start.Why != null)
                return Say(player, start?.Why ?? KwPierWords.AimStart);
            pending = start;
            return Say(player, $"Dock started {start.Says}: now click the water where it ends (right click or Esc cancels)");
        }

        private static bool Second(Player player, Piece piece, KwPierStart start)
        {
            if (start.OnDock && start.Joins == null)
            {
                Cancel(null);
                return Say(player, "The dock it started on is gone; dock cancelled");
            }
            Vector3? end = KwPierAim.End(player, start, out string? why);
            if (end == null)
                return Say(player, why ?? KwPierWords.AimEnd);
            KwPierPlan plan = KwPierPlan.Make(start, KwPierPlan.Snap(start, end.Value), Width, Kind, seed, true);
            return plan.Why != null ? Say(player, plan.Why) : Build(player, piece, plan);
        }

        // The cost shown and checked as the game counts it, the sections placed; true when any was.
        private static bool Build(Player player, Piece piece, KwPierPlan plan)
        {
            piece.m_resources = KwPierCost.Requirements(plan.Kind, plan.Cost, false);
            bool free = player.NoCostCheat() || ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey());
            string? missing = free ? null : KwBpCost.Missing(player, KwPierCost.Items(plan.Kind, plan.Cost));
            if (missing != null)
                return Say(player, $"This dock needs {missing}");
            int made = KwPierBuild.Spawn(player, plan);
            (pending, seed) = (null, NewSeed());
            if (made == 0)
                return Say(player, "The dock could not be made (the log says why)");
            Say(player, KwPierWords.Built(plan, made));
            return true;
        }

        /// <summary>The wheel's notches: with Alt the width, else the material; nothing while no notch turned.</summary>
        public static void Steer(int notches, bool alt)
        {
            if (notches == 0)
                return;
            if (alt)
                Width = Mathf.Clamp(Snap(Width + notches * WidthStep), MinWidth, MaxWidth);
            else
                Kind = KwPierKinds.Step(Kind, notches);
        }

        /// <summary>The first click forgotten, the player told (words) when there are any.</summary>
        public static void Cancel(string? words)
        {
            pending = null;
            if (words != null && Player.m_localPlayer != null)
                Say(Player.m_localPlayer, words);
        }

        private static float Snap(float width) => Mathf.Clamp(Mathf.Round(width / WidthStep) * WidthStep, MinWidth, MaxWidth);

        private static int NewSeed() => (int)((uint)System.Environment.TickCount * 2654435761u >> 1) | 1;

        private static bool Say(Player player, string text)
        {
            player.Message(MessageHud.MessageType.Center, text);
            return false;
        }
    }
}
