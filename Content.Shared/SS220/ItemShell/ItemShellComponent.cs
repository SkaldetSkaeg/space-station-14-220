// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.Shared.SS220.ItemShell;

/// <summary>
/// Persistent shell that contains its item while folded and remains inside it while unfolded.
/// </summary>
[RegisterComponent]
public sealed partial class ItemShellComponent : Component, ISerializationHooks
{
    public const string ContentContainerId = "item-shell-content";

    /// <summary>
    /// Item spawned into this shell by ContainerFill when the map initializes.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<ShellableItemComponent> ItemPrototype;

    /// <summary>
    /// Item paired with this shell, including while unfolded outside its container.
    /// </summary>
    [DataField]
    public EntityUid? LinkedItem;

    /// <summary>
    /// Sound played when the item is unfolded into a hand.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    void ISerializationHooks.AfterDeserialization()
    {
        if (string.IsNullOrWhiteSpace(ItemPrototype.Id))
            throw new InvalidOperationException("ItemShell requires a non-empty itemPrototype.");

        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        if (!prototypes.TryGetMapping(typeof(EntityPrototype), ItemPrototype.Id, out var mapping))
            throw new InvalidOperationException($"ItemShell item prototype '{ItemPrototype}' does not exist.");

        if (mapping.TryGet<ValueDataNode>("abstract", out var abstractNode) && abstractNode.AsBool())
            throw new InvalidOperationException($"ItemShell item prototype '{ItemPrototype}' is abstract.");

        var serialization = IoCManager.Resolve<ISerializationManager>();
        var itemNode = new ValueDataNode(ItemPrototype.Id);
        if (!serialization.ValidateNode<EntProtoId<ShellableItemComponent>>(itemNode).Valid)
            throw new InvalidOperationException($"ItemShell item prototype '{ItemPrototype}' must have ShellableItem.");
    }
}
