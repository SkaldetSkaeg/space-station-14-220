// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.SS220.Teleport;
using Content.Shared.SS220.Teleport.Systems;
using Robust.Shared.Map;

namespace Content.Shared.SS220.SelfLinkedTeleport;

public abstract partial class SharedSelfLinkedTeleportSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
    [Dependency] private SharedTeleportSystem _teleport = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfLinkedTeleportComponent, TeleportTargetEvent>(OnTeleportTarget);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, GhostTeleportTargetEvent>(OnGhostTeleportTarget);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, TeleportUseAttemptEvent>(OnTeleportUseAttempt);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, ExaminedEvent>(OnExamined);
    }

    private void OnTeleportTarget(Entity<SelfLinkedTeleportComponent> ent, ref TeleportTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetDestination(ent, out var destination))
            return;

        var destinationCoordinates = Transform(destination).Coordinates;

        if (!TryTeleport(ent, args.Target, args.User, destinationCoordinates))
            return;

        args.Handled = true;
    }

    private void OnGhostTeleportTarget(Entity<SelfLinkedTeleportComponent> ent, ref GhostTeleportTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetDestination(ent, out var destination))
            return;

        var destinationCoordinates = Transform(destination).Coordinates;
        if (!_teleport.TryTeleportGhost(args.Target, destinationCoordinates))
            return;

        args.Handled = true;
    }

    private void OnTeleportUseAttempt(Entity<SelfLinkedTeleportComponent> ent, ref TeleportUseAttemptEvent args)
    {
        if (ent.Comp.LinkedEntity != null)
            return;

        _popup.PopupPredicted(
            Loc.GetString("linked-teleport-no-exit"),
            null,
            ent,
            args.User,
            PopupType.MediumCaution);

        args.Cancelled = true;
    }

    private void OnExamined(Entity<SelfLinkedTeleportComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.LinkedEntity != null)
            args.PushMarkup(Loc.GetString("linked-teleport-has-link"));
        else
            args.PushMarkup(Loc.GetString("linked-teleport-no-exit"));
    }

    private bool TryGetDestination(Entity<SelfLinkedTeleportComponent> ent, out EntityUid destination)
    {
        destination = ent.Comp.LinkedEntity ?? EntityUid.Invalid;

        if (!Exists(destination))
            return false;

        if (TerminatingOrDeleted(destination))
            return false;

        return true;
    }

    protected virtual bool TryTeleport(
        Entity<SelfLinkedTeleportComponent> ent,
        EntityUid target,
        EntityUid user,
        EntityCoordinates destinationCoordinates)
    {
        return _teleport.TryTeleport(ent, target, destinationCoordinates);
    }

    protected virtual void UpdateVisuals(Entity<SelfLinkedTeleportComponent> ent)
    {
        _appearance.SetData(ent, SelfLinkedVisuals.State, ent.Comp.LinkedEntity != null);

        if (_lights.TryGetLight(ent.Owner, out var light))
            _lights.SetEnabled(ent.Owner, ent.Comp.LinkedEntity != null, light);

        Dirty(ent);
    }
}
