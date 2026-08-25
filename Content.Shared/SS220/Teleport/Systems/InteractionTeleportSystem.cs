// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Popups;
using Content.Shared.SS220.Teleport.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Teleport.Systems;

public sealed partial class InteractionTeleportSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteractionTeleportComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<InteractionTeleportComponent, CanDropTargetEvent>(OnCanDropTarget);
        SubscribeLocalEvent<InteractionTeleportComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<InteractionTeleportComponent, InteractionTeleportDoAfterEvent>(OnTeleportDoAfter);
    }

    private void OnGetVerb(Entity<InteractionTeleportComponent> ent, ref GetVerbsEvent<Verb> args)//Not sure maybe it should be "InteractionVerb"
    {
        if (!args.CanAccess)
            return;

        if (!args.CanInteract)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.TargetWhitelist, args.User))
        {
            if (ent.Comp.WhitelistRejectedLoc != null)
                _popup.PopupPredicted(Loc.GetString(ent.Comp.WhitelistRejectedLoc), ent, args.User, PopupType.MediumCaution);
            return;
        }

        if (_whitelist.IsWhitelistPass(ent.Comp.TargetBlacklist, args.User))
            return;

        var user = args.User;

        var teleportVerb = new Verb
        {
            Text = Loc.GetString("teleport-use-verb"),
            Act = () =>
            {
                TryStartTeleport(ent, user, user);
            }
        };
        args.Verbs.Add(teleportVerb);
    }

    private void OnCanDropTarget(Entity<InteractionTeleportComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.CanDrop = !_whitelist.IsWhitelistFail(ent.Comp.TargetWhitelist, args.Dragged) &&
                       !_whitelist.IsWhitelistPass(ent.Comp.TargetBlacklist, args.Dragged);
    }

    private void OnDragDropTarget(Entity<InteractionTeleportComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.TargetWhitelist, args.Dragged))
        {
            if (ent.Comp.WhitelistRejectedLoc != null)
                _popup.PopupPredicted(Loc.GetString(ent.Comp.WhitelistRejectedLoc), ent, args.User, PopupType.MediumCaution);
            return;
        }

        if (_whitelist.IsWhitelistPass(ent.Comp.TargetBlacklist, args.Dragged))
            return;

        if (!TryStartTeleport(ent, args.Dragged, args.User))
            return;

        args.Handled = true;
    }

    private bool TryStartTeleport(Entity<InteractionTeleportComponent> ent, EntityUid target, EntityUid user)
    {
        var attemptEvent = new TeleportUseAttemptEvent(target, user);
        RaiseLocalEvent(ent, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return false;

        if (ent.Comp.TeleportDoAfterTime is null)
            return SendTeleporting(ent, target, user);

        var teleportDoAfter = new DoAfterArgs(EntityManager, user, ent.Comp.TeleportDoAfterTime.Value, new InteractionTeleportDoAfterEvent(), ent, target)
        {
            BreakOnDamage = ent.Comp.DamageThreshold != null,
            DamageThreshold = ent.Comp.DamageThreshold ?? 0,
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (!_doAfter.TryStartDoAfter(teleportDoAfter))
            return false;

        _popup.PopupPredicted(Loc.GetString("teleport-user-started"), ent, user, PopupType.MediumCaution);
        return true;
    }

    private void OnTeleportDoAfter(Entity<InteractionTeleportComponent> ent, ref InteractionTeleportDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target == null)
            return;

        SendTeleporting(ent, args.Target.Value, args.User);
    }

    private bool SendTeleporting(Entity<InteractionTeleportComponent> ent, EntityUid target, EntityUid user)
    {
        var teleportEvent = new TeleportTargetEvent(target, user);
        RaiseLocalEvent(ent, ref teleportEvent);
        return teleportEvent.Handled;
    }
}
