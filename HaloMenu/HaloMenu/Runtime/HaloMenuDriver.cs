using HaloMenu.Api;
using UnityEngine;

namespace HaloMenu.Runtime
{
    /// <summary>The one MonoBehaviour in the mod. Ticks every ring's input once a frame, open or closed, so a
    /// closed ring can still notice its hotkey going down.</summary>
    public sealed class HaloMenuDriver : MonoBehaviour
    {
        public HaloMenuServiceImpl Service;

        private void Update()
        {
            RingRuntime[] rings = Service.AllRings;
            float deltaTime = Time.unscaledDeltaTime;
            for (int i = 0; i < rings.Length; i++)
                rings[i].TickInput(deltaTime);
        }
    }
}
