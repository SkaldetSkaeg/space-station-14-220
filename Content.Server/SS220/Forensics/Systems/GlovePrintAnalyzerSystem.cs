// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text;
using Content.Server.Popups;
using Content.Server.SS220.Forensics.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.SS220.Forensics;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Reads glove traces from the currently inserted physical swab without retaining previous results.
/// </summary>
public sealed class GlovePrintAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlovePrintAnalyzerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, BoundUserInterfaceMessageAttempt>(OnUiMessageAttempt);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, BoundUIOpenedEvent>(OnOpened);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, GlovePrintAnalyzerRequestResultsMessage>(OnRequestResults);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, GlovePrintAnalyzerEjectMessage>(OnEject);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, GlovePrintAnalyzerPrintMessage>(OnPrint);
    }

    private void OnInserted(Entity<GlovePrintAnalyzerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SampleSlot)
            return;

        SendResults(ent);
    }

    private void OnRemoved(Entity<GlovePrintAnalyzerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SampleSlot)
            return;

        SendResults(ent);
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
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SampleSlot, out var slot))
            return;

        if (!_itemSlots.TryEjectToHands(ent, slot, args.Actor))
            return;
    }

    private void OnPrint(Entity<GlovePrintAnalyzerComponent> ent, ref GlovePrintAnalyzerPrintMessage args)
    {
        var results = GetResults(ent);
        if (!results.HasSwab || !results.Used)
            return;

        if (_timing.CurTime < ent.Comp.PrintReadyAt)
        {
            _popup.PopupEntity(Loc.GetString("forensic-scanner-printer-not-ready"), ent, args.Actor);
            SendResults(ent, args.Actor);
            return;
        }

        var report = Spawn(ent.Comp.MachineOutput, Transform(ent).Coordinates);
        if (!TryComp<PaperComponent>(report, out var paper))
        {
            Log.Error($"Glove trace analyzer output {ToPrettyString(report)} has no PaperComponent.");
            Del(report);
            return;
        }

        var text = new StringBuilder();
        text.AppendLine(Loc.GetString("glove-print-analyzer-interface-traces"));
        if (results.GlovePrints.Count == 0)
            text.AppendLine(Loc.GetString("glove-print-analyzer-interface-no-traces"));

        foreach (var trace in results.GlovePrints)
        {
            var name = _prototype.TryIndex(trace.Prototype, out var prototype)
                ? prototype.Name
                : Loc.GetString("glove-print-analyzer-interface-unknown-type");
            text.AppendLine(Loc.GetString("glove-print-analyzer-interface-type", ("type", name)));
            text.AppendLine(Loc.GetString("glove-print-analyzer-interface-code", ("code", trace.Code)));
            text.AppendLine();
        }

        _metadata.SetEntityName(report, Loc.GetString("glove-print-analyzer-report-title"));
        _paper.SetContent((report, paper), FormattedMessage.EscapeText(text.ToString()));
        _hands.PickupOrDrop(args.Actor, report, checkActionBlocker: false);
        _audio.PlayPvs(ent.Comp.SoundPrint, ent,
            AudioParams.Default.WithVariation(0.25f).WithVolume(3f).WithRolloffFactor(2.8f).WithMaxDistance(4.5f));
        ent.Comp.PrintReadyAt = _timing.CurTime + ent.Comp.PrintCooldown;
        SendResults(ent);
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
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SampleSlot, out var slot))
            return new GlovePrintAnalyzerResultsMessage([], false, false, ent.Comp.PrintReadyAt, ent.Comp.PrintCooldown);

        if (slot.Item == null)
            return new GlovePrintAnalyzerResultsMessage([], false, false, ent.Comp.PrintReadyAt, ent.Comp.PrintCooldown);

        if (!TryComp<GlovePrintSwabComponent>(slot.Item, out var swab))
            return new GlovePrintAnalyzerResultsMessage([], false, false, ent.Comp.PrintReadyAt, ent.Comp.PrintCooldown);

        if (!swab.Used)
            return new GlovePrintAnalyzerResultsMessage([], true, false, ent.Comp.PrintReadyAt, ent.Comp.PrintCooldown);

        return new GlovePrintAnalyzerResultsMessage([.. swab.GlovePrints], true, true,
            ent.Comp.PrintReadyAt, ent.Comp.PrintCooldown);
    }
}
