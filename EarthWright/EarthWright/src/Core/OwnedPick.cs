using System.Collections.Generic;
using UnityEngine;

namespace EarthWright.Core
{
    /// <summary>
    /// Picks a pickable and then removes it, without ever picking a copy another machine owns: the game drops a
    /// pickable's items on the machine that runs its pick RPC, and taking it over from an owner that has just picked it
    /// (its "picked" flag not here yet) would drop the items twice. An object this machine owns, or nobody owns
    /// (claimed first), is picked and removed at once. One owned elsewhere is picked by its owner (the RPC goes there)
    /// and removed here <see cref="RemoveDelay"/> seconds later, once its picked state has arrived; taking over an object
    /// only to destroy it changes nothing for anyone.
    /// </summary>
    public static class OwnedPick
    {
        private const float RemoveDelay = 1f;

        private struct Later
        {
            public ZNetView View;
            public float Due;
        }

        private static readonly List<Later> removals = new List<Later>();

        internal static void Initialize() => Ticker.OnUpdate("EarthWright pick removals", Tick);

        /// <summary>Picks (when <paramref name="pick"/>) and removes the object; false when it is already gone.</summary>
        public static bool PickThenRemove(ZNetView view, bool pick)
        {
            if (view == null || !view.IsValid())
                return false;
            if (pick && view.HasOwner() && !view.IsOwner())
            {
                view.InvokeRPC("RPC_Pick", 0);
                Queue(view);
                return true;
            }
            view.ClaimOwnership();
            if (pick)
                view.InvokeRPC("RPC_Pick", 0);
            Remove(view);
            return true;
        }

        /// <summary>Destroys an object for everyone (this machine takes it first; destroying needs the owner).</summary>
        public static void Remove(ZNetView view)
        {
            if (view == null || !view.IsValid() || ZNetScene.instance == null)
                return;
            view.ClaimOwnership();
            ZNetScene.instance.Destroy(view.gameObject);
        }

        private static void Queue(ZNetView view) => removals.Add(new Later { View = view, Due = Time.time + RemoveDelay });

        private static void Tick()
        {
            if (removals.Count == 0)
                return;
            for (int i = removals.Count - 1; i >= 0; i--)
            {
                if (Time.time < removals[i].Due)
                    continue;
                ZNetView view = removals[i].View;
                removals.RemoveAt(i);
                Remove(view);
            }
        }
    }
}
