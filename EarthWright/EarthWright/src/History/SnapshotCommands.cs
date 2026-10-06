using EarthWright.Core;
using UnityEngine;

namespace EarthWright.History
{
    /// <summary>
    /// The console subcommand <c>ew snapshot save &lt;name&gt; [radius] | restore &lt;name&gt; | list | delete &lt;name&gt;</c>:
    /// named copies of the ground around the player, kept for this session only.
    /// </summary>
    internal static class SnapshotCommands
    {
        private const string Usage = "Usage: ew snapshot save <name> [radius] | restore <name> | list | delete <name>";

        public static void Register()
        {
            Command.Add("snapshot", "snapshot save <name> [radius] | restore <name> | list | delete <name>   copies of the ground around you (this session)", Run);
        }

        private static void Run(Terminal.ConsoleEventArgs args)
        {
            string verb = args.Length > 2 ? args[2].ToLowerInvariant() : "";
            string name = args.Length > 3 ? args[3] : "";
            if (verb == "list")
                List(args);
            else if (verb == "save" && name.Length > 0)
                Save(args, name);
            else if (verb == "restore" && name.Length > 0)
                Restore(args, name);
            else if (verb == "delete" && name.Length > 0)
                args.Context.AddString(Snapshots.Delete(name) ? $"Snapshot {name} deleted." : Language.Format(HistoryWords.SnapshotMissing, name));
            else
                args.Context.AddString(Usage);
        }

        private static void Save(Terminal.ConsoleEventArgs args, string name)
        {
            Player player = Player.m_localPlayer;
            string refusal = SaveRefusal(player, name);
            if (refusal != null)
            {
                args.Context.AddString(refusal);
                return;
            }
            float radius = Mathf.Clamp(args.TryParameterFloat(4, HistorySettings.SnapshotRadius.Value), 1f, Snapshots.MaxRadius);
            Snapshot snapshot = Snapshots.Save(name, player.transform.position, radius, out bool partial);
            if (snapshot == null)
            {
                args.Context.AddString($"EarthWright keeps at most {Snapshots.MaxBytes / (1024 * 1024)} MB of snapshots; delete one first "
                    + "(ew snapshot delete <name>) or use a smaller radius.");
                return;
            }
            string saved = Language.Format(HistoryWords.SnapshotSaved, name, snapshot.Points.ToString());
            args.Context.AddString(saved + $" (radius {radius:0.#} m)");
            if (partial)
                args.Context.AddString(Language.Localize(HistoryWords.SnapshotPartial));
            Messages.Center(saved);
        }

        private static string SaveRefusal(Player player, string name)
        {
            if (player == null)
                return "EarthWright: snapshots are taken around your character; join a world first.";
            if (!Snapshots.Has(name) && Snapshots.Count >= Snapshots.Max)
                return $"EarthWright keeps at most {Snapshots.Max} snapshots; delete one first (ew snapshot delete <name>).";
            return null;
        }

        private static void Restore(Terminal.ConsoleEventArgs args, string name)
        {
            Snapshot snapshot = Snapshots.Find(name);
            if (snapshot == null)
            {
                args.Context.AddString(Language.Format(HistoryWords.SnapshotMissing, name));
                return;
            }
            string message = Outcome(Snapshots.Restore(snapshot), name);
            args.Context.AddString(Language.Localize(message));
            Messages.Center(message);
        }

        private static string Outcome(RestoreOutcome outcome, string name)
        {
            if (outcome.TooFar)
                return HistoryWords.TooFar;
            if (outcome.Refusal != null)
                return outcome.Refusal;
            if (outcome.Left.Count > 0)
                return HistoryWords.Partly;
            if (outcome.Sent == 0)
                return Language.Format(HistoryWords.SnapshotSame, name);
            return Language.Format(HistoryWords.SnapshotRestored, name);
        }

        private static void List(Terminal.ConsoleEventArgs args)
        {
            args.Context.AddString($"EarthWright snapshots: {Snapshots.Count} (at most {Snapshots.Max}, kept until you log out).");
            foreach (Snapshot s in Snapshots.All)
            {
                int minutes = Mathf.FloorToInt((Time.time - s.Taken) / 60f);
                args.Context.AddString($"  {s.Name}: around ({s.Center.x:0}, {s.Center.z:0}), radius {s.Radius:0.#} m, {s.Points} points, {minutes} min ago");
            }
        }
    }
}
