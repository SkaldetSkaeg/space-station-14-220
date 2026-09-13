using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Spreader;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Spreader;

public sealed partial class KudzuSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery = default!;
    [Dependency] private EntityQuery<KudzuComponent> _kudzuQuery = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageableQuery = default!;

    private static readonly ProtoId<EdgeSpreaderPrototype> KudzuGroup = "Kudzu";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<KudzuComponent, ComponentStartup>(SetupKudzu);
        SubscribeLocalEvent<KudzuComponent, SpreadNeighborsEvent>(OnKudzuSpread);
        SubscribeLocalEvent<KudzuComponent, DamageDealtEvent>(OnDamageDealt, after: new[] { typeof(DamageableSystem) });
    }

    private void OnDamageDealt(Entity<KudzuComponent> ent, ref DamageDealtEvent args)
    {
        if (!_damageableQuery.TryComp(ent, out var damageable))
            return;

        // Run after the damage model has applied damage or healing, so growth uses the remaining damage.
        var totalDamage = _damageable.GetPositiveDamage((ent.Owner, damageable)).GetTotal();
        var growthDamage = (int) (totalDamage / ent.Comp.GrowthHealth);
        if (growthDamage <= 0)
            return;

        if (!EnsureComp<GrowingKudzuComponent>(ent.Owner, out _))
            ent.Comp.GrowthLevel = ent.Comp.MaxGrowthLevel;

        ent.Comp.GrowthLevel = Math.Max(1, ent.Comp.GrowthLevel - growthDamage);
        if (TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            _appearance.SetData(ent.Owner, KudzuVisuals.GrowthLevel, ent.Comp.GrowthLevel, appearance);
    }

    private void OnKudzuSpread(Entity<KudzuComponent> ent, ref SpreadNeighborsEvent args)
    {
        if (ent.Comp.GrowthLevel < ent.Comp.MaxGrowthLevel)
            return;

        if (args.NeighborFreeTiles.Count == 0)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
            return;
        }

        if (!_robustRandom.Prob(ent.Comp.SpreadChance))
            return;

        var prototype = MetaData(ent).EntityPrototype?.ID;

        if (prototype == null)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
            return;
        }

        foreach (var neighbor in args.NeighborFreeTiles)
        {
            var neighborUid = Spawn(prototype, _map.GridTileToLocal(neighbor.Tile.GridUid, neighbor.Grid, neighbor.Tile.GridIndices));
            DebugTools.Assert(HasComp<EdgeSpreaderComponent>(neighborUid));
            DebugTools.Assert(HasComp<ActiveEdgeSpreaderComponent>(neighborUid));
            DebugTools.Assert(Comp<EdgeSpreaderComponent>(neighborUid).Id == KudzuGroup);
            args.Updates--;
            if (args.Updates <= 0)
                return;
        }
    }

    private void SetupKudzu(Entity<KudzuComponent> ent, ref ComponentStartup args)
    {
        DebugTools.Assert(ent.Comp.MaxGrowthLevel >= 1);
        ent.Comp.MaxGrowthLevel = Math.Max(1, ent.Comp.MaxGrowthLevel);
        ent.Comp.GrowthLevel = Math.Clamp(ent.Comp.GrowthLevel, 1, ent.Comp.MaxGrowthLevel);

        if (!_appearanceQuery.TryComp(ent, out var appearance))
            return;

        _appearance.SetData(ent, KudzuVisuals.Variant, _robustRandom.Next(1, ent.Comp.SpriteVariants), appearance);
        _appearance.SetData(ent, KudzuVisuals.GrowthLevel, ent.Comp.GrowthLevel, appearance);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var kudzuEnumerator = EntityQueryEnumerator<GrowingKudzuComponent>();
        var curTime = _timing.CurTime;

        while (kudzuEnumerator.MoveNext(out var uid, out var grow))
        {
            if (grow.NextTick > curTime)
                continue;

            grow.NextTick = curTime + TimeSpan.FromSeconds(0.5);

            if (!_kudzuQuery.TryComp(uid, out var kudzu))
            {
                RemCompDeferred(uid, grow);
                continue;
            }

            if (!_robustRandom.Prob(kudzu.GrowthTickChance))
                continue;

            if (_damageableQuery.TryComp(uid, out var damage))
            {
                var totalDamage = _damageable.GetPositiveDamage((uid, damage)).GetTotal();
                if (totalDamage > 1.0)
                {
                    if (kudzu.DamageRecovery != null)
                    {
                        // This kudzu features healing, so Gradually heal
                        _damageable.TryChangeDamage(uid, kudzu.DamageRecovery, true);
                    }
                    // Don't grow when quite damaged.
                    if (totalDamage >= kudzu.GrowthBlock && _robustRandom.Prob(0.95f))
                        continue;
                }
            }

            kudzu.GrowthLevel = Math.Min(kudzu.GrowthLevel + 1, kudzu.MaxGrowthLevel);

            if (kudzu.GrowthLevel >= kudzu.MaxGrowthLevel)
            {
                // why cache when you can simply cease to be? Also saves a bit of memory/time.
                RemCompDeferred(uid, grow);
            }

            if (_appearanceQuery.TryComp(uid, out var appearance))
            {
                _appearance.SetData(uid, KudzuVisuals.GrowthLevel, kudzu.GrowthLevel, appearance);
            }
        }
    }
}
