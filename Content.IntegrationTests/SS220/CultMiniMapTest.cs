// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.CultMiniMap;
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.SS220;

public sealed class CultMiniMapTest : GameTest
{
    public override PoolSettings PoolSettings => PsDisconnected;

    [TestPrototypes]
    private const string HealthPrototype = """
- type: entity
  id: CultMiniMapHealthDummy
  components:
  - type: Damageable
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      100: Critical
      200: Dead
    triggersAlerts: false
    allowRevives: true

- type: entity
  id: CultMiniMapConfiguredViewer
  components:
  - type: CultMiniMap
    trackedComponents:
    - component: CultYogg
      label: cult-mini-map-cultist
      icon: /Textures/Interface/NavMap/beveled_diamond.png
      color: Violet
      scale: 0.75
    - component: MobState
      icon:
        sprite: SS220/Interface/Actions/cult_yogg.rsi
        state: migo_teleport
      color: White
      scale: 1.5
""";

    [Test]
    public async Task ConfiguredRulesSelectTargetsAndAppearances()
    {
        var map = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();

        await Server.WaitAssertion(() =>
        {
            var viewer = SEntMan.SpawnEntity("CultMiniMapConfiguredViewer", map.GridCoords);
            var cultist = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(cultist);
            var mob = SEntMan.SpawnEntity("CultMiniMapHealthDummy", map.GridCoords);
            var both = SEntMan.SpawnEntity("CultMiniMapHealthDummy", map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(both);
            var miGo = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<MiGoComponent>(miGo);

            Assert.That(SEntMan.GetComponent<CultMiniMapComponent>(viewer).MiniMapActionEntity, Is.Not.Null);
            Assert.That(SEntMan.HasComponent<CultMiniMapComponent>(mob), Is.False,
                "Being tracked does not grant the ability to view the map.");
            ui.OpenUi(viewer, CultMiniMapUIKey.Key, viewer);
            var state = GetState(viewer);
            Assert.That(state.Members.Select(member => member.Entity), Is.EquivalentTo(new[]
            {
                SEntMan.GetNetEntity(viewer), SEntMan.GetNetEntity(cultist),
                SEntMan.GetNetEntity(mob), SEntMan.GetNetEntity(both),
            }), "Custom rules replace the defaults, while the viewer remains in their own section.");

            var selfMarker = state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(viewer)).Marker;
            Assert.That(selfMarker.Component, Is.EqualTo(CultMiniMapMarker.SelfComponent));
            Assert.That(selfMarker.Label?.ToString(), Is.EqualTo("cult-mini-map-self-section"));
            Assert.That(selfMarker.Icon, Is.EqualTo(new SpriteSpecifier.Texture(
                new ResPath("/Textures/Interface/NavMap/beveled_star.png"))));
            Assert.That(selfMarker.Color, Is.EqualTo(Color.Cyan));
            Assert.That(selfMarker.Scale, Is.EqualTo(1.2f));

            var cultMarker = state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(cultist)).Marker;
            Assert.That(cultMarker.Component, Is.EqualTo("CultYogg"));
            Assert.That(cultMarker.Label?.ToString(), Is.EqualTo("cult-mini-map-cultist"));
            Assert.That(cultMarker.Icon, Is.EqualTo(new SpriteSpecifier.Texture(
                new ResPath("/Textures/Interface/NavMap/beveled_diamond.png"))));
            Assert.That(cultMarker.Color, Is.EqualTo(Color.Violet));
            Assert.That(cultMarker.Scale, Is.EqualTo(0.75f));
            Assert.That(state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(both)).Marker.Component,
                Is.EqualTo("CultYogg"), "The first matching rule wins; there must be no duplicate markers.");

            var mobMarker = state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(mob)).Marker;
            Assert.That(mobMarker.Component, Is.EqualTo("MobState"));
            Assert.That(mobMarker.Label, Is.Null);
            Assert.That(mobMarker.Icon, Is.EqualTo(new SpriteSpecifier.Rsi(
                new ResPath("SS220/Interface/Actions/cult_yogg.rsi"), "migo_teleport")));
            Assert.That(mobMarker.Color, Is.EqualTo(Color.White));
            Assert.That(mobMarker.Scale, Is.EqualTo(1.5f));

            // Configuration is per observer; another map still uses its own defaults.
            ui.OpenUi(cultist, CultMiniMapUIKey.Key, cultist);
            Assert.That(GetState(cultist).Members.Select(member => member.Entity), Is.EquivalentTo(new[]
            {
                SEntMan.GetNetEntity(cultist), SEntMan.GetNetEntity(both), SEntMan.GetNetEntity(miGo),
            }));
            ui.CloseUi(cultist, CultMiniMapUIKey.Key);
            ui.CloseUi(viewer, CultMiniMapUIKey.Key);
        });
    }

    [Test]
    public async Task ConfigurationChangesUpdateOpenMap()
    {
        var map = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();
        EntityUid viewer = default;
        EntityUid target = default;
        CultMiniMapMarker oldMarker = default!;

        await Server.WaitAssertion(() =>
        {
            viewer = SEntMan.SpawnEntity("CultMiniMapConfiguredViewer", map.GridCoords);
            target = SEntMan.SpawnEntity("CultMiniMapHealthDummy", map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(target);
            ui.OpenUi(viewer, CultMiniMapUIKey.Key, viewer);
            oldMarker = GetState(viewer).Members
                .Single(member => member.Entity == SEntMan.GetNetEntity(target)).Marker;

            var rules = SEntMan.GetComponent<CultMiniMapComponent>(viewer).TrackedComponents;
            rules[0].Color = Color.Red;
            rules.RemoveAt(0);
            rules[0].Color = Color.Green;
            rules[0].Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_star.png"));
            rules[0].Scale = 0f;
            // Invalid names can be entered through runtime edits; they must not break other rules.
            rules.Insert(0, new CultMiniMapTrackedComponent { Component = "NonexistentMiniMapTestComponent" });
        });

        await Server.WaitRunTicks(120);
        await Server.WaitAssertion(() =>
        {
            var marker = GetState(viewer).Members
                .Single(member => member.Entity == SEntMan.GetNetEntity(target)).Marker;
            Assert.That(marker.Component, Is.EqualTo("MobState"));
            Assert.That(marker.Color, Is.EqualTo(Color.Green));
            Assert.That(marker.Icon, Is.EqualTo(new SpriteSpecifier.Texture(
                new ResPath("/Textures/Interface/NavMap/beveled_star.png"))));
            Assert.That(marker.Scale, Is.EqualTo(1f), "Invalid sizes must not hide or corrupt map markers.");
            Assert.That(oldMarker.Component, Is.EqualTo("CultYogg"));
            Assert.That(oldMarker.Color, Is.EqualTo(Color.Violet), "Published states must not change with the config.");
            SEntMan.GetComponent<CultMiniMapComponent>(viewer).TrackedComponents.Clear();
        });

        await Server.WaitRunTicks(120);
        await Server.WaitAssertion(() =>
        {
            var members = GetState(viewer).Members;
            Assert.That(members, Has.Count.EqualTo(1));
            Assert.That(members.Single().Entity, Is.EqualTo(SEntMan.GetNetEntity(viewer)));
            Assert.That(members.Single().Marker.Component, Is.EqualTo(CultMiniMapMarker.SelfComponent));
            ui.CloseUi(viewer, CultMiniMapUIKey.Key);
        });
    }

    [Test]
    public async Task HealthUpdatesWithoutSensorsAndHandlesMissingData()
    {
        var map = await Pair.CreateTestMap();
        var otherMap = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();
        var damage = SEntMan.System<DamageableSystem>();
        var thresholds = SEntMan.System<MobThresholdSystem>();
        EntityUid viewer = default;
        EntityUid target = default;
        EntityUid miGo = default;
        EntityUid noThreshold = default;

        await Server.WaitAssertion(() =>
        {
            viewer = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(viewer);
            // Health remains available even when coordinates cannot be shown on this map.
            target = SEntMan.SpawnEntity("CultMiniMapHealthDummy", otherMap.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(target);
            miGo = SEntMan.SpawnEntity("MobMiGo", map.GridCoords);
            damage.SetDamage(miGo, new DamageSpecifier { DamageDict = { ["Blunt"] = 70 } });
            noThreshold = SEntMan.SpawnEntity(null, map.GridCoords);
            SEntMan.AddComponent<MiGoComponent>(noThreshold);
            SEntMan.AddComponent<MobStateComponent>(noThreshold);
            SEntMan.AddComponent<DamageableComponent>(noThreshold);

            ui.OpenUi(viewer, CultMiniMapUIKey.Key, viewer);
            var state = GetState(viewer);
            var self = state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(viewer));
            Assert.That(self.HealthState, Is.EqualTo(MobState.Invalid));
            Assert.That(self.DamagePercentage, Is.Null);
            var noThresholdState = state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(noThreshold));
            Assert.That(noThresholdState.HealthState, Is.EqualTo(MobState.Alive));
            Assert.That(noThresholdState.DamagePercentage, Is.Null);
            var miGoState = state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(miGo));
            Assert.That(miGoState.HealthState, Is.EqualTo(MobState.Alive));
            Assert.That(miGoState.DamagePercentage, Is.EqualTo(0.5f).Within(0.001f));
        });

        // Exercise damage, critical state, death and healing through the normal damage system.
        foreach (var (amount, expectedState) in new[]
        {
            (0, MobState.Alive), (50, MobState.Alive), (110, MobState.Critical),
            (210, MobState.Dead), (0, MobState.Alive),
        })
        {
            await Server.WaitPost(() =>
                damage.SetDamage(target, new DamageSpecifier { DamageDict = { ["Blunt"] = amount } }));
            await Server.WaitRunTicks(120);
            await Server.WaitAssertion(() =>
            {
                var member = GetState(viewer).Members.Single(member => member.Entity == SEntMan.GetNetEntity(target));
                Assert.That(member.HealthState, Is.EqualTo(expectedState));
                Assert.That(member.DamagePercentage, Is.EqualTo(amount / 100f).Within(0.001f));
                Assert.That(member.Coordinates, Is.Null);
            });
        }

        await Server.WaitPost(() => thresholds.SetMobStateThreshold(target, 0, MobState.Critical));
        await Server.WaitRunTicks(120);
        await Server.WaitAssertion(() =>
        {
            var member = GetState(viewer).Members.Single(member => member.Entity == SEntMan.GetNetEntity(target));
            Assert.That(member.DamagePercentage, Is.Null, "A zero threshold must not produce NaN or infinity.");
            ui.CloseUi(viewer, CultMiniMapUIKey.Key);
            SEntMan.DeleteEntity(map.MapUid);
        });
    }

    [Test]
    public async Task MembersAndPositionsUpdateWithoutSuitSensors()
    {
        var map = await Pair.CreateTestMap();
        var otherMap = await Pair.CreateTestMap();
        EntityUid viewer = default;
        EntityUid cultist = default;
        EntityUid miGo = default;
        EntityUid both = default;
        EntityUid outsider = default;
        EntityUid remote = default;
        var ui = SEntMan.System<SharedUserInterfaceSystem>();
        var transform = SEntMan.System<SharedTransformSystem>();

        await Server.WaitAssertion(() =>
        {
            viewer = SEntMan.SpawnEntity(null, map.GridCoords);
            cultist = SEntMan.SpawnEntity(null, new EntityCoordinates(map.Grid, 200, 0));
            miGo = SEntMan.SpawnEntity(null, map.GridCoords);
            both = SEntMan.SpawnEntity(null, map.GridCoords);
            outsider = SEntMan.SpawnEntity(null, map.GridCoords);
            remote = SEntMan.SpawnEntity(null, otherMap.GridCoords);
            SEntMan.AddComponent<CultYoggComponent>(viewer);
            SEntMan.AddComponent<CultYoggComponent>(cultist);
            SEntMan.AddComponent<MiGoComponent>(miGo);
            SEntMan.AddComponent<CultYoggComponent>(both);
            SEntMan.AddComponent<MiGoComponent>(both);
            SEntMan.AddComponent<MiGoComponent>(remote);

            ui.OpenUi(viewer, CultMiniMapUIKey.Key, viewer);
            var state = GetState(viewer);
            Assert.That(state.Members.Select(member => member.Entity), Is.EquivalentTo(new[]
            {
                SEntMan.GetNetEntity(viewer), SEntMan.GetNetEntity(cultist), SEntMan.GetNetEntity(miGo),
                SEntMan.GetNetEntity(both), SEntMan.GetNetEntity(remote),
            }));
            var miGoMarker = state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(both)).Marker;
            Assert.That(miGoMarker.Component, Is.EqualTo("MiGo"));
            Assert.That(miGoMarker.Icon, Is.EqualTo(new SpriteSpecifier.Texture(
                new ResPath("/Textures/SS220/Interface/NavMap/migo.png"))));
            Assert.That(state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(remote)).Coordinates, Is.Null);
            var position = state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(cultist)).Coordinates;
            Assert.That(position.HasValue, Is.True);
            Assert.That(position.Value.NetEntity, Is.EqualTo(SEntMan.GetNetEntity(map.Grid)));
            Assert.That(position.Value.Position.X, Is.EqualTo(200f));

            transform.SetCoordinates(cultist, new EntityCoordinates(map.Grid, 250, 0));
            SEntMan.RemoveComponent<CultYoggComponent>(both);
            SEntMan.RemoveComponent<MiGoComponent>(both);
            SEntMan.DeleteEntity(miGo);
            SEntMan.AddComponent<CultYoggComponent>(outsider);
        });

        await Server.WaitRunTicks(120);

        await Server.WaitAssertion(() =>
        {
            var state = GetState(viewer);
            Assert.That(state.Members.Select(member => member.Entity), Is.EquivalentTo(new[]
            {
                SEntMan.GetNetEntity(viewer), SEntMan.GetNetEntity(cultist),
                SEntMan.GetNetEntity(outsider), SEntMan.GetNetEntity(remote),
            }));
            Assert.That(state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(cultist)).Coordinates.Value.Position.X,
                Is.EqualTo(250f));

            transform.SetCoordinates(viewer, otherMap.GridCoords);
        });

        await Server.WaitRunTicks(120);

        await Server.WaitAssertion(() =>
        {
            var state = GetState(viewer);
            Assert.That(state.Grid, Is.EqualTo(SEntMan.GetNetEntity(otherMap.Grid)));
            Assert.That(state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(remote)).Coordinates, Is.Not.Null);
            Assert.That(state.Members.Single(member => member.Entity == SEntMan.GetNetEntity(cultist)).Coordinates, Is.Null);
            transform.SetCoordinates(viewer, new EntityCoordinates(otherMap.MapUid, 50, 0));
        });

        await Server.WaitRunTicks(120);

        await Server.WaitAssertion(() =>
        {
            var state = GetState(viewer);
            Assert.That(state.Grid, Is.Null);
            Assert.That(state.Members, Has.Count.EqualTo(4));
            Assert.That(state.Members.All(member => member.Coordinates == null), Is.True);
            ui.CloseUi(viewer, CultMiniMapUIKey.Key);
            Assert.That(ui.TryGetUiState<CultMiniMapState>(viewer, CultMiniMapUIKey.Key, out _), Is.False);
            // Pair only tracks the last map for automatic cleanup.
            SEntMan.DeleteEntity(map.MapUid);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task AbilityFollowsEitherMembershipAndIsPrivate(bool cultistFirst)
    {
        var map = await Pair.CreateTestMap();
        var ui = SEntMan.System<SharedUserInterfaceSystem>();

        await Server.WaitAssertion(() =>
        {
            // Cover both conversion of an existing entity and the normal Mi-Go spawn path.
            var owner = SEntMan.SpawnEntity(cultistFirst ? null : "MobMiGo", map.GridCoords);
            var outsider = SEntMan.SpawnEntity(null, map.GridCoords);
            if (cultistFirst)
                SEntMan.AddComponent<CultYoggComponent>(owner);
            var ability = SEntMan.GetComponent<CultMiniMapComponent>(owner);
            var action = ability.MiniMapActionEntity;
            Assert.That(action, Is.Not.Null);
            Assert.That(SEntMan.GetComponent<ActionsComponent>(owner).Actions, Does.Contain(action.Value));
            ui.OpenUi(owner, CultMiniMapUIKey.Key, outsider);
            Assert.That(ui.IsUiOpen(owner, CultMiniMapUIKey.Key), Is.False);

            if (cultistFirst)
                SEntMan.AddComponent<MiGoComponent>(owner);
            else
                SEntMan.AddComponent<CultYoggComponent>(owner);

            Assert.That(SEntMan.GetComponent<CultMiniMapComponent>(owner).MiniMapActionEntity, Is.EqualTo(action));
            if (cultistFirst)
                SEntMan.RemoveComponent<CultYoggComponent>(owner);
            else
                SEntMan.RemoveComponent<MiGoComponent>(owner);

            Assert.That(SEntMan.HasComponent<CultMiniMapComponent>(owner), Is.True);
            ui.OpenUi(owner, CultMiniMapUIKey.Key, owner);
            Assert.That(ui.IsUiOpen(owner, CultMiniMapUIKey.Key), Is.True);

            if (cultistFirst)
                SEntMan.RemoveComponent<MiGoComponent>(owner);
            else
                SEntMan.RemoveComponent<CultYoggComponent>(owner);

            Assert.That(SEntMan.HasComponent<CultMiniMapComponent>(owner), Is.False);
            Assert.That(SEntMan.GetComponent<ActionsComponent>(owner).Actions, Does.Not.Contain(action.Value));
            Assert.That(ui.IsUiOpen(owner, CultMiniMapUIKey.Key), Is.False);
            Assert.That(ui.TryGetUiState<CultMiniMapState>(owner, CultMiniMapUIKey.Key, out _), Is.False);
            ui.OpenUi(owner, CultMiniMapUIKey.Key, owner);
            Assert.That(ui.IsUiOpen(owner, CultMiniMapUIKey.Key), Is.False);
        });
    }

    private CultMiniMapState GetState(EntityUid owner)
    {
        Assert.That(SEntMan.System<SharedUserInterfaceSystem>()
            .TryGetUiState<CultMiniMapState>(owner, CultMiniMapUIKey.Key, out var state), Is.True);
        return state;
    }
}
