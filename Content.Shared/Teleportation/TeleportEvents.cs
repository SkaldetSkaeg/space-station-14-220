using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation;

/// <summary>
/// Notifies a teleporter immediately before it moves a target.
/// Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The entity being teleported.</param>
[ByRefEvent, Serializable]
public record struct BeforeTeleportEvent(EntityUid Target);

/// <summary>
/// Passes a target and the user that activated the teleporter to a teleport implementation.
/// Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The entity to teleport.</param>
/// <param name="User">The entity that activated the teleporter.</param>
[ByRefEvent, Serializable]
public record struct TeleportRequestEvent(EntityUid Target, EntityUid User)
{
    /// <summary>
    /// Whether a teleport implementation successfully handled the request.
    /// </summary>
    public bool Handled;
}

/// <summary>
/// Passes an observer to a teleport implementation without invoking the regular teleport lifecycle.
/// Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The observer to teleport.</param>
[ByRefEvent, Serializable]
public record struct GhostTeleportRequestEvent(EntityUid Target)
{
    /// <summary>
    /// Whether a teleport implementation successfully handled the request.
    /// </summary>
    public bool Handled;
}

/// <summary>
/// Notifies a teleporter that it has moved a target.
/// Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The entity that was teleported.</param>
[ByRefEvent, Serializable]
public record struct TargetTeleportedEvent(EntityUid Target);

/// <summary>
/// Notifies a target that it has been teleported.
/// Raised on the teleported entity.
/// </summary>
/// <param name="Teleporter">The entity that performed the teleportation.</param>
[ByRefEvent, Serializable]
public record struct TeleportedEvent(EntityUid Teleporter);

/// <summary>
/// Checks whether a teleporter can be used.
/// Raised on the teleporter entity.
/// </summary>
/// <param name="Target">The entity that would be teleported.</param>
/// <param name="User">The entity that activates the teleporter.</param>
/// <param name="Cancelled">Whether using the teleporter has been prevented.</param>
[ByRefEvent, Serializable]
public record struct TeleportUseAttemptEvent(EntityUid Target, EntityUid User, bool Cancelled = false);
