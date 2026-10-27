using System.Collections.Generic;
using System.Diagnostics;

namespace Broadside
{
    /// <summary>
    /// The ship's owner deciding a sea edit (<see cref="KwSeaShip.EditRpc"/>): refused when this peer no longer owns the
    /// ship, the edit does not read, it was planned on an older revision, an entry is gone or not a sea kind's
    /// (<see cref="KwSeaApply.Why"/>) or a kind says no (<see cref="KwSeaKind.Check"/>); else the blueprint changed in place,
    /// the marks of the new wood dropped (<see cref="KwSeaMarks"/>), each kind's own ZDO state kept right
    /// (<see cref="KwSeaKind.Owner"/>), the blueprint and the next revision written, the asker answered with the cost
    /// (<see cref="KwSeaCost"/>), and the change redrawn here at once.
    /// </summary>
    public static class KwSeaOwner
    {
        public static void Edit(KwSeaShip ship, long sender, ZPackage pkg, int token)
        {
            KwSeaEdit? edit = KwSeaEdit.Read(pkg);
            string? why = Why(ship, edit);
            if (why != null)
            {
                Answer(ship, sender, token, false, why, new List<(string, int)>());
                return;
            }
            Stopwatch watch = Stopwatch.StartNew();
            KwBlueprint before = KwSeaApply.Copy(ship.Blueprint);
            KwSeaChanges changes = KwSeaApply.Apply(ship.Blueprint, edit!);
            ZDO zdo = ship.View.GetZDO();
            Marks(ship, changes, zdo);
            List<(string Item, int Amount)> cost = KwSeaCost.Of(ship, before, ship.Blueprint, changes);
            int revision = zdo.GetInt(KwSeaShip.RevHash) + 1;
            zdo.Set(KwShipBody.Key, KwShipData.Pack(ship.Blueprint));
            zdo.Set(KwSeaShip.RevHash, revision);
            double applied = watch.Elapsed.TotalMilliseconds;
            Answer(ship, sender, token, true, edit!.Done, cost);
            ship.Applied(revision, changes);
            Log.Info($"Broadside: a ship's edit ({changes}) applied and written by its owner in {applied:0.0} ms, revision {revision}");
        }

        // Why the edit is refused here; null when it goes ahead.
        private static string? Why(KwSeaShip ship, KwSeaEdit? edit)
        {
            if (!ship.IsOwner)
                return "The ship changed hands: try again";
            if (edit == null)
                return "That change does not read (is everyone on the same Broadside?)";
            if (edit.Revision != ship.View.GetZDO().GetInt(KwSeaShip.RevHash) || edit.Revision != ship.Revision)
                return "The ship changed meanwhile: try again";
            string? why = KwSeaApply.Why(ship.Blueprint, edit, KwSeaKinds.Owned);
            foreach (KwSeaKind kind in Kinds(ship, edit))
                why ??= kind.Check(ship, edit);
            return why;
        }

        // The kinds whose pieces the edit touches, before or after.
        private static List<KwSeaKind> Kinds(KwSeaShip ship, KwSeaEdit edit)
        {
            List<KwSeaKind> kinds = new List<KwSeaKind>();
            foreach (KwSeaPart part in edit.Parts)
            {
                KwBpPiece? old = part.Index >= 0 && part.Index < ship.Blueprint.Pieces.Count ? ship.Blueprint.Pieces[part.Index] : null;
                foreach (KwBpPiece? piece in new[] { old, part.Piece })
                {
                    KwSeaKind? kind = piece != null ? KwSeaKinds.Of(piece) : null;
                    if (kind != null && !kinds.Contains(kind))
                        kinds.Add(kind);
                }
            }
            return kinds;
        }

        // The new wood's marks dropped, then each kind's own state.
        private static void Marks(KwSeaShip ship, KwSeaChanges changes, ZDO zdo)
        {
            HashSet<int> wood = new HashSet<int>();
            foreach (KwSeaChange change in changes.All)
            {
                KwSeaKind? kind = change.Old != null ? KwSeaKinds.Of(change.Old) : null;
                if (!change.Added && (kind == null || kind.NewWood(change)))
                    wood.Add(change.Index);
            }
            KwSeaMarks.Drop(zdo, wood);
            foreach (KwSeaKind kind in KwSeaKinds.All)
            {
                KwSeaChanges mine = changes.Mine(kind);
                if (!mine.Empty)
                    KwShipBody.Step("Broadside sea owner " + kind.Name, () => kind.Owner(ship, mine, zdo));
            }
        }

        private static void Answer(KwSeaShip ship, long sender, int token, bool ok, string words, List<(string Item, int Amount)> cost)
        {
            ZPackage pkg = new ZPackage();
            KwSeaCost.Write(pkg, cost);
            ship.View.InvokeRPC(sender, KwSeaShip.EditedRpc, token, ok, words ?? "", pkg);
        }
    }
}
