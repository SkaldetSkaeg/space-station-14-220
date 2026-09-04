// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.IntegrationTests.Fixtures;
using Content.Server.SS220.CultYogg.Cultists;
using Content.Server.SS220.GameTicking.Rules;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.EntityEffects.Events;
using Content.Shared.SS220.InnerHandToggleable;
using Content.Shared.SS220.RestrictedItem;
using Content.Shared.SS220.Roles;
using Content.Shared.SS220.StuckOnEquip;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.SS220;

public sealed class CultYoggEquipmentTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [TestCase(false)]
    [TestCase(true)]
    public async Task PurificationAndRoleRemovalKeepUnrelatedItems(bool holyWater)
    {
        var map = await Pair.CreateTestMap();
        EntityUid owner = default, cultItem = default, ordinaryItem = default, restrictedItem = default;
        await Server.WaitAssertion(() =>
        {
            var hands = SEntMan.System<SharedHandsSystem>();
            var inventory = SEntMan.System<InventorySystem>();
            owner = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            SEntMan.EnsureComponent<CultYoggComponent>(owner);
            var mindSystem = SEntMan.System<SharedMindSystem>();
            var mind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mind, owner);
            var roles = SEntMan.System<SharedRoleSystem>();
            roles.MindAddRole(mind, "MindRoleCultYogg", silent: true);

            cultItem = SEntMan.SpawnEntity("ClawCultYogg", map.GridCoords);
            ordinaryItem = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            restrictedItem = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            SEntMan.AddComponent<StuckOnEquipComponent>(ordinaryItem).InHandItem = true;
            SEntMan.AddComponent<RestrictedItemComponent>(restrictedItem);
            var handNames = SEntMan.GetComponent<HandsComponent>(owner).SortedHands;
            hands.DoPickup(owner, handNames[0], cultItem);
            hands.DoPickup(owner, handNames[1], ordinaryItem);
            Assert.That(inventory.TryEquip(owner, restrictedItem, "pocket1", force: true));

            if (!holyWater)
            {
                Assert.That(roles.MindRemoveRole<CultYoggRoleComponent>(mind));
                return;
            }

            // A rule entity is sufficient for the real holy-water deconversion path; no round start is needed.
            var rule = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<GameRuleComponent>(rule);
            SEntMan.AddComponent<CultYoggRuleComponent>(rule);
            Assert.That(SEntMan.System<CultYoggRuleSystem>().TryGetCultGameRule(out _));
            var purified = SEntMan.EnsureComponent<CultYoggPurifiedComponent>(owner);
            purified.BeforePurifyingTime = TimeSpan.FromSeconds(0.2);
            var drink = new OnSaintWaterDrinkEvent(owner, 15);
            SEntMan.EventBus.RaiseLocalEvent(owner, drink);
        });
        await Pair.RunSeconds(0.5f);
        await Server.WaitAssertion(() =>
        {
            var hands = SEntMan.System<SharedHandsSystem>();
            Assert.That(hands.IsHolding(owner, cultItem), Is.False);
            Assert.That(SEntMan.GetComponent<StuckOnEquipComponent>(cultItem).IsStuck, Is.False);
            Assert.That(hands.IsHolding(owner, ordinaryItem));
            Assert.That(SEntMan.GetComponent<StuckOnEquipComponent>(ordinaryItem).IsStuck);
            Assert.That(SEntMan.System<InventorySystem>().TryGetSlotEntity(owner, "pocket1", out var pocketItem));
            Assert.That(pocketItem, Is.EqualTo(restrictedItem));
            if (holyWater)
                Assert.That(SEntMan.HasComponent<CultYoggComponent>(owner), Is.False);
        });
    }

    [Test]
    public async Task CleanupDropsHiddenCultItems()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var owner = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var item = SEntMan.SpawnEntity("SedativeStingCultYogg", map.GridCoords);
            var inner = SEntMan.EnsureComponent<InnerHandToggleableComponent>(owner);
            var hand = SEntMan.GetComponent<HandsComponent>(owner).SortedHands[0];
            var container = SEntMan.System<SharedContainerSystem>().EnsureContainer<ContainerSlot>(owner, "test-inner-hand");
            inner.HandsContainers[hand] = new InnerContainerInfo { Container = container, InnerItemUid = item };
            Assert.That(SEntMan.System<SharedContainerSystem>().Insert(item, container));

            Assert.That(SEntMan.System<CultYoggEquipmentSystem>().DropCultEquipment(owner));
            Assert.That(container.ContainedEntity, Is.Null);
            Assert.That(inner.HandsContainers[hand].InnerItemUid, Is.Null);
            Assert.That(SEntMan.GetComponent<StuckOnEquipComponent>(item).IsStuck, Is.False);
        });
    }

    [Test]
    public async Task AttachedCultItemsHaveCuttingAndCleanupConfiguration()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            string[] prototypes =
            [
                "ClawCultYogg", "BeachCultYogg", "DagonsScale", "Venomancer", "SpikegunCultYogg",
                "Spitballer", "SedativeStingCultYogg", "ClothingBackpackCultYogg",
                "ClothingBackpackSatchelCultYogg", "ClothingBackpackDuffelCultYogg", "ClothingOuterHardsuitCultYogg",
            ];
            foreach (var prototype in prototypes)
            {
                var item = SEntMan.SpawnEntity(prototype, map.GridCoords);
                Assert.That(SEntMan.System<TagSystem>().HasTag(item, CultYoggEquipmentSystem.EquipmentTag), prototype);
                Assert.That(SEntMan.HasComponent<StuckOnEquipComponent>(item), prototype);
                Assert.That(SEntMan.HasComponent<CuttableStuckComponent>(item),
                    Is.EqualTo(prototype != "ClothingOuterHardsuitCultYogg"), prototype);
            }
        });
    }
}
