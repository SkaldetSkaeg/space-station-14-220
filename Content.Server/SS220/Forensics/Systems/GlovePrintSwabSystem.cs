// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Forensics;
using Content.Server.Popups;
using Content.Server.SS220.Forensics.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.SS220.Forensics;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Collects a single snapshot of glove traces into a swab for later analysis.
/// </summary>
public sealed class GlovePrintSwabSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly GlovePrintSystem _glovePrint = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlovePrintSwabComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<GlovePrintSwabComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        SubscribeLocalEvent<GlovePrintSwabComponent, GlovePrintSwabDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<GlovePrintSwabComponent, ExaminedEvent>(OnExamined);
    }

    private void OnAfterInteract(Entity<GlovePrintSwabComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (args.Target == null)
            return;

        // A handled interaction deposits contact evidence; collecting a sample must not contaminate it.
        StartCollection(ent, args.User, args.Target.Value);
    }

    private void OnUtilityVerb(Entity<GlovePrintSwabComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (ent.Comp.Used)
            return;

        var user = args.User;
        var target = args.Target;
        args.Verbs.Add(new UtilityVerb
        {
            Act = () => StartCollection(ent, user, target),
            IconEntity = GetNetEntity(ent),
            Text = Loc.GetString("glove-print-swab-verb-text"),
            Message = Loc.GetString("glove-print-swab-verb-message"),
            DoContactInteraction = false,
        });
    }

    private void StartCollection(Entity<GlovePrintSwabComponent> ent, EntityUid user, EntityUid target)
    {
        if (ent.Comp.Used)
        {
            _popup.PopupEntity(Loc.GetString("glove-print-swab-already-used"), ent, user);
            return;
        }

        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, ent.Comp.SampleDelay,
            new GlovePrintSwabDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        }))
            return;
    }

    private void OnDoAfter(Entity<GlovePrintSwabComponent> ent, ref GlovePrintSwabDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (ent.Comp.Used)
            return;

        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;
        if (Deleted(target))
            return;

        ent.Comp.GlovePrints = GetSample(target);
        ent.Comp.SourceName = Identity.Name(target, EntityManager);
        ent.Comp.Used = true;
        _label.Label(ent, ent.Comp.SourceName);
        args.Handled = true;
    }

    private List<GlovePrintTrace> GetSample(EntityUid target)
    {
        if (HasComp<GlovePrintComponent>(target))
        {
            if (!_glovePrint.TryGetPrint(target, out var print) || print == null)
                return [];

            var prototype = MetaData(target).EntityPrototype;
            EntProtoId? prototypeId = prototype == null ? (EntProtoId?)null : new EntProtoId(prototype.ID);
            return [new GlovePrintTrace(print, prototypeId)];
        }

        if (!TryComp<ForensicsComponent>(target, out var forensics))
            return [];

        return [.. forensics.GlovePrints];
    }

    private void OnExamined(Entity<GlovePrintSwabComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!ent.Comp.Used)
        {
            args.PushText(Loc.GetString("glove-print-swab-unused"));
            return;
        }

        args.PushText(Loc.GetString("glove-print-swab-used"));
        if (ent.Comp.SourceName == null)
            return;

        args.PushText(Loc.GetString("glove-print-swab-source", ("source", ent.Comp.SourceName)));
    }
}
