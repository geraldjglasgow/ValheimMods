using System.Collections.Generic;
using System.Linq;

namespace Charter;

/// <summary>
/// The author side of pushes: collects what changed during a frame and turns it into push bodies, a full set
/// for a new peer or when the binding turns on, a delta otherwise.
/// </summary>
internal sealed class Publisher
{
	private const int LargeArticle = 256 * 1024;

	private readonly Ledger ledger;
	private readonly Courier courier;
	private readonly Journal journal;
	private readonly HashSet<IClause> dirtyClauses = new();
	private readonly HashSet<IArticle> dirtyArticles = new();
	private readonly HashSet<string> warnedArticles = new();
	private bool full;

	public Publisher(Ledger ledger, Courier courier, Journal journal)
	{
		this.ledger = ledger;
		this.courier = courier;
		this.journal = journal;
	}

	public void MarkDirty(IClause clause)
	{
		if (clause.Local || !Side.IsServer)
		{
			return;
		}
		dirtyClauses.Add(clause);
		if (clause == ledger.BindingClause && ledger.BindingOn)
		{
			full = true;
		}
	}

	public void MarkDirty(IArticle article)
	{
		if (Side.IsServer)
		{
			dirtyArticles.Add(article);
		}
	}

	/// <summary>The first push to a peer that the game just accepted: everything.</summary>
	public void SendFirst(ZNetPeer peer) => courier.Send(peer, Build(Stewardship.IsSteward(peer), true), "first");

	/// <summary>A push with only the flags, when the peer's steward status changed.</summary>
	public void SendFlags(ZNetPeer peer) => courier.Send(peer, Header(peer), "steward flag");

	/// <summary>Tells one peer why its amendment was rejected, with the clause's current value to settle its entry.</summary>
	public void SendNotice(ZNetPeer peer, IClause? clause, string notice)
	{
		PushBody body = Header(peer);
		body.Notice = notice;
		if (clause != null)
		{
			body.Clauses.Add(Record(clause));
		}
		courier.Send(peer, body, "notice");
	}

	/// <summary>Once per frame: one push with everything that changed since the last one, to every ready peer.</summary>
	public void Flush()
	{
		if (dirtyClauses.Count == 0 && dirtyArticles.Count == 0 && !full)
		{
			return;
		}
		if (Side.IsServer)
		{
			SendToAll(full ? "full" : "delta");
		}
		dirtyClauses.Clear();
		dirtyArticles.Clear();
		full = false;
	}

	/// <summary>
	/// The same push to every ready peer. The body differs only by the peer's steward flag, so it is built, serialised
	/// and compressed at most twice (once per flag) rather than once per peer.
	/// </summary>
	private void SendToAll(string reason)
	{
		Parcel? player = null;
		Parcel? steward = null;
		foreach (ZNetPeer peer in Side.ReadyPeers())
		{
			Parcel parcel = Stewardship.IsSteward(peer)
				? (steward ??= new Parcel(Build(true, full)))
				: (player ??= new Parcel(Build(false, full)));
			courier.Send(peer, parcel, reason);
		}
	}

	private PushBody Header(ZNetPeer peer) => Header(Stewardship.IsSteward(peer));

	private PushBody Header(bool steward) => new() { Bound = ledger.BindingOn, Steward = steward };

	private PushBody Build(bool steward, bool everything)
	{
		PushBody body = Header(steward);
		IEnumerable<IClause> clauses = everything ? ledger.Pushable : dirtyClauses;
		if (!body.Bound)
		{
			clauses = clauses.Where(c => c == ledger.BindingClause);
		}
		foreach (IClause clause in clauses)
		{
			body.Clauses.Add(Record(clause));
		}
		foreach (IArticle article in Travelling(everything ? ledger.Articles : dirtyArticles, body.Bound))
		{
			body.Articles.Add(Record(article));
		}
		return body;
	}

	/// <summary>Standing articles travel always; ordinary ones only while the charter binds.</summary>
	private static IEnumerable<IArticle> Travelling(IEnumerable<IArticle> articles, bool bound)
	{
		return bound ? articles : articles.Where(a => a.Standing);
	}

	private static ClauseRecord Record(IClause clause) => new(clause.Section, clause.Key, clause.CurrentToml);

	private ArticleRecord Record(IArticle article)
	{
		int size = ArticleValues.Size(article.Kind, article.Boxed);
		if (size > LargeArticle && warnedArticles.Add(article.Name))
		{
			journal.Warning($"article '{article.Name}' is about {size} bytes, above 256 KiB");
		}
		return new ArticleRecord(article.Name, article.Kind, article.Boxed);
	}
}
