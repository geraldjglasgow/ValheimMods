using PatchGuard;

namespace GrindstoneSkills
{
    /// <summary>
    /// Vanish on the creature's owner, a client or the dedicated server alike: <see cref="Keys.RpcVanish"/> (ZDOID of the
    /// player) arrives from the vanishing player's client (<see cref="VanishScan"/>), registered on a monster's view
    /// when the first one arrives for it (<see cref="LazyRpcs"/>), not on every one that wakes.
    /// <list type="bullet">
    /// <item>Only the owner simulates the creature (BaseAI.UpdateAI returns at once elsewhere), so only the owner acts,
    /// and only when the creature's target (MonsterAI.m_targetCreature) is that player: a creature hunting someone else,
    /// or nobody, is left alone. Bosses are left alone too.</item>
    /// <item>Losing track is what MonsterAI.UpdateTarget itself does when a creature gives up a hunt: no alert (written to
    /// the ZDO by BaseAI.SetAlerted, so every machine sees it), no target, and <see cref="RetargetDelay"/> seconds before
    /// it looks for one again. After that it finds the player again only by the game's rules (sight within its view
    /// range shrunk by the player's stealth, or hearing their noise). A raider sent to hunt players (BaseAI.HuntPlayer)
    /// is alerted again at once by the game and goes for the closest player once those seconds are over.</item>
    /// </list>
    /// Off, nothing is sent; an RPC arriving anyway is ignored.
    /// </summary>
    public static class VanishRpc
    {
        /// <summary>MonsterAI.UpdateTarget waits this long before looking for a new target after giving one up.</summary>
        private const float RetargetDelay = 5f;

        /// <summary>Registers the vanish RPC on a monster's view, when the first one arrives for it (<see cref="LazyRpcs"/>).</summary>
        public static void RegisterOn(ZNetView nview)
        {
            MonsterAI ai = nview.GetComponent<MonsterAI>();
            if (ai != null && !LazyRpcs.Has(nview, Keys.RpcVanish))
                nview.Register<ZDOID>(Keys.RpcVanish, (sender, player) => Guard.Run(Keys.RpcVanish, () => Receive(ai, player)));
        }

        private static void Receive(MonsterAI ai, ZDOID player)
        {
            if (!SneakSkill.On || ai == null || ai.m_nview == null || !ai.m_nview.IsValid() || !ai.m_nview.IsOwner())
                return;
            Character target = ai.m_targetCreature;
            if (target != null && target.GetZDOID() == player && !ai.m_character.IsBoss())
                LoseTrack(ai);
        }

        private static void LoseTrack(MonsterAI ai)
        {
            ai.SetAlerted(false);
            ai.m_targetCreature = null;
            ai.m_targetStatic = null;
            ai.m_timeSinceAttacking = 0f;
            ai.m_updateTargetTimer = RetargetDelay;
        }
    }
}
