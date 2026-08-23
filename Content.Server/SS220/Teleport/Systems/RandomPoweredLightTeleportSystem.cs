// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Teleport.Components;
using Content.Shared.Light.Components;
using Content.Shared.SS220.Teleport;
using Content.Shared.Station;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Teleport.Systems;

public sealed partial class RandomPoweredLightTeleportSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedStationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomPoweredLightTeleportComponent, TeleportTargetEvent>(OnTeleportTarget);
    }

    private void OnTeleportTarget(Entity<RandomPoweredLightTeleportComponent> ent, ref TeleportTargetEvent args)
    {
        var before = new BeforeTeleportTargetEvent(args.Target, args.User);
        RaiseLocalEvent(ent, ref before);

        TeleportToRandomLocation(args.Target);

        var teleported = new TargetTeleportedEvent(args.Target);
        RaiseLocalEvent(ent, ref teleported);
    }

    private void TeleportToRandomLocation(EntityUid target)
    {
        if (_station.GetStations().FirstOrNull() is not { } station)
            return;

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
            return;
        }

        var teleportLocation = _random.Pick(validLocations);
        _transform.SetCoordinates(target, Transform(target), teleportLocation);
    }
}
