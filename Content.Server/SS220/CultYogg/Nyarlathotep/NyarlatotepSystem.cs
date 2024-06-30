using Content.Server.Beam;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Content.Server.SS220.CultYogg.Nyarlathotep.Components;
using Content.Shared.SS220.CultYogg;

namespace Content.Server.SS220.CultYogg.Nyarlathotep;

public sealed class NyarlathotepSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    /// <summary>
    /// Adds a component to pursue targets
    /// Performs a duplicate component check, on the MiGi component to not harass cult members
    /// and cuts off entities that are not alive
    /// </summary>
    public void SearchNearNyarlathotep(EntityUid user, float range)
    {
        foreach (var target in _entityLookupSystem.GetComponentsInRange<MobStateComponent>(_transform.GetMapCoordinates(user), range))
        {
            if(!HasComp<MiGoComponent>(target.Owner) && !HasComp<NyarlathotepTargetComponent>(target.Owner) && _mobStateSystem.IsAlive(target.Owner))
            {
                EntityManager.AddComponent(target.Owner, new NyarlathotepTargetComponent());
            }
        }
    }
}
