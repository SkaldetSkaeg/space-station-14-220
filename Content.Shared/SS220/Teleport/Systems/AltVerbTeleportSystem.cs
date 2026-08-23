// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.SS220.Teleport.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Teleport.Systems;

public sealed partial class AltVerbTeleportSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AltVerbTeleportComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerb);
        SubscribeLocalEvent<AltVerbTeleportComponent, AltVerbTeleportDoAfterEvent>(OnAltVerbTeleportDoAfter);
    }

    private void OnGetAlternativeVerb(Entity<AltVerbTeleportComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (_hands.GetActiveItem((args.User, args.Hands)) != ent.Owner)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, args.User))
        {
            if (ent.Comp.WhitelistRejectedLoc != null)
                _popup.PopupPredicted(Loc.GetString(ent.Comp.WhitelistRejectedLoc), ent, args.User, PopupType.MediumCaution);

            return;
        }

        if (_whitelist.IsWhitelistPass(ent.Comp.UserBlacklist, args.User))
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

        var ev = new TeleportUseAttemptEvent(user, user);
        RaiseLocalEvent(ent, ref ev);

        if (ev.Cancelled)
            return false;

        if (ent.Comp.TeleportDoAfterTime is null)
        {
            SendTeleporting(ent, user);
            return true;
        }

        var teleportDoAfter = new DoAfterArgs(EntityManager, user, ent.Comp.TeleportDoAfterTime.Value, new AltVerbTeleportDoAfterEvent(), eventTarget: ent, target: user)
        {
            BreakOnDamage = ent.Comp.DamageThreshold != null,
            DamageThreshold = ent.Comp.DamageThreshold ?? 0,
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (_doAfter.TryStartDoAfter(teleportDoAfter))
        {
            _popup.PopupPredicted(Loc.GetString("teleport-user-started"), ent, user, PopupType.MediumCaution);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void OnAltVerbTeleportDoAfter(Entity<AltVerbTeleportComponent> ent, ref AltVerbTeleportDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        if (_hands.GetActiveItem(args.User) != ent.Owner)
            return;

        SendTeleporting(ent, target);
    }

    private void SendTeleporting(Entity<AltVerbTeleportComponent> ent, EntityUid user)
    {
        var ev = new TeleportTargetEvent(user, user);
        RaiseLocalEvent(ent, ref ev);
    }
}
