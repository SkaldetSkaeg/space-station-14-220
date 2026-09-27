// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.ItemShell;

/// <summary>
/// Item that remains unfolded only in a hand or its registered hidden container.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShellableItemComponent : Component
{
    public const string ShellContainerId = "item-shell";

    /// <summary>
    /// Shell to create for an item spawned without a shell.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId ShellPrototype;

    /// <summary>
    /// Persistent shell associated with this item.
    /// Stored inside the item while it is unfolded.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? Shell;
}
