// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mobs;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.CultMiniMap;

[Serializable, NetSerializable]
public enum CultMiniMapUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CultMiniMapState(
    NetEntity? grid,
    string gridName,
    List<CultMiniMapMember> members,
    List<CultMiniMapPing> pings) : BoundUserInterfaceState
{
    public readonly NetEntity? Grid = grid;
    public readonly string GridName = gridName;
    public readonly List<CultMiniMapMember> Members = members;
    public readonly List<CultMiniMapPing> Pings = pings;
}

[Serializable, NetSerializable]
public sealed class CultMiniMapPing(
    uint id,
    NetCoordinates coordinates,
    SpriteSpecifier icon,
    Color color,
    float scale)
{
    public readonly uint Id = id;
    public readonly NetCoordinates Coordinates = coordinates;
    public readonly SpriteSpecifier Icon = icon;
    public readonly Color Color = color;
    public readonly float Scale = scale;
}

[Serializable, NetSerializable]
public sealed class CultMiniMapPingMessage(NetCoordinates coordinates) : BoundUserInterfaceMessage
{
    public readonly NetCoordinates Coordinates = coordinates;
}

[Serializable, NetSerializable]
public sealed class CultMiniMapMember(
    NetEntity entity,
    string name,
    CultMiniMapMarker marker,
    NetCoordinates? coordinates,
    float rotation,
    MobState healthState,
    float? damagePercentage)
{
    public readonly NetEntity Entity = entity;
    public readonly string Name = name;
    public readonly CultMiniMapMarker Marker = marker;
    public readonly float Rotation = rotation;
    public readonly MobState HealthState = healthState;

    // Damage relative to this mob's critical threshold, as in crew monitoring.
    // Null if damage or a positive critical threshold is unavailable.
    public readonly float? DamagePercentage = damagePercentage;

    // Relative to the viewer's grid, so targets outside PVS do not need client entities.
    // Null when the viewer has no grid or the target is on another map/in nullspace.
    public readonly NetCoordinates? Coordinates = coordinates;
}

/// <summary>
/// Snapshot of a matching rule's appearance, independent of later component configuration changes.
/// </summary>
[Serializable, NetSerializable]
public sealed class CultMiniMapMarker(
    string component,
    LocId? label,
    SpriteSpecifier icon,
    Color color,
    float scale,
    CultMiniMapMarkerType markerType,
    bool showInList,
    bool showHealth)
{
    public const string SelfComponent = "$self";

    public readonly string Component = component;
    public readonly LocId? Label = label;
    public readonly SpriteSpecifier Icon = icon;
    public readonly Color Color = color;
    public readonly float Scale = scale;
    public readonly CultMiniMapMarkerType MarkerType = markerType;
    public readonly bool ShowInList = showInList;
    public readonly bool ShowHealth = showHealth;
}

[Serializable, NetSerializable]
public enum CultMiniMapMarkerType : byte
{
    Icon,
    Wall,
    SecretDoor,
    Airlock,
}
