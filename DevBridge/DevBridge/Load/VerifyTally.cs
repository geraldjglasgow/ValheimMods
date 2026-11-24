using System.Collections.Generic;

namespace DevBridge.Load
{
    /// <summary>The counts of one /load?verify=1 report (<see cref="ShadowVerify"/>), for one fake or all, and its reply.</summary>
    internal sealed class VerifyTally
    {
        private static readonly string[] Types = { "float", "vec3", "quat", "int", "long", "string", "bytes" };

        internal int Checked;
        internal int Ok;
        internal int Missing;
        internal int Revision;
        internal int Fields;
        internal int Unverifiable;
        internal int Awaiting;
        internal int ExtraKeys;
        internal int AgainstObject;
        internal int AgainstHistory;
        internal int Unreadable;
        internal int Fakes;
        internal readonly List<object> Samples = new List<object>();

        internal static string TypeName(int type) => type >= 0 && type < Types.Length ? Types[type] : "type " + type;

        internal void Count(string kind)
        {
            if (kind == "missing") Missing++;
            else if (kind == "revision") Revision++;
            else Fields++;
        }

        internal void Add(VerifyTally one, FakeShadow shadow)
        {
            Fakes++;
            Checked += one.Checked;
            Ok += one.Ok;
            Missing += one.Missing;
            Revision += one.Revision;
            Fields += one.Fields;
            Unverifiable += one.Unverifiable;
            Awaiting += one.Awaiting;
            ExtraKeys += one.ExtraKeys;
            AgainstObject += one.AgainstObject;
            AgainstHistory += one.AgainstHistory;
            Unreadable += shadow.Unreadable;
        }

        internal Dictionary<string, object> Row(FakePlayer fake)
        {
            FakeShadow shadow = fake.Socket.Shadow;
            Dictionary<string, object> row = Counts();
            row["name"] = fake.Name;
            row["deltas"] = fake.Deltas;
            row["server_hears_deltas"] = FakeDeltas.ServerListens(fake);
            row["received"] = new Dictionary<string, object>
            {
                ["game_format_packages"] = shadow.GamePackages, ["delta_format_packages"] = shadow.DeltaPackages,
                ["whole_records"] = shadow.WholeRecords, ["delta_records"] = shadow.DeltaRecords,
                ["deltas_not_fitting"] = shadow.BaseMismatches, ["asked_whole"] = shadow.AsksSent,
                ["unreadable_packages"] = shadow.Unreadable, ["last_error"] = shadow.LastError, ["objects_held"] = shadow.Objects.Count,
            };
            return row;
        }

        internal Dictionary<string, object> Reply(ServerRecords records, List<object> rows)
        {
            Dictionary<string, object> reply = Counts();
            reply["verifying_fakes"] = Fakes;
            reply["passed"] = Fakes > 0 && Missing == 0 && Revision == 0 && Fields == 0 && Unreadable == 0;
            reply["unreadable_packages"] = Unreadable;
            reply["highseat_history"] = records.Missing == null ? (object)$"readable, protocol version {records.Version}" : records.Missing;
            reply["samples"] = Samples;
            reply["fakes"] = rows;
            if (Fakes == 0) reply["note"] = "no fake verifies: add some with /load?players=N&verify=1 (deltas=1 for the delta format)";
            return reply;
        }

        private Dictionary<string, object> Counts() => new Dictionary<string, object>
        {
            ["checked"] = Checked, ["ok"] = Ok, ["missing"] = Missing, ["revision_mismatch"] = Revision, ["value_mismatch"] = Fields,
            ["unverifiable"] = Unverifiable, ["awaiting_whole_copy"] = Awaiting, ["extra_keys"] = ExtraKeys,
            ["against_object"] = AgainstObject, ["against_history"] = AgainstHistory,
        };
    }
}
