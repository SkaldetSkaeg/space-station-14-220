// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Client.SS220.Forensics;
using Content.IntegrationTests.Fixtures;
using Content.Server.SS220.Forensics.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.SS220.Forensics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.SS220.Tests.Forensics;

public sealed class GlovePrintScannerUiTest : GameTest
{
    [Test]
    public async Task ConnectedPlayerReceivesScanSampleAndClearedResults()
    {
        var map = await Pair.CreateTestMap();
        Assert.That(ServerSession, Is.Not.Null);
        var session = ServerSession!;
        var previousEntity = session.AttachedEntity;
        EntityUid user = default;
        EntityUid gloves = default;
        EntityUid evidence = default;
        EntityUid scanner = default;
        NetEntity scannerNet = default;
        string print = null;
        string targetName = null;

        await Server.WaitAssertion(() =>
        {
            user = SSpawnAtPosition("MobHuman", map.GridCoords);
            gloves = SSpawnAtPosition("ClothingHandsGlovesColorBlack", map.GridCoords);
            evidence = SSpawnAtPosition("Crowbar", map.GridCoords);
            scanner = SSpawnAtPosition("GlovePrintScanner", map.GridCoords);
            scannerNet = SEntMan.GetNetEntity(scanner);
            print = SEntMan.GetComponent<GlovePrintComponent>(gloves).Print;
            targetName = SEntMan.GetComponent<MetaDataComponent>(evidence).EntityName;
            Assert.That(print, Is.Not.Null.And.Not.Empty);

            var inventory = SEntMan.System<InventorySystem>();
            Assert.That(inventory.TryEquip(user, gloves, "gloves"), Is.True);
            SEntMan.System<SharedInteractionSystem>().DoContactInteraction(user, evidence);
            Assert.That(inventory.TryUnequip(user, "gloves"), Is.True);
            Assert.That(SEntMan.System<SharedHandsSystem>().TryPickupAnyHand(user, scanner), Is.True);
            SEntMan.GetComponent<GlovePrintScannerComponent>(scanner).ScanDelay = TimeSpan.Zero;
            Assert.That(Server.PlayerMan.SetAttachedEntity(session, user), Is.True);
        });
        await RunUntilSynced();

        await Server.WaitAssertion(() => Scan(scanner, user, evidence));
        await RunTicksSync(15);
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var window = GetWindow();
            var loc = Client.ResolveDependency<ILocalizationManager>();
            Assert.That(window.FindControl<Label>("TargetLabel").Text,
                Is.EqualTo(loc.GetString("glove-print-scanner-interface-target", ("target", targetName))));
            Assert.That(window.FindControl<Label>("TracesLabel").Text, Does.Contain(print));
            Assert.That(window.FindControl<Label>("SampleLabel").Text,
                Is.EqualTo(loc.GetString("glove-print-scanner-interface-no-sample")));
            Assert.That(window.FindControl<Label>("MatchLabel").Text, Is.Empty);
        });

        await Server.WaitAssertion(() => Scan(scanner, user, gloves));
        await RunTicksSync(15);
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var window = GetWindow();
            var loc = Client.ResolveDependency<ILocalizationManager>();
            Assert.That(window.FindControl<Label>("TracesLabel").Text, Does.Contain(print));
            Assert.That(window.FindControl<Label>("SampleLabel").Text,
                Is.EqualTo(loc.GetString("glove-print-scanner-interface-sample", ("sample", print))));
            Assert.That(window.FindControl<Label>("MatchLabel").Text,
                Is.EqualTo(loc.GetString("glove-print-scanner-interface-match")));
            GetBui(scannerNet).SendMessage(new GlovePrintScannerClearMessage());
        });

        await RunTicksSync(15);
        await RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            var scan = SEntMan.GetComponent<GlovePrintScannerComponent>(scanner);
            Assert.That(scan.GlovePrints, Is.Empty);
            Assert.That(scan.Sample, Is.Null);
            Assert.That(scan.LastScannedName, Is.Empty);
            Assert.That(SEntMan.GetComponent<UserInterfaceComponent>(scanner).States, Is.Empty,
                "Private results must be delivered through messages rather than replicated UI state.");
        });
        await Client.WaitAssertion(() =>
        {
            AssertEmptyResults();
            GetBui(scannerNet).Close();
        });

        await RunTicksSync(15);
        await RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<UserInterfaceSystem>().TryOpenUi(scanner, GlovePrintScannerUiKey.Key, user),
                Is.True);
        });
        await RunTicksSync(15);
        await RunUntilSynced();
        await Client.WaitAssertion(AssertEmptyResults);
        await Server.WaitAssertion(() => Assert.That(Server.PlayerMan.SetAttachedEntity(session, previousEntity), Is.True));
        await RunUntilSynced();
    }

    private void Scan(EntityUid scanner, EntityUid user, EntityUid target)
    {
        var coordinates = SEntMan.GetComponent<TransformComponent>(user).Coordinates;
        Assert.That(SEntMan.System<SharedInteractionSystem>().InteractDoAfter(
            user, scanner, target, coordinates, canReach: true), Is.False);
    }

    private BoundUserInterface GetBui(NetEntity scanner)
    {
        var ui = CEntMan.GetComponent<UserInterfaceComponent>(CEntMan.GetEntity(scanner));
        if (!ui.ClientOpenInterfaces.TryGetValue(GlovePrintScannerUiKey.Key, out var bui))
            throw new InvalidOperationException("The glove print scanner interface is not open on the client.");

        return bui;
    }

    private GlovePrintScannerMenu GetWindow()
    {
        var ui = Client.ResolveDependency<IUserInterfaceManager>();
        var window = ui.WindowRoot.Children.OfType<GlovePrintScannerMenu>().SingleOrDefault(control => control.IsOpen);
        if (window == null)
            throw new InvalidOperationException("The glove print scanner window is not open on the client.");

        return window;
    }

    private void AssertEmptyResults()
    {
        var window = GetWindow();
        var loc = Client.ResolveDependency<ILocalizationManager>();
        Assert.That(window.FindControl<Label>("TargetLabel").Text,
            Is.EqualTo(loc.GetString("glove-print-scanner-interface-no-target")));
        Assert.That(window.FindControl<Label>("TracesLabel").Text,
            Is.EqualTo(loc.GetString("glove-print-scanner-interface-no-traces")));
        Assert.That(window.FindControl<Label>("SampleLabel").Text,
            Is.EqualTo(loc.GetString("glove-print-scanner-interface-no-sample")));
        Assert.That(window.FindControl<Label>("MatchLabel").Text, Is.Empty);
        Assert.That(window.Clear.Disabled, Is.True);
    }
}
