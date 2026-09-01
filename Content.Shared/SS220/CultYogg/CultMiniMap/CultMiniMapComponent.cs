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
    /// Only map owners with the same channel receive each other's pings.
    /// </summary>
    [DataField]
    public string PingChannel = "cult-yogg";

    [DataField]
    public SpriteSpecifier PingIcon =
        new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_circle.png"));

    [DataField]
    public Color PingColor = Color.DeepSkyBlue;

    [DataField]
    public float PingScale = 1.2f;

    [DataField]
    public float PingDuration = 8f;

    [DataField]
    public float PingCooldown = 3f;

    [DataField]
    public int MaxActivePings = 8;

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
        new()
        {
            Component = "CultYoggBuilding",
            Label = "cult-mini-map-wall",
            Prototypes = new() { "WallCultYogg" },
            MarkerType = CultMiniMapMarkerType.Wall,
            Color = Color.Red,
            ShowInList = false,
            ShowHealth = false,
        },
        new()
        {
            Component = "CultYoggBuilding",
            Label = "cult-mini-map-secret-door",
            Prototypes = new() { "CultYoggDoor" },
            MarkerType = CultMiniMapMarkerType.SecretDoor,
            Color = Color.Red,
            ShowInList = false,
            ShowHealth = false,
        },
        new()
        {
            Component = "CultYoggBuilding",
            Label = "cult-mini-map-airlock",
            Prototypes = new() { "CultYoggAirlock" },
            MarkerType = CultMiniMapMarkerType.Airlock,
            Color = Color.Red,
            ShowInList = false,
            ShowHealth = false,
        },
        BuildingIconRule("CultYoggPod", "cult-mini-map-pod",
            new SpriteSpecifier.Texture(new ResPath("/Textures/SS220/Interface/NavMap/cult_pod.png"))),
        BuildingIconRule("CultYoggFungusHydroponic", "cult-mini-map-fungus",
            new SpriteSpecifier.Texture(new ResPath("/Textures/SS220/Interface/NavMap/cult_fungus.png"))),
        BuildingIconRule("CultYoggAltar", "cult-mini-map-altar",
            new SpriteSpecifier.Texture(new ResPath("/Textures/SS220/Interface/NavMap/cult_altar.png"))),
        BuildingIconRule("CultYoggPond", "cult-mini-map-pond",
            new SpriteSpecifier.Texture(new ResPath("/Textures/SS220/Interface/NavMap/cult_pond.png"))),
        new()
        {
            Component = "SelfLinkedTeleport",
            Label = "cult-mini-map-teleporter",
            Prototypes = new() { "VoidTeleportEnter", "VoidTeleportExit" },
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/SS220/Interface/NavMap/cult_gate.png")),
            Color = Color.Red,
            ShowInList = false,
            ShowHealth = false,
        },
        new()
        {
            Component = "CultYoggBuilding",
            Label = "cult-mini-map-building",
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_square.png")),
            Color = Color.Orange,
            Scale = 0.8f,
            ShowInList = false,
            ShowHealth = false,
        },
    };

    private static CultMiniMapTrackedComponent BuildingIconRule(
        EntProtoId prototype,
        LocId label,
        SpriteSpecifier icon)
    {
        return new CultMiniMapTrackedComponent
        {
            Component = "CultYoggBuilding",
            Label = label,
            Prototypes = new() { prototype },
            Icon = icon,
            Color = Color.Red,
            ShowInList = false,
            ShowHealth = false,
        };
    }

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
    /// Optional entity prototype filter. An empty list matches every entity with <see cref="Component"/>.
    /// </summary>
    [DataField]
    public List<EntProtoId> Prototypes = new();

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

    /// <summary>
    /// How the marker is drawn. Structural marker types use map-scaled vector geometry instead of <see cref="Icon"/>.
    /// </summary>
    [DataField]
    public CultMiniMapMarkerType MarkerType = CultMiniMapMarkerType.Icon;

    /// <summary>
    /// Whether matching entities appear in the side list. They remain visible on the map when disabled.
    /// </summary>
    [DataField]
    public bool ShowInList = true;

    /// <summary>
    /// Whether health is collected and shown for matching entities.
    /// </summary>
    [DataField]
    public bool ShowHealth = true;
}

public sealed partial class CultMiniMapActionEvent : InstantActionEvent
{
}
