using System;
using System.Collections.Generic;
using OpenKeep.Core;
using UnityEngine;

namespace OpenKeep.Blueprints
{
    /// <summary>
    /// Clears a site before its ground is shaped: what <see cref="SiteObjects"/> allows, where pieces will stand and on
    /// the slopes around them wherever the ground moves by more than <see cref="MoveToClear"/>, so nothing is left
    /// floating or buried. Objects under a ward the player has no access to stay. Each object is claimed and destroyed
    /// through the game's network scene (nothing drops).
    /// </summary>
    public static class SiteClearing
    {
        private const float MoveToClear = 0.3f;

        /// <summary>A blueprint's site: its pieces' rectangle and the ground it moves.</summary>
        public static int Clear(SitePlan plan)
        {
            Rect area = SiteArea.World(plan.Blueprint, plan.Frame, SiteProtection.Grow(plan.Work));
            return Clear(area, plan.Work, point => SiteProtection.InPieces(point, plan));
        }

        /// <summary>Clears and returns how many objects were taken away; warded ones are counted on screen.</summary>
        public static int Clear(Rect area, GroundWork work, Func<Vector3, bool> building)
        {
            int cleared = 0, warded = 0;
            foreach (ZNetView view in Candidates(area, work, building))
            {
                if (!PrivateArea.CheckAccess(view.transform.position, 0f, flash: false, wardCheck: false))
                {
                    warded++;
                    continue;
                }
                view.ClaimOwnership();
                ZNetScene.instance.Destroy(view.gameObject);
                cleared++;
            }
            if (cleared > 0 || warded > 0)
                Messages.TopLeft(BlueprintWords.Format(BlueprintWords.Cleared, cleared, warded));
            return cleared;
        }

        private static List<ZNetView> Candidates(Rect area, GroundWork work, Func<Vector3, bool> building)
        {
            List<ZNetView> found = new List<ZNetView>();
            if (ZNetScene.instance == null)
                return found;
            foreach (ZNetView view in ZNetScene.instance.m_instances.Values)
            {
                if (view == null || !view.IsValid())
                    continue;
                Vector3 p = view.transform.position;
                bool wanted = building(p) || work.MoveAt(p) > MoveToClear;
                if (wanted && area.Contains(new Vector2(p.x, p.z)) && !Character.InInterior(p) && SiteObjects.Clearable(view.gameObject))
                    found.Add(view);
            }
            return found;
        }
    }
}
