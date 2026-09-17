using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Content.Client.Administration.UI.Events;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Moq;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Server.Console;
using Robust.Shared.Console;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Administration;

public sealed class AdminEventsWindowTest : InteractionTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [TestCase(1f)]
    [TestCase(1.25f)]
    public async Task SelectsRuleTablesRefreshesAndClosesWhenDeadminned(float uiScale)
    {
        await Client.WaitPost(() =>
        {
            var config = Client.ResolveDependency<IConfigurationManager>();
            config.SetCVar(Robust.Shared.CVars.ResAutoScaleEnabled, false);
            config.SetCVar(Robust.Shared.CVars.DisplayUIScale, uiScale);
        });
        var admins = Server.ResolveDependency<IAdminManager>();
        EntityUid dynamicRule = default;
        await Server.WaitPost(() =>
        {
            admins.PromoteHost(ServerSession);
            SEntMan.System<GameTicker>().ClearGameRules();
            var basic = SEntMan.System<GameTicker>().AddGameRule("AdminEventsTestScheduler");
            SEntMan.System<GameTicker>().StartGameRule(basic);
            Server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars.EventsEnabled, true);
            dynamicRule = SEntMan.System<GameTicker>().AddGameRule("DynamicStationEventScheduler");
            SEntMan.System<GameTicker>().AddGameRule("InactivityTimeRestart");
            Server.ResolveDependency<IConsoleHost>().ExecuteCommand(ServerSession, "eventsui");
        });
        await RunUntilSynced();

        var window = GetWindow<AdminEventsWindow>();
        await Client.WaitPost(() => window.SetSize = new Vector2(900, 550));
        await RunUntilSynced();
        var rules = GetControlFromField<BoxContainer>("RulesList", window);
        var entries = GetControlFromField<BoxContainer>("EntriesList", window);
        var entriesScroll = GetControlFromField<ScrollContainer>("EntriesScroll", window);
        var add = GetControlFromField<Button>("AddRuleButton", window);
        var stop = GetControlFromField<Button>("StopRuleButton", window);
        var tabs = GetControlFromField<TabContainer>("Tabs", window);
        var eventRules = GetControlFromField<BoxContainer>("EventRulesList", window);
        var history = GetControlFromField<BoxContainer>("HistoryList", window);
        BoxContainer LatestHistory() => history.Children.OfType<PanelContainer>().First().Children.OfType<BoxContainer>().Single();
        string HistoryValue(string key) => LatestHistory().Children.OfType<TableContainer>().Single()
            .Children.OfType<RichTextLabel>().Single(label => label.Name == key).GetMessage();
        var addEvent = GetControlFromField<Button>("AddEventButton", window);
        var stopEvent = GetControlFromField<Button>("StopEventButton", window);
        var nextAttempt = GetControlFromField<Label>("NextAttemptLabel", window);
        IEnumerable<BoxContainer> LikelihoodRows() => entries.Children.OfType<PanelContainer>()
            .Where(panel => panel.Name == "LikelihoodPanel").SelectMany(panel => panel.Children.OfType<BoxContainer>())
            .SelectMany(box => box.Children.OfType<BoxContainer>());
        IEnumerable<BoxContainer> SectionRows(Collapsible section) => section.Body!.Children.OfType<BoxContainer>().Single()
            .Children.OfType<PanelContainer>().SelectMany(panel => panel.Children.OfType<BoxContainer>());
        IEnumerable<BoxContainer> EntryRows() => entries.Children.OfType<Collapsible>().SelectMany(SectionRows);
        Collapsible Section(string name) => entries.Children.OfType<Collapsible>().Single(section => section.Name == name);
        Button RuleButton(string prototype) => rules.Children.OfType<Button>()
            .Single(button => button.Text.StartsWith(prototype + " ("));
        bool HasEntry(string text) => entries.Children.OfType<RichTextLabel>()
            .Any(label => label.GetMessage().Contains(text)) || EntryRows().Any(row => row.Name == text)
            || entries.Children.OfType<Collapsible>().Any(section => ((CollapsibleHeading) section.Heading!).Title!.Contains(text));
        BoxContainer EntryRow(string prototype) => EntryRows().Single(row => row.Name == prototype);
        Button InfoButton(string prototype) => EntryRow(prototype).Children.OfType<Button>().Single();
        RichTextLabel EntryLabel(string prototype) => EntryRow(prototype).Children.OfType<RichTextLabel>().Single();
        string EntryCategory(string prototype) => entries.Children.OfType<Collapsible>()
            .Single(section => SectionRows(section).Any(row => row.Name == prototype)).Name;
        void AssertPaneLayout()
        {
            var left = GetControlFromField<PanelContainer>("RulesPane", window);
            var right = GetControlFromField<PanelContainer>("TablePane", window);
            Assert.That(left.Size.X, Is.GreaterThanOrEqualTo(250));
            Assert.That(right.Size.X, Is.GreaterThan(left.Size.X));
            Assert.That(right.Position.X, Is.GreaterThanOrEqualTo(left.Position.X + left.Size.X));
            Assert.That(add.Parent, Is.SameAs(rules.Parent.Parent));
            Assert.That(add.Parent.Parent, Is.SameAs(left));
            Assert.That(add.Position.Y, Is.GreaterThanOrEqualTo(rules.Parent.Position.Y + rules.Parent.Size.Y));
            Assert.That(stop.Parent, Is.SameAs(add.Parent));
            Assert.That(stop.Position.Y, Is.GreaterThanOrEqualTo(add.Position.Y + add.Size.Y));
            Assert.That(stop.Disabled, Is.False);
            Assert.That(window.UIScale, Is.EqualTo(uiScale));
            AssertDraws(window);
        }

        await Client.WaitAssertion(() =>
        {
            Assert.That(RuleButton("AdminEventsTestScheduler").Pressed, Is.True);
            Assert.That(rules.Children.OfType<Button>().Any(button => button.Text.Contains("InactivityTimeRestart")), Is.False);
            Assert.That(eventRules.Children.OfType<Button>(), Is.Empty);
            Assert.That(history.Children.OfType<PanelContainer>(), Is.Empty);
            Assert.That(entries.VisibleInTree, Is.True);
            Assert.That(HasEntry("AdminEventsTestTable"), Is.True);
            Assert.That(HasEntry("PowerGridCheck"), Is.True);
            Assert.That(EntryLabel("AdminEventsTestReady").Modulate, Is.EqualTo(Color.White));
            Assert.That(EntryLabel("AdminEventsTestReady").ToolTip, Does.Contain(Loc.GetString("admin-events-delay-none")));
            Assert.That(EntryLabel("KingRatMigration").Modulate, Is.EqualTo(Color.LightCoral));
            Assert.That(EntryLabel("KingRatMigration").GetMessage(), Is.EqualTo("KingRatMigration"));
            Assert.That(EntryCategory("AdminEventsTestReady"), Is.EqualTo("admin-events-category-waiting"));
            Assert.That(EntryCategory("KingRatMigration"), Is.EqualTo("admin-events-category-unavailable"));
            Assert.That(nextAttempt.VisibleInTree, Is.True);
            Assert.That(nextAttempt.Text, Does.Match(@"\d+:\d{2}"));
            Assert.That(LikelihoodRows(), Is.Not.Empty);
            Assert.That(LikelihoodRows().Select(row => row.Name), Does.Not.Contain("KingRatMigration"));
            Assert.That(LikelihoodRows().Select(row => row.Name), Does.Not.Contain("AdminEventsTestZeroWeight"));
            Assert.That(LikelihoodRows().All(row => EntryLabel(row.Name).Modulate == Color.White), Is.True);
            Assert.That(HasEntry(Loc.GetString("admin-events-category-triggered", ("count", 0))), Is.True);
            AssertPaneLayout();
        });

        await ClickControl(Section("admin-events-category-waiting").Heading!);
        await Client.WaitAssertion(() => Assert.That(EntryRow("AdminEventsTestReady").VisibleInTree, Is.False));
        await ClickControl(window.RefreshButton);
        await RunUntilSynced();
        await Client.WaitAssertion(() => Assert.That(Section("admin-events-category-waiting").BodyVisible, Is.False));
        await ClickControl(RuleButton("DynamicStationEventScheduler"));
        await ClickControl(RuleButton("AdminEventsTestScheduler"));
        await Client.WaitAssertion(() => Assert.That(Section("admin-events-category-waiting").BodyVisible, Is.False));
        await ClickControl(Section("admin-events-category-waiting").Heading!);
        await Client.WaitAssertion(() => Assert.That(EntryRow("AdminEventsTestReady").VisibleInTree, Is.True));

        async Task OpenInfo(string prototype)
        {
            await Client.WaitPost(() => entriesScroll.VScroll = EntryRow(prototype).Parent.GlobalPosition.Y - entries.GlobalPosition.Y);
            await RunUntilSynced();
            await ClickControl(InfoButton(prototype));
        }

        await OpenInfo("KingRatMigration");
        var details = GetWindow<AdminEventDetailsWindow>();
        await Client.WaitAssertion(() =>
        {
            var description = GetControlFromField<RichTextLabel>("Description", details).GetMessage();
            Assert.That(description, Is.EqualTo(Loc.GetString("ent-KingRatMigration.desc")));
            Assert.That(description, Is.Not.Empty);
            Assert.That(GetControlFromField<RichTextLabel>("PlayersValue", details).GetMessage(), Is.EqualTo("30"));
            Assert.That(GetControlFromField<RichTextLabel>("EarliestValue", details).GetMessage(),
                Is.EqualTo(Loc.GetString("admin-events-info-minutes", ("minutes", 15))));
            Assert.That(GetControlFromField<RichTextLabel>("RepeatValue", details).GetMessage(),
                Is.EqualTo(Loc.GetString("admin-events-info-minutes", ("minutes", 30))));
            Assert.That(GetControlFromField<RichTextLabel>("LimitValue", details).GetMessage(),
                Is.EqualTo(Loc.GetString("admin-events-entry-unlimited")));
            Assert.That(GetControlFromField<RichTextLabel>("RoundEndValue", details).GetMessage(),
                Is.EqualTo(Loc.GetString("admin-events-entry-round-end-blocked")));
            AssertDraws(details);
        });
        await Client.WaitPost(() => details.SetSize = new Vector2(420, 300));
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var table = GetControlFromField<TableContainer>("ConditionsTable", details);
            var cells = table.Children.ToArray();
            Assert.That(cells, Has.Length.EqualTo(16));
            for (var row = 0; row < cells.Length; row += 2)
            {
                Assert.That(cells[row + 1].Position.X, Is.GreaterThanOrEqualTo(cells[row].Position.X + cells[row].Size.X));
                Assert.That(cells[row + 1].Position.Y, Is.EqualTo(cells[row].Position.Y));
                Assert.That(cells[row + 1].Position.X + cells[row + 1].Size.X, Is.LessThanOrEqualTo(table.Size.X));
            }
            AssertDraws(details);
        });
        await Client.WaitPost(details.Close);
        await OpenInfo("RandomSentience");
        await Client.WaitAssertion(() =>
        {
            Assert.That(GetControlFromField<RichTextLabel>("LimitValue", details).GetMessage(), Is.EqualTo("1"));
            Assert.That(GetControlFromField<RichTextLabel>("RoundEndValue", details).GetMessage(),
                Is.EqualTo(Loc.GetString("admin-events-entry-round-end-allowed")));
        });
        await Client.WaitPost(details.Close);
        await OpenInfo("AdminEventsTestReady");
        await Client.WaitAssertion(() => Assert.That(
            GetControlFromField<RichTextLabel>("Description", details).GetMessage(),
            Is.EqualTo("An event description for the information window.")));

        await Server.WaitPost(() =>
        {
            var ticker = SEntMan.System<GameTicker>();
            var ready = ticker.AddGameRule("AdminEventsTestReady");
            ticker.StartGameRule(ready);
            ticker.EndGameRule(ready);
            var repeatable = ticker.AddGameRule("AdminEventsTestRepeatable");
            ticker.StartGameRule(repeatable);
            ticker.EndGameRule(repeatable);
        });
        // Keep the information window open while refreshing its data through the main window.
        await Client.WaitPost(window.MoveToFront);
        await ClickControl(window.RefreshButton);
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(EntryCategory("AdminEventsTestReady"), Is.EqualTo("admin-events-category-triggered"));
            Assert.That(EntryCategory("AdminEventsTestRepeatable"), Is.EqualTo("admin-events-category-triggered"));
            Assert.That(EntryLabel("AdminEventsTestReady").Modulate, Is.EqualTo(Color.LightCoral));
            Assert.That(EntryLabel("AdminEventsTestRepeatable").Modulate, Is.EqualTo(Color.White));
            Assert.That(HasEntry(Loc.GetString("admin-events-category-triggered", ("count", 2))), Is.True);
            Assert.That(EntryRows().Select(row => row.Name), Is.Unique);
            Assert.That(GetControlFromField<RichTextLabel>("OccurrencesValue", details).GetMessage(), Is.EqualTo("1"));
        });

        await Server.WaitPost(() => Server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars.EventsEnabled, false));
        await ClickControl(window.RefreshButton);
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(EntryCategory("AdminEventsTestRepeatable"), Is.EqualTo("admin-events-category-triggered"));
            Assert.That(EntryLabel("AdminEventsTestRepeatable").Modulate, Is.EqualTo(Color.LightCoral));
            Assert.That(HasEntry(Loc.GetString("admin-events-category-waiting", ("count", 0))), Is.True);
            Assert.That(EntryLabel("AdminEventsTestReady").Modulate, Is.EqualTo(Color.LightCoral));
            Assert.That(GetControlFromField<RichTextLabel>("Availability", details).GetMessage(),
                Is.EqualTo(Loc.GetString("admin-events-unavailable-disabled")));
        });
        await Client.WaitPost(details.Close);

        await ClickControl(add);
        var picker = GetWindow<AdminGameRulePickerWindow>();
        var search = GetControlFromField<LineEdit>("Search", picker);
        var choices = GetControlFromField<BoxContainer>("RuleList", picker);
        var confirm = GetControlFromField<Button>("ConfirmButton", picker);
        await Client.WaitAssertion(() =>
        {
            Assert.That(choices.Children.OfType<Button>().Any(button => button.Text == "DragonSpawn"), Is.False);
            Assert.That(choices.Children.OfType<Button>().Any(button => button.Text == "BasicStationEventScheduler"), Is.True);
            Assert.That(confirm.Disabled, Is.True);
            AssertDraws(picker);
        });
        var eventCategories = GetControlFromField<OptionButton>("EventCategoryFilter", picker);
        async Task SelectFilter(OptionButton filter, string key)
        {
            var text = string.Empty;
            await Client.WaitPost(() => text = Loc.GetString(key));
            await ClickControl(filter);
            var buttons = filter.OptionsScroll.Children.OfType<BoxContainer>().Single()
                .Children.OfType<BoxContainer>().Single().Children.OfType<Button>();
            await ClickControl(buttons.Single(button => button.Text == text));
            await RunUntilSynced();
        }
        await Client.WaitPost(() => search.SetText("NoSuchGameRuleForThisTest", invokeEvent: true));
        await RunUntilSynced();
        await Client.WaitAssertion(() => Assert.That(choices.Children.OfType<Button>(), Is.Empty));
        await Client.WaitPost(() => search.SetText("RampingStationEventScheduler", invokeEvent: true));
        await RunUntilSynced();
        await ClickControl(choices.Children.OfType<Button>().Single(button => button.Text == "RampingStationEventScheduler"));
        await ClickControl(confirm);
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(picker.IsOpen, Is.False);
            Assert.That(RuleButton("RampingStationEventScheduler").Pressed, Is.True);
        });
        await Server.WaitAssertion(() => Assert.That(SEntMan.System<GameTicker>().GetAddedGameRules()
            .Count(uid => SEntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == "RampingStationEventScheduler"), Is.EqualTo(1)));

        await ClickControl(RuleButton("DynamicStationEventScheduler"));
        await Client.WaitAssertion(() =>
        {
            Assert.That(RuleButton("AdminEventsTestScheduler").Pressed, Is.False);
            Assert.That(HasEntry("DynamicGameRulesTable"), Is.True);
            Assert.That(HasEntry("AdminEventsTestTable"), Is.False);
        });

        await ClickControl(window.RefreshButton);
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(RuleButton("DynamicStationEventScheduler").Pressed, Is.True);
            Assert.That(HasEntry("DynamicGameRulesTable"), Is.True);
        });

        await Client.WaitPost(() => window.SetSize = new Vector2(760, 420));
        await RunUntilSynced();
        await Client.WaitAssertion(AssertPaneLayout);

        await ClickControl(RuleButton("DynamicStationEventScheduler"));
        await ClickControl(stop);
        await RunUntilSynced();
        await Server.WaitAssertion(() => Assert.That(SEntMan.System<GameTicker>().IsGameRuleAdded(dynamicRule), Is.False));
        await Client.WaitAssertion(() =>
        {
            Assert.That(RuleButton("AdminEventsTestScheduler").Pressed, Is.True);
            Assert.That(HasEntry("DynamicGameRulesTable"), Is.False);
            Assert.That(HasEntry("AdminEventsTestTable"), Is.True);
        });

        await Client.WaitPost(() => tabs.CurrentTab = 1);
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(eventRules.VisibleInTree, Is.True);
            Assert.That(rules.VisibleInTree, Is.False);
            Assert.That(history.Children.OfType<PanelContainer>().Select(row => row.Name), Is.EqualTo(new[]
            {
                "AdminEventsTestRepeatable", "AdminEventsTestReady",
            }));
            Assert.That(stopEvent.Disabled, Is.True);
            AssertDraws(window);
        });
        await ClickControl(addEvent);
        await Client.WaitAssertion(() =>
        {
            Assert.That(choices.Children.OfType<Button>().Any(button => button.Text == "DragonSpawn"), Is.True);
            Assert.That(choices.Children.OfType<Button>().Single(button => button.Text == "AnomalySpawn").ToolTip,
                Does.Contain(Loc.GetString("admin-events-delay-range", ("min", 10), ("max", 20))));
            Assert.That(choices.Children.OfType<Button>().Single(button => button.Text == "BluespaceArtifact").ToolTip,
                Does.Contain(Loc.GetString("admin-events-delay-fixed", ("seconds", 30))));
            Assert.That(choices.Children.OfType<Button>().Any(button => button.Text == "BasicStationEventScheduler"), Is.False);
            Assert.That(choices.Children.OfType<Button>().Any(button => button.Text == "Sandbox"), Is.False);
        });
        await SelectFilter(eventCategories, "admin-events-subgroup-antagonists");
        await Client.WaitAssertion(() =>
        {
            Assert.That(choices.Children.OfType<Button>().Any(button => button.Text == "DragonSpawn"), Is.True);
            Assert.That(choices.Children.OfType<Button>().Any(button => button.Text == "GiftsEngineering"), Is.False);
        });
        await SelectFilter(eventCategories, "admin-events-add-all-events");
        await Client.WaitPost(() => search.SetText("AdminEventsTestReady", invokeEvent: true));
        await RunUntilSynced();
        await ClickControl(choices.Children.OfType<Button>().Single());
        await ClickControl(confirm);
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(tabs.CurrentTab, Is.EqualTo(1));
            Assert.That(eventRules.Children.OfType<Button>().Single().Pressed, Is.True);
            Assert.That(stopEvent.Disabled, Is.False);
            Assert.That(history.Children.OfType<PanelContainer>().Count(), Is.EqualTo(3));
        });
        await ClickControl(stopEvent);
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(eventRules.Children.OfType<Button>(), Is.Empty);
            Assert.That(stopEvent.Disabled, Is.True);
            Assert.That(history.Children.OfType<PanelContainer>().Count(), Is.EqualTo(3));
            Assert.That(history.Children.OfType<PanelContainer>().First().Name, Is.EqualTo("AdminEventsTestReady"));
            Assert.That(LatestHistory().Children.OfType<BoxContainer>().Single().Children.OfType<Label>()
                .Single(label => label.Name == "HistoryStatus").Text, Is.EqualTo(Loc.GetString("admin-events-history-status-stopped")));
            Assert.That(HistoryValue("admin-events-history-source"),
                Is.EqualTo(Loc.GetString("admin-events-history-source-admin", ("name", ServerSession.Name))));
            Assert.That(HistoryValue("admin-events-history-reason"), Does.Contain(ServerSession.Name));
            Assert.That(HistoryValue("admin-events-history-started"), Does.Match(@"\d+:\d{2}:\d{2}"));
            Assert.That(HistoryValue("admin-events-history-ended"), Does.Match(@"\d+:\d{2}:\d{2}"));
            AssertDraws(window);
        });
        await Server.WaitAssertion(() => Assert.That(SEntMan.System<GameTicker>().GetAddedGameRules()
            .Any(uid => SEntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == "AdminEventsTestReady"), Is.False));
        await Client.WaitPost(() => tabs.CurrentTab = 0);
        await RunUntilSynced();
        await Client.WaitAssertion(() => Assert.That(RuleButton("AdminEventsTestScheduler").Pressed, Is.True));

        await Server.WaitPost(() => SEntMan.System<GameTicker>().ClearGameRules());
        await ClickControl(window.RefreshButton);
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(rules.Children.OfType<Button>(), Is.Empty);
            Assert.That(stop.Disabled, Is.True);
            Assert.That(HasEntry("AdminEventsTestTable"), Is.False);
        });

        await Server.WaitPost(() => SEntMan.System<GameTicker>().AddGameRule("BasicStationEventScheduler"));
        await ClickControl(window.RefreshButton);
        await RunUntilSynced();
        await OpenInfo(EntryRows().First().Name);
        await Client.WaitAssertion(() => Assert.That(details.IsOpen, Is.True));
        await Client.WaitPost(window.MoveToFront);
        await ClickControl(add);
        await Client.WaitAssertion(() => Assert.That(picker.IsOpen, Is.True));

        await Server.WaitPost(() => admins.DeAdmin(ServerSession));
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(window.IsOpen, Is.False);
            Assert.That(details.IsOpen, Is.False);
            Assert.That(picker.IsOpen, Is.False);
        });
        await Server.WaitAssertion(() => Assert.That(
            Server.ResolveDependency<IConGroupController>().CanCommand(ServerSession, "eventsui"), Is.False));
    }

    private void AssertDraws(Control control)
    {
        // Run the real UI/style drawing code, replacing only the graphics backend.
        // Headless test ticks otherwise skip rendering and would miss UIBox2 assertions.
        var screen = new Mock<DrawingHandleScreen>(Texture.White) { CallBase = true };
        screen.Setup(handle => handle.GetTransform()).Returns(Matrix3x2.Identity);
        var render = new Mock<IRenderHandle>();
        render.SetupGet(handle => handle.DrawingHandleScreen).Returns(screen.Object);
        Client.ResolveDependency<IUserInterfaceManager>().RenderControl(render.Object, control, Vector2i.Zero);
        Assert.That(screen.Invocations, Is.Not.Empty);
    }
}
