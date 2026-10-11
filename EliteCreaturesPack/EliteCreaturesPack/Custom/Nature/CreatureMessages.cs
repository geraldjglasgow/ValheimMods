using System;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// A custom creature's spawn and death messages (`progress:`), shown in the middle of the screen of every player
    /// within <see cref="Range"/> metres, by the game's own player message (<c>Player.MessageAllInRange</c>: the
    /// creature's owner sends each nearby player's owner a "Message" RPC, so nobody else is sent anything). Both are
    /// decided on the owner alone. The spawn message is told once in a creature's life: the owner marks the ZDO
    /// (<see cref="Told"/>) the first time it starts, and tells it only if the creature has just come into the world, so
    /// a creature loaded again, or one that was already in the world when the message was added, says nothing. The death
    /// message rides the game's death event, which only the owner raises, once. The texts are this component's own
    /// serialized fields, so every creature made from the prefab carries them; a <c>$word</c> is translated by each player.
    /// </summary>
    public sealed class CreatureMessages : MonoBehaviour
    {
        /// <summary>How far from the creature a player sees its messages: about the reach of a fight's sounds.</summary>
        public const float Range = 100f;

        /// <summary>A spawn is fresh this long after the game stamps the creature's spawn time (its BaseAI's first Awake).</summary>
        private const double FreshSeconds = 10;

        /// <summary>ZDO bool: the spawn message has been dealt with (told, or skipped as not fresh).</summary>
        public static readonly int Told = "ecp_spawn_told".GetStableHashCode();

        public string Spawn = "";
        public string Death = "";

        private void Awake() => SafeCall.Run("custom creature messages", static me => me.Listen(), this);

        private void Start() => SafeCall.Run("custom creature spawn message", static me => me.TellSpawn(), this);

        private void Listen()
        {
            Character character = GetComponent<Character>();
            if (character != null && Death.Length > 0)
            {
                character.m_onDeath += OnDeath;
            }
        }

        private void OnDeath() => SafeCall.Run("custom creature death message", static me => me.Tell(me.Death), this);

        private void TellSpawn()
        {
            ZNetView nview = GetComponent<ZNetView>();
            if (Spawn.Length == 0 || nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }
            ZDO zdo = nview.GetZDO();
            if (zdo.GetBool(Told))
            {
                return;
            }
            zdo.Set(Told, true);
            if (Fresh(zdo))
            {
                Tell(Spawn);
            }
        }

        private void Tell(string message) =>
            Player.MessageAllInRange(transform.position, Range, MessageHud.MessageType.Center, message);

        /// <summary>Whether the creature has just come into the world: the game stamps its spawn time once, as it is made.</summary>
        private static bool Fresh(ZDO zdo)
        {
            long spawned = zdo.GetLong(ZDOVars.s_spawnTime, 0L);
            return spawned == 0L || (ZNet.instance.GetTime() - new DateTime(spawned)).TotalSeconds < FreshSeconds;
        }
    }
}
