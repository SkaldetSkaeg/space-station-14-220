using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Ghost;

/// <summary>
/// Components and their configuration controlled by one ghost HUD checkbox.
/// </summary>
[Prototype]
public sealed partial class GhostHudPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField(required: true)]
    public ComponentRegistry Components { get; private set; } = new();
}
