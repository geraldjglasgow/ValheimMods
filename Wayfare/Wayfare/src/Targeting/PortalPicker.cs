using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wayfare.Core;
using Wayfare.Portals;

namespace Wayfare.Targeting
{
    /// <summary>The TargetTeleport mode (General, Teleport Mode, the user's design 2026-10-08): walking into a named portal
    /// opens the picker window (<see cref="PickerWindow"/>) with a dropdown of every other named portal the player may
    /// target (<see cref="PickerChoices"/>). Teleport asks the server exactly as a click on the map does
    /// (<see cref="TeleportGate.RequestTeleport"/>), and the grant ends here (<see cref="Complete"/>). Cancel, Esc
    /// (WindowInput), walking away from the portal, death or another mode closes it. The list refills while open when
    /// the portal snapshot or the favourites change (the server's answer to the refresh on opening), never under an open
    /// list, and keeps the destination chosen.</summary>
    public static class PortalPicker
    {
        private const float LeaveDistance = 4f;

        private static readonly List<PortalInfo> choices = new List<PortalInfo>();
        private static PickerWindow window;
        private static TeleportWorld source;
        private static ZDOID chosen = ZDOID.None;
        private static int filledPortals = -1;
        private static int filledFavourites = -1;

        public static bool IsOpen => window != null && window.Visible;

        public static void Open(TeleportWorld portal)
        {
            Player player = Player.m_localPlayer;
            if (player == null || portal == null || portal.m_nview == null || !portal.m_nview.IsValid() || (IsOpen && portal == source))
                return;
            if (!PortalFields.HasName(portal.m_nview.GetZDO()))
            {
                player.Message(MessageHud.MessageType.Center, Words.PickerUnnamed);
                return;
            }
            if (window == null)
                window = PickerWindow.Build();
            if (window == null)
                return;
            if (InventoryGui.IsVisible() && InventoryGui.instance != null)
                InventoryGui.instance.Hide(); // as the map pickers do (MapOpening)
            Show(portal);
        }

        private static void Show(TeleportWorld portal)
        {
            source = portal;
            chosen = ZDOID.None;
            PortalRegistry.Refresh(); // a portal built since the last check shows once the server answers
            Refill();
            window.Show(Words.PickerTopic, Words.PickerGo);
        }

        public static void Close()
        {
            if (window != null)
                window.Hide();
            source = null;
        }

        /// <summary>The window's Teleport button: the chosen destination, asked of the server.</summary>
        public static void Go()
        {
            if (!IsOpen || !Here(out ZDOID here) || chosen == ZDOID.None)
                return;
            TeleportGate.RequestTeleport(here, chosen);
        }

        /// <summary>The server's grant for this picker: the player steps out of the destination, the window closes.</summary>
        public static void Complete(Vector3 targetPos, Quaternion targetRot)
        {
            if (PortalTravel.Go(source, targetPos, targetRot))
                Close();
        }

        /// <summary>Every frame the window is shown.</summary>
        internal static void Tick()
        {
            if (MustClose())
            {
                Close();
                return;
            }
            if (!window.Dropdown.IsExpanded && (filledPortals != PortalRegistry.Version || filledFavourites != PlayerFavourites.Version))
                Refill();
        }

        private static bool MustClose()
        {
            Player player = Player.m_localPlayer;
            if (!WayfareConfig.InMode(TeleportMode.TargetTeleport) || player == null || player.IsDead() || !Here(out _))
                return true;
            return Vector3.Distance(player.transform.position, source.transform.position) > LeaveDistance;
        }

        private static bool Here(out ZDOID here)
        {
            bool valid = source != null && source.m_nview != null && source.m_nview.IsValid();
            here = valid ? source.m_nview.GetZDO().m_uid : ZDOID.None;
            return valid;
        }

        private static void Refill()
        {
            filledPortals = PortalRegistry.Version;
            filledFavourites = PlayerFavourites.Version;
            if (!Here(out ZDOID here) || Player.m_localPlayer == null)
                return;
            Vector3 from = source.transform.position;
            PickerChoices.Fill(choices, here, from);
            Present(choices.Count > 0 ? PickerChoices.Labels(choices, from) : null);
        }

        /// <summary>The labels into the dropdown, the chosen destination kept where it is still listed, else the first;
        /// with none, one grey line saying so and Teleport off.</summary>
        private static void Present(List<string> labels)
        {
            Button go = window.Go;
            TMP_Dropdown dropdown = window.Dropdown;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.ClearOptions();
            go.interactable = labels != null;
            dropdown.interactable = labels != null;
            if (labels == null)
            {
                chosen = ZDOID.None;
                dropdown.AddOptions(new List<string> { Localization.instance.Localize(Words.PickerNone) });
                return;
            }
            dropdown.AddOptions(labels);
            int index = Mathf.Max(0, choices.FindIndex(info => info.Id == chosen));
            chosen = choices[index].Id;
            dropdown.SetValueWithoutNotify(index);
            dropdown.onValueChanged.AddListener(Choose);
        }

        private static void Choose(int index)
        {
            if (index >= 0 && index < choices.Count)
                chosen = choices[index].Id;
        }
    }
}
