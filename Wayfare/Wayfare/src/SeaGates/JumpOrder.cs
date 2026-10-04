using UnityEngine;

namespace Wayfare.SeaGates
{
    /// <summary>What the ship's owner tells each crew member when a jump begins (<see cref="CrewJump.JumpRpc"/>).
    /// Source pose: where the ship was frozen. Destination pose: where it will be placed (it may still be slid clear
    /// on arrival, so the crew lands against the live ship, not this pose). Destination pillars: for the dry-ground
    /// fallback. Wait seconds: how long the crew waits for the ship to settle before taking the fallback.</summary>
    public sealed class JumpOrder
    {
        public int JumpId;
        public ZDOID Ship;
        public Vector3 SourcePos;
        public Quaternion SourceRot;
        public Vector3 DestPos;
        public Quaternion DestRot;
        public Vector3 DestAnchor;
        public Vector3 DestPartner;
        public float WaitSeconds;

        public ZPackage Write()
        {
            ZPackage pkg = new ZPackage();
            pkg.Write(JumpId);
            pkg.Write(Ship);
            pkg.Write(SourcePos);
            pkg.Write(SourceRot);
            pkg.Write(DestPos);
            pkg.Write(DestRot);
            pkg.Write(DestAnchor);
            pkg.Write(DestPartner);
            pkg.Write(WaitSeconds);
            return pkg;
        }

        public static JumpOrder Read(ZPackage pkg)
        {
            return new JumpOrder
            {
                JumpId = pkg.ReadInt(),
                Ship = pkg.ReadZDOID(),
                SourcePos = pkg.ReadVector3(),
                SourceRot = pkg.ReadQuaternion(),
                DestPos = pkg.ReadVector3(),
                DestRot = pkg.ReadQuaternion(),
                DestAnchor = pkg.ReadVector3(),
                DestPartner = pkg.ReadVector3(),
                WaitSeconds = pkg.ReadSingle(),
            };
        }
    }
}
