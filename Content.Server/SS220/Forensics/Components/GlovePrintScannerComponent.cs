// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;

namespace Content.Server.SS220.Forensics.Components;

/// <summary>
/// Stores glove traces and a reference sample for a separate glove-print scanner.
/// Results are disclosed through its interface rather than component replication.
/// </summary>
[RegisterComponent]
public sealed partial class GlovePrintScannerComponent : Component
{
    /// <summary>
    /// The individual glove traces from the last scanned object.
    /// </summary>
    [DataField]
    public List<string> GlovePrints = [];

    /// <summary>
    /// The permanent identity sampled directly from a pair of gloves.
    /// </summary>
    [DataField]
    public string? Sample;

    /// <summary>
    /// The display name of the object from which the stored traces were collected.
    /// </summary>
    [DataField]
    public string LastScannedName = string.Empty;

    /// <summary>
    /// The time required to scan an object or collect a glove sample.
    /// </summary>
    [DataField]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Played when a sampled glove identity matches the stored traces.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundMatch = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

    /// <summary>
    /// Played when a sampled glove identity does not match the stored traces.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundNoMatch = new SoundPathSpecifier("/Audio/Machines/airlock_deny.ogg");
}
