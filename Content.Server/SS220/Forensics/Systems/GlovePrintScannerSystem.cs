// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Forensics;
using Content.Server.Popups;
using Content.Server.SS220.Forensics.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.SS220.Forensics;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Collects deposited material evidence or an item's own material reference into a new physical sample.
/// </summary>
public sealed class GlovePrintScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly GlovePrintSystem _glovePrint = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly MicroFiberSystem _microFiber = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlovePrintScannerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<GlovePrintScannerComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        SubscribeLocalEvent<GlovePrintScannerComponent, GlovePrintScannerDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<GlovePrintScannerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target == null)
            return;

        // A handled interaction deposits contact evidence; scanning must not contaminate the target.
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
        if (ent.Comp.Mode == GlovePrintScannerMode.OwnPrint &&
            !_glovePrint.TryGetPrint(target, out _) && !_microFiber.TryGetTrace(target, out _))
        {
            _popup.PopupEntity(Loc.GetString("glove-print-scanner-no-own-print"), ent, user);
            return;
        }

        var doAfterEvent = new GlovePrintScannerDoAfterEvent
        {
            Mode = ent.Comp.Mode,
        };
        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, ent.Comp.ScanDelay,
            doAfterEvent, ent, target: target, used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            DuplicateCondition = DuplicateConditions.SameTool,
            CancelDuplicate = false,
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

        if (args.Mode != ent.Comp.Mode)
        {
            _popup.PopupEntity(Loc.GetString("glove-print-scanner-mode-changed"), ent, args.User);
            return;
        }

        if (!TryGetSample(target, args.Mode, out var traces, out var microFibers))
        {
            _popup.PopupEntity(Loc.GetString("glove-print-scanner-no-own-print"), ent, args.User);
            return;
        }

        var swab = Spawn(ent.Comp.SwabPrototype, Transform(args.User).Coordinates);
        if (!TryComp<GlovePrintSwabComponent>(swab, out var sample))
        {
            Log.Error($"Glove trace scanner output {ToPrettyString(swab)} has no GlovePrintSwabComponent.");
            Del(swab);
            return;
        }

        sample.GlovePrints = traces;
        sample.MicroFibers = microFibers;
        sample.Used = true;
        sample.SourceName = MetaData(target).EntityName;
        _metadata.SetEntityName(swab, Loc.GetString("glove-print-swab-label", ("source", sample.SourceName)));
        _hands.PickupOrDrop(args.User, swab, checkActionBlocker: false);
        args.Handled = true;
    }

    private bool TryGetSample(EntityUid target, GlovePrintScannerMode mode, out List<GlovePrintTrace> traces,
        out List<MicroFiberTrace> microFibers)
    {
        traces = [];
        microFibers = [];
        if (mode == GlovePrintScannerMode.SurfaceTraces)
        {
            if (TryComp<ForensicsComponent>(target, out var forensics))
            {
                traces = [.. forensics.GlovePrints];
                microFibers = [.. forensics.MicroFibers];
            }

            return true;
        }

        if (_glovePrint.TryGetPrint(target, out var print) && print != null)
        {
            var prototype = MetaData(target).EntityPrototype;
            EntProtoId? prototypeId = prototype == null ? (EntProtoId?)null : new EntProtoId(prototype.ID);
            traces = [new GlovePrintTrace(print, prototypeId)];
        }

        if (!_microFiber.TryGetTrace(target, out var fiber))
            return traces.Count != 0;

        microFibers = [fiber];
        return true;
    }
}
