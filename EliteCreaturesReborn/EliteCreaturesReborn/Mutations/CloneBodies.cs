using UnityEngine;

namespace EliteCreaturesReborn.Mutations
{
    /// <summary>
    /// A Cloning creature and its decoy are made in the same place, so their bodies would shove each other apart the
    /// moment the decoy appears and give the trick away. Each machine tells its own physics to let the two pass through
    /// each other: the decoy's owner as it makes it, and every machine as the decoy wakes there, so whichever machine
    /// later owns either body already ignores the other. Local and harmless: it touches only this pair.
    /// </summary>
    internal static class CloneBodies
    {
        public static void Ignore(Character? real, Character? decoy)
        {
            if (real == null || decoy == null)
            {
                return;
            }
            Collider[] decoyParts = decoy.GetComponentsInChildren<Collider>(true);
            foreach (Collider part in real.GetComponentsInChildren<Collider>(true))
            {
                foreach (Collider other in decoyParts)
                {
                    if (part != null && other != null)
                    {
                        Physics.IgnoreCollision(part, other);
                    }
                }
            }
        }
    }
}
