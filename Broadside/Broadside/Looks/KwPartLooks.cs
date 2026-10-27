using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The looks a generated part adds as it builds on the dock (<see cref="KwGenerated"/>, on every peer): planking's
    /// painted bands (<see cref="KwBandsShown"/>, screens only), its openings' frames, covers and doors
    /// (<see cref="KwOpeningPart"/>, everywhere: their owner answers calls) and a railing's shield hooks
    /// (<see cref="KwRailShields"/>, everywhere too). A throw here is logged and leaves the part built.
    /// </summary>
    public static class KwPartLooks
    {
        public static void Built(GameObject part, string tool, string saved, KwDef? def, KwBiomeTier tier)
        {
            if (tool == "planking")
            {
                KwShipBody.Step("Broadside painted bands", () => KwBandsShown.Dress(part.transform, saved, def, tier));
                KwShipBody.Step("Broadside openings", () => KwOpeningPart.Fit(part, tool, saved, tier));
            }
            else if (tool == "rail")
                KwShipBody.Step("Broadside shield hooks", () => KwRailShields.Fit(part, saved));
        }
    }
}
