using System;
using UnityEngine;

namespace BundlePrefabs;

/// <summary>
/// Gives a mod's bundle parts the game's own look at runtime instead of shipping copies of its textures: borrow a
/// material from a game prefab (its textures, shader and settings), then put materials on a model by the names its
/// placeholder materials carry.
/// </summary>
public static class GameMaterials
{
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
	/// game's creature and item shader (<c>Custom/Creature</c>) has all of these; any it lacks are skipped.
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
		SetFloat(material, "_Metallic", 0f);
		SetFloat(material, "_MetalGloss", 0f);
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
	/// Flametal Shield 0.77). A placeholder without a mask leaves the material matte.
	/// </summary>
	public static Material Metal(Material material, Material placeholder, float metalGloss)
	{
		Texture? mask = placeholder.HasProperty("_MetallicGlossMap") ? placeholder.GetTexture("_MetallicGlossMap") : null;
		if (mask == null || !material.HasProperty("_MetallicGlossMap"))
		{
			return material;
		}
		material.SetTexture("_MetallicGlossMap", mask);
		material.EnableKeyword("_METALLICGLOSSMAP");
		SetFloat(material, "_Metallic", 1f);
		SetFloat(material, "_MetalGloss", metalGloss);
		SetFloat(material, "_UseGlossmap", 0f);
		return material;
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
