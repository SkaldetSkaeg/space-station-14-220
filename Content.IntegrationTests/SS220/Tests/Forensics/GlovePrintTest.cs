// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.IntegrationTests.Fixtures;
using Content.Server.Forensics;
using Content.Server.Forensics.Components;
using Content.Server.SS220.Forensics.Components;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.SS220.Forensics;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
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

            Assert.That(inventory.TryEquip(wearer, firstGloves, "gloves"), Is.True);
            interaction.DoContactInteraction(wearer, evidence);
            Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);
            Assert.That(inventory.TryEquip(wearer, secondGloves, "gloves"), Is.True);
            interaction.DoContactInteraction(wearer, evidence);

            var traces = SEntMan.GetComponent<ForensicsComponent>(evidence);
            Assert.That(traces.GlovePrints, Has.Count.EqualTo(2));
            Assert.That(traces.Fibers, Has.Count.EqualTo(1), "Identical fibers must not contain individual glove codes.");

            SEntMan.RemoveComponent<FiberComponent>(secondGloves);
            var otherEvidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            interaction.DoContactInteraction(wearer, otherEvidence);
            var otherTraces = SEntMan.GetComponent<ForensicsComponent>(otherEvidence);
            Assert.That(otherTraces.Fibers, Is.Empty);
            Assert.That(otherTraces.GlovePrints, Has.Count.EqualTo(1), "Glove traces must not depend on a Fiber component.");
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
            var print = SEntMan.GetComponent<GlovePrintComponent>(gloves).Print;
            Assert.That(print, Is.Not.Null.And.Not.Empty);

            Assert.That(inventory.TryEquip(wearer, gloves, "gloves"), Is.True);
            interaction.DoContactInteraction(wearer, evidence);
            interaction.DoContactInteraction(wearer, evidence);
            var traces = SEntMan.GetComponent<ForensicsComponent>(evidence);
            Assert.That(traces.GlovePrints, Is.EquivalentTo(new[] { print }));
            Assert.That(traces.Fingerprints.Count, Is.EqualTo(leavesFingerprints ? 1 : 0));

            Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);
            var secondWearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            Assert.That(inventory.TryEquip(secondWearer, gloves, "gloves"), Is.True);
            var otherEvidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            interaction.DoContactInteraction(secondWearer, otherEvidence);
            Assert.That(SEntMan.GetComponent<ForensicsComponent>(otherEvidence).GlovePrints,
                Is.EquivalentTo(new[] { print }));
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
            var scanner = SEntMan.SpawnEntity("GlovePrintScanner", map.GridCoords);
            SEntMan.GetComponent<GlovePrintScannerComponent>(scanner).ScanDelay = TimeSpan.Zero;
            Scan(scanner, wearer, gloves);
            Assert.That(SEntMan.GetComponent<GlovePrintScannerComponent>(scanner).Sample, Is.Null);
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
            var print = SEntMan.GetComponent<GlovePrintComponent>(gloves).Print;
            var evidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            Assert.That(inventory.TryEquip(wearer, gloves, "gloves"), Is.True);
            interaction.DoContactInteraction(wearer, evidence);
            Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);
            var traces = SEntMan.GetComponent<ForensicsComponent>(evidence);
            Assert.That(traces.Fibers, Is.Empty);
            Assert.That(traces.Fingerprints, Is.Empty);
            Assert.That(traces.GlovePrints, Does.Contain(print));

            var forensics = SEntMan.System<ForensicsSystem>();
            var copiedEvidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            forensics.CopyForensicsFrom(traces, copiedEvidence);
            Assert.That(SEntMan.GetComponent<ForensicsComponent>(copiedEvidence).GlovePrints, Does.Contain(print));

            var soap = SEntMan.SpawnEntity("Soap", map.GridCoords);
            interaction.DoContactInteraction(wearer, soap);
            var cleaner = SEntMan.GetComponent<CleansForensicsComponent>(soap);
            cleaner.CleanDelay = 0;
            Assert.That(forensics.TryStartCleaning((soap, cleaner), wearer, evidence), Is.True);
            Assert.That(traces.GlovePrints, Is.Empty);
            Assert.That(SEntMan.GetComponent<ForensicsComponent>(copiedEvidence).GlovePrints, Does.Contain(print));

            interaction.DoContactInteraction(wearer, gloves);
            Assert.That(forensics.TryStartCleaning((soap, cleaner), wearer, gloves), Is.True);
            Assert.That(SEntMan.GetComponent<GlovePrintComponent>(gloves).Print, Is.EqualTo(print));
            Assert.That(inventory.TryEquip(wearer, gloves, "gloves"), Is.True);
            interaction.DoContactInteraction(wearer, evidence);
            Assert.That(traces.GlovePrints, Is.EquivalentTo(new[] { print }));
        });
    }

    [Test]
    public async Task OnlyDedicatedScannerReadsAndComparesGloveTraces()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var gloves = SEntMan.SpawnEntity("ClothingHandsGlovesColorBlack", map.GridCoords);
            var otherGloves = SEntMan.SpawnEntity("ClothingHandsGlovesColorBlack", map.GridCoords);
            var evidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            var print = SEntMan.GetComponent<GlovePrintComponent>(gloves).Print;
            var inventory = SEntMan.System<InventorySystem>();
            Assert.That(inventory.TryEquip(wearer, gloves, "gloves"), Is.True);
            SEntMan.System<SharedInteractionSystem>().DoContactInteraction(wearer, evidence);
            Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);

            var ordinaryScanner = SEntMan.SpawnEntity("ForensicScanner", map.GridCoords);
            var ordinary = SEntMan.GetComponent<ForensicScannerComponent>(ordinaryScanner);
            ordinary.ScanDelay = 0;
            Scan(ordinaryScanner, wearer, evidence);
            Assert.That(ordinary.Fibers.Single(), Does.Not.Contain(print));
            var pad = SEntMan.SpawnEntity("ForensicPad", map.GridCoords);
            var sample = SEntMan.GetComponent<ForensicPadComponent>(pad);
            sample.ScanDelay = 0;
            Scan(pad, wearer, gloves);
            Assert.That(sample.Used, Is.True);
            Assert.That(sample.Sample, Is.EqualTo(ordinary.Fibers.Single()));

            var scanner = SEntMan.SpawnEntity("GlovePrintScanner", map.GridCoords);
            var scan = SEntMan.GetComponent<GlovePrintScannerComponent>(scanner);
            scan.ScanDelay = TimeSpan.Zero;
            Assert.That(inventory.TryEquip(wearer, otherGloves, "gloves"), Is.True);
            Assert.That(SEntMan.System<SharedInteractionSystem>().InteractDoAfter(
                wearer, scanner, evidence, map.GridCoords, canReach: true), Is.False);
            Assert.That(scan.GlovePrints, Is.EquivalentTo(new[] { print }));
            Assert.That(SEntMan.GetComponent<ForensicsComponent>(evidence).GlovePrints,
                Is.EquivalentTo(new[] { print }), "Scanning must not add the investigator's glove traces.");
            Scan(scanner, wearer, otherGloves);
            Assert.That(scan.Sample, Is.Not.EqualTo(print));
            Assert.That(HasSound(scan.SoundNoMatch), Is.True);
            Scan(scanner, wearer, gloves);
            Assert.That(scan.Sample, Is.EqualTo(print));
            Assert.That(HasSound(scan.SoundMatch), Is.True);
            Assert.That(scan.GlovePrints, Is.EquivalentTo(new[] { print }), "Sampling must retain the scanned evidence.");

            Assert.That(SEntMan.GetComponent<UserInterfaceComponent>(scanner).States, Is.Empty,
                "Private results must not enter component state replicated to nearby clients.");
            SEntMan.EventBus.RaiseLocalEvent(scanner, new GlovePrintScannerClearMessage());
            Assert.That(scan.GlovePrints, Is.Empty);
            Assert.That(scan.Sample, Is.Null);
            Assert.That(scan.LastScannedName, Is.Empty);

            Scan(scanner, wearer, SEntMan.SpawnEntity("Crowbar", map.GridCoords));
            Assert.That(scan.GlovePrints, Is.Empty, "Scanning a clean object must not retain earlier traces.");
        });
    }

    [Test]
    public async Task ScannerResultsRequireHoldingTheDevice()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var user = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var scanner = SEntMan.SpawnEntity("GlovePrintScanner", map.GridCoords);
            var ui = SEntMan.System<UserInterfaceSystem>();
            var hands = SEntMan.System<SharedHandsSystem>();
            Assert.That(ui.TryOpenUi(scanner, GlovePrintScannerUiKey.Key, user), Is.False,
                "A raw UI open must not expose results from a scanner lying nearby.");

            var otherUser = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            Assert.That(ui.TryOpenUi(scanner, GlovePrintScannerUiKey.Key, otherUser), Is.False);
            Assert.That(hands.TryPickup(user, scanner), Is.True);
            Assert.That(ui.TryOpenUi(scanner, GlovePrintScannerUiKey.Key, user), Is.True);

            var request = new GlovePrintScannerRequestResultsMessage { Actor = user };
            ui.RaiseUiMessage(scanner, GlovePrintScannerUiKey.Key, request);
            Assert.That(request.Entity, Is.EqualTo(SEntMan.GetNetEntity(scanner)));
            var scan = SEntMan.GetComponent<GlovePrintScannerComponent>(scanner);
            scan.GlovePrints.Add("stored glove print");
            scan.Sample = "sample";
            scan.LastScannedName = "evidence";
            ui.RaiseUiMessage(scanner, GlovePrintScannerUiKey.Key, new GlovePrintScannerClearMessage { Actor = user });
            Assert.That(scan.GlovePrints, Is.Empty);
            Assert.That(scan.Sample, Is.Null);
            Assert.That(scan.LastScannedName, Is.Empty);

            scan.GlovePrints.Add("stored glove print");
            scan.Sample = "sample";
            scan.LastScannedName = "evidence";
            Assert.That(hands.TryDrop(user, scanner), Is.True);
            Assert.That(ui.TryOpenUi(scanner, GlovePrintScannerUiKey.Key, user), Is.False);
            var blockedRequest = new GlovePrintScannerRequestResultsMessage { Actor = user };
            ui.RaiseUiMessage(scanner, GlovePrintScannerUiKey.Key, blockedRequest);
            Assert.That(blockedRequest.Entity, Is.EqualTo(NetEntity.Invalid));
            ui.RaiseUiMessage(scanner, GlovePrintScannerUiKey.Key, new GlovePrintScannerClearMessage { Actor = user });
            Assert.That(scan.GlovePrints, Is.EquivalentTo(new[] { "stored glove print" }));
            Assert.That(scan.Sample, Is.EqualTo("sample"));
            Assert.That(scan.LastScannedName, Is.EqualTo("evidence"));
        });
    }

    [TestCase("ClothingHandsGlovesColorBlack", "ClothingHandsGlovesFingerless", false)]
    [TestCase("ClothingHandsGlovesColorBlack", "ClothingHandsGlovesFingerless", true)]
    [TestCase("ClothingHandsGlovesColorYellowBudget", "ClothingHandsGlovesFingerlessInsulated", false)]
    [TestCase("ClothingHandsGlovesCombat", "ClothingHandsGlovesFingerless", false)]
    public async Task CuttingPreservesIdentityAndMatching(string prototype, string resultPrototype, bool equipped)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var gloves = SEntMan.SpawnEntity(prototype, map.GridCoords);
            var print = SEntMan.GetComponent<GlovePrintComponent>(gloves).Print;
            var inventory = SEntMan.System<InventorySystem>();
            Assert.That(inventory.TryEquip(wearer, gloves, "gloves"), Is.True);
            var evidence = SEntMan.SpawnEntity("Crowbar", map.GridCoords);
            SEntMan.System<SharedInteractionSystem>().DoContactInteraction(wearer, evidence);
            if (!equipped)
                Assert.That(inventory.TryUnequip(wearer, "gloves"), Is.True);

            var scanner = SEntMan.SpawnEntity("GlovePrintScanner", map.GridCoords);
            var scan = SEntMan.GetComponent<GlovePrintScannerComponent>(scanner);
            scan.ScanDelay = TimeSpan.Zero;
            Scan(scanner, wearer, evidence);

            SEntMan.GetComponent<ButcherableComponent>(gloves).ButcherDelay = 0;
            var knife = SEntMan.SpawnEntity("KitchenKnife", map.GridCoords);
            Scan(knife, wearer, gloves);
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
            Scan(scanner, wearer, cutGloves.Value);
            Assert.That(scan.Sample, Is.EqualTo(print));
            Assert.That(scan.GlovePrints, Does.Contain(print));
            Assert.That(HasSound(scan.SoundMatch), Is.True);
        });
    }

    private void Scan(EntityUid scanner, EntityUid user, EntityUid target)
    {
        var coordinates = SEntMan.GetComponent<TransformComponent>(user).Coordinates;
        SEntMan.EventBus.RaiseLocalEvent(scanner, new AfterInteractEvent(user, scanner, target, coordinates, true));
    }

    private bool HasSound(SoundSpecifier sound)
    {
        var path = ((SoundPathSpecifier)sound).Path.ToString();
        var query = SEntMan.EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var audio))
        {
            if (audio.FileName == path)
                return true;
        }

        return false;
    }
}
