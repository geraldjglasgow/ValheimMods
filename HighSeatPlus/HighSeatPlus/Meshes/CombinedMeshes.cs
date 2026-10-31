using System;
using UnityEngine;

namespace HighSeatPlus.Meshes
{
    /// <summary>
    /// Combined building meshes (PLAN.md design 9; setting "Combine building meshes", this player's own). A building
    /// that has settled is drawn by a few combined renderers per cell instead of one or more per piece: the pieces'
    /// own renderers are turned off with Renderer.forceRenderingOff and nothing else about them changes - colliders,
    /// ZDOs, components, LOD groups and the renderers' enabled flags stay as they are, so the hammer, repair, comfort,
    /// roof and support checks, wards and other mods see what they see without this mod. Anything that changes how a
    /// piece looks splits its group back in the same frame (<see cref="CellGroup"/>), and switching the setting off
    /// gives every piece back to the game at once. Only this PC's drawing changes; nothing is sent or saved.
    /// <para>Why combine at all when the game's piece materials already use GPU instancing: instancing merges only
    /// identical meshes with identical materials, while a base is dozens of kinds sharing a handful of materials (the
    /// wood of floors, walls, beams, poles and roof frames is one material), and every piece still costs Unity a
    /// renderer to cull, LOD-select, sort and feed for the camera and each shadow cascade. A combined group is one
    /// renderer per material and LOD level for a whole cell. Kept as the game has it: transparent and vegetation
    /// materials, and every piece that moves, animates, lights up, emits particles or switches its own parts.</para>
    /// <para>Runs on this component, added to ZNetScene's object when a world loads on a machine with graphics (never on
    /// a dedicated server). Each frame: the LOD bias and field of view are checked once a second
    /// (<see cref="GroupLod"/>), up to <see cref="InspectPerFrame"/> settled pieces are looked at, up to
    /// <see cref="GroupsPerFrame"/> waiting groups are settled with at most about <see cref="VerticesPerFrame"/>
    /// vertices combined, and <see cref="SweepPartsPerFrame"/> combined renderers are checked. A piece settles after
    /// <see cref="SettleSeconds"/> unchanged. Any exception switches the feature off for the session, every piece drawn
    /// by the game again, logged once.</para>
    /// </summary>
    internal sealed class CombinedMeshes : MonoBehaviour
    {
        public const float SettleSeconds = 5f;
        private const int InspectPerFrame = 16;
        private const int GroupsPerFrame = 8;
        private const int VerticesPerFrame = 24000;
        private const int SweepPartsPerFrame = 48;
        private const string ContainerName = "HighSeatPlus combined meshes";

        private static CombinedMeshes _instance;
        private static bool _failed;

        /// <summary>True while pieces are being combined; every patch does nothing otherwise.</summary>
        public static bool Active { get; private set; }

        /// <summary>The parent of every build, at the world origin.</summary>
        public static Transform Container { get; private set; }

        public static void Attach(GameObject host)
        {
            if (_instance == null && !_failed)
            {
                host.AddComponent<CombinedMeshes>();
            }
        }

        /// <summary>Called by any part of the feature that failed: everything goes back to the game for the session.</summary>
        public static void Fail(Exception error)
        {
            if (_failed)
            {
                return;
            }
            _failed = true;
            HighSeatPlus.Log.LogWarning("Combined building meshes stopped after an error; every piece is drawn by the game again. " + error);
            try
            {
                TurnOff();
            }
            catch (Exception second)
            {
                HighSeatPlus.Log.LogWarning("Could not hand every piece back: " + second.Message);
            }
        }

        private void Awake()
        {
            _instance = this;
        }

        private void Update()
        {
            if (_failed)
            {
                return;
            }
            try
            {
                Tick();
            }
            catch (Exception error)
            {
                Fail(error);
            }
        }

        private void Tick()
        {
            // A dedicated server started with graphics still draws nothing anybody sees.
            bool wanted = Settings.CombineBuildingMeshes.Value && (ZNet.instance == null || !ZNet.instance.IsDedicated());
            if (wanted != Active)
            {
                if (wanted)
                {
                    TurnOn();
                }
                else
                {
                    TurnOff();
                }
            }
            if (Active)
            {
                Work(Time.time);
            }
        }

        private static void Work(float now)
        {
            if (GroupLod.Refresh(now))
            {
                CellGroups.RefreshLods();
            }
            PieceRegistry.ProcessPending(now, InspectPerFrame);
            CellGroups.Process(now, GroupsPerFrame, VerticesPerFrame);
            CellGroups.Sweep(SweepPartsPerFrame);
        }

        private static void TurnOn()
        {
            if (Container == null)
            {
                Container = new GameObject(ContainerName).transform;
            }
            Active = true;
            PieceRegistry.AddAll();
        }

        private static void TurnOff()
        {
            Active = false;
            CellGroups.Clear();
            PieceRegistry.Clear();
        }

        private void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }
            _instance = null;
            try
            {
                TurnOff();
            }
            catch (Exception error)
            {
                HighSeatPlus.Log.LogWarning("Combined building meshes did not close cleanly: " + error.Message);
            }
            if (Container != null)
            {
                Destroy(Container.gameObject);
            }
            Container = null;
            GroupLod.Reset();
            RenderRules.Clear();
            LodLayouts.Clear();
        }
    }
}
