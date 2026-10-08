using EliteCrafting.Items;
using UnityEngine;

namespace EliteCrafting.Tables
{
    /// <summary>
    /// A rune, chisel or gem used at the Rune Table shows and sounds on every client near it (user request 2026-10-07: "a
    /// little vortex like thing colored the color of the rune go from the table up to above the table and make some like
    /// crafting noise ... it should dissipate above the table"): the table's owner counts each use in its ZDO with the
    /// stone's id (<see cref="TableStore.MarkCast"/>, from the payment); each client sees the count grow (looked at ten
    /// times a second, one int), plays the game's workbench craft sound and the vortex (<see cref="TableVortex"/>) in the
    /// stone's colour on the slab. A table loaded with an older count plays nothing. Off on a dedicated server.
    /// </summary>
    internal sealed class TableCast : MonoBehaviour
    {
        private const float Every = 0.1f;

        // The slab's middle on the table top, in the model's metres (ValheimAssets RuneTable: slab x -0.44..0.60, top 1.016).
        private static readonly Vector3 SlabTop = new Vector3(0.08f, 1.02f, 0f);

        private ZNetView? _view;
        private TableStore? _store;
        private Transform? _model;
        private int _seen = -1;
        private float _next;

        private void Awake()
        {
            _view = GetComponent<ZNetView>();
            _store = _view != null ? new TableStore(_view) : null;
            _model = TableShelf.Find(transform, "col_box_table")?.parent;
            enabled = !StoneVisuals.Headless;
        }

        private void Update()
        {
            if (Time.time < _next || _store == null || !_store.Valid)
            {
                return;
            }
            _next = Time.time + Every;
            int casts = _store.Casts;
            if (casts == _seen)
            {
                return;
            }
            bool first = _seen < 0;
            _seen = casts;
            if (!first)
            {
                Play(_store.CastStone);
            }
        }

        private void Play(string stoneId)
        {
            Vector3 at = _model != null ? _model.TransformPoint(SlabTop) : transform.position + Vector3.up;
            GameObject? workbench = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("piece_workbench") : null;
            workbench?.GetComponent<CraftingStation>()?.m_craftItemEffects.Create(at, Quaternion.identity);
            TableVortex.Spawn(at, StoneVisuals.Tint(stoneId));
        }
    }
}
