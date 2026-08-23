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
        if (ent.Comp.TargetsComponent is null)
            return false;

        if (!_componentFactory.TryGetRegistration(ent.Comp.TargetsComponent, out var registration))
            return false;

        var validLocations = new List<EntityCoordinates>();

        var query1 = EntityManager.AllEntityQueryEnumerator(registration.Type);
        while (query1.MoveNext(out var destination, out _))
        {
            if (_whitelist.IsWhitelistFail(ent.Comp.TeleportTargetWhitelist, destination))
                continue;

            validLocations.Add(Transform(destination).Coordinates);
        }

        if (validLocations.Count == 0)
            return false;

        var teleportLocation = _random.Pick(validLocations);

        if (!_teleport.TryTeleport(ent, target, user, teleportLocation))
            return false;

        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(user):user} used linked telepoter {ToPrettyString(ent):teleport} and tried teleport {ToPrettyString(target):target} to random location");
        return true;
    }
}
