using HarmonyLib;
using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// Adding a mead base, on the barrel's ZDO owner. <see cref="Keys.RpcAddBase"/> is registered on every fermenter's
    /// ZNetView beside the game's own RPCs (Fermenter.Awake registers those only when the view has a ZDO, and so does
    /// this). The handler runs the game's RPC_AddItem, which fills the barrel only on the owner and only when it is empty
    /// and the item allowed. Whenever RPC_AddItem fills the barrel, the base's stars and the cook's level are written
    /// to its ZDO: the values from our RPC, or 0 and 0 when the game's own RPC_AddItem arrived from anything else.
    /// </summary>
    internal static class FermenterAddReceive
    {
        /// <summary>What our RPC carried, while its handler runs the game's RPC_AddItem.</summary>
        private struct Added
        {
            public int StarCount;
            public float Level;
        }

        private static Added? receiving;

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.Awake))]
        private static class Register
        {
            [HarmonyPostfix]
            private static void Postfix(Fermenter __instance)
            {
                ZNetView view = __instance.m_nview;
                if (view != null && view.GetZDO() != null)
                    view.Register<ZPackage>(Keys.RpcAddBase, (sender, package) => Receive(__instance, sender, package));
            }
        }

        private static void Receive(Fermenter fermenter, long sender, ZPackage package)
        {
            Guard.Run(Keys.RpcAddBase, () => AddBase(fermenter, sender, package));
        }

        private static void AddBase(Fermenter fermenter, long sender, ZPackage package)
        {
            if (fermenter == null)
                return;
            int nameHash = package.ReadInt();
            bool cheated = package.ReadBool();
            receiving = new Added { StarCount = package.ReadInt(), Level = package.ReadSingle() };
            try
            {
                fermenter.RPC_AddItem(sender, nameHash, cheated);
            }
            finally
            {
                receiving = null;
            }
        }

        [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.RPC_AddItem))]
        private static class WriteBase
        {
            [HarmonyPrefix]
            private static void Prefix(Fermenter __instance, out bool __state)
            {
                __state = __instance.m_nview != null && __instance.m_nview.IsOwner() && __instance.GetContent() == 0;
            }

            [HarmonyPostfix]
            private static void Postfix(Fermenter __instance, int nameHash, bool __state)
            {
                if (!__state || nameHash == 0 || __instance.GetContent() != nameHash)
                    return;
                Added added = receiving ?? default;
                FermenterBase.Write(__instance.m_nview.GetZDO(), added.StarCount, added.Level);
            }
        }
    }
}
