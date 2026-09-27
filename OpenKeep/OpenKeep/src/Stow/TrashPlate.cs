using System;
using PatchGuard;
using PlateColumn;
using UnityEngine;
using UnityEngine.UI;

namespace OpenKeep.Stow
{
    /// <summary>
    /// The trash can on its own plate in the column of stat plates on the player panel's right (the PlateColumn
    /// library, which our other mods add to as well): between the armour and the weight readouts, showing the bin in
    /// its own colours, with a tooltip saying how to use it. Clicking it with a dragged stack trashes the stack.
    /// Returns false, with nothing created, when the game's plates are missing or the bin image cannot be read, so the
    /// button row keeps the can.
    /// </summary>
    public static class TrashPlate
    {
        private const string Id = "openkeep_trash";

        /// <summary>Between the game's armour and weight plates.</summary>
        private const int Rank = (Column.ArmorRank + Column.WeightRank) / 2;

        public static bool TryCreate(InventoryGui gui, Action onClick)
        {
            Sprite bin = StowSprites.Bin;
            Plate plate = bin != null
                ? Column.Add(gui, new PlateSpec(Id, Rank, bin, false, StowWords.Trash, StowWords.DragHint))
                : null;
            if (plate == null)
            {
                Plugin.Log.LogInfo("trash can stays in the button row: the armour or weight plate, or the bin image, is missing");
                return false;
            }
            MakeButton(plate.Rect.gameObject, plate.Icon, onClick);
            Plugin.Log.LogInfo($"trash can on its own plate at {plate.Rect.anchoredPosition} from the player panel's top-right");
            return true;
        }

        /// <summary>Turns <paramref name="go"/> into the trash button: the bin lights up on hover and press.</summary>
        public static void MakeButton(GameObject go, Image bin, Action onClick)
        {
            Button button = go.GetComponent<Button>();
            if (button == null)
                button = go.AddComponent<Button>();
            button.targetGraphic = bin;
            ColorBlock colours = button.colors;
            colours.highlightedColor = new Color(1f, 0.55f, 0.45f, 1f);
            colours.selectedColor = colours.highlightedColor;
            colours.pressedColor = new Color(1f, 0.3f, 0.2f, 1f);
            button.colors = colours;
            button.onClick.AddListener(() => Guard.Run("trash can", onClick));
        }
    }
}
