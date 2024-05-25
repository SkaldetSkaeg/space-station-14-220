// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Mobs;
using Content.Shared.Item;

namespace Content.Shared.SS220.ItemOnDeath;

public sealed class SharedItemOnDeathSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemOnDeathComponent, MobStateChangedEvent>(OnMobStateChanged);
    }
    private void OnMobStateChanged(EntityUid uid, ItemOnDeathComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead && !HasComp<ItemComponent>(uid))
            _entityManager.AddComponent<ItemComponent>(uid);

        if (args.OldMobState == MobState.Dead && HasComp<ItemComponent>(uid))
            _entityManager.RemoveComponent<ItemComponent>(uid);
    }

}
