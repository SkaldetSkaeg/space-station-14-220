// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.SS220.Teleport.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Network;

namespace Content.Shared.SS220.Teleport.Systems;

public sealed partial class AltVerbTeleportSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AltVerbTeleportComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerb);
        SubscribeLocalEvent<AltVerbTeleportComponent, AltVerbTeleportDoAfterEvent>(OnAltVerbTeleportDoAfter);
    }

    private void OnGetAlternativeVerb(Entity<AltVerbTeleportComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess)
            return;

        if (!args.CanInteract)
            return;

        if (args.Hands == null)
            return;

        if (_hands.GetActiveItem((args.User, args.Hands)) != ent.Owner)
            return;

        if (!IsUserAllowed(ent, args.User))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(ent.Comp.VerbText),
            IconEntity = GetNetEntity(ent.Owner),
            Act = () => TryStartTeleport(ent, user)
        });
    }

    private bool TryStartTeleport(Entity<AltVerbTeleportComponent> ent, EntityUid user)
    {
        if (_hands.GetActiveItem(user) != ent.Owner)
            return false;

        if (!IsUserAllowed(ent, user))
        {
            ShowWhitelistRejectedPopup(ent, user);
            return false;
        }

        var attemptEvent = new TeleportUseAttemptEvent(user, user);
        RaiseLocalEvent(ent, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return false;

        if (ent.Comp.TeleportDoAfterTime is null)
            return TrySendTeleporting(ent, user);

        var teleportDoAfter = new DoAfterArgs(EntityManager, user, ent.Comp.TeleportDoAfterTime.Value, new AltVerbTeleportDoAfterEvent(), eventTarget: ent, target: user)
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

        _popup.PopupPredicted(
            Loc.GetString("teleport-user-started"),
            null,
            user,
            user,
            PopupType.MediumCaution);
        return true;
    }

    private bool IsUserAllowed(Entity<AltVerbTeleportComponent> ent, EntityUid user)
    {
        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, user))
            return false;

        if (_whitelist.IsWhitelistPass(ent.Comp.UserBlacklist, user))
            return false;

        return true;
    }

    private void ShowWhitelistRejectedPopup(Entity<AltVerbTeleportComponent> ent, EntityUid user)
    {
        if (!_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, user))
            return;

        if (ent.Comp.WhitelistRejectedLoc is not { } rejectedLoc)
            return;

        _popup.PopupPredicted(
            Loc.GetString(rejectedLoc),
            null,
            ent,
            user,
            PopupType.MediumCaution);
    }

    private void OnAltVerbTeleportDoAfter(Entity<AltVerbTeleportComponent> ent, ref AltVerbTeleportDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        if (_hands.GetActiveItem(args.User) != ent.Owner)
            return;

        if (TrySendTeleporting(ent, target))
            return;

        if (_net.IsClient)
            return;

        Log.Error($"AltVerbTeleport on {ToPrettyString(ent)} couldn't teleport {ToPrettyString(target)} because no teleport implementation handled the request");
    }

    private bool TrySendTeleporting(Entity<AltVerbTeleportComponent> ent, EntityUid user)
    {
        var teleportEvent = new TeleportTargetEvent(user, user);
        RaiseLocalEvent(ent, ref teleportEvent);
        return teleportEvent.Handled;
    }
}
