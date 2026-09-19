using Content.Shared.Eui;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>
/// A read-only snapshot of the round's unfinished rules and their event tables.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminEventsEuiState : EuiStateBase
{
    public bool EventsEnabled;
    public bool CanAddRules;
    public bool CanStopRules;
    public List<AdminEventRuleInfo> Rules = [];
    public List<AdminEventTableInfo> Tables = [];
    public List<AdminGameRulePrototypeInfo> AvailableRules = [];
    public List<AdminEventHistoryEntry> History = [];
    public List<AdminSchedulerTimerInfo> Timers = [];
}

/// <summary>
/// A concrete GameRule prototype offered in the administrator's rule picker.
/// Start delay bounds are configured seconds, not the delay sampled for a historical instance.
/// </summary>
[Serializable, NetSerializable]
public sealed record AdminGameRulePrototypeInfo(
    string Id,
    string Name,
    string Description,
    ProtoId<GameRuleCategoryPrototype> Category,
    bool IsScheduler,
    int? MinimumStartDelaySeconds,
    int? MaximumStartDelaySeconds);

/// <summary>
/// Requests that the server add a new instance of a GameRule to the round.
/// </summary>
[Serializable, NetSerializable]
public sealed class AddAdminGameRuleMessage(string prototype) : EuiMessageBase
{
    public readonly string Prototype = prototype;
}

/// <summary>
/// Acknowledges an add request. Null means the request was rejected.
/// </summary>
[Serializable, NetSerializable]
public sealed class AddAdminGameRuleResultMessage(NetEntity? entity) : EuiMessageBase
{
    public readonly NetEntity? Entity = entity;
}

/// <summary>
/// Requests that the server end one unfinished GameRule instance.
/// </summary>
[Serializable, NetSerializable]
public sealed class StopAdminGameRuleMessage(NetEntity entity) : EuiMessageBase
{
    public readonly NetEntity Entity = entity;
}

/// <summary>
/// Acknowledges whether the selected GameRule was stopped.
/// </summary>
[Serializable, NetSerializable]
public sealed class StopAdminGameRuleResultMessage(bool success) : EuiMessageBase
{
    public readonly bool Success = success;
}

/// <summary>
/// Identifies a current rule instance and its lifecycle status.
/// </summary>
[Serializable, NetSerializable]
public sealed record AdminEventRuleInfo(NetEntity Entity, string Prototype, string Name, AdminEventRuleStatus Status,
    ProtoId<GameRuleCategoryPrototype> Category, bool IsScheduler);

/// <summary>
/// One GameRule instance, retained after cancellation, completion or deletion.
/// Times are elapsed round time. Null means that transition has not been recorded.
/// </summary>
[Serializable, NetSerializable]
public sealed record AdminEventHistoryEntry(
    int Sequence,
    NetEntity Entity,
    string Prototype,
    string Name,
    string Description,
    TimeSpan? AddedAt,
    TimeSpan? StartedAt,
    TimeSpan? EndedAt,
    GameRuleHistoryStatus Status,
    GameRuleSource Source,
    GameRuleEndReason EndReason,
    string? EndedBy);

/// <summary>
/// The lifecycle states shown for unfinished rules.
/// </summary>
[Serializable, NetSerializable]
public enum AdminEventRuleStatus : byte
{
    Pending,
    Delayed,
    Active,
}

/// <summary>
/// Possible station events for a scheduler, independent of current eligibility and selection conditions.
/// An empty table name represents an inline selector.
/// </summary>
[Serializable, NetSerializable]
public sealed record AdminEventTableInfo(NetEntity Scheduler, string Table, List<AdminEventTableEntry> Entries);

/// <summary>
/// A station event's identity, conditions and eligibility at the time the snapshot was built.
/// Time thresholds are in minutes since round start or the previous occurrence respectively.
/// Occurrences counts actual starts across the round, including finished events and other schedulers.
/// </summary>
[Serializable, NetSerializable]
public sealed record AdminEventTableEntry(
    string Prototype,
    string Name,
    string Description,
    float Weight,
    int MinimumPlayers,
    int EarliestStartMinutes,
    int ReoccurrenceDelayMinutes,
    int? MaxOccurrences,
    bool OccursDuringRoundEnd,
    int Occurrences,
    AdminEventAvailability Availability);

/// <summary>
/// Whether the scheduler could select an event using the current snapshot's conditions.
/// The scheduler's countdown and the random draw are not a guarantee of immediate execution.
/// </summary>
[Serializable, NetSerializable]
public enum AdminEventAvailability : byte
{
    Available,
    EventsDisabled,
    SchedulerInactive,
    ConditionsNotMet,
}

/// <summary>
/// Requests a fresh snapshot for an already open event viewer.
/// </summary>
[Serializable, NetSerializable]
public sealed class RefreshAdminEventsMessage : EuiMessageBase;

/// <summary>
/// Server countdown to the scheduler's next selection attempt. Null means it is not running.
/// Paused means automatic events are disabled and the remaining time is frozen.
/// </summary>
[Serializable, NetSerializable]
public sealed record AdminSchedulerTimerInfo(NetEntity Scheduler, float? Seconds, bool Paused);

/// <summary>
/// Requests a lightweight timer update without rebuilding the event tables.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestAdminSchedulerTimerMessage(NetEntity scheduler) : EuiMessageBase
{
    public readonly NetEntity Scheduler = scheduler;
}

/// <summary>
/// Updates a scheduler timer for the administrator viewing it.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminSchedulerTimerMessage(AdminSchedulerTimerInfo timer) : EuiMessageBase
{
    public readonly AdminSchedulerTimerInfo Timer = timer;
}
