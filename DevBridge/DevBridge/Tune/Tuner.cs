using System;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Eval;
using DevBridge.Events;

namespace DevBridge.Tune
{
    /// <summary>Reads a target's member on the prefab and its live copies, or writes it on all of them and records the change.</summary>
    internal static class Tuner
    {
        internal const string Note =
            "local to this machine: an attack is worked out on the attacker's owner, so a tuned creature fights differently only " +
            "where this machine owns it (single player, or the host for the creatures it owns); new spawns copy the prefab, so " +
            "they get it too; a value a mod writes at load comes back on restart, so paste code=1 into the mod";

        internal static Dictionary<string, object> Read(TuneTarget target)
        {
            object value = target.Path.Read(target.Templates[0]);
            Dictionary<string, object> reply = Head(target);
            reply["prefab"] = CodeText.Show(value);
            reply["live"] = LiveValues(target);
            TuneChange change = TuneBook.Find(target);
            if (change == null) return reply;
            reply["change"] = change.Number;
            reply["original"] = CodeText.Show(change.Original);
            return reply;
        }

        /// <summary>Writes value= on the prefab and on every distinct live object once, remembering each original.</summary>
        internal static Dictionary<string, object> Set(TuneTarget target, string text)
        {
            object template = target.Templates[0];
            object before = target.Path.Read(template);
            Type type = target.Path.Type(template);
            TuneValue.Next(text, before, type); // a value that does not fit fails here, before anything is written
            TuneChange change = TuneBook.Open(target, before);
            var written = new Dictionary<string, int>();
            try
            {
                WriteAll(target, change, text, type, before, written);
            }
            finally
            {
                TuneBook.Keep(change);
            }
            return Reply(target, change, before, written);
        }

        private static void WriteAll(TuneTarget target, TuneChange change, string text, Type type, object prefabBefore, Dictionary<string, int> written)
        {
            var done = new HashSet<object>(SameObject.Instance);
            foreach (TuneRoot root in target.Roots())
            {
                if (!target.Path.TryAnchor(root.Value, out KeyValuePair<object, TunePath> anchor) || !done.Add(anchor.Key)) continue;
                object before = anchor.Value.Read(anchor.Key);
                anchor.Value.Write(anchor.Key, TuneValue.Next(text, before, type));
                change.Remember(anchor, before, prefabBefore, root.Where);
                Count(written, root.Where);
            }
        }

        private static Dictionary<string, object> Reply(TuneTarget target, TuneChange change, object before, Dictionary<string, int> written)
        {
            Dictionary<string, object> reply = Head(target);
            reply["change"] = change.Number;
            reply["original"] = CodeText.Show(change.Original);
            reply["before"] = CodeText.Show(before);
            reply["after"] = CodeText.Show(target.Path.Read(target.Templates[0]));
            reply["copies"] = written.Values.Sum();
            reply["written"] = written;
            EventLog.Add("tune", new Dictionary<string, object>(reply) { ["action"] = "set" });
            reply["note"] = Note;
            return reply;
        }

        private static Dictionary<string, object> Head(TuneTarget target) => new Dictionary<string, object>
        {
            ["target"] = target.Label,
            ["path"] = target.PathText,
            ["type"] = Describe.TypeName(target.Path.Type(target.Templates[0])),
        };

        /// <summary>Each distinct value the live objects hold, with how many hold it and where.</summary>
        private static List<Dictionary<string, object>> LiveValues(TuneTarget target) =>
            LiveObjects(target).GroupBy(pair => Describe.Value(pair.Key)).Select(group => new Dictionary<string, object>
            {
                ["value"] = CodeText.Show(group.First().Key),
                ["copies"] = group.Count(),
                ["where"] = group.GroupBy(pair => pair.Value).ToDictionary(where => where.Key, where => where.Count()),
            }).ToList();

        /// <summary>The value and place of each distinct live object; objects shared with the prefab count as the prefab.</summary>
        private static List<KeyValuePair<object, string>> LiveObjects(TuneTarget target)
        {
            var seen = new HashSet<object>(target.Templates.Select(template => target.Path.Anchor(template).Key), SameObject.Instance);
            var found = new List<KeyValuePair<object, string>>();
            foreach (TuneRoot root in target.Live())
            {
                if (!target.Path.TryAnchor(root.Value, out KeyValuePair<object, TunePath> anchor) || !seen.Add(anchor.Key)) continue;
                found.Add(new KeyValuePair<object, string>(anchor.Value.Read(anchor.Key), root.Where));
            }
            return found;
        }

        private static void Count(Dictionary<string, int> counts, string key) => counts[key] = (counts.TryGetValue(key, out int count) ? count : 0) + 1;
    }
}
