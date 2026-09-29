using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using DevBridge.Stage;
using UnityEngine;

namespace DevBridge.Routes
{
    /// <summary>/animate: drive a placed object's Animator by hand, since still copies have no creature code to do it.</summary>
    internal static class AnimateRoute
    {
        internal static void Register(Router router) => router.Add("/animate",
            "/animate?id=N          its animator: layers and the clips playing, parameters, the controller's clips (newest animated\n" +
            "                       placement without id); then, in this order:\n" +
            "       &swap=<game clip>:<bundle clip>,...   the bundle's clips in place of the controller's (reset=1 undoes)\n" +
            "       &set=forward_speed:2,sleeping:false&trigger=stagger&play=<state>&layer=&at=0..1&fade=<s>&speed=1",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            Placement placement = Placements.Pick(request, Animated, "an animator");
            Animator animator = Poses.Of(placement);
            var done = new Dictionary<string, object>();
            if (request.Flag("reset")) Reset(placement, animator);
            if (request.Has("swap")) done["swapped"] = Poses.Swap(placement, animator, request.Get("swap"));
            if (request.Has("set")) Poses.Set(animator, request.Get("set"));
            if (request.Has("trigger")) Poses.Trigger(animator, request.Get("trigger"));
            if (request.Has("play")) Poses.Play(animator, request.Get("play"), request.Int("layer", -1), request.Float("fade", 0f), request.Float("at", 0f));
            if (request.Has("speed")) animator.speed = request.Float("speed", 1f);
            Dictionary<string, object> info = Poses.Describe(animator);
            info["id"] = placement.Id;
            foreach (KeyValuePair<string, object> pair in done) info[pair.Key] = pair.Value;
            request.Json(info);
        }

        private static bool Animated(Placement placement) =>
            placement.Alive && placement.Root.GetComponentsInChildren<Animator>(true).Any(a => a.runtimeAnimatorController);

        private static void Reset(Placement placement, Animator animator)
        {
            Poses.Unswap(placement, keepSpec: false);
            animator.speed = 1f;
            animator.Rebind();
            animator.Update(0f);
        }
    }
}
