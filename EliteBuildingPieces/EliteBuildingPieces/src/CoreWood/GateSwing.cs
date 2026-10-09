using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EliteBuildingPieces.CoreWood
{
    /// <summary>
    /// Swings a core wood door's leaves to the state the game's Door gives it: 0 closed, 1 open away from a user standing
    /// behind it (towards +Z), -1 open the other way. 100 degrees in 0.7 s; the left leaf turns negative about Y to open
    /// towards +Z, the right one mirrored. The game's Door does everything else (the use key, the ward check, sounds,
    /// the state in the ZDO, the animator whose tags time its use), so this only follows it. Door.SetState runs on every
    /// peer, the dedicated server included, where the leaves' colliders have to move too. Switched off between swings;
    /// a door that loads open shows open at once.
    /// </summary>
    public sealed class GateSwing : MonoBehaviour
    {
        private const float OpenAngle = 100f;
        private const float Speed = OpenAngle / 0.7f;

        private Transform[] leaves;
        private float[] turns;
        private float angle;
        private float target;
        private bool shown;

        private void Awake() => enabled = false;

        public void Follow(int state)
        {
            Find();
            target = Mathf.Clamp(state, -1, 1) * OpenAngle;
            if (!shown)
            {
                shown = true;
                Turn(angle = target);
            }
            enabled = angle != target;
        }

        private void Update()
        {
            angle = Mathf.MoveTowards(angle, target, Speed * Time.deltaTime);
            Turn(angle);
            if (angle == target)
                enabled = false;
        }

        private void Turn(float degrees)
        {
            for (int i = 0; i < leaves.Length; i++)
                leaves[i].localRotation = Quaternion.Euler(0f, degrees * turns[i], 0f);
        }

        private void Find()
        {
            if (leaves != null)
                return;
            List<Transform> found = new List<Transform>();
            List<float> signs = new List<float>();
            foreach (Transform part in GetComponentsInChildren<Transform>(true))
            {
                if (part.name == "door_left" || part.name == "door_right")
                {
                    found.Add(part);
                    signs.Add(part.name == "door_left" ? -1f : 1f);
                }
            }
            leaves = found.ToArray();
            turns = signs.ToArray();
        }
    }

    /// <summary>Hands each state the game's Door shows to the door's <see cref="GateSwing"/>, if it has one.</summary>
    [HarmonyPatch(typeof(Door), nameof(Door.SetState))]
    public static class GateSwingPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Door __instance, int state)
        {
            GateSwing swing = __instance.GetComponent<GateSwing>();
            if (swing != null)
                swing.Follow(state);
        }
    }
}
