// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.CultMiniMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CultMiniMapComponent : Component
{
    /// <summary>
    /// Marker used only for this map's owner. The owner is always shown in a separate section.
    /// </summary>
    [DataField]
    public SpriteSpecifier SelfIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_star.png"));

    [DataField]
    public Color SelfColor = Color.Cyan;

    [DataField]
    public float SelfScale = 1.2f;

    /// <summary>
    /// Components visible on this owner's map. The first matching rule supplies the marker.
    /// These server-side settings are sent to the owner through the UI state.
    /// </summary>
    [DataField]
    public List<CultMiniMapTrackedComponent> TrackedComponents = new()
    {
        new()
        {
            Component = "MiGo",
            Label = "cult-mini-map-migo",
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/SS220/Interface/NavMap/migo.png")),
            Color = Color.Gold,
        },
        new() { Component = "CultYogg", Label = "cult-mini-map-cultist", Color = Color.Violet },
    };

    [ViewVariables]
    public EntProtoId MiniMapAction = "ActionCultMiniMap";

    [ViewVariables, AutoNetworkedField]
    public EntityUid? MiniMapActionEntity;
}

[DataDefinition]
public sealed partial class CultMiniMapTrackedComponent
{
    /// <summary>
    /// Registered component name, without the Component suffix.
    /// </summary>
    [DataField(required: true, customTypeSerializer: typeof(ComponentNameSerializer))]
    public string Component = string.Empty;

    /// <summary>
    /// Optional localization key for the type shown in the list. Defaults to the component name.
    /// </summary>
    [DataField]
    public LocId? Label;

    /// <summary>
    /// PNG texture or RSI state. The map displays its first frame.
    /// </summary>
    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_circle.png"));

    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Positive size multiplier for the map marker. List icons have a fixed size.
    /// </summary>
    [DataField]
    public float Scale = 1f;
}

public sealed partial class CultMiniMapActionEvent : InstantActionEvent
{
}
