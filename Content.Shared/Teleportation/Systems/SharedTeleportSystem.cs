using Content.Shared.Ghost.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Weapons.Misc;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Teleportation.Systems;

/// <summary>
/// Moves entities to resolved destination coordinates and raises the common teleport lifecycle events.
/// </summary>
public sealed partial class SharedTeleportSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedJointSystem _joints = default!;

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

        StopSpatialRelationships(target);
        _transform.SetCoordinates(target, Transform(target), destination);

        var targetTeleported = new TargetTeleportedEvent(target);
        RaiseLocalEvent(teleporter, ref targetTeleported);

        var teleported = new TeleportedEvent(teleporter);
        RaiseLocalEvent(target, ref teleported);

        return true;
    }

    /// <summary>
    /// Attempts to teleport a ghost or spectral entity without invoking the regular teleport lifecycle.
    /// Pulling and grappling relationships are still stopped before movement.
    /// </summary>
    public bool TryTeleportGhost(EntityUid target, EntityCoordinates destination)
    {
        if (!HasComp<GhostComponent>(target) && !HasComp<SpectralComponent>(target))
            return false;

        if (TerminatingOrDeleted(target))
            return false;

        if (!destination.IsValid(EntityManager))
            return false;

        StopSpatialRelationships(target);
        _transform.SetCoordinates(target, Transform(target), destination);
        return true;
    }

    private void StopSpatialRelationships(EntityUid target)
    {
        StopPullingRelationships(target);

        // Do not leave a relayed physics joint spanning unrelated coordinates or maps after teleportation.
        _joints.RemoveJoint(target, SharedGrapplingGunSystem.GrapplingJoint);
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
