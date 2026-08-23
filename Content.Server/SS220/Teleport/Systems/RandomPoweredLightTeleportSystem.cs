// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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
            return false;

        var validLocations = new List<EntityCoordinates>();
        var locations = EntityQueryEnumerator<PoweredLightComponent, TransformComponent>();

        while (locations.MoveNext(out var uid, out _, out var transform))
        {
            if (_station.GetOwningStation(uid) != station)
                continue;

            validLocations.Add(transform.Coordinates);
        }

        if (validLocations.Count == 0)
        {
            Log.Warning($"RandomPoweredLightTeleport couldn't teleport {ToPrettyString(target)} because there were no valid locations");
            return false;
        }

        var teleportLocation = _random.Pick(validLocations);
        return _teleport.TryTeleport(teleporter, target, user, teleportLocation);
    }
}
