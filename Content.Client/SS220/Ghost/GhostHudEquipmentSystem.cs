// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Overlays;
using Content.Shared.Ghost;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Client.SS220.Ghost;

/// <summary>
/// Keeps ghost HUDs controlled by the observer's components and preserves equipment HUDs for living viewers.
/// </summary>
public abstract class GhostHudEquipmentSystem<T> : EquipmentHudSystem<T> where T : IComponent
{
    protected override void OnRefreshEquipmentHud(
        Entity<T> ent,
        ref InventoryRelayedEvent<RefreshEquipmentHudEvent<T>> args)
    {
        if (HasComp<GhostComponent>(args.Owner))
            return;

        base.OnRefreshEquipmentHud(ent, ref args);
    }
}
