using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hearthhold
{
    /// <summary>
    /// A build piece of Hearthhold's own made as a copy of one of the game's (same model, same networked components),
    /// renamed, with its own name, cost and components (<see cref="Dress"/>). Made once per process on every machine,
    /// dedicated server included, under an inactive holder kept with DontDestroyOnLoad, so none of the copy's Awakes run
    /// and it never gets a ZDO. Each world load it joins the scene's prefab list and name table under its name's stable
    /// hash before any ZDO becomes an object, so pieces already built always load. It sits in the hammer's Misc tab while
    /// <see cref="Buildable"/> says so (<see cref="RefreshHammer"/> after that changes). Installed by
    /// <see cref="ClonedPieces"/>; everything is idempotent.
    /// </summary>
    public sealed class ClonedPiece
    {
        private const string Workbench = "piece_workbench";
        private const string Hammer = "Hammer";

        private static GameObject holder;

        private readonly string source;
        private readonly (string Item, int Amount)[] cost;

        public ClonedPiece(string prefabName, string source, string displayName, string description, params (string, int)[] cost)
        {
            PrefabName = prefabName;
            Hash = prefabName.GetStableHashCode();
            this.source = source;
            DisplayName = displayName;
            Description = description;
            this.cost = cost;
        }

        public string PrefabName { get; }
        public int Hash { get; }
        public string DisplayName { get; }
        public string Description { get; }

        /// <summary>Adds the feature's own components and changes to the fresh copy, once.</summary>
        public Action<GameObject> Dress { get; set; }

        /// <summary>Whether the hammer offers it now; always when unset.</summary>
        public Func<bool> Buildable { get; set; }

        /// <summary>The copy; null until the net scene first woke (or when the game's piece is missing).</summary>
        public GameObject Prefab { get; private set; }

        public bool Is(GameObject instance) => instance != null && Prefab != null && Utils.GetPrefabName(instance) == PrefabName;

        internal void Install()
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
                return;
            if (Prefab == null)
                Build(scene);
            if (Prefab == null)
                return;
            if (!scene.m_prefabs.Contains(Prefab))
                scene.m_prefabs.Add(Prefab);
            scene.m_namedPrefabs[Hash] = Prefab;
            RefreshHammer();
        }

        /// <summary>Puts the piece in the hammer's table, or takes it out, as <see cref="Buildable"/> says now.</summary>
        public void RefreshHammer()
        {
            GameObject hammer = ObjectDB.instance != null && Prefab != null ? ObjectDB.instance.GetItemPrefab(Hammer) : null;
            PieceTable table = hammer != null ? hammer.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces : null;
            if (table == null)
                return;
            bool wanted = Buildable == null || Buildable();
            if (wanted && !table.m_pieces.Contains(Prefab))
                table.m_pieces.Add(Prefab);
            else if (!wanted)
                table.m_pieces.Remove(Prefab);
        }

        private void Build(ZNetScene scene)
        {
            GameObject original = scene.GetPrefab(source);
            if (original == null || original.GetComponent<Piece>() == null)
            {
                Hearthhold.Log.LogWarning($"The game's {source} was not found: there is no {DisplayName}.");
                return;
            }
            GameObject copy = UnityEngine.Object.Instantiate(original, Holder.transform, false);
            copy.name = PrefabName;
            Configure(copy.GetComponent<Piece>(), scene);
            Dress?.Invoke(copy);
            Prefab = copy;
        }

        private void Configure(Piece piece, ZNetScene scene)
        {
            piece.m_name = DisplayName;
            piece.m_description = Description;
            piece.m_category = Piece.PieceCategory.Misc;
            piece.m_enabled = true;
            if (piece.m_craftingStation == null)
                piece.m_craftingStation = scene.GetPrefab(Workbench)?.GetComponent<CraftingStation>();
            piece.m_resources = cost.Select(part => Requirement(scene, part.Item, part.Amount)).Where(r => r.m_resItem != null).ToArray();
        }

        private static Piece.Requirement Requirement(ZNetScene scene, string item, int amount) =>
            new Piece.Requirement { m_resItem = scene.GetPrefab(item)?.GetComponent<ItemDrop>(), m_amount = amount, m_recover = true };

        private static GameObject Holder
        {
            get
            {
                if (holder != null)
                    return holder;
                holder = new GameObject("Hearthhold_Prefabs");
                holder.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(holder);
                return holder;
            }
        }
    }
}
