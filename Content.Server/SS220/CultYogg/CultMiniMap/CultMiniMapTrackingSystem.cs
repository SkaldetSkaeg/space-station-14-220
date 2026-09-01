// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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
using Robust.Shared.Timing;

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
        if (args.UiKey.Equals(CultMiniMapUIKey.Key) && !_ui.IsUiOpen(ent.Owner, CultMiniMapUIKey.Key))
            _ui.SetUiState(ent.Owner, CultMiniMapUIKey.Key, null);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;
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

        var selfScale = float.IsFinite(ent.Comp.SelfScale) && ent.Comp.SelfScale > 0f
            ? ent.Comp.SelfScale
            : 1f;
        var selfMarker = new CultMiniMapMarker(
            CultMiniMapMarker.SelfComponent,
            "cult-mini-map-self-section",
            ent.Comp.SelfIcon,
            ent.Comp.SelfColor,
            selfScale);
        var members = new List<CultMiniMapMember>
        {
            CreateMember(viewer, ent.Owner, selfMarker),
        };
        var seen = new HashSet<EntityUid> { ent.Owner };
        foreach (var rule in ent.Comp.TrackedComponents)
            AddMembers(viewer, rule, members, seen);

        _ui.SetUiState(ent.Owner, CultMiniMapUIKey.Key, new CultMiniMapState(
            GetNetEntity(grid),
            grid == null ? string.Empty : MetaData(grid.Value).EntityName,
            members));
    }

    private void AddMembers(TransformComponent viewer, CultMiniMapTrackedComponent rule,
        List<CultMiniMapMember> members, HashSet<EntityUid> seen)
    {
        // YAML validates component names; tolerate unavailable types in runtime edits as well.
        if (!_componentFactory.TryGetRegistration(rule.Component, out var registration))
            return;

        var scale = float.IsFinite(rule.Scale) && rule.Scale > 0f ? rule.Scale : 1f;
        var marker = new CultMiniMapMarker(rule.Component, rule.Label, rule.Icon, rule.Color, scale);
        var query = EntityManager.AllEntityQueryEnumerator(registration.Type);
        while (query.MoveNext(out var uid, out _))
        {
            if (TerminatingOrDeleted(uid) || !seen.Add(uid))
                continue;

            members.Add(CreateMember(viewer, uid, marker));
        }
    }

    private CultMiniMapMember CreateMember(TransformComponent viewer, EntityUid uid, CultMiniMapMarker marker)
    {
        var xform = Transform(uid);
        var meta = MetaData(uid);
        NetCoordinates? coordinates = null;
        if (viewer.GridUid is { } grid && xform.MapID != MapId.Nullspace && xform.MapID == viewer.MapID)
            coordinates = GetNetCoordinates(_transform.WithEntityId(xform.Coordinates, grid));

        var healthState = TryComp<MobStateComponent>(uid, out var mobState)
            ? mobState.CurrentState
            : MobState.Invalid;
        float? damagePercentage = null;
        if (TryComp<DamageableComponent>(uid, out var damageable)
            && TryComp<MobThresholdsComponent>(uid, out var thresholds)
            && _thresholds.TryGetThresholdForState(uid, MobState.Critical, out var criticalThreshold, thresholds)
            && criticalThreshold.Value > 0)
        {
            damagePercentage = MathF.Max(0f,
                _damageable.GetTotalDamage((uid, damageable)).Float() / criticalThreshold.Value.Float());
        }

        return new CultMiniMapMember(meta.NetEntity, meta.EntityName, marker,
            coordinates, healthState, damagePercentage);
    }
}
