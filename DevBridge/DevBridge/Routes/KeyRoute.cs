using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DevBridge.Input;
using DevBridge.Server;
using DevBridge.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DevBridge.Routes
{
    /// <summary>/key and /type: keyboard input the game reads as real key presses, and text for input fields.</summary>
    internal static class KeyRoute
    {
        internal static void Register(Router router)
        {
            router.Add("/key",
                "/key?tap=E&seconds=0.1  press and release (several at once: tap=LeftCtrl+F3)\n" +
                "/key?hold=W,LeftShift&seconds=2   hold for a time, the reply comes after release\n" +
                "/key?down=W | up=W | up=all        press or release without a timer (release everything when done)",
                Keys);
            router.Add("/type",
                "/type?text=...&path=<input field>&submit=1\n" +
                "                       set the text of an input field (the selected one without path); submit fires its submit events",
                Type);
        }

        private static void Keys(BridgeRequest request)
        {
            if (request.Has("down")) VirtualInput.Press(KeyNames.Parse(request.Require("down")));
            else if (request.Get("up") == "all") VirtualInput.ReleaseAll();
            else if (request.Has("up")) VirtualInput.Release(KeyNames.Parse(request.Require("up")));
            else
            {
                string keys = request.Get("tap") ?? request.Get("hold") ?? throw new BridgeException("give tap=, hold=, down= or up=");
                float seconds = request.Float("seconds", request.Has("tap") ? 0.1f : 1f);
                Async.Start(request, PressFor(request, KeyNames.Parse(keys), seconds));
                return;
            }
            request.Json(Held());
        }

        private static IEnumerator PressFor(BridgeRequest request, Key[] keys, float seconds)
        {
            VirtualInput.Press(keys);
            yield return new WaitForSecondsRealtime(Mathf.Max(seconds, 0.05f));
            VirtualInput.Release(keys);
            yield return null;
            request.Json(Held());
        }

        private static Dictionary<string, object> Held() =>
            new Dictionary<string, object> { ["held"] = VirtualInput.HeldKeys.ToList() };

        private static void Type(BridgeRequest request)
        {
            GameObject target = request.Has("path") ? ScenePaths.Require(request.Get("path")) : Selected();
            string text = request.Get("text", "");
            TMP_InputField field = target.GetComponentInChildren<TMP_InputField>(true);
            if (field) SetText(field, text, request.Flag("submit"));
            else SetLegacy(target.GetComponentInChildren<UnityEngine.UI.InputField>(true), text, request.Flag("submit"));
            request.Json(new Dictionary<string, object> { ["field"] = ScenePaths.PathOf(target.transform), ["text"] = text });
        }

        private static GameObject Selected()
        {
            GameObject selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            return selected ? selected : throw new BridgeException("no input field is selected; give path=");
        }

        private static void SetText(TMP_InputField field, string text, bool submit)
        {
            field.text = text;
            if (!submit) return;
            field.onSubmit.Invoke(text);
            field.onEndEdit.Invoke(text);
        }

        private static void SetLegacy(UnityEngine.UI.InputField field, string text, bool submit)
        {
            if (!field) throw new BridgeException("no input field there");
            field.text = text;
            if (submit) field.onEndEdit.Invoke(text);
        }
    }
}
