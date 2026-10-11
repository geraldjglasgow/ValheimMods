using System.Linq;
using EliteCreaturesPack.Core;
using UnityEngine;

namespace EliteCreaturesPack.Custom.Humans
{
    /// <summary>
    /// Rolls a human's look once, on its owner, as it first spawns, and keeps it in its ZDO under the keys the game keeps
    /// a player's look in (<see cref="HumanLookRoll"/>), marked <see cref="Rolled"/> so it never rolls again. VisEquipment,
    /// left flagged as a player's, reads those keys on every peer, so every peer, a player joining later and every reload
    /// draw the same person. The ranges are the definitions' <see cref="HumanLook"/>s, laid onto this component's own
    /// serialized fields (<see cref="Take"/>) so every creature instantiated from the prefab carries them. The values
    /// below are the human base's: anything the game draws on players. Nothing here draws: it runs the same on a
    /// dedicated server.
    /// </summary>
    public sealed class HumanAppearance : MonoBehaviour
    {
        public string Gender = "random";
        public string[] Hair = new string[0];
        public string[] Beard = new string[0];
        public bool BeardlessWomen = true;
        public Color[] HairColours = new Color[0];
        public Vector2 SkinTone = new Vector2(0f, 1f);

        private static readonly int Rolled = "ecp_human_look".GetStableHashCode();

        /// <summary>
        /// Lays a definition's look ranges onto the prefab: what it sets replaces what the base (or an earlier definition
        /// of the chain) gave, what it leaves out (null) stays.
        /// </summary>
        public void Take(HumanLook look)
        {
            Gender = look.Gender ?? Gender;
            Hair = look.Hair?.ToArray() ?? Hair;
            Beard = look.Beard?.ToArray() ?? Beard;
            BeardlessWomen = look.BeardlessWomen ?? BeardlessWomen;
            HairColours = look.HairColours?.ToArray() ?? HairColours;
            SkinTone = look.SkinTone ?? SkinTone;
        }

        private void Start() => SafeCall.Run("human look", static me => me.RollOnce(), this);

        private void RollOnce()
        {
            ZNetView nview = GetComponent<ZNetView>();
            ZDO? zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
            if (zdo == null || !nview!.IsOwner() || zdo.GetBool(Rolled))
            {
                return;
            }
            HumanLookRoll.Write(zdo, this);
            zdo.Set(Rolled, true);
        }
    }
}
