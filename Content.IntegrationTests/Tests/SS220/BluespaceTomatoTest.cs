// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Tests.Movement;
using Content.Server.Botany;
using Content.Server.Botany.Systems;
using Content.Server.SS220.Teleport.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.SS220.Teleport;
using Content.Shared.SS220.Teleport.Triggers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Content.IntegrationTests.Tests.SS220;

public sealed class BluespaceTomatoTest : MovementTest
{
    private static readonly EntProtoId Tomato = "FoodBluespaceTomato";
    private static readonly ProtoId<SeedPrototype> Seed = "bluespaceTomato";

    [TestCase(false, true)]
    [TestCase(true, true)]
    [TestCase(false, false)]
    public async Task EatingTeleportsEater(bool forceFeed, bool triggerEnabled)
    {
        var tomato = await PlaceInHands(Tomato);
        var eater = SPlayer;
        if (forceFeed)
        {
            var coordinates = ToServer(PlayerCoords).Offset(new Vector2(0.8f, 0));
            eater = ToServer(await SpawnTarget("MobHuman", SEntMan.GetNetCoordinates(coordinates)));
        }

        var origin = Transform.GetWorldPosition(eater);
        var feederOrigin = Transform.GetWorldPosition(SPlayer);
        await Server.WaitPost(() =>
        {
            if (!triggerEnabled)
                SEntMan.RemoveComponent<IngestedTeleportTriggerComponent>(ToServer(tomato));

            Assert.That(Server.System<IngestionSystem>().TryIngest(SPlayer, eater, ToServer(tomato)), Is.True);
        });
        await RunSeconds(10);

        var distance = Vector2.Distance(origin, Transform.GetWorldPosition(eater));
        if (triggerEnabled)
            Assert.That(distance, Is.GreaterThan(0.5f));
        else
            Assert.That(distance, Is.LessThan(0.01f));
        AssertDeleted(tomato);
        if (forceFeed)
            Assert.That(Transform.GetWorldPosition(SPlayer), Is.EqualTo(feederOrigin));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ThrowingTeleportsTarget(bool triggerEnabled)
    {
        var tomato = await PlaceInHands(Tomato);
        var target = ToServer(await SpawnTarget("MobHuman"));
        var origin = Transform.GetWorldPosition(target);
        var throwerOrigin = Transform.GetWorldPosition(SPlayer);

        if (!triggerEnabled)
        {
            await Server.WaitPost(() =>
                SEntMan.RemoveComponent<ThrownHitTeleportTriggerComponent>(ToServer(tomato)));
        }

        Assert.That(await ThrowItem(), Is.True);
        await RunSeconds(2);

        var distance = Vector2.Distance(origin, Transform.GetWorldPosition(target));
        if (triggerEnabled)
            Assert.That(distance, Is.GreaterThan(0.5f));
        else
            Assert.That(distance, Is.LessThan(0.5f));
        Assert.That(Transform.GetWorldPosition(SPlayer), Is.EqualTo(throwerOrigin));
        AssertDeleted(tomato);
    }

    [TestCase(1f, false)]
    [TestCase(3f, true)]
    public async Task ConfiguredRadiusWorksWithoutProduceAndCanBeReused(float radius, bool canTeleport)
    {
        var originCoordinates = ToServer(PlayerCoords);
        await SetTile(Plating, SEntMan.GetNetCoordinates(originCoordinates.Offset(new Vector2(6, 0))), MapData.Grid);

        await Server.WaitPost(() =>
        {
            var coordinates = Transform.GetMoverCoordinates(SPlayer);
            var destination = Transform.ToWorldPosition(coordinates.Offset(new Vector2(2, 0)));
            foreach (var offset in new[] { -2, -1, 1 })
                SEntMan.SpawnEntity(WallPrototype, coordinates.Offset(new Vector2(offset, 0)));

            // No botany, ingestion, throwing or one-use components are needed by the destination provider.
            var teleporter = SEntMan.SpawnEntity(null, coordinates);
            var serialization = Server.ResolveDependency<ISerializationManager>();
            var node = new MappingDataNode { { "radius", serialization.WriteValue(radius) } };
            var component = serialization.Read<RandomTeleportInRadiusComponent>(node, notNullableOverride: true);
            SEntMan.AddComponent(teleporter, component);
            for (var i = 0; i < 2; i++)
            {
                Transform.SetCoordinates(SPlayer, coordinates);
                var request = new TeleportRequestEvent(SPlayer, SPlayer);
                SEntMan.EventBus.RaiseLocalEvent(teleporter, ref request);
                Assert.That(request.Handled, Is.EqualTo(canTeleport));
                if (!canTeleport)
                {
                    Assert.That(Transform.GetMoverCoordinates(SPlayer), Is.EqualTo(coordinates));
                    continue;
                }

                Assert.That(Vector2.Distance(Transform.GetWorldPosition(SPlayer), destination), Is.LessThan(0.01f),
                    "The two-metre tile is available; the six-metre tile is outside the base radius.");
            }
        });
    }

    [Test]
    public async Task RadiusConfigurationIsRequiredAndValidated()
    {
        await Server.WaitPost(() =>
        {
            var serialization = Server.ResolveDependency<ISerializationManager>();
            Assert.Throws<RequiredFieldNotMappedException>(() =>
                serialization.Read<RandomTeleportInRadiusComponent>(new MappingDataNode(), notNullableOverride: true));

            foreach (var radius in new[] { "0", "-1", "NaN", "Infinity", "-Infinity" })
            {
                var node = new MappingDataNode { { "radius", new ValueDataNode(radius) } };
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    serialization.Read<RandomTeleportInRadiusComponent>(node, notNullableOverride: true));
            }

            var valid = new MappingDataNode { { "radius", new ValueDataNode("3") } };
            var component = serialization.Read<RandomTeleportInRadiusComponent>(valid, notNullableOverride: true);
            Assert.That(component.Radius, Is.EqualTo(3f));
        });
    }

    [TestCase(0)]
    [TestCase(45)]
    public async Task HarvestedPotencyControlsRangeAndAvoidsWalls(int rotation)
    {
        await Server.WaitPost(() =>
        {
            Transform.SetLocalRotation(MapData.Grid, Angle.FromDegrees(rotation));
            var origin = Transform.GetWorldPosition(SPlayer);
            var coordinates = Transform.GetMoverCoordinates(SPlayer);
            var expectedDestination = Transform.ToWorldPosition(coordinates.Offset(new Vector2(2, 0)));
            // Leave only the tile two metres to the right as a possible destination.
            foreach (var offset in new[] { -2, -1, 1 })
                SEntMan.SpawnEntity(WallPrototype, coordinates.Offset(new Vector2(offset, 0)));

            var seed = ProtoMan.Index(Seed).Clone();
            seed.Yield = 1;
            seed.Potency = 2;
            var tomato = Server.System<BotanySystem>().GenerateProduct(seed, coordinates).Single();

            var shortRange = new TeleportRequestEvent(SPlayer, SPlayer);
            SEntMan.EventBus.RaiseLocalEvent(tomato, ref shortRange);
            Assert.That(shortRange.Handled, Is.False, "There is no free floor tile within one metre.");
            Assert.That(Transform.GetWorldPosition(SPlayer), Is.EqualTo(origin));

            seed.Potency = 10;
            var longRange = new TeleportRequestEvent(SPlayer, SPlayer);
            SEntMan.EventBus.RaiseLocalEvent(tomato, ref longRange);
            Assert.That(longRange.Handled, Is.True);
            Assert.That(Vector2.Distance(Transform.GetWorldPosition(SPlayer), expectedDestination), Is.LessThan(0.01f));

            var repeated = new TeleportRequestEvent(SPlayer, SPlayer);
            SEntMan.EventBus.RaiseLocalEvent(tomato, ref repeated);
            Assert.That(repeated.Handled, Is.False, "One tomato must not teleport twice before queued deletion.");
        });
    }
}
