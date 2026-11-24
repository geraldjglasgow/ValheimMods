using System;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The grave Revive is aimed at, and whether it can be revived (<see cref="Revive"/>). A grave is the game's
    /// <c>TombStone</c>; its ZDO holds what the game wrote when its player died (<c>Player.CreateTombStone</c> calls
    /// <c>TombStone.Setup</c> with the profile's name and player id, <c>ZDOVars.s_ownerName</c> and <c>s_owner</c>, and
    /// <c>TombStone.Awake</c> stamps <c>s_timeOfDeath</c> with the network time's ticks; read 2026-11-03). The aim is a
    /// <see cref="Aim"/> sphere along the camera's view within <see cref="Reach"/> of the caster, any layer. A grave
    /// can be revived when its owner is another player, died within <see cref="Window"/> and is in the server's
    /// player list (by name: the list carries no player id).
    /// </summary>
    public static class ReviveGrave
    {
        public const float Reach = 30f;
        public static readonly TimeSpan Window = TimeSpan.FromMinutes(3);

        private const float Aim = 0.75f;

        /// <summary>The nearest grave along the aim, or null.</summary>
        public static TombStone Aimed(Player caster)
        {
            Transform eye = GameCamera.instance != null ? GameCamera.instance.transform : null;
            if (eye == null)
                return null;
            float length = Reach + Vector3.Distance(eye.position, caster.transform.position);
            TombStone best = null;
            float bestDistance = float.MaxValue;
            foreach (RaycastHit hit in Physics.SphereCastAll(eye.position, Aim, eye.forward, length, Physics.AllLayers, QueryTriggerInteraction.Collide))
            {
                TombStone grave = hit.collider != null ? hit.collider.GetComponentInParent<TombStone>() : null;
                if (grave != null && hit.distance < bestDistance)
                {
                    best = grave;
                    bestDistance = hit.distance;
                }
            }
            return best;
        }

        /// <summary>The word saying why this grave cannot be revived by this caster, or null when it can.</summary>
        public static string Refusal(TombStone grave, Player caster)
        {
            ZDO zdo = Zdo(grave);
            if (zdo == null)
                return "ee_spell_revive_no_grave";
            long owner = zdo.GetLong(ZDOVars.s_owner, 0L);
            if (owner == 0L || owner == caster.GetPlayerID())
                return "ee_spell_revive_own";
            if (!Recent(zdo))
                return "ee_spell_revive_too_late";
            return Online(zdo.GetString(ZDOVars.s_ownerName, "")) ? null : "ee_spell_revive_offline";
        }

        public static ZDO Zdo(TombStone grave)
        {
            ZNetView view = grave != null ? grave.GetComponent<ZNetView>() : null;
            return view != null && view.IsValid() ? view.GetZDO() : null;
        }

        private static bool Recent(ZDO zdo)
        {
            long died = zdo.GetLong(ZDOVars.s_timeOfDeath, 0L);
            return died > 0L && ZNet.instance != null && ZNet.instance.GetTime().Ticks - died <= Window.Ticks;
        }

        private static bool Online(string name)
        {
            if (ZNet.instance == null || string.IsNullOrEmpty(name))
                return false;
            foreach (ZNet.PlayerInfo info in ZNet.instance.GetPlayerList())
            {
                if (info.m_name == name)
                    return true;
            }
            return false;
        }
    }
}
