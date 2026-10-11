using System.Collections.Generic;
using BundlePrefabs;
using EliteCreaturesPack.Custom.Definitions;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Build
{
    /// <summary>
    /// What a step works with while it puts one definition on one creature.
    /// <para>
    /// <b>Chains.</b> A creature whose base is another custom creature is built as that one's base plus that one's
    /// definition plus its own: its shell is a copy of the root game or mod prefab (<see cref="Base"/>), and the steps run
    /// once for each definition of <see cref="Chain"/>, base-most first, <see cref="Creature"/> last. So in each pass,
    /// <see cref="Definition"/> is the definition to apply now and <see cref="Creature"/> the creature being built
    /// (<see cref="IsLeaf"/> when they are the same). A later pass overrides an earlier one: what a definition leaves
    /// unset keeps what the earlier passes put there, which is the base's value. Relative values (a size, a speed scale)
    /// therefore stack, as they would on a copy of the finished base. Per-creature registrations (a name, a ZDO key, an
    /// ECR registration) are made for <see cref="Creature"/>, never for <see cref="Definition"/>.
    /// </para>
    /// <para>
    /// <b>Other creatures.</b> Every custom creature's shell exists before any step runs, so a definition can name one
    /// that is defined later in the files, or one that names this one back (A spawns B on death, B spawns A):
    /// <see cref="Find"/> gives its shell, which is the very object registered as its prefab. Call <see cref="Needs"/>
    /// for each one this creature cannot do without, so it is left out too if that one fails.
    /// </para>
    /// </summary>
    public sealed class CreatureBuild
    {
        private readonly ShellRecord record;

        internal CreatureBuild(ShellRecord record, CreatureDefinition definition, PrefabLookup find)
        {
            this.record = record;
            Definition = definition;
            Find = find;
            Report = new BuildReport(record, definition);
        }

        /// <summary>The definition this pass applies (the creature's own, or one of its custom bases').</summary>
        public CreatureDefinition Definition { get; }

        /// <summary>The creature being built: its name is the prefab's name.</summary>
        public CreatureDefinition Creature => record.Chain.Creature;

        /// <summary>The creature's definitions, base-most first, <see cref="Creature"/> last.</summary>
        public IReadOnlyList<CreatureDefinition> Chain => record.Chain.Links;

        /// <summary>Whether this pass applies the creature's own definition (the last pass).</summary>
        public bool IsLeaf => ReferenceEquals(Definition, Creature);

        /// <summary>Whether the creature is a person on the player's body (its chain's root base is <c>Human</c>).</summary>
        public bool IsHuman => record.Chain.Human;

        /// <summary>The prefab being built: an inactive copy named <see cref="CreatureDefinition.Name"/>, registered with
        /// ZNetScene on every peer once every step has run. Change only this (and parts from <see cref="CopyPart"/>).</summary>
        public GameObject Shell => record.Shell;

        /// <summary>The game or mod prefab at the root of the chain, the shell's original (the player's prefab for a human).
        /// Read it, never change it. Null only for a human whose player prefab could not be found.</summary>
        public GameObject? Base => record.BasePrefab;

        /// <summary>Prefabs, items, effects, status effects and the other custom creatures' shells, by name.</summary>
        public PrefabLookup Find { get; }

        /// <summary>Warnings and failures, with the file, creature and line.</summary>
        public BuildReport Report { get; }

        /// <summary>This creature cannot do without the custom creature <paramref name="customName"/>: if that one is left
        /// out, so is this one.</summary>
        public void Needs(string customName) => record.Needs.Add(customName);

        /// <summary>
        /// An inactive copy of <paramref name="source"/> named <c>&lt;creature&gt;_&lt;suffix&gt;</c> (a number added when
        /// taken), kept with this creature: an attack item, a projectile, an effect. With <paramref name="networked"/> it is
        /// registered with ZNetScene beside the creature (anything spawned in the world: projectiles, spawned effects);
        /// without, it is only referenced (an attack item in the creature's hands). It is destroyed when the creature is
        /// left out or the creatures are built again for the next world. Anything a step registers elsewhere (ObjectDB) it
        /// registers again on every build.
        /// </summary>
        public GameObject CopyPart(GameObject source, string suffix, bool networked)
        {
            GameObject part = PrefabBench.Copy(source, PartName(suffix));
            record.Parts.Add(new KeyValuePair<GameObject, bool>(part, networked));
            record.PartOrigins[part.name] = OriginOf(source.name) ?? source.name;
            return part;
        }

        /// <summary>
        /// The prefab a part of this creature was copied from (a copy of one of its parts leads back to that part's
        /// original), or null when <paramref name="name"/> is not one of its parts: how a line that names an attack by its
        /// item (<c>troll_throw</c>, an attack's <c>from</c>) finds the copy the combat step made of it.
        /// </summary>
        public string? OriginOf(string name) => record.PartOrigins.TryGetValue(name, out string origin) ? origin : null;

        /// <summary>A name for a part of this creature that no prefab and no other part has: <c>ECP_X_attack</c>, <c>ECP_X_attack2</c>.</summary>
        public string PartName(string suffix)
        {
            string stem = Creature.Name + "_" + suffix;
            string name = stem;
            while (Find.Prefab(name) != null || Find.PartTaken(name))
            {
                record.PartCount++;
                name = stem + (record.PartCount + 1);
            }
            Find.TakePart(name);
            return name;
        }
    }
}
