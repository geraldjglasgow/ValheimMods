using System;
using System.Collections.Generic;
using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Traits;

namespace EliteCreaturesReborn.Api
{
    /// <summary>
    /// The ability endpoints of <see cref="EliteCreaturesApi"/> (<c>features/api.md</c>): what the aspects that need
    /// something of the creature itself use - the attack items Portalbound sends through its portals, and what Summoner
    /// calls - handed to <see cref="Registrations"/>. Only an empty name is left out. What can be checked against the game
    /// (the creature's items, a summoned prefab) is checked once the game's prefab table has it and reported, but kept:
    /// another mod may finish its prefabs after this call, and both are checked again when used (a portal carries only a
    /// projectile attack the creature holds; a wave skips a name the game does not know, logged once). Each call replaces
    /// what the prefab had; an empty list clears it.
    /// </summary>
    internal static class ApiAbilities
    {
        public static string[] SetPortalAttacks(string prefab, string[]? attacks)
        {
            ApiProblems problems = new ApiProblems(nameof(EliteCreaturesApi.SetPortalAttacks), prefab);
            List<string> kept = new List<string>();
            foreach (string? attack in attacks ?? Array.Empty<string>())
            {
                if (string.IsNullOrEmpty(attack))
                {
                    problems.Add("an empty attack name, left out");
                }
                else if (!kept.Contains(attack!))
                {
                    kept.Add(attack!);
                }
            }
            Registrations.SetPortalAttacks(prefab, kept.ToArray());
            CheckAttacks(prefab, kept, problems);
            return problems.ToArray();
        }

        public static string[] SetSummons(string prefab, string[]? creatures, int[]? stars)
        {
            ApiProblems problems = new ApiProblems(nameof(EliteCreaturesApi.SetSummons), prefab);
            string[] names = creatures ?? Array.Empty<string>();
            int[] counts = stars ?? Array.Empty<int>();
            if (counts.Length != names.Length)
            {
                problems.Add($"{counts.Length} star counts for {names.Length} creatures; one without comes with the aspect's own stars");
            }
            List<SummonPick> picks = new List<SummonPick>();
            for (int i = 0; i < names.Length; i++)
            {
                if (string.IsNullOrEmpty(names[i]))
                {
                    problems.Add("an empty creature name, left out");
                    continue;
                }
                picks.Add(new SummonPick(names[i], i < counts.Length ? counts[i] : SummonPick.OwnStars));
                CheckCreature(names[i], problems);
            }
            Registrations.SetSummons(prefab, picks.ToArray());
            return problems.ToArray();
        }

        // Once the game has the creature: each attack an item it can be given, thrown as a projectile.
        private static void CheckAttacks(string prefab, List<string> attacks, ApiProblems problems)
        {
            Character? body = ApiProblems.Creature(prefab);
            if (body == null || attacks.Count == 0)
            {
                return;
            }
            if (!(body is Humanoid humanoid))
            {
                problems.Add("its creature holds no attack items, so Portalbound never fires on it");
                return;
            }
            foreach (string attack in attacks)
            {
                Attack.AttackType? type = CreatureItems.AttackTypeOf(humanoid, attack);
                if (type == null)
                {
                    problems.Add($"'{attack}' is not an item its creature can be given (kept, checked again as it attacks)");
                }
                else if (type != Attack.AttackType.Projectile)
                {
                    problems.Add($"'{attack}' is not a projectile attack: only a throw or a shot goes through a portal");
                }
            }
        }

        private static void CheckCreature(string name, ApiProblems problems)
        {
            if (ZNetScene.instance != null && ApiProblems.Creature(name) == null)
            {
                problems.Add($"'{name}' is not a creature the game knows yet (kept: a wave skips it while it is not)");
            }
        }
    }
}
