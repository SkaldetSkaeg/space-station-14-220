// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// Predicts the scanner's mode switch and displays its current collection mode.
/// </summary>
public sealed class SharedGlovePrintScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlovePrintScannerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<GlovePrintScannerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUseInHand(Entity<GlovePrintScannerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.Mode = ent.Comp.Mode == GlovePrintScannerMode.SurfaceTraces
            ? GlovePrintScannerMode.OwnPrint
            : GlovePrintScannerMode.SurfaceTraces;
        Dirty(ent);
        _popup.PopupClient(Loc.GetString("glove-print-scanner-mode-selected", ("mode", GetModeName(ent.Comp.Mode))),
            ent, args.User);
        args.Handled = true;
    }

    private void OnExamined(Entity<GlovePrintScannerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(Loc.GetString("glove-print-scanner-mode-selected", ("mode", GetModeName(ent.Comp.Mode))));
        args.PushText(Loc.GetString("glove-print-scanner-switch-hint"));
    }

    private string GetModeName(GlovePrintScannerMode mode)
    {
        return Loc.GetString(mode == GlovePrintScannerMode.SurfaceTraces
            ? "glove-print-scanner-mode-surface"
            : "glove-print-scanner-mode-own");
    }
}
