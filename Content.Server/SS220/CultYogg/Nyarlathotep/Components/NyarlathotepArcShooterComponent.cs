using Content.Server.SS220.CultYogg.Nyarlathotep;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.Components;

[RegisterComponent, Access(typeof(NyarlathotepTargetSearcherSystem)), AutoGenerateComponentPause]
public sealed partial class NyarlathotepSearchTargetsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SearchMinInterval = 2.5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SearchMaxInterval = 8.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SearchRange = 5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextSearchTime;
}
