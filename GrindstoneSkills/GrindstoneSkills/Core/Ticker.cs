using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// One update for the features that would otherwise give every object of theirs a timer of its own (every loaded
    /// fish, every compost bin): each frame it hands control to each feature's static tick, which tests its own clock
    /// first. A plain object in the game scene, made by the first object that needs it, on any machine (a dedicated
    /// server too); when the game scene closes it goes with it, and the next one makes it again.
    /// </summary>
    internal sealed class Ticker : MonoBehaviour
    {
        private static Ticker instance;

        public static void Ensure()
        {
            if (instance == null)
                instance = new GameObject("grindstone_ticker").AddComponent<Ticker>();
        }

        private void Update()
        {
            FishMark.TickAll();
            CompostBin.TickAll();
        }
    }
}
