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
        SubscribeLocalEvent<RandomPoweredLightTeleportComponent, GhostTeleportTargetEvent>(OnGhostTeleportTarget);
    }

    private void OnTeleportTarget(Entity<RandomPoweredLightTeleportComponent> ent, ref TeleportTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!TryTeleportToRandomLocation(ent, args.Target))
            return;

        args.Handled = true;
    }

    private void OnGhostTeleportTarget(Entity<RandomPoweredLightTeleportComponent> ent, ref GhostTeleportTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!TryTeleportGhostToRandomLocation(args.Target))
            return;

        args.Handled = true;
    }

    private bool TryTeleportToRandomLocation(EntityUid teleporter, EntityUid target)
    {
        if (!TryGetDestination(target, out var destinationCoordinates, out var destinationType))
            return false;

        if (!_teleport.TryTeleport(teleporter, target, destinationCoordinates))
        {
            LogTeleportFailure(target, destinationType);
            return false;
        }

        LogFallbackSuccess(target, destinationType);
        return true;
    }

    private bool TryTeleportGhostToRandomLocation(EntityUid target)
    {
        if (!TryGetDestination(target, out var destinationCoordinates, out var destinationType))
            return false;

        if (!_teleport.TryTeleportGhost(target, destinationCoordinates))
        {
            LogTeleportFailure(target, destinationType);
            return false;
        }

        LogFallbackSuccess(target, destinationType);
        return true;
    }

    private bool TryGetDestination(
        EntityUid target,
        out EntityCoordinates destinationCoordinates,
        out TeleportDestinationType destinationType)
    {
        if (_station.GetStations().FirstOrNull() is not { } station)
        {
            Log.Warning($"RandomPoweredLightTeleport found no available stations for {ToPrettyString(target)}");
            return TryGetFallbackDestination(target, out destinationCoordinates, out destinationType);
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
            return TryGetFallbackDestination(target, out destinationCoordinates, out destinationType);
        }

        destinationCoordinates = _random.Pick(validDestinations);
        destinationType = TeleportDestinationType.PoweredLight;
        return true;
    }

    private bool TryGetFallbackDestination(
        EntityUid target,
        out EntityCoordinates destinationCoordinates,
        out TeleportDestinationType destinationType)
    {
        if (!_map.MapExists(_gameTicker.DefaultMap))
        {
            Log.Error($"RandomPoweredLightTeleport couldn't teleport {ToPrettyString(target)} because the default map doesn't exist");
            destinationCoordinates = EntityCoordinates.Invalid;
            destinationType = default;
            return false;
        }

        var mapUid = _map.GetMapOrInvalid(_gameTicker.DefaultMap);
        if (TerminatingOrDeleted(mapUid))
        {
            Log.Error($"RandomPoweredLightTeleport couldn't teleport {ToPrettyString(target)} because the default map is terminating or deleted");
            destinationCoordinates = EntityCoordinates.Invalid;
            destinationType = default;
            return false;
        }

        if (TryGetObserverSpawnPoint(_gameTicker.DefaultMap, out destinationCoordinates))
        {
            destinationType = TeleportDestinationType.ObserverSpawn;
            return true;
        }

        Log.Warning($"RandomPoweredLightTeleport couldn't find an observer spawn point on the default map and will use the map origin");

        destinationCoordinates = new EntityCoordinates(mapUid, Vector2.Zero);
        destinationType = TeleportDestinationType.MapOrigin;
        return true;
    }

    private void LogTeleportFailure(EntityUid target, TeleportDestinationType destinationType)
    {
        switch (destinationType)
        {
            case TeleportDestinationType.ObserverSpawn:
                Log.Error($"RandomPoweredLightTeleport couldn't teleport {ToPrettyString(target)} to an observer spawn point");
                break;
            case TeleportDestinationType.MapOrigin:
                Log.Error($"RandomPoweredLightTeleport couldn't teleport {ToPrettyString(target)} to the default map origin");
                break;
        }
    }

    private void LogFallbackSuccess(EntityUid target, TeleportDestinationType destinationType)
    {
        switch (destinationType)
        {
            case TeleportDestinationType.ObserverSpawn:
                Log.Warning($"RandomPoweredLightTeleport teleported {ToPrettyString(target)} to an observer spawn point on the default map");
                break;
            case TeleportDestinationType.MapOrigin:
                Log.Warning($"RandomPoweredLightTeleport teleported {ToPrettyString(target)} to the default map origin");
                break;
        }
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

    private enum TeleportDestinationType : byte
    {
        PoweredLight,
        ObserverSpawn,
        MapOrigin
    }
}
