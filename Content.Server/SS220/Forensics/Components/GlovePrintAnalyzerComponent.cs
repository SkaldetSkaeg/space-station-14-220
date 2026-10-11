// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Forensics.Components;

/// <summary>
/// Identifies the item slot from which the glove-print analyzer reads a physical sample.
/// </summary>
[RegisterComponent]
public sealed partial class GlovePrintAnalyzerComponent : Component
{
    /// <summary>
    /// The slot accepting a glove-print swab.
    /// </summary>
    [DataField]
    public string SampleSlot = "sampleSlot";
}
