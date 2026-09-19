using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Administration.Systems;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.StationEvents;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking;
using Content.Shared.Destructible.Thresholds;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests.Administration;

public sealed class AdminEventsTest : GameTest
{
    // DummyTicker skips round cleanup, so pooled pairs can retain another test's rule history.
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true, Fresh = true };

    [TestPrototypes]
    internal const string Prototypes = """
        - type: entity
          id: AdminEventsTestReady
          parent: BaseGameRule
          description: An event description for the information window.
          components:
          - type: StationEvent
            earliestStart: 0
            reoccurrenceDelay: 0
            maxOccurrences: 1
        - type: entity
          id: AdminEventsTestRepeatable
          parent: BaseGameRule
          components:
          - type: StationEvent
            earliestStart: 0
            reoccurrenceDelay: 0
        - type: entity
          id: AdminEventsTestCategoryParent
          parent: BaseGameRule
          abstract: true
          components:
          - type: StationEvent
            category: DerelictCyborgs
          - type: AntagSelection
            antags: []
        - type: entity
          id: AdminEventsTestCategoryInherited
          parent: AdminEventsTestCategoryParent
          components:
          - type: StationEvent
            minimumPlayers: 1
        - type: entity
          id: AdminEventsTestCategoryOverride
          parent: AdminEventsTestCategoryParent
          components:
          - type: StationEvent
            category: Effects
        - type: entity
          id: AdminEventsTestTooEarly
          parent: AdminEventsTestReady
          components:
          - type: StationEvent
            earliestStart: 100000
        - type: entity
          id: AdminEventsTestTooFew
          parent: AdminEventsTestReady
          components:
          - type: StationEvent
            minimumPlayers: 100000
        - type: entity
          id: AdminEventsTestZeroWeight
          parent: AdminEventsTestReady
          components:
          - type: StationEvent
            weight: 0
        - type: entity
          id: AdminEventsTestTableBlocked
          parent: AdminEventsTestReady
        - type: entity
          id: AdminEventsTestScheduler
          parent: BaseGameRule
          components:
          - type: BasicStationEventScheduler
            scheduledGameRules: !type:NestedSelector
              tableId: AdminEventsTestTable
        - type: entityTable
          id: AdminEventsTestTable
          table: !type:AllSelector
            children:
            - id: AdminEventsTestReady
            - id: AdminEventsTestRepeatable
            - id: AdminEventsTestTooEarly
            - id: AdminEventsTestTooFew
            - id: AdminEventsTestZeroWeight
            - id: AdminEventsTestTableBlocked
              conditions:
              - !type:HasBudgetCondition
                costOverride: 1
            - id: KingRatMigration
            - id: PowerGridCheck
            - id: RandomSentience
        """;

    [Test]
    public async Task AddingRulesValidatesPrototypesAndPermissions()
    {
        await Server.WaitAssertion(() =>
        {
            var admins = Server.ResolveDependency<IAdminManager>();
            var ticker = SEntMan.System<ServerGameTicker>();
            var events = SEntMan.System<AdminEventsSystem>();
            ticker.ClearGameRules();
            admins.PromoteHost(ServerSession);

            var availableRules = events.GetSnapshot().AvailableRules;
            AdminGameRulePrototypeInfo Rule(string id) => availableRules.Single(rule => rule.Id == id);
            Assert.That(Rule("AdminEventsTestReady").MinimumStartDelaySeconds, Is.Null);
            Assert.That(Rule("AdminEventsTestReady").MaximumStartDelaySeconds, Is.Null);
            Assert.That(Rule("AnomalySpawn").MinimumStartDelaySeconds, Is.EqualTo(10));
            Assert.That(Rule("AnomalySpawn").MaximumStartDelaySeconds, Is.EqualTo(20));
            Assert.That(Rule("BluespaceArtifact").MinimumStartDelaySeconds, Is.EqualTo(30));
            Assert.That(Rule("BluespaceArtifact").MaximumStartDelaySeconds, Is.EqualTo(30));
            Assert.That(Rule("DragonSpawn").Category, Is.EqualTo(AdminGameRuleCategory.Events));
            Assert.That(Rule("BasicStationEventScheduler").Category, Is.EqualTo(AdminGameRuleCategory.Schedulers));
            Assert.That(Rule("DynamicStationEventScheduler").Category, Is.EqualTo(AdminGameRuleCategory.Schedulers));
            Assert.That(Rule("ClericalError").EventCategory, Is.EqualTo(StationEventCategory.Effects));
            Assert.That(Rule("LoneOpsSpawn").EventCategory, Is.EqualTo(StationEventCategory.Antagonists));
            Assert.That(Rule("RevenantSpawn").EventCategory, Is.EqualTo(StationEventCategory.Antagonists));
            Assert.That(Rule("ClosetSkeleton").EventCategory, Is.EqualTo(StationEventCategory.Antagonists));
            Assert.That(Rule("AdminEventsTestCategoryInherited").EventCategory, Is.EqualTo(StationEventCategory.DerelictCyborgs));
            Assert.That(Rule("AdminEventsTestCategoryOverride").EventCategory, Is.EqualTo(StationEventCategory.Effects));
            Assert.That(Rule("DerelictEngineerCyborgSpawn").EventCategory, Is.EqualTo(StationEventCategory.DerelictCyborgs));
            Assert.That(Rule("KingRatMigration").EventCategory, Is.EqualTo(StationEventCategory.Creatures));
            Assert.That(Rule("GiftsMedical").EventCategory, Is.EqualTo(StationEventCategory.CargoGifts));
            Assert.That(Rule("ImmovableRodSpawn").EventCategory, Is.EqualTo(StationEventCategory.Meteors));
            Assert.That(Rule("UnknownShuttleInstigator").EventCategory, Is.EqualTo(StationEventCategory.Shuttles));
            string[] catalog = [.. availableRules.Select(rule => rule.Id)];
            Assert.That(catalog, Does.Contain("DragonSpawn"));
            Assert.That(catalog, Does.Contain("BasicStationEventScheduler"));
            Assert.That(catalog, Does.Not.Contain("BaseGameRule"));
            Assert.That(catalog, Does.Not.Contain("MobHuman"));
            Assert.That(catalog, Does.Contain("Sandbox"));
            Assert.That(catalog, Does.Contain("DynamicRule"));
            Assert.That(catalog, Does.Contain("InactivityTimeRestart"));
            Assert.That(catalog, Is.Unique);
            Assert.That(events.TryAddRule(ServerSession, "MissingAdminEventsRule"), Is.Null);
            Assert.That(events.TryAddRule(ServerSession, "BaseGameRule"), Is.Null);
            Assert.That(events.TryAddRule(ServerSession, "MobHuman"), Is.Null);
            var sandbox = events.TryAddRule(ServerSession, "Sandbox");
            Assert.That(sandbox, Is.Not.Null);
            Assert.That(events.GetSnapshot().Rules.Any(rule => rule.Entity == sandbox), Is.True);
            Assert.That(events.TryStopRule(ServerSession, sandbox!.Value), Is.True);
            Assert.That(events.GetSnapshot().History.Single(entry => entry.Entity == sandbox).EndedAt, Is.Not.Null);

            var data = admins.GetAdminData(ServerSession)!;
            var flags = data.Flags;
            data.Flags &= ~AdminFlags.Fun;
            Assert.That(events.TryAddRule(ServerSession, "AdminEventsTestScheduler"), Is.Null);
            data.Flags = flags;
            admins.DeAdmin(ServerSession);
            Assert.That(events.TryAddRule(ServerSession, "AdminEventsTestScheduler"), Is.Null);
            admins.ReAdmin(ServerSession);
            Assert.That(ticker.GetAddedGameRules(), Is.Empty);

            var entity = events.TryAddRule(ServerSession, "AdminEventsTestScheduler");
            Assert.That(entity, Is.Not.Null);
            var uid = SEntMan.GetEntity(entity.Value);
            Assert.That(ticker.GetAddedGameRules(), Is.EquivalentTo(new[] { uid }));
            Assert.That(ticker.IsGameRuleActive(uid), Is.EqualTo(ticker.RunLevel == GameRunLevel.InRound));
        });
    }

    [Test]
    public async Task StoppingRulesValidatesPermissionsAndEndsOnlyTheSelectedInstance()
    {
        await Server.WaitAssertion(() =>
        {
            var admins = Server.ResolveDependency<IAdminManager>();
            var ticker = SEntMan.System<ServerGameTicker>();
            var events = SEntMan.System<AdminEventsSystem>();
            ticker.ClearGameRules();
            admins.PromoteHost(ServerSession);

            var active = ticker.AddGameRule("AdminEventsTestScheduler")!.Value.Owner;
            ticker.StartGameRule(active);
            var pending = ticker.AddGameRule("AdminEventsTestScheduler")!.Value.Owner;
            var delayed = ticker.AddGameRule("AdminEventsTestScheduler")!.Value.Owner;
            SEntMan.AddComponent<DelayedStartRuleComponent>(delayed).RuleStartTime = TimeSpan.MaxValue;
            var entity = SEntMan.GetNetEntity(active);

            var data = admins.GetAdminData(ServerSession)!;
            var flags = data.Flags;
            data.Flags &= ~AdminFlags.Fun;
            Assert.That(events.TryStopRule(ServerSession, entity), Is.False);
            data.Flags = flags;
            data.Flags &= ~AdminFlags.Admin;
            Assert.That(events.TryStopRule(ServerSession, entity), Is.False);
            data.Flags = flags;
            admins.DeAdmin(ServerSession);
            Assert.That(events.TryStopRule(ServerSession, entity), Is.False);
            admins.ReAdmin(ServerSession);
            Assert.That(ticker.IsGameRuleActive(active), Is.True);

            var nonRule = SEntMan.SpawnEntity(null, Robust.Shared.Map.MapCoordinates.Nullspace);
            var nonRuleEntity = SEntMan.GetNetEntity(nonRule);
            Assert.That(events.TryStopRule(ServerSession, nonRuleEntity), Is.False);
            SEntMan.DeleteEntity(nonRule);
            Assert.That(events.TryStopRule(ServerSession, nonRuleEntity), Is.False);
            Assert.That(events.TryStopRule(ServerSession, default), Is.False);

            Assert.That(events.TryStopRule(ServerSession, entity), Is.True);
            Assert.That(SEntMan.HasComponent<ActiveGameRuleComponent>(active), Is.False);
            Assert.That(events.TryStopRule(ServerSession, entity), Is.False);
            Assert.That(ticker.GetAddedGameRules(), Is.EquivalentTo(new[] { pending, delayed }));
            Assert.That(events.GetSnapshot().History.Count(rule => rule.Prototype == "AdminEventsTestScheduler" && rule.StartedAt != null), Is.EqualTo(1));

            Assert.That(events.TryStopRule(ServerSession, SEntMan.GetNetEntity(pending)), Is.True);
            Assert.That(ticker.StartGameRule(pending), Is.False);
            Assert.That(events.TryStopRule(ServerSession, SEntMan.GetNetEntity(delayed)), Is.True);
            Assert.That(SEntMan.HasComponent<DelayedStartRuleComponent>(delayed), Is.False);
            Assert.That(ticker.StartGameRule(delayed), Is.False);
            Assert.That(events.GetSnapshot().Rules, Is.Empty);
            Assert.That(events.GetSnapshot().Tables, Is.Empty);
        });
    }

    [TestCase("AdminEventsTestScheduler")]
    [TestCase("RampingStationEventScheduler")]
    public async Task SchedulerTimersFollowCountdownAndPause(string prototype)
    {
        await Server.WaitAssertion(() =>
        {
            var config = Server.ResolveDependency<IConfigurationManager>();
            config.SetCVar(CCVars.EventsEnabled, true);
            var ticker = SEntMan.System<ServerGameTicker>();
            var events = SEntMan.System<AdminEventsSystem>();
            ticker.ClearGameRules();
            var uid = ticker.AddGameRule(prototype)!.Value.Owner;
            var entity = SEntMan.GetNetEntity(uid);
            Assert.That(events.GetSchedulerTimer(entity).Seconds, Is.Null);
            ticker.StartGameRule(uid);
            var before = events.GetSchedulerTimer(entity);
            Assert.That(before.Seconds, Is.GreaterThan(1));
            Assert.That(before.Paused, Is.False);
            void Advance()
            {
                if (prototype == "AdminEventsTestScheduler")
                    SEntMan.System<BasicStationEventSchedulerSystem>().Update(1);
                else
                    SEntMan.System<RampingStationEventSchedulerSystem>().Update(1);
            }
            Advance();
            var after = events.GetSchedulerTimer(entity);
            Assert.That(after.Seconds, Is.EqualTo(before.Seconds.Value - 1).Within(0.01));
            Assert.That(events.GetSnapshot().Timers.Single(), Is.EqualTo(after));
            config.SetCVar(CCVars.EventsEnabled, false);
            Advance();
            var paused = events.GetSchedulerTimer(entity);
            Assert.That(paused.Seconds, Is.EqualTo(after.Seconds));
            Assert.That(paused.Paused, Is.True);
            ticker.EndGameRule(uid);
            Assert.That(events.GetSchedulerTimer(entity).Seconds, Is.Null);
            SEntMan.DeleteEntity(uid);
            Assert.That(events.GetSchedulerTimer(entity).Seconds, Is.Null);
            Assert.That(events.GetSchedulerTimer(default).Seconds, Is.Null);
        });
    }

    [Test]
    public async Task SnapshotTracksCurrentEligibilityWithoutStartingEvents()
    {
        await Server.WaitAssertion(() =>
        {
            var config = Server.ResolveDependency<IConfigurationManager>();
            config.SetCVar(CCVars.EventsEnabled, true);
            var ticker = SEntMan.System<ServerGameTicker>();
            var events = SEntMan.System<AdminEventsSystem>();
            ticker.ClearGameRules();
            var scheduler = ticker.AddGameRule("AdminEventsTestScheduler")!.Value.Owner;

            AdminEventAvailability Status(string id) => events.GetSnapshot().Tables.Single().Entries
                .Single(entry => entry.Prototype == id).Availability;
            int Occurrences(string id) => events.GetSnapshot().Tables.Single().Entries
                .Single(entry => entry.Prototype == id).Occurrences;
            Assert.That(Occurrences("AdminEventsTestReady"), Is.Zero);
            Assert.That(Status("AdminEventsTestReady"), Is.EqualTo(AdminEventAvailability.SchedulerInactive));
            ticker.StartGameRule(scheduler);
            Assert.That(Status("AdminEventsTestReady"), Is.EqualTo(AdminEventAvailability.Available));
            Assert.That(Status("AdminEventsTestTooEarly"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));
            Assert.That(Status("AdminEventsTestTooFew"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));
            Assert.That(Status("AdminEventsTestZeroWeight"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));
            Assert.That(Status("AdminEventsTestTableBlocked"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));

            var random = Server.ResolveDependency<IRobustRandom>();
            random.SetSeed(42);
            var expected = random.Next();
            random.SetSeed(42);
            events.GetSnapshot();
            Assert.That(random.Next(), Is.EqualTo(expected));

            config.SetCVar(CCVars.GameTickerIgnoredPresets, "AdminEventsTestReady");
            Assert.That(Status("AdminEventsTestReady"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));
            config.SetCVar(CCVars.GameTickerIgnoredPresets, "");
            Assert.That(Status("AdminEventsTestReady"), Is.EqualTo(AdminEventAvailability.Available));
            config.SetCVar(CCVars.EventsEnabled, false);
            Assert.That(Status("AdminEventsTestReady"), Is.EqualTo(AdminEventAvailability.EventsDisabled));
            config.SetCVar(CCVars.EventsEnabled, true);

            var ready = ticker.AddGameRule("AdminEventsTestReady")!.Value.Owner;
            Assert.That(Occurrences("AdminEventsTestReady"), Is.Zero, "Adding a pending rule does not trigger it.");
            var pendingHistory = events.GetSnapshot().History.Single(entry => entry.Entity == SEntMan.GetNetEntity(ready));
            Assert.That(pendingHistory.Status, Is.EqualTo(GameRuleHistoryStatus.Pending));
            Assert.That(pendingHistory.StartedAt, Is.Null);
            ticker.StartGameRule(ready);
            Assert.That(Occurrences("AdminEventsTestReady"), Is.EqualTo(1));
            Assert.That(Status("AdminEventsTestReady"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));
            ticker.EndGameRule(ready);
            // The one-per-round limit continues to block the event after it ends.
            Assert.That(Status("AdminEventsTestReady"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));
            Assert.That(Occurrences("AdminEventsTestReady"), Is.EqualTo(1), "Finished events remain in the round history.");

            var repeatable = ticker.AddGameRule("AdminEventsTestRepeatable")!.Value.Owner;
            ticker.StartGameRule(repeatable);
            ticker.EndGameRule(repeatable);
            Assert.That(Occurrences("AdminEventsTestRepeatable"), Is.EqualTo(1));
            Assert.That(Status("AdminEventsTestRepeatable"), Is.EqualTo(AdminEventAvailability.Available));
            var repeated = ticker.AddGameRule("AdminEventsTestRepeatable")!.Value.Owner;
            ticker.StartGameRule(repeated);
            ticker.EndGameRule(repeated);
            Assert.That(Occurrences("AdminEventsTestRepeatable"), Is.EqualTo(2));
            SEntMan.DeleteEntity(ready);
            SEntMan.DeleteEntity(repeatable);
            SEntMan.DeleteEntity(repeated);
            List<AdminEventHistoryEntry> history = [.. events.GetSnapshot().History.Where(entry => entry.Prototype == "AdminEventsTestReady" || entry.Prototype == "AdminEventsTestRepeatable")];
            Assert.That(history.Select(entry => entry.Prototype), Is.EqualTo(new[]
            {
                "AdminEventsTestRepeatable", "AdminEventsTestRepeatable", "AdminEventsTestReady",
            }));
            Assert.That(history.Select(entry => entry.StartedAt), Is.All.Not.Null);
            Assert.That(history.Select(entry => entry.StartedAt), Is.Ordered.Descending);
            Assert.That(events.GetSnapshot().History.Where(entry => entry.Prototype == "AdminEventsTestReady" || entry.Prototype == "AdminEventsTestRepeatable"), Is.EqualTo(history));
        });
    }

    [Test]
    public async Task HistoryTracksInstancesSourcesCancellationAndCleanup()
    {
        await Server.WaitAssertion(() =>
        {
            var ticker = SEntMan.System<ServerGameTicker>();
            var events = SEntMan.System<AdminEventsSystem>();
            var history = SEntMan.System<GameRuleHistorySystem>();
            var admins = Server.ResolveDependency<IAdminManager>();
            admins.PromoteHost(ServerSession);
            ticker.ClearGameRules();

            GameRuleHistoryEntry Entry(EntityUid uid) => history.GetHistory().Single(entry => entry.Entity == SEntMan.GetNetEntity(uid));
            var source = new GameRuleSource(GameRuleSourceKind.Administrator, ServerSession.Name);
            var first = ticker.AddGameRule("AdminEventsTestRepeatable", source)!.Value.Owner;
            Assert.That(Entry(first).Status, Is.EqualTo(GameRuleHistoryStatus.Pending));
            Assert.That(Entry(first).Source, Is.EqualTo(source));
            ticker.StartGameRule(first);
            Assert.That(Entry(first).Status, Is.EqualTo(GameRuleHistoryStatus.Active));
            Assert.That(Entry(first).StartedAt, Is.Not.Null);
            Assert.That(Entry(first).StartedAt, Is.GreaterThanOrEqualTo(Entry(first).AddedAt));
            ticker.EndGameRule(first, reason: GameRuleEndReason.DurationElapsed);
            var finished = Entry(first);
            Assert.That(finished.EndReason, Is.EqualTo(GameRuleEndReason.DurationElapsed));
            Assert.That(finished.Status, Is.EqualTo(GameRuleHistoryStatus.Ended));
            Assert.That(finished.EndedAt, Is.GreaterThanOrEqualTo(finished.StartedAt));
            SEntMan.DeleteEntity(first);
            Assert.That(history.GetHistory().Single(entry => entry.Sequence == finished.Sequence), Is.EqualTo(finished));
            var displayed = events.GetSnapshot().History.Single(entry => entry.Sequence == finished.Sequence);
            Assert.That(displayed.Entity, Is.EqualTo(finished.Entity));
            Assert.That(displayed.Status, Is.EqualTo(GameRuleHistoryStatus.Ended));
            Assert.That(displayed.Source, Is.EqualTo(source));
            Assert.That(displayed.EndReason, Is.EqualTo(GameRuleEndReason.DurationElapsed));
            Assert.That(displayed.EndedAt, Is.EqualTo(finished.EndedAt));

            var delayed = ticker.AddGameRule("AdminEventsTestRepeatable", source)!.Value.Owner;
            SEntMan.GetComponent<GameRuleComponent>(delayed).Delay = new MinMax(30, 30);
            ticker.StartGameRule(delayed);
            Assert.That(Entry(delayed).Status, Is.EqualTo(GameRuleHistoryStatus.Delayed));
            Assert.That(Entry(delayed).StartedAt, Is.Null);
            Assert.That(events.TryStopRule(ServerSession, SEntMan.GetNetEntity(delayed)), Is.True);
            Assert.That(Entry(delayed).Status, Is.EqualTo(GameRuleHistoryStatus.Cancelled));
            Assert.That(Entry(delayed).StartedAt, Is.Null);
            Assert.That(Entry(delayed).EndedBy, Is.EqualTo(ServerSession.Name));
            Assert.That(Entry(delayed).EndReason, Is.EqualTo(GameRuleEndReason.Administrator));

            var manual = ticker.AddGameRule("AdminEventsTestRepeatable", source)!.Value.Owner;
            ticker.StartGameRule(manual);
            events.TryStopRule(ServerSession, SEntMan.GetNetEntity(manual));
            Assert.That(Entry(manual).Status, Is.EqualTo(GameRuleHistoryStatus.Stopped));

            var deleted = ticker.AddGameRule("AdminEventsTestRepeatable")!.Value.Owner;
            ticker.StartGameRule(deleted);
            var deletedId = Entry(deleted).Sequence;
            SEntMan.DeleteEntity(deleted);
            Assert.That(history.GetHistory().Single(entry => entry.Sequence == deletedId).EndReason, Is.EqualTo(GameRuleEndReason.EntityDeleted));

            var unknown = ticker.AddGameRule("AdminEventsTestRepeatable")!.Value.Owner;
            ticker.StartGameRule(unknown);
            ticker.EndGameRule(unknown);
            Assert.That(Entry(unknown).Source.Kind, Is.EqualTo(GameRuleSourceKind.Unknown));
            Assert.That(Entry(unknown).EndReason, Is.EqualTo(GameRuleEndReason.Unknown));

            var scheduler = ticker.AddGameRule("AdminEventsTestScheduler")!.Value.Owner;
            SEntMan.System<EventManagerSystem>().RunRandomEvent(new NestedSelector { TableId = "AdminEventsTestTable" }, scheduler);
            var scheduled = history.GetHistory().First();
            Assert.That(scheduled.Source.Kind, Is.EqualTo(GameRuleSourceKind.Scheduler));
            Assert.That(scheduled.Source.Scheduler, Is.EqualTo(SEntMan.GetNetEntity(scheduler)));
            Assert.That(scheduled.Source.Name, Is.EqualTo("AdminEventsTestScheduler"));
            Assert.That(scheduled.Source.Table, Is.EqualTo("AdminEventsTestTable"));

            var console = Server.ResolveDependency<IConsoleHost>();
            console.ExecuteCommand("addgamerule AdminEventsTestRepeatable");
            var consoleEntry = history.GetHistory().First();
            Assert.That(consoleEntry.Source.Kind, Is.EqualTo(GameRuleSourceKind.ServerConsole));
            console.ExecuteCommand($"endgamerule {consoleEntry.Entity}");
            Assert.That(history.GetHistory().First().EndReason, Is.EqualTo(GameRuleEndReason.ServerConsole));
            Assert.That(history.GetHistory().Select(entry => entry.Entity), Is.Unique);

            ticker.ClearGameRules(ServerSession.Name);
            Assert.That(history.GetHistory().Single(entry => entry.Sequence == scheduled.Sequence).EndReason, Is.EqualTo(GameRuleEndReason.RulesCleared));
            SEntMan.EventBus.RaiseEvent(EventSource.Local, new RoundRestartCleanupEvent());
            SEntMan.DeleteEntity(manual);
            Assert.That(history.GetHistory(), Is.Empty);
        });
    }

    [TestCase("BasicStationEventScheduler")]
    [TestCase("RampingStationEventScheduler")]
    public async Task SnapshotIncludesUnfinishedRulesAndCurrentTables(string prototype)
    {
        await Server.WaitAssertion(() =>
        {
            var ticker = SEntMan.System<ServerGameTicker>();
            var events = SEntMan.System<AdminEventsSystem>();
            ticker.ClearGameRules();

            var pending = ticker.AddGameRule(prototype)!.Value.Owner;
            var active = ticker.AddGameRule(prototype)!.Value.Owner;
            ticker.StartGameRule(active);
            var delayed = ticker.AddGameRule(prototype)!.Value.Owner;
            SEntMan.AddComponent<DelayedStartRuleComponent>(delayed);
            var ended = ticker.AddGameRule(prototype)!.Value.Owner;
            ticker.EndGameRule(ended);

            var state = events.GetSnapshot();
            Assert.That(state.Rules, Has.Count.EqualTo(3));
            Assert.That(state.Rules.Single(rule => rule.Entity == SEntMan.GetNetEntity(pending)).Status,
                Is.EqualTo(AdminEventRuleStatus.Pending));
            Assert.That(state.Rules.Single(rule => rule.Entity == SEntMan.GetNetEntity(active)).Status,
                Is.EqualTo(AdminEventRuleStatus.Active));
            Assert.That(state.Rules.Single(rule => rule.Entity == SEntMan.GetNetEntity(delayed)).Status,
                Is.EqualTo(AdminEventRuleStatus.Delayed));
            Assert.That(state.Rules.Any(rule => rule.Entity == SEntMan.GetNetEntity(ended)), Is.False);
            Assert.That(state.Tables, Has.Count.EqualTo(3));
            foreach (var table in state.Tables)
            {
                Assert.That(table.Table, Is.EqualTo("BasicGameRulesTable"));
                Assert.That(table.Entries.Select(entry => entry.Prototype), Does.Contain("PowerGridCheck"));
                Assert.That(table.Entries.Select(entry => entry.Prototype), Is.Unique);

                var ratKing = table.Entries.Single(entry => entry.Prototype == "KingRatMigration");
                Assert.That(ratKing.MinimumPlayers, Is.EqualTo(30));
                Assert.That(ratKing.EarliestStartMinutes, Is.EqualTo(15));
                Assert.That(ratKing.ReoccurrenceDelayMinutes, Is.EqualTo(30));
                Assert.That(ratKing.MaxOccurrences, Is.Null);
                Assert.That(ratKing.OccursDuringRoundEnd, Is.False);

                var sentience = table.Entries.Single(entry => entry.Prototype == "RandomSentience");
                Assert.That(sentience.MaxOccurrences, Is.EqualTo(1));
                Assert.That(sentience.OccursDuringRoundEnd, Is.True);
            }

            // Viewing tables must neither start pending rules nor add random events.
            var refreshed = events.GetSnapshot();
            Assert.That(refreshed.Rules, Is.EqualTo(state.Rules));
            Assert.That(refreshed.Tables[0].Entries, Is.EqualTo(state.Tables[0].Entries));

            ticker.ClearGameRules();
            var empty = events.GetSnapshot();
            Assert.That(empty.Rules, Is.Empty);
            Assert.That(empty.Tables, Is.Empty);
        });
    }
}
