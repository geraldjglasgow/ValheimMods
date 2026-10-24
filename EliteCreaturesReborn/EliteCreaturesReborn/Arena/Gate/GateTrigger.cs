using BundlePrefabs;
using EliteCreaturesReborn.Arena.Travel;
using EliteCreaturesReborn.Config;
using EliteCreaturesReborn.Patches;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Gate
{
    /// <summary>
    /// Walking into Helgrind's opening (features/arena.md section 2), on the walker's own machine: on the stone portal's
    /// <c>TELEPORT</c> box (on <c>character_trigger</c>, moved to <c>portal_center</c> and sized to the gate's opening,
    /// <see cref="GateModel.Opening"/>), in place of the game's trigger.
    /// The local player is jumped into Hel's hall through <see cref="ArenaTravel.Enter"/>, keeping the gate's <c>exit</c>
    /// point (1 m up, as the game's portals put an arriving player, facing away from the gate) as where the way out brings
    /// them back, and carrying the trial the gate is set to (<see cref="GateChoice"/>). While Arena is off a centre message says the gate is closed (the hall itself is built on the way in,
    /// on demand); carrying
    /// what the game will not let through a portal (ores, metals and the like), the game's own message, as its portals
    /// do. A placement ghost (no ZDO) does nothing.
    /// </summary>
    internal sealed class GateTrigger : MonoBehaviour
    {
        private const float Rise = 1f;

        private Transform? _exit;
        private bool _looked;

        private void OnTriggerEnter(Collider other)
        {
            Player? player = other.GetComponent<Player>();
            if (player == null || player != Player.m_localPlayer)
            {
                return;
            }
            ZNetView? view = GetComponentInParent<ZNetView>();
            if (view != null && view.IsValid())
            {
                SafeCall.Run("Helgrind", static (gate, walker) => gate.Walk(walker), this, player);
            }
        }

        private void Walk(Player player)
        {
            if (player.IsTeleporting())
            {
                return;
            }
            if (!ArenaSettings.Enabled) // the arena is built on demand: the jump in builds it (ArenaTravel.Enter)
            {
                player.Message(MessageHud.MessageType.Center, "$" + GateWords.Closed);
                return;
            }
            if (!player.IsTeleportable(false))
            {
                player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return;
            }
            Transform root = transform.parent != null ? transform.parent : transform;
            Transform? exit = ExitPoint();
            Vector3 at = exit != null ? exit.position : root.TransformPoint(GateModel.ExitAsBuilt);
            Quaternion facing = exit != null ? exit.rotation : root.rotation;
            if (ArenaTravel.Enter(player, at + Vector3.up * Rise, facing))
            {
                GateChoice.Carry(player, GetComponentInParent<ZNetView>());
            }
        }

        // The model's exit point, found once; without the model the same place is worked out from the piece.
        private Transform? ExitPoint()
        {
            if (!_looked)
            {
                _looked = true;
                _exit = transform.parent != null ? GameMaterials.Find(transform.parent, GateModel.Exit) : null;
            }
            return _exit;
        }
    }
}
