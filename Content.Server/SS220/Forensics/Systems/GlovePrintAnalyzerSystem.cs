// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text;
using Content.Server.DeviceLinking.Systems;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.SS220.Forensics.Components;
using Content.Server.SS220.Photocopier;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Interaction;
using Content.Shared.SS220.Forensics;
using Content.Shared.SS220.Photocopier;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Analyzes physical glove-trace samples and retains private snapshots for later viewing and printing.
/// </summary>
public sealed class GlovePrintAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly PhotocopierSystem _photocopier = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly TimeSpan PrinterCheckInterval = TimeSpan.FromMilliseconds(250);
    private TimeSpan _nextPrinterCheck;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlovePrintAnalyzerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, LinkAttemptEvent>(OnLinkAttempt);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, BoundUserInterfaceMessageAttempt>(OnUiMessageAttempt);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, BoundUIOpenedEvent>(OnOpened);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, GlovePrintAnalyzerRequestResultsMessage>(OnRequestResults);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, GlovePrintAnalyzerEjectMessage>(OnEject);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, GlovePrintAnalyzerAnalyzeMessage>(OnAnalyze);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, GlovePrintAnalyzerSetDescriptionMessage>(OnSetDescription);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, GlovePrintAnalyzerPrintMessage>(OnPrint);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var checkPrinter = _timing.CurTime >= _nextPrinterCheck;
        if (checkPrinter)
            _nextPrinterCheck = _timing.CurTime + PrinterCheckInterval;

        var query = EntityQueryEnumerator<GlovePrintAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            Entity<GlovePrintAnalyzerComponent> ent = (uid, component);
            if (checkPrinter)
                UpdatePrinterStatus(ent);

            if (component.AnalyzingSwab == null)
                continue;

            if (!TryGetSample(ent, out var sample))
            {
                CancelAnalysis(ent);
                component.PendingDescription = string.Empty;
                SendResults(ent);
                continue;
            }

            if (sample.Owner != component.AnalyzingSwab.Value || !sample.Comp.Used)
            {
                CancelAnalysis(ent);
                component.PendingDescription = string.Empty;
                SendResults(ent);
                continue;
            }

            if (_timing.CurTime < component.AnalysisFinishAt)
                continue;

            var id = component.History.Count + 1;
            var identifier = $"{_gameTicker.RoundId}-{ent.Owner.Id}-{id}";
            component.History.Add(new GlovePrintAnalyzerHistoryEntry(id, [.. sample.Comp.GlovePrints],
                component.PendingDescription, identifier, sample.Comp.SourceName, [.. sample.Comp.MicroFibers],
                _gameTicker.RoundDuration()));
            ResetAnalysis(ent);
            component.PendingDescription = string.Empty;
            Del(sample.Owner);
            SendResults(ent);
        }
    }

    private void OnMapInit(Entity<GlovePrintAnalyzerComponent> ent, ref MapInitEvent args)
    {
        UpdatePrinterStatus(ent);
    }

    private void OnLinkAttempt(Entity<GlovePrintAnalyzerComponent> ent, ref LinkAttemptEvent args)
    {
        if (args.Source != ent.Owner || args.SourcePort != GlovePrintAnalyzerUi.ReportSourcePort.Id)
            return;

        if (args.SinkPort != GlovePrintAnalyzerUi.ReportSinkPort.Id)
        {
            args.Cancel();
            return;
        }

        if (!HasComp<PhotocopierComponent>(args.Sink))
            args.Cancel();
    }

    private void OnNewLink(Entity<GlovePrintAnalyzerComponent> ent, ref NewLinkEvent args)
    {
        if (args.Source != ent.Owner || args.SourcePort != GlovePrintAnalyzerUi.ReportSourcePort.Id)
            return;

        if (args.SinkPort != GlovePrintAnalyzerUi.ReportSinkPort.Id)
            return;

        if (!HasComp<PhotocopierComponent>(args.Sink))
            return;

        foreach (var previous in _deviceLink.GetLinkedSinks((ent.Owner, null), GlovePrintAnalyzerUi.ReportSourcePort))
        {
            if (previous == args.Sink)
                continue;

            _deviceLink.RemoveSinkFromSource(ent, previous);
        }

        ent.Comp.LinkedPhotocopier = args.Sink;
        SendResults(ent);
    }

    private void OnPortDisconnected(Entity<GlovePrintAnalyzerComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != GlovePrintAnalyzerUi.ReportSourcePort.Id)
            return;

        // The link system raises this event before removing the old link from its tables.
        ent.Comp.LinkedPhotocopier = null;
        SendResults(ent);
    }

    private void UpdatePrinterStatus(Entity<GlovePrintAnalyzerComponent> ent)
    {
        if (ent.Comp.LinkedPhotocopier == null || !IsLinkedPhotocopier(ent, ent.Comp.LinkedPhotocopier.Value))
        {
            ent.Comp.LinkedPhotocopier = null;
            foreach (var printer in _deviceLink.GetLinkedSinks((ent.Owner, null), GlovePrintAnalyzerUi.ReportSourcePort))
            {
                if (!IsLinkedPhotocopier(ent, printer))
                    continue;

                ent.Comp.LinkedPhotocopier = printer;
                break;
            }
        }

        if (ent.Comp.PrinterStatus != GetPrinterStatus(ent))
            SendResults(ent);
    }

    private bool IsLinkedPhotocopier(Entity<GlovePrintAnalyzerComponent> ent, EntityUid printer)
    {
        if (TerminatingOrDeleted(printer))
            return false;

        if (!HasComp<PhotocopierComponent>(printer) || !HasComp<DeviceLinkSinkComponent>(printer))
            return false;

        return _deviceLink.GetLinks(ent, printer).Contains(
            (GlovePrintAnalyzerUi.ReportSourcePort, GlovePrintAnalyzerUi.ReportSinkPort));
    }

    private GlovePrintAnalyzerPrinterStatus GetPrinterStatus(Entity<GlovePrintAnalyzerComponent> ent)
    {
        if (ent.Comp.LinkedPhotocopier == null)
            return GlovePrintAnalyzerPrinterStatus.Disconnected;

        var printer = ent.Comp.LinkedPhotocopier.Value;
        if (!IsLinkedPhotocopier(ent, printer))
            return GlovePrintAnalyzerPrinterStatus.Disconnected;

        return _photocopier.CanPrintReport(printer)
            ? GlovePrintAnalyzerPrinterStatus.Ready
            : GlovePrintAnalyzerPrinterStatus.Unavailable;
    }

    private void OnInserted(Entity<GlovePrintAnalyzerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SampleSlot)
            return;

        ent.Comp.PendingDescription = string.Empty;
        SendResults(ent);
    }

    private void OnRemoved(Entity<GlovePrintAnalyzerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SampleSlot)
            return;

        if (ent.Comp.AnalyzingSwab == args.Entity)
            CancelAnalysis(ent);

        ent.Comp.PendingDescription = string.Empty;
        SendResults(ent);
    }

    private void ResetAnalysis(Entity<GlovePrintAnalyzerComponent> ent)
    {
        ent.Comp.AnalyzingSwab = null;
        ent.Comp.AnalysisStartedAt = TimeSpan.Zero;
        ent.Comp.AnalysisFinishAt = TimeSpan.Zero;
    }

    private void CancelAnalysis(Entity<GlovePrintAnalyzerComponent> ent)
    {
        ResetAnalysis(ent);
        _audio.PlayPvs(ent.Comp.SoundDeny, ent);
    }

    private void OnUiMessageAttempt(Entity<GlovePrintAnalyzerComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (!Equals(args.UiKey, GlovePrintAnalyzerUiKey.Key))
            return;

        if (!CanAccess(ent, args.Actor))
            args.Cancel();
    }

    private void OnOpened(Entity<GlovePrintAnalyzerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, GlovePrintAnalyzerUiKey.Key))
            return;

        SendResults(ent, args.Actor);
    }

    private void OnRequestResults(Entity<GlovePrintAnalyzerComponent> ent, ref GlovePrintAnalyzerRequestResultsMessage args)
    {
        SendResults(ent, args.Actor);
    }

    private void OnEject(Entity<GlovePrintAnalyzerComponent> ent, ref GlovePrintAnalyzerEjectMessage args)
    {
        if (!CanAccess(ent, args.Actor))
            return;

        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SampleSlot, out var slot))
            return;

        if (!_itemSlots.TryEjectToHands(ent, slot, args.Actor))
            return;
    }

    private void OnAnalyze(Entity<GlovePrintAnalyzerComponent> ent, ref GlovePrintAnalyzerAnalyzeMessage args)
    {
        if (!CanAccess(ent, args.Actor))
            return;

        if (args.Description == null || args.Description.Length > GlovePrintAnalyzerUi.MaxDescriptionLength)
            return;

        if (ent.Comp.AnalyzingSwab != null)
            return;

        if (!TryGetSample(ent, out var sample))
            return;

        if (!sample.Comp.Used)
            return;

        if (args.Swab == null)
            return;

        if (args.Swab.Value != GetNetEntity(sample.Owner))
            return;

        ent.Comp.PendingDescription = args.Description;
        ent.Comp.AnalyzingSwab = sample.Owner;
        ent.Comp.AnalysisStartedAt = _timing.CurTime;
        ent.Comp.AnalysisFinishAt = _timing.CurTime + ent.Comp.AnalysisDuration;
        SendResults(ent);
    }

    private void OnSetDescription(Entity<GlovePrintAnalyzerComponent> ent, ref GlovePrintAnalyzerSetDescriptionMessage args)
    {
        if (!CanAccess(ent, args.Actor))
            return;

        if (args.Description == null || args.Description.Length > GlovePrintAnalyzerUi.MaxDescriptionLength)
            return;

        if (args.SampleId == null)
        {
            if (!TryGetSample(ent, out var sample))
                return;

            if (!sample.Comp.Used)
                return;

            if (args.Swab == null)
                return;

            if (args.Swab.Value != GetNetEntity(sample.Owner))
                return;

            ent.Comp.PendingDescription = args.Description;
            SendResults(ent);
            return;
        }

        var sampleId = args.SampleId.Value;
        var index = ent.Comp.History.FindIndex(entry => entry.Id == sampleId);
        if (index < 0)
            return;

        var stored = ent.Comp.History[index];
        ent.Comp.History[index] = new GlovePrintAnalyzerHistoryEntry(stored.Id, stored.GlovePrints,
            args.Description, stored.Identifier, stored.SourceName, stored.MicroFibers, stored.AnalyzedAt);
        SendResults(ent);
    }

    private void OnPrint(Entity<GlovePrintAnalyzerComponent> ent, ref GlovePrintAnalyzerPrintMessage args)
    {
        if (!CanAccess(ent, args.Actor))
            return;

        if (!TryGetPrintSample(ent, args.SampleId, out var sample))
            return;

        if (_timing.CurTime < ent.Comp.PrintReadyAt)
        {
            _popup.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), ent, args.Actor);
            SendResults(ent, args.Actor);
            return;
        }

        if (GetPrinterStatus(ent) != GlovePrintAnalyzerPrinterStatus.Ready)
        {
            _popup.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), ent, args.Actor);
            SendResults(ent, args.Actor);
            return;
        }

        if (ent.Comp.LinkedPhotocopier == null)
            return;

        var printer = ent.Comp.LinkedPhotocopier.Value;
        if (!_photocopier.TryQueueReport(printer, ent.Comp.MachineOutput,
                Loc.GetString("glove-print-analyzer-report-title"), CreateReport(sample)))
        {
            SendResults(ent, args.Actor);
            return;
        }

        ent.Comp.PrintReadyAt = _timing.CurTime + ent.Comp.PrintCooldown;
        SendResults(ent);
    }

    /// <summary>
    /// Formats the official security report, escaping sample values while preserving the template's markup.
    /// </summary>
    private string CreateReport(GlovePrintAnalyzerHistoryEntry sample)
    {
        var text = new StringBuilder();
        text.AppendLine(Loc.GetString("glove-print-analyzer-report-header",
            ("identifier", FormattedMessage.EscapeText(sample.Identifier))));
        text.AppendLine(Loc.GetString("glove-print-analyzer-report-analyzed-at",
            ("time", GlovePrintAnalyzerUi.FormatRoundTime(sample.AnalyzedAt))));
        if (!string.IsNullOrEmpty(sample.SourceName))
            text.AppendLine(Loc.GetString("glove-print-analyzer-report-source",
                ("source", FormattedMessage.EscapeText(sample.SourceName))));

        text.AppendLine();
        text.AppendLine(Loc.GetString("glove-print-analyzer-report-results", ("count", sample.GlovePrints.Count)));
        text.AppendLine();

        if (sample.GlovePrints.Count == 0)
        {
            text.AppendLine(Loc.GetString("glove-print-analyzer-interface-no-traces"));
            text.AppendLine();
        }

        for (var i = 0; i < sample.GlovePrints.Count; i++)
        {
            var trace = sample.GlovePrints[i];
            var name = _prototype.TryIndex(trace.Prototype, out var prototype)
                ? prototype.Name
                : Loc.GetString("glove-print-analyzer-interface-unknown-type");
            text.AppendLine(Loc.GetString("glove-print-analyzer-report-trace",
                ("number", i + 1), ("type", FormattedMessage.EscapeText(name)),
                ("code", FormattedMessage.EscapeText(trace.Code))));
            text.AppendLine();
        }

        text.AppendLine(Loc.GetString("glove-print-analyzer-report-micro-fibers", ("count", sample.MicroFibers.Count)));
        for (var i = 0; i < sample.MicroFibers.Count; i++)
        {
            var trace = sample.MicroFibers[i];
            text.AppendLine(Loc.GetString("glove-print-analyzer-report-micro-fiber",
                ("number", i + 1), ("type", FormattedMessage.EscapeText(GetMicroFiberName(trace))),
                ("code", FormattedMessage.EscapeText(trace.Code))));
            text.AppendLine();
        }
        text.AppendLine();

        if (!string.IsNullOrEmpty(sample.Description))
        {
            text.AppendLine(Loc.GetString("glove-print-analyzer-report-description",
                ("description", FormattedMessage.EscapeText(sample.Description))));
            text.AppendLine();
        }

        text.AppendLine(Loc.GetString("glove-print-analyzer-report-footer"));
        return text.ToString();
    }

    private string GetMicroFiberName(MicroFiberTrace trace)
    {
        if (!string.IsNullOrEmpty(trace.SourceName))
            return trace.SourceName;

        if (_prototype.TryIndex(trace.Prototype, out var prototype))
            return prototype.Name;

        return Loc.GetString("glove-print-analyzer-interface-unknown-item");
    }

    private bool TryGetPrintSample(Entity<GlovePrintAnalyzerComponent> ent, int? sampleId,
        out GlovePrintAnalyzerHistoryEntry sample)
    {
        sample = null!;
        if (sampleId == null)
            return false;

        var stored = ent.Comp.History.Find(entry => entry.Id == sampleId.Value);
        if (stored == null)
            return false;

        sample = stored;
        return true;
    }

    private bool TryGetSample(Entity<GlovePrintAnalyzerComponent> ent, out Entity<GlovePrintSwabComponent> sample)
    {
        sample = default;
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SampleSlot, out var slot))
            return false;

        if (slot.Item == null)
            return false;

        if (TerminatingOrDeleted(slot.Item.Value))
            return false;

        if (!TryComp<GlovePrintSwabComponent>(slot.Item.Value, out var swab))
            return false;

        sample = (slot.Item.Value, swab);
        return true;
    }

    private bool CanAccess(Entity<GlovePrintAnalyzerComponent> ent, EntityUid actor)
    {
        var range = _ui.GetUiRange(ent.Owner, GlovePrintAnalyzerUiKey.Key);
        if (range <= 0)
            return _interaction.IsAccessible(actor, ent.Owner);

        if (HasComp<IgnoreUIRangeComponent>(actor))
            return true;

        return _interaction.InRangeAndAccessible(actor, ent.Owner, range);
    }

    private void SendResults(Entity<GlovePrintAnalyzerComponent> ent)
    {
        var results = GetResults(ent);
        foreach (var actor in _ui.GetActors(ent.Owner, GlovePrintAnalyzerUiKey.Key))
        {
            if (!CanAccess(ent, actor))
                continue;

            _ui.ServerSendUiMessage(ent.Owner, GlovePrintAnalyzerUiKey.Key, results, actor);
        }
    }

    private void SendResults(Entity<GlovePrintAnalyzerComponent> ent, EntityUid actor)
    {
        if (!CanAccess(ent, actor))
            return;

        _ui.ServerSendUiMessage(ent.Owner, GlovePrintAnalyzerUiKey.Key, GetResults(ent), actor);
    }

    private GlovePrintAnalyzerResultsMessage GetResults(Entity<GlovePrintAnalyzerComponent> ent)
    {
        List<GlovePrintAnalyzerHistoryEntry> history = [.. ent.Comp.History];
        var hasSwab = TryGetSample(ent, out var sample);
        var used = hasSwab && sample.Comp.Used;
        ent.Comp.PrinterStatus = GetPrinterStatus(ent);
        return new GlovePrintAnalyzerResultsMessage(hasSwab, used, ent.Comp.PrintReadyAt, ent.Comp.PrintCooldown,
            history, ent.Comp.AnalyzingSwab != null, ent.Comp.AnalysisStartedAt, ent.Comp.AnalysisFinishAt,
            ent.Comp.PendingDescription, hasSwab ? GetNetEntity(sample.Owner) : null, ent.Comp.PrinterStatus, ent.Owner.Id,
            hasSwab ? sample.Comp.SourceName : string.Empty);
    }
}
