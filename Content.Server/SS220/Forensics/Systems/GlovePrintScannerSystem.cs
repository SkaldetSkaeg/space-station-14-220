// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Forensics;
using Content.Server.Popups;
using Content.Server.SS220.Forensics.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.SS220.Forensics;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Collects glove traces and compares them with a sample taken directly from gloves.
/// </summary>
public sealed class GlovePrintScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly GlovePrintSystem _glovePrint = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlovePrintScannerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<GlovePrintScannerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        SubscribeLocalEvent<GlovePrintScannerComponent, GlovePrintScannerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<GlovePrintScannerComponent, BoundUserInterfaceMessageAttempt>(OnUiMessageAttempt);
        SubscribeLocalEvent<GlovePrintScannerComponent, GlovePrintScannerClearMessage>(OnClear);
        SubscribeLocalEvent<GlovePrintScannerComponent, GlovePrintScannerRequestResultsMessage>(OnRequestResults);
    }

    private void OnAfterInteract(Entity<GlovePrintScannerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target == null)
            return;

        // A handled interaction deposits contact evidence; scanning must leave the target untouched.
        StartScan(ent, args.User, args.Target.Value);
    }

    private void OnUtilityVerb(Entity<GlovePrintScannerComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var user = args.User;
        var target = args.Target;
        args.Verbs.Add(new UtilityVerb
        {
            Act = () => StartScan(ent, user, target),
            IconEntity = GetNetEntity(ent),
            Text = Loc.GetString("glove-print-scanner-verb-text"),
            Message = Loc.GetString("glove-print-scanner-verb-message"),
            DoContactInteraction = false,
        });
    }

    private void StartScan(Entity<GlovePrintScannerComponent> ent, EntityUid user, EntityUid target)
    {
        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, ent.Comp.ScanDelay,
            new GlovePrintScannerDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        }))
            return;
    }

    private void OnDoAfter(Entity<GlovePrintScannerComponent> ent, ref GlovePrintScannerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;
        if (Deleted(target))
            return;

        args.Handled = true;
        if (HasComp<GlovePrintComponent>(target))
        {
            if (_glovePrint.TryGetPrint(target, out var sample))
                ent.Comp.Sample = sample;
            else
            {
                ent.Comp.Sample = null;
                _popup.PopupEntity(Loc.GetString("glove-print-scanner-no-sample"), ent, args.Args.User);
            }
        }
        else
        {
            ent.Comp.GlovePrints = TryComp<ForensicsComponent>(target, out var forensics)
                ? [.. forensics.GlovePrints]
                : [];
            ent.Comp.LastScannedName = MetaData(target).EntityName;
        }

        PlayComparison(ent);
        _ui.OpenUi(ent.Owner, GlovePrintScannerUiKey.Key, args.Args.User);
        SendResults(ent);
    }

    private void PlayComparison(Entity<GlovePrintScannerComponent> ent)
    {
        if (ent.Comp.Sample == null)
            return;

        if (ent.Comp.LastScannedName.Length == 0)
            return;

        var sound = ent.Comp.GlovePrints.Contains(ent.Comp.Sample)
            ? ent.Comp.SoundMatch
            : ent.Comp.SoundNoMatch;
        _audio.PlayPvs(sound, ent);
    }

    private void OnUiMessageAttempt(Entity<GlovePrintScannerComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (!args.UiKey.Equals(GlovePrintScannerUiKey.Key))
            return;

        if (_hands.IsHolding(args.Actor, ent.Owner))
            return;

        args.Cancel();
    }

    private void OnClear(Entity<GlovePrintScannerComponent> ent, ref GlovePrintScannerClearMessage args)
    {
        ent.Comp.GlovePrints.Clear();
        ent.Comp.Sample = null;
        ent.Comp.LastScannedName = string.Empty;
        SendResults(ent);
    }

    private void OnRequestResults(Entity<GlovePrintScannerComponent> ent, ref GlovePrintScannerRequestResultsMessage args)
    {
        _ui.ServerSendUiMessage(ent.Owner, GlovePrintScannerUiKey.Key, GetResults(ent), args.Actor);
    }

    private void SendResults(Entity<GlovePrintScannerComponent> ent)
    {
        _ui.ServerSendUiMessage(ent.Owner, GlovePrintScannerUiKey.Key, GetResults(ent));
    }

    private GlovePrintScannerResultsMessage GetResults(Entity<GlovePrintScannerComponent> ent)
    {
        return new GlovePrintScannerResultsMessage([.. ent.Comp.GlovePrints], ent.Comp.Sample, ent.Comp.LastScannedName);
    }
}
