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
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PullingSystem _pulling = default!;

    public bool TryTeleport(EntityUid teleporter, EntityUid target, EntityUid user, EntityCoordinates destination)
    {
        if (!Exists(teleporter))
            return false;

        if (!Exists(target))
            return false;

        if (TerminatingOrDeleted(target))
            return false;

        if (!destination.IsValid(EntityManager))
            return false;

        var beforeTeleportEvent = new BeforeTeleportTargetEvent(target, user);
        RaiseLocalEvent(teleporter, ref beforeTeleportEvent);

        if (TryComp(target, out PullableComponent? targetPullable))
            _pulling.TryStopPull(target, targetPullable);

        if (TryComp(target, out PullerComponent? targetPuller) &&
            TryComp(targetPuller.Pulling, out PullableComponent? pulledEntity))
        {
            _pulling.TryStopPull(targetPuller.Pulling.Value, pulledEntity);
        }

        _transform.SetCoordinates(target, Transform(target), destination);

        var targetTeleportedEvent = new TargetTeleportedEvent(target);
        RaiseLocalEvent(teleporter, ref targetTeleportedEvent);

        var afterTeleportedEvent = new AfterTeleportedEvent(teleporter);
        RaiseLocalEvent(target, ref afterTeleportedEvent);

        return true;
    }
}
