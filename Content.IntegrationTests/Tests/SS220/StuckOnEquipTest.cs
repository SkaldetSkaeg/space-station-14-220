// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.IntegrationTests.Fixtures;
using Content.Server.Administration.Managers;
using Content.Server.Disposal.Unit;
using Content.Shared.Clothing.Components;
using Content.Shared.Ghost;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.StuckOnEquip;
using Content.Shared.Standing;
using Content.Shared.Strip.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.SS220;

public sealed class StuckOnEquipTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [Test]
    public async Task AdminBodyRemainsStuckButGhostCanDrop()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var hands = SEntMan.System<SharedHandsSystem>();
            var stuckSystem = SEntMan.System<SharedStuckOnEquipSystem>();
            var human = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var ghost = SEntMan.SpawnEntity("AdminObserver", map.GridCoords);
            Server.PlayerMan.SetAttachedEntity(ServerSession, human);
            Server.ResolveDependency<IAdminManager>().PromoteHost(ServerSession);

            var item = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            var stuck = SEntMan.AddComponent<StuckOnEquipComponent>(item);
            stuck.InHandItem = true;
            Assert.That(hands.TryPickup(human, item));
            Assert.That(stuck.IsStuck);
            Assert.That(hands.TryDrop(human, item), Is.False);
            Assert.That(stuckSystem.TryAdminGhostRemove(human, item), Is.False);

            // A ghost without interaction permission must not be able to release the item either.
            SEntMan.System<SharedGhostSystem>().SetCanGhostInteract(ghost, false);
            Assert.That(stuckSystem.TryAdminGhostRemove(ghost, item), Is.False);
            Assert.That(stuck.IsStuck);
            SEntMan.System<SharedGhostSystem>().SetCanGhostInteract(ghost, true);

            Assert.That(stuckSystem.TryAdminGhostRemove(ghost, item));
            Assert.That(stuck.IsStuck, Is.False);
            Assert.That(hands.TryPickup(ghost, item));
            Assert.That(stuck.IsStuck, "Aghost must not change how items become stuck.");
            Assert.That(hands.TryDrop(ghost, item));
            Assert.That(stuck.IsStuck, Is.False);
            Assert.That(hands.TryPickup(human, item));
            Assert.That(hands.TryDrop(human, item), Is.False);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task AdminGhostStripsThroughEquipmentUi(bool inHand)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var hands = SEntMan.System<SharedHandsSystem>();
            var inventory = SEntMan.System<InventorySystem>();
            var human = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var ghost = SEntMan.SpawnEntity("AdminObserver", map.GridCoords);
            Server.PlayerMan.SetAttachedEntity(ServerSession, ghost);
            Server.ResolveDependency<IAdminManager>().PromoteHost(ServerSession);

            var item = SEntMan.SpawnEntity(inHand ? "Crowbar" : "ClothingOuterHardsuitCultYogg", map.GridCoords);
            var stuck = SEntMan.EnsureComponent<StuckOnEquipComponent>(item);
            string slot;
            ToggleableClothingComponent suit = null;
            if (inHand)
            {
                stuck.InHandItem = true;
                Assert.That(hands.TryPickup(human, item));
                Assert.That(hands.IsHolding(human, item, out slot));
            }
            else
            {
                slot = "outerClothing";
                Assert.That(inventory.TryEquip(human, item, slot, force: true));
                suit = SEntMan.GetComponent<ToggleableClothingComponent>(item);
                Assert.That(inventory.TryEquip(human, suit.ClothingUid.Value, suit.Slot, force: true));
                Assert.That(inventory.CanUnequip(human, slot, out _), Is.False);
            }

            Assert.That(stuck.IsStuck);
            var message = new StrippingSlotButtonPressed(slot, inHand) { Actor = ghost };
            SEntMan.EventBus.RaiseLocalEvent(human, message);

            Assert.That(hands.IsHolding(ghost, item));
            Assert.That(stuck.IsStuck, Is.EqualTo(inHand));
            if (suit != null)
            {
                Assert.That(inventory.TryGetSlotEntity(human, slot, out _), Is.False);
                Assert.That(inventory.TryGetSlotEntity(human, suit.Slot, out _), Is.False);
                Assert.That(SEntMan.System<SharedContainerSystem>().TryGetContainingContainer(
                    (suit.ClothingUid.Value, null, null), out var helmetContainer));
                Assert.That(helmetContainer, Is.SameAs(suit.Container));
            }
            Assert.That(hands.TryDrop(ghost, item));
        });
    }

    [Test]
    public async Task DisposalExitAndFallingKeepHandItemStuck()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var hands = SEntMan.System<SharedHandsSystem>();
            var containers = SEntMan.System<SharedContainerSystem>();
            var human = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var item = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            var stuck = SEntMan.AddComponent<StuckOnEquipComponent>(item);
            stuck.InHandItem = true;
            Assert.That(hands.TryPickup(human, item));

            var holder = SEntMan.SpawnEntity("DisposalHolder", map.GridCoords);
            var holderComp = SEntMan.GetComponent<DisposalHolderComponent>(holder);
            holderComp.CurrentDirection = Direction.East;
            Assert.That(containers.Insert(human, holderComp.Container));
            Assert.That(stuck.IsStuck);

            SEntMan.System<DisposableSystem>().ExitDisposals(holder);
            var fall = new DropHandItemsEvent();
            SEntMan.EventBus.RaiseLocalEvent(human, ref fall);
            Assert.That(hands.IsHolding(human, item));
            Assert.That(stuck.IsStuck);
            Assert.That(hands.TryDrop(human, item), Is.False);
        });
    }

    [Test]
    public async Task FailedRemovalAndStorageInsertionPreserveLock()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var hands = SEntMan.System<SharedHandsSystem>();
            var containers = SEntMan.System<SharedContainerSystem>();
            var system = SEntMan.System<SharedStuckOnEquipSystem>();
            var human = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var ghost = SEntMan.SpawnEntity("AdminObserver", map.GridCoords);
            var item = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            var stuck = SEntMan.AddComponent<StuckOnEquipComponent>(item);
            stuck.InHandItem = true;
            Assert.That(hands.TryPickup(human, item));

            var storage = containers.EnsureContainer<ContainerSlot>(human, "test-inner-hand");
            var blocker = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            Assert.That(containers.Insert(blocker, storage));
            Assert.That(system.TryInsertUnstuckItem((item, stuck), storage), Is.False);
            Assert.That(stuck.IsStuck);
            Assert.That(hands.IsHolding(human, item));

            Assert.That(containers.Remove(blocker, storage));
            Assert.That(system.TryInsertUnstuckItem((item, stuck), storage));
            Assert.That(stuck.IsStuck, Is.False);
            Assert.That(hands.TryPickup(human, item));
            Assert.That(stuck.IsStuck);

            // Releasing StuckOnEquip must not bypass an independent removal restriction.
            SEntMan.AddComponent<UnremoveableComponent>(item);
            Assert.That(system.TryAdminGhostRemove(ghost, item), Is.False);
            Assert.That(stuck.IsStuck);
            Assert.That(hands.IsHolding(human, item));
            SEntMan.RemoveComponent<UnremoveableComponent>(item);

            Assert.That(containers.TryGetContainingContainer((item, null, null), out var hand));
            Assert.That(containers.Remove(item, hand, force: true));
            Assert.That(stuck.IsStuck, Is.False, "Forced removal must not leave a stale lock.");
            Assert.That(containers.Insert(item, storage));
            Assert.That(containers.Remove(item, storage));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task DeathHandlesHandsWithoutInventory(bool dropOnDeath)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var hands = SEntMan.System<SharedHandsSystem>();
            var human = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            SEntMan.RemoveComponent<InventoryComponent>(human);
            var item = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            var stuck = SEntMan.AddComponent<StuckOnEquipComponent>(item);
            stuck.InHandItem = true;
            stuck.ShouldDropOnDeath = dropOnDeath;
            Assert.That(hands.TryPickup(human, item));

            SEntMan.System<MobStateSystem>().ChangeMobState(human, MobState.Dead);
            Assert.That(hands.IsHolding(human, item), Is.EqualTo(!dropOnDeath));
            Assert.That(stuck.IsStuck, Is.EqualTo(!dropOnDeath));
        });
    }

    [Test]
    public async Task PocketsRemainFreeAndCultCleanupStillRemovesThem()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var inventory = SEntMan.System<InventorySystem>();
            var system = SEntMan.System<SharedStuckOnEquipSystem>();
            var human = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var item = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            var stuck = SEntMan.AddComponent<StuckOnEquipComponent>(item);
            Assert.That(inventory.TryEquip(human, item, "pocket1", force: true));
            Assert.That(stuck.IsStuck, Is.False);
            Assert.That(inventory.CanUnequip(human, "pocket1", out _));
            Assert.That(system.TryRemoveStuckItems(human));
            Assert.That(inventory.TryGetSlotEntity(human, "pocket1", out _), Is.False);
            Assert.That(system.TryRemoveStuckItems(human), Is.False);
        });
    }
}
