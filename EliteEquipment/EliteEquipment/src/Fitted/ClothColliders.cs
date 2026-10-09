using System.Collections.Generic;
using UnityEngine;

namespace EliteEquipment.Fitted
{
    /// <summary>
    /// The colliders a chest's cloth made on the player's bones (<see cref="ChestClothSim"/>), on the cloth's holder: they
    /// are destroyed with it, wherever the bones keep them.
    /// </summary>
    public sealed class ClothColliders : MonoBehaviour
    {
        public List<GameObject> Made { get; } = new List<GameObject>();

        private void OnDestroy()
        {
            foreach (GameObject made in Made)
            {
                if (made != null)
                    Destroy(made);
            }
        }
    }
}
