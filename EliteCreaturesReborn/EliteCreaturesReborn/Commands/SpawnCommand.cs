using System.Collections.Generic;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// <c>elite spawn &lt;prefab&gt; &lt;stars&gt; [mutation...]</c>: makes exactly that creature and bypasses EVERYTHING -
    /// the star distribution, the mutation chances, <c>max mutations</c> and the glyph limit - so a rule can be checked
    /// in ten seconds. It instantiates the prefab (the machine that runs this owns it), then hands the exact traits to
    /// its controller before the controller resolves, which writes them to the ZDO as a fresh roll. A typo suggests near
    /// matches rather than failing silently.
    /// </summary>
    public static class SpawnCommand
    {
        public static void Run(Terminal.ConsoleEventArgs args)
        {
            if (args.Length < 4)
            {
                EliteCommands.Reply(args, "usage: elite spawn <prefab> <stars> [mutation...]");
                return;
            }
            GameObject? prefab = Lookup(args[2]);
            if (prefab == null)
            {
                EliteCommands.Reply(args, Suggest(args[2]));
                return;
            }
            if (!int.TryParse(args[3], out int stars) || stars < 0)
            {
                EliteCommands.Reply(args, $"elite spawn: '{args[3]}' is not a star count (0 or more).");
                return;
            }
            Place(args, prefab, stars, ParseMutations(args));
        }

        private static GameObject? Lookup(string name)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            return prefab != null && prefab.GetComponent<Character>() != null ? prefab : null;
        }

        // Runs on the machine that will OWN the new creature (it instantiates it); its controller forces the traits.
        private static void Place(Terminal.ConsoleEventArgs args, GameObject prefab, int stars, List<Mutation> muts)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                EliteCommands.Reply(args, "elite spawn: needs a local player to place the creature.");
                return;
            }
            Vector3 pos = player.transform.position + player.transform.forward * 2.5f;
            GameObject go = Object.Instantiate(prefab, pos, Quaternion.identity);
            Force(args, go, stars, muts, pos);
        }

        private static void Force(Terminal.ConsoleEventArgs args, GameObject go, int stars, List<Mutation> muts, Vector3 pos)
        {
            EliteController? controller = go.GetComponent<EliteController>();
            if (controller == null || go.GetComponent<Character>().IsBoss())
            {
                EliteCommands.Reply(args, "elite spawn: that prefab is a boss or unsupported creature and cannot mutate.");
                return;
            }
            CreatureTraits traits = new CreatureTraits(stars, 0);
            foreach (Mutation m in muts)
            {
                traits.Add(m);
            }
            controller.ForceTraits(traits, Heightmap.FindBiome(pos));
            EliteCommands.Reply(args, $"elite spawn: {go.name} at {stars} star(s){Words(muts)}.");
        }

        // Named mutations are applied whether or not the rules would ever have given them; an unknown word is warned and
        // skipped rather than aborting the spawn.
        private static List<Mutation> ParseMutations(Terminal.ConsoleEventArgs args)
        {
            List<Mutation> muts = new List<Mutation>();
            for (int i = 4; i < args.Length; i++)
            {
                Mutation? m = MutationCatalog.FromName(args[i]);
                if (m.HasValue)
                {
                    muts.Add(m.Value);
                }
                else
                {
                    EliteCommands.Reply(args, $"elite spawn: unknown mutation '{args[i]}', skipped.");
                }
            }
            return muts;
        }

        private static string Words(List<Mutation> muts)
        {
            if (muts.Count == 0)
            {
                return "";
            }
            List<string> words = new List<string>();
            foreach (Mutation m in muts)
            {
                words.Add(MutationCatalog.Word(m));
            }
            return " [" + string.Join(", ", words) + "]";
        }

        private static string Suggest(string typed)
        {
            List<string> near = new List<string>();
            string lower = typed.ToLowerInvariant();
            foreach (GameObject p in ZNetScene.instance.m_prefabs)
            {
                if (p != null && p.name.ToLowerInvariant().Contains(lower) && p.GetComponent<Character>() != null)
                {
                    near.Add(p.name);
                    if (near.Count >= 8) { break; }
                }
            }
            return near.Count > 0
                ? $"elite spawn: no creature prefab '{typed}'. Did you mean: {string.Join(", ", near)}?"
                : $"elite spawn: no creature prefab '{typed}'.";
        }
    }
}
