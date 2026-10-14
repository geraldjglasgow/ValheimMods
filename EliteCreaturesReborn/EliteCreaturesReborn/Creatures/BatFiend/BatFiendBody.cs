using System.Collections.Generic;
using System.Reflection;
using BundlePrefabs;
using UnityEngine;

namespace EliteCreaturesReborn.Creatures.BatFiend
{
    /// <summary>
    /// The Wolf copy's body swapped for the Bat-fiend's (the crypt mimic's and Old Gorehide's way): the Wolf's
    /// <c>Visual</c> goes and the dressed model takes its place under that name, with the game's
    /// <see cref="CharacterAnimEvent"/> on its animator (which receives the clips' hit events; no foot IK or head turn,
    /// both need a humanoid avatar), an <c>EyePos</c> at its head for the AI, the Wolf's star looks on its body (sizes and
    /// tints, the Wolf's own extra objects dropped), footsteps on its two feet and two hands from the walk and run clips'
    /// footstep curve, the Wolf's equipment joints on its chest (its attacks hold no model), and the colliders fitted to
    /// it: the character capsule upright round its middle and a box over the trunk so the long body can be hit anywhere.
    /// </summary>
    internal static class BatFiendBody
    {
        private static readonly string[] Feet = { "Foot.L", "Foot.R", "Hand.L", "Hand.R" };
        private const string Chest = "Chest";
        private const float EyeAhead = 0.22f, EyeUp = 0.1f;

        public static void Wear(GameObject creature)
        {
            List<LevelEffects.LevelSetup> setups = Setups(creature.GetComponentInChildren<LevelEffects>(true));
            Transform? old = creature.transform.Find("Visual");
            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }
            GameObject visual = BatFiendModel.Copy(creature.transform);
            Looks(visual, setups);
            Events(visual);
            Eye(creature, visual.transform);
            Steps(creature.GetComponent<FootStep>(), visual.transform);
            Equipment(creature.GetComponent<VisEquipment>(), visual.transform);
            Shape(creature);
        }

        /// <summary>The Wolf's star looks (size, hue, saturation, value, glow), without the objects of its own body they switched on.</summary>
        private static List<LevelEffects.LevelSetup> Setups(LevelEffects? wolf)
        {
            var setups = new List<LevelEffects.LevelSetup>();
            foreach (LevelEffects.LevelSetup setup in wolf != null ? wolf.m_levelSetups : new List<LevelEffects.LevelSetup>())
            {
                setups.Add(new LevelEffects.LevelSetup
                {
                    m_scale = setup.m_scale, m_hue = setup.m_hue, m_saturation = setup.m_saturation, m_value = setup.m_value,
                    m_setEmissiveColor = setup.m_setEmissiveColor, m_emissiveColor = setup.m_emissiveColor,
                });
            }
            return setups;
        }

        private static void Looks(GameObject visual, List<LevelEffects.LevelSetup> setups)
        {
            LevelEffects looks = visual.AddComponent<LevelEffects>();
            looks.m_mainRender = BatFiendModel.Body(visual);
            looks.m_levelSetups = setups;
            looks.m_baseEnableObject = null;
        }

        private static void Events(GameObject visual)
        {
            CharacterAnimEvent events = visual.AddComponent<CharacterAnimEvent>();
            (events.m_footIK, events.m_headRotation) = (false, false);
            events.m_feets = new CharacterAnimEvent.Foot[0];
            events.m_eyes = new Transform[0];
        }

        /// <summary>What the AI sees from: a point at the front of its head (on the creature's own front when it has no head bone).</summary>
        private static void Eye(GameObject creature, Transform visual)
        {
            Transform? stale = creature.transform.Find("EyePos");
            if (stale != null)
            {
                Object.DestroyImmediate(stale.gameObject);
            }
            Transform? head = BatFiendModel.Head(visual);
            var eye = new GameObject("EyePos").transform;
            eye.SetParent(head != null ? head : creature.transform, false);
            eye.position = head != null
                ? head.position + creature.transform.forward * EyeAhead + Vector3.up * EyeUp
                : creature.transform.TransformPoint(new Vector3(0f, 1f, 0.9f));
            creature.GetComponent<Character>().m_eye = eye;
        }

        /// <summary>A step at whichever foot or hand is furthest forward, each time the walk or run clip's footstep curve changes sign.</summary>
        private static void Steps(FootStep? steps, Transform visual)
        {
            if (steps == null)
            {
                return;
            }
            var feet = new List<Transform>();
            foreach (string name in Feet)
            {
                Transform? foot = GameMaterials.Find(visual, name);
                if (foot != null)
                {
                    feet.Add(foot);
                }
            }
            (steps.m_feet, steps.m_footlessFootsteps) = (feet.ToArray(), feet.Count == 0);
        }

        /// <summary>
        /// Every joint of the Wolf's equipment pointed at the chest, its body model cleared: the joints were in the Visual
        /// that is gone, and the game attaches a held item to whichever joint it names (the attacks carry no model).
        /// </summary>
        private static void Equipment(VisEquipment? equipment, Transform visual)
        {
            if (equipment == null)
            {
                return;
            }
            Transform? chest = GameMaterials.Find(visual, Chest) ?? visual;
            foreach (FieldInfo field in typeof(VisEquipment).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.FieldType == typeof(Transform))
                {
                    field.SetValue(equipment, chest);
                }
            }
            equipment.m_bodyModel = null;
        }

        /// <summary>The capsule the game moves and grounds it by, and the hit box over its trunk, both at model scale.</summary>
        private static void Shape(GameObject creature)
        {
            CapsuleCollider capsule = creature.GetComponent<CapsuleCollider>();
            (capsule.radius, capsule.height, capsule.direction) = (BatFiendNumbers.CapsuleRadius, BatFiendNumbers.CapsuleHeight, 1);
            capsule.center = new Vector3(0f, BatFiendNumbers.CapsuleHeight * 0.5f, 0f);
            var shell = new GameObject("body_shell") { layer = creature.layer };
            shell.transform.SetParent(creature.transform, false);
            var box = shell.AddComponent<BoxCollider>();
            (box.size, box.center) = (BatFiendNumbers.ShellSize, BatFiendNumbers.ShellCenter);
            Rigidbody? body = creature.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.mass *= BatFiendNumbers.MassFactor;
            }
        }
    }
}
