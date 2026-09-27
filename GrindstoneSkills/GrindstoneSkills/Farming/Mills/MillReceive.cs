using HarmonyLib;
using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// Adding a starred item, on the mill's owner. <see cref="Keys.RpcAddMill"/> is registered on every Smelter's
    /// ZNetView beside the game's own (only when the view has a ZDO). The handler runs the game's RPC_AddOre, which queues
    /// the item only on the owner and only when it is allowed; while it runs, the stars it carried are what
    /// <see cref="MillQueue"/> records for the item queued. The game's own RPC_AddOre (0-star adds, other mods) records 0.
    /// </summary>
    internal static class MillReceive
    {
        /// <summary>The stars of the add being received, or 0 outside it.</summary>
        public static int Receiving { get; private set; }

        [HarmonyPatch(typeof(Smelter), nameof(Smelter.Awake))]
        private static class Register
        {
            [HarmonyPostfix]
            private static void Postfix(Smelter __instance)
            {
                ZNetView view = __instance.m_nview;
                if (view != null && view.GetZDO() != null)
                    view.Register<ZPackage>(Keys.RpcAddMill, (sender, package) => Guard.Run(Keys.RpcAddMill, () => Add(__instance, sender, package)));
            }
        }

        private static void Add(Smelter smelter, long sender, ZPackage package)
        {
            if (smelter == null)
                return;
            string name = package.ReadString();
            bool cheated = package.ReadBool();
            Receiving = package.ReadInt();
            try
            {
                smelter.RPC_AddOre(sender, name, cheated);
            }
            finally
            {
                Receiving = 0;
            }
        }
    }
}
