using Content.Client.Overlays;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Actions;
using Content.Shared.Overlays;
using Content.Shared.SS220.Ghost;
using Content.Shared.UserInterface;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.SS220.Ghost;

[TestFixture]
public sealed class GhostHudTest : GameTest
{
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
        await Toggle(GhostHudType.Medical, false);
        await AssertHuds(false, false);
        await Toggle(GhostHudType.Security, true);
        await AssertHuds(false, true);
        await Toggle(GhostHudType.Medical, true);
        await AssertHuds(true, true);
        await Toggle(GhostHudType.Security, false);
        await AssertHuds(true, false);
        await Toggle(GhostHudType.Medical, false);
        await AssertHuds(false, false);
        await Toggle(GhostHudType.Medical, true);
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

        async Task Toggle(GhostHudType hud, bool enabled)
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
                Assert.That(SEntMan.HasComponent<GhostHudOnOtherComponent>(ghost), Is.EqualTo(security));
                Assert.That(SEntMan.HasComponent<ShowJobIconsComponent>(ghost), Is.EqualTo(security));
                Assert.That(SEntMan.HasComponent<ShowMindShieldIconsComponent>(ghost), Is.EqualTo(security));
                Assert.That(SEntMan.HasComponent<ShowCriminalRecordIconsComponent>(ghost), Is.EqualTo(security));
            });

            await Client.WaitAssertion(() =>
            {
                var clientGhost = CEntMan.GetEntity(netGhost);
                var ui = CEntMan.System<SharedUserInterfaceSystem>();
                Assert.That(ui.TryGetUiState<GhostHudBoundUserInterfaceState>(clientGhost, GhostHudUiKey.Key, out var state), Is.True);
                Assert.That(state!.MedicalEnabled, Is.EqualTo(medical));
                Assert.That(state.SecurityEnabled, Is.EqualTo(security));
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
}
