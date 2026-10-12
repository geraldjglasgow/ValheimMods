using System.Collections.Generic;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The creatures near the local player for <see cref="Vanish"/>, on the local player's own client. They come from the
    /// game's own list of every loaded creature AI (BaseAI.BaseAIInstances, kept by the game as creatures load and
    /// unload) with squared distances; nothing searches the world or allocates, apart from the one RPC per creature told.
    /// <list type="bullet">
    /// <item>A hostile creature is a monster (MonsterAI; animals that only flee, AnimalAI, are not), alive and hostile to
    /// the player by the game's own rule (BaseAI.IsEnemy: tamed creatures are not).</item>
    /// <item>A hunter is a hostile creature, not a boss, that hunts the player. On this client's own creatures that is
    /// known: its target (MonsterAI.m_targetCreature) is the player. Another machine's creature keeps its target there,
    /// and this client sees only its replicated alert (BaseAI.IsAlerted) and whether it has a target at all
    /// (BaseAI.HaveTarget, from the ZDO), so any alerted creature with a target counts, and its owner checks the
    /// target (<see cref="VanishRpc"/>). A creature with no owner for the moment is skipped: an RPC to its owner would
    /// go to everybody.</item>
    /// </list>
    /// </summary>
    public static class VanishScan
    {
        /// <summary>Whether a hostile creature is within <paramref name="radius"/> metres of the player.</summary>
        public static bool EnemyWithin(Player player, float radius)
        {
            Vector3 position = player.transform.position;
            float reach = radius * radius;
            List<BaseAI> all = BaseAI.BaseAIInstances;
            for (int i = 0; i < all.Count; i++)
            {
                if (Hostile(all[i], player, position, reach) != null)
                    return true;
            }
            return false;
        }

        /// <summary>Sends <see cref="Keys.RpcVanish"/> to the owner of every hunter within <paramref name="radius"/> metres; returns how many.</summary>
        public static int TellHunters(Player player, float radius)
        {
            Vector3 position = player.transform.position;
            float reach = radius * radius;
            ZDOID id = player.GetZDOID();
            List<BaseAI> all = BaseAI.BaseAIInstances;
            int told = 0;
            for (int i = 0; i < all.Count; i++)
            {
                MonsterAI ai = Hostile(all[i], player, position, reach);
                if (ai == null || ai.m_character.IsBoss() || !Hunting(ai, player))
                    continue;
                ai.m_nview.InvokeRPC(Keys.RpcVanish, id);
                told++;
            }
            return told;
        }

        /// <summary>The creature's AI when it is a monster within the squared reach of the spot, alive and hostile to the player; else null.</summary>
        private static MonsterAI Hostile(BaseAI ai, Player player, Vector3 position, float reach)
        {
            MonsterAI monster = ai as MonsterAI;
            if (monster == null || (monster.transform.position - position).sqrMagnitude > reach)
                return null;
            Character character = monster.m_character;
            return character != null && !character.IsDead() && BaseAI.IsEnemy(player, character) ? monster : null;
        }

        /// <summary>Whether the creature hunts the player, as far as this client can tell.</summary>
        private static bool Hunting(MonsterAI ai, Player player)
        {
            ZNetView view = ai.m_nview;
            if (view == null || !view.IsValid())
                return false;
            if (view.IsOwner())
                return ReferenceEquals(ai.m_targetCreature, player);
            return view.GetZDO().HasOwner() && ai.IsAlerted() && ai.HaveTarget();
        }
    }
}
