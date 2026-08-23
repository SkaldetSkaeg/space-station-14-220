// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Teleport.Components;

/// <summary>
///     Requests teleportation when a target collides with a teleporter fixture.
/// </summary>
[RegisterComponent]
public sealed partial class CollisionTeleportTriggerComponent : Component
{
    /// <summary>
    ///     Fixture that triggers teleportation. Any fixture can trigger it when not specified.
    /// </summary>
    [DataField]
    public string? TeleporterFixtureId;

    /// <summary>
    ///     Entities matching this filter are deleted instead of teleported.
    /// </summary>
    [DataField]
    public EntityWhitelist? BlacklistToDelete;

    /// <summary>
    ///     Targets currently touching the teleporter fixture.
    ///     Prevents targets with multiple fixtures from triggering more than once per contact.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> CollidingTargets = [];
}
