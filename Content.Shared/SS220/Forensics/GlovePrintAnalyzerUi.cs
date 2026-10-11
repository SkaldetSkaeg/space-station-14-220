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
    /// The name of the sampled source, or null when the inserted swab has not been used.
    /// </summary>
    public readonly string? SourceName;

    /// <summary>
    /// Whether a swab is physically inserted and can be ejected.
    /// </summary>
    public readonly bool HasSwab;

    public GlovePrintAnalyzerResultsMessage(List<GlovePrintTrace> glovePrints, string? sourceName, bool hasSwab)
    {
        GlovePrints = glovePrints;
        SourceName = sourceName;
        HasSwab = hasSwab;
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
