using System;
using System.Collections.Generic;
using UnityEngine;
using Wayfare.Portals;

namespace Wayfare.Targeting
{
    /// <summary>The picker's destinations, from the portal snapshot (<see cref="PortalRegistry"/>): every named portal but
    /// the one the player stands at that the player may target (the map icons' access rule, <see cref="PortalAccess"/>),
    /// favourites first, then by name, then nearest first; each labelled with its name and its distance from here in
    /// grey, which also tells two portals of one name apart.</summary>
    internal static class PickerChoices
    {
        private const string DistanceColour = "#A0A0A0";

        public static void Fill(List<PortalInfo> into, ZDOID here, Vector3 from)
        {
            into.Clear();
            long playerId = Player.m_localPlayer.GetPlayerID();
            bool isAdmin = ZNet.instance != null && ZNet.instance.LocalPlayerIsAdminOrHost();
            foreach (PortalInfo info in PortalRegistry.Portals)
            {
                if (info.Id != here && PortalFields.HasName(info.Tag) && PortalAccess.MayTarget(info.Mode, info.Owner, playerId, isAdmin))
                    into.Add(info);
            }
            into.Sort((a, b) => Compare(a, b, from));
        }

        private static int Compare(PortalInfo a, PortalInfo b, Vector3 from)
        {
            bool favouriteA = PlayerFavourites.IsFavourite(a.Id);
            if (favouriteA != PlayerFavourites.IsFavourite(b.Id))
                return favouriteA ? -1 : 1;
            int byName = string.Compare(a.Tag.Trim(), b.Tag.Trim(), StringComparison.CurrentCultureIgnoreCase);
            if (byName != 0)
                return byName;
            return Vector3.Distance(from, a.Position).CompareTo(Vector3.Distance(from, b.Position));
        }

        public static List<string> Labels(List<PortalInfo> choices, Vector3 from)
        {
            List<string> labels = new List<string>(choices.Count);
            foreach (PortalInfo info in choices)
                labels.Add(info.Tag.Trim() + "  <color=" + DistanceColour + ">" + Distance(Vector3.Distance(from, info.Position)) + "</color>");
            return labels;
        }

        private static string Distance(float metres)
        {
            return metres < 1000f ? Mathf.RoundToInt(metres) + " m" : (metres / 1000f).ToString("0.0") + " km";
        }
    }
}
