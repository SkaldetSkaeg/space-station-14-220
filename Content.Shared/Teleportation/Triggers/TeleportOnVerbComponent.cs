using Content.Shared.Whitelist;

namespace Content.Shared.Teleportation.Triggers;

/// <summary>
/// Requests teleportation when an allowed user activates a verb on the teleporter.
/// </summary>
[RegisterComponent]
public sealed partial class TeleportOnVerbComponent : Component
{
    /// <summary>
    /// Teleport behavior requested by this trigger.
    /// </summary>
    [DataField]
    public TeleportMode Mode = TeleportMode.Normal;

    /// <summary>
    /// Text displayed for the teleport verb.
    /// </summary>
    [DataField]
    public LocId VerbText = "portal-component-ghost-traverse";

    /// <summary>
    /// Description displayed while the teleport verb is available.
    /// </summary>
    [DataField]
    public LocId? EnabledMessage;

    /// <summary>
    /// Entities allowed to use the teleport verb.
    /// </summary>
    [DataField]
    public EntityWhitelist? UserWhitelist;

    /// <summary>
    /// Entities prevented from using the teleport verb.
    /// </summary>
    [DataField]
    public EntityWhitelist? UserBlacklist;
}
