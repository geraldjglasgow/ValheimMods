using System.Collections.Generic;
using EarthWright.Core;
using EarthWright.Protection;

namespace EarthWright.Extras
{
    /// <summary>
    /// The wild pickables one uproot click may remove: those inside the footprint (<see cref="NaturalPickables"/>) that
    /// this player may touch - no ward of someone else over them (the first refusing ward flashes, as the game does),
    /// and nothing the Protection module's object rules refuse (terrain tools switch, terrain lock, admin zones, combat
    /// lock). The first refusal is kept to tell the player why nothing happened.
    /// </summary>
    public sealed class UprootTargets
    {
        public readonly List<Pickable> Allowed = new List<Pickable>();

        /// <summary>Why a wild plant in the footprint was left alone (the first reason), or null.</summary>
        public string Refusal;

        public static UprootTargets Find(Player player, Footprint area)
        {
            UprootTargets targets = new UprootTargets();
            string tool = LocalTool.RightItemName;
            foreach (Pickable pickable in NaturalPickables.In(area))
            {
                string reason = targets.RefusalFor(player, pickable, tool);
                if (reason == null)
                    targets.Allowed.Add(pickable);
                else if (targets.Refusal == null)
                    targets.Refusal = reason;
            }
            return targets;
        }

        private string RefusalFor(Player player, Pickable pickable, string tool)
        {
            UnityEngine.Vector3 position = pickable.transform.position;
            if (!PrivateArea.CheckAccess(position, 0f, Refusal == null))
                return "$msg_privatezone";
            return Safe.Call("EarthWright uproot rules", () => ObjectRules.Refusal(player, position, tool), null);
        }
    }
}
