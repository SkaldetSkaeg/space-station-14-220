// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Teleport.Systems;
using Robust.Shared.Serialization;

namespace Content.Server.SS220.Teleport.Components;

/// <summary>
/// Handles teleport requests by choosing a random unblocked floor tile near the target.
/// </summary>
[RegisterComponent, Access(typeof(RandomTeleportInRadiusSystem))]
public sealed partial class RandomTeleportInRadiusComponent : Component, ISerializationHooks
{
    /// <summary>
    /// Maximum distance in metres before modifiers are applied. Must be finite and greater than zero.
    /// </summary>
    [DataField(required: true)]
    public float Radius;

    /// <summary>
    /// Prevents re-entrant requests during radius queries and teleport lifecycle events.
    /// </summary>
    public bool Teleporting;

    void ISerializationHooks.AfterDeserialization()
    {
        if (!float.IsFinite(Radius) || Radius <= 0)
            throw new ArgumentOutOfRangeException(nameof(Radius), Radius,
                "RandomTeleportInRadiusComponent radius must be finite and greater than zero.");
    }
}
