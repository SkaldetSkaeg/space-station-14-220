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
