// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Globalization;
using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// Shared limits for descriptions attached to pending and completed glove analyses.
/// </summary>
public static class GloveMicroFiberAnalyzerUi
{
    /// <summary>
    /// The analyzer port used to establish a report printer link.
    /// </summary>
    public static readonly ProtoId<SourcePortPrototype> ReportSourcePort = "GloveMicroFiberReport";

    /// <summary>
    /// The copier port accepting report printer links.
    /// </summary>
    public static readonly ProtoId<SinkPortPrototype> ReportSinkPort = "PrintDocument";

    /// <summary>
    /// Maximum description length accepted by the editor and server.
    /// </summary>
    public const int MaxDescriptionLength = 512;

    /// <summary>
    /// Formats a recorded round duration as hours, minutes and seconds without wrapping after a day.
    /// </summary>
    public static string FormatRoundTime(TimeSpan roundTime)
    {
        if (roundTime < TimeSpan.Zero)
            roundTime = TimeSpan.Zero;

        var hours = roundTime.Ticks / TimeSpan.TicksPerHour;
        return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2}",
            hours, roundTime.Minutes, roundTime.Seconds);
    }
}

/// <summary>
/// The interface used to examine a swab inserted into the glove-micro-fiber analyzer.
/// </summary>
[Serializable, NetSerializable]
public enum GloveMicroFiberAnalyzerUiKey : byte
{
    Key,
}

/// <summary>
/// The linked copier's ability to receive a report from this analyzer.
/// </summary>
[Serializable, NetSerializable]
public enum GloveMicroFiberAnalyzerPrinterStatus : byte
{
    Disconnected,
    Unavailable,
    Ready,
}

/// <summary>
/// A stored snapshot of a physical sample previously inserted into this analyzer.
/// </summary>
[Serializable, NetSerializable]
public sealed class GloveMicroFiberAnalyzerHistoryEntry
{
    /// <summary>
    /// The sample's permanent number within this analyzer.
    /// </summary>
    public readonly int Id;

    /// <summary>
    /// The glove identities and types captured when analysis completed.
    /// </summary>
    public readonly List<GloveMicroFiberTrace> GloveMicroFibers;

    /// <summary>
    /// The individual material profiles captured when analysis completed.
    /// </summary>
    public readonly List<StorageMicroFiberTrace> StorageMicroFibers;

    /// <summary>
    /// The investigator's description of this analysis record.
    /// </summary>
    public readonly string Description;

    /// <summary>
    /// The round, analyzer UID and local history number identifying this completed analysis.
    /// </summary>
    public readonly string Identifier;

    /// <summary>
    /// The scanned item's name captured on the physical sample.
    /// </summary>
    public readonly string SourceName;

    /// <summary>
    /// Elapsed round time when this sample's analysis completed, retained as a permanent snapshot.
    /// </summary>
    public readonly TimeSpan AnalyzedAt;

    public GloveMicroFiberAnalyzerHistoryEntry(int id, List<GloveMicroFiberTrace> gloveMicroFibers,
        string description = "", string identifier = "", string sourceName = "",
        List<StorageMicroFiberTrace>? storageMicroFibers = null, TimeSpan analyzedAt = default)
    {
        Id = id;
        GloveMicroFibers = gloveMicroFibers;
        StorageMicroFibers = storageMicroFibers ?? [];
        Description = description;
        Identifier = identifier;
        SourceName = sourceName;
        AnalyzedAt = analyzedAt;
    }
}

/// <summary>
/// Private completed analyses and the pending sample's status, sent only to users of the interface.
/// </summary>
[Serializable, NetSerializable]
public sealed class GloveMicroFiberAnalyzerResultsMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The server-side analyzer UID used in completed sample identifiers.
    /// </summary>
    public readonly int AnalyzerId;

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

    /// <summary>
    /// This analyzer's stored samples, sent only to authorized users of the interface.
    /// </summary>
    public readonly List<GloveMicroFiberAnalyzerHistoryEntry> History;

    /// <summary>
    /// Whether the analyzer is processing the physically inserted swab.
    /// </summary>
    public readonly bool IsAnalyzing;

    /// <summary>
    /// When the current analysis started.
    /// </summary>
    public readonly TimeSpan AnalysisStartedAt;

    /// <summary>
    /// When the current analysis will finish if the swab stays inserted.
    /// </summary>
    public readonly TimeSpan AnalysisFinishAt;

    /// <summary>
    /// The description to attach to the pending sample when its analysis completes.
    /// </summary>
    public readonly string PendingDescription;

    /// <summary>
    /// The physical swab targeted by pending analysis and description requests.
    /// </summary>
    public readonly NetEntity? PendingSwab;

    /// <summary>
    /// The scanned item's name on the inserted sample, without revealing its unanalyzed material evidence.
    /// </summary>
    public readonly string PendingSourceName;

    /// <summary>
    /// Whether a valid linked copier is ready to print a report.
    /// </summary>
    public readonly GloveMicroFiberAnalyzerPrinterStatus PrinterStatus;

    public GloveMicroFiberAnalyzerResultsMessage(bool hasSwab, bool used, TimeSpan printReadyAt, TimeSpan printCooldown,
        List<GloveMicroFiberAnalyzerHistoryEntry> history, bool isAnalyzing, TimeSpan analysisStartedAt,
        TimeSpan analysisFinishAt, string pendingDescription, NetEntity? pendingSwab,
        GloveMicroFiberAnalyzerPrinterStatus printerStatus, int analyzerId, string pendingSourceName = "")
    {
        AnalyzerId = analyzerId;
        HasSwab = hasSwab;
        Used = used;
        PrintReadyAt = printReadyAt;
        PrintCooldown = printCooldown;
        History = history;
        IsAnalyzing = isAnalyzing;
        AnalysisStartedAt = analysisStartedAt;
        AnalysisFinishAt = analysisFinishAt;
        PendingDescription = pendingDescription;
        PendingSwab = pendingSwab;
        PrinterStatus = printerStatus;
        PendingSourceName = pendingSourceName;
    }
}

/// <summary>
/// Requests the currently inserted sample's results after opening the analyzer.
/// </summary>
[Serializable, NetSerializable]
public sealed class GloveMicroFiberAnalyzerRequestResultsMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Requests removal of the swab from the analyzer into the user's hands.
/// </summary>
[Serializable, NetSerializable]
public sealed class GloveMicroFiberAnalyzerEjectMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Starts processing the inserted sample, including the description currently entered by the user.
/// </summary>
[Serializable, NetSerializable]
public sealed class GloveMicroFiberAnalyzerAnalyzeMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The description entered for the pending sample.
    /// </summary>
    public readonly string Description;

    /// <summary>
    /// The expected physical sample, preventing a delayed request from analyzing a replacement swab.
    /// </summary>
    public readonly NetEntity? Swab;

    public GloveMicroFiberAnalyzerAnalyzeMessage(string description, NetEntity? swab)
    {
        Description = description;
        Swab = swab;
    }
}

/// <summary>
/// Saves a description for a completed analysis or, when the ID is null, for the pending physical sample.
/// </summary>
[Serializable, NetSerializable]
public sealed class GloveMicroFiberAnalyzerSetDescriptionMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The completed record to edit, or null to edit the pending physical sample.
    /// </summary>
    public readonly int? SampleId;

    /// <summary>
    /// The new description, including an empty string to clear it.
    /// </summary>
    public readonly string Description;

    /// <summary>
    /// The expected physical sample when editing a pending description.
    /// </summary>
    public readonly NetEntity? Swab;

    public GloveMicroFiberAnalyzerSetDescriptionMessage(int? sampleId, string description, NetEntity? swab = null)
    {
        SampleId = sampleId;
        Description = description;
        Swab = swab;
    }
}

/// <summary>
/// Requests a paper report of a completed analysis belonging to this analyzer.
/// </summary>
[Serializable, NetSerializable]
public sealed class GloveMicroFiberAnalyzerPrintMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// The stored sample to print. Requests without a stored sample ID are rejected.
    /// </summary>
    public readonly int? SampleId;

    public GloveMicroFiberAnalyzerPrintMessage(int? sampleId = null)
    {
        SampleId = sampleId;
    }
}
