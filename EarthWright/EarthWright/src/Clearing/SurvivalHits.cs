using EarthWright.Core;
using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Survival clearing with drops: the object is struck through its own damage RPC with a blow of the player's tool
    /// tier (axe for wood, pickaxe for stone) that is far larger than any object's health, so the game's own code destroys it and drops its wood and
    /// stone (a tree falls and leaves a log and a stump, which <see cref="SurvivalFollowUp"/> clears next). Rocks are
    /// struck once per hit area, since each area holds its own health. Pickables are picked, then removed
    /// (<see cref="OwnedPick"/>). Every RPC goes to the object's owner, who runs the game's damage code; the blow names
    /// this player as the attacker. Only an object nobody owns is claimed, so the RPC is not sent to everybody.
    /// </summary>
    public static class SurvivalHits
    {
        private const float Blow = 100000f;

        /// <summary>Strikes the object; returns how many hits went out.</summary>
        public static int Strike(ClearTarget target, Player player, ClearTools tools)
        {
            ZNetView view = target.View;
            GameObject go = view.gameObject;
            if (go.GetComponent<Pickable>() != null)
                return OwnedPick.PickThenRemove(view, true) ? 1 : 0;
            if (!view.HasOwner())
                view.ClaimOwnership();
            HitData hit = MakeHit(go, target.Kind, player, tools);
            MineRock5 big = go.GetComponent<MineRock5>();
            MineRock small = go.GetComponent<MineRock>();
            if (big != null)
                return StrikeAreas(view, "RPC_Damage", hit, big.m_hitAreas != null ? big.m_hitAreas.Count : 0);
            if (small != null)
                return StrikeAreas(view, "Hit", hit, small.m_hitAreas != null ? small.m_hitAreas.Length : 0);
            view.InvokeRPC("RPC_Damage", hit);
            return 1;
        }

        /// <summary>One blow per hit area; the rock destroys itself when the last area breaks. Without areas it is removed.</summary>
        private static int StrikeAreas(ZNetView view, string rpc, HitData hit, int areas)
        {
            if (areas == 0)
            {
                ClearExecutor.Remove(view);
                return 1;
            }
            int sent = 0;
            for (int i = 0; i < areas && view.IsValid(); i++, sent++)
                view.InvokeRPC(rpc, hit, i);
            return sent;
        }

        private static HitData MakeHit(GameObject go, ClearCategory kind, Player player, ClearTools tools)
        {
            HitData hit = new HitData();
            hit.m_toolTier = (short)Mathf.Clamp(tools.TierFor(kind), 0, short.MaxValue);
            hit.m_itemWorldLevel = (byte)Mathf.Clamp(tools.WorldLevelFor(kind), 0, 255);
            hit.m_point = go.transform.position + Vector3.up;
            hit.m_dir = Away(player, go.transform.position);
            hit.m_hitType = HitData.HitType.PlayerHit;
            hit.SetAttacker(player);
            SetDamage(hit);
            return hit;
        }

        /// <summary>
        /// Every physical kind of damage at once, so no object is immune to the blow (a stump broken like stone, a bush
        /// that ignores chopping). Which tool the player needs, and its tier, the planner has already checked.
        /// </summary>
        private static void SetDamage(HitData hit)
        {
            hit.m_damage.m_chop = Blow;
            hit.m_damage.m_pickaxe = Blow;
            hit.m_damage.m_slash = Blow;
            hit.m_damage.m_blunt = Blow;
            hit.m_damage.m_pierce = Blow;
        }

        /// <summary>The direction from the player to the object, level; trees fall away from the player.</summary>
        private static Vector3 Away(Player player, Vector3 target)
        {
            Vector3 dir = target - player.transform.position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.01f ? dir.normalized : player.transform.forward;
        }
    }
}
