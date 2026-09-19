using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;

namespace Content.Server.GameTicking;

/// <summary>
/// Records individual GameRule lifecycles throughout the round, independently of open admin windows.
/// Retains starts after entity deletion so schedulers can enforce round-wide limits.
/// </summary>
public sealed partial class GameRuleHistorySystem : EntitySystem
{
    [Dependency] private ServerGameTicker _ticker = default!;
    private readonly Dictionary<EntityUid, GameRuleHistoryEntry> _entries = [];
    private int _sequence;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRuleComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
    }

    private TimeSpan RoundTime => _ticker.RunLevel == GameRunLevel.PreRoundLobby ? TimeSpan.Zero : _ticker.RoundDuration();

    private GameRuleHistoryEntry GetOrAdd(EntityUid uid)
    {
        if (_entries.TryGetValue(uid, out var entry))
            return entry;

        var metadata = MetaData(uid);
        entry = new GameRuleHistoryEntry(++_sequence, GetNetEntity(uid), metadata.EntityPrototype?.ID ?? metadata.EntityName, metadata.EntityName,
            metadata.EntityDescription, null, null, null, GameRuleHistoryStatus.Pending,
            new GameRuleSource(GameRuleSourceKind.Unknown), GameRuleEndReason.Unknown, null);
        _entries.Add(uid, entry);
        return entry;
    }

    /// <summary>
    /// Called by GameTicker before notifying rule systems, which may synchronously start or delete the entity.
    /// </summary>
    public void RecordAdded(EntityUid uid)
    {
        if (!HasComp<GameRuleComponent>(uid))
            return;

        var entry = GetOrAdd(uid);
        _entries[uid] = entry with { AddedAt = RoundTime };
    }

    /// <summary>
    /// Captures an actual start before rule-specific effects can complete or delete the event.
    /// </summary>
    public void RecordStarted(EntityUid uid)
    {
        if (!HasComp<GameRuleComponent>(uid))
            return;

        var entry = GetOrAdd(uid);
        _entries[uid] = entry with
        {
            StartedAt = RoundTime,
            Status = GameRuleHistoryStatus.Active,
        };
    }

    /// <summary>
    /// Captures completion before rule-specific cleanup can delete the event entity.
    /// </summary>
    public void RecordEnded(EntityUid uid, GameRuleEndReason reason, string? endedBy)
    {
        // Shutdown also runs during round cleanup; never recreate cleared history here.
        if (!_entries.TryGetValue(uid, out var entry))
            return;

        if (entry.EndedAt != null)
            return;

        _entries[uid] = entry with
        {
            EndedAt = RoundTime,
            Status = GetEndedStatus(entry.StartedAt != null, reason),
            EndReason = reason,
            EndedBy = endedBy,
        };
    }

    /// <summary>
    /// Attaches the caller's source after spawning, including rules that finish during initialization.
    /// </summary>
    public void RecordSource(EntityUid uid, GameRuleSource? source)
    {
        if (source == null || !_entries.TryGetValue(uid, out var entry))
            return;

        _entries[uid] = entry with { Source = source };
    }

    private void OnTerminating(EntityUid uid, GameRuleComponent component, ref EntityTerminatingEvent args)
    {
        // Do not create entries for unused prototypes or repopulate history during a round reset's entity flush.
        if (!_entries.TryGetValue(uid, out var entry))
            return;

        if (entry.EndedAt != null)
            return;

        _entries[uid] = entry with
        {
            EndedAt = RoundTime,
            Status = GetEndedStatus(entry.StartedAt != null, GameRuleEndReason.EntityDeleted),
            EndReason = GameRuleEndReason.EntityDeleted,
        };
    }

    private static GameRuleHistoryStatus GetEndedStatus(bool started, GameRuleEndReason reason)
    {
        if (!started)
            return GameRuleHistoryStatus.Cancelled;

        if (reason == GameRuleEndReason.Administrator || reason == GameRuleEndReason.ServerConsole || reason == GameRuleEndReason.RulesCleared)
            return GameRuleHistoryStatus.Stopped;

        return GameRuleHistoryStatus.Ended;
    }

    /// <summary>
    /// Counts actual starts even after rule components or entities have been removed.
    /// The last start is measured from the beginning of the round.
    /// </summary>
    public (int Count, TimeSpan LastStart, bool Active) GetStartStatistics(string prototype)
    {
        var count = 0;
        var lastStart = TimeSpan.Zero;
        var active = false;
        foreach (var entry in _entries.Values)
        {
            if (entry.Prototype != prototype || entry.StartedAt == null)
                continue;

            count++;
            if (entry.StartedAt.Value > lastStart)
                lastStart = entry.StartedAt.Value;

            active |= entry.Status == GameRuleHistoryStatus.Active;
        }

        return (count, lastStart, active);
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent args)
    {
        _entries.Clear();
        _sequence = 0;
    }

    /// <summary>
    /// Returns newest-added rules first, with delayed starts reflected in the current snapshot.
    /// </summary>
    public List<GameRuleHistoryEntry> GetHistory()
    {
        return
        [
            .. _entries.Select(pair => pair.Value.Status == GameRuleHistoryStatus.Pending && HasComp<DelayedStartRuleComponent>(pair.Key)
                    ? pair.Value with { Status = GameRuleHistoryStatus.Delayed }
                    : pair.Value)
                .OrderByDescending(entry => entry.Sequence)
        ];
    }
}
