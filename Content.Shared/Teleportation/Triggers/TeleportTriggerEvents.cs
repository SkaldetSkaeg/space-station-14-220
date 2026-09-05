using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Triggers;

/// <summary>
/// The requested teleport behavior, independent of the trigger that activated it.
/// </summary>
[Serializable, NetSerializable]
public enum TeleportMode : byte
{
    /// <summary>
    /// Regular teleportation, including lifecycle events and portal collision timeout protection.
    /// </summary>
    Normal,

    /// <summary>
    /// Explicit ghost traversal without the regular lifecycle or portal collision timeout.
    /// </summary>
    Ghost,
}

/// <summary>
/// Passes a target and the user that activated the teleporter to a teleport implementation.
/// Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The entity to teleport.</param>
/// <param name="User">The entity that activated the teleporter.</param>
/// <param name="Mode">The requested teleport behavior.</param>
[ByRefEvent, Serializable]
public record struct TeleportRequestEvent(EntityUid Target, EntityUid User, TeleportMode Mode = TeleportMode.Normal)
{
    /// <summary>
    /// Whether a teleport implementation successfully handled the request.
    /// </summary>
    public bool Handled;
}

/// <summary>
/// Checks whether a teleporter can be used in the requested mode.
/// Raised on the teleporter entity.
/// Handlers must only update the cancellation state because this event can be raised while verbs are being collected.
/// </summary>
/// <param name="Target">The entity that would be teleported.</param>
/// <param name="User">The entity that activates the teleporter.</param>
/// <param name="Cancelled">Whether using the teleporter has been prevented.</param>
/// <param name="CancelReason">Localization key explaining why the teleport is unavailable.</param>
/// <param name="Mode">The requested teleport behavior; must match the subsequent request.</param>
[ByRefEvent, Serializable]
public record struct TeleportUseAttemptEvent(
    EntityUid Target,
    EntityUid User,
    bool Cancelled = false,
    LocId? CancelReason = null,
    TeleportMode Mode = TeleportMode.Normal);

/// <summary>
/// Notifies a teleporter that a target has stopped colliding with its collision trigger.
/// Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The entity that left the trigger.</param>
[ByRefEvent, Serializable]
public record struct TeleportTriggerExitedEvent(EntityUid Target);
