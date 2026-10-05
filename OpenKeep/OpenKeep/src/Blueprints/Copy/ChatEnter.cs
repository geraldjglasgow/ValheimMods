using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OpenKeep.Blueprints.Copy
{
    /// <summary>
    /// Enter, the game's chat key, for the blueprint tools: a tool claims it with a test ("Enter is pressed and mine this
    /// frame") or marks the frame in which it acted on it, and the chat line stays shut that frame
    /// (<see cref="ChatEnterPatch"/>). One Chat.Update prefix serves any number of tools, so a new tool adds a claim
    /// instead of a patch (the Site planner still has its own, <c>PlannerChatPatch</c>, and could move onto this).
    /// The test is asked as well as the mark because the tool's update may run before or after the chat's.
    /// </summary>
    public static class ChatEnter
    {
        private static readonly List<(string Name, Func<bool> Wants)> claims = new List<(string, Func<bool>)>();
        private static int takenFrame = -1;

        /// <summary>Enter (either key) went down this frame.</summary>
        public static bool Pressed => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

        /// <summary>Adds a tool's test: true when Enter went down this frame and the tool acts on it.</summary>
        public static void Claim(string name, Func<bool> wants) => claims.Add((name, wants));

        /// <summary>The tool acted on Enter this frame.</summary>
        public static void Take() => takenFrame = Time.frameCount;

        /// <summary>Enter belongs to a tool this frame: the chat must not open on it.</summary>
        public static bool Taken()
        {
            if (takenFrame == Time.frameCount)
                return true;
            foreach ((string name, Func<bool> wants) in claims)
            {
                if (BlueprintSafe.Call(name, wants, false))
                    return true;
            }
            return false;
        }
    }

    /// <summary>Chat.Update prefix: skipped for a frame in which a blueprint tool takes Enter, so the chat line does not open.</summary>
    [HarmonyPatch(typeof(Chat), nameof(Chat.Update))]
    public static class ChatEnterPatch
    {
        [HarmonyPrefix]
        public static bool Prefix() => !BlueprintSafe.Call("OpenKeep blueprint Enter", ChatEnter.Taken, false);
    }
}
