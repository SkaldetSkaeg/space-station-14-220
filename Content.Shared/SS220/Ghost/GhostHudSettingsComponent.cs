// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Ghost;

/// <summary>
/// Available HUDs in display order. Enabled starts with the prototype default and tracks the player's selection.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GhostHudSettingsComponent : Component
{
    [DataField(required: true)]
    public List<GhostHudSetting> Huds = [];

    /// <summary>
    /// HUD categories hidden by the observer, including icons available through other components.
    /// </summary>
    [AutoNetworkedField]
    public HashSet<ProtoId<GhostHudPrototype>> HiddenHuds = [];
}

[DataDefinition]
public sealed partial class GhostHudSetting
{
    [DataField(required: true)]
    public ProtoId<GhostHudPrototype> Id;

    [DataField]
    public bool Enabled;
}
