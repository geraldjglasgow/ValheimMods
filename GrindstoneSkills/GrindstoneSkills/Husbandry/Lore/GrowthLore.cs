namespace GrindstoneSkills
{
    /// <summary>
    /// The lore lines of what grows: a tamed young animal ("Grows up in 20 min") and a warm egg ("Hatches in 12 min"),
    /// with the growth factor of the best keeper near it now (<see cref="GrowingUp.Factor"/>). The game grows the young from its time since spawning
    /// (<c>Growup.m_growTime</c>) and hatches a single warm egg from when it became warm (<c>EggGrow.m_growTime</c>).
    /// </summary>
    public static class GrowthLore
    {
        public static string YoungLine(Character young)
        {
            Growup growup = young != null ? young.GetComponent<Growup>() : null;
            BaseAI ai = young != null ? young.GetBaseAI() : null;
            if (growup == null || ai == null || !young.IsTamed())
                return "";
            double left = growup.m_growTime / GrowingUp.Factor(young.transform.position) - ai.GetTimeSinceSpawned().TotalSeconds;
            return left > 0.0 ? $"Grows up in {LoreText.Duration(left)}" : "Grows up any moment";
        }

        public static string EggLine(EggGrow egg)
        {
            ZNetView nview = egg.m_nview;
            if (nview == null || !nview.IsValid() || egg.m_item == null || egg.m_item.m_itemData.m_stack > 1)
                return "";
            float start = nview.GetZDO().GetFloat(ZDOVars.s_growStart);
            if (start <= 0f)
                return "";
            double left = start + egg.m_growTime / GrowingUp.Factor(egg.transform.position) - ZNet.instance.GetTimeSeconds();
            return left > 0.0 ? $"Hatches in {LoreText.Duration(left)}" : "Hatches any moment";
        }
    }
}
