using System.Collections.Generic;
using System.Globalization;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// <c>elite purge [radius]</c>: removes the loaded creatures this mod has marked (a resolved controller carrying
    /// stars or mutations), for clearing the mess after testing. With a radius in metres only those within that
    /// distance of the admin's player go; without one, every loaded marked creature does. Destroying through
    /// <see cref="ZNetScene"/> takes the object off the network without running death, so it drops nothing - except
    /// stolen goods, which are the player's own property, not loot, and drop here before the destroy as the one
    /// documented exception. Runs on the admin's machine over the loaded creatures; ownership of each is claimed by the
    /// destroy call as needed.
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
            if (!TryReach(args, out Vector3 centre, out float radius))
            {
                return;
            }
            int removed = Purge(scene, centre, radius);
            string where = radius > 0f ? $" within {radius.ToString("0.#", CultureInfo.InvariantCulture)} m" : "";
            EliteCommands.Reply(args,
                $"elite purge: removed {removed} marked creature(s){where}, no drops (stolen goods excepted).");
        }

        // No radius (0) means every loaded creature; a radius is measured from the local player.
        private static bool TryReach(Terminal.ConsoleEventArgs args, out Vector3 centre, out float radius)
        {
            centre = Vector3.zero;
            radius = 0f;
            if (args.Length < 3)
            {
                return true;
            }
            // Written as !(> 0) so NaN is refused too.
            if (!float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out radius)
                || !(radius > 0f))
            {
                EliteCommands.Reply(args, $"elite purge: '{args[2]}' is not a radius in metres (more than 0).");
                return false;
            }
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                EliteCommands.Reply(args, "elite purge: a radius needs a local player to measure from.");
                return false;
            }
            centre = player.transform.position;
            return true;
        }

        private static int Purge(ZNetScene scene, Vector3 centre, float radius)
        {
            int removed = 0;
            foreach (Character c in new List<Character>(Character.GetAllCharacters()))
            {
                if (c == null || !InReach(c, centre, radius))
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
            return removed;
        }

        private static bool InReach(Character c, Vector3 centre, float radius) =>
            radius <= 0f || Vector3.Distance(c.transform.position, centre) <= radius;

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
