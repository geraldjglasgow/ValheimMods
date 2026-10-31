using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;

namespace HighSeatPlus.Crowd
{
    /// <summary>
    /// A far player's cloth held still: capes and any other worn cloth stop simulating and are drawn on the body's bones
    /// as modelled, never hidden. The game's capes are MagicaCloth, built on every client by VisEquipment.SetupCloth. A
    /// disabled MagicaCloth leaves the simulation and gives its renderer back the original skinned mesh, the state
    /// MagicaCloth's own culling shows for a cape nobody sees; enabled again it starts over from the pose the body has
    /// then, so coming back in full shows no wrong pose. A cape's PlayerClothWindShelter, which hands its cloth new
    /// settings every frame, is stopped with it. Unity's own Cloth, which gear from other mods may still carry, is stopped
    /// the same way (the game turns the cloth on its armour stands off with distance too) and its built-up motion cleared
    /// when it starts again. Only what was running is stopped and exactly that is started again; a part destroyed
    /// meanwhile (gear taken off) is skipped. Gear put on while drawn simpler is stopped as it arrives
    /// (<see cref="GearChange"/>), by going over the player again: what is already stopped is passed by.
    /// </summary>
    internal static class StillCloth
    {
        private static readonly List<MagicaCloth> FoundMagica = new List<MagicaCloth>();
        private static readonly List<Cloth> FoundUnity = new List<Cloth>();

        public static void Stop(FarPlayer far)
        {
            if (far.Player == null)
            {
                return;
            }
            Prune(far.Cloths);
            Prune(far.Shelters);
            Prune(far.UnityCloths);
            far.Player.GetComponentsInChildren(true, FoundMagica);
            for (int i = 0; i < FoundMagica.Count; i++)
            {
                StopMagica(far, FoundMagica[i]);
            }
            FoundMagica.Clear();
            far.Player.GetComponentsInChildren(true, FoundUnity);
            for (int i = 0; i < FoundUnity.Count; i++)
            {
                StopUnity(far, FoundUnity[i]);
            }
            FoundUnity.Clear();
        }

        private static void StopMagica(FarPlayer far, MagicaCloth cloth)
        {
            if (!cloth.enabled)
            {
                return;
            }
            cloth.enabled = false;
            far.Cloths.Add(cloth);
            if (cloth.TryGetComponent(out PlayerClothWindShelter shelter) && shelter.enabled)
            {
                shelter.enabled = false;
                far.Shelters.Add(shelter);
            }
        }

        private static void StopUnity(FarPlayer far, Cloth cloth)
        {
            if (cloth.enabled)
            {
                cloth.enabled = false;
                far.UnityCloths.Add(cloth);
            }
        }

        public static void Start(FarPlayer far)
        {
            foreach (MagicaCloth cloth in far.Cloths)
            {
                if (cloth != null)
                {
                    cloth.enabled = true;
                }
            }
            foreach (PlayerClothWindShelter shelter in far.Shelters)
            {
                if (shelter != null)
                {
                    shelter.enabled = true;
                }
            }
            StartUnity(far);
            far.Cloths.Clear();
            far.Shelters.Clear();
        }

        private static void StartUnity(FarPlayer far)
        {
            foreach (Cloth cloth in far.UnityCloths)
            {
                if (cloth != null)
                {
                    cloth.enabled = true;
                    cloth.ClearTransformMotion();
                }
            }
            far.UnityCloths.Clear();
        }

        // Parts destroyed with gear taken off while drawn simpler.
        private static void Prune<T>(List<T> parts) where T : Object
        {
            for (int i = parts.Count - 1; i >= 0; i--)
            {
                if (parts[i] == null)
                {
                    parts.RemoveAt(i);
                }
            }
        }
    }
}
