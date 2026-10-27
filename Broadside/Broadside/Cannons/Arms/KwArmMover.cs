using System.Collections.Generic;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A point of a weapon, in kit axes from its joint's pivot as the joint's parts are drawn: held by the joint, or by one
    /// of the weapon's moving parts (named), whose pose then carries it.
    /// </summary>
    public readonly struct KwArmPoint
    {
        public KwArmPoint(KwJoint joint, Vector3 at, string mover = "")
        {
            (Joint, At, Mover) = (joint, at, mover);
        }

        public KwJoint Joint { get; }
        public Vector3 At { get; }
        public string Mover { get; }
    }

    /// <summary>
    /// A moving part of a weapon (<see cref="KwArmDef.Mover"/>): meshes drawn like the joint's parts, in the joint's kit
    /// axes, made into one node, <c>mv_&lt;name&gt;</c>, on the joint or on another moving part it rides on, that turns
    /// and scales about its pivot. Its Fire track plays over <see cref="KwArmDef.FireTime"/> at each shot and ends in the
    /// empty pose; its Reload track is stretched over the reload and ends in the loaded pose, which is how it is drawn.
    /// Every peer poses it from the weapon's state (<see cref="KwArmMotion"/>).
    /// </summary>
    public sealed class KwArmMover
    {
        public const string Prefix = "mv_";

        public KwArmMover(string name, KwJoint joint, Vector3 pivot, string under)
        {
            (Name, Joint, Pivot, Under) = (name, joint, pivot, under);
        }

        public string Name { get; }
        public KwJoint Joint { get; }

        /// <summary>What it turns and scales about, in the joint's kit axes.</summary>
        public Vector3 Pivot { get; }

        /// <summary>The moving part it rides on (defined before it, on the same joint); empty: the joint itself.</summary>
        public string Under { get; }
        public string Node => Prefix + Name;

        public List<(KwLook Look, System.Action<MeshBuilder> Shape)> Parts { get; } = new List<(KwLook Look, System.Action<MeshBuilder> Shape)>();
        public KwTrack Fire { get; } = new KwTrack();
        public KwTrack Reload { get; } = new KwTrack();

        /// <summary>Adds a mesh in one of the kit's looks, drawn in the joint's kit axes.</summary>
        public KwArmMover Part(KwLook look, System.Action<MeshBuilder> shape)
        {
            Parts.Add((look, shape));
            return this;
        }
    }

    /// <summary>
    /// A cord of a weapon (<see cref="KwArmDef.Cord"/>), a bowstring's half say: a round line in one look between two points
    /// of one joint, <c>cd_&lt;name&gt;</c> under it, either point held by a moving part, laid between them again whenever
    /// the parts move, so it stretches and swings with them.
    /// </summary>
    public sealed class KwArmCord
    {
        public const string Prefix = "cd_";

        public KwArmCord(string name, KwLook look, float radius, KwArmPoint from, KwArmPoint to)
        {
            (Name, Look, Radius, From, To) = (name, look, radius, from, to);
        }

        public string Name { get; }
        public KwLook Look { get; }
        public float Radius { get; }
        public KwArmPoint From { get; }
        public KwArmPoint To { get; }
        public string Node => Prefix + Name;
    }

    /// <summary>
    /// Where the gunner's fists hold a weapon (<see cref="KwArmDef.Hands"/>, <see cref="KwArmDef.ReloadHands"/>): a point for
    /// the left hand and one for the right, on a joint or on a moving part (then the hands go with it, round a crank say),
    /// and how far the gunner leans in to them, degrees forward.
    /// </summary>
    public sealed class KwArmGrip
    {
        public KwArmGrip(KwArmPoint left, KwArmPoint right)
        {
            (Left, Right) = (left, right);
        }

        public KwArmPoint Left { get; }
        public KwArmPoint Right { get; }
        public float Lean { get; set; }
    }
}
