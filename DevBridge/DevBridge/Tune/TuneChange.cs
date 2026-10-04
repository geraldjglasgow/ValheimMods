using System.Collections.Generic;
using System.Linq;

namespace DevBridge.Tune
{
    /// <summary>One object a change wrote, the path from it to the member, and its value before /tune first touched it.</summary>
    internal sealed class Touched
    {
        internal object Anchor;
        internal TunePath Rest;
        internal object Original;
        internal string Where;
    }

    /// <summary>
    /// One tuned member of one target: the prefab's value before the first write, and every object written since, each
    /// with its own original. Later /tune calls on the same target and path add to it.
    /// </summary>
    internal sealed class TuneChange
    {
        internal readonly int Number;
        internal readonly TuneTarget Target;
        internal readonly object Original;
        internal readonly Dictionary<object, Touched> Written = new Dictionary<object, Touched>(SameObject.Instance);

        internal TuneChange(int number, TuneTarget target, object original)
        {
            Number = number;
            Target = target;
            Original = original;
        }

        /// <summary>The prefab's value now, or null when its object is gone (a logout destroyed it).</summary>
        internal object Current
        {
            get
            {
                Touched prefab = Written.Values.FirstOrDefault(entry => entry.Where == "prefab");
                return prefab == null || !Alive(prefab.Anchor) ? null : prefab.Rest.Read(prefab.Anchor);
            }
        }

        /// <summary>
        /// Notes an object about to be written, the first time only. A copy that holds the prefab's value as it was before
        /// this call inherited it from the prefab (it spawned after an earlier change), so its original is the prefab's.
        /// </summary>
        internal void Remember(KeyValuePair<object, TunePath> anchor, object value, object prefabBefore, string where)
        {
            if (Written.ContainsKey(anchor.Key)) return;
            object original = Equals(value, prefabBefore) ? Original : value;
            Written[anchor.Key] = new Touched { Anchor = anchor.Key, Rest = anchor.Value, Original = original, Where = where };
        }

        /// <summary>Puts every original back, and the prefab's original on copies made since that inherited the change.</summary>
        internal int Revert()
        {
            object tuned = Current;
            int restored = 0;
            foreach (Touched entry in Written.Values.Where(entry => Alive(entry.Anchor)))
            {
                entry.Rest.Write(entry.Anchor, entry.Original);
                restored++;
            }
            return restored + Newcomers(tuned);
        }

        private int Newcomers(object tuned)
        {
            int count = 0;
            foreach (TuneRoot root in Rescan())
            {
                if (!Target.Path.TryAnchor(root.Value, out KeyValuePair<object, TunePath> anchor) || Written.ContainsKey(anchor.Key)) continue;
                if (!Equals(anchor.Value.Read(anchor.Key), tuned)) continue;
                anchor.Value.Write(anchor.Key, Original);
                count++;
            }
            return count;
        }

        private List<TuneRoot> Rescan()
        {
            try
            {
                return Target.Live();
            }
            catch (Server.BridgeException)
            {
                return new List<TuneRoot>(); // no world loaded: nothing live to catch up
            }
        }

        internal Dictionary<string, object> Row() => new Dictionary<string, object>
        {
            ["change"] = Number,
            ["target"] = Target.Label,
            ["path"] = Target.PathText,
            ["original"] = CodeText.Show(Original),
            ["current"] = CodeText.Show(Current),
            ["copies"] = Written.Count,
            ["written"] = Written.Values.GroupBy(entry => entry.Where).ToDictionary(group => group.Key, group => group.Count()),
        };

        internal string Code() =>
            $"{Target.Label}: {Target.PathText} = {CodeText.Literal(Current)}; // was {CodeText.Literal(Original)}";

        private static bool Alive(object anchor) => !(anchor is UnityEngine.Object unity) || unity;
    }
}
