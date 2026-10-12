// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// Predicts the scanner's mode switch and displays its current collection mode.
/// </summary>
public sealed class SharedGlovePrintScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlovePrintScannerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<GlovePrintScannerComponent, ExaminedEvent>(OnExamined);
        SubscribeAllEvent<GlovePrintScannerSetModeEvent>(OnSetMode);
    }

    private void OnUseInHand(Entity<GlovePrintScannerComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var mode = ent.Comp.Mode == GlovePrintScannerMode.SurfaceTraces
            ? GlovePrintScannerMode.OwnPrint
            : GlovePrintScannerMode.SurfaceTraces;
        SetMode(ent, mode, args.User);
        args.Handled = true;
    }

    private void OnSetMode(GlovePrintScannerSetModeEvent msg, EntitySessionEventArgs args)
    {
        if (msg.Mode != GlovePrintScannerMode.SurfaceTraces && msg.Mode != GlovePrintScannerMode.OwnPrint)
            return;

        var user = args.SenderSession.AttachedEntity;
        if (user == null)
            return;

        if (!TryGetEntity(msg.Scanner, out var scanner))
            return;

        if (!TryComp<GlovePrintScannerComponent>(scanner.Value, out var component))
            return;

        if (!_hands.IsHolding(user.Value, scanner))
            return;

        if (!_blocker.CanInteract(user.Value, scanner))
            return;

        if (!_blocker.CanUseHeldEntity(user.Value, scanner.Value))
            return;

        SetMode((scanner.Value, component), msg.Mode, user.Value);
    }

    private void SetMode(Entity<GlovePrintScannerComponent> ent, GlovePrintScannerMode mode, EntityUid user)
    {
        if (ent.Comp.Mode == mode)
            return;

        ent.Comp.Mode = mode;
        Dirty(ent);
        _popup.PopupClient(Loc.GetString("glove-print-scanner-mode-selected", ("mode", GetModeName(mode))),
            ent, user);
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
