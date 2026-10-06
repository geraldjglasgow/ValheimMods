using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// Added to every loaded feeder by <see cref="Feeders"/>: when the feeder's object is destroyed (its zone unloaded,
    /// the piece taken down or broken), it takes the feeder out of the loaded list, so the list never grows with dead
    /// entries between searches.
    /// </summary>
    public class FeederLife : MonoBehaviour
    {
        public Container Feeder;

        private void OnDestroy() => Feeders.Forget(Feeder);
    }
}
