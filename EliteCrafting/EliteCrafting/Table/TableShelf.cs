using EliteCrafting.Items;
using EliteCrafting.Rules;
using UnityEngine;

namespace EliteCrafting.Tables
{
    /// <summary>
    /// The Rune Table's back shelf shows what the table holds (user idea 2026-10-07: "expand the back shelf of table, and
    /// only show the runes there that are stored"): one rune tablet standing upright per rune on the model's seven anchors
    /// (<c>shelf_0</c> to <c>shelf_6</c>; the five runes, in <see cref="StoneCatalog.BuiltInIds"/> order, on the middle
    /// ones), shown while the table holds at
    /// least one of that rune. The tablets are copies of the rune items' own dressed models, without colliders. Every
    /// client draws it from the table's replicated ZDO (looked at when its data changes, at most twice a second); nothing
    /// is sent. Switched off on a dedicated server and on a model without the anchors.
    /// </summary>
    internal sealed class TableShelf : MonoBehaviour
    {
        private const float Every = 0.5f;
        private const int Anchors = 7;

        // The runes sit centred on the shelf: rune i on anchor i + First.
        private static readonly int First = System.Math.Max(0, (Anchors - StoneCatalog.BuiltInIds.Length) / 2);

        private readonly Transform?[] _anchors = new Transform?[StoneCatalog.BuiltInIds.Length];
        private readonly GameObject?[] _tablets = new GameObject?[StoneCatalog.BuiltInIds.Length];
        private ZNetView? _view;
        private TableStore? _store;
        private uint _seen = uint.MaxValue;
        private float _next;

        private void Awake()
        {
            _view = GetComponent<ZNetView>();
            _store = _view != null ? new TableStore(_view) : null;
            bool any = false;
            for (int i = 0; i < _anchors.Length; i++)
            {
                _anchors[i] = Find(transform, "shelf_" + (i + First));
                any |= _anchors[i] != null;
            }
            enabled = any && !StoneVisuals.Headless;
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
            for (int i = 0; i < _anchors.Length; i++)
            {
                Show(i, _store.Runes(StoneCatalog.BuiltInIds[i]) > 0);
            }
        }

        private void Show(int index, bool on)
        {
            if (on && _tablets[index] == null && _anchors[index] != null)
            {
                _tablets[index] = Make(StoneCatalog.BuiltInIds[index], _anchors[index]!);
            }
            if (_tablets[index] != null && _tablets[index]!.activeSelf != on)
            {
                _tablets[index]!.SetActive(on);
            }
        }

        // The rune item's dressed tablet mesh, stood up on its bottom edge with its glyph facing the player: the flat
        // tablet's face (+Y) turned to +Z, its back face on the anchor, its lowest point on the shelf.
        private static GameObject? Make(string runeId, Transform anchor)
        {
            MeshFilter? source = TabletMesh(runeId);
            if (source == null)
            {
                return null;
            }
            GameObject tablet = Instantiate(source.gameObject, anchor, false);
            tablet.name = "ecf_shelf_" + runeId;
            foreach (Collider collider in tablet.GetComponentsInChildren<Collider>(true))
            {
                Destroy(collider);
            }
            Bounds bounds = source.sharedMesh.bounds;
            tablet.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tablet.transform.localPosition = new Vector3(-bounds.center.x, bounds.max.z, -bounds.min.y);
            tablet.transform.localScale = Vector3.one;
            return tablet;
        }

        private static MeshFilter? TabletMesh(string runeId)
        {
            GameObject? prefab = StonePrefabs.Get(runeId);
            Transform? model = prefab != null ? prefab.transform.Find("attach/model") : null;
            MeshFilter? mesh = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
            return mesh != null && mesh.sharedMesh != null ? mesh : null;
        }

        internal static Transform? Find(Transform root, string name)
        {
            foreach (Transform part in root.GetComponentsInChildren<Transform>(true))
            {
                if (part.name == name)
                {
                    return part;
                }
            }
            return null;
        }
    }
}
