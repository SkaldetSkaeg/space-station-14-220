using System.Linq;
using Content.Client.Overlays;
using Content.Client.SS220.Ghost;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Actions;
using Content.Shared.Overlays;
using Content.Shared.SS220.Ghost;
using Content.Shared.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Client.UserInterface.Controls;

namespace Content.IntegrationTests.Tests.SS220.Ghost;

[TestFixture]
public sealed class GhostHudTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = """
        - type: ghostHud
          id: TestGhostMedical
          name: ghost-hud-medical
          components:
          - type: ShowHealthBars
            damageContainers: [Ipc]
          - type: ShowHealthIcons
            damageContainers: [Ipc]

        - type: entity
          parent: MobObserver
          id: TestGhostHudObserver
          components:
          - type: GhostHudSettings
            huds:
            - id: TestGhostMedical
              enabled: false

        - type: entity
          parent: AdminObserver
          id: TestGhostHudAdmin
          components:
          - type: GhostHudSettings
            huds:
            - id: Security
              enabled: true
            - id: TestGhostMedical
              enabled: true
        """;

    public override PoolSettings PoolSettings => new()
    {
        Connected = true,
        DummyTicker = false,
        Dirty = true,
    };

    [TestCase("MobObserver")]
    [TestCase("AdminObserver")]
    public async Task HudSettingsToggleIndependently(string prototype)
    {
        await Pair.CreateTestMap();

        EntityUid ghost = default;
        NetEntity netGhost = default;
        EntityUid actionUid = default;
        var actions = SEntMan.System<SharedActionsSystem>();
        var serverUi = SEntMan.System<SharedUserInterfaceSystem>();

        await Server.WaitAssertion(() =>
        {
            ghost = SEntMan.SpawnEntity(prototype, Pair.TestMap!.GridCoords);
            netGhost = SEntMan.GetNetEntity(ghost);
            var session = Server.PlayerMan.GetSessionById(Client.Session!.UserId);
            Server.PlayerMan.SetAttachedEntity(session, ghost);

            var entry = SEntMan.GetComponent<IntrinsicUIComponent>(ghost).UIs[GhostHudUiKey.Key];
            Assert.That(entry.ToggleActionEntity, Is.Not.Null);
            actionUid = entry.ToggleActionEntity!.Value;
            var action = actions.GetAction(actionUid);
            Assert.That(action, Is.Not.Null);
            actions.PerformAction(ghost, action!.Value);
            Assert.That(serverUi.IsUiOpen(ghost, GhostHudUiKey.Key, ghost), Is.True);
        });

        await Pair.RunTicksSync(10);
        await AssertHuds(true, false);

        // Exercise the same client-to-server messages sent by the checkboxes.
        await Toggle("Medical", false);
        await AssertHuds(false, false);
        await Toggle("Security", true);
        await AssertHuds(false, true);
        await Toggle("Medical", true);
        await AssertHuds(true, true);
        await Toggle("Security", false);
        await AssertHuds(true, false);
        await Toggle("Medical", false);
        await AssertHuds(false, false);
        await Toggle("Medical", true);
        await AssertHuds(true, false);

        // Closing and reopening the window must keep the selected settings.
        await Server.WaitAssertion(() =>
        {
            actions.PerformAction(ghost, actions.GetAction(actionUid)!.Value);
            Assert.That(serverUi.IsUiOpen(ghost, GhostHudUiKey.Key, ghost), Is.False);
        });
        await Pair.RunTicksSync(5);
        await Server.WaitAssertion(() =>
        {
            actions.PerformAction(ghost, actions.GetAction(actionUid)!.Value);
            Assert.That(serverUi.IsUiOpen(ghost, GhostHudUiKey.Key, ghost), Is.True);
        });
        await Pair.RunTicksSync(5);
        await AssertHuds(true, false);

        async Task Toggle(ProtoId<GhostHudPrototype> hud, bool enabled)
        {
            await Client.WaitPost(() =>
            {
                var clientGhost = CEntMan.GetEntity(netGhost);
                CEntMan.System<SharedUserInterfaceSystem>().ClientSendUiMessage(
                    clientGhost, GhostHudUiKey.Key, new GhostHudToggledMessage(hud, enabled));
            });
            await Pair.RunTicksSync(10);
        }

        async Task AssertHuds(bool medical, bool security)
        {
            await Server.WaitAssertion(() =>
            {
                Assert.That(SEntMan.HasComponent<ShowHealthBarsComponent>(ghost), Is.EqualTo(medical));
                Assert.That(SEntMan.HasComponent<ShowHealthIconsComponent>(ghost), Is.EqualTo(medical));
                Assert.That(SEntMan.HasComponent<ShowJobIconsComponent>(ghost), Is.EqualTo(security));
                Assert.That(SEntMan.HasComponent<ShowMindShieldIconsComponent>(ghost), Is.EqualTo(security));
                Assert.That(SEntMan.HasComponent<ShowCriminalRecordIconsComponent>(ghost), Is.EqualTo(security));
            });

            await Client.WaitAssertion(() =>
            {
                var clientGhost = CEntMan.GetEntity(netGhost);
                var ui = CEntMan.System<SharedUserInterfaceSystem>();
                Assert.That(ui.TryGetUiState<GhostHudBoundUserInterfaceState>(clientGhost, GhostHudUiKey.Key, out var state), Is.True);
                Assert.That(state!.Huds.Select(hud => hud.Id.Id), Is.EqualTo(new[] { "Medical", "Security" }));
                Assert.That(state.Huds[0].Enabled, Is.EqualTo(medical));
                Assert.That(state.Huds[1].Enabled, Is.EqualTo(security));
                Assert.That(CEntMan.System<ShowHealthBarsSystem>().IsActive, Is.EqualTo(medical));
                Assert.That(CEntMan.System<ShowHealthIconsSystem>().IsActive, Is.EqualTo(medical));
                Assert.That(CEntMan.System<ShowJobIconsSystem>().IsActive, Is.EqualTo(security));

                if (!medical)
                    return;

                var healthBars = CEntMan.GetComponent<ShowHealthBarsComponent>(clientGhost);
                var healthIcons = CEntMan.GetComponent<ShowHealthIconsComponent>(clientGhost);
                Assert.That(healthBars.DamageContainers, Does.Contain("Ipc"));
                Assert.That(healthIcons.DamageContainers, Does.Contain("Ipc"));
                Assert.That(healthBars.DamageContainers, Does.Contain("Biological"));
                Assert.That(healthIcons.DamageContainers, Does.Contain("Biological"));
            });
        }
    }

    [TestCase("TestGhostHudObserver", false)]
    [TestCase("TestGhostHudAdmin", true)]
    public async Task HudChoicesAndDefaultsComeFromPrototype(string prototype, bool admin)
    {
        await Pair.CreateTestMap();
        EntityUid ghost = default;
        NetEntity netGhost = default;

        await Server.WaitAssertion(() =>
        {
            ghost = SEntMan.SpawnEntity(prototype, Pair.TestMap!.GridCoords);
            netGhost = SEntMan.GetNetEntity(ghost);
            var session = Server.PlayerMan.GetSessionById(Client.Session!.UserId);
            Server.PlayerMan.SetAttachedEntity(session, ghost);
            Assert.That(SEntMan.System<IntrinsicUISystem>().InteractUI(ghost, GhostHudUiKey.Key), Is.True);
        });

        await Pair.RunTicksSync(10);
        await AssertSettings(admin);

        // Neither a known HUD outside this entity's list nor an unknown ID may be enabled.
        await SendToggle("Medical", true);
        await SendToggle("UnknownGhostHud", true);
        if (!admin)
            await SendToggle("Security", true);

        await AssertSettings(admin);
        await SendToggle("TestGhostMedical", !admin);
        await AssertSettings(!admin);
        await SendToggle("TestGhostMedical", admin);
        await AssertSettings(admin);
        await SendToggle("TestGhostMedical", true);
        await AssertSettings(true);
        await SendToggle("TestGhostMedical", true);
        await AssertSettings(true);

        // A player's changes must not alter the defaults used by another ghost.
        await Server.WaitAssertion(() =>
        {
            var otherGhost = SEntMan.SpawnEntity(prototype, Pair.TestMap!.GridCoords);
            var settings = SEntMan.GetComponent<GhostHudSettingsComponent>(otherGhost);
            Assert.That(settings.Huds.Single(hud => hud.Id == "TestGhostMedical").Enabled, Is.EqualTo(admin));
            Assert.That(SEntMan.HasComponent<ShowHealthBarsComponent>(otherGhost), Is.EqualTo(admin));
        });

        async Task SendToggle(ProtoId<GhostHudPrototype> hud, bool enabled)
        {
            await Client.WaitPost(() =>
            {
                CEntMan.System<SharedUserInterfaceSystem>().ClientSendUiMessage(
                    CEntMan.GetEntity(netGhost), GhostHudUiKey.Key, new GhostHudToggledMessage(hud, enabled));
            });
            await Pair.RunTicksSync(10);
        }

        async Task AssertSettings(bool medical)
        {
            await Client.WaitAssertion(() =>
            {
                var clientGhost = CEntMan.GetEntity(netGhost);
                var ui = CEntMan.System<SharedUserInterfaceSystem>();
                Assert.That(ui.TryGetUiState<GhostHudBoundUserInterfaceState>(clientGhost, GhostHudUiKey.Key, out var state), Is.True);
                var expectedIds = admin ? new[] { "Security", "TestGhostMedical" } : new[] { "TestGhostMedical" };
                Assert.That(state!.Huds.Select(hud => hud.Id.Id), Is.EqualTo(expectedIds));
                Assert.That(state.Huds.Single(hud => hud.Id == "TestGhostMedical").Enabled, Is.EqualTo(medical));
                Assert.That(CEntMan.System<ShowHealthBarsSystem>().IsActive, Is.EqualTo(medical));
                Assert.That(CEntMan.System<ShowHealthIconsSystem>().IsActive, Is.EqualTo(medical));
                Assert.That(CEntMan.System<ShowJobIconsSystem>().IsActive, Is.EqualTo(admin));
                Assert.That(CEntMan.HasComponent<ShowCriminalRecordIconsComponent>(clientGhost), Is.EqualTo(admin));

                using var window = new GhostHudWindow();
                window.SetState(state);
                window.SetState(state);
                var checkboxes = window.FindControl<BoxContainer>("HudList").Children.OfType<CheckBox>().ToArray();
                Assert.That(checkboxes.Select(box => box.Pressed), Is.EqualTo(state.Huds.Select(hud => hud.Enabled)));

                if (!medical)
                    return;

                Assert.That(CEntMan.GetComponent<ShowHealthBarsComponent>(clientGhost).DamageContainers.Select(id => id.Id),
                    Is.EqualTo(new[] { "Ipc" }));
                Assert.That(CEntMan.GetComponent<ShowHealthIconsComponent>(clientGhost).DamageContainers.Select(id => id.Id),
                    Is.EqualTo(new[] { "Ipc" }));
            });
        }
    }
}
