using System.Collections.Generic;

namespace Charter;

/// <summary>
/// A push body made ready for the wire once: serialised, compressed above 1 KiB and cut into fragment-sized slices.
/// Every peer of a flush that gets the same body (the same steward flag) shares one parcel; only the fragment headers,
/// which carry the peer's own sequence, are written per peer.
/// </summary>
internal sealed class Parcel
{
	public Parcel(PushBody body)
	{
		Steward = body.Steward;
		Clauses = body.Clauses.Count;
		Articles = body.Articles.Count;
		byte[] bytes = body.ToBytes();
		BodyBytes = bytes.Length;
		if (TooLarge)
		{
			return;
		}
		Compressed = bytes.Length > Fragmenter.CompressAbove;
		byte[] payload = Compressed ? Fragmenter.Compress(bytes) : bytes;
		WireBytes = payload.Length;
		Slices = Fragmenter.Slice(payload);
	}

	public bool Steward { get; }
	public int Clauses { get; }
	public int Articles { get; }

	/// <summary>The serialised body before compression.</summary>
	public int BodyBytes { get; }

	public bool TooLarge => BodyBytes > Fragmenter.LargestBody;
	public bool Compressed { get; }

	/// <summary>The bytes on the wire, compressed or not.</summary>
	public int WireBytes { get; }

	/// <summary>The payload cut into fragment-sized pieces; empty when the body is too large to send.</summary>
	public IReadOnlyList<byte[]> Slices { get; } = new List<byte[]>();
}
