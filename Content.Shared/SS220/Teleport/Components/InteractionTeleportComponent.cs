// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Teleport.Components;

/// <summary>
///     Used when you need to teleport not through contact, but through DragDrop or Verb
/// </summary>
[RegisterComponent]
public sealed partial class InteractionTeleportComponent : Component
{
    /// <summary>
    ///     Which entities can be teleported
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetWhitelist;

    /// <summary>
    ///     Which entities can't be teleported
    /// </summary>
    [DataField]
    public EntityWhitelist? TargetBlacklist;

    /// <summary>
    ///     Message when whitelisting is rejected
    /// </summary>
    [DataField]
    public LocId? WhitelistRejectedLoc;

    /// <summary>
    ///     How long we are entering teleport
    ///     Null if DoAfter shouldn't happen
    /// </summary>
    [DataField]
    public TimeSpan? TeleportDoAfterTime;

    /// <summary>
    ///     The amount of damage required to interrupt a DoAfter of the teleport
    /// </summary>
    [DataField]
    public FixedPoint2? DamageThreshold;
}
