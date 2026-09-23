// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Teleport.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.SS220.Teleport;
using Content.Shared.SS220.Teleport.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.SS220.Teleport.Systems;

/// <summary>
/// Selects a random unblocked floor tile using a base radius that other systems can modify.
/// </summary>
public sealed partial class RandomTeleportInRadiusSystem : EntitySystem
{
    [Dependency] private readonly SharedTeleportSystem _teleport = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomTeleportInRadiusComponent, TeleportRequestEvent>(OnTeleportRequest);
    }

    private void OnTeleportRequest(Entity<RandomTeleportInRadiusComponent> ent, ref TeleportRequestEvent args)
    {
        if (args.Handled || ent.Comp.Teleporting)
            return;

        if (EntityManager.IsQueuedForDeletion(ent))
            return;

        ent.Comp.Teleporting = true;
        try
        {
            if (!TryGetDestination(ent, args.Target, args.User, out var destination))
                return;

            args.Handled = _teleport.TryTeleport(ent, args.Target, destination);
        }
        finally
        {
            ent.Comp.Teleporting = false;
        }
    }

    private bool TryGetDestination(Entity<RandomTeleportInRadiusComponent> teleporter, EntityUid target, EntityUid user, out EntityCoordinates destination)
    {
        destination = EntityCoordinates.Invalid;
        var radiusModification = new TryModifyRadiusEvent(target, user, teleporter.Comp.Radius);
        RaiseLocalEvent(teleporter, ref radiusModification);
        var radius = radiusModification.Radius;
        // Modifiers can invalidate an otherwise valid configured radius.
        if (!float.IsFinite(radius) || radius <= 0)
            return false;

        if (!TryComp(target, out TransformComponent? xform))
            return false;

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return false;

        var gridUid = xform.GridUid.Value;
        var gridXform = Transform(gridUid);
        var origin = _transform.GetWorldPosition(xform);
        var originTile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        List<EntityCoordinates> candidates = [];
        foreach (var tile in _map.GetTilesIntersecting(gridUid, grid, new Circle(origin, radius)))
        {
            if (tile.GridIndices == originTile)
                continue;

            if (_turf.IsSpace(tile))
                continue;

            if (_turf.IsTileBlocked(gridUid, tile.GridIndices, CollisionGroup.MobMask, grid, gridXform))
                continue;

            candidates.Add(_map.GridTileToLocal(gridUid, grid, tile.GridIndices));
        }

        if (candidates.Count == 0)
            return false;

        destination = _random.Pick(candidates);
        return true;
    }
}
