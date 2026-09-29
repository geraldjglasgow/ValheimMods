using System.Collections.Generic;
using System.Linq;
using DevBridge.Server;
using UnityEngine;

namespace DevBridge.Stage
{
    /// <summary>
    /// A bundle asset worn by a game creature's still copy, as the mods put the workshop's gear on the game's creatures.
    /// A kit (top-level children named after the creature's bones: l_hand, r_hand, root, ...) hangs each child on the
    /// bone of its name at no offset and unit scale, as the mods' kit code does; with bone= the whole asset hangs on one
    /// bone at no offset, keeping its world scale, as VisEquipment attaches an item.
    /// </summary>
    internal static class Wear
    {
        /// <summary>Hangs the asset on the body; returns the bones used and the mounts that found no bone.</summary>
        internal static Dictionary<string, List<string>> Hang(GameObject asset, GameObject body, string bone)
        {
            asset.transform.SetParent(body.transform, true);
            if (bone != null) return Whole(asset, body, bone);
            var result = new Dictionary<string, List<string>> { ["hung"] = new List<string>(), ["no bone"] = new List<string>() };
            foreach (Transform mount in asset.transform.Cast<Transform>().ToArray())
            {
                Transform target = Bone(body, asset, mount.name);
                result[target ? "hung" : "no bone"].Add(mount.name);
                if (!target) continue;
                mount.SetParent(target, false);
                (mount.localPosition, mount.localRotation, mount.localScale) = (Vector3.zero, Quaternion.identity, Vector3.one);
            }
            if (result["hung"].Count == 0)
                throw new BridgeException($"none of {asset.name}'s top-level children is named after a bone of {body.name}: give bone=<bone name> to hang it whole");
            return result;
        }

        private static Dictionary<string, List<string>> Whole(GameObject asset, GameObject body, string bone)
        {
            Transform target = Bone(body, asset, bone) ?? throw new BridgeException($"{body.name} has no bone {bone} (common: RightHand_Attach, LeftHand_Attach, Head, Spine2)");
            asset.transform.SetParent(target, true);
            asset.transform.localPosition = Vector3.zero;
            asset.transform.localRotation = Quaternion.identity;
            return new Dictionary<string, List<string>> { ["hung"] = new List<string> { bone } };
        }

        /// <summary>A transform anywhere in the body by exact name, not inside the asset being hung.</summary>
        private static Transform Bone(GameObject body, GameObject asset, string name) =>
            body.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name && !t.IsChildOf(asset.transform));
    }
}
