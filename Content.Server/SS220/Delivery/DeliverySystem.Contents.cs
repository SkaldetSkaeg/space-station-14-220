// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Delivery;
using Content.Shared.EntityTable;
using Content.Shared.SS220.EntityTable;
using Robust.Shared.Map;

namespace Content.Server.Delivery;

public sealed partial class DeliverySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private void FillContents(Entity<DeliveryComponent> ent, string? job, string? species)
    {
        if (ent.Comp.Contents == null)
            return;

        var context = new EntityTableContext(new Dictionary<string, object>
        {
            { JobSelector.JobContextKey, job ?? string.Empty },
            { SpeciesSelector.SpeciesContextKey, species ?? string.Empty },
        });

        var xform = Transform(ent);
        var container = _container.GetContainer(ent, ent.Comp.Container);
        var coordinates = new EntityCoordinates(ent, System.Numerics.Vector2.Zero);
        foreach (var prototype in _entityTable.GetSpawns(ent.Comp.Contents, ctx: context))
        {
            var item = Spawn(prototype, coordinates);
            if (_container.Insert(item, container, containerXform: xform))
                continue;

            Log.Error($"Failed to insert {ToPrettyString(item)} into delivery {ToPrettyString(ent)}.");
            _transform.AttachToGridOrMap(item);
            break;
        }
    }
}
