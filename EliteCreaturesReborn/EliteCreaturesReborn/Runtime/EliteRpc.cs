using EliteCreaturesReborn.Mutations;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// The two world-event broadcasts that cannot ride a creature's ZDO, because by the time they happen the creature
    /// (and its ZDO, and its ZNetView) is gone: a Bloated death has no per-creature channel left to scope to, so both go
    /// out over the game's routed-RPC bus to <c>Everybody</c>.
    /// <list type="bullet">
    /// <item><b>Bloat</b> fires at death: every client spawns a warning that rides its own local corpse for the fuse.</item>
    /// <item><b>Blast</b> fires when the owner's fuse ends: every client draws the blast at the owner's corpse, and only
    /// the machine that sent it (the owner) deals the damage there - so what everyone sees and what actually hurts agree,
    /// even though ragdoll physics settles the corpse in a slightly different spot on every machine.</item>
    /// </list>
    /// Per-creature effects that CAN be scoped (the Warding flash) go through <see cref="CreatureRpc"/> instead. Handlers
    /// are registered lazily once the network is up, keyed on the bus instance so a new world re-registers.
    /// </summary>
    public static class EliteRpc
    {
        public const string Bloat = "ecr_bloat";
        public const string Blast = "ecr_blast";

        private static ZRoutedRpc? _registeredOn;

        /// <summary>
        /// Registers the global handlers on the current routed-RPC bus, once per bus. Keyed on the ZRoutedRpc instance,
        /// not a bool, so leaving a world and joining another (which builds a fresh bus) re-registers rather than going
        /// silent. Safe to call from every creature's setup; the network is always up by then.
        /// </summary>
        public static void EnsureRegistered()
        {
            ZRoutedRpc bus = ZRoutedRpc.instance;
            if (bus == null || ReferenceEquals(bus, _registeredOn))
            {
                return;
            }
            _registeredOn = bus;
            bus.Register<ZPackage>(Bloat, OnBloat);
            bus.Register<ZPackage>(Blast, OnBlast);
        }

        /// <summary>Owner-side, at death: announce the Bloated death so every client wears a warning on its own corpse.</summary>
        public static void FireBloat(Vector3 pos, float baseDamage, float radius, float delay, int stars,
            string warningEffect, string blastEffect)
        {
            EnsureRegistered();
            if (ZRoutedRpc.instance == null)
            {
                return;
            }
            ZPackage pkg = new ZPackage();
            pkg.Write(pos.x); pkg.Write(pos.y); pkg.Write(pos.z);
            pkg.Write(baseDamage); pkg.Write(radius); pkg.Write(delay); pkg.Write(stars);
            pkg.Write(warningEffect ?? ""); pkg.Write(blastEffect ?? "");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Bloat, pkg);
        }

        /// <summary>Owner-side, at fuse end: announce the blast at the owner's corpse so all clients agree on its place.</summary>
        public static void FireBlast(Vector3 pos, float baseDamage, float radius, int stars, string blastEffect)
        {
            EnsureRegistered();
            if (ZRoutedRpc.instance == null)
            {
                return;
            }
            ZPackage pkg = new ZPackage();
            pkg.Write(pos.x); pkg.Write(pos.y); pkg.Write(pos.z);
            pkg.Write(baseDamage); pkg.Write(radius); pkg.Write(stars);
            pkg.Write(blastEffect ?? "");
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, Blast, pkg);
        }

        private static void OnBloat(long sender, ZPackage pkg) => Guard.Run("EliteRpc.OnBloat", () => ReadBloat(sender, pkg));

        // Runs on every client. The sender (the creature's owner) owns the fuse and will fire the blast; only its corpse
        // rider is marked as the one that broadcasts the blast, so the blast position is the owner's corpse alone.
        private static void ReadBloat(long sender, ZPackage pkg)
        {
            Vector3 pos = new Vector3(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle());
            float baseDamage = pkg.ReadSingle(), radius = pkg.ReadSingle(), delay = pkg.ReadSingle();
            int stars = pkg.ReadInt();
            string warning = pkg.ReadString(), blast = pkg.ReadString();
            BloatedCorpse.Spawn(pos, baseDamage, radius, delay, stars, warning, blast, isOwner: sender == ZNet.GetUID());
        }

        private static void OnBlast(long sender, ZPackage pkg) => Guard.Run("EliteRpc.OnBlast", () => ReadBlast(sender, pkg));

        // Runs on every client. Only the sender (the owner) lets the blast deal damage; the rest only draw it.
        private static void ReadBlast(long sender, ZPackage pkg)
        {
            Vector3 pos = new Vector3(pkg.ReadSingle(), pkg.ReadSingle(), pkg.ReadSingle());
            float baseDamage = pkg.ReadSingle(), radius = pkg.ReadSingle();
            int stars = pkg.ReadInt();
            string blast = pkg.ReadString();
            BloatedBlast.Detonate(pos, baseDamage, radius, stars, blast, damaging: sender == ZNet.GetUID());
        }
    }
}
