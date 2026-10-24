using System;
using System.Collections.Generic;
using UnityEngine;

namespace BundlePrefabs;

/// <summary>
/// Gives a mod's bundle parts the game's own look at runtime instead of shipping copies of its textures: borrow a
/// material from a game prefab (its textures, shader and settings), then put materials on a model by the names its
/// placeholder materials carry.
/// </summary>
public static class GameMaterials
{
	// The building shader's metal mask and its gloss (Custom/Piece; the creature and item shader's are
	// _MetallicGlossMap and _MetalGloss).
	private const string PieceMetalMap = "_MetallicTex";
	private const string PieceMetalGloss = "_MetallicAlphaGloss";

	private static readonly Dictionary<Texture, Texture2D> GlossyMasks = new Dictionary<Texture, Texture2D>();

	/// <summary>
	/// A copy of the material on the prefab's first mesh renderer (or the named child's), or null. Particle, line and trail
	/// renderers are passed over: most game items keep their sparkle on the root (a ParticleSystemRenderer on
	/// `Legacy Shaders/Particles/Alpha Blended`), and borrowing it made EliteCrafting's runes see-through and unlit.
	/// `anyRenderer` takes the first renderer of any kind (effects borrowing a particle material).
	/// </summary>
	public static Material? Borrow(GameObject? prefab, string? child = null, bool anyRenderer = false)
	{
		if (prefab == null)
		{
			return null;
		}
		Transform root = child == null ? prefab.transform : Find(prefab.transform, child) ?? prefab.transform;
		Renderer? renderer = anyRenderer ? root.GetComponentInChildren<Renderer>(true) : FirstMesh(root);
		return renderer == null || renderer.sharedMaterial == null ? null : new Material(renderer.sharedMaterial);
	}

	private static Renderer? FirstMesh(Transform root)
	{
		foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
		{
			if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
			{
				return renderer;
			}
		}
		return null;
	}

	/// <summary>Replaces each placeholder material (by name) with the one `pick` returns; null keeps the placeholder.</summary>
	public static void Apply(GameObject model, Func<string, Material?> pick)
	{
		foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
		{
			Material[] materials = renderer.sharedMaterials;
			for (int i = 0; i < materials.Length; i++)
			{
				materials[i] = pick(materials[i].name.Replace(" (Instance)", "")) ?? materials[i];
			}
			renderer.sharedMaterials = materials;
		}
	}

	/// <summary>
	/// A copy of a game material (its shader, lighting and settings) wearing a bundle placeholder's own baked textures:
	/// its albedo and, when it has one, its normal map. For a model textured in the workshop that should still be lit
	/// the way the game lights the thing it belongs to.
	/// </summary>
	public static Material Dress(Material game, Material placeholder)
	{
		var material = new Material(game) { name = placeholder.name, color = Color.white };
		material.mainTexture = placeholder.mainTexture;
		material.mainTextureScale = Vector2.one;
		material.mainTextureOffset = Vector2.zero;
		if (material.HasProperty("_BumpMap"))
		{
			material.SetTexture("_BumpMap", placeholder.HasProperty("_BumpMap") ? placeholder.GetTexture("_BumpMap") : null);
		}
		return material;
	}

	/// <summary>
	/// A dressed material (<see cref="Dress"/>) without the game maps it kept that are laid out for the game model's
	/// UVs, not the workshop model's: metal and gloss, glow, style variants. No metal, the given gloss, no glow, no
	/// styles (a material with styles on, such as a painted shield's, draws its emptied style map as plain white). The
	/// game's creature and item shader (<c>Custom/Creature</c>) has all of these; the building shader (<c>Custom/Piece</c>)
	/// keeps its metal in <c>_MetallicTex</c>, cleared too; any a shader lacks are skipped.
	/// </summary>
	public static Material Plain(Material material, float gloss)
	{
		Unstyled(material);
		foreach (string map in new[] { "_MetallicGlossMap", "_EmissionMap" })
		{
			if (material.HasProperty(map))
			{
				material.SetTexture(map, null);
			}
		}
		if (material.HasProperty(PieceMetalMap))
		{
			material.SetTexture(PieceMetalMap, null);
			material.DisableKeyword("_METALLICGLOSSMAP");
		}
		SetFloat(material, "_Metallic", 0f);
		SetFloat(material, "_MetalGloss", 0f);
		SetFloat(material, PieceMetalGloss, 0f);
		SetFloat(material, "_Glossiness", gloss);
		if (material.HasProperty("_EmissionColor"))
		{
			material.SetColor("_EmissionColor", Color.black);
		}
		material.DisableKeyword("_EMISSION");
		return material;
	}

	/// <summary>
	/// A game material with its style variants off (the painted designs a shield's <c>_StyleTex</c> lays over its face),
	/// everything else kept: for a workshop model whose UVs are laid on that game item's own atlas, which wears the
	/// game's albedo, normal map, metal mask and gloss as they are.
	/// </summary>
	public static Material Unstyled(Material material)
	{
		if (material.HasProperty("_StyleTex"))
		{
			material.SetTexture("_StyleTex", null);
		}
		SetFloat(material, "_UseStyles", 0f);
		material.DisableKeyword("_USESTYLES_ON");
		return material;
	}

	/// <summary>
	/// A plain material (<see cref="Plain"/>) given back the shine of metal from the workshop model's own metal mask:
	/// the placeholder's <c>_MetallicGlossMap</c> (red 1 on metal texels, laid out for the model's UVs), metallic on and
	/// <paramref name="metalGloss"/> smoothness on the metal, as the game's metal gear has (the Silver Shield 0.65, the
	/// Flametal Shield 0.77). The creature and item shader takes the mask as it is; the building shader takes it as
	/// <see cref="PieceMetal"/> says. A placeholder without a mask leaves the material matte.
	/// </summary>
	public static Material Metal(Material material, Material placeholder, float metalGloss)
	{
		Texture? mask = placeholder.HasProperty("_MetallicGlossMap") ? placeholder.GetTexture("_MetallicGlossMap") : null;
		if (mask == null)
		{
			return material;
		}
		if (material.HasProperty("_MetallicGlossMap"))
		{
			return ItemMetal(material, mask, metalGloss);
		}
		return material.HasProperty(PieceMetalMap) ? PieceMetal(material, mask, metalGloss) : material;
	}

	private static Material ItemMetal(Material material, Texture mask, float metalGloss)
	{
		material.SetTexture("_MetallicGlossMap", mask);
		material.EnableKeyword("_METALLICGLOSSMAP");
		SetFloat(material, "_Metallic", 1f);
		SetFloat(material, "_MetalGloss", metalGloss);
		SetFloat(material, "_UseGlossmap", 0f);
		return material;
	}

	/// <summary>
	/// Metal on the game's building shader (<c>Custom/Piece</c>), which has no <c>_MetallicGlossMap</c>: it reads metal
	/// from <c>_MetallicTex</c> and the metal's gloss from that map's alpha times <c>_MetallicAlphaGloss</c>, as the
	/// reinforced chest has it (<c>ironchest_m</c>, 0.682; found 2026-10-22: until then no workshop piece's metal shone).
	/// The workshop's mask is opaque everywhere, so a copy is made with its alpha set to its red: only metal texels gloss
	/// and the rest keeps the plain gloss. Nothing on a dedicated server, which draws nothing.
	/// </summary>
	private static Material PieceMetal(Material material, Texture mask, float metalGloss)
	{
		Texture2D? glossy = GlossOnMetal(mask);
		if (glossy == null)
		{
			return material;
		}
		material.SetTexture(PieceMetalMap, glossy);
		material.EnableKeyword("_METALLICGLOSSMAP");
		SetFloat(material, "_Metallic", 1f);
		SetFloat(material, PieceMetalGloss, metalGloss);
		if (material.HasProperty("_MetalColor"))
		{
			material.SetColor("_MetalColor", Color.white);
		}
		return material;
	}

	// One copy per mask: the pieces of a set share their atlas.
	private static Texture2D? GlossOnMetal(Texture mask)
	{
		if (GlossyMasks.TryGetValue(mask, out Texture2D made))
		{
			return made;
		}
		Color32[]? pixels = TexturePixels.ReadLinear(mask, out int width, out int height);
		if (pixels == null)
		{
			return null;
		}
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i].a = pixels[i].r;
		}
		var copy = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
		{
			name = mask.name + "_piece", filterMode = FilterMode.Point, wrapMode = mask.wrapMode,
		};
		copy.SetPixels32(pixels);
		copy.Apply(false, true);
		GlossyMasks[mask] = copy;
		return copy;
	}

	private static void SetFloat(Material material, string name, float value)
	{
		if (material.HasProperty(name))
		{
			material.SetFloat(name, value);
		}
	}

	/// <summary>A transform anywhere below root, by name.</summary>
	public static Transform? Find(Transform root, string name)
	{
		foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
		{
			if (child.name == name)
			{
				return child;
			}
		}
		return null;
	}
}
