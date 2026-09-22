using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.GameTicking;
using Content.Server.StationEvents;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.GameRules;

public sealed class EventAvailabilityTest : GameTest
{
    public override PoolSettings PoolSettings => new() { InLobby = true, Dirty = true, Fresh = true };

    [TestPrototypes]
    internal const string Prototypes = """
        - type: entity
          id: EventAvailabilityTestRepeatable
          parent: BaseGameRule
          components:
          - type: GameRule
            category: Incidents
          - type: StationEvent
            earliestStart: 0
            reoccurrenceDelay: 20
            duration: null
        - type: entity
          id: EventAvailabilityTestLimited
          parent: EventAvailabilityTestRepeatable
          components:
          - type: StationEvent
            maxOccurrences: 1
        - type: entity
          id: EventAvailabilityTestTimed
          parent: EventAvailabilityTestRepeatable
          components:
          - type: StationEvent
            duration: 60
        """;

    [TestCase("EventAvailabilityTestRepeatable", true)]
    [TestCase("EventAvailabilityTestLimited", false)]
    [TestCase("EventAvailabilityTestTimed", true)]
    public async Task RepeatIntervalAndRoundLimitApplyToActiveAndEndedEvents(
        string prototypeId,
        bool canRepeat)
    {
        await Server.WaitAssertion(() =>
        {
            var ticker = SEntMan.System<ServerGameTicker>();
            var events = SEntMan.System<EventManagerSystem>();
            var history = SEntMan.System<GameRuleHistorySystem>();
            ticker.ClearGameRules();
            var prototype = SProtoMan.Index<EntityPrototype>(prototypeId);
            EntProtoId[] candidates = [prototypeId];

            void AssertAvailable(TimeSpan time, bool expected)
            {
                Assert.That(events.AvailableEvents(1, time).ContainsKey(prototype), Is.EqualTo(expected));
                Assert.That(events.TryBuildLimitedEvents(candidates, out var available, time, 1), Is.EqualTo(expected));
                Assert.That(available.ContainsKey(prototype), Is.EqualTo(expected));
            }

            // A repeat interval must not delay the first occurrence.
            AssertAvailable(TimeSpan.FromMinutes(1), true);
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
            var rule = ticker.AddGameRule(prototypeId)!.Value.Owner;
            Assert.That(ticker.StartGameRule(rule), Is.True);
            Assert.That(ticker.IsGameRuleActive(rule), Is.True);
            Assert.That(history.GetStartStatistics(prototypeId).Count, Is.EqualTo(1));
            Assert.That(history.GetStartStatistics(prototypeId).LastStart, Is.EqualTo(TimeSpan.Zero));

            var interval = TimeSpan.FromMinutes(20);
            // Elapsed cooldown does not permit overlapping instances of an active event.
            AssertAvailable(interval - TimeSpan.FromSeconds(1), false);
            AssertAvailable(interval, false);
            AssertAvailable(interval + TimeSpan.FromSeconds(1), false);

            Assert.That(ticker.EndGameRule(rule), Is.True);
            AssertAvailable(interval - TimeSpan.FromSeconds(1), false);
            AssertAvailable(interval, canRepeat);
            AssertAvailable(interval + TimeSpan.FromSeconds(1), canRepeat);

            // Deleting an ended entity must not reset its cooldown or round limit.
            SEntMan.DeleteEntity(rule);
            AssertAvailable(interval - TimeSpan.FromSeconds(1), false);
            AssertAvailable(interval, canRepeat);
        });
    }
}
