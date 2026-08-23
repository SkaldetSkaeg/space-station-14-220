// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Teleport.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Shared.SS220.Teleport.Systems;

public sealed partial class CollisionTeleportTriggerSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CollisionTeleportTriggerComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<CollisionTeleportTriggerComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnStartCollide(Entity<CollisionTeleportTriggerComponent> ent, ref StartCollideEvent args)
    {
        if (ent.Comp.TeleporterFixtureId is { } fixtureId && args.OurFixtureId != fixtureId)
            return;

        if (!args.OtherFixture.Hard)
            return;

        var target = args.OtherEntity;
        if (!ent.Comp.CollidingTargets.Add(target))
            return;

        if (_whitelist.IsWhitelistPass(ent.Comp.DeleteWhitelist, target))
        {
            PredictedQueueDel(target);
            return;
        }

        var attemptEvent = new TeleportUseAttemptEvent(target, target);
        RaiseLocalEvent(ent, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return;

        var teleportEvent = new TeleportTargetEvent(target, target);
        RaiseLocalEvent(ent, ref teleportEvent);
    }

    private void OnEndCollide(Entity<CollisionTeleportTriggerComponent> ent, ref EndCollideEvent args)
    {
        if (ent.Comp.TeleporterFixtureId is { } fixtureId && args.OurFixtureId != fixtureId)
            return;

        ent.Comp.CollidingTargets.Remove(args.OtherEntity);
    }
}
