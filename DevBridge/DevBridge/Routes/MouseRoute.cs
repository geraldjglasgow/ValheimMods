using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Input;
using DevBridge.Server;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace DevBridge.Routes
{
    /// <summary>/mouse: the game's mouse device itself, for camera turns, drags and anything the game polls.</summary>
    internal static class MouseRoute
    {
        private const float MaxStep = 40f;

        internal static void Register(Router router) => router.Add("/mouse",
            "/mouse?x=&y=           move the pointer to screen pixel x,y from the top-left (UI hover follows)\n" +
            "/mouse?dx=&dy=         move relative, spread over frames (turns the camera in game; dy>0 looks up)\n" +
            "/mouse?click=left|right|middle[&x=&y=]   press and release, at x,y when given\n" +
            "/mouse?down=left | up=left | scroll=N",
            Handle);

        private static void Handle(BridgeRequest request)
        {
            if (request.Has("dx") || request.Has("dy")) { Async.Start(request, Turn(request)); return; }
            if (request.Has("click")) { Async.Start(request, Click(request)); return; }
            if (request.Has("x")) VirtualInput.MoveTo(Point(request));
            if (request.Has("down")) VirtualInput.SetButton(KeyNames.Button(request.Get("down")), true);
            if (request.Has("up")) VirtualInput.SetButton(KeyNames.Button(request.Get("up")), false);
            if (request.Has("scroll")) VirtualInput.Scroll(request.Float("scroll", 0f));
            request.Json(State());
        }

        private static IEnumerator Turn(BridgeRequest request)
        {
            var total = new Vector2(request.Float("dx", 0f), request.Float("dy", 0f));
            int steps = Mathf.Max(1, Mathf.CeilToInt(total.magnitude / MaxStep));
            for (int i = 0; i < steps; i++)
            {
                VirtualInput.Nudge(total / steps);
                yield return null;
            }
            VirtualInput.Nudge(Vector2.zero);
            request.Json(State());
        }

        private static IEnumerator Click(BridgeRequest request)
        {
            MouseButton button = KeyNames.Button(request.Get("click"));
            if (request.Has("x")) VirtualInput.MoveTo(Point(request));
            yield return new WaitForSecondsRealtime(0.05f);
            VirtualInput.SetButton(button, true);
            yield return new WaitForSecondsRealtime(0.05f);
            VirtualInput.SetButton(button, false);
            yield return null;
            request.Json(State());
        }

        private static Vector2 Point(BridgeRequest request) => Fmt.ScreenPoint(request.Float("x", 0f), request.Float("y", 0f));

        private static Dictionary<string, object> State() => new Dictionary<string, object>
        {
            ["pointer"] = new[] { Mathf.Round(VirtualInput.Pointer.x), Mathf.Round(Screen.height - VirtualInput.Pointer.y) },
            ["held"] = VirtualInput.HeldKeys.ToList(),
        };
    }
}
