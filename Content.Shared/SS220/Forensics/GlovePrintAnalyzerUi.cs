// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// The interface used to examine a swab inserted into the glove-print analyzer.
/// </summary>
[Serializable, NetSerializable]
public enum GlovePrintAnalyzerUiKey : byte
{
    Key,
}

/// <summary>
/// Private results from the physically inserted swab, sent only to users of the interface.
/// </summary>
[Serializable, NetSerializable]
public sealed class GlovePrintAnalyzerResultsMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The collected glove identities and their types.
    /// </summary>
    public readonly List<GlovePrintTrace> GlovePrints;

    /// <summary>
    /// Whether the inserted swab contains a collected sample, including a sample without traces.
    /// </summary>
    public readonly bool Used;

    /// <summary>
    /// Whether a swab is physically inserted and can be ejected.
    /// </summary>
    public readonly bool HasSwab;

    /// <summary>
    /// When the printer can produce another report.
    /// </summary>
    public readonly TimeSpan PrintReadyAt;

    /// <summary>
    /// Delay between printed reports.
    /// </summary>
    public readonly TimeSpan PrintCooldown;

    public GlovePrintAnalyzerResultsMessage(List<GlovePrintTrace> glovePrints, bool hasSwab, bool used,
        TimeSpan printReadyAt, TimeSpan printCooldown)
    {
        GlovePrints = glovePrints;
        HasSwab = hasSwab;
        Used = used;
        PrintReadyAt = printReadyAt;
        PrintCooldown = printCooldown;
    }
}

/// <summary>
/// Requests the currently inserted sample's results after opening the analyzer.
/// </summary>
[Serializable, NetSerializable]
public sealed class GlovePrintAnalyzerRequestResultsMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Requests removal of the swab from the analyzer into the user's hands.
/// </summary>
[Serializable, NetSerializable]
public sealed class GlovePrintAnalyzerEjectMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Requests a paper report of the currently inserted collected sample.
/// </summary>
[Serializable, NetSerializable]
public sealed class GlovePrintAnalyzerPrintMessage : BoundUserInterfaceMessage
{
}
