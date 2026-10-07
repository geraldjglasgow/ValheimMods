namespace GrindstoneSkills
{
    /// <summary>
    /// Eggs show the laying hen's star. The game already stores the hen's level in the egg's quality (a one-star hen
    /// lays quality 2 eggs, which hatch one-star chicks); every item prefab with an EggGrow joins <see cref="Stars"/>'
    /// items, so those eggs show their star and stack apart. Runs on every
    /// machine while Husbandry is on, from <see cref="YieldDiscovery"/> and again whenever "Husbandry Enabled" changes.
    /// Adding is idempotent and nothing is removed while the game runs: turning Husbandry off keeps the eggs' badges
    /// until a restart.
    /// </summary>
    public static class YieldStarItems
    {
        public static void Register()
        {
            if (!HusbandrySkill.Active || ObjectDB.instance == null)
                return;
            int before = Stars.Count;
            foreach (ItemDrop egg in PrefabIndex.Items().Eggs)
                Stars.Add(egg);
            if (Stars.Count != before)
                GrindstoneSkills.Log.LogInfo($"Husbandry: eggs show their hen's star ({Stars.Count} star items).");
        }
    }
}
