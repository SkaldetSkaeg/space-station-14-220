// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.SS220.Teleport;
using Content.Shared.SS220.Teleport.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.SS220.RandomTeleport;

public sealed partial class RandomTeleportSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedTeleportSystem _teleport = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomTeleportComponent, TeleportTargetEvent>(OnTeleportTarget);
    }

    private void OnTeleportTarget(Entity<RandomTeleportComponent> ent, ref TeleportTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!TryTeleport(ent, args.Target, args.User))
            return;

        args.Handled = true;
    }

    private bool TryTeleport(Entity<RandomTeleportComponent> ent, EntityUid target, EntityUid user)
    {
        if (ent.Comp.DestinationComponent is null)
            return false;

        if (!_componentFactory.TryGetRegistration(ent.Comp.DestinationComponent, out var registration))
            return false;

        var validDestinations = new List<EntityCoordinates>();

        var query = EntityManager.AllEntityQueryEnumerator(registration.Type);
        while (query.MoveNext(out var destination, out _))
        {
            if (TerminatingOrDeleted(destination))
                continue;

            if (_whitelist.IsWhitelistFail(ent.Comp.DestinationWhitelist, destination))
                continue;

            var coordinates = Transform(destination).Coordinates;
            if (!coordinates.IsValid(EntityManager))
                continue;

            validDestinations.Add(coordinates);
        }

        if (validDestinations.Count == 0)
            return false;

        var destinationCoordinates = _random.Pick(validDestinations);

        if (!_teleport.TryTeleport(ent, target, user, destinationCoordinates))
            return false;

        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(user):user} used teleporter {ToPrettyString(ent):teleport} and teleported {ToPrettyString(target):target} to random location");
        return true;
    }
}
