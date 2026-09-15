using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Visuals;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// The per-creature calls routed through a creature's own ZNetView, because that channel is the one the engine
    /// scopes to the clients that actually hold the creature. One is an owner-directed command - "you devoured this, take
    /// it" (to the devourer's owner, where its ZDO and health live). The other is a scoped effect broadcast - the Warding
    /// reflect flash - which every client holding the creature draws and no one else receives, unlike the world-wide bus
    /// in <see cref="EliteRpc"/>. (The prey pin is not here: a bite pins the prey directly on the prey's own owner, which
    /// is the same machine the hit resolves on, so it needs no routing.) Registered once per creature; sending to an
    /// object you already own is delivered locally, so a single-machine session uses one path.
    /// </summary>
    public static class CreatureRpc
    {
        public const string Devour = "ecr_devour";
        public const string Flash = "ecr_flash";

        /// <summary>Registers all handlers on a creature, capturing its own Character/controller. Called once per creature.</summary>
        public static void Register(ZNetView nview, Character character, EliteController controller)
        {
            // The command handler runs on THIS creature's owner - that is where a no-target routed call is delivered.
            nview.Register<float, float>(Devour,
                (sender, health, damage) => ApplyAbsorb(character, controller, health, damage));
            // The flash handler runs on EVERY client holding the creature, drawing a pure cosmetic; nobody gates it.
            nview.Register<Vector3, float, string>(Flash, (sender, pos, radius, role) => DrawFlash(pos, radius, role));
        }

        /// <summary>
        /// Scoped effect broadcast through the creature's own ZNetView. <c>Everybody</c> is delivered locally to the
        /// sender and relayed to peers, but the game dispatches it only where this creature's ZNetView exists (others
        /// drop it), so a player three biomes away never draws a flash from a fight they cannot see. Keeps the wire quiet
        /// even though Warding fires on every melee hit.
        /// </summary>
        public static void FireFlash(ZNetView creatureView, Vector3 pos, float radius, string role)
        {
            if (creatureView != null && creatureView.IsValid())
            {
                creatureView.InvokeRPC(ZRoutedRpc.Everybody, Flash, pos, radius, role ?? "");
            }
        }

        private static void DrawFlash(Vector3 pos, float radius, string role) =>
            Guard.Run("CreatureRpc.Flash", () => CosmeticClone.Flash(EffectResolver.ForRole(role), pos, radius));

        /// <summary>Devourer-owner-side: bank a kill's health and damage. Routed to the owner from wherever the kill resolved.</summary>
        public static void Absorb(Character killer, float health, float damage)
        {
            ZNetView nview = killer.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return;
            }
            if (nview.IsOwner())
            {
                ApplyAbsorb(killer, killer.GetComponent<EliteController>(), health, damage);
                return;
            }
            nview.InvokeRPC(Devour, health, damage); // targets the ZDO owner
        }

        private static void ApplyAbsorb(Character killer, EliteController controller, float health, float damage) =>
            Guard.Run("CreatureRpc.Absorb", () => Bank(killer, controller, health, damage));

        private static void Bank(Character killer, EliteController controller, float health, float damage)
        {
            if (killer == null || controller == null || !controller.IsOwner())
            {
                return;
            }
            ZDO zdo = controller.View.GetZDO();
            TraitStore.AddDevoured(zdo, health, damage);
            StartCooldown(controller, zdo);
            controller.RefreshHealth();
            killer.Heal(health, showText: false);
        }

        // The meal is banked, so the cooldown starts here on the devourer's owner: it cannot eat again until this instant.
        // Stored as whole milliseconds off the shared clock, so every machine reads the same deadline for the next bite.
        private static void StartCooldown(EliteController controller, ZDO zdo)
        {
            float seconds = controller.Rules.PowerOf(Mutation.Devouring, Fields.DevourCooldown);
            double now = ZNet.instance != null ? ZNet.instance.GetTimeSeconds() : 0.0;
            TraitStore.SetDevourReadyAt(zdo, (long)((now + seconds) * 1000.0));
        }
    }
}
