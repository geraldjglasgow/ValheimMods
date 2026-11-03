using BundlePrefabs;
using PatchGuard;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// A storage crate's lid on every peer: open while someone has the crate open, from the Container on its owner and the
    /// game's own in-use value in the ZDO elsewhere (the owner writes it), eased over 0.4 s about its hinge; settled, it
    /// does nothing. Sends nothing. A placement ghost (no ZDO), a dedicated server or a model without a lid leaves it shut.
    /// </summary>
    public sealed class KwCrateLid : MonoBehaviour
    {
        public const string LidName = "lid";
        private const float LidTime = 0.4f;
        // The lid's opening turn about its hinge: the front edge up and over the back (the workshop's report gives the sign).
        private static readonly Vector3 Open = new Vector3(-100f, 0f, 0f);

        private ZNetView? view;
        private Container? box;
        private Transform? lid;
        private Quaternion rest;
        private float stage;
        private bool broken;

        private void Start()
        {
            view = GetComponent<ZNetView>();
            box = GetComponent<Container>();
            Transform? look = transform.Find(KwCrateModel.LookName);
            lid = look != null ? GameMaterials.Find(look, LidName) : null;
            rest = lid != null ? lid.localRotation : Quaternion.identity;
            if (view == null || view.GetZDO() == null || lid == null || !KwIcons.CanRender)
                enabled = false;
        }

        // A throw is reported once and stops this lid, rather than once a frame.
        private void Update()
        {
            if (broken)
                return;
            broken = true;
            Guard.Run("Broadside crate lid", static l => l.Swing(Time.deltaTime), this);
            broken = false;
        }

        private void Swing(float dt)
        {
            if (!view!.IsValid())
                return;
            bool open = view.IsOwner() ? box != null && box.IsInUse() : view.GetZDO().GetInt(ZDOVars.s_inUse) == 1;
            float goal = open ? 1f : 0f;
            if (Mathf.Approximately(stage, goal))
                return;
            stage = Mathf.MoveTowards(stage, goal, dt / LidTime);
            lid!.localRotation = rest * Quaternion.Euler(Open * Mathf.SmoothStep(0f, 1f, stage));
        }
    }
}
