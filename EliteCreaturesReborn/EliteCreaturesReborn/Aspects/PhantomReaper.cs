using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// When a Phantom boss dies, its remaining copies go with it. Runs on the dying boss's owner from the death patch;
    /// the copies stand around the boss, so every one still standing is loaded here. Each is sent the vanish through its
    /// own network view, to whichever machine owns it, which lets it fall - hollow, so no drops and no body.
    /// </summary>
    internal static class PhantomReaper
    {
        public static void Release(ZDOID boss)
        {
            if (boss == ZDOID.None)
            {
                return;
            }
            int released = 0;
            foreach (Character character in Character.GetAllCharacters())
            {
                if (IsCopyOf(character, boss))
                {
                    CreatureRpc.Vanish(character);
                    released++;
                }
            }
            Log.Diag($"phantom boss {boss} fell; {released} copies released");
        }

        private static bool IsCopyOf(Character character, ZDOID boss)
        {
            ZNetView nview = character.GetComponent<ZNetView>();
            ZDO? zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
            return zdo != null && !character.IsDead() && AspectStore.GetPhantomOf(zdo) == boss;
        }
    }
}
