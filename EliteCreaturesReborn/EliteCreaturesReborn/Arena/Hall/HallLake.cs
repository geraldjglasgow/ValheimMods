using System.Collections.Generic;
using LocalEffects;
using UnityEngine;

namespace EliteCreaturesReborn.Arena.Hall
{
    /// <summary>
    /// Hel's river kills (features/arena.md section 1; the user, 2026-10-13: "if a player escapes the arena and touches
    /// the black lake, then they die instantly"). Nothing under the river's surface (y -8) is solid, so whoever goes over
    /// the wall and past the plateau's rim falls through the land and the liquid. Four times a second, on every peer,
    /// each loaded character in the band under the surface near the arena (<see cref="Under"/>) is taken once: its owner
    /// deals it lethal damage through the game's own damage path (so the death, its effects, the tombstone and other mods'
    /// hooks run as usual): the local player reads "Hel's river takes you." first; a creature drops nothing. Every client
    /// that sees it go under splashes there: the river's own bubbles and the game's big water impact. It is not a death
    /// in the arena (<see cref="ArenaHall.Contains"/> is false below y -5), so the trial's skill protection does not
    /// apply: the game's own skill loss is the price of escaping. The player's grave is moved to the arena's gate
    /// (<see cref="HallLakeGrave"/>). The band reaches 400 m down and 1,200 m out, over the far ocean where the arena
    /// stands: no land, so no dungeon interior (the game puts them 5,000 m over their entrances) is anywhere near it.
    /// </summary>
    internal sealed class HallLake : MonoBehaviour
    {
        public const float Surface = -8f;
        private const float Depth = 400f;
        private const float Reach = 1200f;
        private const float Every = 0.25f;
        private const float Lethal = 1e7f;
        private const string Splash = "fx_WaterImpact_Big";
        private const float SplashRadius = 4f;
        private const float BubbleLife = 4f;

        private readonly HashSet<Character> _taken = new HashSet<Character>();
        private float _next;
        private GameObject? _splash;
        private bool _splashTried;

        /// <summary>True for a world position in the band under Hel's river near the arena.</summary>
        public static bool Under(Vector3 position)
        {
            Vector3 local = position - ArenaHall.Origin;
            return local.y < Surface && local.y > Surface - Depth && local.x * local.x + local.z * local.z < Reach * Reach;
        }

        private void Update()
        {
            if (Time.time < _next)
            {
                return;
            }
            _next = Time.time + Every;
            List<Character> all = Character.GetAllCharacters();
            for (int i = 0; i < all.Count; i++)
            {
                Check(all[i]);
            }
            _taken.RemoveWhere(static c => c == null);
        }

        private void Check(Character character)
        {
            if (character == null)
            {
                return;
            }
            if (!Under(character.transform.position))
            {
                _taken.Remove(character);
                return;
            }
            if (character.IsDead() || !_taken.Add(character))
            {
                return;
            }
            SplashAt(character.transform.position);
            ZNetView? view = character.m_nview;
            if (view != null && view.IsValid() && view.IsOwner())
            {
                Take(character);
            }
        }

        // The owner's lethal hit; a remote player is taken on their own machine.
        private static void Take(Character character)
        {
            if (character is Player player)
            {
                if (player != Player.m_localPlayer)
                {
                    return;
                }
                player.Message(MessageHud.MessageType.Center, "$" + HallWords.River);
            }
            else if (character.TryGetComponent(out CharacterDrop drop))
            {
                drop.SetDropsEnabled(false);
            }
            var hit = new HitData { m_point = character.transform.position, m_hitType = HitData.HitType.Drowning };
            hit.m_blockable = hit.m_dodgeable = false;
            hit.m_damage.m_damage = Lethal;
            character.Damage(hit);
        }

        // Clients: the river's bubbles and the game's big water impact where it went under, at the surface.
        private void SplashAt(Vector3 position)
        {
            if (HallBuilder.NoScreen)
            {
                return;
            }
            Vector3 at = new Vector3(position.x, ArenaHall.Origin.y + Surface, position.z);
            GameObject? bubbles = LocalEffect.Attach(HallRiverFx.Bubble, transform, at, endless: true);
            if (bubbles != null)
            {
                Destroy(bubbles, BubbleLife);
            }
            LocalEffect.Flash(SplashPrefab(), at, SplashRadius);
        }

        private GameObject? SplashPrefab()
        {
            if (!_splashTried)
            {
                _splashTried = true;
                _splash = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Splash) : null;
            }
            return _splash;
        }
    }
}
