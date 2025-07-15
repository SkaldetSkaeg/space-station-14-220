using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.EquipableProjectile;

/// <summary>
/// Embeds this entity inside of the hit target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EquipableProjectileComponent : Component
{
    /// <summary>
    /// Sound to play after embedding into a hit target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;

}
