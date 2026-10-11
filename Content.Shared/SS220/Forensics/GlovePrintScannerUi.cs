// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// The interface for examining the glove traces and sample stored in a scanner.
/// </summary>
[Serializable, NetSerializable]
public enum GlovePrintScannerUiKey : byte
{
    Key,
}

/// <summary>
/// Results disclosed to users of the scanner interface after a completed scan.
/// </summary>
[Serializable, NetSerializable]
public sealed class GlovePrintScannerResultsMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The glove identities found on the last scanned object.
    /// </summary>
    public readonly List<string> GlovePrints;

    /// <summary>
    /// The identity sampled from the reference pair of gloves.
    /// </summary>
    public readonly string? Sample;

    /// <summary>
    /// The display name of the object from which the traces were collected.
    /// </summary>
    public readonly string LastScannedName;

    public GlovePrintScannerResultsMessage(List<string> glovePrints, string? sample, string lastScannedName)
    {
        GlovePrints = glovePrints;
        Sample = sample;
        LastScannedName = lastScannedName;
    }
}

/// <summary>
/// Requests the stored results after opening the scanner interface.
/// </summary>
[Serializable, NetSerializable]
public sealed class GlovePrintScannerRequestResultsMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Clears both the scanned traces and the reference glove sample.
/// </summary>
[Serializable, NetSerializable]
public sealed class GlovePrintScannerClearMessage : BoundUserInterfaceMessage
{
}
