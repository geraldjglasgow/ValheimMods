using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// A human dies the player's death, as a creature: its death effects are the player's (copied with the rest of its
    /// Character values), the blood burst and the player's ragdoll, through the game's creature death (Character.OnDeath:
    /// no tombstone, the ragdoll holding the drops as every creature ragdoll does). Three things are changed:
    /// <list type="bullet">
    /// <item>The gamepad rumbles the player's hit and death carry for the person holding the controller
    /// (sfx_hit_vibration_only, sfx_death_vibration_only) are taken out of every effect list (<see cref="Quiet"/>).</item>
    /// <item>The game dresses a ragdoll's armour for any humanoid but its body model, skin, hair colour, hair and beard
    /// only for a Player; <see cref="Dress"/> gives the ragdoll the human's, from its ZDO, on the owner that made the
    /// ragdoll and so owns it: the ragdoll's ZDO carries the look to every peer.</item>
    /// <item>The player's body lies a minute (a player has a tombstone to come back to) and goes without a trace; a
    /// human's goes as the Draugr's does, after its 2 s with the game's corpse puff, the drops spilling out then.</item>
    /// </list>
    /// </summary>
    internal static class HumanDeath
    {
        private const string Rumble = "vibration", PlayerBody = "Player_ragdoll", Corpse = "Draugr_ragdoll";
        private const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

        /// <summary>At build: the rumble cues out of every effect list the human took from the player.</summary>
        public static void Quiet(Humanoid human)
        {
            foreach (FieldInfo field in EffectLists())
            {
                if (field.GetValue(human) is EffectList list)
                {
                    list.m_effectPrefabs = list.m_effectPrefabs.Where(effect => !IsRumble(effect)).ToArray();
                }
            }
        }

        /// <summary>At death, on the owner: the ragdoll takes the human's look and lies no longer than a Draugr's.</summary>
        public static void Dress(Humanoid human, Ragdoll ragdoll)
        {
            ZDO? look = human.m_nview.GetZDO();
            VisEquipment? body = ragdoll.GetComponent<VisEquipment>();
            if (look == null || body == null)
            {
                return;
            }
            body.SetModel(look.GetInt(ZDOVars.s_modelIndex));
            body.SetSkinColor(look.GetVec3(ZDOVars.s_skinColor, Vector3.one));
            body.SetHairColor(look.GetVec3(ZDOVars.s_hairColor, Vector3.one));
            body.SetHairItem(look.GetInt(ZDOVars.s_hairItem));
            body.SetBeardItem(look.GetInt(ZDOVars.s_beardItem));
            Hasten(ragdoll);
        }

        /// <summary>The player's ragdoll only: a definition that gave the human another death keeps that one's timing.</summary>
        private static void Hasten(Ragdoll ragdoll)
        {
            GameObject? draugr = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Corpse) : null;
            Ragdoll? pace = draugr != null ? draugr.GetComponent<Ragdoll>() : null;
            if (pace == null || Look.CorpseCopies.OriginalOf(Utils.GetPrefabName(ragdoll.gameObject)) != PlayerBody)
            {
                return;
            }
            ragdoll.m_removeEffect = pace.m_removeEffect;
            ragdoll.CancelInvoke(nameof(Ragdoll.DestroyNow));
            ragdoll.InvokeRepeating(nameof(Ragdoll.DestroyNow), pace.m_ttl, 1f);
        }

        private static IEnumerable<FieldInfo> EffectLists() =>
            typeof(Character).GetFields(Declared).Concat(typeof(Humanoid).GetFields(Declared))
                .Where(field => field.FieldType == typeof(EffectList));

        private static bool IsRumble(EffectList.EffectData effect) =>
            effect.m_prefab != null && effect.m_prefab.name.Contains(Rumble);
    }
}
