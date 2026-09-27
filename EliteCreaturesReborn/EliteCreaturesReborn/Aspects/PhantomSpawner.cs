using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Scaling;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using UnityEngine;

namespace EliteCreaturesReborn.Aspects
{
    /// <summary>
    /// One Phantom split, on the boss's owner, each time its health falls past a `split at` mark
    /// (<see cref="PhantomBehaviour"/>): its copies appear in an even ring around it - same prefab, stars, size and name
    /// - `per player` for each player online, so a player alone faces one and a group of four faces four. Each is marked
    /// in its own ZDO with the boss it belongs to. The mark is what makes a copy a copy on every machine
    /// (<see cref="PhantomBody"/>), and what lets the boss's death find and dismiss them (<see cref="PhantomReaper"/>).
    /// </summary>
    internal static class PhantomSpawner
    {
        private const float Radius = 5f;

        /// <summary>A sanity cap on one split, however many players are online or whatever the rule file says.</summary>
        private const int MaxCopies = 16;

        public static void Spawn(EliteController boss)
        {
            int players = PlayersOnline();
            int copies = Mathf.Clamp(
                Mathf.RoundToInt(players * AspectMath.Power(Aspect.Phantom, Fields.PerPlayer)), 0, MaxCopies);
            ZDOID bossId = boss.View.GetZDO().m_uid;
            float start = Random.Range(0f, 360f);
            CreatureTraits traits = new CreatureTraits(boss.Traits.Stars, Aspect.Phantom);
            for (int i = 0; i < copies; i++)
            {
                Vector3 pos = SpawnPlace.Around(boss.transform.position, start + 360f * i / copies, Radius);
                BossCopy.Make(boss, pos, traits, copy => AspectStore.SetPhantomOf(copy, bossId));
            }
            Log.Diag($"{boss.name}: split off {copies} phantom copies for {players} players online");
        }

        /// <summary>
        /// Everyone on the server, whichever machine owns the boss: the server keeps the list and sends it to every
        /// client, and it never counts a dedicated server itself. Never less than one.
        /// </summary>
        private static int PlayersOnline() => ZNet.instance != null ? Mathf.Max(1, ZNet.instance.GetNrOfPlayers()) : 1;
    }
}
