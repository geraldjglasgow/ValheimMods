namespace GrindstoneSkills
{
    /// <summary>
    /// Animal items that carry stars the way dishes do, by joining <see cref="Kitchen"/>'s items (stars shown, stacks
    /// apart by star, counted in recipes whatever their star, starred ingredients raising a dish's odds). Runs on every
    /// machine, from <see cref="YieldDiscovery"/> and again whenever "Prime Cuts" or "Husbandry Enabled" changes.
    /// Adding is idempotent and nothing is removed while the game runs: turning a switch off keeps the items starred
    /// until a restart, as the Prime Cuts setting says.
    /// <list type="bullet">
    /// <item>Eggs: every item prefab with an EggGrow. The game already stores the laying hen's level in the egg's
    /// quality (a one-star hen lays quality 2 eggs, which hatch one-star chicks); as kitchen items those eggs show
    /// their star, stack apart and count in recipes. Added while Husbandry is on.</item>
    /// <item>Meat (<see cref="YieldCatalog.Meat"/>): added only while Husbandry and Prime Cuts are both on, because a
    /// kitchen ingredient counts in Cooking's ingredient average even at 0 stars.</item>
    /// </list>
    /// </summary>
    public static class YieldStarItems
    {
        public static void Register()
        {
            if (!HusbandrySkill.Active)
                return;
            int before = Kitchen.Count;
            RegisterEggs();
            if (HusbandryYieldSettings.PrimeCuts.Value)
                RegisterMeat();
            if (Kitchen.Count != before)
                GrindstoneSkills.Log.LogInfo($"Husbandry: items that carry stars now {Kitchen.Count} (eggs{(HusbandryYieldSettings.PrimeCuts.Value ? ", Prime Cuts meat" : "")}).");
        }

        private static void RegisterEggs()
        {
            if (ObjectDB.instance == null)
                return;
            foreach (ItemDrop egg in PrefabIndex.Items().Eggs)
                Kitchen.AddStarItem(egg);
        }

        private static void RegisterMeat()
        {
            foreach (ItemDrop item in YieldCatalog.Meat)
                Kitchen.AddStarItem(item);
        }
    }
}
