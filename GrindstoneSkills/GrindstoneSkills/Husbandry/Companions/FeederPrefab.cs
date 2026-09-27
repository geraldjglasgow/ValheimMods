using HarmonyLib;
using UnityEngine;

namespace GrindstoneSkills
{
    /// <summary>
    /// The Animal Feeder piece (<see cref="Keys.FeederPrefab"/>): a copy of the game's barrel (piece_chest_barrel) named
    /// "Animal Feeder", a 4 x 2 container in the hammer's Misc tab, built at a workbench for Feeder Recipe
    /// (<see cref="FeederRecipe"/>). Made once per process on every machine, dedicated server included, whether
    /// Husbandry is on or not, so feeders already built always load. The copy is made under an inactive holder kept
    /// with DontDestroyOnLoad, so none of its Awakes run and it never gets a ZDO.
    /// <list type="bullet">
    /// <item>After ZNetScene.Awake (each world load) it is added to the scene's prefab list and to its name table under
    /// the name's stable hash, as the game registers its own prefabs, before any ZDO is turned into an object.</item>
    /// <item>Once both the scene and the item database exist (whichever wakes second), the recipe is applied and the piece
    /// added to the Hammer's build table.</item>
    /// </list>
    /// Everything is idempotent: the item database also wakes in the start scene, where there is no net scene.
    /// </summary>
    public static class FeederPrefab
    {
        public const string SourcePrefab = "piece_chest_barrel";
        public const string WorkbenchPrefab = "piece_workbench";
        public const string HammerItem = "Hammer";
        public const string DisplayName = "Animal Feeder";
        public const string Description = "Hungry tamed animals, and animals being tamed, come to eat the food you put in it.";
        public const int Width = 4;
        public const int Height = 2;

        public static readonly int Hash = Keys.FeederPrefab.GetStableHashCode();

        private static GameObject holder;

        /// <summary>The feeder prefab; null until the net scene first woke (or when the game's barrel is missing).</summary>
        public static GameObject Prefab { get; private set; }

        /// <summary>The prefab's Piece, the one the hammer and the build menu hand around.</summary>
        public static Piece Piece { get; private set; }

        public static bool Is(Piece piece) => piece != null && Piece != null && piece == Piece;

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class SceneAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => HookGuard.Run("animal feeder", Install);
        }

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static class DatabaseAwake
        {
            [HarmonyPostfix]
            private static void Postfix() => HookGuard.Run("animal feeder", Install);
        }

        public static void Install()
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
                return;
            if (Prefab == null)
                Build(scene);
            if (Prefab == null)
                return;
            Register(scene);
            if (ObjectDB.instance == null)
                return;
            FeederRecipe.Apply();
            AddToHammer(ObjectDB.instance);
        }

        private static void Build(ZNetScene scene)
        {
            GameObject barrel = scene.GetPrefab(SourcePrefab);
            if (barrel == null || barrel.GetComponent<Piece>() == null || barrel.GetComponentInChildren<Container>(true) == null)
            {
                GrindstoneSkills.Log.LogWarning($"The game's {SourcePrefab} was not found: there is no Animal Feeder.");
                return;
            }
            GameObject copy = Object.Instantiate(barrel, Holder.transform, false);
            copy.name = Keys.FeederPrefab;
            Configure(copy.GetComponent<Piece>(), copy.GetComponentInChildren<Container>(true), scene);
            Piece = copy.GetComponent<Piece>();
            Prefab = copy;
        }

        private static void Configure(Piece piece, Container container, ZNetScene scene)
        {
            piece.m_name = DisplayName;
            piece.m_description = Description;
            piece.m_category = Piece.PieceCategory.Misc;
            piece.m_enabled = true;
            if (piece.m_craftingStation == null)
                piece.m_craftingStation = Workbench(scene);
            piece.m_resources = FeederRecipe.Parse(HusbandryCompanionSettings.FeederRecipe.Value);
            container.m_name = DisplayName;
            container.m_width = Width;
            container.m_height = Height;
        }

        private static CraftingStation Workbench(ZNetScene scene)
        {
            GameObject workbench = scene.GetPrefab(WorkbenchPrefab);
            return workbench != null ? workbench.GetComponent<CraftingStation>() : null;
        }

        private static void Register(ZNetScene scene)
        {
            if (!scene.m_prefabs.Contains(Prefab))
                scene.m_prefabs.Add(Prefab);
            scene.m_namedPrefabs[Hash] = Prefab;
        }

        private static void AddToHammer(ObjectDB database)
        {
            GameObject hammer = database.GetItemPrefab(HammerItem);
            ItemDrop item = hammer != null ? hammer.GetComponent<ItemDrop>() : null;
            PieceTable table = item != null ? item.m_itemData.m_shared.m_buildPieces : null;
            if (table != null && !table.m_pieces.Contains(Prefab))
                table.m_pieces.Add(Prefab);
        }

        private static GameObject Holder
        {
            get
            {
                if (holder != null)
                    return holder;
                holder = new GameObject("GrindstoneSkills_Prefabs");
                holder.SetActive(false);
                Object.DontDestroyOnLoad(holder);
                return holder;
            }
        }
    }
}
