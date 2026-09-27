using UnityEngine;

namespace EarthWright.Clearing
{
    /// <summary>
    /// Survival clearing with drops: the object is struck through its own damage RPC with a blow of the player's tool
    /// tier (axe for wood, pickaxe for stone) that is far larger than any object's health, so the game's own code destroys it and drops its wood and
    /// stone (a tree falls and leaves a log and a stump, which <see cref="SurvivalFollowUp"/> clears next). Rocks are
    /// struck once per hit area, since each area holds its own health. Pickables are picked, then removed.
    /// The caller has claimed the object, so the RPC runs here, where the attacker (this player) is known.
    /// </summary>
    public static class SurvivalHits
    {
        private const float Blow = 100000f;

        public static void Strike(ClearTarget target, Player player, ClearTools tools)
        {
            ZNetView view = target.View;
            GameObject go = view.gameObject;
            if (go.GetComponent<Pickable>() != null)
            {
                Pick(view);
                return;
            }
            HitData hit = MakeHit(go, target.Kind, player, tools);
            MineRock5 big = go.GetComponent<MineRock5>();
            MineRock small = go.GetComponent<MineRock>();
            if (big != null)
                StrikeAreas(view, "RPC_Damage", hit, big.m_hitAreas != null ? big.m_hitAreas.Count : 0);
            else if (small != null)
                StrikeAreas(view, "Hit", hit, small.m_hitAreas != null ? small.m_hitAreas.Length : 0);
            else
                view.InvokeRPC("RPC_Damage", hit);
        }

        /// <summary>The pickable's own pick RPC drops its item and extras; the emptied object is then removed.</summary>
        private static void Pick(ZNetView view)
        {
            view.InvokeRPC("RPC_Pick", 0);
            ClearExecutor.Remove(view);
        }

        /// <summary>One blow per hit area; the rock destroys itself when the last area breaks. Without areas it is removed.</summary>
        private static void StrikeAreas(ZNetView view, string rpc, HitData hit, int areas)
        {
            if (areas == 0)
            {
                ClearExecutor.Remove(view);
                return;
            }
            for (int i = 0; i < areas && view.IsValid(); i++)
                view.InvokeRPC(rpc, hit, i);
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
