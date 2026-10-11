// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Forensics.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.SS220.Forensics;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Reads glove traces from the currently inserted physical swab without retaining previous results.
/// </summary>
public sealed class GlovePrintAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlovePrintAnalyzerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, GlovePrintAnalyzerRequestResultsMessage>(OnRequestResults);
        SubscribeLocalEvent<GlovePrintAnalyzerComponent, GlovePrintAnalyzerEjectMessage>(OnEject);
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

    private void OnRequestResults(Entity<GlovePrintAnalyzerComponent> ent, ref GlovePrintAnalyzerRequestResultsMessage args)
    {
        _ui.ServerSendUiMessage(ent.Owner, GlovePrintAnalyzerUiKey.Key, GetResults(ent), args.Actor);
    }

    private void OnEject(Entity<GlovePrintAnalyzerComponent> ent, ref GlovePrintAnalyzerEjectMessage args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SampleSlot, out var slot))
            return;

        if (!_itemSlots.TryEjectToHands(ent, slot, args.Actor))
            return;
    }

    private void SendResults(Entity<GlovePrintAnalyzerComponent> ent)
    {
        _ui.ServerSendUiMessage(ent.Owner, GlovePrintAnalyzerUiKey.Key, GetResults(ent));
    }

    private GlovePrintAnalyzerResultsMessage GetResults(Entity<GlovePrintAnalyzerComponent> ent)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SampleSlot, out var slot))
            return new GlovePrintAnalyzerResultsMessage([], null, false);

        if (slot.Item == null)
            return new GlovePrintAnalyzerResultsMessage([], null, false);

        if (!TryComp<GlovePrintSwabComponent>(slot.Item, out var swab))
            return new GlovePrintAnalyzerResultsMessage([], null, false);

        if (!swab.Used)
            return new GlovePrintAnalyzerResultsMessage([], null, true);

        return new GlovePrintAnalyzerResultsMessage([.. swab.GlovePrints], swab.SourceName, true);
    }
}
