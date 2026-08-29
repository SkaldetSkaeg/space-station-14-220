// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.CultYogg.FungusMachine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FungusMachineComponent : Component
{
    public const string ContainerId = "FungusMachine";

    /// <summary>
    /// PrototypeID for the Fungus machine's inventory, see <see cref="FungusMachineInventoryPrototype"/>
    /// </summary>
    [DataField("pack", customTypeSerializer: typeof(PrototypeIdSerializer<FungusMachineInventoryPrototype>), required: true)]
    public string PackPrototypeId = string.Empty;

    [ViewVariables]
    public Dictionary<string, FungusMachineInventoryEntry> Inventory = new();

    [DataField]
    [AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    ///     Container of unique entities stored inside this Fungus machine.
    /// </summary>
    [ViewVariables] public Container Container = default!;

    /// <summary>
    /// Whitelist of entities that can use the fungus machine
    /// </summary>
    [DataField]
    public EntityWhitelist UsersWhitelist = new()
    {
        Components =
        [
            "MiGo",
            "CultYogg"
        ],
    };
}

[Serializable, NetSerializable]
public sealed class FungusMachineInventoryEntry
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string Id;
    public string ProductId;
    public LocId Name;
    public LocId Description;
    public int GrowthStages;
    public int Yield;
    public int MaturationCycles;
    public int ProductionCycles;
    public int FirstHarvestSeconds;
    public int RepeatHarvestSeconds;
    public bool HarvestRepeats;

    public FungusMachineInventoryEntry(string id, string productId, LocId name, LocId description)
    {
        Id = id;
        ProductId = productId;
        Name = name;
        Description = description;
    }
}

[Serializable, NetSerializable]
public enum FungusMachineVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum FungusMachineVisualState : byte
{
    Empty,
    Growing,
    Grown,
}
