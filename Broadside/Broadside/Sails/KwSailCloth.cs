using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Broadside
{
    /// <summary>
    /// The three sails a player makes, worse to better as the game goes on: a raw hide sail (the Meadows sail, one even
    /// hide as the raft's sail is, worn through: holed, patched and torn, <see cref="KwSailWear"/>), a linen thread sail
    /// and a scale hide sail. Each is the game's own sail material (the
    /// longship's <c>sail_white</c>: its cloth shader, alpha cut out, both sides drawn) wearing that sail's texture: the
    /// raw hide the game's own hide from the raft's sail, copied at runtime (<see cref="KwSailHide"/>), the scale hide
    /// painted in the asset workshop (<c>ValheimAssets/Assets/Ships/Keelwright</c>) and embedded here; a texture that cannot
    /// be had leaves the linen tinted toward its sail's colour. A better sail draws harder (<see cref="Drive"/>). Each is made of the game's
    /// own item, what a ship built from the hammer pays for it (<see cref="Item"/>): deer hide, linen thread, scale hide.
    /// Saved by type; a sail saved with one of the game's cloth names before the types came wears linen. The rigging is
    /// drawn in the game's rope (<see cref="Rope"/>). New work is offered raw hide only for now (<see cref="Offered"/>):
    /// linen needs the Plains' flax and scale hide the Mistlands, and only Meadows and Black Forest things are offered yet;
    /// sails saved in either still load, draw, drive and cost as before.
    /// </summary>
    public static class KwSailCloth
    {
        /// <summary>The cloth a sail of a type this list does not know wears and costs: linen.</summary>
        public const string Plain = "linen";

        /// <summary>The cloth a new sail or bowsprit's jibs start in: raw hide, the Meadows cloth.</summary>
        public const string Default = "rawhide";

        private const string Linen = "sail_white";

        /// <summary>
        /// The sails, worse to better: what is saved, what the form shows, how hard each draws, the game item it is made of
        /// (its prefab name), its texture and stand-in tint.
        /// </summary>
        private static readonly (string Type, string Label, float Drive, string Item, string Texture, Color Tint)[] Sails =
        {
            ("rawhide", "Raw hide", 0.8f, "DeerHide", KwSailHide.Game, new Color(0.62f, 0.46f, 0.32f)),
            ("linen", "Linen thread", 1f, "LinenThread", "", Color.white),
            ("scalehide", "Scale hide", 1.25f, "ScaleHide", "sail_scale.png", new Color(0.5f, 0.55f, 0.47f)),
        };

        private static readonly Dictionary<string, Material> Made = new Dictionary<string, Material>();

        /// <summary>The game's rope (the longship's mast rope, <c>cart_rope</c>), for rigging; null if the game has none.</summary>
        public static Material? Rope { get; private set; }

        /// <summary>The sail types in the form's order (worse to better), as they are saved.</summary>
        public static List<string> Names => new List<string> { Sails[0].Type, Sails[1].Type, Sails[2].Type };

        /// <summary>
        /// The sail types the windows offer for new work: raw hide only, the one cloth of the Meadows and Black Forest (the
        /// user's words, 2026-10-26: "right now we should only be supporting the Meadows and Black Forest textures and sails
        /// and everything like that"); a sail saved in another keeps it (<see cref="KwClothField"/>).
        /// </summary>
        public static List<string> Offered => new List<string> { Default };

        /// <summary>A new sail's cloth taken from one beside it: that cloth when new work may pick it, else the default.</summary>
        public static string ForNew(string type) => Offered.Contains(type) ? type : Default;

        public static void Find(ZNetScene scene)
        {
            Made.Clear();
            KwSailLooks.Clear();
            Material? linen = FindLinen(scene), raft = KwSailHide.Raft(scene);
            Rope = FindRope(scene);
            if (linen != null)
            {
                foreach (var sail in Sails)
                    Made[sail.Type] = Dress(linen, sail.Type, sail.Texture, sail.Tint, raft);
            }
            Log.Info($"Broadside: sails {string.Join(", ", Made.Keys)} on {(linen != null ? linen.name : "nothing")}, the hide {(raft != null ? raft.name : "not found")}; rope {(Rope != null ? Rope.name : "none")}.");
        }

        // The longship's plain linen sail material.
        private static Material? FindLinen(ZNetScene scene)
        {
            foreach (string ship in new[] { "VikingShip", "Karve" })
            {
                GameObject? prefab = scene.GetPrefab(ship);
                foreach (Renderer renderer in prefab != null ? prefab.GetComponentsInChildren<Renderer>(true) : new Renderer[0])
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null && material.name.StartsWith(Linen))
                            return material;
                    }
                }
            }
            return null;
        }

        // A sail's material: the linen's own, or a copy wearing the sail's texture, the game's hide or an embedded one (or,
        // without it, tinted toward it).
        private static Material Dress(Material linen, string type, string texture, Color tint, Material? raft)
        {
            if (texture.Length == 0)
                return linen;
            Material made = new Material(linen) { name = "broadside_sail_" + type };
            bool dressed = texture == KwSailHide.Game ? raft != null && KwSailHide.Dress(made, raft) : Embedded(made, texture);
            if (!dressed)
                made.color = linen.color * tint;
            return made;
        }

        // A copy dressed in an embedded texture and its normal map; false when the texture is not embedded.
        private static bool Embedded(Material made, string texture)
        {
            Texture2D? painted = Load(texture, false);
            if (painted == null)
                return false;
            // the colour is painted in: no tint over it; the normal map holds its own strength; cloth is matte
            made.mainTexture = painted;
            made.color = Color.white;
            Texture2D? relief = Load(texture.Replace(".png", "_n.png"), true);
            if (relief != null && made.HasProperty("_BumpMap"))
            {
                made.SetTexture("_BumpMap", relief);
                made.SetFloat("_BumpScale", 1f);
            }
            if (made.HasProperty("_Glossiness"))
                made.SetFloat("_Glossiness", 0.1f);
            return true;
        }

        // An embedded texture, point filtered as the game's are (a normal map read as linear); null when it is not embedded.
        private static Texture2D? Load(string file, bool linear)
        {
            using Stream? stream = typeof(KwSailCloth).Assembly.GetManifestResourceStream("Broadside.assets." + file);
            if (stream == null)
                return null;
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            Texture2D? texture = KwSailPixels.Decode(bytes, Path.GetFileNameWithoutExtension(file), linear);
            if (texture == null)
                return null;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        // The rope the longship's ropes are drawn in: a line renderer's material.
        private static Material? FindRope(ZNetScene scene)
        {
            GameObject? ship = scene.GetPrefab("VikingShip");
            foreach (LineRenderer line in ship != null ? ship.GetComponentsInChildren<LineRenderer>(true) : new LineRenderer[0])
            {
                if (line.sharedMaterial != null && line.sharedMaterial.name.ToLowerInvariant().Contains("rope"))
                    return line.sharedMaterial;
            }
            return null;
        }

        public static Material? Of(string type) =>
            Made.TryGetValue(type, out Material material) || Made.TryGetValue(Plain, out material) ? material : KwPrefabs.Planks;

        public static string Label(string type) => Sail(type).Label;

        /// <summary>How hard a sail of this type draws, against linen's.</summary>
        public static float Drive(string type) => Sail(type).Drive;

        /// <summary>The game item (its prefab name) a sail of this type is made of; linen thread for a type this list does not know.</summary>
        public static string Item(string type) => Sail(type).Item;

        private static (string Type, string Label, float Drive, string Item, string Texture, Color Tint) Sail(string type)
        {
            foreach (var sail in Sails)
            {
                if (sail.Type == type)
                    return sail;
            }
            return Sails[1];
        }
    }
}
