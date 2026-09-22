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
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Content.IntegrationTests.Tests.Administration;

public sealed class AdminGameRulesControlTest : GameTest
{
    // DummyTicker skips round cleanup, so pooled pairs can retain another test's rule history.
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true, Fresh = true };

    [TestPrototypes]
    internal const string Prototypes = """
        - type: gameRuleCategory
          id: AdminGameRulesControlTestCategory
          name: admin-gamerules-title
          priority: 100
        - type: entity
          id: AdminGameRulesControlTestReady
          parent: BaseGameRule
          description: An event description for the information window.
          components:
          - type: GameRule
            category: Incidents
          - type: StationEvent
            earliestStart: 0
            reoccurrenceDelay: 0
            maxOccurrences: 1
        - type: entity
          id: AdminGameRulesControlTestRepeatable
          parent: BaseGameRule
          components:
          - type: GameRule
            category: Incidents
          - type: StationEvent
            earliestStart: 0
            reoccurrenceDelay: 0
        - type: entity
          id: AdminGameRulesControlTestCategoryParent
          parent: BaseGameRule
          abstract: true
          components:
          - type: GameRule
            category: DerelictCyborgs
          - type: StationEvent
          - type: AntagSelection
            antags: []
        - type: entity
          id: AdminGameRulesControlTestCategoryInherited
          parent: AdminGameRulesControlTestCategoryParent
          components:
          - type: StationEvent
            minimumPlayers: 1
        - type: entity
          id: AdminGameRulesControlTestCategoryOverride
          parent: AdminGameRulesControlTestCategoryParent
          components:
          - type: GameRule
            category: Incidents
        - type: entity
          id: AdminGameRulesControlTestTooEarly
          parent: AdminGameRulesControlTestReady
          components:
          - type: StationEvent
            earliestStart: 100000
        - type: entity
          id: AdminGameRulesControlTestTooFew
          parent: AdminGameRulesControlTestReady
          components:
          - type: StationEvent
            minimumPlayers: 100000
        - type: entity
          id: AdminGameRulesControlTestZeroWeight
          parent: AdminGameRulesControlTestReady
          components:
          - type: StationEvent
            weight: 0
        - type: entity
          id: AdminGameRulesControlTestTableBlocked
          parent: AdminGameRulesControlTestReady
        - type: entity
          id: AdminGameRulesControlTestScheduler
          parent: BaseGameRule
          components:
          - type: GameRule
            category: AdminGameRulesControlTestCategory
          - type: BasicStationEventScheduler
            scheduledGameRules: !type:NestedSelector
              tableId: AdminGameRulesControlTestTable
        - type: entity
          id: AdminGameRulesControlTestCustomCategory
          parent: BaseGameRule
          components:
          - type: GameRule
            category: AdminGameRulesControlTestCategory
        - type: entityTable
          id: AdminGameRulesControlTestTable
          table: !type:AllSelector
            children:
            - id: AdminGameRulesControlTestReady
            - id: AdminGameRulesControlTestRepeatable
            - id: AdminGameRulesControlTestTooEarly
            - id: AdminGameRulesControlTestTooFew
            - id: AdminGameRulesControlTestZeroWeight
            - id: AdminGameRulesControlTestTableBlocked
              conditions:
              - !type:HasBudgetCondition
                costOverride: 1
            - id: KingRatMigration
            - id: PowerGridCheck
            - id: RandomSentience
        """;

    [Test]
    public async Task RuleCategoryIsRequired()
    {
        await Server.WaitAssertion(() =>
        {
            var serialization = Server.ResolveDependency<ISerializationManager>();
            Assert.Throws<RequiredFieldNotMappedException>(() =>
                serialization.Read<GameRuleComponent>(new MappingDataNode(), notNullableOverride: true));
        });
    }

    [Test]
    public async Task CategoriesComeFromRuleData()
    {
        await Server.WaitAssertion(() =>
        {
            const string prototype = "AdminGameRulesControlTestCustomCategory";
            var ticker = SEntMan.System<ServerGameTicker>();
            var events = SEntMan.System<AdminGameRulesControlSystem>();
            ticker.ClearGameRules();
            var uid = ticker.AddGameRule(prototype)!.Value.Owner;
            var state = events.GetSnapshot();
            Assert.That(state.AvailableRules.Single(rule => rule.Id == prototype).Category.Id, Is.EqualTo("AdminGameRulesControlTestCategory"));
            Assert.That(state.Rules.Single().Category.Id, Is.EqualTo("AdminGameRulesControlTestCategory"));

            // The live instance can override its prototype without changing its behavior components.
            SEntMan.GetComponent<GameRuleComponent>(uid).Category = "SpecialModes";
            state = events.GetSnapshot();
            Assert.That(state.Rules.Single().Category.Id, Is.EqualTo("SpecialModes"));
            Assert.That(state.AvailableRules.Single(rule => rule.Id == prototype).Category.Id, Is.EqualTo("AdminGameRulesControlTestCategory"));
        });
    }

    [Test]
    public async Task AddingRulesValidatesPrototypesAndPermissions()
    {
        await Server.WaitAssertion(() =>
        {
            var admins = Server.ResolveDependency<IAdminManager>();
            var ticker = SEntMan.System<ServerGameTicker>();
            var events = SEntMan.System<AdminGameRulesControlSystem>();
            ticker.ClearGameRules();
            admins.PromoteHost(ServerSession);

            var availableRules = events.GetSnapshot().AvailableRules;
            AdminGameRulePrototypeInfo Rule(string id) => availableRules.Single(rule => rule.Id == id);
            Assert.That(Rule("AdminGameRulesControlTestReady").MinimumStartDelaySeconds, Is.Null);
            Assert.That(Rule("AdminGameRulesControlTestReady").MaximumStartDelaySeconds, Is.Null);
            Assert.That(Rule("AnomalySpawn").MinimumStartDelaySeconds, Is.EqualTo(10));
            Assert.That(Rule("AnomalySpawn").MaximumStartDelaySeconds, Is.EqualTo(20));
            Assert.That(Rule("BluespaceArtifact").MinimumStartDelaySeconds, Is.EqualTo(30));
            Assert.That(Rule("BluespaceArtifact").MaximumStartDelaySeconds, Is.EqualTo(30));
            Assert.That(Rule("DragonSpawn").Category.Id, Is.EqualTo("MidRoundAntagonists"));
            Assert.That(Rule("Traitor").Category.Id, Is.EqualTo("RoundStartAntagonists"));
            Assert.That(Rule("TraitorReinforcement").Category.Id, Is.EqualTo("MidRoundAntagonists"));
            Assert.That(Rule("Survivor").Category.Id, Is.EqualTo("Roles"));
            Assert.That(Rule("AdminGameRulesControlTestScheduler").IsScheduler, Is.True);
            Assert.That(Rule("AdminGameRulesControlTestCustomCategory").IsScheduler, Is.False);
            Assert.That(Rule("BasicStationEventScheduler").Category.Id, Is.EqualTo("Schedulers"));
            Assert.That(Rule("DynamicStationEventScheduler").Category.Id, Is.EqualTo("Schedulers"));
            Assert.That(Rule("ClericalError").Category.Id, Is.EqualTo("Incidents"));
            Assert.That(Rule("LoneOpsSpawn").Category.Id, Is.EqualTo("MidRoundAntagonists"));
            Assert.That(Rule("RevenantSpawn").Category.Id, Is.EqualTo("MidRoundAntagonists"));
            Assert.That(Rule("ClosetSkeleton").Category.Id, Is.EqualTo("MidRoundAntagonists"));
            Assert.That(Rule("AdminGameRulesControlTestCategoryInherited").Category.Id, Is.EqualTo("DerelictCyborgs"));
            Assert.That(Rule("AdminGameRulesControlTestCategoryOverride").Category.Id, Is.EqualTo("Incidents"));
            Assert.That(Rule("DerelictEngineerCyborgSpawn").Category.Id, Is.EqualTo("DerelictCyborgs"));
            Assert.That(Rule("KingRatMigration").Category.Id, Is.EqualTo("Creatures"));
            Assert.That(Rule("GiftsMedical").Category.Id, Is.EqualTo("CargoGifts"));
            Assert.That(Rule("ImmovableRodSpawn").Category.Id, Is.EqualTo("Meteors"));
            Assert.That(Rule("UnknownShuttleInstigator").Category.Id, Is.EqualTo("Shuttles"));
            string[] catalog = [.. availableRules.Select(rule => rule.Id)];
            Assert.That(catalog, Does.Contain("DragonSpawn"));
            Assert.That(catalog, Does.Contain("BasicStationEventScheduler"));
            Assert.That(catalog, Does.Not.Contain("BaseGameRule"));
            Assert.That(catalog, Does.Not.Contain("MobHuman"));
            Assert.That(catalog, Does.Contain("Sandbox"));
            Assert.That(catalog, Does.Contain("DynamicRule"));
            Assert.That(catalog, Does.Contain("InactivityTimeRestart"));
            Assert.That(catalog, Is.Unique);
            Assert.That(events.TryAddRule(ServerSession, "MissingAdminGameRulesControlRule"), Is.Null);
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
            Assert.That(events.TryAddRule(ServerSession, "AdminGameRulesControlTestScheduler"), Is.Null);
            data.Flags = flags;
            admins.DeAdmin(ServerSession);
            Assert.That(events.TryAddRule(ServerSession, "AdminGameRulesControlTestScheduler"), Is.Null);
            admins.ReAdmin(ServerSession);
            Assert.That(ticker.GetAddedGameRules(), Is.Empty);

            var entity = events.TryAddRule(ServerSession, "AdminGameRulesControlTestScheduler");
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
            var events = SEntMan.System<AdminGameRulesControlSystem>();
            ticker.ClearGameRules();
            admins.PromoteHost(ServerSession);

            var active = ticker.AddGameRule("AdminGameRulesControlTestScheduler")!.Value.Owner;
            ticker.StartGameRule(active);
            var pending = ticker.AddGameRule("AdminGameRulesControlTestScheduler")!.Value.Owner;
            var delayed = ticker.AddGameRule("AdminGameRulesControlTestScheduler")!.Value.Owner;
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
            Assert.That(events.GetSnapshot().History.Count(rule => rule.Prototype == "AdminGameRulesControlTestScheduler" && rule.StartedAt != null), Is.EqualTo(1));

            Assert.That(events.TryStopRule(ServerSession, SEntMan.GetNetEntity(pending)), Is.True);
            Assert.That(ticker.StartGameRule(pending), Is.False);
            Assert.That(events.TryStopRule(ServerSession, SEntMan.GetNetEntity(delayed)), Is.True);
            Assert.That(SEntMan.HasComponent<DelayedStartRuleComponent>(delayed), Is.False);
            Assert.That(ticker.StartGameRule(delayed), Is.False);
            Assert.That(events.GetSnapshot().Rules, Is.Empty);
            Assert.That(events.GetSnapshot().Tables, Is.Empty);
        });
    }

    [TestCase("AdminGameRulesControlTestScheduler")]
    [TestCase("RampingStationEventScheduler")]
    public async Task SchedulerTimersFollowCountdownAndPause(string prototype)
    {
        await Server.WaitAssertion(() =>
        {
            var config = Server.ResolveDependency<IConfigurationManager>();
            config.SetCVar(CCVars.EventsEnabled, true);
            var ticker = SEntMan.System<ServerGameTicker>();
            var events = SEntMan.System<AdminGameRulesControlSystem>();
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
                if (prototype == "AdminGameRulesControlTestScheduler")
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
            var events = SEntMan.System<AdminGameRulesControlSystem>();
            ticker.ClearGameRules();
            var scheduler = ticker.AddGameRule("AdminGameRulesControlTestScheduler")!.Value.Owner;

            AdminEventAvailability Status(string id) => events.GetSnapshot().Tables.Single().Entries
                .Single(entry => entry.Prototype == id).Availability;
            int Occurrences(string id) => events.GetSnapshot().Tables.Single().Entries
                .Single(entry => entry.Prototype == id).Occurrences;
            Assert.That(Occurrences("AdminGameRulesControlTestReady"), Is.Zero);
            Assert.That(Status("AdminGameRulesControlTestReady"), Is.EqualTo(AdminEventAvailability.SchedulerInactive));
            ticker.StartGameRule(scheduler);
            Assert.That(Status("AdminGameRulesControlTestReady"), Is.EqualTo(AdminEventAvailability.Available));
            Assert.That(Status("AdminGameRulesControlTestTooEarly"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));
            Assert.That(Status("AdminGameRulesControlTestTooFew"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));
            Assert.That(Status("AdminGameRulesControlTestZeroWeight"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));
            Assert.That(Status("AdminGameRulesControlTestTableBlocked"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));

            var random = Server.ResolveDependency<IRobustRandom>();
            random.SetSeed(42);
            var expected = random.Next();
            random.SetSeed(42);
            events.GetSnapshot();
            Assert.That(random.Next(), Is.EqualTo(expected));

            config.SetCVar(CCVars.GameTickerIgnoredPresets, "AdminGameRulesControlTestReady");
            Assert.That(Status("AdminGameRulesControlTestReady"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));
            config.SetCVar(CCVars.GameTickerIgnoredPresets, "");
            Assert.That(Status("AdminGameRulesControlTestReady"), Is.EqualTo(AdminEventAvailability.Available));
            config.SetCVar(CCVars.EventsEnabled, false);
            Assert.That(Status("AdminGameRulesControlTestReady"), Is.EqualTo(AdminEventAvailability.EventsDisabled));
            config.SetCVar(CCVars.EventsEnabled, true);

            var ready = ticker.AddGameRule("AdminGameRulesControlTestReady")!.Value.Owner;
            Assert.That(Occurrences("AdminGameRulesControlTestReady"), Is.Zero, "Adding a pending rule does not trigger it.");
            var pendingHistory = events.GetSnapshot().History.Single(entry => entry.Entity == SEntMan.GetNetEntity(ready));
            Assert.That(pendingHistory.Status, Is.EqualTo(GameRuleHistoryStatus.Pending));
            Assert.That(pendingHistory.StartedAt, Is.Null);
            ticker.StartGameRule(ready);
            Assert.That(Occurrences("AdminGameRulesControlTestReady"), Is.EqualTo(1));
            Assert.That(Status("AdminGameRulesControlTestReady"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));
            ticker.EndGameRule(ready);
            // The one-per-round limit continues to block the event after it ends.
            Assert.That(Status("AdminGameRulesControlTestReady"), Is.EqualTo(AdminEventAvailability.ConditionsNotMet));
            Assert.That(Occurrences("AdminGameRulesControlTestReady"), Is.EqualTo(1), "Finished events remain in the round history.");

            var repeatable = ticker.AddGameRule("AdminGameRulesControlTestRepeatable")!.Value.Owner;
            ticker.StartGameRule(repeatable);
            ticker.EndGameRule(repeatable);
            Assert.That(Occurrences("AdminGameRulesControlTestRepeatable"), Is.EqualTo(1));
            Assert.That(Status("AdminGameRulesControlTestRepeatable"), Is.EqualTo(AdminEventAvailability.Available));
            var repeated = ticker.AddGameRule("AdminGameRulesControlTestRepeatable")!.Value.Owner;
            ticker.StartGameRule(repeated);
            ticker.EndGameRule(repeated);
            Assert.That(Occurrences("AdminGameRulesControlTestRepeatable"), Is.EqualTo(2));
            SEntMan.DeleteEntity(ready);
            SEntMan.DeleteEntity(repeatable);
            SEntMan.DeleteEntity(repeated);
            List<AdminGameRuleHistoryEntry> history = [.. events.GetSnapshot().History.Where(entry => entry.Prototype == "AdminGameRulesControlTestReady" || entry.Prototype == "AdminGameRulesControlTestRepeatable")];
            Assert.That(history.Select(entry => entry.Prototype), Is.EqualTo(new[]
            {
                "AdminGameRulesControlTestRepeatable", "AdminGameRulesControlTestRepeatable", "AdminGameRulesControlTestReady",
            }));
            Assert.That(history.Select(entry => entry.StartedAt), Is.All.Not.Null);
            Assert.That(history.Select(entry => entry.StartedAt), Is.Ordered.Descending);
            Assert.That(events.GetSnapshot().History.Where(entry => entry.Prototype == "AdminGameRulesControlTestReady" || entry.Prototype == "AdminGameRulesControlTestRepeatable"), Is.EqualTo(history));
        });
    }

    [Test]
    public async Task HistoryTracksInstancesSourcesCancellationAndCleanup()
    {
        await Server.WaitAssertion(() =>
        {
            var ticker = SEntMan.System<ServerGameTicker>();
            var events = SEntMan.System<AdminGameRulesControlSystem>();
            var history = SEntMan.System<GameRuleHistorySystem>();
            var admins = Server.ResolveDependency<IAdminManager>();
            admins.PromoteHost(ServerSession);
            ticker.ClearGameRules();

            GameRuleHistoryEntry Entry(EntityUid uid) => history.GetHistory().Single(entry => entry.Entity == SEntMan.GetNetEntity(uid));
            var source = new GameRuleSource(GameRuleSourceKind.Administrator, ServerSession.Name);
            var first = ticker.AddGameRule("AdminGameRulesControlTestRepeatable", source)!.Value.Owner;
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

            var delayed = ticker.AddGameRule("AdminGameRulesControlTestRepeatable", source)!.Value.Owner;
            SEntMan.GetComponent<GameRuleComponent>(delayed).Delay = new MinMax(30, 30);
            ticker.StartGameRule(delayed);
            Assert.That(Entry(delayed).Status, Is.EqualTo(GameRuleHistoryStatus.Delayed));
            Assert.That(Entry(delayed).StartedAt, Is.Null);
            Assert.That(events.TryStopRule(ServerSession, SEntMan.GetNetEntity(delayed)), Is.True);
            Assert.That(Entry(delayed).Status, Is.EqualTo(GameRuleHistoryStatus.Cancelled));
            Assert.That(Entry(delayed).StartedAt, Is.Null);
            Assert.That(Entry(delayed).EndedBy, Is.EqualTo(ServerSession.Name));
            Assert.That(Entry(delayed).EndReason, Is.EqualTo(GameRuleEndReason.Administrator));

            var manual = ticker.AddGameRule("AdminGameRulesControlTestRepeatable", source)!.Value.Owner;
            ticker.StartGameRule(manual);
            events.TryStopRule(ServerSession, SEntMan.GetNetEntity(manual));
            Assert.That(Entry(manual).Status, Is.EqualTo(GameRuleHistoryStatus.Stopped));

            var deleted = ticker.AddGameRule("AdminGameRulesControlTestRepeatable")!.Value.Owner;
            ticker.StartGameRule(deleted);
            var deletedId = Entry(deleted).Sequence;
            SEntMan.DeleteEntity(deleted);
            Assert.That(history.GetHistory().Single(entry => entry.Sequence == deletedId).EndReason, Is.EqualTo(GameRuleEndReason.EntityDeleted));

            var unknown = ticker.AddGameRule("AdminGameRulesControlTestRepeatable")!.Value.Owner;
            ticker.StartGameRule(unknown);
            ticker.EndGameRule(unknown);
            Assert.That(Entry(unknown).Source.Kind, Is.EqualTo(GameRuleSourceKind.Unknown));
            Assert.That(Entry(unknown).EndReason, Is.EqualTo(GameRuleEndReason.Unknown));

            var scheduler = ticker.AddGameRule("AdminGameRulesControlTestScheduler")!.Value.Owner;
            SEntMan.System<EventManagerSystem>().RunRandomEvent(new NestedSelector { TableId = "AdminGameRulesControlTestTable" }, scheduler);
            var scheduled = history.GetHistory().First();
            Assert.That(scheduled.Source.Kind, Is.EqualTo(GameRuleSourceKind.Scheduler));
            Assert.That(scheduled.Source.Scheduler, Is.EqualTo(SEntMan.GetNetEntity(scheduler)));
            Assert.That(scheduled.Source.Name, Is.EqualTo("AdminGameRulesControlTestScheduler"));
            Assert.That(scheduled.Source.Table, Is.EqualTo("AdminGameRulesControlTestTable"));

            var console = Server.ResolveDependency<IConsoleHost>();
            console.ExecuteCommand("addgamerule AdminGameRulesControlTestRepeatable");
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
            var events = SEntMan.System<AdminGameRulesControlSystem>();
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
                Is.EqualTo(AdminGameRuleStatus.Pending));
            Assert.That(state.Rules.Single(rule => rule.Entity == SEntMan.GetNetEntity(active)).Status,
                Is.EqualTo(AdminGameRuleStatus.Active));
            Assert.That(state.Rules.Single(rule => rule.Entity == SEntMan.GetNetEntity(delayed)).Status,
                Is.EqualTo(AdminGameRuleStatus.Delayed));
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
