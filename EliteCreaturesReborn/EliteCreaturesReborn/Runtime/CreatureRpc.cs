using EliteCreaturesReborn.Aspects;
using EliteCreaturesReborn.Mutations;
using EliteCreaturesReborn.Rules;
using EliteCreaturesReborn.Traits;
using EliteCreaturesReborn.Util;
using EliteCreaturesReborn.Visuals;
using PatchGuard;
using UnityEngine;

namespace EliteCreaturesReborn.Runtime
{
    /// <summary>
    /// The per-creature calls routed through a creature's own ZNetView, because that channel is the one the engine
    /// scopes to the clients that actually hold the creature. One is an owner-directed command - "you devoured this, take
    /// it" (to the devourer's owner, where its ZDO and health live, and where the meal joins its meal list). The other is
    /// a scoped effect broadcast - the Warding reflect sound, the devour tell and the other one-shot tells - which every
    /// client holding the creature draws and no one else receives, unlike the world-wide bus
    /// in <see cref="EliteRpc"/>. Two more owner-directed commands serve the boss aspects: a Twin's share of its
    /// partner's lost health, and a Phantom copy's dismissal when its boss dies. (The prey pin is not here: a bite pins
    /// the prey directly on the prey's own owner, which is the same machine the hit resolves on, so it needs no
    /// routing.) Registered once per creature; sending to an object you already own is delivered locally, so a
    /// single-machine session uses one path.
    /// </summary>
    public static class CreatureRpc
    {
        public const string Devour = "ecr_devour";
        public const string Flash = "ecr_flash";
        public const string TwinShare = "ecr_twin_share";
        public const string VanishCall = "ecr_vanish";

        /// <summary>The flash role of the devour tell, as it travels over the effect bus.</summary>
        public const string DevourRole = "devour";

        /// <summary>The flash role of the reflect tell (Warding, and the Reflective boss aspect).</summary>
        public const string ReflectRole = "reflect";

        /// <summary>
        /// The reflect tell is only heard, at this share of its effect's own loudness: it plays on every hit a Warding
        /// creature or a Reflective boss takes, and drawn it cost frames in a long fight while its sound drowned the
        /// fight's own (the user asked for no particles and a quieter sound).
        /// </summary>
        private const float ReflectVolume = 0.3f;

        /// <summary>Registers all handlers on a creature, capturing its own Character/controller. Called once per creature.</summary>
        public static void Register(ZNetView nview, Character character, EliteController controller)
        {
            // The command handler runs on THIS creature's owner - that is where a no-target routed call is delivered.
            nview.Register<float, float, int>(Devour,
                (sender, health, damage, meal) => ApplyAbsorb(character, controller, health, damage, meal));
            // The flash handler runs on EVERY client holding the creature, drawing a pure cosmetic; nobody gates it.
            nview.Register<Vector3, float, string>(Flash, (sender, pos, radius, role) => DrawFlash(pos, radius, role));
            // Twin's share of a partner's lost health, and a Phantom copy's dismissal: both commands to this owner.
            nview.Register<float, bool>(TwinShare, (sender, loss, fatal) => ReceiveShare(character, loss, fatal));
            nview.Register(VanishCall, sender => Guard.Run("CreatureRpc.Vanish", () => Fall(character)));
        }

        private static void ReceiveShare(Character character, float loss, bool fatal) =>
            Guard.Run("CreatureRpc.TwinShare", () => ShareInto(character, loss, fatal));

        private static void ShareInto(Character character, float loss, bool fatal)
        {
            TwinLink link = character.GetComponent<TwinLink>();
            if (link != null)
            {
                link.Receive(loss, fatal);
            }
        }

        /// <summary>Boss-owner side, when a Phantom boss dies: routes the copy's dismissal to whichever machine owns it.</summary>
        public static void Vanish(Character copy)
        {
            ZNetView nview = copy.GetComponent<ZNetView>();
            if (nview != null && nview.IsValid())
            {
                nview.InvokeRPC(VanishCall); // targets the copy's owner; delivered locally when that is this machine
            }
        }

        // On the copy's owner: health to zero, so the game's own death check lets it fall - hollow, so no drops, no body.
        private static void Fall(Character copy)
        {
            ZNetView nview = copy.GetComponent<ZNetView>();
            if (nview != null && nview.IsValid() && nview.IsOwner() && !copy.IsDead())
            {
                copy.SetHealth(0f);
            }
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

        /// <summary>The reflect tell at the attacker, the same scoped way; it carries no size, since nothing is drawn.</summary>
        public static void FireReflect(ZNetView creatureView, Vector3 pos) => FireFlash(creatureView, pos, 0f, ReflectRole);

        /// <summary>
        /// The devour tell at the prey, through the DEVOURER's view, so every client that can see the fight hears the
        /// creature eaten. Only heard (the user asked for no particles), so it carries no size either.
        /// </summary>
        public static void FireDevour(ZNetView devourerView, Vector3 pos) => FireFlash(devourerView, pos, 0f, DevourRole);

        private static void DrawFlash(Vector3 pos, float radius, string role) =>
            Guard.Run("CreatureRpc.Flash", () => DrawRole(pos, radius, role));

        private static void DrawRole(Vector3 pos, float radius, string role)
        {
            GameObject? prefab = EffectResolver.ForRole(role);
            if (role == ReflectRole || role == DevourRole)
            {
                CosmeticClone.SoundOnly(prefab, pos, role == ReflectRole ? ReflectVolume : 1f);
                return;
            }
            CosmeticClone.Flash(prefab, pos, radius);
        }

        /// <summary>
        /// Devourer-owner-side: bank a kill's health and damage, and the eaten creature's prefab (<paramref name="meal"/>)
        /// in its meal list. Routed to the owner from wherever the kill resolved.
        /// </summary>
        public static void Absorb(Character killer, float health, float damage, int meal)
        {
            ZNetView nview = killer.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return;
            }
            if (nview.IsOwner())
            {
                ApplyAbsorb(killer, killer.GetComponent<EliteController>(), health, damage, meal);
                return;
            }
            nview.InvokeRPC(Devour, health, damage, meal); // targets the ZDO owner
        }

        private static void ApplyAbsorb(Character killer, EliteController controller, float health, float damage, int meal) =>
            Guard.Run("CreatureRpc.Absorb", () => Bank(killer, controller, health, damage, meal));

        // The owner re-checks the allowance before banking: one swing can bite two creatures before the first meal is
        // banked, and a meal past the allowance is not kept - the prey is gone, but the devourer gains nothing from it.
        private static void Bank(Character killer, EliteController controller, float health, float damage, int meal)
        {
            if (killer == null || controller == null || !controller.IsOwner())
            {
                return;
            }
            ZDO zdo = controller.View.GetZDO();
            if (DevourLimits.Sated(controller))
            {
                Log.Diag($"{killer.name} has eaten its {MealStore.Count(zdo)}; this meal is not banked");
                return;
            }
            MealStore.Add(zdo, meal);
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
