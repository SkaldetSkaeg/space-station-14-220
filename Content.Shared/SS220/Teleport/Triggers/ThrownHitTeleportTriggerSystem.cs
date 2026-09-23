// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Systems;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Shared.SS220.Teleport.Triggers;

/// <summary>
/// Requests teleportation on thrown impacts before the item can be destroyed by impact damage.
/// </summary>
public sealed partial class ThrownHitTeleportTriggerSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThrownHitTeleportTriggerComponent, StartCollideEvent>(OnStartCollide,
            before: [typeof(DamageOnHighSpeedImpactSystem)]);
    }

    private void OnStartCollide(Entity<ThrownHitTeleportTriggerComponent> ent, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard)
            return;

        if (EntityManager.IsQueuedForDeletion(ent))
            return;

        if (!TryComp<ThrownItemComponent>(ent, out var thrown))
            return;

        if (thrown.Landed)
            return;

        if (args.OtherEntity == thrown.Thrower)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.TargetWhitelist, args.OtherEntity))
            return;

        var user = thrown.Thrower ?? args.OtherEntity;
        var attempt = new TeleportUseAttemptEvent(args.OtherEntity, user);
        RaiseLocalEvent(ent, ref attempt);
        if (attempt.Cancelled)
            return;

        var request = new TeleportRequestEvent(args.OtherEntity, user);
        RaiseLocalEvent(ent, ref request);
    }
}
