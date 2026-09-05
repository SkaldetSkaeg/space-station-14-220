using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.Teleportation.Triggers;

public sealed partial class TeleportOnVerbSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportOnVerbComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<TeleportOnVerbComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess)
            return;

        if (!IsUserAllowed(ent.Comp, args.User))
            return;

        var target = args.User;
        var attempt = new TeleportUseAttemptEvent(target, target, Mode: ent.Comp.Mode);
        RaiseLocalEvent(ent, ref attempt);

        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 11,
            Act = () => RequestTeleport(ent, target),
            Disabled = attempt.Cancelled,
            Text = Loc.GetString(ent.Comp.VerbText),
            Message = GetMessage(ent.Comp, attempt),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/open.svg.192dpi.png"))
        });
    }

    private void RequestTeleport(Entity<TeleportOnVerbComponent> ent, EntityUid target)
    {
        if (!IsUserAllowed(ent.Comp, target))
            return;

        var attempt = new TeleportUseAttemptEvent(target, target, Mode: ent.Comp.Mode);
        RaiseLocalEvent(ent, ref attempt);

        if (attempt.Cancelled)
            return;

        var request = new TeleportRequestEvent(target, target, ent.Comp.Mode);
        RaiseLocalEvent(ent, ref request);

        if (request.Handled)
            return;

        if (_net.IsClient)
            return;

        Log.Error($"TeleportOnVerb on {ToPrettyString(ent)} couldn't teleport {ToPrettyString(target)} " +
                  "because no teleport implementation handled the request");
    }

    private bool IsUserAllowed(TeleportOnVerbComponent component, EntityUid user)
    {
        if (_whitelist.IsWhitelistFail(component.UserWhitelist, user))
            return false;

        if (_whitelist.IsWhitelistPass(component.UserBlacklist, user))
            return false;

        return true;
    }

    private string? GetMessage(TeleportOnVerbComponent component, TeleportUseAttemptEvent attempt)
    {
        if (attempt.CancelReason is { } cancelReason)
            return Loc.GetString(cancelReason);

        if (component.EnabledMessage is { } enabledMessage)
            return Loc.GetString(enabledMessage);

        return null;
    }
}
