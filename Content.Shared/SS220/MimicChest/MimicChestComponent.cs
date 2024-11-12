using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.MimicChest;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MimicChestComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsBusy;

    [DataField, AutoNetworkedField]
    public TimeSpan TimeStay;

    [AutoNetworkedField]
    [DataField]
    public EntityUid? CapturedTarget;

    [DataField, AutoNetworkedField]
    public TimeSpan CaptureStartTime;

    [DataField, AutoNetworkedField]
    public TimeSpan LastPulseTime;

    [DataField("damage", required: true)]
    public DamageSpecifier Damage = default!;

    [DataField]
    public float PullDuration = 3f; // time for pulling to mimic

}
