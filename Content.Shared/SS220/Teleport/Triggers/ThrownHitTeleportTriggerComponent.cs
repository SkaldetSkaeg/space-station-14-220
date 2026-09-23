// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Teleport.Triggers;

/// <summary>
/// Requests teleportation of entities hit by this item while it is being thrown.
/// </summary>
[RegisterComponent]
public sealed partial class ThrownHitTeleportTriggerComponent : Component
{
    /// <summary>
    /// Entities that can be teleported on impact. Null allows any target other than the thrower.
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetWhitelist;
}
