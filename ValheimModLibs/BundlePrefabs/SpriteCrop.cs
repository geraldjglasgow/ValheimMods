using System;
using UnityEngine;

namespace BundlePrefabs;

/// <summary>
/// A new item icon cut out of part of a game icon at runtime, so no copy of the game's art goes into a bundle (OpenKeep's
/// boots: the bottom of the matching leggings' icon). The sprite's region is read back through the GPU (game textures
/// are not CPU readable), the opaque pixels inside the chosen part are found, and they are scaled into a square the
/// size of the source with a small margin. Clients only: a dedicated server has no graphics device, and any failure
/// returns null, so the caller keeps the source icon.
/// </summary>
public static class SpriteCrop
{
	private const byte Opaque = 16;

	/// <summary>
	/// The opaque pixels of <paramref name="part"/> (fractions of the icon, y from the bottom) fitted into a new square
	/// sprite named <paramref name="name"/>, or null.
	/// </summary>
	public static Sprite? Fit(Sprite? source, Rect part, string name, float margin = 0.06f)
	{
		if (source == null)
		{
			return null;
		}
		try
		{
			Color32[]? pixels = TexturePixels.Read(source.texture, source.textureRect, out int width, out int height);
			if (pixels == null)
			{
				return null;
			}
			RectInt box = OpaqueBox(pixels, width, height, part);
			return box.width < 2 || box.height < 2 ? null : Build(pixels, width, height, box, margin, name, source.pixelsPerUnit);
		}
		catch (Exception e)
		{
			Debug.LogWarning($"BundlePrefabs: the icon {name} could not be cut from {source.name}: {e.Message}");
			return null;
		}
	}

	/// <summary>The bounding box of the opaque pixels inside the part, in pixels; empty when there are none.</summary>
	private static RectInt OpaqueBox(Color32[] pixels, int width, int height, Rect part)
	{
		int x0 = Mathf.Clamp(Mathf.FloorToInt(part.xMin * width), 0, width), x1 = Mathf.Clamp(Mathf.CeilToInt(part.xMax * width), 0, width);
		int y0 = Mathf.Clamp(Mathf.FloorToInt(part.yMin * height), 0, height), y1 = Mathf.Clamp(Mathf.CeilToInt(part.yMax * height), 0, height);
		int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
		for (int y = y0; y < y1; y++)
		{
			for (int x = x0; x < x1; x++)
			{
				if (pixels[y * width + x].a < Opaque)
				{
					continue;
				}
				minX = Math.Min(minX, x);
				maxX = Math.Max(maxX, x);
				minY = Math.Min(minY, y);
				maxY = Math.Max(maxY, y);
			}
		}
		return maxX < 0 ? new RectInt(0, 0, 0, 0) : new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
	}

	private static Sprite Build(Color32[] pixels, int width, int height, RectInt box, float margin, string name, float pixelsPerUnit)
	{
		int size = Math.Max(width, height);
		float room = size * (1f - 2f * margin);
		float scale = Math.Min(room / box.width, room / box.height);
		var offset = new Vector2((size - box.width * scale) * 0.5f, (size - box.height * scale) * 0.5f);
		var output = new Color32[size * size];
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float sourceX = box.x + (x + 0.5f - offset.x) / scale - 0.5f, sourceY = box.y + (y + 0.5f - offset.y) / scale - 0.5f;
				output[y * size + x] = Sample(pixels, width, box, sourceX, sourceY);
			}
		}
		var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
		texture.SetPixels32(output);
		texture.Apply(false, true);
		Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), pixelsPerUnit);
		sprite.name = name;
		return sprite;
	}

	/// <summary>Bilinear inside the box; outside it clear, so nothing of the rest of the icon bleeds in.</summary>
	private static Color32 Sample(Color32[] pixels, int width, RectInt box, float x, float y)
	{
		if (x < box.xMin - 0.5f || y < box.yMin - 0.5f || x > box.xMax - 0.5f || y > box.yMax - 0.5f)
		{
			return new Color32(0, 0, 0, 0);
		}
		int ix = Mathf.Clamp(Mathf.FloorToInt(x), box.xMin, box.xMax - 1), iy = Mathf.Clamp(Mathf.FloorToInt(y), box.yMin, box.yMax - 1);
		int jx = Math.Min(ix + 1, box.xMax - 1), jy = Math.Min(iy + 1, box.yMax - 1);
		float fx = Mathf.Clamp01(x - ix), fy = Mathf.Clamp01(y - iy);
		Color bottom = Color.Lerp(pixels[iy * width + ix], pixels[iy * width + jx], fx);
		Color top = Color.Lerp(pixels[jy * width + ix], pixels[jy * width + jx], fx);
		return Color.Lerp(bottom, top, fy);
	}
}
