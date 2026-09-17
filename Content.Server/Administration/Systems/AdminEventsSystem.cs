using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.StationEvents;
using Content.Server.StationEvents.Components;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Systems;

/// <summary>
/// Builds snapshots for the admin event viewer without sampling the round's random event tables.
/// </summary>
public sealed partial class AdminEventsSystem : EntitySystem
{
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private EventManagerSystem _events = default!;
    [Dependency] private EntityTableSystem _tables = default!;
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private StationEventHistorySystem _history = default!;

    /// <summary>
    /// Adds a validated GameRule with the same permission and round-start behavior as addgamerule.
    /// </summary>
    public NetEntity? TryAddRule(ICommonSession player, string id)
    {
        if (!_admins.HasAdminFlag(player, AdminFlags.Admin | AdminFlags.Fun))
            return null;

        if (string.IsNullOrWhiteSpace(id))
            return null;

        if (!ProtoMan.TryIndex<EntityPrototype>(id, out var prototype))
            return null;

        if (prototype.Abstract)
            return null;

        if (!prototype.TryComp<GameRuleComponent>(out _, EntityManager.ComponentFactory))
            return null;

        if (!IsVisibleCategory(GetCategory(prototype)))
            return null;

        var uid = _ticker.AddGameRule(id, new GameRuleSource(GameRuleSourceKind.Administrator, player.Name));
        _adminLog.Add(LogType.EventStarted, $"{player} added game rule {ToPrettyString(uid)} via the events window");
        if (_ticker.RunLevel == GameRunLevel.InRound)
            _ticker.StartGameRule(uid);

        return GetNetEntity(uid);
    }

    /// <summary>
    /// Ends a specific unfinished rule, including a pending or delayed start, with endgamerule permissions.
    /// </summary>
    public bool TryStopRule(ICommonSession player, NetEntity entity)
    {
        if (!_admins.HasAdminFlag(player, AdminFlags.Admin | AdminFlags.Fun))
            return false;

        if (!TryGetEntity(entity, out var uid))
            return false;

        if (!TryComp<GameRuleComponent>(uid, out var rule))
            return false;

        var prototype = MetaData(uid.Value).EntityPrototype;
        if (prototype == null || !IsVisibleCategory(GetCategory(prototype)))
            return false;

        var ruleName = ToPrettyString(uid.Value);
        if (!_ticker.EndGameRule(uid.Value, rule, GameRuleEndReason.Administrator, player.Name))
            return false;

        // EndGameRule prevents a restart but leaves the delayed-start timer behind.
        if (!Deleted(uid.Value))
            RemComp<DelayedStartRuleComponent>(uid.Value);

        _adminLog.Add(LogType.EventStopped, $"{player} stopped game rule {ruleName} via the events window");
        return true;
    }

    /// <summary>
    /// Returns unfinished rules and the possible events in each attached scheduler's table.
    /// This data must only be sent to authorized administrators.
    /// </summary>
    public AdminEventsEuiState GetSnapshot()
    {
        var state = new AdminEventsEuiState { EventsEnabled = _events.EventsEnabled };
        // Pending history entries have a suffix, so they do not match event prototype IDs.
        var occurrences = _ticker.AllPreviousGameRules
            .GroupBy(rule => rule.Item2, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        foreach (var uid in _ticker.GetAddedGameRules())
        {
            var metadata = MetaData(uid);
            var prototype = metadata.EntityPrototype;
            if (prototype == null)
                continue;

            var category = GetCategory(prototype);
            if (!IsVisibleCategory(category))
                continue;

            var status = AdminEventRuleStatus.Pending;
            if (_ticker.IsGameRuleActive(uid))
                status = AdminEventRuleStatus.Active;
            else if (HasComp<DelayedStartRuleComponent>(uid))
                status = AdminEventRuleStatus.Delayed;

            var netEntity = GetNetEntity(uid);
            state.Rules.Add(new AdminEventRuleInfo(netEntity,
                prototype.ID, metadata.EntityName, status, category));

            if (category == AdminGameRuleCategory.Schedulers)
                state.Timers.Add(GetSchedulerTimer(netEntity));

            if (TryComp<BasicStationEventSchedulerComponent>(uid, out var basic))
                state.Tables.Add(GetTable(netEntity, basic.ScheduledGameRules, status, occurrences));

            if (TryComp<RampingStationEventSchedulerComponent>(uid, out var ramping))
                state.Tables.Add(GetTable(netEntity, ramping.ScheduledGameRules, status, occurrences));
        }

        state.Rules = state.Rules.OrderBy(rule => rule.Prototype, StringComparer.Ordinal).ToList();
        state.AvailableRules = _ticker.GetAllGameRulePrototypes()
            .Where(prototype => IsVisibleCategory(GetCategory(prototype)))
            .OrderBy(prototype => prototype.ID, StringComparer.Ordinal)
            .Select(GetPrototypeInfo)
            .ToList();

        state.History = _history.GetHistory();
        return state;
    }

    /// <summary>
    /// Reads the next attempt time without advancing the scheduler or drawing random events.
    /// Must only be sent to authorized administrators.
    /// </summary>
    public AdminSchedulerTimerInfo GetSchedulerTimer(NetEntity scheduler)
    {
        var inactive = new AdminSchedulerTimerInfo(scheduler, null, false);
        if (!TryGetEntity(scheduler, out var uid))
            return inactive;

        if (!TryComp<GameRuleComponent>(uid, out var rule))
            return inactive;

        if (!_ticker.IsGameRuleActive(uid.Value, rule))
            return inactive;

        float? seconds = null;
        if (TryComp<BasicStationEventSchedulerComponent>(uid, out var basic))
            seconds = basic.TimeUntilNextEvent;

        if (TryComp<RampingStationEventSchedulerComponent>(uid, out var ramping))
            seconds = seconds == null ? ramping.TimeUntilNextEvent : Math.Min(seconds.Value, ramping.TimeUntilNextEvent);

        if (seconds == null)
            return inactive;

        return new AdminSchedulerTimerInfo(scheduler, Math.Max(0, seconds.Value), !_events.EventsEnabled);
    }

    private AdminGameRulePrototypeInfo GetPrototypeInfo(EntityPrototype prototype)
    {
        prototype.TryComp<GameRuleComponent>(out var rule, EntityManager.ComponentFactory);
        var delay = rule?.Delay;
        // StartGameRule samples MinMax.Next, which truncates the configured bounds to integer seconds.
        return new AdminGameRulePrototypeInfo(prototype.ID, prototype.Name, prototype.Description,
            GetCategory(prototype), GetEventCategory(prototype),
            delay == null ? null : Math.Max(0, (int) delay.Value.Min),
            delay == null ? null : Math.Max(0, (int) delay.Value.Max));
    }

    private static bool IsVisibleCategory(AdminGameRuleCategory category)
    {
        return category == AdminGameRuleCategory.Schedulers || category == AdminGameRuleCategory.Events;
    }

    private AdminEventTableInfo GetTable(
        NetEntity scheduler,
        EntityTableSelector selector,
        AdminEventRuleStatus status,
        Dictionary<string, int> occurrences)
    {
        // Enumerate eligible paths without drawing random numbers or starting any rules.
        var candidates = _tables.ListSpawns(selector, new EntityTableContext { RespectConditions = true })
            .Where(entry => entry.Item2 > 0)
            .Select(entry => entry.spawn);
        _events.TryBuildLimitedEvents(candidates, out var available);
        var entries = new List<AdminEventTableEntry>();
        // ListSpawns expands nested tables without consuming RNG or evaluating spawn conditions.
        foreach (var id in _tables.ListSpawns(selector).Select(entry => entry.spawn).Distinct())
        {
            if (!ProtoMan.Resolve(id, out var prototype))
                continue;

            if (prototype.Abstract)
                continue;

            if (!prototype.TryComp<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
                continue;

            var availability = AdminEventAvailability.Available;
            if (!_events.EventsEnabled)
                availability = AdminEventAvailability.EventsDisabled;
            else if (status != AdminEventRuleStatus.Active)
                availability = AdminEventAvailability.SchedulerInactive;
            else if (stationEvent.Weight <= 0 || !available.ContainsKey(prototype))
                availability = AdminEventAvailability.ConditionsNotMet;

            entries.Add(new AdminEventTableEntry(
                id.Id,
                prototype.Name,
                prototype.Description,
                stationEvent.Weight,
                stationEvent.MinimumPlayers,
                stationEvent.EarliestStart,
                stationEvent.ReoccurrenceDelay,
                stationEvent.MaxOccurrences,
                stationEvent.OccursDuringRoundEnd,
                occurrences.GetValueOrDefault(id.Id),
                availability));
        }

        var table = selector is NestedSelector nested ? nested.TableId.Id : string.Empty;
        entries.Sort((left, right) => StringComparer.Ordinal.Compare(left.Prototype, right.Prototype));
        return new AdminEventTableInfo(scheduler, table, entries);
    }
}
