using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DevBridge.Server;

namespace DevBridge.Tune
{
    /// <summary>
    /// A member path such as m_attack.m_attackRange or m_randomSets[0].m_items[1], read and written on any root object. A
    /// write into a struct (m_damages.m_blunt) is carried back up through every struct holding it to the first class
    /// object, which is the object the write really changes: its anchor.
    /// </summary>
    internal sealed class TunePath
    {
        private static readonly Regex Segment = new Regex(@"^(?<name>[A-Za-z_][A-Za-z0-9_]*)(\[(?<key>[^\]]+)\])*$");

        private readonly List<PathStep> steps;

        private TunePath(List<PathStep> steps)
        {
            this.steps = steps;
        }

        internal bool IsEmpty => steps.Count == 0;

        /// <summary>".m_attack.m_attackRange", with the names as the types spell them once the path has been walked.</summary>
        internal string Text => string.Concat(steps.Select(step => step.Text));

        internal static TunePath Parse(string text)
        {
            var steps = new List<PathStep>();
            text = (text ?? "").Trim();
            if (text.Length == 0) return new TunePath(steps);
            foreach (string part in text.Split('.'))
            {
                Match match = Segment.Match(part.Trim());
                if (!match.Success) throw new BridgeException($"'{part}' in {text} is not a member name with optional [index]");
                steps.Add(new PathStep(match.Groups["name"].Value, null));
                foreach (System.Text.RegularExpressions.Capture key in match.Groups["key"].Captures) steps.Add(new PathStep(null, Key(key.Value)));
            }
            return new TunePath(steps);
        }

        private static object Key(string text)
        {
            text = text.Trim();
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) ? index : (object)text.Trim('"', '\'');
        }

        internal object Read(object root)
        {
            object current = root;
            foreach (PathStep step in steps) current = step.Get(Live(current, step));
            return current;
        }

        /// <summary>The declared type of the member at the end of the path.</summary>
        internal Type Type(object root) => IsEmpty ? root.GetType() : steps[steps.Count - 1].Type(Holders(root)[steps.Count - 1]);

        /// <summary>Sets the last step, then puts each changed struct back into its holder, up to the first class object.</summary>
        internal void Write(object root, object value)
        {
            if (IsEmpty) throw new BridgeException("give the member to change in field=");
            object[] holders = Holders(root);
            for (int i = steps.Count - 1; i >= 0; i--)
            {
                steps[i].Set(holders[i], value);
                if (!holders[i].GetType().IsValueType) return;
                value = holders[i];
            }
            throw new BridgeException("the path starts at a struct, so a change has nowhere to go");
        }

        /// <summary>The deepest class object on the path (where a write ends) and the rest of the path from it.</summary>
        internal KeyValuePair<object, TunePath> Anchor(object root)
        {
            if (IsEmpty) return new KeyValuePair<object, TunePath>(root, this);
            object[] holders = Holders(root);
            int i = steps.Count - 1;
            while (i > 0 && holders[i].GetType().IsValueType) i--;
            return new KeyValuePair<object, TunePath>(holders[i], new TunePath(steps.GetRange(i, steps.Count - i)));
        }

        /// <summary>The anchor, or false for a copy the path cannot be walked on (a null or a shorter list on the way).</summary>
        internal bool TryAnchor(object root, out KeyValuePair<object, TunePath> anchor)
        {
            try
            {
                anchor = Anchor(root);
                return true;
            }
            catch (Exception)
            {
                // one odd copy is skipped rather than stopping the change halfway through the others
                anchor = default(KeyValuePair<object, TunePath>);
                return false;
            }
        }

        /// <summary>holders[i] is the object step i reads from: the root, then each value along the path.</summary>
        private object[] Holders(object root)
        {
            var holders = new object[steps.Count];
            object current = root;
            for (int i = 0; i < steps.Count; i++)
            {
                holders[i] = Live(current, steps[i]);
                if (i < steps.Count - 1) current = steps[i].Get(holders[i]);
            }
            return holders;
        }

        private static object Live(object holder, PathStep next)
        {
            if (holder == null || holder is UnityEngine.Object unity && !unity) throw new BridgeException($"null (or destroyed) before {next.Text}");
            return holder;
        }
    }
}
