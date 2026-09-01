// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.CultMiniMap;
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.CultYogg.CultMiniMap;

/// <summary>
/// Grants the map to cult members and supplies positions independently of suit sensors and PVS.
/// </summary>
public sealed class CultMiniMapTrackingSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    private TimeSpan _nextUpdate;
    private uint _nextPingId;
    private readonly List<ActivePing> _pings = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextPingByOwner = new();
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggComponent, ComponentInit>(OnCultInit);
        SubscribeLocalEvent<MiGoComponent, ComponentInit>(OnMiGoInit);
        SubscribeLocalEvent<CultYoggComponent, ComponentShutdown>(OnCultShutdown);
        SubscribeLocalEvent<MiGoComponent, ComponentShutdown>(OnMiGoShutdown);
        SubscribeLocalEvent<CultMiniMapComponent, BoundUIOpenedEvent>(OnOpened);
        SubscribeLocalEvent<CultMiniMapComponent, BoundUIClosedEvent>(OnClosed);
        SubscribeLocalEvent<CultMiniMapComponent, CultMiniMapPingMessage>(OnPing);
        SubscribeLocalEvent<CultMiniMapComponent, ComponentRemove>(OnMapRemove);
    }

    private void OnCultInit(Entity<CultYoggComponent> ent, ref ComponentInit args)
    {
        EnsureComp<CultMiniMapComponent>(ent);
    }

    private void OnMiGoInit(Entity<MiGoComponent> ent, ref ComponentInit args)
    {
        EnsureComp<CultMiniMapComponent>(ent);
    }

    private void OnCultShutdown(Entity<CultYoggComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent) && !HasComp<MiGoComponent>(ent))
            RemComp<CultMiniMapComponent>(ent);
    }

    private void OnMiGoShutdown(Entity<MiGoComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent) && !HasComp<CultYoggComponent>(ent))
            RemComp<CultMiniMapComponent>(ent);
    }

    private void OnOpened(Entity<CultMiniMapComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey.Equals(CultMiniMapUIKey.Key))
            UpdateUserInterface(ent);
    }

    private void OnClosed(Entity<CultMiniMapComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(CultMiniMapUIKey.Key))
            return;

        if (_ui.IsUiOpen(ent.Owner, CultMiniMapUIKey.Key))
            return;

        _ui.SetUiState(ent.Owner, CultMiniMapUIKey.Key, null);
    }

    private void OnPing(Entity<CultMiniMapComponent> ent, ref CultMiniMapPingMessage args)
    {
        TryCreatePing(ent, args.Actor, GetCoordinates(args.Coordinates));
    }

    private void OnMapRemove(Entity<CultMiniMapComponent> ent, ref ComponentRemove args)
    {
        _nextPingByOwner.Remove(ent.Owner);
    }

    /// <summary>
    /// Validates and publishes a ping. Public so non-UI callers can use the same authoritative path.
    /// </summary>
    public bool TryCreatePing(Entity<CultMiniMapComponent> ent, EntityUid actor, EntityCoordinates coordinates)
    {
        if (actor != ent.Owner)
            return false;

        if (!_ui.IsUiOpen(ent.Owner, CultMiniMapUIKey.Key))
            return false;

        if (Transform(ent).GridUid is not { } grid)
            return false;

        if (coordinates.EntityId != grid)
            return false;

        if (!coordinates.IsValid(EntityManager))
            return false;

        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return false;

        if (!gridComp.LocalAABB.Enlarged(1f).Contains(coordinates.Position))
            return false;

        var now = _timing.CurTime;
        if (_nextPingByOwner.TryGetValue(ent.Owner, out var nextPing) && nextPing > now)
            return false;

        var cooldown = NonNegativeOrDefault(ent.Comp.PingCooldown, 3f);
        var duration = PositiveOrDefault(ent.Comp.PingDuration, 8f);
        var scale = PositiveOrDefault(ent.Comp.PingScale, 1f);
        _nextPingByOwner[ent.Owner] = now + TimeSpan.FromSeconds(cooldown);

        TrimChannel(ent.Comp.PingChannel, Math.Max(1, ent.Comp.MaxActivePings));
        _pings.Add(new ActivePing(
            NextPingId(),
            grid,
            coordinates.Position,
            ent.Comp.PingChannel,
            ent.Comp.PingIcon,
            ent.Comp.PingColor,
            scale,
            now + TimeSpan.FromSeconds(duration)));

        BroadcastPings(ent.Comp.PingChannel);
        return true;
    }

    private static float NonNegativeOrDefault(float value, float fallback)
    {
        return float.IsFinite(value) && value >= 0f ? value : fallback;
    }

    private static float PositiveOrDefault(float value, float fallback)
    {
        return float.IsFinite(value) && value > 0f ? value : fallback;
    }

    private void TrimChannel(string channel, int maxActive)
    {
        while (_pings.Count(ping => ping.Channel == channel) >= maxActive)
        {
            var oldest = _pings.FindIndex(ping => ping.Channel == channel);
            if (oldest < 0)
                return;

            _pings.RemoveAt(oldest);
        }
    }

    private uint NextPingId()
    {
        do
        {
            _nextPingId++;
        } while (_nextPingId == 0);

        return _nextPingId;
    }

    private void BroadcastPings(string channel)
    {
        var query = EntityQueryEnumerator<CultMiniMapComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.PingChannel != channel)
                continue;

            if (!_ui.IsUiOpen(uid, CultMiniMapUIKey.Key))
                continue;

            UpdateUserInterface((uid, component));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + UpdateInterval;
        _pings.RemoveAll(ping => ping.ExpiresAt <= now || !Exists(ping.Grid));
        var query = EntityQueryEnumerator<CultMiniMapComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_ui.IsUiOpen(uid, CultMiniMapUIKey.Key))
                UpdateUserInterface((uid, comp));
        }
    }

    private void UpdateUserInterface(Entity<CultMiniMapComponent> ent)
    {
        var viewer = Transform(ent);
        var grid = viewer.GridUid;
        if (grid != null)
            EnsureComp<NavMapComponent>(grid.Value);

        var selfMarker = new CultMiniMapMarker(
            CultMiniMapMarker.SelfComponent,
            "cult-mini-map-self-section",
            ent.Comp.SelfIcon,
            ent.Comp.SelfColor,
            PositiveOrDefault(ent.Comp.SelfScale, 1f),
            CultMiniMapMarkerType.Icon,
            true,
            true);
        var members = new List<CultMiniMapMember>
        {
            CreateMember(viewer, ent.Owner, selfMarker),
        };
        var seen = new HashSet<EntityUid> { ent.Owner };
        foreach (var rule in ent.Comp.TrackedComponents)
            AddMembers(viewer, rule, members, seen);

        var pings = GetVisiblePings(ent.Comp.PingChannel, viewer, grid);

        _ui.SetUiState(ent.Owner, CultMiniMapUIKey.Key, new CultMiniMapState(
            GetNetEntity(grid),
            grid == null ? string.Empty : MetaData(grid.Value).EntityName,
            members,
            pings));
    }

    private List<CultMiniMapPing> GetVisiblePings(
        string channel,
        TransformComponent viewer,
        EntityUid? grid)
    {
        var result = new List<CultMiniMapPing>();
        if (grid is not { } gridUid)
            return result;

        foreach (var ping in _pings)
        {
            if (ping.Channel != channel || !Exists(ping.Grid))
                continue;

            var mapCoordinates = _transform.ToMapCoordinates(new EntityCoordinates(ping.Grid, ping.Position));
            if (mapCoordinates.MapId != viewer.MapID)
                continue;

            var coordinates = _transform.ToCoordinates(gridUid, mapCoordinates);
            result.Add(new CultMiniMapPing(
                ping.Id,
                GetNetCoordinates(coordinates),
                ping.Icon,
                ping.Color,
                ping.Scale));
        }

        return result;
    }

    private void AddMembers(TransformComponent viewer, CultMiniMapTrackedComponent rule,
        List<CultMiniMapMember> members, HashSet<EntityUid> seen)
    {
        // YAML validates component names; tolerate unavailable types in runtime edits as well.
        if (!_componentFactory.TryGetRegistration(rule.Component, out var registration))
            return;

        var scale = float.IsFinite(rule.Scale) && rule.Scale > 0f ? rule.Scale : 1f;
        var marker = new CultMiniMapMarker(
            rule.Component,
            rule.Label,
            rule.Icon,
            rule.Color,
            scale,
            rule.MarkerType,
            rule.ShowInList,
            rule.ShowHealth);
        var query = EntityManager.AllEntityQueryEnumerator(registration.Type);
        while (query.MoveNext(out var uid, out _))
        {
            if (TerminatingOrDeleted(uid) || !MatchesPrototype(uid, rule) || !seen.Add(uid))
                continue;

            members.Add(CreateMember(viewer, uid, marker));
        }
    }

    private bool MatchesPrototype(EntityUid uid, CultMiniMapTrackedComponent rule)
    {
        if (rule.Prototypes.Count == 0)
            return true;

        if (MetaData(uid).EntityPrototype?.ID is not { } id)
            return false;

        return rule.Prototypes.Contains(new EntProtoId(id));
    }

    private CultMiniMapMember CreateMember(TransformComponent viewer, EntityUid uid, CultMiniMapMarker marker)
    {
        var xform = Transform(uid);
        var meta = MetaData(uid);
        NetCoordinates? coordinates = null;
        if (viewer.GridUid is { } grid && xform.MapID != MapId.Nullspace && xform.MapID == viewer.MapID)
            coordinates = GetNetCoordinates(_transform.WithEntityId(xform.Coordinates, grid));

        var healthState = marker.ShowHealth ? GetHealthState(uid) : MobState.Invalid;
        var damagePercentage = marker.ShowHealth ? GetDamagePercentage(uid) : null;
        var rotation = viewer.GridUid is { } viewerGrid
            ? _transform.GetWorldRotation(xform) - _transform.GetWorldRotation(viewerGrid)
            : xform.LocalRotation;

        return new CultMiniMapMember(meta.NetEntity, meta.EntityName, marker,
            coordinates, (float) rotation.Theta, healthState, damagePercentage);
    }

    private MobState GetHealthState(EntityUid uid)
    {
        return TryComp<MobStateComponent>(uid, out var mobState)
            ? mobState.CurrentState
            : MobState.Invalid;
    }

    private float? GetDamagePercentage(EntityUid uid)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return null;

        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return null;

        if (!_thresholds.TryGetThresholdForState(uid, MobState.Critical, out var criticalThreshold, thresholds))
            return null;

        if (criticalThreshold.Value <= 0)
            return null;

        // Crew monitoring uses this legacy API for the same percentage; the engine has no numeric replacement yet.
#pragma warning disable CS0618 // DamageableSystem.GetTotalDamage
        var totalDamage = _damageable.GetTotalDamage((uid, damageable)).Float();
#pragma warning restore CS0618
        return MathF.Max(0f, totalDamage / criticalThreshold.Value.Float());
    }

    private sealed record ActivePing(
        uint Id,
        EntityUid Grid,
        Vector2 Position,
        string Channel,
        SpriteSpecifier Icon,
        Color Color,
        float Scale,
        TimeSpan ExpiresAt);
}
