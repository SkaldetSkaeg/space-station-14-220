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

    private void OnMapInit(Entity<SelfLinkedTeleportComponent> ent, ref MapInitEvent args)
    {
        TryFindNewLink(ent);
    }

    private void OnRemove(Entity<SelfLinkedTeleportComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.LinkedEntity is not { } linkedTeleporter)
            return;

        if (TryComp<SelfLinkedTeleportComponent>(linkedTeleporter, out var linkedTeleporterComp))
        {
            linkedTeleporterComp.LinkedEntity = null;
            TryFindNewLink((linkedTeleporter, linkedTeleporterComp));
        }

        ent.Comp.LinkedEntity = null;
        UpdateVisuals(ent);
    }

    private bool TryFindNewLink(Entity<SelfLinkedTeleportComponent> ent)
    {
        if (ent.Comp.LinkedEntity != null)
            return false;

        UpdateVisuals(ent);

        var query = EntityQueryEnumerator<SelfLinkedTeleportComponent>();
        while (query.MoveNext(out var candidate, out var candidateComp))
        {
            if (candidate == ent.Owner)
                continue;

            if (TerminatingOrDeleted(candidate))
                continue;

            if (_whitelist.IsWhitelistFail(ent.Comp.LinkWhitelist, candidate))
                continue;

            if ((!ent.Comp.CanLinkToOtherMaps || !candidateComp.CanLinkToOtherMaps) &&
                Transform(ent).MapID != Transform(candidate).MapID)
            {
                continue;
            }

            if (candidateComp.LinkedEntity != null)
                continue;

            ent.Comp.LinkedEntity = candidate;
            candidateComp.LinkedEntity = ent;
            UpdateVisuals(ent);
            UpdateVisuals((candidate, candidateComp));

            return true;
        }

        return false;
    }

    protected override bool TryTeleport(
        Entity<SelfLinkedTeleportComponent> ent,
        EntityUid target,
        EntityUid user,
        EntityCoordinates destinationCoordinates)
    {
        if (!base.TryTeleport(ent, target, user, destinationCoordinates))
            return false;

        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(user):user} used linked teleporter {ToPrettyString(ent):teleport enter} and teleported {ToPrettyString(target):target} to {ToPrettyString(ent.Comp.LinkedEntity):teleport exit}");
        return true;
    }
}
