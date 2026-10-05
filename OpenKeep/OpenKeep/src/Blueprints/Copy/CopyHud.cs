using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// The Copy tool's block of HUD lines ("copy", <see cref="BlueprintHud"/>): the selection (pieces, buildings and the
    /// footprint in the frame it would be saved in, its front toward the camera), the piece under the crosshair with
    /// what a click takes (the piece, with Shift its building, with G its joined pieces of one type) or a grey hint, and
    /// the keys. The selection line is made again only when the selection or the facing quarter changed.
    /// </summary>
    public static class CopyHud
    {
        public const string Block = "copy";

        private static string selectionLine;
        private static int lineVersion = -1;
        private static float lineYaw = -1f;

        public static void Show()
        {
            List<string> lines = new List<string>
            {
                "<b>" + SelectionLine() + "</b>",
                CopyAim.Piece != null ? AimLine(CopyAim.Piece) : BlueprintHud.Coloured(Language.Localize(CopyWords.AimHint), BlueprintHud.Grey),
                Language.Localize(CopyWords.Keys),
            };
            BlueprintHud.Set(Block, lines);
        }

        public static void Clear() => BlueprintHud.Clear(Block);

        private static string SelectionLine()
        {
            float yaw = BlueprintCapture.FacingYaw();
            if (selectionLine != null && lineVersion == CopySelection.Version && Mathf.Approximately(lineYaw, yaw))
                return selectionLine;
            selectionLine = Describe(yaw);
            lineVersion = CopySelection.Version;
            lineYaw = yaw;
            return selectionLine;
        }

        private static string Describe(float yaw)
        {
            if (CopySelection.Count == 0)
                return Language.Localize(CopyWords.TitleEmpty);
            Vector2 size = CopyFootprint.Of(CopySelection.Boxes, yaw);
            string across = BlueprintWords.Metres(size.x), deep = BlueprintWords.Metres(size.y);
            int buildings = CopySelection.Buildings;
            return buildings == 1 ? BlueprintWords.Format(CopyWords.TitleOne, CopySelection.Count, across, deep)
                : BlueprintWords.Format(CopyWords.Title, CopySelection.Count, buildings, across, deep);
        }

        private static string AimLine(Piece piece)
        {
            string line = BlueprintWords.Format(CopyWords.Aiming, Language.Localize(piece.m_name));
            string action = Action();
            return action != null ? line + "  -  " + action : line;
        }

        /// <summary>What a click does now, or null while the building under the crosshair is not known yet.</summary>
        private static string Action()
        {
            int count = CopyHover.Pieces.Count;
            bool drops = CopyHover.Drops;
            if (count == 0)
                return null;
            switch (CopyHover.Mode)
            {
                case CopyMode.Piece:
                    return Language.Localize(drops ? CopyWords.DropPiece : CopyWords.TakePiece);
                case CopyMode.SameType:
                    return BlueprintWords.Format(drops ? CopyWords.DropGroup : CopyWords.TakeGroup, count);
                default:
                    return BuildingAction(count, drops);
            }
        }

        /// <summary>A click on a building: selects it, selects the rest of a partly selected one, or lets it go.</summary>
        private static string BuildingAction(int count, bool drops)
        {
            if (drops)
                return BlueprintWords.Format(CopyWords.DropBuilding, count);
            int adds = CopyHover.Adds;
            return adds < count ? BlueprintWords.Format(CopyWords.TakeRest, adds, count) : BlueprintWords.Format(CopyWords.TakeBuilding, count);
        }
    }
}
