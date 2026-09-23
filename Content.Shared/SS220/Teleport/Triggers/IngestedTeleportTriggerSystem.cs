// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Nutrition;

namespace Content.Shared.SS220.Teleport.Triggers;

/// <summary>
/// Requests teleportation after ingestion, independently of the destination provider.
/// </summary>
public sealed partial class IngestedTeleportTriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IngestedTeleportTriggerComponent, IngestedEvent>(OnIngested);
    }

    private void OnIngested(Entity<IngestedTeleportTriggerComponent> ent, ref IngestedEvent args)
    {
        if (args.Split.Volume <= 0)
            return;

        if (EntityManager.IsQueuedForDeletion(ent))
            return;

        var attempt = new TeleportUseAttemptEvent(args.Target, args.User);
        RaiseLocalEvent(ent, ref attempt);
        if (attempt.Cancelled)
            return;

        var request = new TeleportRequestEvent(args.Target, args.User);
        RaiseLocalEvent(ent, ref request);
    }
}
