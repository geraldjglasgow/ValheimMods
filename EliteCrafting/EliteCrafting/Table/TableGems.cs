using EliteCrafting.Items;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Tables
{
    /// <summary>
    /// The gems a Rune Table holds, one of each kind lying on its top (user request 2026-10-07: "if you have the freya gem
    /// in table, then you see a pink gem on the table, if there are none, then you see no gem on the table, that for each
    /// gem"): a copy of the gem item's own model, without colliders, at a fixed spot of its own on the free right-hand end
    /// of the top (the slab is in the middle, the bowl at the left end, the rack along the back), turned a little, sized to
    /// fit its spot and resting on the planks. Every client draws it from the table's replicated ZDO, looked at when its
    /// data changes, at most twice a second; nothing is sent. Switched off on a dedicated server and on a model without its
    /// <c>col_box_table</c> collider.
    /// </summary>
    internal sealed class TableGems : MonoBehaviour
    {
        private const float Every = 0.5f;
        private const float Top = 1.0f;
        private const float Widest = 0.16f;

        // Each gem's spot in the model's metres (x, z), in StoneCatalog.GemIds order: three rows (4, 3, 4) between the
        // slab's end (x -0.44) and the table's end (x -1.47), in front of the rack (z -0.33).
        private static readonly Vector2[] Spots =
        {
            new Vector2(-0.64f, 0.38f), new Vector2(-0.87f, 0.38f), new Vector2(-1.10f, 0.38f), new Vector2(-1.33f, 0.38f),
            new Vector2(-0.755f, 0.15f), new Vector2(-0.985f, 0.15f), new Vector2(-1.215f, 0.15f),
            new Vector2(-0.64f, -0.08f), new Vector2(-0.87f, -0.08f), new Vector2(-1.10f, -0.08f), new Vector2(-1.33f, -0.08f),
        };

        private readonly GameObject?[] _gems = new GameObject?[StoneCatalog.GemIds.Length];
        private ZNetView? _view;
        private TableStore? _store;
        private Transform? _model;
        private uint _seen = uint.MaxValue;
        private float _next;

        private void Awake()
        {
            _view = GetComponent<ZNetView>();
            _store = _view != null ? new TableStore(_view) : null;
            _model = TableShelf.Find(transform, "col_box_table")?.parent;
            enabled = _model != null && !StoneVisuals.Headless;
        }

        private void Update()
        {
            if (Time.time < _next || _store == null || !_store.Valid)
            {
                return;
            }
            _next = Time.time + Every;
            uint revision = _view!.GetZDO().DataRevision;
            if (revision == _seen)
            {
                return;
            }
            _seen = revision;
            for (int i = 0; i < _gems.Length && i < Spots.Length; i++)
            {
                Show(i, _store.Runes(StoneCatalog.GemIds[i]) > 0);
            }
        }

        private void Show(int index, bool on)
        {
            if (on && _gems[index] == null)
            {
                _gems[index] = Make(index);
            }
            if (_gems[index] != null && _gems[index]!.activeSelf != on)
            {
                _gems[index]!.SetActive(on);
            }
        }

        private GameObject? Make(int index)
        {
            string id = StoneCatalog.GemIds[index];
            GameObject? prefab = StonePrefabs.Get(id);
            Transform? model = prefab != null ? prefab.transform.Find("attach/model") : null;
            if (model == null)
            {
                return null;
            }
            GameObject gem = Instantiate(model.gameObject, _model!, false);
            gem.name = "ecf_table_" + id;
            foreach (Collider collider in gem.GetComponentsInChildren<Collider>(true))
            {
                Destroy(collider);
            }
            gem.transform.localRotation = Quaternion.Euler(0f, index * 137f % 360f, 0f);
            Settle(gem, Spots[index]);
            return gem;
        }

        // Scaled down to the spot when wider than it, then moved so its middle is over the spot and its bottom on the top.
        private void Settle(GameObject gem, Vector2 spot)
        {
            gem.transform.localPosition = new Vector3(spot.x, Top, spot.y);
            Bounds bounds = WorldBounds(gem);
            float widest = Mathf.Max(bounds.size.x, bounds.size.z);
            if (widest > Widest)
            {
                gem.transform.localScale *= Widest / widest;
                bounds = WorldBounds(gem);
            }
            Vector3 target = _model!.TransformPoint(new Vector3(spot.x, Top, spot.y));
            gem.transform.position += new Vector3(target.x - bounds.center.x, target.y - bounds.min.y, target.z - bounds.center.z);
        }

        private static Bounds WorldBounds(GameObject gem)
        {
            Renderer[] renderers = gem.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(gem.transform.position, Vector3.zero);
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }
    }
}
