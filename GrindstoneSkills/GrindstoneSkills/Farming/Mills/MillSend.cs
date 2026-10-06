using HarmonyLib;

namespace GrindstoneSkills
{
    /// <summary>
    /// Adding a starred item to a mill, on the client that adds it. The game's Smelter.OnAddOre picks the item (the one
    /// used, else the first it can take), removes one from the inventory with Inventory.RemoveItem(item, 1), then sends
    /// "RPC_AddOre"(string name, bool cheated) to the owner through ZNetView.InvokeRPC. While OnAddOre runs, the stars
    /// of the removed unit are noted; when it has stars, that one send is swapped for <see cref="Keys.RpcAddMill"/>, which
    /// carries them too (<see cref="MillReceive"/>). Every check, the removal and the message stay the game's; a 0-star
    /// add, and any send that does not look like the game's, goes out untouched.
    /// </summary>
    internal static class MillSend
    {
        private const string VanillaRpc = "RPC_AddOre";

        private sealed class Adding
        {
            public ZNetView View;
            public int StarCount;
        }

        private static Adding adding;

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddOre))]
        private static class AddScope
        {
            [HarmonyPrefix]
            private static void Prefix(Smelter __instance) => adding = new Adding { View = __instance.m_nview };

            [HarmonyFinalizer]
            private static void Finalizer() => adding = null;
        }

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), typeof(ItemDrop.ItemData), typeof(int))]
        private static class Removed
        {
            [HarmonyPrefix]
            private static void Prefix(ItemDrop.ItemData item)
            {
                if (adding != null && item != null)
                    adding.StarCount = Stars.Get(item);
            }
        }

        /// <summary>
        /// From <see cref="RpcSendSwap"/> (ZNetView.InvokeRPC): true when this send was the game's add and went out as
        /// ours instead, so the game's own send is skipped.
        /// </summary>
        internal static bool Swapped(ZNetView view, string method, object[] parameters)
        {
            Adding add = adding;
            if (add == null || add.StarCount <= 0 || !ReferenceEquals(add.View, view) || method != VanillaRpc)
                return false;
            if (parameters == null || parameters.Length != 2 || !(parameters[0] is string name) || !(parameters[1] is bool cheated))
                return false;
            adding = null;
            view.InvokeRPC(Keys.RpcAddMill, Package(name, cheated, add.StarCount));
            return true;
        }

        private static ZPackage Package(string name, bool cheated, int stars)
        {
            ZPackage package = new ZPackage();
            package.Write(name);
            package.Write(cheated);
            package.Write(stars);
            return package;
        }
    }
}
