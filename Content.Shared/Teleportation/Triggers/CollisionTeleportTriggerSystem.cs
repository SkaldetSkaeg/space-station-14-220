using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Teleportation.Triggers;

public sealed partial class CollisionTeleportTriggerSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CollisionTeleportTriggerComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<CollisionTeleportTriggerComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnStartCollide(Entity<CollisionTeleportTriggerComponent> ent, ref StartCollideEvent args)
    {
        if (!IsTriggerCollision(ent.Comp, args.OurFixtureId, args.OtherFixtureId, args.OtherFixture))
            return;

        var target = args.OtherEntity;

        if (Transform(target).Anchored)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.TargetWhitelist, target))
            return;

        if (_whitelist.IsWhitelistPass(ent.Comp.TargetBlacklist, target))
            return;

        var attempt = new TeleportUseAttemptEvent(target, target);
        RaiseLocalEvent(ent, ref attempt);

        if (attempt.Cancelled)
            return;

        var request = new TeleportRequestEvent(target, target);
        RaiseLocalEvent(ent, ref request);

        if (request.Handled)
            return;

        if (_net.IsClient)
            return;

        Log.Error($"CollisionTeleportTrigger on {ToPrettyString(ent)} couldn't teleport " +
                  $"{ToPrettyString(target)} because no teleport implementation handled the request");
    }

    private void OnEndCollide(Entity<CollisionTeleportTriggerComponent> ent, ref EndCollideEvent args)
    {
        if (!IsTriggerCollision(ent.Comp, args.OurFixtureId, args.OtherFixtureId, args.OtherFixture))
            return;

        var exited = new TeleportTriggerExitedEvent(args.OtherEntity);
        RaiseLocalEvent(ent, ref exited);
    }

    private static bool IsTriggerCollision(
        CollisionTeleportTriggerComponent component,
        string teleporterFixtureId,
        string targetFixtureId,
        Fixture targetFixture)
    {
        if (component.TriggerFixtureId is { } triggerFixtureId && teleporterFixtureId != triggerFixtureId)
            return false;

        if (!targetFixture.Hard && !component.AllowedNonHardTargetFixtureIds.Contains(targetFixtureId))
            return false;

        return true;
    }
}
