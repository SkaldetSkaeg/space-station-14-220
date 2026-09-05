using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Network;

namespace Content.Shared.Teleportation.Triggers;

public sealed partial class TeleportOnVerbSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeVerb<Verb>(TeleportVerbType.Verb);
        SubscribeVerb<AlternativeVerb>(TeleportVerbType.Alternative);
        SubscribeVerb<InteractionVerb>(TeleportVerbType.Interaction);
        SubscribeVerb<ActivationVerb>(TeleportVerbType.Activation);
    }

    private void SubscribeVerb<TVerb>(TeleportVerbType type) where TVerb : Verb, new()
    {
        SubscribeLocalEvent<TeleportOnVerbComponent, GetVerbsEvent<TVerb>>(
            (Entity<TeleportOnVerbComponent> ent, ref GetVerbsEvent<TVerb> args) => OnGetVerbs(ent, ref args, type));
    }

    private void OnGetVerbs<TVerb>(Entity<TeleportOnVerbComponent> ent, ref GetVerbsEvent<TVerb> args, TeleportVerbType type)
        where TVerb : Verb, new()
    {
        if (ent.Comp.VerbType != type || !args.CanAccess)
            return;

        if (!IsUserAllowed(ent.Comp, args.User))
            return;

        var target = args.User;
        var attempt = new TeleportUseAttemptEvent(target, target);
        RaiseLocalEvent(ent, ref attempt);

        if (attempt.Cancelled && ent.Comp.HideWhenDisabled)
            return;

        args.Verbs.Add(new TVerb
        {
            Priority = ent.Comp.Priority,
            Act = () => RequestTeleport(ent, target),
            Disabled = attempt.Cancelled,
            Text = Loc.GetString(ent.Comp.VerbText),
            Message = GetMessage(ent.Comp, attempt),
            Icon = ent.Comp.VerbIcon,
            Category = ent.Comp.VerbCategory is { } category ? new VerbCategory(category, null) : null,
        });
    }

    private void RequestTeleport(Entity<TeleportOnVerbComponent> ent, EntityUid target)
    {
        if (!IsUserAllowed(ent.Comp, target))
            return;

        var attempt = new TeleportUseAttemptEvent(target, target);
        RaiseLocalEvent(ent, ref attempt);

        if (attempt.Cancelled)
            return;

        var request = new TeleportRequestEvent(target, target);
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
        return !_whitelist.IsWhitelistFail(component.UserWhitelist, user) &&
               !_whitelist.IsWhitelistPass(component.UserBlacklist, user);
    }

    private string? GetMessage(TeleportOnVerbComponent component, TeleportUseAttemptEvent attempt)
    {
        var message = attempt.Cancelled
            ? attempt.CancelReason ?? component.DisabledMessage
            : component.EnabledMessage;
        return message is { } key ? Loc.GetString(key) : null;
    }
}
