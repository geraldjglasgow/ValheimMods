using UnityEngine;

namespace OpenKeep.Signs
{
    /// <summary>
    /// An automatic sign floats above its container and never crumbles or wears: the game's <c>WearNTear</c> fields
    /// are named backwards, <c>m_noSupportWear = true</c> means "take damage without support" (its <c>UpdateWear</c>
    /// deals 100 damage per wear tick then) and <c>m_noRoofWear = true</c> means "get wet and wear without a roof",
    /// so both are cleared on the loaded instance. Only the owner runs wear, and every owner runs the mod; the
    /// prefab is untouched, so player-placed signs keep their vanilla wear.
    /// </summary>
    public static class SignWear
    {
        public static void Protect(GameObject sign)
        {
            WearNTear wear = sign != null ? sign.GetComponent<WearNTear>() : null;
            if (wear == null)
                return;
            wear.m_noSupportWear = false;
            wear.m_noRoofWear = false;
        }
    }
}
