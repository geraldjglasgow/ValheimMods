using System;
using System.Collections.Generic;
using HarmonyLib;
using PatchGuard;
using UnityEngine;
using UnityEngine.Rendering;

namespace Broadside
{
    /// <summary>
    /// The waterline on the dock: while the local player designs (a Broadside tool or piece chosen in the hammer, within
    /// reach of a dry dock, <see cref="KwBpDock.Find"/>) or has the blueprint table's window open, the ship on that dock
    /// shows where the sea will stand on its hull once it is launched (<see cref="KwWaterlineLevel"/>, drawn by
    /// <see cref="KwWaterlineShape"/>): a light blue-green band round the outside of the hull, unlit so it reads by day and
    /// by night, and a faint sheet inside at the same height. Looked at about three times a second; the drawing is made
    /// again only when the pieces on the dock change (or the dock does). Local to each player's screen: nothing is sent or
    /// saved, and nothing is made on a dedicated server (the drawer comes with the local player's spawn,
    /// <see cref="KwDockWaterlineSpawn"/>, and goes with the world's scene).
    /// </summary>
    public sealed class KwDockWaterline : MonoBehaviour
    {
        private const float Every = 0.35f;
        private static readonly int SpanHash = KwGenerated.Key.GetStableHashCode();
        private static KwDockWaterline? instance;
        private static bool noShelf;
        private readonly List<Piece> ship = new List<Piece>();
        private MeshRenderer? shown;
        private Mesh? mesh;
        private int? drawn;
        private float next;
        private bool broken, any;

        /// <summary>The drawer, made once per world on a screen.</summary>
        public static void Ensure()
        {
            if (instance == null && KwShipBody.Drawn)
                instance = new GameObject("KW_dockwaterline").AddComponent<KwDockWaterline>();
        }

        private void Awake()
        {
            mesh = new Mesh { name = "KW_dock_waterline", indexFormat = IndexFormat.UInt32 };
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            shown = gameObject.AddComponent<MeshRenderer>();
            (shown.shadowCastingMode, shown.receiveShadows, shown.enabled) = (ShadowCastingMode.Off, false, false);
            (shown.lightProbeUsage, shown.reflectionProbeUsage) = (LightProbeUsage.Off, ReflectionProbeUsage.Off);
            // The sprite shader the game ships: unlit, see-through, coloured by each point; else the UI's.
            Shader? shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader != null)
                shown.sharedMaterial = new Material(shader) { name = "KW_dock_waterline", renderQueue = 3001 };
            else
                broken = Fail("the game has no see-through shader");
        }

        private void OnDestroy()
        {
            if (mesh != null)
                Destroy(mesh);
            if (shown != null && shown.sharedMaterial != null)
                Destroy(shown.sharedMaterial);
        }

        private void Update()
        {
            if (broken || Time.time < next)
                return;
            next = Time.time + Every;
            try
            {
                Guard.Run("Broadside dock waterline", Tick);
            }
            catch (Exception)
            {
                // Guard has logged it under the mod's name; the line stays off rather than failing three times a second.
                broken = true;
                Hide();
            }
        }

        private void Tick()
        {
            KwBpDockInfo? dock = Wanted();
            if (dock == null || !FindShip(dock))
            {
                Hide();
                return;
            }
            int key = Key(dock);
            if (key != drawn)
                Rebuild(dock, key);
            shown!.enabled = any;
        }

        private void Hide()
        {
            if (shown != null)
                shown.enabled = false;
        }

        // The dock to draw on: the open table's, else the one the player designs at; null for none.
        private static KwBpDockInfo? Wanted()
        {
            Player? player = Player.m_localPlayer;
            if (player == null || player.IsDead())
                return null;
            Component? table = OpenTable();
            if (table != null)
                return KwBpDock.Find(table.transform.position);
            return Designing(player) ? KwBpDock.Find(player.transform.position) : null;
        }

        // A Broadside tool or piece chosen in the hammer (not a saved ship, which is built on the water).
        private static bool Designing(Player player)
        {
            Piece? piece = player.InPlaceMode() ? player.GetSelectedPiece() : null;
            string name = piece != null ? piece.name : "";
            return name.StartsWith("KW_", StringComparison.Ordinal) && !name.StartsWith(KwBlueprintStation.EntryPrefix, StringComparison.Ordinal);
        }

        // The table whose window is open, or null; never asked again once the window could not be made.
        private static Component? OpenTable()
        {
            if (noShelf)
                return null;
            try
            {
                BlueprintShelf.Shelf shelf = KwBpShelf.Shelf;
                return shelf.IsOpen ? shelf.Station : null;
            }
            catch (Exception e)
            {
                noShelf = Fail($"the table's window is not there ({e.Message})");
                return null;
            }
        }

        // The ship pieces on the dock (all of them: the rudder and the sails tell its bow); false with no hull part among them.
        private bool FindShip(KwBpDockInfo dock)
        {
            ship.Clear();
            ship.AddRange(KwBpDock.Ship(dock));
            return ship.Exists(piece => KwBpDock.NameOf(piece)?.StartsWith(KwToolPieces.PartPrefix, StringComparison.Ordinal) == true);
        }

        // What the drawing is made from: the dock's frame and which pieces stand on it (every one's ZDO id, in any order;
        // a part's span never changes once it is built).
        private int Key(KwBpDockInfo dock)
        {
            int key = dock.Frame.position.GetHashCode() * 31 + dock.Frame.rotation.GetHashCode();
            foreach (Piece piece in ship)
                key += piece.m_nview.GetZDO().m_uid.GetHashCode() * 16777619;
            return key * 31 + ship.Count;
        }

        private void Rebuild(KwBpDockInfo dock, int key)
        {
            drawn = key;
            KwBlueprint placed = KwBpStrays.Trim(Capture(dock.Frame));
            KwWaterlineDraw draw = KwWaterlineShape.Of(placed, KwWaterlineLevel.OnDock(placed));
            mesh!.Clear();
            any = !draw.Empty;
            if (!any)
                return;
            mesh.SetVertices(draw.Points);
            mesh.SetColors(draw.Colors);
            mesh.SetTriangles(draw.Triangles, 0);
            mesh.RecalculateBounds();
            transform.SetPositionAndRotation(dock.Frame.position, dock.Frame.rotation);
        }

        // The ship's pieces placed in the dock's frame with their spans (all the waterline reads), as a blueprint saved there
        // holds them, but not turned round (OnDock works out which way round it will be saved).
        private KwBlueprint Capture(Transform frame)
        {
            Quaternion back = Quaternion.Inverse(frame.rotation);
            KwBlueprint placed = new KwBlueprint("");
            foreach (Piece piece in ship)
            {
                string span = piece.m_nview.GetZDO().GetString(SpanHash);
                List<KwBpValue> data = span.Length > 0 ? new List<KwBpValue> { new KwBpValue(SpanHash, span) } : new List<KwBpValue>();
                Transform at = piece.transform;
                placed.Pieces.Add(new KwBpPiece(KwBpDock.NameOf(piece)!, back * (at.position - frame.position), back * at.rotation, data));
            }
            return placed;
        }

        private static bool Fail(string why)
        {
            Log.Warn($"Dock waterline off: {why}");
            return true;
        }
    }

    /// <summary>Player.OnSpawned postfix: the local player's spawn makes the dock waterline's drawer (on screens only).</summary>
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    public static class KwDockWaterlineSpawn
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            try
            {
                if (__instance == Player.m_localPlayer)
                    KwDockWaterline.Ensure();
            }
            catch (Exception e)
            {
                // Never out of the spawn: the player must still arrive.
                Log.Error($"Broadside dock waterline could not start: {e}");
            }
        }
    }
}
