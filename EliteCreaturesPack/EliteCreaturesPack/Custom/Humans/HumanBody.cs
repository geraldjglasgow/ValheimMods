using System;
using EliteCreaturesPack.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// The human base of custom creatures (features/custom-creatures.md, section 4): a hostile person on the player's
    /// body, wearing and fighting with the game's own weapons, armour and shields through the player's animations, bows
    /// and crossbows included, showing hair, beard and skin and hair colours, which the game draws only for players.
    /// <see cref="Build"/> makes the creature prefab, <see cref="Dress"/> sets the ranges its look is rolled within (both
    /// called by the custom creature build: Custom/Build ShellMaker, then <see cref="HumanStep"/>). The pieces:
    /// <see cref="HumanShell"/> (the player's body as a creature), <see cref="HumanAi"/> (its mind),
    /// <see cref="HumanFists"/> and <see cref="HumanWeaponAi"/> (weapons the AI can use), <see cref="HumanRanged"/> and
    /// <see cref="HumanAmmo"/> (bows and crossbows), <see cref="HumanDeath"/>, <see cref="HumanAppearance"/>.
    /// </summary>
    public static class HumanBody
    {
        /// <summary>The definition's <c>base:</c> value meaning the player's body.</summary>
        public const string Base = "Human";

        /// <summary>
        /// An inactive copy of the player's body (<c>PrefabBench.Copy</c>: no Awake, no ZDO) made into a hostile humanoid
        /// named `name`, with the human base's look ranges (anything the game draws on players); or null with the reason
        /// logged. The custom creature build calls it for each human of a world (Custom/Build ShellMaker), on the server
        /// and on every client alike, once the world's scene is up and the last world's custom prefabs are cleared; the
        /// build adds the save mark, runs the steps and registers the prefab. Everything made here hangs on the shell (the
        /// fists' holder is a child of it) and is destroyed with it; nothing is registered here and nothing is kept, so
        /// the next world's build makes it all anew.
        /// </summary>
        public static GameObject? Build(string name)
        {
            ZNetScene? scene = ZNetScene.instance;
            string? refusal = Refusal(scene, name);
            if (refusal != null)
            {
                Log.Error($"Human '{name}' not built: {refusal}.");
                return null;
            }
            GameObject? shell = null;
            try
            {
                shell = HumanShell.Copy(scene!, name);
                Fit(shell, scene!);
                return shell;
            }
            catch (Exception e)
            {
                Discard(shell, name, e);
                return null;
            }
        }

        /// <summary>
        /// Lays a definition's look ranges on the prefab (what rolls the look on its owner as each human first spawns,
        /// kept in its ZDO): the values the look sets replace the earlier ones, the ones it leaves out stay. Called for
        /// each definition of a chain in order, so the creature's own definition has the last word.
        /// </summary>
        public static void Dress(GameObject prefab, HumanLook look)
        {
            if (prefab == null || look == null)
            {
                Log.Error("Human look not set: no prefab or no look given.");
                return;
            }
            HumanAppearance appearance = prefab.GetComponent<HumanAppearance>();
            if (appearance == null)
            {
                appearance = prefab.AddComponent<HumanAppearance>();
            }
            appearance.Take(look);
        }

        /// <summary>
        /// Makes what a human carries usable: its weapons' AI values fitted (<see cref="HumanWeaponAi"/>, only those no
        /// AI was ever set up for) and a full stack of every ammunition its weapons take (<see cref="HumanAmmo"/>). Runs
        /// by itself as each human is armed (Humanoid.Start, on every peer); code that gives a human items after that
        /// calls it again, which is safe: a fitted weapon is left alone and the ammunition only topped up.
        /// </summary>
        public static void Arm(Humanoid human)
        {
            HumanWeaponAi.FitAll(human);
            HumanAmmo.Stock(human);
        }

        private static string? Refusal(ZNetScene? scene, string name)
        {
            if (scene == null)
            {
                return "the game's network scene is not up";
            }
            if (string.IsNullOrEmpty(name))
            {
                return "it has no name";
            }
            if (scene.GetPrefab(name) != null)
            {
                return "a prefab of that name already exists";
            }
            GameObject? player = scene.GetPrefab(HumanShell.PlayerPrefab);
            return player == null || player.GetComponent<Player>() == null ? "the game has no Player prefab" : null;
        }

        /// <summary>A build that failed part way leaves nothing behind on the bench.</summary>
        private static void Discard(GameObject? shell, string name, Exception e)
        {
            if (shell != null)
            {
                Object.DestroyImmediate(shell);
            }
            Log.Error($"Human '{name}' not built: {e}");
        }

        private static void Fit(GameObject shell, ZNetScene scene)
        {
            Humanoid human = HumanShell.Convert(shell, scene);
            HumanAi.Add(shell, scene);
            HumanFists.Give(shell, human);
            HumanDeath.Quiet(human);
            shell.AddComponent<HumanRanged>().enabled = false;
            shell.AddComponent<HumanAppearance>();
        }
    }
}
