// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Server.Forensics;
using Content.Server.Forensics.Components;
using Content.Server.SS220.Forensics.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.SS220.Forensics;
using Robust.Shared.GameObjects;
using System.Linq;

namespace Content.IntegrationTests.SS220.Tests.Forensics;

public sealed class GlovePrintTest : GameTest
{
    [Test]
    public async Task GloveTracesAreIndependentOfFibers()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var inventory = SEntMan.System<InventorySystem>();
            var interaction = SEntMan.System<SharedInteractionSystem>();
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var firstGloves = SEntMan.SpawnEntity("ClothingHandsGlovesColorBlack", map.GridCoords);
            var secondGloves = SEntMan.SpawnEntity("ClothingHandsGlovesColorBlack", map.GridCoords);
            var evidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            var firstTrace = GetTrace(firstGloves);
            var secondTrace = GetTrace(secondGloves);

            Assert.That(firstTrace.Code, Is.Not.EqualTo(secondTrace.Code));
            Assert.That(firstTrace.Prototype, Is.EqualTo(secondTrace.Prototype));
            Assert.That(inventory.TryEquip(wearer, firstGloves, "gloves"), Is.True);
            interaction.DoContactInteraction(wearer, evidence);
            Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);
            Assert.That(inventory.TryEquip(wearer, secondGloves, "gloves"), Is.True);
            interaction.DoContactInteraction(wearer, evidence);

            var traces = SEntMan.GetComponent<ForensicsComponent>(evidence);
            Assert.That(traces.GlovePrints, Is.EquivalentTo(new[] { firstTrace, secondTrace }));
            Assert.That(traces.Fibers, Has.Count.EqualTo(1), "Identical fibers must not contain individual glove codes.");

            SEntMan.RemoveComponent<FiberComponent>(secondGloves);
            var otherEvidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            interaction.DoContactInteraction(wearer, otherEvidence);
            var otherTraces = SEntMan.GetComponent<ForensicsComponent>(otherEvidence);
            Assert.That(otherTraces.Fibers, Is.Empty);
            Assert.That(otherTraces.GlovePrints, Is.EquivalentTo(new[] { secondTrace }),
                "Glove traces must not depend on a Fiber component.");
        });
    }

    [TestCase("ClothingHandsGlovesColorBlack", false)]
    [TestCase("ClothingHandsGlovesFingerless", true)]
    public async Task PairIdentitySurvivesRepeatedContactAndNewWearer(string prototype, bool leavesFingerprints)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var inventory = SEntMan.System<InventorySystem>();
            var interaction = SEntMan.System<SharedInteractionSystem>();
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var gloves = SEntMan.SpawnEntity(prototype, map.GridCoords);
            var evidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            var trace = GetTrace(gloves);

            Assert.That(inventory.TryEquip(wearer, gloves, "gloves"), Is.True);
            interaction.DoContactInteraction(wearer, evidence);
            interaction.DoContactInteraction(wearer, evidence);
            var traces = SEntMan.GetComponent<ForensicsComponent>(evidence);
            Assert.That(traces.GlovePrints, Is.EquivalentTo(new[] { trace }));
            Assert.That(traces.Fingerprints.Count, Is.EqualTo(leavesFingerprints ? 1 : 0));

            Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);
            var secondWearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            Assert.That(inventory.TryEquip(secondWearer, gloves, "gloves"), Is.True);
            var otherEvidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            interaction.DoContactInteraction(secondWearer, otherEvidence);
            Assert.That(SEntMan.GetComponent<ForensicsComponent>(otherEvidence).GlovePrints,
                Is.EquivalentTo(new[] { trace }));
        });
    }

    [Test]
    public async Task ForensicGlovesLeaveNoTraces()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var gloves = SEntMan.SpawnEntity("ClothingHandsGlovesForensic", map.GridCoords);
            var evidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            Assert.That(SEntMan.System<InventorySystem>().TryEquip(wearer, gloves, "gloves"), Is.True);
            SEntMan.System<SharedInteractionSystem>().DoContactInteraction(wearer, evidence);
            var traces = SEntMan.GetComponent<ForensicsComponent>(evidence);
            Assert.That(traces.GlovePrints, Is.Empty);
            Assert.That(traces.Fibers, Is.Empty);
            Assert.That(traces.Fingerprints, Is.Empty);

            var swab = CollectSample(wearer, gloves);
            var sample = SEntMan.GetComponent<GlovePrintSwabComponent>(swab);
            Assert.That(sample.Used, Is.True);
            Assert.That(sample.GlovePrints, Is.Empty);
        });
    }

    [Test]
    public async Task CleaningRemovesEvidenceAndPreservesPairIdentity()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var inventory = SEntMan.System<InventorySystem>();
            var interaction = SEntMan.System<SharedInteractionSystem>();
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var gloves = SEntMan.SpawnEntity("ClothingHandsGlovesColorBlack", map.GridCoords);
            SEntMan.RemoveComponent<FiberComponent>(gloves);
            var trace = GetTrace(gloves);
            var evidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            Assert.That(inventory.TryEquip(wearer, gloves, "gloves"), Is.True);
            interaction.DoContactInteraction(wearer, evidence);
            Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);
            var traces = SEntMan.GetComponent<ForensicsComponent>(evidence);
            Assert.That(traces.Fibers, Is.Empty);
            Assert.That(traces.Fingerprints, Is.Empty);
            Assert.That(traces.GlovePrints, Does.Contain(trace));

            var forensics = SEntMan.System<ForensicsSystem>();
            var copiedEvidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            forensics.CopyForensicsFrom(traces, copiedEvidence);
            Assert.That(SEntMan.GetComponent<ForensicsComponent>(copiedEvidence).GlovePrints, Does.Contain(trace));

            var soap = SEntMan.SpawnEntity("Soap", map.GridCoords);
            interaction.DoContactInteraction(wearer, soap);
            var cleaner = SEntMan.GetComponent<CleansForensicsComponent>(soap);
            cleaner.CleanDelay = 0;
            Assert.That(forensics.TryStartCleaning((soap, cleaner), wearer, evidence), Is.True);
            Assert.That(traces.GlovePrints, Is.Empty);
            Assert.That(SEntMan.GetComponent<ForensicsComponent>(copiedEvidence).GlovePrints, Does.Contain(trace));

            interaction.DoContactInteraction(wearer, gloves);
            Assert.That(forensics.TryStartCleaning((soap, cleaner), wearer, gloves), Is.True);
            Assert.That(GetTrace(gloves), Is.EqualTo(trace));
            Assert.That(inventory.TryEquip(wearer, gloves, "gloves"), Is.True);
            interaction.DoContactInteraction(wearer, evidence);
            Assert.That(traces.GlovePrints, Is.EquivalentTo(new[] { trace }));
        });
    }

    [Test]
    public async Task OnlyAnalyzerReadsCollectedGloveTraces()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var gloves = SEntMan.SpawnEntity("ClothingHandsGlovesColorBlack", map.GridCoords);
            var investigatorGloves = SEntMan.SpawnEntity("ClothingHandsGlovesColorBlack", map.GridCoords);
            var evidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            var trace = GetTrace(gloves);
            var inventory = SEntMan.System<InventorySystem>();
            var interaction = SEntMan.System<SharedInteractionSystem>();
            Assert.That(inventory.TryEquip(wearer, gloves, "gloves"), Is.True);
            interaction.DoContactInteraction(wearer, evidence);
            Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);

            var ordinaryScanner = SEntMan.SpawnEntity("ForensicScanner", map.GridCoords);
            var ordinary = SEntMan.GetComponent<ForensicScannerComponent>(ordinaryScanner);
            ordinary.ScanDelay = 0;
            AfterInteract(ordinaryScanner, wearer, evidence);
            Assert.That(ordinary.Fibers.Single(), Does.Not.Contain(trace.Code));
            var pad = SEntMan.SpawnEntity("ForensicPad", map.GridCoords);
            var padSample = SEntMan.GetComponent<ForensicPadComponent>(pad);
            padSample.ScanDelay = 0;
            AfterInteract(pad, wearer, gloves);
            Assert.That(padSample.Used, Is.True);
            Assert.That(padSample.Sample, Is.EqualTo(ordinary.Fibers.Single()));

            Assert.That(inventory.TryEquip(wearer, investigatorGloves, "gloves"), Is.True);
            var swab = CollectSample(wearer, evidence);
            var sample = SEntMan.GetComponent<GlovePrintSwabComponent>(swab);
            Assert.That(sample.GlovePrints, Is.EquivalentTo(new[] { trace }));
            Assert.That(SEntMan.GetComponent<ForensicsComponent>(evidence).GlovePrints,
                Is.EquivalentTo(new[] { trace }), "Collecting must not add the investigator's glove traces.");

            var analyzer = SEntMan.SpawnEntity("GlovePrintAnalyzer", map.GridCoords);
            var machine = SEntMan.GetComponent<GlovePrintAnalyzerComponent>(analyzer);
            var slots = SEntMan.System<ItemSlotsSystem>();
            Assert.That(slots.TryGetSlot(analyzer, machine.SampleSlot, out var slot), Is.True);
            Assert.That(slot.Item, Is.Null);
            var crowbar = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            Assert.That(slots.CanInsert(analyzer, crowbar, wearer, slot), Is.False,
                "The analyzer must only accept a glove trace swab.");
            Assert.That(interaction.InteractUsing(wearer, swab, analyzer, map.GridCoords), Is.True);
            Assert.That(slot.Item, Is.EqualTo(swab), "Analyzing requires insertion of the physical collected swab.");
            Assert.That(sample.GlovePrints, Is.EquivalentTo(new[] { trace }));
            Assert.That(SEntMan.GetComponent<UserInterfaceComponent>(analyzer).States, Is.Empty,
                "Private results must not enter component state replicated to nearby clients.");
        });
    }

    [Test]
    public async Task SampleSurvivesCleaningAndDeletedGloves()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var gloves = SEntMan.SpawnEntity("ClothingHandsGlovesColorBlack", map.GridCoords);
            var evidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            var trace = GetTrace(gloves);
            var inventory = SEntMan.System<InventorySystem>();
            Assert.That(inventory.TryEquip(wearer, gloves, "gloves"), Is.True);
            SEntMan.System<SharedInteractionSystem>().DoContactInteraction(wearer, evidence);
            var swab = CollectSample(wearer, evidence);
            var sample = SEntMan.GetComponent<GlovePrintSwabComponent>(swab);
            Assert.That(sample.GlovePrints, Is.EquivalentTo(new[] { trace }));

            var hands = SEntMan.System<SharedHandsSystem>();
            Assert.That(hands.TryDrop(wearer, swab), Is.True);
            var soap = SEntMan.SpawnEntity("Soap", map.GridCoords);
            Assert.That(hands.TryPickupAnyHand(wearer, soap), Is.True);
            var cleaner = SEntMan.GetComponent<CleansForensicsComponent>(soap);
            cleaner.CleanDelay = 0;
            Assert.That(SEntMan.System<ForensicsSystem>().TryStartCleaning((soap, cleaner), wearer, evidence), Is.True);
            Assert.That(SEntMan.GetComponent<ForensicsComponent>(evidence).GlovePrints, Is.Empty);
            Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);
            SEntMan.DeleteEntity(gloves);
            SEntMan.DeleteEntity(evidence);

            Assert.That(sample.GlovePrints, Is.EquivalentTo(new[] { trace }),
                "A collected sample must retain the glove type and code after its source and gloves are gone.");
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RepeatedScansDoNotOverwriteEarlierSamples(bool firstSourceHasTraces)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var firstGloves = SEntMan.SpawnEntity("ClothingHandsGlovesColorBlack", map.GridCoords);
            var secondGloves = SEntMan.SpawnEntity("ClothingHandsGlovesColorBlack", map.GridCoords);
            var firstEvidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            var secondEvidence = SEntMan.SpawnEntity("Wrench", map.GridCoords);
            var inventory = SEntMan.System<InventorySystem>();
            var interaction = SEntMan.System<SharedInteractionSystem>();
            Assert.That(inventory.TryEquip(wearer, firstGloves, "gloves"), Is.True);
            if (firstSourceHasTraces)
                interaction.DoContactInteraction(wearer, firstEvidence);

            var swab = CollectSample(wearer, firstEvidence);
            var sample = SEntMan.GetComponent<GlovePrintSwabComponent>(swab);
            List<GlovePrintTrace> originalTraces = [.. sample.GlovePrints];
            Assert.That(originalTraces.Count, Is.EqualTo(firstSourceHasTraces ? 1 : 0));
            Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);
            Assert.That(inventory.TryEquip(wearer, secondGloves, "gloves"), Is.True);
            interaction.DoContactInteraction(wearer, secondEvidence);

            Assert.That(SEntMan.System<SharedHandsSystem>().TryDrop(wearer, swab), Is.True);
            var laterSwab = CollectSample(wearer, secondEvidence);
            Assert.That(laterSwab, Is.Not.EqualTo(swab));
            Assert.That(SEntMan.GetComponent<GlovePrintSwabComponent>(laterSwab).GlovePrints,
                Is.EquivalentTo(new[] { GetTrace(secondGloves) }));
            Assert.That(sample.Used, Is.True);
            Assert.That(sample.GlovePrints, Is.EquivalentTo(originalTraces),
                "A later scan must create a new sample and preserve even an earlier empty snapshot.");
        });
    }

    [TestCase("ClothingHandsGlovesColorBlack", "ClothingHandsGlovesFingerless", false)]
    [TestCase("ClothingHandsGlovesColorBlack", "ClothingHandsGlovesFingerless", true)]
    [TestCase("ClothingHandsGlovesColorYellowBudget", "ClothingHandsGlovesFingerlessInsulated", false)]
    [TestCase("ClothingHandsGlovesCombat", "ClothingHandsGlovesFingerless", false)]
    public async Task CuttingPreservesPairCodeAndOriginalTypeSnapshot(string prototype, string resultPrototype, bool equipped)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var gloves = SEntMan.SpawnEntity(prototype, map.GridCoords);
            var originalTrace = GetTrace(gloves);
            var inventory = SEntMan.System<InventorySystem>();
            var interaction = SEntMan.System<SharedInteractionSystem>();
            Assert.That(inventory.TryEquip(wearer, gloves, "gloves"), Is.True);
            var evidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            interaction.DoContactInteraction(wearer, evidence);
            if (!equipped)
                Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);

            var originalSwab = CollectSample(wearer, evidence);
            var originalSample = SEntMan.GetComponent<GlovePrintSwabComponent>(originalSwab);
            Assert.That(originalSample.GlovePrints, Is.EquivalentTo(new[] { originalTrace }));

            SEntMan.GetComponent<ButcherableComponent>(gloves).ButcherDelay = 0;
            var knife = SEntMan.SpawnEntity("KitchenKnife", map.GridCoords);
            AfterInteract(knife, wearer, gloves);
            EntityUid? cutGloves = null;
            var query = SEntMan.EntityQueryEnumerator<GlovePrintComponent, MetaDataComponent>();
            while (query.MoveNext(out var uid, out _, out var meta))
            {
                if (meta.EntityPrototype?.ID != resultPrototype)
                    continue;

                Assert.That(cutGloves, Is.Null);
                cutGloves = uid;
            }

            Assert.That(cutGloves, Is.Not.Null);
            var cutTrace = GetTrace(cutGloves.Value);
            Assert.That(cutTrace.Code, Is.EqualTo(originalTrace.Code));
            Assert.That(cutTrace.Prototype, Is.Not.EqualTo(originalTrace.Prototype));
            Assert.That(SEntMan.System<SharedHandsSystem>().TryDrop(wearer, originalSwab), Is.True);
            var cutSwab = CollectSample(wearer, cutGloves.Value, GlovePrintScannerMode.OwnPrint);
            Assert.That(SEntMan.GetComponent<GlovePrintSwabComponent>(cutSwab).GlovePrints,
                Is.EquivalentTo(new[] { cutTrace }));

            if (equipped)
                Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);

            Assert.That(inventory.TryEquip(wearer, cutGloves.Value, "gloves"), Is.True);

            Assert.That(inventory.TryGetSlotEntity(wearer, "gloves", out var equippedGloves), Is.True);
            Assert.That(equippedGloves, Is.EqualTo(cutGloves));
            interaction.DoContactInteraction(wearer, evidence);
            Assert.That(SEntMan.GetComponent<ForensicsComponent>(evidence).GlovePrints,
                Is.EquivalentTo(new[] { originalTrace, cutTrace }),
                "Contacts before and after cutting must retain their original type for the same pair code.");
            Assert.That(originalSample.GlovePrints, Is.EquivalentTo(new[] { originalTrace }),
                "Later contacts must not change a previously collected type snapshot.");
        });
    }

    private EntityUid CollectSample(EntityUid user, EntityUid target,
        GlovePrintScannerMode mode = GlovePrintScannerMode.SurfaceTraces)
    {
        var coordinates = SEntMan.GetComponent<TransformComponent>(user).Coordinates;
        var scanner = SEntMan.SpawnEntity("GlovePrintScanner", coordinates);
        var component = SEntMan.GetComponent<GlovePrintScannerComponent>(scanner);
        component.ScanDelay = TimeSpan.Zero;
        component.Mode = mode;
        var hands = SEntMan.System<SharedHandsSystem>();
        Assert.That(hands.TryPickupAnyHand(user, scanner), Is.True);
        var existingSwabs = GetSwabs();
        Assert.That(SEntMan.System<SharedInteractionSystem>().InteractDoAfter(
            user, scanner, target, coordinates, canReach: true), Is.False);
        var swab = GetSwabs().Single(uid => !existingSwabs.Contains(uid));
        Assert.That(SEntMan.GetComponent<GlovePrintSwabComponent>(swab).Used, Is.True,
            "The scanner must emit a physical, completed sample after the real interaction.");
        Assert.That(hands.TryDrop(user, scanner), Is.True);
        if (!hands.IsHolding(user, swab))
            Assert.That(hands.TryPickupAnyHand(user, swab), Is.True);

        return swab;
    }

    private List<EntityUid> GetSwabs()
    {
        List<EntityUid> swabs = [];
        var query = SEntMan.EntityQueryEnumerator<GlovePrintSwabComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            swabs.Add(uid);
        }

        return swabs;
    }

    private GlovePrintTrace GetTrace(EntityUid gloves)
    {
        var print = SEntMan.GetComponent<GlovePrintComponent>(gloves).Print;
        Assert.That(print, Is.Not.Null.And.Not.Empty);
        var prototype = SEntMan.GetComponent<MetaDataComponent>(gloves).EntityPrototype;
        Assert.That(prototype, Is.Not.Null);
        return new GlovePrintTrace(print!, prototype!.ID);
    }

    private void AfterInteract(EntityUid used, EntityUid user, EntityUid target)
    {
        var coordinates = SEntMan.GetComponent<TransformComponent>(user).Coordinates;
        SEntMan.EventBus.RaiseLocalEvent(used, new AfterInteractEvent(user, used, target, coordinates, true));
    }
}
