using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Casters
{
    /// <summary>
    /// One Eir's Spring on the caster's machine (<see cref="Spring"/>): an object at the circle's centre that heals every
    /// friend inside once a second, <see cref="Ticks"/> times, then goes. Inside is within <see cref="Spring.Radius"/>
    /// across and <see cref="Height"/> up or down of the centre. It ends early when the caster leaves the game or the
    /// switch goes off.
    /// </summary>
    public sealed class SpringPool : MonoBehaviour
    {
        private const float Every = 1f;
        private const int Ticks = 8;
        private const float Height = 2.5f;

        private static readonly List<Character> near = new List<Character>();

        private Player caster;
        private float heal;
        private float next;
        private int ticks;

        public static void Open(Player caster, Vector3 center, float heal)
        {
            var go = new GameObject("EE_Spring");
            go.transform.position = center;
            SpringPool pool = go.AddComponent<SpringPool>();
            pool.caster = caster;
            pool.heal = heal;
            pool.next = Time.time + Every;
        }

        private void Update()
        {
            if (caster == null || !CastersSettings.On || ticks >= Ticks)
            {
                Destroy(gameObject);
                return;
            }
            if (Time.time < next)
                return;
            next += Every;
            ticks++;
            CasterSafe.Run("eir's spring", Tick);
        }

        private void Tick()
        {
            near.Clear();
            Character.GetCharactersInRange(transform.position, Spring.Radius + Height, near);
            foreach (Character character in near)
            {
                if (Inside(character))
                    character.Heal(heal, true);
            }
            near.Clear();
        }

        private bool Inside(Character character)
        {
            Vector3 offset = character.transform.position - transform.position;
            return FriendTarget.Friendly(character, caster) && Mathf.Abs(offset.y) <= Height
                && new Vector2(offset.x, offset.z).sqrMagnitude <= Spring.Radius * Spring.Radius;
        }
    }
}
