using System.Collections.Generic;

namespace DevBridge.Events
{
    /// <summary>
    /// hit, death, spawn and boss events, built on the main thread from what the patches in CharacterPatches saw. A hit
    /// carries the numbers as the target's owner applied them: after block, resistances, armour and difficulty. Fire,
    /// poison and spirit are taken out of a hit by the game and come back as their own ticks (how=Burning, Poisoned).
    /// </summary>
    internal static class CharacterEvents
    {
        private static HitData arriving;
        private static float arrivingTotal;

        /// <summary>Remembers a hit's total as it reaches the target's owner, before anything reduces it.</summary>
        internal static void Arrive(Character target, HitData hit)
        {
            if (!target.IsOwner()) return;
            arriving = hit;
            arrivingTotal = hit.GetTotalDamage();
        }

        internal static void Hit(Character target, HitData hit)
        {
            Character attacker = hit.GetAttacker();
            Dictionary<string, object> data = CharacterFacts.About(target, "target");
            data["damage"] = Fmt.R(hit.GetTotalDamage());
            data["types"] = Types(hit.m_damage);
            if (ReferenceEquals(hit, arriving)) data["raw"] = Fmt.R(arrivingTotal);
            data["health"] = Fmt.R(target.GetHealth());
            data["max"] = Fmt.R(target.GetMaxHealth());
            data["how"] = hit.m_hitType.ToString();
            data["attacker"] = attacker ? CharacterFacts.Name(attacker) : "(none)";
            if (attacker)
            {
                data["attacker_prefab"] = CharacterFacts.Prefab(attacker);
                data["distance"] = CharacterFacts.Distance(attacker, target);
            }
            EventLog.AddPlain("hit", data);
        }

        private static Dictionary<string, object> Types(HitData.DamageTypes d)
        {
            var types = new Dictionary<string, object>();
            void Put(string name, float value)
            {
                if (value > 0f) types[name] = Fmt.R(value);
            }
            Put("damage", d.m_damage);
            Put("blunt", d.m_blunt);
            Put("slash", d.m_slash);
            Put("pierce", d.m_pierce);
            Put("chop", d.m_chop);
            Put("pickaxe", d.m_pickaxe);
            Put("fire", d.m_fire);
            Put("frost", d.m_frost);
            Put("lightning", d.m_lightning);
            Put("poison", d.m_poison);
            Put("spirit", d.m_spirit);
            return types;
        }

        /// <summary>A death on its owner, with the last hit's cause and attacker; a boss also gets a boss event.</summary>
        internal static void Death(Character character)
        {
            Dictionary<string, object> data = CharacterFacts.About(character);
            Cause(data, character.m_lastHit);
            data["position"] = CharacterFacts.Position(character);
            CharacterFacts.Flags(data, character);
            EventLog.AddPlain("death", data);
            if (character.IsBoss()) Boss("died", character);
        }

        /// <summary>How the last hit was dealt and who dealt it, when the game still knows.</summary>
        internal static void Cause(Dictionary<string, object> data, HitData last)
        {
            Character killer = last?.GetAttacker();
            data["how"] = last != null ? last.m_hitType.ToString() : "(unknown)";
            data["killer"] = killer ? CharacterFacts.Name(killer) : "(none)";
            if (killer) data["killer_prefab"] = CharacterFacts.Prefab(killer);
        }

        /// <summary>A character that appeared since the last Update, so a spawner has set its level; loaded means it was
        /// made for an existing ZDO.</summary>
        internal static void Spawn(Character character, bool loaded)
        {
            if (!character || character.m_nview.GetZDO() == null) return;
            Dictionary<string, object> data = CharacterFacts.About(character);
            data["origin"] = loaded ? "loaded" : "created";
            data["owner"] = character.IsOwner() ? "me" : "other";
            data["position"] = CharacterFacts.Position(character);
            CharacterFacts.Flags(data, character);
            EventLog.AddPlain("spawn", data);
            if (character.IsBoss()) Boss("appeared", character);
        }

        internal static void Boss(string what, Character boss)
        {
            var data = new Dictionary<string, object> { ["what"] = what };
            CharacterFacts.Into(data, boss);
            data["position"] = CharacterFacts.Position(boss);
            EventLog.AddPlain("boss", data);
        }
    }
}
