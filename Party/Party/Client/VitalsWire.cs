using UnityEngine;

namespace Party.Client
{
    /// <summary>
    /// The vitals wire format: one ZPackage per report, written by the reporting client, relayed unread by the
    /// server, applied by each receiving member. A package because ZRoutedRpc.Register tops out at six typed
    /// parameters and the deliver RPC already carries the sender id besides.
    /// </summary>
    public static class VitalsWire
    {
        public static ZPackage Write(float health, float stamina, float eitr, int ailments, Vector3 pos, bool posValid)
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(health);
            pkg.Write(stamina);
            pkg.Write(eitr);
            pkg.Write(ailments);
            pkg.Write(pos);
            pkg.Write(posValid);
            return pkg;
        }

        public static void Apply(PartyMemberView member, ZPackage pkg)
        {
            member.Health = pkg.ReadSingle();
            member.Stamina = pkg.ReadSingle();
            member.Eitr = pkg.ReadSingle();
            member.Ailments = pkg.ReadInt();
            member.Position = pkg.ReadVector3();
            member.PositionValid = pkg.ReadBool();
        }
    }
}
