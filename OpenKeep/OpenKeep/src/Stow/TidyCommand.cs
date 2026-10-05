using System.Collections.Generic;
using System.Linq;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Stow
{
    /// <summary>
    /// <c>openkeep tidy</c>: how Auto Tidy sees the chests within 20 m of the player - each chest's main items with
    /// their shares, its "random items" score, junk or not, the strays and kept items it holds and who owns it - and
    /// the looks so far with their timing and the learned pairs, for testing and for judging the load.
    /// </summary>
    public static class TidyCommand
    {
        private const float ListRange = 20f;
        private const int Shown = 4;

        public static void Run(Terminal.ConsoleEventArgs args)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                args.Context.AddString("OpenKeep: no local player.");
                return;
            }
            string state = StowSettings.Enabled.Value && StowSettings.AutoTidy.Value ? "on" : "off";
            TidyThemes.Refresh();
            args.Context.AddString($"OpenKeep: Auto Tidy is {state}; {TidySchedule.Stats()}; {TidyThemes.Learned} learned pairs.");
            Vector3 at = player.transform.position;
            foreach (Container chest in ContainerScan.All().Where(c => TidyChests.TakesPart(c) && Vector3.Distance(at, c.transform.position) <= ListRange))
                args.Context.AddString(Line(chest, Vector3.Distance(at, chest.transform.position)));
        }

        private static string Line(Container chest, float distance)
        {
            string head = $"  {ContainerScan.PrefabName(chest)}  {distance:0.0} m";
            if (!TidyChests.IsLoaded(chest))
                return head + "  not loaded";
            TidyProfile profile = TidyProfiles.Of(chest);
            string owner = chest.m_nview.IsOwner() ? "mine" : "other's";
            string main = string.Join(", ", profile.Prefabs.OrderByDescending(profile.Share).Take(Shown)
                .Select(prefab => $"{prefab} {profile.Share(prefab):P0}"));
            string junk = profile.IsJunk ? " JUNK" : "";
            return $"{head}  {owner}  random {profile.RandomScore:0.0}{junk}  [{main}]  strays: {Held(chest, profile.IsStray)}  kept: {Held(chest, profile.IsKept)}";
        }

        private static string Held(Container chest, System.Func<string, bool> which)
        {
            HashSet<string> found = new HashSet<string>();
            foreach (ItemDrop.ItemData item in chest.GetInventory().GetAllItems())
            {
                string prefab = ItemNames.PrefabName(item);
                if (which(prefab))
                    found.Add(prefab);
            }
            return found.Count == 0 ? "none" : string.Join(", ", found);
        }
    }
}
