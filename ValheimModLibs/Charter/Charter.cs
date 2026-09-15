using System;
using BepInEx.Configuration;
using HarmonyLib;

namespace Charter;

/// <summary>
/// One mod's set of settings that the server can make binding. The server writes the charter; players live
/// under it. Register clauses and the binding entry in Awake, create articles, then have the mod call
/// <see cref="Install"/> once with its Harmony instance.
/// </summary>
public sealed class Charter
{
	private bool lastBound;
	private bool lastSteward;

	/// <param name="guid">The plugin GUID, unique per mod; part of every RPC name.</param>
	/// <param name="title">Shown to players in messages.</param>
	/// <param name="version">This build.</param>
	/// <param name="oldestAccepted">The oldest version of the mod the author still lets in; defaults to <paramref name="version"/>.</param>
	/// <param name="mandatory">Both sides must have the mod. False: a side without it is fine and the mod stays local there.</param>
	public Charter(string guid, string title, string version, string? oldestAccepted = null, bool mandatory = true)
	{
		Guid = guid;
		Title = title;
		Version = version;
		OldestAccepted = oldestAccepted ?? version;
		Mandatory = mandatory;
		Journal = new Journal(title);
		Ledger = new Ledger();
		Receiver = new Receiver(this, Ledger, Journal);
		Courier = new Courier(guid, Journal);
		Publisher = new Publisher(Ledger, Courier, Journal);
		Stewardship = new Stewardship(Courier, Publisher);
		Amendments = new Amendments(this, Ledger, Journal);
		Router = new ChangeRouter(this, Ledger, Journal);
		Tags = new ReadOnlyTags();
		lastSteward = IsSteward;
		Enrol();
	}

	public string Guid { get; }
	public string Title { get; }
	public string Version { get; }
	public string OldestAccepted { get; }
	public bool Mandatory { get; }

	/// <summary>The author's binding is on. On the author: the binding entry's own value.</summary>
	public bool IsBound => Side.IsServer ? Ledger.BindingOn : Receiver.Bound;

	/// <summary>This side's values are the truth right now: the server, or a player under no binding.</summary>
	public bool IsAuthor => Side.IsServer || !Receiver.Bound;

	/// <summary>This player may change clause values right now.</summary>
	public bool MayAmend => !IsBound || IsSteward;

	/// <summary>On the server's admin list, or the host / single player.</summary>
	public bool IsSteward => Side.IsServer || Receiver.Steward;

	/// <summary>Local time of the last push received; null on the author.</summary>
	public DateTime? LastPush => Side.IsServer ? null : Receiver.LastPush;

	/// <summary>Compressed bytes received in the last complete push.</summary>
	public int PushedBytes => Receiver.PushedBytes;

	/// <summary>After a complete push was applied; true on the first one of a connection.</summary>
	public event Action<bool>? Pushed;

	public event Action? StewardChanged;

	public event Action? BindingChanged;

	/// <summary>Process-wide per copy of the library.</summary>
	public static Verbosity Verbosity { get; set; } = Verbosity.Normal;

	internal Journal Journal { get; }
	internal Ledger Ledger { get; }
	internal Receiver Receiver { get; }
	internal Courier Courier { get; }
	internal Publisher Publisher { get; }
	internal Stewardship Stewardship { get; }
	internal Amendments Amendments { get; }
	internal ChangeRouter Router { get; }
	internal ReadOnlyTags Tags { get; }

	/// <summary>Registers an entry. Local entries are never pushed and never bound (per-player display preferences).</summary>
	public Clause<T> Clause<T>(ConfigEntry<T> entry, bool local = false)
	{
		Clause<T> clause = new(entry, local, Ledger);
		Ledger.Add(clause);
		Router.Watch(entry.ConfigFile);
		return clause;
	}

	/// <summary>Registers the master switch. It is always pushed, so a player sees whether the charter binds.</summary>
	public void Binding(ConfigEntry<bool> entry)
	{
		Clause<bool> clause = Clause(entry);
		Ledger.SetBinding(clause, () => entry.Value);
	}

	/// <summary>Installs the game patches once per copy of the library; the facade calls it from Finish.</summary>
	public static void Install(Harmony harmony) => GameHooks.Install(harmony);

	internal void Attach(ZNetPeer peer, bool server)
	{
		if (server)
		{
			peer.m_rpc.Register<ZPackage>($"Charter_{Guid}_Amend", Amendments.OnAmend);
		}
		else
		{
			peer.m_rpc.Register<ZPackage>($"Charter_{Guid}_Push", Receiver.OnPush);
		}
	}

	internal void Tick(float now)
	{
		Publisher.Flush();
		Courier.Tick();
		Stewardship.Tick(now);
		Receiver.Poll();
		NoteChanges();
	}

	internal void AfterPush(bool first)
	{
		NoteChanges();
		try
		{
			Pushed?.Invoke(first);
		}
		catch (Exception e)
		{
			Journal.Error($"a Pushed handler threw: {e}");
		}
	}

	internal void AfterSessionEnd() => NoteChanges();

	private void Enrol()
	{
		GameHooks.Track(this);
		Family.Register(new FamilyEntry(Guid, Title, Version, OldestAccepted, Mandatory));
		Family.SetStatus(Guid, () => Reports.Status(this));
		Family.SetDiff(Guid, () => Reports.Diff(this));
		Family.SetVerbosity(Guid, level => Verbosity = (Verbosity)level);
	}

	private void NoteChanges()
	{
		bool bound = IsBound;
		bool steward = IsSteward;
		Tags.Update(bound && !steward, Ledger.Pushable);
		if (bound != lastBound)
		{
			lastBound = bound;
			Raise(BindingChanged, nameof(BindingChanged));
		}
		if (steward != lastSteward)
		{
			lastSteward = steward;
			Raise(StewardChanged, nameof(StewardChanged));
		}
	}

	private void Raise(Action? handler, string name)
	{
		try
		{
			handler?.Invoke();
		}
		catch (Exception e)
		{
			Journal.Error($"a {name} handler threw: {e}");
		}
	}
}
