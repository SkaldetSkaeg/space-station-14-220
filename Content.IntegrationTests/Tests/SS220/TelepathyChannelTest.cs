// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

#nullable enable
using System.Linq;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Chat;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Shared.Chat;
using Content.Shared.SS220.Telepathy;
using Robust.Client.State;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.SS220;

/// <summary>
/// Telepathy selection follows the local player's component, including changes without a server notification.
/// </summary>
public sealed class TelepathyChannelTest : InteractionTest
{
    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();
        await Server.WaitPost(() => Server.ResolveDependency<IAdminManager>().DeAdmin(ServerSession));
        await Pair.RunUntilSynced();
    }

    [Test]
    public async Task ClientComponentChangesUpdateExistingSelector()
    {
        await Client.WaitAssertion(() =>
        {
            var chat = UiMan.GetUIController<ChatUIController>();
            using var selector = new ChannelSelectorPopup();
            AssertChannels(chat, false);
            AssertSelector(selector, false);

            // Exercise the client lifecycle without a separate notification from the server.
            CEntMan.AddComponent<TelepathyComponent>(CPlayer);
            CEntMan.FrameUpdate(0f);
            AssertChannels(chat, true);
            AssertSelector(selector, true);

            CEntMan.RemoveComponent<TelepathyComponent>(CPlayer);
            CEntMan.FrameUpdate(0f);
            AssertChannels(chat, false);
            AssertSelector(selector, false);

            // Several changes in a frame must use the final component state.
            CEntMan.AddComponent<TelepathyComponent>(CPlayer);
            CEntMan.RemoveComponent<TelepathyComponent>(CPlayer);
            CEntMan.AddComponent<TelepathyComponent>(CPlayer);
            CEntMan.FrameUpdate(0f);
            AssertChannels(chat, true);
            AssertSelector(selector, true);

            CEntMan.RemoveComponent<TelepathyComponent>(CPlayer);
            CEntMan.FrameUpdate(0f);
            AssertChannels(chat, false);
            AssertSelector(selector, false);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ReplicatedComponentAddsAndRemovesChannelWithoutReattaching(bool reconnectFirst)
    {
        if (reconnectFirst)
            await Reconnect();

        await Client.WaitAssertion(() => AssertChannels(UiMan.GetUIController<ChatUIController>(), false));
        await Server.WaitPost(() => SEntMan.AddComponent<TelepathyComponent>(SPlayer));
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(ClientSession.AttachedEntity, Is.EqualTo(CPlayer));
            Assert.That(CEntMan.HasComponent<TelepathyComponent>(CPlayer), Is.True);
            AssertChannels(UiMan.GetUIController<ChatUIController>(), true);
        });

        await Server.WaitPost(() => SEntMan.RemoveComponent<TelepathyComponent>(SPlayer));
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(ClientSession.AttachedEntity, Is.EqualTo(CPlayer));
            Assert.That(CEntMan.HasComponent<TelepathyComponent>(CPlayer), Is.False);
            AssertChannels(UiMan.GetUIController<ChatUIController>(), false);
        });
    }

    [Test]
    public async Task FieldsSynchronizeOnCreationAndExistingComponentChanges()
    {
        await Server.WaitPost(() =>
        {
            var telepathy = SEntMan.AddComponent<TelepathyComponent>(SPlayer);
            telepathy.CanSend = true;
            telepathy.TelepathyChannelPrototype = "TelepathyTestDynamicChannel";
            telepathy.ReceiveAllChannels = true;
            SEntMan.Dirty(SPlayer, telepathy);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
            AssertFields(CEntMan.GetComponent<TelepathyComponent>(CPlayer), true, "TelepathyTestDynamicChannel", true));

        await Server.WaitPost(() =>
        {
            var telepathy = SEntMan.GetComponent<TelepathyComponent>(SPlayer);
            telepathy.CanSend = false;
            telepathy.TelepathyChannelPrototype = null;
            telepathy.ReceiveAllChannels = false;
            SEntMan.Dirty(SPlayer, telepathy);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
            AssertFields(CEntMan.GetComponent<TelepathyComponent>(CPlayer), false, null, false));

        await Server.WaitPost(() =>
        {
            var telepathy = SEntMan.GetComponent<TelepathyComponent>(SPlayer);
            telepathy.CanSend = true;
            telepathy.TelepathyChannelPrototype = "TelepathyTestReplacementChannel";
            SEntMan.Dirty(SPlayer, telepathy);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
            AssertFields(CEntMan.GetComponent<TelepathyComponent>(CPlayer), true, "TelepathyTestReplacementChannel", false));
    }

    [Test]
    public async Task PrivateStateIsSentOnlyWhileAttachedToEntity()
    {
        EntityUid other = default;
        NetEntity otherNet = default;
        EntityUid clientOther = default;
        await Server.WaitPost(() =>
        {
            other = SEntMan.SpawnEntity(PlayerPrototype, SEntMan.GetCoordinates(PlayerCoords));
            otherNet = SEntMan.GetNetEntity(other);
            var telepathy = SEntMan.AddComponent<TelepathyComponent>(other);
            telepathy.CanSend = true;
            telepathy.TelepathyChannelPrototype = "TelepathyTestPrivateChannel";
            telepathy.ReceiveAllChannels = true;
            SEntMan.Dirty(other, telepathy);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            // The entity itself is visible, but another character's telepathy is private.
            Assert.That(CEntMan.TryGetEntity(otherNet, out var uid), Is.True);
            Assert.That(uid, Is.Not.Null);
            clientOther = uid!.Value;
            Assert.That(CEntMan.HasComponent<TelepathyComponent>(clientOther), Is.False);
            AssertChannels(UiMan.GetUIController<ChatUIController>(), false);
        });

        await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, other));
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(ClientSession.AttachedEntity, Is.EqualTo(clientOther));
            AssertFields(CEntMan.GetComponent<TelepathyComponent>(clientOther), true, "TelepathyTestPrivateChannel", true);
            AssertChannels(UiMan.GetUIController<ChatUIController>(), true);
        });

        await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, SPlayer));
        await Pair.RunUntilSynced();
        await Server.WaitPost(() =>
        {
            var telepathy = SEntMan.GetComponent<TelepathyComponent>(other);
            telepathy.TelepathyChannelPrototype = "TelepathyTestHiddenChange";
            SEntMan.Dirty(other, telepathy);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(ClientSession.AttachedEntity, Is.EqualTo(CPlayer));
            AssertChannels(UiMan.GetUIController<ChatUIController>(), false);
            if (CEntMan.TryGetComponent<TelepathyComponent>(clientOther, out var telepathy))
                Assert.That(telepathy.TelepathyChannelPrototype?.Id, Is.Not.EqualTo("TelepathyTestHiddenChange"));
        });
    }

    private async Task Reconnect()
    {
        var username = ClientSession.Name;
        var netManager = Client.ResolveDependency<IClientNetManager>();
        await Client.WaitPost(() => netManager.ClientDisconnect("Telepathy reconnect test"));
        await Pair.RunTicksSync(5);
        // InteractionTest attaches directly without joining a game, so GameTicker skips this cleanup.
        await Server.WaitPost(() => Server.ResolveDependency<UserDbDataManager>().ClientDisconnected(ServerSession));
        Client.SetConnectTarget(Server);
        await Client.WaitPost(() => netManager.ClientConnect(null!, 0, username));
        await Pair.RunTicksSync(20);
        await Server.WaitAssertion(() =>
        {
            ServerSession = Server.PlayerMan.Sessions.Single();
            Server.ResolveDependency<IAdminManager>().DeAdmin(ServerSession);
            Server.PlayerMan.SetAttachedEntity(ServerSession, SPlayer);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(Client.Session, Is.Not.Null);
            ClientSession = Client.Session!;
            CPlayer = CEntMan.GetEntity(Player);
            Client.ResolveDependency<IStateManager>().RequestStateChange<GameplayState>();
            Assert.That(ClientSession.AttachedEntity, Is.EqualTo(CPlayer));
        });
    }

    private static void AssertFields(
        TelepathyComponent telepathy,
        bool canSend,
        ProtoId<TelepathyChannelPrototype>? channel,
        bool receiveAllChannels)
    {
        Assert.Multiple(() =>
        {
            Assert.That(telepathy.CanSend, Is.EqualTo(canSend));
            Assert.That(telepathy.TelepathyChannelPrototype, Is.EqualTo(channel));
            Assert.That(telepathy.ReceiveAllChannels, Is.EqualTo(receiveAllChannels));
        });
    }

    private static void AssertChannels(ChatUIController chat, bool expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(chat.CanSendChannels.HasFlag(ChatSelectChannel.Telepathy), Is.EqualTo(expected));
            Assert.That(chat.SelectableChannels.HasFlag(ChatSelectChannel.Telepathy), Is.EqualTo(expected));
            Assert.That(chat.FilterableChannels.HasFlag(ChatChannel.Telepathy), Is.EqualTo(expected));
        });
    }

    private static void AssertSelector(ChannelSelectorPopup selector, bool expected)
    {
        var hasTelepathy = selector.Children.Single().Children
            .OfType<ChannelSelectorItemButton>()
            .Any(button => button.Channel == ChatSelectChannel.Telepathy);
        Assert.That(hasTelepathy, Is.EqualTo(expected));
    }
}
