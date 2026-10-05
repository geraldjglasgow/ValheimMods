using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// The Blueprints tab's Fix ground entry while it is selected: the building under the crosshair (or the pinned
    /// one) with the outline of the ground it would get and the HUD (pieces, how many touch the ground, the earth to cut
    /// and fill with its stone, or why not). The first click pins the building, the next one fixes its ground;
    /// Backspace lets go, Alt+Z puts the ground of the last fix back. Re-checked when the building changes and every
    /// second, less often when checking takes long.
    /// </summary>
    public static class GroundFixSession
    {
        private const float RecheckInterval = 1f;

        private static Piece pinned;
        private static GroundFixPlan plan;
        private static float lastPlan = -100f;
        private static float planCost;

        public static bool Pinned => pinned != null;

        public static void Tick()
        {
            Player player = Player.m_localPlayer;
            if (!Selected(player, menuClosed: true))
            {
                Hide(release: !Selected(player, menuClosed: false));
                return;
            }
            HandleKeys();
            Piece start = pinned != null ? pinned : GroundFixBuilding.Aimed();
            if (start == null)
            {
                GroundOutline.Hide();
                BlueprintHud.Set(BlueprintHud.FixBlock, new List<string> { BlueprintHud.Coloured(Language.Localize(BlueprintWords.FixAim), BlueprintHud.Grey) });
                return;
            }
            Refresh(start, player);
            if (plan != null)
                GroundOutline.Show(plan.Work, plan.Target.Under);
            ShowHud();
        }

        private static bool Selected(Player player, bool menuClosed)
        {
            if (!BlueprintSettings.Enabled || player == null || player.IsDead() || !player.InPlaceMode())
                return false;
            return (!menuClosed || !Hud.IsPieceSelectionVisible()) && BlueprintMenu.IsFix(player.GetSelectedPiece());
        }

        private static void Refresh(Piece start, Player player)
        {
            bool moved = plan == null || plan.Start != start;
            float now = Time.realtimeSinceStartup;
            float gap = Mathf.Clamp(planCost * 10f, 0.2f, 2f);
            if (now < lastPlan + (moved ? gap : Mathf.Max(RecheckInterval, gap)))
                return;
            plan = BlueprintSafe.Call("OpenKeep fix ground plan", () => GroundFixPlan.Make(start, player), null);
            lastPlan = Time.realtimeSinceStartup;
            planCost = lastPlan - now;
        }

        private static void ShowHud()
        {
            if (plan == null)
                return;
            List<string> lines = new List<string> { "<b>" + BlueprintWords.Format(BlueprintWords.FixTitle, plan.Building.Count, plan.TouchingCount) + "</b>" };
            if (plan.TouchingCount > 0)
                BlueprintHud.AddGround(lines, plan.Work, plan.Bill);
            lines.Add(plan.Problem != null ? BlueprintHud.Coloured(plan.Problem, BlueprintHud.Red)
                : Language.Localize(Pinned ? BlueprintWords.FixPinned : BlueprintWords.FixClick));
            BlueprintHud.Set(BlueprintHud.FixBlock, lines);
        }

        /// <summary>The click (from <see cref="BlueprintTool"/>): pin the building under the crosshair, or fix the pinned one's ground.</summary>
        public static void OnClick(Player player)
        {
            if (pinned == null)
            {
                pinned = GroundFixBuilding.Aimed();
                Messages.Center(pinned != null ? BlueprintWords.FixPinned : BlueprintWords.FixAim);
                return;
            }
            GroundFixPlan now = GroundFixPlan.Make(pinned, player);
            if (now.Problem != null)
            {
                Messages.Center(now.Problem);
                return;
            }
            now.Apply(player);
            plan = null;
            pinned = null;
        }

        private static void HandleKeys()
        {
            Player player = Player.m_localPlayer;
            if (player == null || !player.TakeInput() || Keys.TextInputActive)
                return;
            if (Input.GetKeyDown(BlueprintRules.ReleaseKey) && pinned != null)
            {
                pinned = null;
                Messages.Center(BlueprintWords.Unpinned);
            }
            if (BlueprintKeys.Alt && Input.GetKeyDown(KeyCode.Z))
                Undo();
        }

        private static void Undo()
        {
            int back = GroundUndo.LastWasFix ? GroundUndo.Restore() : 0;
            Messages.Center(back > 0 ? BlueprintWords.Format(BlueprintWords.FixUndone, back) : BlueprintWords.FixNoUndo);
            plan = null;
        }

        private static void Hide(bool release)
        {
            GroundOutline.Hide();
            BlueprintHud.Clear(BlueprintHud.FixBlock);
            if (release)
                pinned = null;
        }
    }
}
