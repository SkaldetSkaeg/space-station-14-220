// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.Forensics.Components;

/// <summary>
/// Identifies the item slot from which the glove-print analyzer reads a physical sample.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class GlovePrintAnalyzerComponent : Component
{
    /// <summary>
    /// The slot accepting a glove-print swab.
    /// </summary>
    [DataField]
    public string SampleSlot = "sampleSlot";

    /// <summary>
    /// The paper used for printed reports.
    /// </summary>
    [DataField]
    public EntProtoId MachineOutput = "ForensicReportPaper";

    /// <summary>
    /// Delay between printed reports.
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When the printer can produce another report.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan PrintReadyAt;

    /// <summary>
    /// Sound played when a report is printed.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");
}
