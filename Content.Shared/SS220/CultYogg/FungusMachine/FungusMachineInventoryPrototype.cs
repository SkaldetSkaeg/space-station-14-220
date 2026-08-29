// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Robust.Shared.Localization;

namespace Content.Shared.SS220.CultYogg.FungusMachine;

[Prototype]
public sealed partial class FungusMachineInventoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("cultures")]
    public List<FungusMachineCulturePrototype> Cultures { get; private set; } = [];
}

[DataDefinition]
public sealed partial class FungusMachineCulturePrototype
{
    [DataField(required: true)]
    public EntProtoId Id;

    [DataField(required: true)]
    public EntProtoId ProductId;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;
}
