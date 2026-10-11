using BundlePrefabs;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteCreaturesReborn.Raids
{
    /// <summary>
    /// Opens and closes the Raiders Chest's lid, horn and all, on every client: open while the game's own in-use flag says
    /// someone has the chest open (the container's state on its owner, the ZDO's <c>InUse</c> elsewhere), so everybody
    /// nearby sees it. The lid swings on its hinge with an ease in and out, as Mímir's Chest's does. Nothing is sent; it
    /// reads only what the game already replicates. During a raid the chest cannot be opened, so the lid stays shut.
    /// Switched off on a dedicated server (nothing to draw) and on the placement ghost.
    /// </summary>
    internal sealed class ChestLid : MonoBehaviour
    {
        /// <summary>The lid's turn about its local X axis fully open, in degrees (the model's hinge, its BRIEF.md).</summary>
        public const float OpenAngle = -100f;

        private const float Seconds = 0.55f;

        private ZNetView? _view;
        private Container? _container;
        private Transform? _lid;
        private Quaternion _closed;
        private float _open;

        private void Awake()
        {
            _view = GetComponent<ZNetView>();
            _container = GetComponent<Container>();
            _lid = GameMaterials.Find(transform, ChestModel.LidPart);
            if (_lid == null || _container == null || _view == null || !_view.IsValid()
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                enabled = false;
                return;
            }
            _closed = _lid.localRotation;
        }

        private void Update()
        {
            if (_view == null || !_view.IsValid())
            {
                return;
            }
            float target = IsOpen() ? 1f : 0f;
            if (_open == target)
            {
                return; // settled: one ZDO read a frame
            }
            _open = Mathf.MoveTowards(_open, target, Time.deltaTime / Seconds);
            _lid!.localRotation = _closed * Quaternion.Euler(OpenAngle * Mathf.SmoothStep(0f, 1f, _open), 0f, 0f);
        }

        private bool IsOpen() => _container!.IsInUse() || _view!.GetZDO().GetInt(ZDOVars.s_inUse) == 1;
    }
}
