using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace EliteEquipment.Boots
{
    /// <summary>
    /// The body's leg paint (<c>_LegsTex</c>, where the game paints leggings and their footwear onto the body) for what a
    /// player wears: the split leggings' trousers paint while Separate Boots is on, else the game's own (the leggings' or
    /// the bare legs'), with the worn boots' paint laid over it, so the boots paint the feet whatever leggings are on; and
    /// the leggings' metal and normal maps with the same edits as their paint (<see cref="PaintMaps"/>). The
    /// game sets its own paint when the leggings or the body change; this follows every visuals update and lays its own
    /// over it when what is worn changes or the game's came back. Each mix is made once per client and kept. Nothing on
    /// a dedicated server.
    /// </summary>
    public static class BodyPaint
    {
        private static readonly int LegsTex = Shader.PropertyToID("_LegsTex");
        private static readonly bool headless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        private static readonly ConditionalWeakTable<VisEquipment, Worn> worn = new ConditionalWeakTable<VisEquipment, Worn>();
        private static readonly Dictionary<(object, BootSet), Texture2D> mixed = new Dictionary<(object, BootSet), Texture2D>();
        private static readonly Dictionary<Texture, Paint> read = new Dictionary<Texture, Paint>();

        /// <summary>After the game's visuals update: the leg paint for its leggings and <paramref name="boots"/> (null for none).</summary>
        public static void Update(VisEquipment vis, BootSet boots)
        {
            if (headless)
                return;
            Material body = vis.m_bodyModel.sharedMaterial;
            Worn state = worn.GetOrCreateValue(vis);
            var key = (vis.m_currentLegItemHash, boots, vis.m_currentModelIndex, BootsSettings.On);
            if (body == null || (key.Equals(state.Key) && (state.Texture == null || body.GetTexture(LegsTex) == state.Texture)))
                return;
            state.Key = key;
            state.Texture = Mixed(vis, boots);
            body.SetTexture(LegsTex, state.Texture != null ? state.Texture : GameTexture(vis));
            Maps(vis, body);
        }

        /// <summary>
        /// The split leggings' metal and normal maps with their paint edits (<see cref="PaintMaps"/>), so paint carried down
        /// keeps its look; the game's own back where one of ours is on and no longer fits.
        /// </summary>
        private static void Maps(VisEquipment vis, Material body)
        {
            BootSet legs = BootsSettings.On ? BootSets.ByLegsHash(vis.m_currentLegItemHash) : null;
            foreach (string property in PaintMaps.Properties)
            {
                if (!body.HasProperty(property))
                    continue;
                Texture edited = legs != null ? PaintMaps.For(legs, property) : null;
                Texture current = body.GetTexture(property);
                if (edited != null)
                    body.SetTexture(property, edited);
                else if (current != null && current.name.StartsWith(PaintMaps.Prefix))
                    body.SetTexture(property, PaintMaps.Game(vis.m_currentLegItemHash, property));
            }
        }

        /// <summary>The paint to lay on, or null when the game's own is right (no split leggings, no boots paint).</summary>
        private static Texture2D Mixed(VisEquipment vis, BootSet boots)
        {
            BootSet legs = BootsSettings.On ? BootSets.ByLegsHash(vis.m_currentLegItemHash) : null;
            Paint trousers = legs != null ? PaintCut.Trousers(legs) : null;
            Paint feet = boots != null ? PaintCut.Boots(boots) : null;
            if (trousers == null && feet == null)
                return null;
            object under = trousers != null ? legs : (object)GameTexture(vis);
            if (!mixed.TryGetValue((under, boots), out Texture2D texture))
                mixed[(under, boots)] = texture = Make(Paint.Over(feet, trousers ?? GameRead(under as Texture)), boots, legs);
            return texture;
        }

        private static Texture2D Make(Paint paint, BootSet boots, BootSet legs) =>
            paint?.ToTexture($"EE_LegsPaint_{legs?.Key ?? "game"}_{boots?.Key ?? "bare"}");

        /// <summary>What the game paints for the leggings worn now: their own paint, else the bare legs'.</summary>
        private static Texture GameTexture(VisEquipment vis)
        {
            int hash = vis.m_currentLegItemHash;
            GameObject prefab = hash != 0 && ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(hash) : null;
            Material paint = prefab != null ? prefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_armorMaterial : null;
            return paint != null && paint.HasProperty(LegsTex) ? paint.GetTexture(LegsTex) : vis.m_emptyLegsTexture;
        }

        private static Paint GameRead(Texture texture)
        {
            if (texture == null)
                return null;
            if (!read.TryGetValue(texture, out Paint paint))
                read[texture] = paint = Paint.Read(texture);
            return paint;
        }

        private sealed class Worn
        {
            public (int, BootSet, int, bool) Key { get; set; } = (-1, null, -1, false);

            /// <summary>The paint laid on last; null while the game's own is right.</summary>
            public Texture2D Texture { get; set; }
        }
    }
}
