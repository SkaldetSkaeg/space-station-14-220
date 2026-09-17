using System.Linq;
using Content.Server.GameTicking;
using Content.Server.StationEvents.Components;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;

namespace Content.Server.StationEvents;

/// <summary>
/// Records individual station event lifecycles throughout the round, independently of open admin windows.
/// Records stay server-side and are exposed only through the authorized admin EUI.
/// </summary>
public sealed partial class StationEventHistorySystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;
    private readonly Dictionary<EntityUid, AdminEventHistoryEntry> _entries = new();
    private int _sequence;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationEventComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundCleanup);
    }

    private TimeSpan RoundTime => _ticker.RunLevel == GameRunLevel.PreRoundLobby ? TimeSpan.Zero : _ticker.RoundDuration();

    private AdminEventHistoryEntry GetOrAdd(EntityUid uid, string prototype)
    {
        if (_entries.TryGetValue(uid, out var entry))
            return entry;

        var metadata = MetaData(uid);
        entry = new AdminEventHistoryEntry(++_sequence, GetNetEntity(uid), prototype, metadata.EntityName,
            metadata.EntityDescription, null, null, null, AdminEventHistoryStatus.Pending,
            new GameRuleSource(GameRuleSourceKind.Unknown), GameRuleEndReason.Unknown, null);
        _entries.Add(uid, entry);
        return entry;
    }

    /// <summary>
    /// Called by GameTicker before notifying rule systems, which may synchronously start or delete the entity.
    /// </summary>
    public void RecordAdded(EntityUid uid, GameRuleAddedEvent args)
    {
        if (!HasComp<StationEventComponent>(uid))
            return;

        var entry = GetOrAdd(uid, args.RuleId);
        _entries[uid] = entry with { AddedAt = RoundTime, Source = args.Source ?? entry.Source };
    }

    /// <summary>
    /// Captures an actual start before rule-specific effects can complete or delete the event.
    /// </summary>
    public void RecordStarted(EntityUid uid, GameRuleStartedEvent args)
    {
        if (!HasComp<StationEventComponent>(uid))
            return;

        var entry = GetOrAdd(uid, args.RuleId);
        _entries[uid] = entry with
        {
            StartedAt = RoundTime,
            Status = AdminEventHistoryStatus.Active,
        };
    }

    /// <summary>
    /// Captures completion before rule-specific cleanup can delete the event entity.
    /// </summary>
    public void RecordEnded(EntityUid uid, GameRuleEndedEvent args)
    {
        if (!HasComp<StationEventComponent>(uid))
            return;

        var entry = GetOrAdd(uid, args.RuleId);
        _entries[uid] = entry with
        {
            EndedAt = RoundTime,
            Status = GetEndedStatus(entry.StartedAt != null, args.Reason),
            EndReason = args.Reason,
            EndedBy = args.EndedBy,
        };
    }

    private void OnTerminating(EntityUid uid, StationEventComponent component, ref EntityTerminatingEvent args)
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

    private static AdminEventHistoryStatus GetEndedStatus(bool started, GameRuleEndReason reason)
    {
        if (!started)
            return AdminEventHistoryStatus.Cancelled;

        if (reason == GameRuleEndReason.Administrator || reason == GameRuleEndReason.ServerConsole || reason == GameRuleEndReason.RulesCleared)
            return AdminEventHistoryStatus.Stopped;

        return AdminEventHistoryStatus.Ended;
    }

    private void OnRoundCleanup(RoundRestartCleanupEvent args)
    {
        _entries.Clear();
        _sequence = 0;
    }

    /// <summary>
    /// Returns newest-added events first, with delayed starts reflected in the current snapshot.
    /// Only authorized administrators should receive this data.
    /// </summary>
    public List<AdminEventHistoryEntry> GetHistory()
    {
        return _entries.Select(pair => pair.Value.Status == AdminEventHistoryStatus.Pending && HasComp<DelayedStartRuleComponent>(pair.Key)
                ? pair.Value with { Status = AdminEventHistoryStatus.Delayed }
                : pair.Value)
            .OrderByDescending(entry => entry.Sequence)
            .ToList();
    }
}
