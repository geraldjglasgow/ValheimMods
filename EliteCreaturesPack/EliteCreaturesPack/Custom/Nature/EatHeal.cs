using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Nature
{
    /// <summary>
    /// `behaviour: eat heal`: each item a custom creature eats off the ground heals it by <see cref="Heal"/>. The game's
    /// eating heals nothing, it only feeds (and so tames) a tameable creature; this listens to the same moment
    /// (<c>MonsterAI.m_onConsumedItem</c>, raised where the AI runs: on the creature's owner) and heals through the game
    /// (<c>Character.Heal</c>, which writes the health into the ZDO and shows the green number). The amount is this
    /// component's own serialized field, so every creature made from the prefab carries it.
    /// </summary>
    public sealed class EatHeal : MonoBehaviour
    {
        public float Heal;

        private Character? character;

        private void Awake() => SafeCall.Run("custom creature eating", static me => me.Listen(), this);

        private void Listen()
        {
            character = GetComponent<Character>();
            MonsterAI ai = GetComponent<MonsterAI>();
            if (ai != null)
            {
                ai.m_onConsumedItem += OnAte;
            }
        }

        private void OnAte(ItemDrop item) => SafeCall.Run("custom creature eating heal", static me => me.HealUp(), this);

        private void HealUp()
        {
            if (character != null && Heal > 0f)
            {
                character.Heal(Heal);
            }
        }
    }
}
