using System.Collections.Generic;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// <c>elite purge</c>: removes the loaded creatures this mod has marked (a resolved controller carrying stars or
    /// mutations), for clearing the mess after testing. Destroying through <see cref="ZNetScene"/> takes the object off
    /// the network without running death, so it drops nothing - except stolen goods, which are the player's own
    /// property, not loot, and drop here before the destroy as the one documented exception. Runs on the admin's
    /// machine over the loaded creatures; ownership of each is claimed by the destroy call as needed.
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
                if (c == null)
                {
                    continue;
                }
                EliteController? controller = c.GetComponent<EliteController>();
                if (IsMarked(controller))
                {
                    DropPouchIfAny(c, controller!);
                    scene.Destroy(c.gameObject);
                    removed++;
                }
            }
            EliteCommands.Reply(args, $"elite purge: removed {removed} marked creature(s), no drops (stolen goods excepted).");
        }

        private static bool IsMarked(EliteController? controller) =>
            controller != null && controller.Ready && (controller.Traits.Any || controller.Traits.Stars > 0);

        private static void DropPouchIfAny(Character c, EliteController controller)
        {
            if (!controller.Traits.Has(Mutation.Thieving))
            {
                return;
            }
            ZNetView nview = c.GetComponent<ZNetView>();
            if (nview != null && nview.IsValid())
            {
                PouchDrop.DropAll(PouchStore.Load(nview.GetZDO()), c.transform.position);
            }
        }
    }
}
