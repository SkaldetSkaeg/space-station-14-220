// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Forensics.Components;

/// <summary>
/// Persistent identity of a pair of gloves, separate from removable evidence on its surface.
/// Only gloves that leave fibers expose this identity in forensic samples.
/// </summary>
[RegisterComponent]
public sealed partial class GlovePrintComponent : Component
{
    /// <summary>
    /// Random identifier generated once for this pair and retained when its fingertips are cut off.
    /// </summary>
    [DataField]
    public string? Print;
}
