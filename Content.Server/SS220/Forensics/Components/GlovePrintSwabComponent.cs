// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Forensics;

namespace Content.Server.SS220.Forensics.Components;

/// <summary>
/// Stores a single collected glove-trace sample, independent of the source's later state.
/// </summary>
[RegisterComponent]
public sealed partial class GlovePrintSwabComponent : Component
{
    /// <summary>
    /// Whether this swab contains a collected sample, including a sample without glove traces.
    /// </summary>
    [DataField]
    public bool Used;

    /// <summary>
    /// The glove traces captured when the sample was taken.
    /// </summary>
    [DataField]
    public List<GlovePrintTrace> GlovePrints = [];
}
