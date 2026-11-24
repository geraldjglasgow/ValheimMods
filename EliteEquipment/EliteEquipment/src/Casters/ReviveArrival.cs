using Translations;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// A Revive on the revived player's own machine (<see cref="Revive"/>): waits, up to <see cref="Wait"/>, for the
    /// player to be in the world and alive (still on the death screen, or just respawned: the game refuses a teleport in
    /// the first 2 s after a spawn, <c>Player.TeleportTo</c>'s <c>m_teleportCooldown</c>), then moves them to the grave
    /// with the game's own distant teleport (<c>Player.TeleportTo</c>: the area loads, then the floor is found) and, once
    /// they are there, heals them up to <see cref="Revive.HealthShare"/> of their most health (<c>Character.Heal</c>,
    /// never lowering it), says who called them back and shows the pillar on them to every player near. One revive per
    /// grave: a second call to the same grave is ignored, so two healers do not pull a player back twice.
    /// </summary>
    public sealed class ReviveArrival : MonoBehaviour
    {
        private const float Wait = 30f;
        private const float Lift = 0.5f;

        private static ReviveArrival current;
        private static ZDOID lastGrave = ZDOID.None;

        private Vector3 grave;
        private string caster;
        private float until;
        private bool moving;

        public static void Begin(Vector3 grave, ZDOID id, string caster)
        {
            if (id == lastGrave)
                return;
            lastGrave = id;
            if (current != null)
                Destroy(current.gameObject);
            current = new GameObject("EE_ReviveArrival").AddComponent<ReviveArrival>();
            current.grave = grave;
            current.caster = caster;
            current.until = Time.time + Wait;
        }

        private void Update()
        {
            Player player = Player.m_localPlayer;
            if (Time.time > until)
            {
                Destroy(gameObject);
                return;
            }
            if (player == null || player.IsDead() || (moving && player.IsTeleporting()))
                return;
            if (!moving)
            {
                moving = player.TeleportTo(grave + Vector3.up * Lift, player.transform.rotation, distantTeleport: true);
                until = moving ? Time.time + Wait : until;
                return;
            }
            Destroy(gameObject);
            CasterSafe.Run("revive arrival", () => Arrive(player));
        }

        private void Arrive(Player player)
        {
            float half = player.GetMaxHealth() * Revive.HealthShare;
            if (player.GetHealth() < half)
                player.Heal(half - player.GetHealth(), true);
            player.Message(MessageHud.MessageType.Center, Lang.Format("ee_spell_revive_back", caster));
            CasterFx.Send(HealReleases.Revive, player.transform.position, player.GetZDOID(), Spells.Revive);
        }
    }
}
