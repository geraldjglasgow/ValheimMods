using UnityEngine;

namespace ShipConfig
{
    /// <summary>
    /// A ship's speed over the water, from how far it moved flat between two samples, so every player aboard reads the
    /// same whether their machine moves the ship or not. Eased a little, since a ship another machine moves arrives in
    /// steps. A jump faster than <see cref="MostMetresPerSecond"/> (a teleport, a ship that just loaded) reads 0.
    /// </summary>
    public sealed class Speedometer
    {
        private const float MostMetresPerSecond = 60f;
        private const float Ease = 0.5f;

        private Ship ship;
        private Vector3 lastPosition;
        private float lastTime;

        /// <summary>Metres per second at the last sample.</summary>
        public float Speed { get; private set; }

        /// <summary>A new reading; while game time stands still (paused) the last one stays.</summary>
        public void Sample(Ship current)
        {
            Vector3 position = current.transform.position;
            float seconds = Time.time - lastTime;
            if (current == ship && seconds <= 0f)
                return;
            bool measurable = current == ship;
            Vector3 moved = position - lastPosition;
            ship = current;
            lastPosition = position;
            lastTime = Time.time;
            float speed = measurable ? new Vector2(moved.x, moved.z).magnitude / seconds : 0f;
            Speed = speed > MostMetresPerSecond ? 0f : Mathf.Lerp(Speed, speed, measurable ? Ease : 1f);
        }

        /// <summary>The next sample starts over (the panel hid: the player left the ship).</summary>
        public void Reset()
        {
            ship = null;
            Speed = 0f;
        }
    }
}
