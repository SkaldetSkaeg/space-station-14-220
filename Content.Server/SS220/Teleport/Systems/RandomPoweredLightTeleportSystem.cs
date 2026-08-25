// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.SS220.Teleport.Components;
using Content.Shared.Light.Components;
using Content.Shared.SS220.Teleport;
using Content.Shared.SS220.Teleport.Systems;
using Content.Shared.Station;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Teleport.Systems;

public sealed partial class RandomPoweredLightTeleportSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private SharedTeleportSystem _teleport = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomPoweredLightTeleportComponent, TeleportTargetEvent>(OnTeleportTarget);
    }

    private void OnTeleportTarget(Entity<RandomPoweredLightTeleportComponent> ent, ref TeleportTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!TryTeleportToRandomLocation(ent, args.Target, args.User))
            return;

        args.Handled = true;
    }

    private bool TryTeleportToRandomLocation(EntityUid teleporter, EntityUid target, EntityUid user)
    {
        if (_station.GetStations().FirstOrNull() is not { } station)
        {
            Log.Warning($"RandomPoweredLightTeleport found no available stations for {ToPrettyString(target)}");
            return TryTeleportToFallback(teleporter, target, user);
        }

        var validDestinations = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<PoweredLightComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var transform))
        {
            if (_station.GetOwningStation(uid) != station)
                continue;

            if (TerminatingOrDeleted(uid))
                continue;

            if (!transform.Coordinates.IsValid(EntityManager))
                continue;

            validDestinations.Add(transform.Coordinates);
        }

        if (validDestinations.Count == 0)
        {
            Log.Warning($"RandomPoweredLightTeleport found no valid powered lights for {ToPrettyString(target)}");
            return TryTeleportToFallback(teleporter, target, user);
        }

        var destinationCoordinates = _random.Pick(validDestinations);
        return _teleport.TryTeleport(teleporter, target, user, destinationCoordinates);
    }

    private bool TryTeleportToFallback(EntityUid teleporter, EntityUid target, EntityUid user)
    {
        if (!_map.MapExists(_gameTicker.DefaultMap))
        {
            Log.Error($"RandomPoweredLightTeleport couldn't teleport {ToPrettyString(target)} because the default map doesn't exist");
            return false;
        }

        var mapUid = _map.GetMapOrInvalid(_gameTicker.DefaultMap);
        if (TerminatingOrDeleted(mapUid))
        {
            Log.Error($"RandomPoweredLightTeleport couldn't teleport {ToPrettyString(target)} because the default map is terminating or deleted");
            return false;
        }

        if (TryGetObserverSpawnPoint(_gameTicker.DefaultMap, out var observerCoordinates))
        {
            if (!_teleport.TryTeleport(teleporter, target, user, observerCoordinates))
            {
                Log.Error($"RandomPoweredLightTeleport couldn't teleport {ToPrettyString(target)} to an observer spawn point");
                return false;
            }

            Log.Warning($"RandomPoweredLightTeleport teleported {ToPrettyString(target)} to an observer spawn point on the default map");
            return true;
        }

        Log.Warning($"RandomPoweredLightTeleport couldn't find an observer spawn point on the default map and will use the map origin");

        var fallbackCoordinates = new EntityCoordinates(mapUid, Vector2.Zero);
        if (!_teleport.TryTeleport(teleporter, target, user, fallbackCoordinates))
        {
            Log.Error($"RandomPoweredLightTeleport couldn't teleport {ToPrettyString(target)} to the default map origin");
            return false;
        }

        Log.Warning($"RandomPoweredLightTeleport teleported {ToPrettyString(target)} to the default map origin");
        return true;
    }

    private bool TryGetObserverSpawnPoint(MapId mapId, out EntityCoordinates coordinates)
    {
        var spawnPoints = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var spawnPoint, out var transform))
        {
            if (spawnPoint.SpawnType != SpawnPointType.Observer)
                continue;

            if (transform.MapID != mapId)
                continue;

            if (TerminatingOrDeleted(uid))
                continue;

            if (!transform.Coordinates.IsValid(EntityManager))
                continue;

            spawnPoints.Add(transform.Coordinates);
        }

        if (spawnPoints.Count == 0)
        {
            coordinates = EntityCoordinates.Invalid;
            return false;
        }

        coordinates = _random.Pick(spawnPoints);
        return true;
    }
}
