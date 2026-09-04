using Content.Shared.Ghost.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Map;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// Moves entities to resolved destination coordinates and raises the common teleport lifecycle events.
/// </summary>
public sealed partial class SharedTeleportSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PullingSystem _pulling = default!;

    /// <summary>
    /// Attempts to move a target to already resolved destination coordinates.
    /// </summary>
    /// <param name="teleporter">The entity performing the teleportation.</param>
    /// <param name="target">The entity to teleport.</param>
    /// <param name="destination">The destination chosen by the teleport implementation.</param>
    public bool TryTeleport(EntityUid teleporter, EntityUid target, EntityCoordinates destination)
    {
        if (!Exists(teleporter))
            return false;

        if (!Exists(target))
            return false;

        if (TerminatingOrDeleted(target))
            return false;

        if (!destination.IsValid(EntityManager))
            return false;

        var beforeTeleport = new BeforeTeleportEvent(target);
        RaiseLocalEvent(teleporter, ref beforeTeleport);

        StopPullingRelationships(target);
        _transform.SetCoordinates(target, Transform(target), destination);

        var targetTeleported = new TargetTeleportedEvent(target);
        RaiseLocalEvent(teleporter, ref targetTeleported);

        var teleported = new TeleportedEvent(teleporter);
        RaiseLocalEvent(target, ref teleported);

        return true;
    }

    /// <summary>
    /// Attempts to teleport an observer without invoking the regular teleport lifecycle.
    /// </summary>
    public bool TryTeleportGhost(EntityUid target, EntityCoordinates destination)
    {
        if (!HasComp<GhostComponent>(target))
            return false;

        if (TerminatingOrDeleted(target))
            return false;

        if (!destination.IsValid(EntityManager))
            return false;

        _transform.SetCoordinates(target, Transform(target), destination);
        return true;
    }

    private void StopPullingRelationships(EntityUid target)
    {
        if (TryComp(target, out PullableComponent? targetPullable))
            _pulling.TryStopPull(target, targetPullable);

        if (!TryComp(target, out PullerComponent? targetPuller))
            return;

        if (targetPuller.Pulling is not { } pulledTarget)
            return;

        if (!TryComp(pulledTarget, out PullableComponent? pulledEntity))
            return;

        _pulling.TryStopPull(pulledTarget, pulledEntity);
    }
}
