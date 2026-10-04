using System.Collections.Generic;
using EliteCreaturesReborn.Runtime;
using EliteCreaturesReborn.Traits;
using UnityEngine;

namespace EliteCreaturesReborn.Commands
{
    /// <summary>
    /// <c>elite spawn &lt;prefab&gt; &lt;stars&gt; [mutation...]</c>: makes exactly that creature and bypasses EVERYTHING -
    /// the star distribution, the mutation chances, <c>max mutations</c> and the glyph limit - so a rule can be checked
    /// in ten seconds. A boss takes one aspect word instead of mutations (<c>elite spawn Bonemass 2 Twin</c>), and its
    /// twin or phantom copies arrive with it exactly as they would from the altar. Bountiful also takes the aspect words
    /// after it as its extras (<c>elite spawn gd_king 2 Bountiful Enraged Mending</c>), or rolls them from the boss's
    /// rotation as the altar would when none follow. It instantiates the prefab (the machine that runs this owns it), then hands the exact traits to
    /// its controller before the controller resolves, which writes them to the ZDO as a fresh roll. A typo suggests near
    /// matches rather than failing silently.
    /// </summary>
    public static class SpawnCommand
    {
        public static void Run(Terminal.ConsoleEventArgs args)
        {
            if (args.Length < 4)
            {
                EliteCommands.Reply(args, "usage: elite spawn <prefab> <stars> [mutation... | boss aspect | Bountiful [aspect...]]");
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
            CreatureTraits? traits = prefab.GetComponent<Character>().IsBoss()
                ? BossTraits(args, prefab.name, stars) : CreatureTraitsFrom(args, stars);
            if (traits != null)
            {
                Place(args, prefab, traits);
            }
        }

        // A boss takes one aspect word, `none` for the plain fight; leaving it out is the plain fight too. Only Bountiful
        // takes more: the words after it are its extras, and with none it rolls them from the boss's rotation.
        private static CreatureTraits? BossTraits(Terminal.ConsoleEventArgs args, string bossPrefab, int stars)
        {
            Aspect? aspect = args.Length > 4 ? AspectCatalog.FromName(args[4]) : Aspect.None;
            if (aspect == null)
            {
                EliteCommands.Reply(args, $"elite spawn: '{args[4]}' is not a boss aspect. Use none, "
                    + string.Join(", ", System.Array.ConvertAll(AspectCatalog.InOrder, AspectCatalog.Word)) + ".");
                return null;
            }
            CreatureTraits traits = new CreatureTraits(stars, aspect.Value);
            if (aspect == Aspect.Bountiful)
            {
                traits.ExtraAspects = args.Length > 5 ? NamedExtras(args) : AspectRoller.RollExtras(bossPrefab);
            }
            else if (args.Length > 5)
            {
                EliteCommands.Reply(args, "elite spawn: only Bountiful carries more than one aspect; the rest are ignored.");
            }
            return traits;
        }

        // Named extras ride beside Bountiful whatever the rules say; a word that cannot be one is warned and skipped.
        private static int NamedExtras(Terminal.ConsoleEventArgs args)
        {
            int mask = 0;
            for (int i = 5; i < args.Length; i++)
            {
                Aspect? extra = AspectCatalog.FromName(args[i]);
                string? refusal = Refusal(extra, mask);
                if (refusal != null)
                {
                    EliteCommands.Reply(args, $"elite spawn: '{args[i]}' skipped - {refusal}.");
                    continue;
                }
                mask |= 1 << (int)extra!.Value;
            }
            return mask;
        }

        // Why a named word cannot ride beside Bountiful; null when it can.
        private static string? Refusal(Aspect? extra, int mask) => extra switch
        {
            null => "not a boss aspect",
            Aspect.None or Aspect.Bountiful => "not an aspect Bountiful can carry",
            Aspect named when AspectRoller.BringsBodies(named) && AspectRoller.HoldsBodies(mask)
                => "a boss carries at most one of Twin, Tethered and Phantom",
            _ => null,
        };

        private static CreatureTraits CreatureTraitsFrom(Terminal.ConsoleEventArgs args, int stars)
        {
            CreatureTraits traits = new CreatureTraits(stars, 0);
            foreach (Mutation m in ParseMutations(args))
            {
                traits.Add(m);
            }
            return traits;
        }

        private static GameObject? Lookup(string name)
        {
            GameObject? prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            return prefab != null && prefab.GetComponent<Character>() != null ? prefab : null;
        }

        // Runs on the machine that will OWN the new creature (it instantiates it); its controller forces the traits.
        private static void Place(Terminal.ConsoleEventArgs args, GameObject prefab, CreatureTraits traits)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                EliteCommands.Reply(args, "elite spawn: needs a local player to place the creature.");
                return;
            }
            Vector3 pos = player.transform.position + player.transform.forward * 2.5f;
            GameObject go = Object.Instantiate(prefab, pos, Quaternion.identity);
            Force(args, go, traits, pos);
        }

        private static void Force(Terminal.ConsoleEventArgs args, GameObject go, CreatureTraits traits, Vector3 pos)
        {
            EliteController? controller = go.GetComponent<EliteController>();
            if (controller == null)
            {
                EliteCommands.Reply(args, "elite spawn: that prefab is not a creature this mod can mark.");
                return;
            }
            controller.ForceTraits(traits, Heightmap.FindBiome(pos));
            EliteCommands.Reply(args, $"elite spawn: {go.name} at {traits.Stars} star(s){Words(traits)}.");
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

        private static string Words(CreatureTraits traits)
        {
            List<string> words = new List<string>();
            foreach (Aspect aspect in traits.Aspects())
            {
                words.Add(AspectCatalog.Word(aspect));
            }
            foreach (Mutation m in traits.Active())
            {
                words.Add(MutationCatalog.Word(m));
            }
            return words.Count == 0 ? "" : " [" + string.Join(", ", words) + "]";
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
