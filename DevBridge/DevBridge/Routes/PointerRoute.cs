using System.Collections.Generic;
using DevBridge.Input;
using DevBridge.Server;
using DevBridge.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;

namespace DevBridge.Routes
{
    /// <summary>
    /// /click and /hover: UI pointer events sent straight to an element, no real cursor needed. For events through the
    /// mouse device itself (drags, what the game polls) use /mouse.
    /// </summary>
    internal static class PointerRoute
    {
        private static GameObject hovered;

        internal static void Register(Router router)
        {
            router.Add("/click",
                "/click?path=<path>|x=&y=&button=left|right|middle\n" +
                "                       pointer down, up and click on a UI element, by path or at screen pixel x,y from the top-left",
                Click);
            router.Add("/hover",
                "/hover?path=<path>|x=&y=   pointer-enter on an element and its parents (tooltips); the last one gets pointer-exit",
                Hover);
        }

        private static void Click(BridgeRequest request)
        {
            GameObject target = Target(request, out Vector2 point);
            var data = Pointer(point);
            data.button = ToInputButton(KeyNames.Button(request.Get("button")));
            GameObject pressed = ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerDownHandler);
            GameObject clicker = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
            data.pointerPress = pressed ? pressed : clicker;
            data.rawPointerPress = target;
            ExecuteEvents.Execute(data.pointerPress ? data.pointerPress : target, data, ExecuteEvents.pointerUpHandler);
            if (clicker) ExecuteEvents.Execute(clicker, data, ExecuteEvents.pointerClickHandler);
            request.Json(new Dictionary<string, object>
            {
                ["target"] = ScenePaths.PathOf(target.transform),
                ["pressed"] = pressed ? ScenePaths.PathOf(pressed.transform) : null,
                ["clicked"] = clicker ? ScenePaths.PathOf(clicker.transform) : null,
            });
        }

        private static void Hover(BridgeRequest request)
        {
            GameObject target = Target(request, out Vector2 point);
            var data = Pointer(point);
            if (hovered && hovered != target) Broadcast(hovered, data, ExecuteEvents.pointerExitHandler);
            Broadcast(target, data, ExecuteEvents.pointerEnterHandler);
            hovered = target;
            request.Json(new Dictionary<string, object> { ["hovered"] = ScenePaths.PathOf(target.transform) });
        }

        private static void Broadcast<T>(GameObject from, PointerEventData data, ExecuteEvents.EventFunction<T> function)
            where T : IEventSystemHandler
        {
            for (Transform t = from.transform; t; t = t.parent) ExecuteEvents.Execute(t.gameObject, data, function);
        }

        private static GameObject Target(BridgeRequest request, out Vector2 point)
        {
            if (request.Has("path"))
            {
                GameObject found = ScenePaths.Require(request.Get("path"));
                point = UiDescribe.Centre(found);
                return found;
            }
            if (!request.Has("x") || !request.Has("y")) throw new BridgeException("give path= or x= and y=");
            point = Fmt.ScreenPoint(request.Float("x", 0f), request.Float("y", 0f));
            return Raycast(point);
        }

        private static GameObject Raycast(Vector2 point)
        {
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(Pointer(point), hits);
            if (hits.Count == 0) throw new BridgeException($"no UI element at {point.x:0},{Screen.height - point.y:0}");
            return hits[0].gameObject;
        }

        private static PointerEventData Pointer(Vector2 point)
        {
            EventSystem system = EventSystem.current;
            if (!system) throw new BridgeException("no EventSystem yet");
            return new PointerEventData(system) { position = point, pointerId = -1, clickCount = 1, eligibleForClick = true };
        }

        private static PointerEventData.InputButton ToInputButton(MouseButton button) =>
            button == MouseButton.Right ? PointerEventData.InputButton.Right
            : button == MouseButton.Middle ? PointerEventData.InputButton.Middle
            : PointerEventData.InputButton.Left;
    }
}
