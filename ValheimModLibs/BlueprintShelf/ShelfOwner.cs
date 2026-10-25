using System.Collections.Generic;

namespace BlueprintShelf;

/// <summary>
/// One player's part of the shared pool: their character's id and name, the blueprints they shared and their folders
/// (every folder, empty ones too), as paths from the top of their part, sorted.
/// </summary>
internal sealed class ShelfOwner
{
	public long Id;
	public string Name = "";
	public readonly List<string> Paths = new();
	public readonly List<string> Folders = new();
}

/// <summary>
/// The pool as the server sends it (the Listing RPC): every owner with their blueprints and folders, packed, so a few
/// hundred paths stay a few kilobytes.
/// </summary>
internal static class ShelfListing
{
	public static ZPackage Write(List<ShelfOwner> owners)
	{
		ZPackage inner = new ZPackage();
		inner.Write(owners.Count);
		foreach (ShelfOwner owner in owners)
		{
			inner.Write(owner.Id);
			inner.Write(owner.Name);
			WriteList(inner, owner.Paths);
			WriteList(inner, owner.Folders);
		}
		ZPackage outer = new ZPackage();
		outer.Write(ShelfZip.Pack(inner.GetArray()));
		return outer;
	}

	/// <summary>The owners in the package; empty when it cannot be read.</summary>
	public static List<ShelfOwner> Read(ZPackage package, int limit)
	{
		List<ShelfOwner> owners = new List<ShelfOwner>();
		byte[]? data = ShelfZip.Unpack(package.ReadByteArray(), limit);
		if (data == null)
		{
			return owners;
		}
		ZPackage inner = new ZPackage(data);
		int count = inner.ReadInt();
		for (int i = 0; i < count; i++)
		{
			ShelfOwner owner = new ShelfOwner { Id = inner.ReadLong(), Name = inner.ReadString() };
			ReadList(inner, owner.Paths);
			ReadList(inner, owner.Folders);
			owners.Add(owner);
		}
		return owners;
	}

	private static void WriteList(ZPackage package, List<string> list)
	{
		package.Write(list.Count);
		foreach (string item in list)
		{
			package.Write(item);
		}
	}

	private static void ReadList(ZPackage package, List<string> list)
	{
		int count = package.ReadInt();
		for (int i = 0; i < count; i++)
		{
			list.Add(package.ReadString());
		}
	}
}
