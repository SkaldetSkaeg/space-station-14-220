// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Teleport.Components;

/// <summary>
/// Scales a queried teleport radius by the produce's potency. Ten potency preserves the base radius.
/// </summary>
[RegisterComponent]
public sealed partial class TeleportRadiusFromPotencyComponent : Component;
