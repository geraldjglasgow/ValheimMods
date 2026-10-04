using System;
using System.Collections.Generic;
using System.Linq;

namespace DevBridge.Tune
{
    /// <summary>An object a path starts from, and where it lives: the prefab, a creature, a player, a container, the world.</summary>
    internal struct TuneRoot
    {
        internal readonly object Value;
        internal readonly string Where;

        internal TuneRoot(object value, string where)
        {
            Value = value;
            Where = where;
        }
    }

    /// <summary>
    /// What one /tune call changes: a component of a prefab, or an item's shared data, with the prefab's own object(s),
    /// a scan for the live copies and the member path.
    /// </summary>
    internal sealed class TuneTarget
    {
        /// <summary>The item or component, whichever way it was named; with the path it identifies a change.</summary>
        internal string Identity;

        /// <summary>Troll / troll_punch, SwordIron, Troll / Character.</summary>
        internal string Label;

        /// <summary>What the code lines call the root: shared, character, monsterAI.</summary>
        internal string Variable;

        internal TunePath Path;

        /// <summary>The prefab's own objects, never empty: the component, or the item prefab's shared data.</summary>
        internal List<object> Templates;

        /// <summary>Scans the loaded world for the live copies; run again on revert to catch copies made since.</summary>
        internal Func<List<TuneRoot>> Live;

        internal string Key => Identity + Path.Text;

        internal string PathText => Variable + Path.Text;

        /// <summary>The prefab's objects first, then the live copies.</summary>
        internal IEnumerable<TuneRoot> Roots() => Templates.Select(template => new TuneRoot(template, "prefab")).Concat(Live());
    }
}
