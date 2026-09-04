// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.InnerHandToggleable;
using Content.Shared.SS220.Roles;
using Content.Shared.SS220.StuckOnEquip;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CultYogg.Cultists;

/// <summary>
/// Removes only explicitly tagged cult equipment when its owner is cleansed or loses their cult role.
/// </summary>
public sealed partial class CultYoggEquipmentSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedStuckOnEquipSystem _stuck = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private TagSystem _tags = default!;

    public static readonly ProtoId<TagPrototype> EquipmentTag = "CultYoggEquipment";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleRemoved);
    }

    private void OnRoleRemoved(RoleRemovedEvent args)
    {
        if (args.Mind.OwnedEntity is not { } owner
            || !HasComp<CultYoggComponent>(owner)
            || _roles.MindHasRole<CultYoggRoleComponent>(args.MindId, out _))
            return;

        DropCultEquipment(owner);
    }

    public bool DropCultEquipment(EntityUid owner)
    {
        var dropped = false;
        foreach (var item in _inventory.GetHandOrInventoryEntities(owner).ToArray())
        {
            dropped |= TryDropItem(owner, item);
        }

        return DropHiddenEquipment(owner) || dropped;
    }

    private bool TryDropItem(EntityUid owner, EntityUid item)
    {
        if (!_tags.HasTag(item, EquipmentTag))
            return false;

        if (TryComp<StuckOnEquipComponent>(item, out var stuck))
            return _stuck.TryRemoveItem((item, stuck), owner, force: true);

        if (_inventory.TryGetContainingSlot(item, out var slot))
            return _inventory.TryUnequip(owner, slot.Name, silent: true, force: true, triggerHandContact: true);

        if (!_hands.IsHolding(owner, item, out _)
            || !_containers.TryGetContainingContainer((item, null, null), out var container))
            return false;

        return _containers.Remove(item, container, force: true);
    }

    private bool DropHiddenEquipment(EntityUid owner)
    {
        if (!TryComp<InnerHandToggleableComponent>(owner, out var inner))
            return false;

        var dropped = false;
        foreach (var (hand, info) in inner.HandsContainers)
        {
            dropped |= TryDropHiddenItem(owner, inner, hand, info);
        }

        return dropped;
    }

    private bool TryDropHiddenItem(EntityUid owner, InnerHandToggleableComponent inner, string hand, InnerContainerInfo info)
    {
        if (info.InnerItemUid is not { } item
            || info.Container == null
            || !_tags.HasTag(item, EquipmentTag)
            || !info.Container.Contains(item)
            || !_containers.Remove(item, info.Container, force: true))
            return false;

        info.InnerItemUid = null;
        if (_hands.GetActiveHand(owner) == hand)
            _actions.RemoveAction(inner.ActionEntity);

        return true;
    }
}
