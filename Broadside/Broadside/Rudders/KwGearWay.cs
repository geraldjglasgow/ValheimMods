using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A level the steering gear's line shaft runs along, hung under what is over it (<see cref="KwGearGrid"/> lays its way
    /// over one): under a deck (<see cref="KwGearLine"/>) or under a cabin's roof (<see cref="KwGearCeiling"/>). Kit axes of
    /// the rudder.
    /// </summary>
    public interface IKwGearWay
    {
        /// <summary>The shaft's height at a place; null where nothing it hangs from is over it.</summary>
        float? At(float x, float y);

        /// <summary>The top of what is over a point of the shaft, where a hanger is fastened; null with nothing near over it.</summary>
        float? Over(Vector3 p);

        /// <summary>Whether a straight stretch is clear all along.</summary>
        bool Run(Vector3 a, Vector3 b);

        /// <summary>Whether a bevel box fits round a corner.</summary>
        bool Corner(Vector3 p);
    }
}
