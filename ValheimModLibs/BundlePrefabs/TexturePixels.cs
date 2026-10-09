using UnityEngine;

namespace BundlePrefabs;

/// <summary>
/// The pixels of a game texture, read back through the GPU: the game's textures are not CPU readable, so the region
/// is blitted into a temporary render texture and read from there. For a mod's own runtime copies of game art
/// (<see cref="SpriteCrop"/>'s icons, OpenKeep's leg paint without the feet), never shipped in a bundle. Null on a
/// dedicated server (no graphics device); exceptions are the caller's.
/// </summary>
public static class TexturePixels
{
	/// <summary>The whole texture's pixels, bottom row first.</summary>
	public static Color32[]? Read(Texture texture, out int width, out int height) =>
		Read(texture, new Rect(0, 0, texture.width, texture.height), out width, out height);

	/// <summary>
	/// The whole texture's values as stored, bottom row first: for data textures (normal, metal and gloss maps), whose
	/// texels are not colours and must not pass through the sRGB conversion <see cref="Read(Texture, out int, out int)"/> applies.
	/// </summary>
	public static Color32[]? ReadLinear(Texture texture, out int width, out int height) =>
		Read(texture, new Rect(0, 0, texture.width, texture.height), out width, out height, RenderTextureReadWrite.Linear);

	/// <summary>The pixels of a region of the texture (in pixels, y from the bottom), bottom row first.</summary>
	public static Color32[]? Read(Texture texture, Rect region, out int width, out int height) =>
		Read(texture, region, out width, out height, RenderTextureReadWrite.Default);

	private static Color32[]? Read(Texture texture, Rect region, out int width, out int height, RenderTextureReadWrite space)
	{
		width = (int)region.width;
		height = (int)region.height;
		if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || width <= 0 || height <= 0)
		{
			return null;
		}
		RenderTexture previous = RenderTexture.active;
		RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, space);
		try
		{
			Graphics.Blit(texture, target, new Vector2(region.width / texture.width, region.height / texture.height),
				new Vector2(region.x / texture.width, region.y / texture.height));
			RenderTexture.active = target;
			return ReadActive(width, height, space == RenderTextureReadWrite.Linear);
		}
		finally
		{
			RenderTexture.active = previous;
			RenderTexture.ReleaseTemporary(target);
		}
	}

	private static Color32[] ReadActive(int width, int height, bool linear)
	{
		var readable = new Texture2D(width, height, TextureFormat.RGBA32, false, linear);
		try
		{
			readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			readable.Apply(false);
			return readable.GetPixels32();
		}
		finally
		{
			Object.Destroy(readable);
		}
	}
}
