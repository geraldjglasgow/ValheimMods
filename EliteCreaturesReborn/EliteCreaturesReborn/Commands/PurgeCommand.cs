using System.Collections.Generic;
using EliteCreaturesReborn.Runtime;
using UnityEngine;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// <c>elite purge</c>: removes the loaded creatures this mod has marked (a resolved controller carrying stars or
    /// mutations), for clearing the mess after testing. Destroying through <see cref="ZNetScene"/> takes the object off
    /// the network without running death, so it drops nothing. Runs on the admin's machine over the loaded creatures;
    /// ownership of each is claimed by the destroy call as needed.
    /// </summary>
    public static class PurgeCommand
    {
        public static void Run(Terminal.ConsoleEventArgs args)
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
            {
                EliteCommands.Reply(args, "elite purge: no scene loaded.");
                return;
            }
            List<Character> all = new List<Character>(Character.GetAllCharacters());
            int removed = 0;
            foreach (Character c in all)
            {
                if (IsMarked(c))
                {
                    scene.Destroy(c.gameObject);
                    removed++;
                }
            }
            EliteCommands.Reply(args, $"elite purge: removed {removed} marked creature(s), no drops.");
        }

        private static bool IsMarked(Character c)
        {
            EliteController? controller = c != null ? c.GetComponent<EliteController>() : null;
            return controller != null && controller.Ready && (controller.Traits.Any || controller.Traits.Stars > 0);
        }
    }
}
