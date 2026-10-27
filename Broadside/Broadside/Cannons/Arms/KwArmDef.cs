using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A set of cannons, found by reflection (<see cref="KwArmory"/>): any class with a parameterless constructor
    /// that implements this, in its own file, adds its weapons to the hammer; nothing shared needs editing.
    /// </summary>
    public interface IKwArmSet
    {
        IEnumerable<KwArmDef> Arms();
    }

    /// <summary>What part of a weapon a mesh moves with: the foot (fixed on the deck), the yaw part (turns about the upright), the pitch part (tilts on the yaw part).</summary>
    public enum KwJoint
    {
        Foot,
        Yaw,
        Pitch,
    }

    /// <summary>
    /// One mesh of a weapon: the joint it moves with, the game material it wears (<see cref="KwTierMaterials"/>) and its
    /// shape, drawn in kit axes from that joint's own pivot: a foot part from the weapon's origin, a yaw part from
    /// <see cref="KwArmDef.YawPivot"/>, a pitch part from <see cref="KwArmDef.PitchPivot"/> with the barrel along +x.
    /// Parts of one joint and look are drawn as one mesh.
    /// </summary>
    public sealed class KwArmPart
    {
        public KwArmPart(KwJoint joint, KwLook look, System.Action<MeshBuilder> shape)
        {
            Joint = joint;
            Look = look;
            Shape = shape;
        }

        public KwJoint Joint { get; }
        public KwLook Look { get; }
        public System.Action<MeshBuilder> Shape { get; }
    }

    /// <summary>
    /// A ship cannon (<see cref="KwArm"/> in the world): the piece <c>BS_&lt;id&gt;</c> (<c>BS_cannon_small_iron</c> and so
    /// on), built only on a launched Broadside ship's deck (<see cref="KwArmPlace"/>) on its naval truck carriage
    /// (<see cref="Ship"/>); its cost, its meshes by joint, its moving parts and cords with their fire and reload tracks
    /// (the archive's <c>AssetLab/AssetLab/Keelwright/Arms/ANIMATION.md</c>), where its pivots, muzzle, gunner's feet and
    /// hands are, how far and fast it turns, and what its triggers fire. Kit axes: x forward (the way the carriage faces),
    /// y left, z up, metres; the origin is on the deck under the middle of its carriage.
    /// </summary>
    public sealed class KwArmDef
    {
        /// <summary>Every prefab of the cannons starts with <see cref="Prefix"/>; the cannons' own with <see cref="CannonPrefix"/>.</summary>
        public const string Prefix = "BS_", CannonPrefix = "BS_cannon_";

        public KwArmDef(string id, string label, int tier, KwShot primary)
        {
            Id = id;
            Label = label;
            Tier = tier;
            Primary = primary;
        }

        public string Id { get; }
        public string Label { get; }

        /// <summary>The biome tier (<see cref="KwTiers"/>) it belongs to.</summary>
        public int Tier { get; }
        public string Prefab => Prefix + Id;
        public string Description { get; set; } = "";

        /// <summary>What the piece costs in the hammer: game or Broadside item prefab names and amounts, refunded on removal.</summary>
        public List<(string Item, int Amount)> Cost { get; } = new List<(string Item, int Amount)>();
        public List<KwArmPart> Parts { get; } = new List<KwArmPart>();

        /// <summary>Adds a part: its joint, its look and its shape (drawn from the joint's pivot).</summary>
        public KwArmDef Part(KwJoint joint, KwLook look, System.Action<MeshBuilder> shape)
        {
            Parts.Add(new KwArmPart(joint, look, shape));
            return this;
        }

        /// <summary>
        /// Adds a moving part named <paramref name="name"/> on a joint, or riding on the moving part named
        /// <paramref name="under"/> (defined before it, on the same joint), turning about <paramref name="pivot"/> (the joint's
        /// kit axes); give it meshes with <see cref="KwArmMover.Part(KwLook, System.Action{MeshBuilder})"/> and keys on its
        /// <see cref="KwArmMover.Fire"/> and <see cref="KwArmMover.Reload"/> tracks.
        /// </summary>
        public KwArmMover Mover(string name, KwJoint joint, Vector3 pivot, string under = "")
        {
            KwArmMover mover = new KwArmMover(name, joint, pivot, under);
            Movers.Add(mover);
            return mover;
        }

        /// <summary>Adds a cord in a look, so thick, between two points of one joint (either held by a moving part).</summary>
        public KwArmDef Cord(string name, KwLook look, float radius, KwArmPoint from, KwArmPoint to)
        {
            Cords.Add(new KwArmCord(name, look, radius, from, to));
            return this;
        }

        /// <summary>The yaw part's pivot from the origin, the pitch part's from the yaw pivot, the muzzle from the pitch pivot.</summary>
        public Vector3 YawPivot { get; set; } = new Vector3(0f, 0f, 0.05f);
        public Vector3 PitchPivot { get; set; } = new Vector3(0f, 0f, 0.6f);
        public Vector3 Muzzle { get; set; } = new Vector3(1f, 0f, 0f);

        /// <summary>
        /// Where the gunner's feet are walked to, from the origin with the weapon facing ahead: behind the breech, a little
        /// to its right, on the yaw part, so it turns round with the carriage as it is levered round (but does not roll
        /// back with it; he steps aside from it at a shot, out of the carriage's recoil, <see cref="KwShipRecoil"/>).
        /// </summary>
        public Vector3 Stand { get; set; }

        /// <summary>The four-truck naval carriage it stands on, hauled about the deck by its rear (<see cref="KwDeckHaul"/>).</summary>
        public KwShipSpec Ship { get; set; } = null!;

        /// <summary>Degrees the gunner turns from the way the weapon points, toward its left, once at his stand point (behind its breech on its right, he turns a little toward it); 0, he faces where it points.</summary>
        public float Facing { get; set; }

        /// <summary>Where the gunner's fists hold while aiming, and (eased in and out) while reloading; none: his arms as the game poses them.</summary>
        public KwArmGrip? Hands { get; set; }
        public KwArmGrip? ReloadHands { get; set; }

        /// <summary>
        /// The most the gunner leans in to his grips, degrees, and how much further he leans for each degree the weapon is
        /// tilted up (its grips behind the trunnions drop as it is raised): at a low breech far behind its trunnions he
        /// bends well over and follows its tilt (<see cref="KwArmHands"/>).
        /// </summary>
        public float MostLean { get; set; } = 50f;
        public float TiltLean { get; set; } = 0.8f;

        /// <summary>
        /// Degrees each way from ahead (180: all round), down and up; turning speeds in degrees a second. A cannon that does
        /// not turn all round (every ship cannon) holds its gunner's view to its arc and its turn speed (<see cref="KwArmDesigns"/>).
        /// </summary>
        public float YawLimit { get; set; } = 180f;
        public float PitchDown { get; set; } = -10f;
        public float PitchUp { get; set; } = 25f;
        public float TurnSpeed { get; set; } = 60f;
        public float TiltSpeed { get; set; } = 30f;

        public float Health { get; set; } = 400f;

        /// <summary>
        /// Seconds the shot's motion lasts (every moving part's Fire track, <see cref="KwArmMover"/>); the reload starts as it
        /// ends and lasts the shot's <see cref="KwShot.Reload"/> less this, so a held trigger still fires every Reload seconds.
        /// </summary>
        public float FireTime { get; set; } = 0.25f;

        /// <summary>
        /// Seconds into the shot's motion the shot leaves: the projectile, the muzzle's effects and the recoil kick (a cannon
        /// fires halfway through its lanyard's pull); 0, at the trigger (<see cref="KwArmLaunch"/>).
        /// </summary>
        public float FireDelay { get; set; }

        /// <summary>The moving parts (<see cref="KwArmMover"/>) and cords (<see cref="KwArmCord"/>), posed by the weapon's state on every peer.</summary>
        public List<KwArmMover> Movers { get; } = new List<KwArmMover>();
        public List<KwArmCord> Cords { get; } = new List<KwArmCord>();

        /// <summary>The attack button's shot; the secondary attack's, when it has one (a load for it takes as long as one for the attack's shot: the reload is the cannon's, <see cref="KwArmDesigns"/>).</summary>
        public KwShot Primary { get; }
        public KwShot? Alternate { get; set; }

        /// <summary>Last touches to the piece on the bench once its parts are drawn, with its prefab (parts in a game material no look has, say); none by default.</summary>
        public System.Action<GameObject>? Finish { get; set; }
    }
}
