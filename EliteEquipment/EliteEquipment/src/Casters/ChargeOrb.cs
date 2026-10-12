using System.Collections.Generic;
using LocalEffects;
using UnityEngine;

using Object = UnityEngine.Object;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// The little white spell at a charging caster's wand tip (user, 2026-10-11: "a little white spell that shows at tip of
    /// wand after you pull the wand back, that should get bigger if you decide to hold to cast, once full size it should
    /// emit a larger glow around it"), drawn for every player near on this machine, from the state the caster shares
    /// (<see cref="ChargeState"/>): the workshop's <c>ee_fx_charge</c>, grown from its own size at a tap to
    /// <see cref="Largest"/> times it at full charge; at full charge <c>ee_fx_charge_full</c>'s larger glow joins it,
    /// with <c>ee_fx_charge_ready</c>'s flash once. Local copies only, gone when the bolt flies.
    /// </summary>
    public static class ChargeOrb
    {
        private const float Largest = 2.2f;

        private sealed class Drawn
        {
            public Transform Tip;
            public GameObject Orb;
            public GameObject Glow;
        }

        private static readonly Dictionary<Player, Drawn> drawn = new Dictionary<Player, Drawn>();
        private static readonly List<Player> gone = new List<Player>();

        public static void Tick()
        {
            foreach (Player player in Player.GetAllPlayers())
                Draw(player);
            if (drawn.Count > 0)
                Sweep();
        }

        /// <summary>Particles under <paramref name="effect"/> follow the scale of everything above them.</summary>
        public static void ScaleWithParent(GameObject effect)
        {
            foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = system.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
        }

        private static void Draw(Player player)
        {
            Transform tip = ChargeState.Read(player, out float charge) ? TipSpawn.Tip(player) : null;
            drawn.TryGetValue(player, out Drawn now);
            if (now != null && now.Tip != tip)
            {
                Remove(now);
                drawn.Remove(player);
                now = null;
            }
            if (tip == null)
                return;
            if (now == null)
                drawn[player] = now = Make(tip);
            Size(now, charge);
        }

        private static Drawn Make(Transform tip)
        {
            GameObject orb = LocalEffect.Attach(CasterFx.Prepared(CasterFx.Charge), tip, tip.position, endless: true);
            if (orb != null)
                ScaleWithParent(orb);
            return new Drawn { Tip = tip, Orb = orb };
        }

        private static void Size(Drawn now, float charge)
        {
            if (now.Orb != null)
                now.Orb.transform.localScale = Vector3.one * Mathf.Lerp(1f, Largest, charge) / Mathf.Max(0.001f, now.Tip.lossyScale.x);
            if (charge < 1f || now.Glow != null)
                return;
            now.Glow = LocalEffect.Attach(CasterFx.Prepared(CasterFx.ChargeFull), now.Tip, now.Tip.position, endless: true);
            CasterFx.Here(CasterFx.ChargeReady, now.Tip.position);
        }

        private static void Remove(Drawn now)
        {
            if (now.Orb != null)
                Object.Destroy(now.Orb);
            if (now.Glow != null)
                Object.Destroy(now.Glow);
        }

        /// <summary>Forgets players no longer here (logged out, out of range); their copies went with their wands.</summary>
        private static void Sweep()
        {
            gone.Clear();
            foreach (KeyValuePair<Player, Drawn> entry in drawn)
            {
                if (entry.Key == null || entry.Value.Tip == null)
                    gone.Add(entry.Key);
            }
            foreach (Player player in gone)
            {
                Remove(drawn[player]);
                drawn.Remove(player);
            }
        }
    }
}
