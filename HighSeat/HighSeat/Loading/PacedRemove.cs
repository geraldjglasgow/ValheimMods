using System.Collections.Generic;
using UnityEngine;

namespace HighSeat
{
    /// <summary>
    /// The game's ZNetScene.RemoveObjects for <see cref="PlayerAreas"/>, with the destroying spread over passes. Each full
    /// pass finds, as the game does, every instance whose object is in neither list (each listed object earmarked with
    /// the frame); then every pass destroys up to its share of them (PerPassHosted on a hosted game, PerPassDedicated on a
    /// dedicated server), each the game's way: instance gone, a non-persistent object it owns destroyed with it. The game
    /// destroys them all in one frame: 50 players leaving a hosted game at once made one frame of 3.2 s
    /// (measured 2026-11-01). One destroyed meanwhile is skipped, and so is one the host's own area holds again since
    /// the full pass (its passes between full ones earmark it the same way, <see cref="Keep"/>).
    /// </summary>
    internal static class PacedRemove
    {
        private const int PerPassHosted = 120;
        private const int PerPassDedicated = 300;

        private static readonly List<ZNetView> Leaving = new List<ZNetView>();
        private static ZNetScene _scene;
        private static byte _mark;
        private static int _next;

        /// <summary>A full pass: every instance whose object is in neither list is to go.</summary>
        public static void Plan(ZNetScene scene, List<ZDO> near, List<ZDO> distant)
        {
            _scene = scene;
            _mark = (byte)(Time.frameCount & 0xFF);
            Keep(near, distant);
            Leaving.Clear();
            _next = 0;
            foreach (ZNetView view in scene.m_instances.Values)
            {
                if (view.GetZDO().TempRemoveEarmark != _mark)
                {
                    Leaving.Add(view);
                }
            }
        }

        /// <summary>Earmarks what the lists hold, so none of it goes.</summary>
        public static void Keep(List<ZDO> near, List<ZDO> distant)
        {
            foreach (ZDO zdo in near)
            {
                zdo.TempRemoveEarmark = _mark;
            }
            foreach (ZDO zdo in distant)
            {
                zdo.TempRemoveEarmark = _mark;
            }
        }

        /// <summary>Destroys this pass's share of those planned.</summary>
        public static void Step(ZNetScene scene)
        {
            if (scene != _scene)
            {
                Leaving.Clear();
                _next = 0;
                return;
            }
            int left = ZNet.instance.IsDedicated() ? PerPassDedicated : PerPassHosted;
            while (_next < Leaving.Count && left > 0)
            {
                ZNetView view = Leaving[_next++];
                if (view != null && view.GetZDO() != null && view.GetZDO().TempRemoveEarmark != _mark)
                {
                    Remove(scene, view);
                    left--;
                }
            }
            if (_next >= Leaving.Count)
            {
                Leaving.Clear();
                _next = 0;
            }
        }

        // As the game: the instance goes, and a non-persistent object it owns is destroyed.
        private static void Remove(ZNetScene scene, ZNetView view)
        {
            ZDO zdo = view.GetZDO();
            view.ResetZDO();
            Object.Destroy(view.gameObject);
            if (!zdo.Persistent && zdo.IsOwner())
            {
                ZDOMan.instance.DestroyZDO(zdo);
            }
            scene.m_instances.Remove(zdo);
        }
    }
}
