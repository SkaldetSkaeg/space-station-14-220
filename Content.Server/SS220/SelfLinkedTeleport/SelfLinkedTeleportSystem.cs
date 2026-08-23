// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.SS220.SelfLinkedTeleport;
using Content.Shared.Whitelist;
using Robust.Shared.Map;

namespace Content.Server.SS220.SelfLinkedTeleport;

public sealed partial class SelfLinkedTeleportSystem : SharedSelfLinkedTeleportSystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfLinkedTeleportComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, ComponentRemove>(OnRemove);
    }

    private void OnMapInit(Entity<SelfLinkedTeleportComponent> ent, ref MapInitEvent args)//not sure about an event type
    {
        TryFindNewLink(ent);
    }

    private void OnRemove(Entity<SelfLinkedTeleportComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.LinkedEntity == null)
            return;

        if (TryComp<SelfLinkedTeleportComponent>(ent.Comp.LinkedEntity, out var linkedTeleporterComp))
            TryFindNewLink((ent.Comp.LinkedEntity.Value, linkedTeleporterComp));

        ent.Comp.LinkedEntity = null;
        UpdateVisuals(ent);
    }

    public bool TryFindNewLink(Entity<SelfLinkedTeleportComponent> ent)
    {
        ent.Comp.LinkedEntity = null;
        UpdateVisuals(ent);

        if (ent.Comp.LinkedEntity != null)
            return true;

        var locations = EntityQueryEnumerator<SelfLinkedTeleportComponent>();
        while (locations.MoveNext(out var uid, out var teleport))
        {
            if (uid == ent.Owner)//shouldn't be linked to itself
                continue;

            if (TerminatingOrDeleted(uid))
                continue;

            if (_whitelist.IsWhitelistFail(ent.Comp.WhitelistLinked, uid))
                continue;

            if (teleport.LinkedEntity != null)
                continue;

            ent.Comp.LinkedEntity = uid;
            teleport.LinkedEntity = ent;
            UpdateVisuals(ent);
            UpdateVisuals((uid, teleport));
            Dirty(uid, teleport);
            Dirty(ent);

            return true;
        }

        return false;
    }

    protected override bool TryTeleport(
        Entity<SelfLinkedTeleportComponent> ent,
        EntityUid target,
        EntityUid user,
        EntityUid destination,
        EntityCoordinates destinationCoordinates)
    {
        if (!base.TryTeleport(ent, target, user, destination, destinationCoordinates))
            return false;

        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(user):user} used linked telepoter {ToPrettyString(ent):teleport enter} and tried teleport {ToPrettyString(target):target} to {ToPrettyString(destination):teleport exit}");
        return true;
    }
}
