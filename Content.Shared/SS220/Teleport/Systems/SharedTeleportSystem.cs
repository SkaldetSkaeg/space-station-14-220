// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Map;

namespace Content.Shared.SS220.Teleport.Systems;

/// <summary>
///     Performs teleportation and raises its common lifecycle events.
/// </summary>
public sealed partial class SharedTeleportSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;

    public bool TryTeleport(EntityUid teleporter, EntityUid target, EntityUid user, EntityCoordinates destination)
    {
        if (!Exists(teleporter) ||
            !Exists(target) ||
            TerminatingOrDeleted(target) ||
            !destination.IsValid(EntityManager))
        {
            return false;
        }

        var before = new BeforeTeleportTargetEvent(target, user);
        RaiseLocalEvent(teleporter, ref before);

        if (TerminatingOrDeleted(target))
            return false;

        if (TryComp(user, out PullerComponent? puller) && TryComp(puller.Pulling, out PullableComponent? pullable))
            _pulling.TryStopPull(puller.Pulling.Value, pullable);

        _transform.SetCoordinates(target, Transform(target), destination);

        var teleporterEvent = new TargetTeleportedEvent(target);
        RaiseLocalEvent(teleporter, ref teleporterEvent);

        var targetEvent = new AfterTeleportedEvent(teleporter);
        RaiseLocalEvent(target, ref targetEvent);

        return true;
    }
}
