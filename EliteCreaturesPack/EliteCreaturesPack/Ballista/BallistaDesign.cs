using MannedWeapons;

namespace EliteCreaturesPack.Ballista
{
    /// <summary>
    /// The Bone Ballista as a manned weapon (the MannedWeapons library): its ZDO keys and calls (`ecp_bal_*`, the names it
    /// has always kept, so built ballistas keep their aim and load), its turn (45 degrees either side of where it was placed,
    /// 90 in all, 20 down and 25 up; the view held to the same and to 240 degrees a second, about what the holder's jog
    /// round its back keeps up with, and tilting no further than the bolt can be laid on what the crosshair is on, nor
    /// faster than the ballista tilts, within 25 up and 30 down), its reload (pull 1.4 s, load 1.2 s taking a Bone Missile
    /// as the load begins, half a second's rest after a shot), its words, and the holder: taken from anywhere within the
    /// library's 4.5 m of the stand point (in front of it too: he is brought round behind it), counted while within 6 m.
    /// </summary>
    public static class BallistaDesign
    {
        public static readonly WeaponDesign Design = new WeaponDesign("bone ballista", new WeaponKeys("ecp_bal", stage: "spring", step: "act", stepAt: "act_at"))
        {
            Aim = new WeaponArcs
            {
                YawLimit = 45f, PitchDown = -20f, PitchUp = 25f, TurnRate = 720f, TiltRate = 200f,
                ViewRate = 240f, LookUp = 25f, LookDown = 30f, Follow = 12f, WriteStep = 0.1f, Reach = 150f,
            },
            Reload = new ReloadCycle(BallistaTimeline.PullEnd, BallistaTimeline.LoadEnd)
            {
                Rest = BallistaTimeline.Settle, AmmoType = BallistaMissile.AmmoType, TakeAt = (int)Act.Loading,
            },
            Body = new OperatorBody
            {
                Arrive = 0.06f, Restart = 0.15f, Slow = 0.25f, Walking = 0.5f, TurnRate = 720f, GiveUp = 0.6f, Still = 0.1f,
                Reached = 0.2f, Reaching = 0.6f, Ease = 8f, Elbows = ElbowHint.Beside,
            },
            Words = new WeaponWords
            {
                Name = "$" + BallistaPiece.Word, Loaded = "$hud_ecp_bal_loaded", Empty = "$hud_ecp_bal_empty",
                Hold = "$hud_ecp_bal_hold", LetGo = "$hud_ecp_bal_letgo", NoAmmo = "$msg_ecp_bal_nomissiles", Unreachable = "$msg_ecp_bal_unreachable",
            },
            HolderRange = 6f,
        };
    }
}
